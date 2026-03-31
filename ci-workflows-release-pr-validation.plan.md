# CI Workflows: PR Validation And Release Plan

## Task Goal

Reshape the repository CI/CD so `PrompterLive` has:

- a clearly named pull-request validation workflow that runs build and tests for PRs
- a clearly named release workflow that builds the standalone app, creates or updates the release tag, publishes a GitHub Release, and keeps GitHub Pages deployment aligned with the release flow
- workflow naming and documentation that match the new ownership clearly

## Scope

### In Scope

- Audit the current `.github/workflows/*.yml` layout and identify gaps against the requested CI/CD shape.
- Add or update GitHub Actions workflows for PR validation and release automation.
- Rename workflows to explicit, purpose-driven names.
- Update repo documentation and architecture/build-governance notes where the canonical workflow ownership changes.
- Push the workflow changes and observe the resulting GitHub Actions runs.

### Out Of Scope

- Runtime feature changes in `src/` or `tests/`.
- Versioning model changes beyond what the release workflow needs to tag and publish releases safely.
- Non-GitHub CI providers or external package registries.

## Current State

- The existing GitHub Pages workflow now deploys successfully, but the deployed runtime is broken on `prompter.managed-code.com` because the artifact rewrote `<base href>` to `"/PrompterLive/"`.
- A dedicated PR validation workflow has been added locally but not pushed yet.
- The release workflow has been expanded locally into build/test -> publish -> GitHub Release -> GitHub Pages stages, but GitHub has not run that shape yet.
- Workflow naming is being normalized across repository automation.

## Constraints

- Keep the runtime browser-only and GitHub Pages compatible.
- Reuse repo-native commands from `AGENTS.md` instead of inventing alternate CI commands.
- Keep workflow ownership explicit: PR validation, release automation, and Pages deployment should each have clear triggers and names.
- Preserve automated app version injection from CI metadata.

## Risks

- A release workflow can create accidental duplicate tags if version calculation is not deterministic.
- Running deploy on the wrong trigger can cause duplicate Pages publishes or mismatched release metadata.
- Workflow naming or trigger changes can break existing contributor expectations if docs are not updated in the same task.
- End-to-end proof requires real GitHub Actions runs after push, not only local YAML linting.

## Testing Methodology

- Inventory baseline: capture current workflow files, names, and trigger responsibilities.
- Local workflow validation: run `actionlint` over all edited workflow files.
- Repo quality validation: run the repo `build`, `test`, and `format` commands that the new workflows are expected to own or depend on.
- GitHub validation: push the workflow changes, then watch the resulting Actions runs for PR/release/deploy-relevant jobs and confirm final status from GitHub.

Quality bar:

- Workflow names are explicit and stable.
- PR workflow runs repo build and tests on pull requests.
- Release workflow can derive a release version/tag and publish a GitHub Release without manual retagging steps.
- Pages deployment remains green under the updated workflow topology.

## Baseline Gap Tracking

- [x] `Missing PR validation workflow`: the repo lacked a dedicated, clearly named pull-request pipeline for build plus test.
  - Root cause note: only the Pages deploy workflow exists for app CI, so merge validation is coupled to post-push deployment instead of PR checks.
  - Intended fix path: add a PR validation workflow with repo-native build/test commands and clear naming.
  - Fix status: implemented locally

- [x] `Missing release workflow`: the repo lacked a dedicated workflow that creates or updates a release tag and publishes a GitHub Release.
  - Root cause note: current automation reacts to already-published releases but does not create them.
  - Intended fix path: add a release automation workflow that computes the release version, creates/updates the tag, and publishes the GitHub Release.
  - Fix status: implemented locally

- [x] `Workflow naming drift`: current workflow names did not fully reflect their actual purpose.
  - Root cause note: naming evolved around a single Pages deployment path instead of a full CI/CD model.
  - Intended fix path: rename workflows to purpose-specific names and update docs that reference them.
  - Fix status: implemented locally

- [x] `Broken custom-domain boot`: the deployed Pages artifact rewrote `<base href>` to `"/PrompterLive/"`, causing `_content` and `_framework` assets to resolve under a non-existent repo subpath on the custom domain.
  - Root cause note: the workflow still assumed repository Pages path hosting while the repo is configured for a custom-domain root deployment.
  - Intended fix path: keep `PAGES_BASE_PATH` at `/`, preserve root-relative asset loading, and ship `CNAME` in the Pages artifact.
  - Fix status: implemented locally, pending GitHub deploy verification

## Ordered Implementation Plan

1. Baseline inventory
   - Action: record the current workflow files, names, triggers, and the latest successful GitHub Pages deploy run.
   - Where: `.github/workflows/` and GitHub Actions metadata.
   - Verification before moving on: this plan explicitly captures the current gap list and the latest deploy status.

2. Versioning and release-shape review
   - Action: inspect `Directory.Build.props` and existing versioning docs to derive a deterministic release tag strategy that fits the current app version model.
   - Where: `Directory.Build.props`, `docs/Architecture.md`, and `docs/Features/AppVersioningAndGitHubPages.md`.
   - Verification before moving on: choose one release tag format and ensure it maps cleanly to the current build metadata.

3. Workflow design
   - Action: define the final workflow split, triggers, permissions, and artifact boundaries for PR validation, release automation, and Pages deployment.
   - Where: this plan and the target workflow YAML files.
   - Verification before moving on: the plan identifies which workflow owns which trigger and why.

