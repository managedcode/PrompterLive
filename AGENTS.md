# AGENTS.md

Project: `PrompterOne`
Stack: `.NET 10`, Blazor WebAssembly, Razor Class Library, TUnit, bUnit, Playwright

## Current Shape

`PrompterOne` is a standalone browser-first WebAssembly app.

- `src/PrompterOne.Web` is the only runnable host.
- `src/PrompterOne.Shared` contains routed Razor UI, exact `design` styling, and browser interop.
- `src/PrompterOne.Core` contains TPS, RSVP, preview, workspace, media-scene, and streaming domain logic.
- `tests/` contains all automated test projects.
- The shipped Blazor UI under `src/PrompterOne.Shared` is the visual and interaction source of truth; do not keep or restore a separate repo-local `design/` prototype tree.

There is no backend in the runtime shape. The app must boot directly in the browser from the WebAssembly host.


## Rule Precedence

1. Read the solution-root `AGENTS.md` first.
2. Read the nearest local `AGENTS.md` for the area you will edit.
3. Apply the stricter rule when both files speak to the same topic.
4. Local `AGENTS.md` files may refine or tighten root rules, but they must not silently weaken them.
5. If a local rule needs an exception, document it explicitly in the nearest local `AGENTS.md`, ADR, or feature doc.

## Conversations (Self-Learning)

Learn the user's stable habits, preferences, and corrections. Record durable rules here instead of relying on chat history.

Before doing any non-trivial task, evaluate the latest user message.
If it contains a durable rule, correction, preference, or workflow change, update `AGENTS.md` first.
If it is only task-local scope, do not turn it into a lasting rule.

Update this file when the user gives:

- a repeated correction
- a permanent requirement
- a lasting preference
- a workflow change
- a high-signal frustration that indicates a rule was missed

Extract rules aggressively when the user says things equivalent to:

- "never", "don't", "stop", "avoid"
- "always", "must", "make sure", "should"
- "remember", "keep in mind", "note that"
- "from now on", "going forward"
- "the workflow is", "we do it like this"

Preferences belong in `## Preferences`:

- positive preferences go under `Likes`
- negative preferences go under `Dislikes`
- comparisons should become explicit rules or preferences

Corrections should update an existing rule when possible instead of creating duplicates.

Treat these as strong signals and record them immediately:

- anger, swearing, sarcasm, or explicit frustration
- ALL CAPS, repeated punctuation, or "don't do this again"
- the same mistake happening twice
- the user manually undoing or rejecting a recurring pattern

Do not record:

- one-off instructions for the current task
- temporary exceptions
- requirements that are already captured elsewhere without change

Rule format:

- one instruction per bullet
- place it in the right section
- capture the why, not only the literal wording
- remove obsolete rules when a better one replaces them

