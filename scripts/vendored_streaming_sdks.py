#!/usr/bin/env python3
from __future__ import annotations

import argparse
import json
import os
import shutil
import subprocess
import sys
import tarfile
import tempfile
from pathlib import Path

REPO_ROOT = Path(__file__).resolve().parents[1]
DEFAULT_MANIFEST = REPO_ROOT / "vendored-streaming-sdks.json"
GITHUB_API_BASE = "https://api.github.com/repos"
USER_AGENT = "PrompterLive-vendored-streaming-sdks"


def parse_args() -> argparse.Namespace:
    parser = argparse.ArgumentParser(description="Manage pinned vendored streaming SDK releases.")
    parser.add_argument(
        "--manifest",
        type=Path,
        default=DEFAULT_MANIFEST,
        help="Path to the vendored streaming SDK manifest.",
    )

    subparsers = parser.add_subparsers(dest="command", required=True)

    sync_parser = subparsers.add_parser("sync", help="Sync vendored SDK files from pinned release metadata.")
    sync_parser.add_argument("--sdk", action="append", dest="sdk_ids", default=[], help="Limit sync to a specific sdk id.")

    verify_parser = subparsers.add_parser("verify", help="Verify the pinned vendor directories match the manifest.")
    verify_parser.add_argument("--sdk", action="append", dest="sdk_ids", default=[], help="Limit verification to a specific sdk id.")

    updates_parser = subparsers.add_parser("check-updates", help="Check upstream GitHub releases against the pinned tags.")
    updates_parser.add_argument("--summary-json", type=Path, help="Optional path to write update summary JSON.")
    updates_parser.add_argument("--issue-body", type=Path, help="Optional path to write a GitHub issue body when updates exist.")

    return parser.parse_args()


def load_manifest(path: Path) -> dict:
    return json.loads(path.read_text(encoding="utf-8"))


def select_sdks(manifest: dict, requested_ids: list[str]) -> list[dict]:
    if not requested_ids:
        return manifest["sdks"]

    requested = set(requested_ids)
    selected = [sdk for sdk in manifest["sdks"] if sdk["id"] in requested]
    missing = requested - {sdk["id"] for sdk in selected}
    if missing:
        raise SystemExit(f"Unknown sdk id(s): {', '.join(sorted(missing))}")
    return selected


def github_request(url: str) -> bytes:
    arguments = [
        "curl",
        "--fail",
        "--silent",
        "--show-error",
        "--location",
        "--header",
        "Accept: application/vnd.github+json",
        "--header",
        f"User-Agent: {USER_AGENT}",
    ]

    token = os.environ.get("GITHUB_TOKEN")
    if token:
        arguments.extend(["--header", f"Authorization: Bearer {token}"])

    arguments.append(url)
    result = subprocess.run(arguments, check=True, capture_output=True)
    return result.stdout


def download_to(url: str, destination: Path) -> None:
    destination.parent.mkdir(parents=True, exist_ok=True)
    destination.write_bytes(github_request(url))


def extract_tarball(archive_path: Path, destination: Path) -> Path:
    with tarfile.open(archive_path, mode="r:gz") as archive:
        archive.extractall(destination, filter="data")

    extracted_root = next(path for path in destination.iterdir() if path.is_dir())
    return extracted_root


def reset_directory(path: Path) -> None:
    shutil.rmtree(path, ignore_errors=True)
    path.mkdir(parents=True, exist_ok=True)


def copy_file(source: Path, destination: Path) -> None:
    destination.parent.mkdir(parents=True, exist_ok=True)
    shutil.copy2(source, destination)


def run_command(arguments: list[str], working_directory: Path) -> None:
    subprocess.run(arguments, cwd=working_directory, check=True)


def sync_livekit(sdk: dict) -> None:
    vendor_directory = REPO_ROOT / sdk["vendorDirectory"]
    reset_directory(vendor_directory)

    with tempfile.TemporaryDirectory(prefix=f"{sdk['id']}-") as temp_dir_name:
        temp_dir = Path(temp_dir_name)
        archive_path = temp_dir / "release.tar.gz"
        download_to(sdk["sourceTarballUrl"], archive_path)
        source_root = extract_tarball(archive_path, temp_dir)

        pnpm_version = sdk["pnpmVersion"]
        run_command(["npx", "--yes", f"pnpm@{pnpm_version}", "install", "--frozen-lockfile"], source_root)
        run_command(["npx", "--yes", f"pnpm@{pnpm_version}", "build"], source_root)

        for license_file in sdk.get("licenseFiles", []):
            copy_file(source_root / license_file, vendor_directory / Path(license_file).name)

        for artifact in sdk["artifacts"]:
            copy_file(source_root / artifact, vendor_directory / Path(artifact).name)


def copy_js_tree(source_directory: Path, destination_directory: Path) -> None:
    for source_path in source_directory.rglob("*.js"):
        relative_path = source_path.relative_to(source_directory)
        copy_file(source_path, destination_directory / relative_path)


def sync_vdo_ninja(sdk: dict) -> None:
    vendor_directory = REPO_ROOT / sdk["vendorDirectory"]
    reset_directory(vendor_directory)

    with tempfile.TemporaryDirectory(prefix=f"{sdk['id']}-") as temp_dir_name:
        temp_dir = Path(temp_dir_name)
        archive_path = temp_dir / "release.tar.gz"
        download_to(sdk["sourceTarballUrl"], archive_path)
        source_root = extract_tarball(archive_path, temp_dir)

        for license_file in sdk.get("licenseFiles", []):
            license_path = source_root / license_file
            if license_path.exists():
                copy_file(license_path, vendor_directory / license_path.name)

        for root_file in sdk["rootFiles"]:
            copy_file(source_root / root_file, vendor_directory / root_file)

        for runtime_directory in sdk["runtimeDirectories"]:
            copy_js_tree(source_root / runtime_directory, vendor_directory / runtime_directory)


