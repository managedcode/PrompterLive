# Legacy Product Prefix Cleanup Plan

## Goal

Finish the product rename by replacing every remaining repo-owned legacy product prefix with the `PrompterOne` and `prompterOne` naming, without regressing the standalone Blazor WebAssembly runtime, JS interop, or browser acceptance suite.

## Scope

### In Scope

- rename remaining repo-owned runtime identifiers that still use the old product prefix in tracked source files
- rename matching browser-test constants and harness globals so the browser suite stays aligned with production contracts
- rename local ignored IDE artifacts that still use the old product name when that can be done safely without changing tracked repo state
- re-scan tracked content and relevant local metadata for any residual legacy product-prefix references after the code changes

### Out Of Scope

- broad refactors unrelated to the rename cleanup
- changes to third-party brand names such as `LiveKit` where `Live` is part of the vendor or protocol name rather than the old product name
- generated `bin/`, `obj/`, or other rebuild output

## Current State

- The main solution, projects, namespaces, and docs already use `PrompterOne`.
- Remaining tracked hits are limited to browser JS globals, interop method names, and browser-test harness constants.
- Remaining filesystem-name hits are in ignored local IDE artifacts such as `.idea/` state and `*.DotSettings.user`.

## Constraints And Risks

- The repo mandates a root-level plan file and a full baseline before non-trivial edits.
- Browser acceptance is the primary gate, so any renamed JS global or interop string must be changed consistently in production and tests.
- `LiveKit` identifiers must stay intact where they refer to the external SDK rather than the product prefix.
- Ignored IDE files are local-only and must not distract from tracked source correctness, but renaming them is acceptable if it is safe and improves workspace consistency.

## Testing Methodology

- Establish a full repo baseline with the required `build` and `test` commands before edits.
- After the rename changes, run targeted source scans to confirm the legacy product prefix is gone from tracked content.
- Run the full repo quality pass in repo order: `build`, `test`, `coverage`, then `format`.
- Quality bar:
  - no tracked legacy product-prefix references remain unless documented as an intentional vendor name
  - `dotnet build /Users/ksemenenko/Developer/PrompterOne/PrompterOne.slnx -warnaserror` succeeds
  - `dotnet test /Users/ksemenenko/Developer/PrompterOne/PrompterOne.slnx` succeeds
  - `dotnet test /Users/ksemenenko/Developer/PrompterOne/PrompterOne.slnx --collect:"XPlat Code Coverage"` succeeds
  - `dotnet format /Users/ksemenenko/Developer/PrompterOne/PrompterOne.slnx` succeeds

## Ordered Steps

### 1. Freeze Scope And Inventory The Remaining Hits

- [x] Search tracked file contents for the legacy product prefix and related case variants while excluding generated output.
- [x] Search file paths for legacy-name leftovers and classify which ones are tracked repo content versus ignored local metadata.
- [x] Record the rename target set and validation sequence in this plan file.
- Verify before moving on:
  - the remaining hit list is concrete and small enough to change intentionally rather than with blind global replacement
  - vendor names such as `LiveKit` are explicitly excluded from the rename

### 2. Establish The Full Baseline Before Edits

- [x] Run `dotnet build /Users/ksemenenko/Developer/PrompterOne/PrompterOne.slnx -warnaserror`.
- [x] Run `dotnet test /Users/ksemenenko/Developer/PrompterOne/PrompterOne.slnx`.
- [x] Record each baseline failure below with symptom, suspected cause, and intended fix path before changing source.
- Verify before moving on:
  - baseline build status is captured
  - baseline test status is captured
  - every failing baseline test, if any, is tracked below as a checklist item

## Baseline Failures

- [x] No baseline failures.
  - Symptom: none. `dotnet build /Users/ksemenenko/Developer/PrompterOne/PrompterOne.slnx -warnaserror` and `dotnet test /Users/ksemenenko/Developer/PrompterOne/PrompterOne.slnx` both passed before edits.
  - Suspected cause: not applicable.
  - Intended fix path: preserve the green baseline through the rename cleanup and rerun the same required gates afterward.

### 3. Rename Tracked Repo-Owned Contracts