- Use `PrompterOne` as the canonical product, solution, namespace, and folder name across code, docs, tests, and build paths; do not reintroduce legacy product-name variants after the rename.
- Localized UI copy must keep the product brand as `PrompterOne` in every language; do not translate, transliterate, or inflect the app name into locale-specific variants such as `СуфлерOne`.
- Repo-owned docs, README, ADRs, and AGENTS files must not contain local usernames, home-directory paths, or personal machine-specific references; use repo-relative paths or neutral wording instead.
- Public-facing screenshots and any screenshot-generating or screenshot-asserting tests must use English-visible content so README, docs, and release assets stay globally readable and consistent.
- Public-facing screenshots that include camera or preview feeds must not ship mirrored or reversed readable text; choose or configure the capture so visible text reads correctly in the final asset.
- Teleprompter reader text alignment must expose explicit left, center, and right modes, default to left alignment, and keep the left-aligned mode optically centered by offsetting the text mass away from a visibly left-heavy block.
- Teleprompter `Read Width` must map honestly to the visible reading lane: at `100%` it must not keep extra internal container padding or shrink-to-content gutters that make the text block visibly narrower than the width guides.
- The TPS editor migration to Monaco must be complete: syntax coloring, IntelliSense/autocomplete, hover or inline tooltip help, decorations, and TPS authoring feedback must be Monaco-native instead of split across legacy overlay or hidden-textarea behavior.
- TPS authoring completeness must be checked against the upstream `managedcode/TPS` README, not only the currently shipped editor menus, so new editor support stays aligned with the full spec for emotions, delivery, pauses, speed, pronunciation, and related cues.
- Editor TPS command surfaces must expose the full currently supported TPS authoring set consistently across top toolbar menus, floating-toolbar menus, and Monaco assistance; do not ship partial or differently grouped command taxonomies between those surfaces.
- User-facing file transfer actions in the shell should use `Import` and `Export` wording instead of `Open Script` and `Save File`, because the app also has its own internal script/workspace structure.
- File workflows must stay local-first inside PrompterOne: scripts need in-app autosave and an internal change-history path in the browser environment, not only external disk import/export actions.
- Hotkey work must target PrompterOne’s own browser surfaces and settings inventory only; do not design around OBS commands or claim OBS integration paths that the product does not have.
- When the vendored TPS SDK already owns parsing or compile semantics, prefer removing redundant local TPS parser wrappers and keep only thin PrompterOne adapters that translate SDK models into app-owned contracts.
- After syncing the vendored TPS SDK, delete repo-local TPS catalogs, constants, wrappers, or helper code that only duplicate the SDK contract; do not keep parallel spec copies in `Core` once consumers can read the vendored SDK directly.
- Production TPS behavior must have one authoritative implementation path. Do not keep a second repo-local TPS parser, compiler, or regex-based semantic fallback beside the vendored SDK; plain-text fallbacks may exist only for truly non-TPS input.
- Product localization must be complete across all supported UI languages: audit hardcoded user-facing strings with an explicit inventory file, move them into shared localization catalogs, and include tooltip text in the same localization pass instead of leaving tooltip copy or chrome labels hardcoded.
- PrompterOne must ship a first-run onboarding flow that explains the product basics, TPS, RSVP, Editor, Learn, Teleprompter, and Go Live in-app; the walkthrough must appear on first launch until the user explicitly completes or dismisses it, persist that choice locally, and be fully localized across all supported UI languages.
- The onboarding flow must include a dedicated TPS explainer step or page, separate from the generic editor step, that tells users what TPS is, why it exists, and how PrompterOne uses it.
- The dedicated TPS onboarding step must explain TPS in concrete beginner terms: what the format is, why it exists, how PrompterOne uses it across Editor/Learn/Teleprompter/Go Live, and where users can continue with the official TPS site or glossary.
- Script discovery and authoring surfaces must support real search by script name and script content; Library/script pages and editor flows must not force manual browsing when the user needs to find files or text inside files.
- Editor find/search interactions must keep keyboard focus inside the find UI while the user types; query input should only update highlighted matches, and only explicit `Next`/`Previous` search navigation may move focus/selection into the Monaco editor surface.
- Editor status chrome must stay compact and omit the footer version pill unless a task explicitly asks to surface version metadata there.
- Editor footer status metrics must keep a stable one-row layout while cursor or count values change; line, column, word, and duration updates must not make neighboring status chips jump or reflow.
- Editor footer status chrome should read like a quiet IDE status strip: one compact row, low emphasis, and no oversized chip or outlined-block treatment for static metrics.
- Editor top-toolbar search must remain visible in narrow layouts; when width runs out, the other toolbar groups should become explicitly horizontally scrollable/reachable before search is allowed to disappear.
- Do not push branches or `main` without an explicit user command for that push; local commits are allowed, but network publish actions require clear approval in the current conversation.
- When a CI test job fails, times out, or is cancelled, the workflow summary must still state which tests failed or that the run ended before per-test failure data was available; do not leave browser-suite failures represented only by generic job annotations.
- Do not add or raise CI timeout settings as a fix for browser-suite instability or slow runs; fix the underlying failure path cleanly instead of masking it with workflow or step timeouts.
- For TUnit reporting in GitHub Actions, prefer TUnit's built-in HTML report and the documented `actions/github-script` runtime exposure step over repo-local custom summary scripts or ad-hoc log parsers.
- Public web hosting is split by role: the standalone PrompterOne app in this repo must publish on `app.prompter.one`, while the marketing landing site for `prompter.one` lives in the separate `PrompterOne-LandingPage` repository.
- Runtime telemetry providers such as Google Analytics, Clarity, and Sentry must not be described as connected or working unless the real production path is verified with actual outbound delivery or loaded vendor SDKs; local harness snapshots, init flags, or stubbed globals are not sufficient proof.
- Runtime telemetry readiness must be proven against Release-built app artifacts in CI; do not sign off GA, Clarity, or Sentry from Debug-only local runs when the shipped Release pipeline has not validated that path.
- For deploy-only, domain, CI, or static-site hosting tasks, do not spend time on unrelated app/browser test suites unless the user explicitly asks or the runtime behavior itself changes; prefer workflow, build, and publish-config validation only.
- Repo-wide .NET SDK and test-runner selection belong in the root `global.json`; do not split `global.json` test-runner opt-ins per project or subfolder once the user asks for a global test-platform policy.
- Browser and component tests must use one selector format only: `data-test`; do not mix in any alternate test-attribute naming variants.
- Shared test-support libraries that contain no runnable test cases must not reference the TUnit engine package directly; keep them on non-engine TUnit packages so solution-level `dotnet test` does not discover zero-test support DLLs as runnable test apps.
- Every runnable test project must declare `MaxParallelTestsForPipeline : EnvironmentAwareParallelLimitBase` with `LocalLimit = 15`; do not keep lower per-project local parallel caps unless the user explicitly asks for an exception.
- Browser-suite CI parallelism is user-tunable. When suite duration becomes a bottleneck and the user asks for higher throughput, prefer splitting work into `4` or `8` parallel GitHub Actions test jobs before reaching for timeout increases; only keep lower `CiLimit` caps when a specific flake requires them.
- Local regression verification must include solution-level `dotnet test --solution ./PrompterOne.slnx --max-parallel-test-modules 1` so test-project split changes are proven under the real all-tests entrypoint, not only as isolated per-project runs.
- When the user explicitly asks to validate a test fix in actual GitHub Actions, do not spend more time on local `CI=true` emulation; push the fix and monitor the real CI run instead.
- Selector-contract remediation requests must be handled repo-wide across all relevant test files (`Web.Tests` and `Web.UITests`), not as partial per-file cleanups.
- Repo-wide quality audits and agent-generated review handoff artifacts must be written as root-level task files so other coding agents can pick them up quickly; do not bury those temporary audit results under `docs/` unless the task is explicitly about durable product documentation.
- Repo-wide cleanup and review passes must explicitly inventory forbidden implementation string literals, `MarkupString` or raw-HTML UI composition, duplicated JS/CSS patterns, architecture-boundary drift, and `foreach`-driven test scenarios that should become isolated TUnit cases.
- Repo-wide audits should use multiple independent reviewers with distinct focuses when the tooling is available, including external CLI reviewers such as Claude and Copilot plus internal agents, and all review outputs should be captured in root-level task files before remediation starts.
- Legacy, dead, duplicate, or speculative code paths should be deleted aggressively instead of being preserved behind compatibility instincts; if code has no clear runtime owner or authoritative contract, remove it rather than keep it as “just in case” ballast.
- For repo-wide remediation passes, keep an explicit root-level accounting of fixed versus remaining feedback items, finish the code fixes first, and only then run and stabilize the test suites; do not bounce back into verification mid-remediation unless the user explicitly asks.
- For task-scoped work, edit, stage, and commit only the files directly required for the requested change; do not widen the change set into unrelated user-owned or parallel worktree edits, do not touch changes owned by another agent, and if a blocker comes from that parallel work, wait briefly and re-check instead of patching around their in-flight fix.

## Rules to Follow (Mandatory)

### Commands

- `build`: `dotnet build ./PrompterOne.slnx -warnaserror`
- `test`: `dotnet test @./tests/dotnet-test-progress.rsp --solution ./PrompterOne.slnx --max-parallel-test-modules 1`
- `format`: `dotnet format ./PrompterOne.slnx`
- `coverage`: `dotnet test @./tests/dotnet-test-progress.rsp --project ./tests/PrompterOne.Core.Tests/PrompterOne.Core.Tests.csproj --coverage --coverage-output-format cobertura && dotnet test @./tests/dotnet-test-progress.rsp --project ./tests/PrompterOne.Web.Tests/PrompterOne.Web.Tests.csproj --coverage --coverage-output-format cobertura && dotnet test @./tests/dotnet-test-progress.rsp --project ./tests/PrompterOne.Web.UITests.Shell/PrompterOne.Web.UITests.Shell.csproj --coverage --coverage-output-format cobertura && dotnet test @./tests/dotnet-test-progress.rsp --project ./tests/PrompterOne.Web.UITests.Studio/PrompterOne.Web.UITests.Studio.csproj --coverage --coverage-output-format cobertura && dotnet test @./tests/dotnet-test-progress.rsp --project ./tests/PrompterOne.Web.UITests.Editor/PrompterOne.Web.UITests.Editor.csproj --coverage --coverage-output-format cobertura && dotnet test @./tests/dotnet-test-progress.rsp --project ./tests/PrompterOne.Web.UITests.Reader/PrompterOne.Web.UITests.Reader.csproj --coverage --coverage-output-format cobertura`

For this `.NET` repo:

- all automated test projects run on `TUnit` and native `Microsoft.Testing.Platform`
- use `@./tests/dotnet-test-progress.rsp` on repo test commands so `dotnet test` emits detailed, non-ANSI per-test progress logs consistently in terminal and CI text logs
- `format` is direct `dotnet format`, not `--verify-no-changes` and not a wrapper
- coverage uses TUnit's native `--coverage` support
- `LangVersion` is not pinned; use the SDK default unless the repo intentionally changes it later
- `--no-build` is forbidden in local commands, docs, and CI; always let `dotnet test` build the active inputs so stale WASM or stale test binaries cannot hide regressions

Useful focused commands:

