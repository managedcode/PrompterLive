# Fix UI Suite Split CI Plan

## Goal

Restore the repository to a state where the test-project split still passes the real all-tests entrypoint and the referenced GitHub Actions run failure pattern is understood and fixed.

## Scope

### In Scope

- Inspect the failing GitHub Actions run `24138107179` and capture the concrete failing test pattern.
- Run the full local all-tests entrypoint with `dotnet test --solution ./PrompterOne.slnx -m:1` and capture the real local baseline.
- Identify the root cause introduced by the UI-suite split and fix the test/configuration/infrastructure issues.
- Update repo commands/docs/config only where required to keep the solution-level test entrypoint correct.
- Re-run the full local verification path until the all-tests entrypoint and the affected suites are green.

### Out Of Scope

- Unrelated feature work outside the failing test/infrastructure path.
- Expanding the split further for wall-clock optimization before the current CI regression is green again.

## Constraints And Risks

- The browser test family must remain self-hosted and browser-realistic; no manual app startup or ad-hoc runners.
- Local verification must include `dotnet test --solution ./PrompterOne.slnx -m:1`.
- Browser test projects must still avoid unsafe overlapping local `dotnet build` or `dotnet test` processes.
- The fix should preserve the intended project split; do not collapse the suites back into one DLL unless the user explicitly asks for that reversal.
- CI and local failures may differ if solution-level test orchestration exposes runner- or discovery-level issues rather than scenario failures.

## Ordered Implementation Plan

- [x] Step 1. Capture the failing GitHub Actions baseline.
  - What: Read the failing GitHub Actions runs, identify failing jobs, error signatures, and the actual failing test counts or discovery failures.
  - Where: GitHub Actions logs for the referenced run.
  - Verify: Summarize the concrete red signal in this plan before editing code.

- [x] Step 2. Capture the local all-tests baseline from the real entrypoint.
  - What: Run the real solution-level entrypoint from repo root and record all failures, aborts, or discovery errors.
  - Where: Solution root.
  - Verify: Update the failing-tests section below with the exact local failures.

- [x] Step 3. Isolate the root cause introduced by the UI-suite split.
  - What: Determine whether the failure is in solution-level test orchestration, test discovery, assembly parallelization, shared browser harness ownership, CI workflow shape, or suite-specific test configuration.
  - Where: Test project configs, shared UI test base, workflow files, and failing suites.
  - Verify: Reproduce the issue with the smallest meaningful command before applying the fix.

- [ ] Step 4. Apply the minimal fix set while preserving the split suite architecture.
  - What: Adjust project/test configuration, shared harness code, or workflow/test entrypoint plumbing needed to make the split compatible with the all-tests entrypoint.
  - Where: Only the owning test/configuration files.
  - Verify: Run the directly affected command(s) and confirm the original failure signature is gone.

- [ ] Step 5. Re-run layered verification.
  - What: Re-run the smallest affected tests, then the affected suites, then the full solution-level `dotnet test --solution ./PrompterOne.slnx -m:1`.
  - Where: Repo root and affected test projects.
  - Verify: All affected commands pass cleanly.

- [ ] Step 6. Push the fix and watch the replacement GitHub Actions run.
  - What: Commit the test-configuration fix, push it to `main` per the user's explicit instruction, and monitor the replacement browser-suite run to completion.
  - Where: Repo root and GitHub Actions.
  - Verify: Replacement run reaches green, or any remaining failure is captured as a new concrete root cause.

- [ ] Step 7. Run final repo validation.
  - What: Run the repo-required build and format commands after tests are green.
  - Where: Repo root.
  - Verify:
    - `dotnet build ./PrompterOne.slnx -warnaserror`
    - `dotnet test --solution ./PrompterOne.slnx -m:1`
    - `dotnet format ./PrompterOne.slnx`
    - `dotnet build ./PrompterOne.slnx -warnaserror`

## Testing Methodology

- Start with the real failing CI run and the real local all-tests entrypoint.
- Reproduce the smallest failing command before changing code.
- Preserve real browser acceptance coverage; do not “fix” by skipping or narrowing suites.
- Use targeted reruns for diagnosis, then end on the full solution-level test command.

## Baseline And Existing Failures

- [x] GitHub Actions baseline captured
  - Symptom: runs `24138107179` and `24139733925` both show all four split browser jobs failing independently in CI while the supporting suites job stays green.
  - Root-cause notes: the current failing SHA `045888f574fdb4c6d9fac9db7e0eaa6bf32135e8` is `Revert "Restore browser test parallel limits"`, which removed the previously restored browser-suite `CiLimit = 2` and `LocalLimit = 15` overrides. The suites now inherit `CiLimit = 10`, which correlates with widespread first-page app-root visibility failures across `Shell`, `Studio`, `Reader`, and `Editor`.
  - Intended fix path: restore the safe browser-suite CI cap while keeping the requested per-project `MaxParallelTestsForPipeline` type.

- [x] Local solution-level test baseline captured
  - Symptom: `dotnet test --solution ./PrompterOne.slnx` starts multiple browser suite DLLs concurrently and also tries to execute `tests/PrompterOne.Testing` plus the shared `tests/PrompterOne.Web.UITests` base project as runnable test apps with zero tests.
  - Root-cause notes: the solution-level entrypoint is currently invalid in two ways:
    1. support/base projects still reference `TUnit`, so they are discovered as executable test apps despite having zero tests;
    2. the solution-level run needs serialized project execution because multiple browser suite DLLs must not self-host concurrently on one local machine.
  - Intended fix path: convert support/base projects to non-engine TUnit package references, then use a serial solution-level test entrypoint.

- [ ] Root-cause remediation checklist
  - [ ] Restore browser-suite `CiLimit = 2` while keeping `LocalLimit = 15` in each runnable UI test project.
  - [ ] Convert `tests/PrompterOne.Testing` and `tests/PrompterOne.Web.UITests` to non-runnable shared test libraries.
  - [ ] Update repo command/documentation surface to `dotnet test --solution ./PrompterOne.slnx -m:1`.
  - [ ] Prove `Shell`, `Studio`, `Reader`, and `Editor` pass again under the fixed configuration.
  - [ ] Prove the full solution-level all-tests entrypoint passes locally.
  - [ ] Prove the replacement GitHub Actions run on the pushed fix is green.

## Final Validation Skills

- [ ] `github:gh-fix-ci`
  - Reason: inspect the failing GitHub Actions run and retrieve the authoritative CI failure signature.

- [ ] `dotnet`
  - Reason: validate the solution-level .NET test/build workflow and keep fixes aligned with the repo’s actual stack.

## Done Criteria

- [ ] The referenced GitHub Actions failure is understood concretely.
- [ ] `dotnet test --solution ./PrompterOne.slnx -m:1` passes locally.
- [ ] The split browser-suite architecture remains intact.
- [ ] The replacement GitHub Actions run is green.
- [ ] Final build/format validation passes.