def sync_sdk(sdk: dict) -> None:
    strategy = sdk["syncStrategy"]
    print(f"Syncing {sdk['id']} from {sdk['releaseTag']} using {strategy}")

    if strategy == "livekit-build-from-release-source":
        sync_livekit(sdk)
        return

    if strategy == "copy-release-runtime-js-tree":
        sync_vdo_ninja(sdk)
        return

    raise SystemExit(f"Unsupported sync strategy: {strategy}")


def verify_sdk(sdk: dict) -> list[str]:
    vendor_directory = REPO_ROOT / sdk["vendorDirectory"]
    errors: list[str] = []

    if not vendor_directory.exists():
        return [f"{sdk['id']}: vendor directory is missing: {vendor_directory}"]

    for license_file in sdk.get("licenseFiles", []):
        license_name = Path(license_file).name
        if not (vendor_directory / license_name).exists():
            errors.append(f"{sdk['id']}: missing license file {license_name}")

    for artifact in sdk.get("artifacts", []):
        artifact_name = Path(artifact).name
        if not (vendor_directory / artifact_name).exists():
            errors.append(f"{sdk['id']}: missing artifact {artifact_name}")

    for root_file in sdk.get("rootFiles", []):
        if not (vendor_directory / root_file).exists():
            errors.append(f"{sdk['id']}: missing root file {root_file}")

    for runtime_directory in sdk.get("runtimeDirectories", []):
        runtime_path = vendor_directory / runtime_directory
        if not runtime_path.exists():
            errors.append(f"{sdk['id']}: missing runtime directory {runtime_directory}")
            continue

        if not any(runtime_path.rglob("*.js")):
            errors.append(f"{sdk['id']}: runtime directory {runtime_directory} does not contain js files")

    return errors


def fetch_latest_release_tag(repository: str) -> tuple[str, str]:
    payload = json.loads(github_request(f"{GITHUB_API_BASE}/{repository}/releases/latest").decode("utf-8"))
    return payload["tag_name"], payload["html_url"]


def build_issue_body(manifest: dict, updates: list[dict]) -> str:
    lines = [
        "# Vendored streaming SDK updates detected",
        "",
        "The pinned vendored browser SDKs are behind the latest GitHub releases.",
        "",
        "| SDK | Pinned | Latest | Pinned release | Latest release |",
        "| --- | --- | --- | --- | --- |",
    ]

    for update in updates:
        lines.append(
            f"| {update['displayName']} | `{update['pinnedTag']}` | `{update['latestTag']}` | "
            f"[pinned]({update['releaseUrl']}) | [latest]({update['latestReleaseUrl']}) |"
        )

    lines.extend(
        [
            "",
            "## Manual refresh",
            "",
            "1. Update the pinned tags and URLs in `vendored-streaming-sdks.json`.",
            "2. Run `python scripts/vendored_streaming_sdks.py sync`.",
            "3. Review the vendored files under `src/PrompterLive.Shared/wwwroot/vendor/`.",
            "4. Commit the manifest and vendored file changes together.",
            "",
            f"Manifest issue title: `{manifest['issueTitle']}`",
        ]
    )

    return "\n".join(lines) + "\n"


def command_sync(arguments: argparse.Namespace) -> int:
    manifest = load_manifest(arguments.manifest)
    for sdk in select_sdks(manifest, arguments.sdk_ids):
        sync_sdk(sdk)
    return 0


def command_verify(arguments: argparse.Namespace) -> int:
    manifest = load_manifest(arguments.manifest)
    errors: list[str] = []
    for sdk in select_sdks(manifest, arguments.sdk_ids):
        errors.extend(verify_sdk(sdk))

    if errors:
        for error in errors:
            print(error, file=sys.stderr)
        return 1

    print("Vendored streaming SDK files match the pinned manifest.")
    return 0


def command_check_updates(arguments: argparse.Namespace) -> int:
    manifest = load_manifest(arguments.manifest)
    updates: list[dict] = []

    for sdk in manifest["sdks"]:
        latest_tag, latest_release_url = fetch_latest_release_tag(sdk["repository"])
        if latest_tag != sdk["releaseTag"]:
            updates.append(
                {
                    "id": sdk["id"],
                    "displayName": sdk["displayName"],
                    "repository": sdk["repository"],
                    "pinnedTag": sdk["releaseTag"],
                    "latestTag": latest_tag,
                    "releaseUrl": sdk["releaseUrl"],
                    "latestReleaseUrl": latest_release_url,
                }
            )

    summary = {"hasUpdates": bool(updates), "issueTitle": manifest["issueTitle"], "updates": updates}

    if arguments.summary_json:
        arguments.summary_json.parent.mkdir(parents=True, exist_ok=True)
        arguments.summary_json.write_text(json.dumps(summary, indent=2) + "\n", encoding="utf-8")

    if arguments.issue_body:
        arguments.issue_body.parent.mkdir(parents=True, exist_ok=True)
        body = build_issue_body(manifest, updates) if updates else ""
        arguments.issue_body.write_text(body, encoding="utf-8")

    print(json.dumps(summary, indent=2))
    return 0


def main() -> int:
    arguments = parse_args()
    if arguments.command == "sync":
        return command_sync(arguments)
    if arguments.command == "verify":
        return command_verify(arguments)
    if arguments.command == "check-updates":
        return command_check_updates(arguments)
    raise SystemExit(f"Unsupported command: {arguments.command}")


if __name__ == "__main__":
    raise SystemExit(main())