- app run: `cd ./src/PrompterOne.Web && dotnet run`
- core tests: `dotnet test @./tests/dotnet-test-progress.rsp --project ./tests/PrompterOne.Core.Tests/PrompterOne.Core.Tests.csproj`
- component tests: `dotnet test @./tests/dotnet-test-progress.rsp --project ./tests/PrompterOne.Web.Tests/PrompterOne.Web.Tests.csproj`
- all tests: `dotnet test @./tests/dotnet-test-progress.rsp --solution ./PrompterOne.slnx --max-parallel-test-modules 1`
- ui tests: `dotnet test @./tests/dotnet-test-progress.rsp --project ./tests/PrompterOne.Web.UITests.Shell/PrompterOne.Web.UITests.Shell.csproj && dotnet test @./tests/dotnet-test-progress.rsp --project ./tests/PrompterOne.Web.UITests.Studio/PrompterOne.Web.UITests.Studio.csproj && dotnet test @./tests/dotnet-test-progress.rsp --project ./tests/PrompterOne.Web.UITests.Editor/PrompterOne.Web.UITests.Editor.csproj && dotnet test @./tests/dotnet-test-progress.rsp --project ./tests/PrompterOne.Web.UITests.Reader/PrompterOne.Web.UITests.Reader.csproj`
- ui shell tests: `dotnet test @./tests/dotnet-test-progress.rsp --project ./tests/PrompterOne.Web.UITests.Shell/PrompterOne.Web.UITests.Shell.csproj`
- ui studio tests: `dotnet test @./tests/dotnet-test-progress.rsp --project ./tests/PrompterOne.Web.UITests.Studio/PrompterOne.Web.UITests.Studio.csproj`
- ui editor tests: `dotnet test @./tests/dotnet-test-progress.rsp --project ./tests/PrompterOne.Web.UITests.Editor/PrompterOne.Web.UITests.Editor.csproj`
- ui reader tests: `dotnet test @./tests/dotnet-test-progress.rsp --project ./tests/PrompterOne.Web.UITests.Reader/PrompterOne.Web.UITests.Reader.csproj`
- playwright browser install: `node ./tests/PrompterOne.Web.UITests/bin/Debug/net10.0/.playwright/package/cli.js install chromium`
- Build the relevant project immediately before starting a local dev server so `dotnet run` cannot serve stale WASM assets or binaries.

Browser test execution rules:

- Use one `dotnet test` process at a time for a browser suite project when running locally.
- The browser suite family self-hosts the built WASM assets on a dynamically assigned loopback HTTP origin.
- Each browser-suite host startup MUST request a fresh OS-assigned loopback port via `http://127.0.0.1:0`. Never pin or reuse a fixed browser-test port across runs.
- Inside a single browser-suite process, the suite may run up to `15` parallel TUnit workers locally and up to `2` in CI; do not raise the CI cap as part of split-suite plumbing unless the user explicitly asks for that experiment.
- Do not run any `PrompterOne.Web.UITests*` project in parallel with another `dotnet build` or `dotnet test` command on the same local machine context.
- In GitHub Actions, run the browser suite family in dedicated macOS jobs or matrix entries and keep supporting suites in separate jobs so CI can parallelize work without Linux x64 browser-runner contention stretching release validation.
- GitHub Actions pipelines must expose explicit staged jobs with readable names such as restore, build, supporting tests, browser tests, release publish, and deploy; vague single-job `validate` graphs are not acceptable when the user needs to see pipeline phases clearly in the Actions UI.
- When monitoring long-running GitHub Actions jobs from the terminal, poll with coarse waits of roughly `3-5` minutes between checks; frequent short-interval polling is noise and does not help on multi-minute browser suites.
- Browser acceptance tests must stay on the production-shaped runtime path; do not add or keep `?wasm-debug=1` or similar debug-query scenarios in automated acceptance coverage unless the user explicitly asks for that path.
- Do not add Python or ad-hoc runner scripts to bootstrap browser verification. The repo test commands must self-host the app and execute the flows end to end on their own.
- Browser UI scenarios are the primary acceptance gate for this repo. Component and core tests are supporting layers, not the release bar.
- Major user flows MUST be covered by long Playwright scenarios that execute real browser interactions end to end.
- Major browser scenarios MUST capture screenshot artifacts under `output/playwright/`.
- For new visual elements, visual regressions, or editor chrome/layout work, inspect the real browser surface and capture screenshots; bUnit may support structural contracts but is not sufficient as the primary signal for visual correctness.
- Responsive layout work is not done until Playwright verifies every routed screen across a phone-and-tablet viewport matrix that includes small, medium, and large handset sizes plus small, medium, and large tablet sizes in both portrait and landscape, with assertions that primary page controls stay visible inside the viewport without clipping.
- Editor typing and latency fixes are not done until they are reproduced and cleared on the live dev-host editor with real keyboard input, not only synthetic input helpers or the static UI-test host.
- When the user reports an editor regression on a specific script or exact `/editor?id=...` URL, reproduce on that same live script before treating browser-suite results as sufficient.
- Editor surface changes must ship with real-browser checks for scroll behavior, floating toolbar dropdowns, and TPS section controls when those areas are touched; static component tests alone are not enough.

Do not hijack shared user dev ports for agent-run preview servers. The user's stable local app ports, including `5041`, stay user-owned. When the agent needs an isolated local preview or manual-check server, run it only on ports in the `5050-5070` range. Keep the default stable launch-settings origin for the user's own dev workflow, and do not reclaim that origin for agent work when it is already in use. The browser-test harness is the exception: it must resolve a fresh dynamic loopback port and propagate the actual origin into Playwright `BaseURL` and permission grants.

Selector and constant rules:

- UI contracts MUST expose stable dedicated test hooks for any flow covered by automated tests, and the only allowed hook format is `data-test`.
- Browser and component tests MUST prefer dedicated test attributes over text, role-name, CSS-class, or DOM-shape selectors.
- If a stable dedicated test hook exists, raw `GetByText`, `GetByRole(... Name = ...)`, `.Locator(".class")`, and literal attribute selectors are forbidden.
- Browser and component tests MUST NOT use style-driven selectors at all (for example `.Locator(".class")`, descendant class chains, or CSS-state probes) when a dedicated `data-test` hook can represent that state.
- Browser-test JavaScript snippets MUST read elements through dedicated `data-test` contracts passed from C# constants; do not use raw `document.querySelector(...)` selectors that target classes, styles, or DOM shape.
- Routes, route patterns, test ids, DOM ids, storage keys, keyboard shortcuts, seeded values, wait durations, and other repeated test inputs MUST come from named constants.
- URLs in tests MUST come from shared route helpers or constants, never inline literals.
- Magic numbers in tests are forbidden. Put timeouts, delays, counts, percentages, and seeded numeric inputs behind named constants.
- Prefer production-owned UI contract constants in `PrompterOne.Shared.Contracts` over duplicating selector strings in test projects.
- Browser-localization storage keys, JS interop identifiers, and culture names MUST come from named constants.

### Project AGENTS Policy