4. Implement workflow updates
   - Action: create or update workflow YAML files for PR validation and release automation, and rename or refine the Pages deploy workflow as needed.
   - Where: `.github/workflows/`.
   - Verification before moving on: each workflow has clear naming, explicit triggers, and repo-native commands.

5. Update documentation
   - Action: update architecture/build-governance and feature docs so they describe the new workflow ownership, trigger model, and release/tag behavior.
   - Where: `docs/Architecture.md` and `docs/Features/AppVersioningAndGitHubPages.md`.
   - Verification before moving on: docs point to the new canonical workflows and keep Mermaid diagrams/rendering intact.

6. Local validation
   - Action: run `actionlint`, repo `build`, repo `test`, and repo `format`.
   - Where: repo root.
   - Verification before moving on: all local validation commands pass against the new workflow/docs state.

7. Push and observe GitHub runs
   - Action: commit and push only the scoped workflow/doc updates, then watch the relevant GitHub Actions runs to final status.
   - Where: `origin/main` unless the user redirects branch strategy.
   - Verification before moving on: GitHub shows the updated workflows and the triggered runs complete successfully or any remaining failure is captured with root cause.

8. Final closeout
   - Action: update this plan with the actual workflow names, validation evidence, and any residual GitHub-only caveats.
   - Where: this plan file and final task summary.
   - Verification before moving on: all checklist items are complete and each tracked gap is marked done or explicitly explained.

## Detailed Step Checklist

- [x] Create and maintain this plan file through task completion.
- [x] Capture current workflow inventory and latest deploy success status.
- [x] Define the release tag/version strategy against existing build metadata.
- [x] Implement a dedicated PR validation workflow.
- [x] Implement a dedicated release workflow with tag and GitHub Release publication.
- [x] Refine workflow naming so each workflow purpose is explicit.
- [x] Update architecture and feature docs for the new CI/CD ownership.
- [x] Run `actionlint` on the edited workflows.
- [x] Run `dotnet build /Users/ksemenenko/Developer/PrompterLive/PrompterLive.slnx -warnaserror`.
- [x] Run `dotnet test /Users/ksemenenko/Developer/PrompterLive/PrompterLive.slnx`.
- [x] Run `dotnet format /Users/ksemenenko/Developer/PrompterLive/PrompterLive.slnx`.
- [ ] Commit and push only the in-scope changes.
- [ ] Watch the resulting GitHub Actions runs and record final status.
- [ ] Update the tracked gap items with final fix notes.

## Results So Far

- Confirmed via `curl` that the broken public deploy served `<base href="/PrompterLive/">` on `prompter.managed-code.com`, which produced 404s for `/_content/*` and `/_framework/*` assets under the wrong subpath.
- Added `.github/workflows/pr-validation.yml` with a dedicated `Build And Test` job for pull requests.
- Converted `.github/workflows/deploy-github-pages.yml` into a staged `Release Pipeline` that now performs build/test first, resolves the release version from `Directory.Build.props`, publishes the release bundle, publishes a GitHub Release, and only then deploys GitHub Pages.
- Updated all `actions/*` usages in repo workflows to the latest official major versions currently published by GitHub:
  - `actions/checkout@v6`
  - `actions/configure-pages@v6`
  - `actions/deploy-pages@v5`
  - `actions/setup-dotnet@v5`
  - `actions/upload-artifact@v7`
  - `actions/upload-pages-artifact@v4`
  - `actions/download-artifact@v8`
  - `actions/setup-python@v6`
  - `actions/setup-node@v6`
  - `actions/github-script@v8`
- Local verification passed:
  - `actionlint .github/workflows/*.yml`
  - `dotnet build /Users/ksemenenko/Developer/PrompterLive/PrompterLive.slnx -warnaserror`
  - `dotnet test /Users/ksemenenko/Developer/PrompterLive/PrompterLive.slnx`
  - `dotnet format /Users/ksemenenko/Developer/PrompterLive/PrompterLive.slnx`

## Final Validation Skills And Commands

1. `dotnet-quality-ci`
   - Reason: keep CI workflows aligned with repo-native .NET quality gates instead of ad-hoc commands.
   - Concrete outcome: workflow commands mapped to repo-defined build/test/format gates.

2. GitHub Actions inspection
   - Reason: verify workflow topology and the final run status from GitHub, not only local YAML parsing.
   - Concrete outcome: run URLs and final conclusions for the updated workflows.

3. `actionlint`
   - Reason: validate workflow syntax locally before push.
   - Command: `actionlint .github/workflows/*.yml`
   - Concrete outcome: no workflow lint errors.

4. Repo build
   - Reason: prove the PR validation workflow’s build command is green.
   - Command: `dotnet build /Users/ksemenenko/Developer/PrompterLive/PrompterLive.slnx -warnaserror`
   - Concrete outcome: successful solution build.

5. Repo test
   - Reason: prove the PR validation workflow’s test command is green.
   - Command: `dotnet test /Users/ksemenenko/Developer/PrompterLive/PrompterLive.slnx`
   - Concrete outcome: successful repo test pass.

6. Repo format
   - Reason: satisfy the repo-required quality pass after workflow/docs changes.
   - Command: `dotnet format /Users/ksemenenko/Developer/PrompterLive/PrompterLive.slnx`
   - Concrete outcome: successful formatting pass.