- [x] Update production browser interop identifiers in `/Users/ksemenenko/Developer/PrompterOne/src/PrompterOne.Shared/wwwroot/media/browser-media.js`, `/Users/ksemenenko/Developer/PrompterOne/src/PrompterOne.Shared/wwwroot/theme/browser-theme.js`, and `/Users/ksemenenko/Developer/PrompterOne/src/PrompterOne.Shared/Settings/Services/BrowserThemeInteropMethodNames.cs`.
- [x] Update the matching browser-test constants and synthetic harness globals in `/Users/ksemenenko/Developer/PrompterOne/tests/PrompterOne.App.UITests/Media/synthetic-media-harness.js`, `/Users/ksemenenko/Developer/PrompterOne/tests/PrompterOne.App.UITests/Media/BrowserTestConstants.Media.cs`, `/Users/ksemenenko/Developer/PrompterOne/tests/PrompterOne.App.UITests/Support/BrowserTestConstants.cs`, and `/Users/ksemenenko/Developer/PrompterOne/tests/PrompterOne.App.Tests/Support/AppTestData.cs`.
- [x] Re-scan tracked source for legacy product-prefix references to confirm only intentional vendor names remain.
- Verify before moving on:
  - each renamed production contract has a matching test-side update
  - the remaining tracked hits, if any, are intentional and documented
- Outcome:
  - tracked source scans are clean for the legacy product prefix after the contract rename
  - `LiveKit` vendor identifiers remain intact while only the repo-owned harness global prefix changed

### 4. Rename Safe Local Metadata Leftovers

- [x] Rename or remove ignored local IDE artifacts that still use the old product name, including the legacy solution `.DotSettings.user` file and the stale Rider `.idea` directory, if they still exist after source changes.
- [x] Re-scan local file paths for old-product-name leftovers outside generated output.
- Verify before moving on:
  - the rename does not modify tracked repo content unexpectedly
  - local-only leftovers are either renamed or explicitly called out as intentionally untouched
- Outcome:
  - the stale Rider `.idea` directory was removed because a current `.idea.PrompterOne` directory already existed
  - the local solution settings file now follows the current solution name as `PrompterOne.slnx.DotSettings.user`
  - the local filesystem scan is clean for the old product name outside excluded generated output

### 5. Final Validation And Plan Closeout

- [x] Run `dotnet build /Users/ksemenenko/Developer/PrompterOne/PrompterOne.slnx -warnaserror`.
- [x] Run `dotnet test /Users/ksemenenko/Developer/PrompterOne/PrompterOne.slnx`.
- [x] Run `dotnet test /Users/ksemenenko/Developer/PrompterOne/PrompterOne.slnx --collect:"XPlat Code Coverage"`.
- [x] Run `dotnet format /Users/ksemenenko/Developer/PrompterOne/PrompterOne.slnx`.
- [x] Update this plan with the final scan results, validation outcomes, and any intentionally untouched local-only residue.
- Verify before moving on:
  - all required repo commands pass
  - scans are clean for tracked repo-owned old-name prefixes
  - this plan reflects the actual end state
- Final outcome:
  - `dotnet build /Users/ksemenenko/Developer/PrompterOne/PrompterOne.slnx -warnaserror` passed after the cleanup
  - focused `PrompterOne.App.Tests` and `PrompterOne.App.UITests` runs passed after the contract rename
  - `dotnet test /Users/ksemenenko/Developer/PrompterOne/PrompterOne.slnx` passed after the cleanup
  - `dotnet test /Users/ksemenenko/Developer/PrompterOne/PrompterOne.slnx --collect:"XPlat Code Coverage"` passed and produced coverage artifacts for core, component, and browser suites
  - `dotnet format /Users/ksemenenko/Developer/PrompterOne/PrompterOne.slnx` completed successfully
  - tracked source scans and local filesystem-name scans are clean for the legacy product prefix

## Final Validation Skills And Commands

1. `dotnet-blazor`
   - Action: verify the Blazor WebAssembly host and JS interop contract names still line up after the rename cleanup.
   - Outcome: production browser scripts and C# interop constants stay consistent.
2. `playwright`
   - Action: rely on the browser acceptance suite executed through the repo `dotnet test` workflow to validate real-browser behavior after the renamed JS globals.
   - Outcome: browser flows keep working with the updated test harness contract names.
3. Repo quality commands
   - `dotnet build /Users/ksemenenko/Developer/PrompterOne/PrompterOne.slnx -warnaserror`
   - `dotnet test /Users/ksemenenko/Developer/PrompterOne/PrompterOne.slnx`
   - `dotnet test /Users/ksemenenko/Developer/PrompterOne/PrompterOne.slnx --collect:"XPlat Code Coverage"`
   - `dotnet format /Users/ksemenenko/Developer/PrompterOne/PrompterOne.slnx`
   - Reason: these are the mandatory repo-defined gates for this task.