- Multi-project solutions MUST keep one root `AGENTS.md` plus one local `AGENTS.md` in each project or module root.
- Each local `AGENTS.md` MUST document:
  - project purpose
  - entry points
  - boundaries
  - project-local commands
  - applicable skills
  - local risks or protected areas
- If a project grows enough that the root file becomes vague, add or tighten the local `AGENTS.md` before continuing implementation.

Current local `AGENTS.md` files:

- [src/PrompterOne.Web/AGENTS.md](./src/PrompterOne.Web/AGENTS.md)
- [src/PrompterOne.Core/AGENTS.md](./src/PrompterOne.Core/AGENTS.md)
- [src/PrompterOne.Shared/AGENTS.md](./src/PrompterOne.Shared/AGENTS.md)
- [tests/PrompterOne.Core.Tests/AGENTS.md](./tests/PrompterOne.Core.Tests/AGENTS.md)
- [tests/PrompterOne.Testing/AGENTS.md](./tests/PrompterOne.Testing/AGENTS.md)
- [tests/PrompterOne.Web.Tests/AGENTS.md](./tests/PrompterOne.Web.Tests/AGENTS.md)
- [tests/PrompterOne.Web.UITests/AGENTS.md](./tests/PrompterOne.Web.UITests/AGENTS.md)
- [tests/PrompterOne.Web.UITests.Shell/AGENTS.md](./tests/PrompterOne.Web.UITests.Shell/AGENTS.md)
- [tests/PrompterOne.Web.UITests.Studio/AGENTS.md](./tests/PrompterOne.Web.UITests.Studio/AGENTS.md)
- [tests/PrompterOne.Web.UITests.Editor/AGENTS.md](./tests/PrompterOne.Web.UITests.Editor/AGENTS.md)
- [tests/PrompterOne.Web.UITests.Reader/AGENTS.md](./tests/PrompterOne.Web.UITests.Reader/AGENTS.md)

### Maintainability Limits

- `file_max_loc`: `400`
- `type_max_loc`: `200`
- `function_max_loc`: `50`
- `max_nesting_depth`: `3`
- `exception_policy`: `Document any justified exception in the nearest ADR, feature doc, or local AGENTS.md with the reason, scope, and removal/refactor plan.`

Local `AGENTS.md` files may tighten these values, but they must not loosen them without an explicit root-level exception.

### Task Delivery

- Start from [docs/Architecture.md](./docs/Architecture.md) and the nearest local `AGENTS.md`.
- Treat [docs/Architecture.md](./docs/Architecture.md) as the architecture map for every non-trivial task.
- Read [docs/Architecture.md](./docs/Architecture.md) before implementation to identify the owning component, the allowed boundary, where code should be added, and where related code should be searched first.
- If the overview is missing, stale, or diagram-free, update it before implementation.
- Define scope before coding:
  - in scope
  - out of scope
- Keep context tight. Do not read the whole repo if the architecture map and local docs are enough.
- If the task matches a skill, use the skill instead of improvising.
- Analyze first:
  - current state
  - required change
  - constraints and risks
- For repo-wide review or audit tasks, produce the initial code-review findings and root-level audit artifacts before running broad automated test baselines; use targeted verification after concrete fixes unless the user explicitly asks for the broader baseline upfront.
- For UI and design-driven work, execute in this order unless an explicit exception is documented:
  - inspect the design reference and current implementation first
  - map the design into the target Blazor structure and wire the intended UI before spending time on test execution
  - run tests only after the design hookup and implementation path are in place
- For non-trivial work, create a root-level `<slug>.plan.md` file before making code or doc changes.
- Keep the `<slug>.plan.md` file as the working plan for the task until completion.
- The plan file MUST contain:
  - task goal and scope
  - a detailed implementation plan with detailed ordered steps
  - very detailed step-by-step actions; each step must say exactly what will be done, where it will be done, and how that step will be verified before moving on
  - constraints and risks
  - explicit test steps as part of the ordered plan, not as a later add-on
  - the test and verification strategy for each planned step
  - the testing methodology for the task: what flows will be tested, how they will be tested, and what quality bar the tests must meet
  - an explicit full-test baseline step after the plan is prepared
  - a tracked list of already failing tests, with one checklist item per failing test
  - root-cause notes and intended fix path for each failing test that must be addressed
  - a checklist with explicit done criteria for each step
  - ordered final validation skills and commands, with reason for each
- Use the plan loop for every non-trivial task:
  - define the intended direction before implementation
  - turn that direction into a detailed `<slug>.plan.md`
  - break the work into small concrete sequential steps; vague plan items such as "implement feature", "fix tests", or "do verification" are forbidden unless they are expanded into exact sub-steps
  - include test creation, test updates, and verification work in the ordered steps from the start
  - once the initial plan is ready, run the full relevant test suite to establish the real baseline
  - if tests are already failing, add each failing test back into `<slug>.plan.md` as a tracked item with its failure symptom, suspected cause, and fix status
  - work through failing tests one by one: reproduce, find the root cause, apply the fix, rerun, and update the plan file
  - include ordered final validation skills in the plan file, with reason for each skill
  - require each selected skill to produce a concrete action, artifact, or verification outcome
  - execute one planned step at a time
  - mark checklist items in `<slug>.plan.md` as work progresses
  - review findings, apply fixes, and rerun relevant verification
  - update the plan file and repeat until done criteria are met or an explicit exception is documented
- Implement code and tests together.
- Run verification in layers:
  - changed tests
  - related suite
  - broader required regressions
- If `build` is separate from `test`, run `build` before `test`.
- After tests pass, run `format`, then the final required verification commands.
- The task is complete only when every planned checklist item is done and all relevant tests are green.
- Summarize the change, risks, and verification before marking the task complete.
- When the user asks to fix GitHub Actions or CI, do not stop at locally green commands: after pushing, watch the relevant GitHub Actions run and continue iterating until the replacement run is green or an explicit external blocker is documented.
- When the browser suite has already flaked across repeated CI runs, do not keep cycling one-off threshold bumps; prove stability with repeated local browser-suite runs and remove or harden brittle root-cause assertions before calling the fix durable.
- When a concrete failing test is already identified, fix and rerun that failing test and its immediate interference set first; do not sit on a full-suite wait before addressing the known red signal.

### Documentation

- All durable docs live in `docs/`.
- [docs/Architecture.md](./docs/Architecture.md) is the required global map and the first stop for agents.
- [docs/Architecture.md](./docs/Architecture.md) MUST describe all major components and feature slices with what they are, why they exist, where they live, what they own, and what they must not own.
- [docs/Architecture.md](./docs/Architecture.md) MUST document the app structure, design principles, and code-placement principles clearly enough that contributors can use it to decide where new code belongs and where existing behavior should be found.
- [docs/Architecture.md](./docs/Architecture.md) MUST contain Mermaid diagrams for:
  - system or module boundaries
  - interfaces or contracts between boundaries
  - key classes or types for the changed area
- Keep one canonical source for each important fact. Link instead of duplicating.
- Public bootstrap templates are limited to root-level agent files. Authoring scaffolds for architecture, features, ADRs, and other workflows live in skills.
- Update feature docs when behaviour changes.
- Update ADRs when architecture, boundaries, or standards change.
- For non-trivial work, the plan file, feature doc, or ADR MUST document the testing methodology:
  - what flows are covered
  - how they are tested
  - which commands prove them
  - what quality and coverage requirements must hold
- Every feature doc under `docs/Features/` MUST contain at least one Mermaid diagram for the main behaviour or flow.
- Every ADR under `docs/ADR/` MUST contain at least one Mermaid diagram for the decision, boundaries, or interactions.
- Mermaid diagrams are mandatory in architecture docs, feature docs, and ADRs.
- Mermaid diagrams must render. Simplify them until they do.

### Testing

- TDD is the default for new behaviour and bug fixes: write the failing test first, make it pass, then refactor.
- Bug fixes start with a failing regression test that reproduces the issue.
- Every behaviour change needs new or updated automated tests with meaningful assertions. New tests are mandatory for new behaviour and bug fixes.
- Tests must prove the real user flow or caller-visible system flow, not only internal implementation details.
- Tests should be as realistic as possible and exercise the system through real flows, contracts, and dependencies.
- Tests must cover positive flows, negative flows, edge cases, and unexpected paths from multiple relevant angles when the behaviour can fail in different ways.
- Prefer integration/API/UI tests over isolated unit tests when behaviour crosses boundaries.
- For `PrompterOne`, prioritize browser UI tests first, then supporting component/core tests only where they help isolate failures.
- Do not use mocks, fakes, stubs, or service doubles in verification.
- Exercise internal and external dependencies through real containers, test instances, or sandbox environments that match the real contract.
- Flaky tests are failures. Fix the cause.
- Do not hide multiple verification scenarios inside one test with a `foreach`; split them into separate TUnit data-driven test cases so failures stay isolated and the runner can schedule the cases independently.
- Supporting TUnit suites should use environment-aware parallel limits: cap CI worker counts lower than local runs, and keep timer-, storage-, or culture-mutation-heavy classes isolated when they prove flaky under suite-wide parallelism.
- Changed production code MUST reach at least 80% line coverage, and at least 70% branch coverage where branch coverage is available.
- Critical flows and public contracts MUST reach at least 90% line coverage with explicit success and failure assertions.
- Repository or module coverage must not decrease without an explicit written exception. Coverage after the change must stay at least at the previous baseline or improve.
- Coverage is for finding gaps, not gaming a number. Coverage numbers do not replace scenario coverage or user-flow verification.
- The task is not done until the full relevant test suite is green, not only the newly added tests.
- For this `.NET` repo, do not mix VSTest and Microsoft.Testing.Platform assumptions. The active model is native `Microsoft.Testing.Platform` through `TUnit`.
- After changing production code, run the repo-defined quality pass: format, build, focused tests when useful, broader tests, coverage, and any configured extra gates.

### Code and Design

- Everything in this solution MUST follow SOLID principles by default.
- Every class, object, module, and service MUST have a clear single responsibility and explicit boundaries.
- SOLID is mandatory.
- SRP and strong cohesion are mandatory for files, types, and functions.
- Prefer composition over inheritance unless inheritance is explicitly justified.
- Large files, types, functions, and deep nesting are design smells. Split them or document a justified exception under `exception_policy`.
- Hardcoded values are forbidden.
- String literals are forbidden in implementation code. Declare them once as named constants, enums, configuration entries, or dedicated value objects, then reuse those symbols.
- Avoid magic literals. Extract shared values into constants, enums, configuration, or dedicated types.
- URLs, storage keys, JS interop identifiers, route fragments, and user-visible fallback strings are implementation literals too. They MUST live behind named constants or localized catalogs.
- JS bridges must not invent repo-owned CSS custom-property names, DOM selectors, or feature data-attribute names inside `.js` files when Blazor/C# can own and pass those contract strings explicitly.
- Fallback device names, fallback device labels, and other invented media-device placeholders are forbidden in runtime and tests; use real device metadata when it exists, otherwise keep the field empty or assert on explicit no-device state instead of fabricating names.
- Design boundaries so real behaviour can be tested through public interfaces.
- The repo-root `.editorconfig` is the source of truth for formatting, naming, style, and analyzer severity. Use nested `.editorconfig` files only when they clearly serve a subtree-specific purpose.
- Repo-wide cleanup must preserve vertical-slice ownership: fix each issue in the owning feature slice, avoid new cross-cutting dumping grounds, and prefer slice-local reusable abstractions over shared misc helpers.

Repo-specific design rules:

- Keep the solution in `PrompterOne.slnx`.
- Keep all production projects under `src/`.
- Keep all test projects under `tests/`.
- Keep shared build settings in `Directory.Build.props`.
- Keep shared package versions in `Directory.Packages.props`.
- Keep the pinned SDK version in `global.json`.
- Do not add, restore, or depend on a repo-local `design/` prototype folder; the routed Blazor implementation and its supporting docs are the only canonical UI source.
- Production UI must stay implemented as Blazor components in `src/PrompterOne.Shared`; do not introduce parallel static HTML prototype screens.
- Do not re-invent the UI when the answer should be “fix the shipped routed screen directly”.
- For parity tasks, fix the full routed screen in the shipped Blazor UI, not just isolated high-signal blocks. Settings, Editor, Learn, Teleprompter, and Go Live must stay coherent in layout and intended interaction while remaining Blazor/C# owned.
- When the user reports a visual or theme regression on a routed screen, verify the adjacent chrome and primary controls on that same screen in the affected theme instead of fixing only the single highlighted widget; add or update a browser regression that covers the broader screen-level parity check.
- When routed UI or visual design changes materially, refresh the README screenshots and feature/status copy before release so public docs match the shipped product.
- Release-ready work is not done until the requested branch is pushed and the corresponding GitHub CI run finishes green; if the user asks to land in `main`, use `main` and wait for the full resulting `Release Pipeline` to finish green, including release creation and deploy steps, instead of stopping at local verification or the first green build job.
- About content must stay factual and current: do not invent team members or contributor names; use Managed Code attribution and official company links only.
- Do not introduce a server host for the app runtime.
- Preserve stable dedicated test selectors on core flows because the Playwright suite depends on them.
- Keep UI routes in shared route constants and keep dedicated test-hook names in shared UI contract constants.
- Keep UI flow logic, keyboard shortcuts, DOM ids/selectors, and reusable UI constants in C#/Blazor contracts whenever the platform allows it; use JS only for unavoidable browser API interop or DOM access that Blazor cannot own directly.
- Prefer deleting JS files entirely when they only hold product UI behavior or duplicated constants; JS modules may exist only as thin bridges to browser APIs or external JS SDKs, with the owning workflow and state kept in C#/Blazor.
- TPS front matter pasted or imported into the editor source MUST be parsed into the metadata rail automatically and removed from the visible body text instead of staying inline in the source editor.
- Script authoring flows MUST support explicit user-driven save/export to the real local disk from the browser; browser or app-local persistence alone is not a sufficient save path.
- Editor authoring MUST accept direct local-file drag-and-drop on the editor surface; dropping onto an empty draft replaces it with the imported document, while dropping onto a non-empty draft appends the imported TPS text at the end without breaking undo/redo.
- TPS support MUST fully implement the current `docs/Reference/TPS.md` contract end to end; legacy or partially compatible TPS syntax is not a supported mode, and any old incompatible behavior should be removed instead of kept behind compatibility shims.
- TPS visual semantics MUST track the current TPS spec end to end: editor and reader surfaces should communicate delivery cues such as volume, emphasis, stress, speed, and delivery mode through typography, spacing, weight, and motion where appropriate, not through color alone.
- Dropdown menus across the routed UI must left-align their item content as one readable cluster; right-pinned meta columns or centered item compositions inside dropdown rows are forbidden.
- Pasted or imported TPS documents MUST render their editor-side authoring styles immediately on first load in the editor; showing the imported script as near-plain text until later interaction is a regression.
- For standalone cloud-storage integrations, persist provider keys, tokens, and connection metadata in browser `localStorage`; do not introduce server-side secret storage for runtime auth in this app shape.
- Third-party runtime JavaScript SDKs MUST be sourced only from explicitly pinned GitHub Release tags and assets, copied into the repo, bundled locally with their runtime dependencies, and never loaded from CDNs, package registries, `latest` endpoints, or ad-hoc remote downloads at app runtime.
- Repo-owned manifests, scripts, workflows, and project files that track third-party runtime JavaScript SDKs MUST point to concrete GitHub release versions and asset URLs, never floating references.
- Any vendored runtime JavaScript SDK that tracks an upstream GitHub repo MUST have an automated watcher job that checks new GitHub releases and opens a repo issue describing the required update when a newer release appears.
- Runtime analytics and session-replay scripts must be owned through a product adapter/service layer, not ad-hoc inline screen code, and development or debug runs must not send telemetry to Google Analytics or Microsoft Clarity.
- Teleprompter TPS speed modifiers MUST affect both playback timing and subtle word- or phrase-level letter spacing, so slower spans open up slightly and faster spans tighten slightly without hurting readability.
- Teleprompter default reader width MUST start at the maximum readable width from the design unless the user explicitly narrows it; shipping a visibly narrower default is a regression.
- Teleprompter desktop reading surfaces may use a wider text zone than the current narrow column when readability still holds; overly cramped line width on large screens is a regression.
- Teleprompter speed styling MUST produce a visible but tasteful letter-spacing or kerning change: slower text opens up slightly and faster text tightens slightly, not a no-op.
- Teleprompter reader word styling MUST mirror TPS/editor inline semantics: explicit inline TPS tags control per-word emphasis and color, while section or block emotion sets card context and must not recolor every reader word.
- Learn, RSVP, and Teleprompter reading surfaces MUST render spoken words only; raw TPS control tags, front matter fragments, and metadata tokens must never leak into the visible reading text.
- Teleprompter underline or highlight treatments that span a phrase or block MUST render as one continuous block-level treatment; separate per-word underlines inside the same phrase are forbidden.
- Teleprompter read-state styling MUST mute phrase-level underline or highlight accents once the emphasized text has been read; bright lingering underline accents on already-read text are forbidden.
- Teleprompter reader text MUST appear on the focal guide immediately when a word or block becomes active; visible post-appearance drift or settling onto the guide is forbidden.
- Teleprompter route styles MUST be present on the first paint; a flash of unstyled or late-styled reader UI during route entry is a regression.
- Teleprompter block transitions MUST stay visually consistent and direction-aware: moving forward keeps the straight reference motion with outgoing cards moving upward and incoming cards rising from below, while stepping backward must visibly reverse that path so the returning previous block comes in from above; alternating, diagonal, bouncing, or intermediate-card motion is forbidden.
- Teleprompter focus treatment MUST stay visually calm: the active focus word may be emphasized, but surrounding text should be gently dimmed instead of creating a bright moving blot, fake box, or attention-grabbing patch that flies up and down.
- Teleprompter emotion styling may tint the surface or accents, but reader text itself MUST stay easy to read and must not become harsh, over-bright, or saturated enough to hurt readability.
- Teleprompter progress and control chrome MUST stay visually subdued during reading, especially on strong emotion-tinted surfaces; bright gold fills, shells, or buttons that pull attention away from the script are regressions.
- Teleprompter playback-active chrome in the top corners, including `Go Live` and exit/back buttons, MUST dim once reading starts; bright header actions that compete with the focal line are regressions.
- Teleprompter back navigation MUST stay as visible and readable as the rest of the page controls; a dim or low-contrast back button on the reader screen is a regression.
- Teleprompter MUST expose both horizontal and vertical mirror toggles on the reader screen so tablet or reflected-glass setups can flip the output without leaving the route or editing CSS manually.
- Teleprompter MUST expose an in-reader orientation toggle, matching the phone control pattern, so operators can switch the text flow direction directly on the reader screen without leaving playback.
- Teleprompter reader background video MUST stay transform-synced with the reader surface: horizontal mirror, vertical mirror, and portrait rotation changes applied to the text lane must apply to the camera/video background as well so the composition stays coherent.
- Teleprompter MUST expose a direct in-reader text-size control so operators can enlarge or reduce the live reading text on the teleprompter surface without leaving playback or relying on settings-only defaults.
- Teleprompter desktop chrome MUST expose a real browser fullscreen toggle when the browser supports it; simulating fullscreen with layout-only expansion is not enough.
- Teleprompter desktop progress MUST show segmented read progress by block, similar to Learn progress semantics, so operators can see overall completion and block boundaries at a glance.
- Learn and Teleprompter playback timing MUST align with real word-by-word progression in the browser: WPM, speed modifiers, and word counting must match the emitted words, and timing work is not done until a browser-level word-sequence check proves it.
- Reader and Learn tokenization MUST treat punctuation-only tokens such as commas, periods, and dashes as punctuation attached to nearby words or pauses, never as standalone counted words.
- Teleprompter reader-width controls and persistence MUST be adaptive to the current viewport or screen size, using relative sizing instead of fixed pixel-only maxima so the readable zone scales correctly across displays.
- App-shell logo navigation MUST always lead to the main home/library screen; it must not deep-link into Go Live, Teleprompter, or another feature-specific route.
- Learn rehearsal speed MUST default to about 250 WPM and stay user-adjustable upward from that baseline; shipping a 300 WPM startup default is too aggressive.
- First-run onboarding MUST be dismissible, reopenable by the user, and after either completion or dismissal it must return the user to the main Library screen instead of leaving them on a feature route.
- Go Live `ON AIR` badges and preview live dots MUST appear only while recording or streaming is actually active; idle selected or armed sources must stay visually non-live.
- The global `Go Live` entry in shell/header chrome MUST be one reusable Blazor component with one consistent layout, spacing, and icon treatment across all routed screens; do not let pages restyle or fork their own variants.
- The global `Go Live` entry MUST stay neutral when idle: red icon accents or live-danger treatment are forbidden until recording or streaming is actually active.
- Go Live chrome MUST stay operational and generic; do not surface the loaded script title or script preview subtitle in the Go Live header/session bar just because a script is open.
- Go Live back navigation MUST return to the actual previous in-app screen when known, and only fall back to library when there is no valid in-app return target; it must never hardcode teleprompter as the back target.
- Go Live local recording MUST capture the same composed program feed that the active live/record session publishes; recording a black frame or a different source than the current program feed is a regression.
- Go Live local recording artifacts MUST contain both decodable video and decodable audio from the real program feed, and their saved resolution/quality must match the active source or chosen output profile instead of silently degrading to a lower-quality fallback.
- Go Live recording status and runtime panels MUST show the real recording details the browser runtime knows, including the resolved output profile and live session/file telemetry when available; blank recording metadata during an active local recording is a regression.
- Go Live default `Full` program layout MUST record and publish only the current active program camera; extra camera overlays may appear only when the operator explicitly chooses a multi-source layout such as split or picture-in-picture.
- Go Live audio meters MUST show real browser audio activity for microphone, program, and recording paths; static placeholder bars or seeded fake levels in the Audio tab are regressions.
- Go Live MUST have one active browser-side broadcast spine per setup: choose LiveKit or VDO.Ninja for the upstream transport, but do not run both as the same session's primary publish path at once.
- Go Live remote fan-out targets such as YouTube, Twitch, and custom RTMP MUST hang off the chosen upstream broadcast spine or its relay/egress layer; the browser runtime must not pretend to publish the same session independently to every platform without a real transport path.
- PrompterOne remains a true standalone browser app for Go Live too: do not introduce any PrompterOne-owned backend, relay, ingest service, or app-managed media server to make broadcasting work.
- For Go Live architecture, the browser is the only PrompterOne runtime. If remote publishing needs signaling, TURN, WHIP, or similar infrastructure, it must come from the chosen third-party transport platform and must not require a custom PrompterOne server tier.
- For Go Live final targets, prefer true client-side publish paths from the browser. If a platform such as YouTube, Twitch, or Custom RTMP has no real browser-compatible ingest path, treat it as constrained or blocked instead of silently designing around a hidden relay.
- Before marking a Go Live target such as YouTube or Twitch as blocked in the standalone architecture, verify the official chosen-transport docs first; do not dismiss VDO.Ninja client-side publish support without checking its current documented target support.
- Settings MUST own the camera and microphone inventory, per-device delay/sync offsets, and output quality profiles; Go Live may operate those persisted sources but must not invent screen-local source definitions or ad-hoc sync values.
- Go Live left rail MUST stay the operational source-control surface for video and audio inputs, while the right rail MUST stay a larger live-output panel for real runtime telemetry, output metadata, destination health, and mix controls.
- Go Live runtime telemetry such as bitrate, FPS, resolution, delay, ping, and destination state MUST come from the active SDK, recorder, or relay path only; guessed, duplicated, or placeholder values are forbidden.
- Go Live delivery priority is local recording of the composed program workspace first; remote streaming layers on only after the same program feed can be recorded locally with correct media, metadata, and operator controls.
- Learn and Teleprompter are separate screens with separate style ownership; do not bundle RSVP and teleprompter reader feature styles into one shared screen stylesheet or let one page inherit the other page's visual treatment.
- User preferences persistence MUST sit behind a platform-agnostic user-settings abstraction, with browser storage implemented via local storage and room for other platform-specific implementations; theme, teleprompter layout preferences, camera/scene preferences, and similar saved settings belong there instead of ad-hoc feature stores.
- Streaming destination/platform configuration MUST be user-defined and persisted in settings; Settings and Go Live must not ship hardcoded platform instances, seeded destination accounts, or fixed fake provider rows beyond real runtime capabilities.
- Runtime screens must not keep inline seeded operational data, fake demo rows, or screen-local platform/source presets in page/component code; reusable labels and presets belong in shared contracts or catalogs, while rendered rows must come from persisted settings, workspace state, or live session state.
- Go Live source rails must not render anonymous, unlabeled, or device-less camera cards; stale persisted scene sources without a real `deviceId` must be pruned, and rendered source cards should expose their real source/device identifiers for diagnostics.
- Build quality gates must stay green under `-warnaserror`.
- GitHub Pages is the expected CI publish target for the standalone WebAssembly app; publish automation must keep the app browser-only and Pages-compatible.
- GitHub Actions MUST keep separate, clearly named workflows for pull-request validation and release automation; vague workflow names are forbidden.
- Pull requests MUST have a dedicated validation workflow that runs the repo build and test gates before merge.
- Releases MUST have a dedicated workflow that produces the release build, creates or updates the release tag, and publishes a GitHub Release with the release artifacts/notes.
- The release workflow MUST be a full staged pipeline: build and tests first, then release publishing, and only then GitHub Pages deployment.
- CI or deployment work is not done when GitHub Actions is merely green; the deployed GitHub Pages app MUST be opened and verified to boot without shell errors.
- GitHub Pages deployment for `prompter.managed-code.com` MUST serve from the custom-domain root with `<base href="/">`; repo-name path prefixes are forbidden.
- Version text shown in the app must come from automated build or release metadata, never from manually edited About copy.
- The runtime must negotiate the initial language from the browser's supported cultures, fall back to English when there is no supported match, and persist the user's explicit language choice in user settings for later sessions.
- UI localization in Blazor must use resource-based localization with shared catalogs and named culture constants instead of screen-local hardcoded strings.
- Supported runtime cultures are English, Ukrainian, French, Spanish, Portuguese, Italian, and German.
- Russian must never be added as a supported runtime culture.

### Critical

- Never commit secrets, keys, or connection strings.
- Never skip tests to make a branch green.
- Never weaken a test or analyzer without explicit justification.
- Never introduce mocks, fakes, stubs, or service doubles to hide real behaviour in tests or local flows.
- Never introduce a non-SOLID design unless the exception is explicitly documented under `exception_policy`.
- Never force-push to `main`.
- Never approve or merge on behalf of a human maintainer.
- The agent may commit locally after the required tests and validation commands pass, but it must not push, merge, or otherwise publish repo changes until the user gives an explicit command to do so.

### Boundaries

Always:

- Read root and local `AGENTS.md` files before editing code.
- Read the relevant docs before changing behaviour or architecture.
- Run the required verification commands yourself.

Ask first:

- changing public API contracts
- adding new dependencies
- modifying database schema
- deleting code files

## Preferences

### Likes

- exact fidelity with the shipped Blazor UI and current runtime behavior, not a separate prototype tree
- localization as a default part of every UI task; menu labels, section copy, buttons, and other user-facing text should be localized in the same change, not deferred
- thin WASM host boundaries
- browser-realistic UI verification
- domain logic that stays reusable and serializable
- `Shouldly` as the default assertion layer when migrating or writing `.NET` tests, with framework-native assertions kept only where they provide a clear runner or browser-integration benefit
- one extra proactive hardening pass after a broad audit or release-critical task, even when the first CI run is already green, so lingering UX or lifecycle issues are caught before the user has to ask again

### Dislikes

- settings features that the user asks to surface as a menu option being hidden inside `About` or another existing section; when the request says "in the menu", add a dedicated settings navigation item
- fallback code paths, compatibility shims, or alternate behavior branches added "just in case"; implement the direct fix and only add a fallback when the user explicitly asks for one
- any "safety-net" or "just in case" workaround added to mask incorrect behavior; fix the root cause cleanly instead of layering retries, forced refocus, defensive rerenders, or compensating logic
- backend creep in the standalone runtime
- `git worktree`, temporary worktrees, throwaway repo copies, or off-branch isolation for normal repo tasks when the active workspace branch is available; do the work in the current repo and current branch unless the user explicitly asks for isolation
- OBS-coupled runtime architecture or UI; `PrompterOne` must be the streaming system itself, not an OBS companion or Browser Source wrapper
- hardcoded fallback reader/test fixtures such as inline `Ready` chunks, fake word models, or synthetic UI state embedded directly in tests when the same behavior can be exercised through shared script fixtures, builders, or production-owned constants
- agent-started local servers taking shared user ports or using ports outside the reserved `5050-5070` agent range
- brittle selectors without dedicated test attributes
- progress updates that imply a fix is done before there is concrete implementation and verification evidence; keep status factual and let the user verify final behavior personally
- long-running local import or conversion flows that leave the shell looking frozen; file import must expose a visible in-app busy/progress state and a clear completion or failure transition
- prematurely interrupting or stopping sub-agents during a requested parallel review/audit pass; when the user asks for multiple agents or external reviewers, let them finish and wait for their results unless the user explicitly redirects the workflow
- automated test or coverage runs for UI-behavior fixes before the user has manually checked the change locally; wait for the user's confirmation before resuming automation
- CI `dotnet test` steps configured with verbose or detailed console loggers that flood the logs with info output; prefer the default or otherwise minimal output that surfaces warnings and errors without noise
- browser-suite CI runs that effectively serialize the Playwright workload and drift into `40+` minute executions; keep real TUnit parallelism enabled and fix bottlenecks or flaky blockers instead of accepting that runtime
- test suites configured below the highest safe parallelism for the current hardware and shared-resource profile; keep tests as parallel as practical and serialize only the classes or groups that genuinely contend on shared browser or timing-sensitive state
- any continued xUnit ownership of the repo test stack; when the user asks for TUnit, remove xUnit packages and attributes instead of leaving a mixed long-term setup behind
- any use of `--no-build` in repo commands, docs, or CI; test runs must rebuild against the current source and current WASM assets every time
- mixed-language root README or public entry docs; keep them English-only unless the user explicitly asks otherwise
- any push or publish action without the user's explicit command; local commits are fine, but network delivery must stay user-controlled
- remediation work that spills into unrelated code outside the current change ownership; when validation exposes a failure outside the touched files or owned behavior, report it separately and do not "fix the suite" by editing someone else's area
- any reintroduction of a repo-local `design/` prototype folder as a parallel source of truth; the shipped Blazor UI must be the only product reference
- fake `display_*` or other presentation-only script metrics that override real TPS-derived words, segments, speed, or duration in user-facing UI
- made-up About/team content or stale attribution; About must point to real Managed Code ownership and official links
- any visible typing latency in the editor; plain input must feel immediate with no observable delay
- teleprompter controls that fade so much they become hard to see during real reading
- teleprompter starting with a narrowed text width instead of the design-max default
- teleprompter paragraph repositioning, line hopping, or per-word vertical transform updates that make the text jump; the shipped reader motion documented in `docs/Features/ReaderRuntime.md` is the required reference, with steady bottom-to-top movement and no extra animation layers beyond that contract
- teleprompter words or blocks appearing away from the focus line and only then drifting onto it; activation must look immediate
- teleprompter section changes that introduce odd transition motion instead of the straight reference direction
- any green teleprompter shell or background treatment; Teleprompter must stay on its dark reader palette and use emotion only for accents, not green screen-wide fills
- fragmented per-word underline styling where the intended emphasis should read as one continuous block
- punctuation showing up or being counted as standalone words in Learn or Teleprompter flows
- app logo clicks landing on a feature route instead of the main home/library screen
- Learn and Teleprompter style boundaries bleeding through a shared feature stylesheet; their visuals must stay isolated by page-owned style manifests
- Learn RSVP compositions that shift when shorter or longer words render; changing word length must not move the overall RSVP component or its anchored centerline
- teleprompter camera starting enabled by default; default reader startup should keep the camera off until the user explicitly enables it
- editor keystroke paths that persist, compile, or rebuild shared session state; keep plain typing in memory and move heavier local sync to debounce or autosave
- murky JavaScript or interop layers that keep product UI behavior in JS when Blazor can own it cleanly
- raw HTML blobs in editor chrome, toolbar catalogs, floating-menu catalogs, or metadata-rail rendering; editor UI content must be expressed through Blazor-owned markup/components or typed render models, with shared CSS classes and tokens instead of `MarkupString`/HTML-string composition
- point fixes that leave the raw-HTML toolbar/floating-menu pattern alive elsewhere; when this editor chrome debt is touched, refactor the whole pattern to Blazor components/render models across the slice instead of cleaning only one or two offending items
- visual UI elements should be extracted into reusable Blazor design-system components and then composed into pages; do not assemble repeated chrome, badges, glyph-label rows, or menu-item visuals ad hoc inside page/catalog code
- page- or catalog-level ownership of dropdown, menu, tooltip, badge, image, or icon-row visuals; standardized interactive chrome must live in reusable Blazor components with their own styles/contracts, and page code may only compose those components
- ad-hoc one-off UI cleanup without a repo search and explicit inventory of similar offenders first; when this class of design-system debt is touched, audit the whole slice, track the file list in a plan, and close items systematically with tests
- broad quality reviews that end at a report; when the audit finds concrete code smells or architecture violations, follow through with fixes in the same task unless an explicit blocker is documented
- repo-wide review or audit requests that start with broad automated test runs before the initial findings are written down; review artifacts and ranked findings must come first unless the user explicitly asks for a test baseline
- when the user asks to finish everything left in a backlog or root plan set, do not stop after the live regressions are green and leave remaining structural refactors as a follow-up; inventory the remaining items explicitly and either complete them in the same task or document the concrete blocker before claiming the work is done
- Blazor UI authored as page-sized raw markup blobs or HTML-string composition instead of small reusable Razor components with clear contracts; routed screens should read as composed component trees, not pasted HTML fragments
- duplicated styling or browser-behavior logic split across CSS, JS, and Razor when one reusable Blazor component or C# contract can own it cleanly
- runtime dependencies fetched from random external sources instead of vendored release artifacts
- progress updates that talk about internal skill routing instead of the concrete repo change
- long exploratory work before producing the concrete vendored files the user explicitly asked for
- unexpected browser debugger pause hooks in the default dev launch profile; browser debugging must stay explicit opt-in

## Preferred Skills

- `playwright` for browser verification and UI-flow debugging
