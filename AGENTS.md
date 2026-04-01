# AGENTS.md

Project: `PrompterOne`
Stack: `.NET 10`, Blazor WebAssembly, Razor Class Library, xUnit, bUnit, Playwright

## Current Shape

`PrompterOne` is a standalone browser-first WebAssembly app.

- `src/PrompterOne.App` is the only runnable host.
- `src/PrompterOne.Shared` contains routed Razor UI, exact `design` styling, and browser interop.
- `src/PrompterOne.Core` contains TPS, RSVP, preview, workspace, media-scene, and streaming domain logic.
- `tests/` contains all automated test projects.
- `design/` is the visual and interaction source of truth.

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

## Rules to Follow (Mandatory)

### Commands

- `build`: `dotnet build /Users/ksemenenko/Developer/PrompterOne/PrompterOne.slnx -warnaserror`
- `test`: `dotnet test /Users/ksemenenko/Developer/PrompterOne/PrompterOne.slnx`
- `format`: `dotnet format /Users/ksemenenko/Developer/PrompterOne/PrompterOne.slnx`
- `coverage`: `dotnet test /Users/ksemenenko/Developer/PrompterOne/PrompterOne.slnx --collect:"XPlat Code Coverage"`

For this `.NET` repo:

- tests run on `VSTest` through `Microsoft.NET.Test.Sdk`
- `format` is direct `dotnet format`, not `--verify-no-changes` and not a wrapper
- coverage uses the VSTest `coverlet.collector` / `XPlat Code Coverage` collector
- `LangVersion` is not pinned; use the SDK default unless the repo intentionally changes it later

Useful focused commands:

- app run: `cd /Users/ksemenenko/Developer/PrompterOne/src/PrompterOne.App && dotnet run`
- core tests: `dotnet test /Users/ksemenenko/Developer/PrompterOne/tests/PrompterOne.Core.Tests/PrompterOne.Core.Tests.csproj`
- component tests: `dotnet test /Users/ksemenenko/Developer/PrompterOne/tests/PrompterOne.App.Tests/PrompterOne.App.Tests.csproj`
- ui tests: `dotnet test /Users/ksemenenko/Developer/PrompterOne/tests/PrompterOne.App.UITests/PrompterOne.App.UITests.csproj`
- playwright browser install: `node /Users/ksemenenko/Developer/PrompterOne/tests/PrompterOne.App.UITests/bin/Debug/net10.0/.playwright/package/cli.js install chromium`
- Build the relevant project immediately before starting a local dev server so `dotnet run` cannot serve stale WASM assets or binaries.

Browser test execution rules:

- Use one `dotnet test` process at a time for the browser suite.
- The browser suite self-hosts the built WASM assets on a dynamically assigned loopback HTTP origin.
- Each browser-suite host startup MUST request a fresh OS-assigned loopback port via `http://127.0.0.1:0`. Never pin or reuse a fixed browser-test port across runs.
- Inside that single process, the browser suite may run up to `4` parallel xUnit workers.
- Do not run `PrompterOne.App.UITests` in parallel with another `dotnet build` or `dotnet test` command.
- If a prior build already ran, prefer `dotnet test ... --no-build` for the browser suite.
- Do not add Python or ad-hoc runner scripts to bootstrap browser verification. The repo test commands must self-host the app and execute the flows end to end on their own.
- Browser UI scenarios are the primary acceptance gate for this repo. Component and core tests are supporting layers, not the release bar.
- Major user flows MUST be covered by long Playwright scenarios that execute real browser interactions end to end.
- Major browser scenarios MUST capture screenshot artifacts under `output/playwright/`.
- Editor typing and latency fixes are not done until they are reproduced and cleared on the live dev-host editor with real keyboard input, not only synthetic input helpers or the static UI-test host.
- When the user reports an editor regression on a specific script or exact `/editor?id=...` URL, reproduce on that same live script before treating browser-suite results as sufficient.
- Editor surface changes must ship with real-browser checks for scroll behavior, floating toolbar dropdowns, and TPS section controls when those areas are touched; static component tests alone are not enough.

Do not hijack shared user dev ports for agent-run preview servers. The user's stable local app ports, including `5041`, stay user-owned. When the agent needs an isolated local preview or manual-check server, run it only on ports in the `5050-5070` range. Keep the default stable launch-settings origin for the user's own dev workflow, and do not reclaim that origin for agent work when it is already in use. The browser-test harness is the exception: it must resolve a fresh dynamic loopback port and propagate the actual origin into Playwright `BaseURL` and permission grants.

Selector and constant rules:

- UI contracts MUST expose stable `data-testid` hooks for any flow covered by automated tests.
- Browser and component tests MUST prefer `data-testid` selectors over text, role-name, CSS-class, or DOM-shape selectors.
- If a stable `data-testid` exists, raw `GetByText`, `GetByRole(... Name = ...)`, `.Locator(".class")`, and `[data-testid='literal']` selectors are forbidden.
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

- [src/PrompterOne.App/AGENTS.md](/Users/ksemenenko/Developer/PrompterOne/src/PrompterOne.App/AGENTS.md)
- [src/PrompterOne.Core/AGENTS.md](/Users/ksemenenko/Developer/PrompterOne/src/PrompterOne.Core/AGENTS.md)
- [src/PrompterOne.Shared/AGENTS.md](/Users/ksemenenko/Developer/PrompterOne/src/PrompterOne.Shared/AGENTS.md)
- [tests/PrompterOne.Core.Tests/AGENTS.md](/Users/ksemenenko/Developer/PrompterOne/tests/PrompterOne.Core.Tests/AGENTS.md)
- [tests/PrompterOne.App.Tests/AGENTS.md](/Users/ksemenenko/Developer/PrompterOne/tests/PrompterOne.App.Tests/AGENTS.md)
- [tests/PrompterOne.App.UITests/AGENTS.md](/Users/ksemenenko/Developer/PrompterOne/tests/PrompterOne.App.UITests/AGENTS.md)

### Maintainability Limits

- `file_max_loc`: `400`
- `type_max_loc`: `200`
- `function_max_loc`: `50`
- `max_nesting_depth`: `3`
- `exception_policy`: `Document any justified exception in the nearest ADR, feature doc, or local AGENTS.md with the reason, scope, and removal/refactor plan.`

Local `AGENTS.md` files may tighten these values, but they must not loosen them without an explicit root-level exception.

### Task Delivery

- Start from [docs/Architecture.md](/Users/ksemenenko/Developer/PrompterOne/docs/Architecture.md) and the nearest local `AGENTS.md`.
- Treat [docs/Architecture.md](/Users/ksemenenko/Developer/PrompterOne/docs/Architecture.md) as the architecture map for every non-trivial task.
- Read [docs/Architecture.md](/Users/ksemenenko/Developer/PrompterOne/docs/Architecture.md) before implementation to identify the owning component, the allowed boundary, where code should be added, and where related code should be searched first.
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

### Documentation

- All durable docs live in `docs/`.
- [docs/Architecture.md](/Users/ksemenenko/Developer/PrompterOne/docs/Architecture.md) is the required global map and the first stop for agents.
- [docs/Architecture.md](/Users/ksemenenko/Developer/PrompterOne/docs/Architecture.md) MUST describe all major components and feature slices with what they are, why they exist, where they live, what they own, and what they must not own.
- [docs/Architecture.md](/Users/ksemenenko/Developer/PrompterOne/docs/Architecture.md) MUST document the app structure, design principles, and code-placement principles clearly enough that contributors can use it to decide where new code belongs and where existing behavior should be found.
- [docs/Architecture.md](/Users/ksemenenko/Developer/PrompterOne/docs/Architecture.md) MUST contain Mermaid diagrams for:
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
- Changed production code MUST reach at least 80% line coverage, and at least 70% branch coverage where branch coverage is available.
- Critical flows and public contracts MUST reach at least 90% line coverage with explicit success and failure assertions.
- Repository or module coverage must not decrease without an explicit written exception. Coverage after the change must stay at least at the previous baseline or improve.
- Coverage is for finding gaps, not gaming a number. Coverage numbers do not replace scenario coverage or user-flow verification.
- The task is not done until the full relevant test suite is green, not only the newly added tests.
- For this `.NET` repo, do not mix VSTest and Microsoft.Testing.Platform assumptions. The active model is VSTest.
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

Repo-specific design rules:

- Keep the solution in `PrompterOne.slnx`.
- Keep all production projects under `src/`.
- Keep all test projects under `tests/`.
- Keep shared build settings in `Directory.Build.props`.
- Keep shared package versions in `Directory.Packages.props`.
- Keep the pinned SDK version in `global.json`.
- Treat `design/index.html`, `design/tokens.css`, `design/components.css`, `design/styles.css`, and `design/app.js` as the exact design reference.
- Treat every file under `design/` as a static design/prototype reference only. Production UI must be implemented as Blazor components in `src/PrompterOne.Shared`; do not ship raw `design` HTML as runtime UI.
- Do not re-invent the UI when the answer should be “port the markup and classes from `design`”.
- For parity tasks, port the full routed screen from its matching `design/*.html` reference, not just isolated high-signal blocks. Settings, Editor, Learn, Teleprompter, and Go Live must match the reference screen in layout and intended interaction while staying Blazor/C# owned.
- About content must stay factual and current: do not invent team members or contributor names; use Managed Code attribution and official company links only.
- Do not introduce a server host for the app runtime.
- Preserve stable `data-testid` selectors on core flows because the Playwright suite depends on them.
- Keep UI routes in shared route constants and keep `data-testid` names in shared UI contract constants.
- Keep UI flow logic, keyboard shortcuts, DOM ids/selectors, and reusable UI constants in C#/Blazor contracts whenever the platform allows it; use JS only for unavoidable browser API interop or DOM access that Blazor cannot own directly.
- Prefer deleting JS files entirely when they only hold product UI behavior or duplicated constants; JS modules may exist only as thin bridges to browser APIs or external JS SDKs, with the owning workflow and state kept in C#/Blazor.
- For standalone cloud-storage integrations, persist provider keys, tokens, and connection metadata in browser `localStorage`; do not introduce server-side secret storage for runtime auth in this app shape.
- Third-party runtime JavaScript SDKs MUST be sourced only from explicitly pinned GitHub Release tags and assets, copied into the repo, bundled locally with their runtime dependencies, and never loaded from CDNs, package registries, `latest` endpoints, or ad-hoc remote downloads at app runtime.
- Repo-owned manifests, scripts, workflows, and project files that track third-party runtime JavaScript SDKs MUST point to concrete GitHub release versions and asset URLs, never floating references.
- Any vendored runtime JavaScript SDK that tracks an upstream GitHub repo MUST have an automated watcher job that checks new GitHub releases and opens a repo issue describing the required update when a newer release appears.
- Teleprompter TPS speed modifiers MUST affect both playback timing and subtle word- or phrase-level letter spacing, so slower spans open up slightly and faster spans tighten slightly without hurting readability.
- Teleprompter default reader width MUST start at the maximum readable width from the design unless the user explicitly narrows it; shipping a visibly narrower default is a regression.
- Teleprompter speed styling MUST produce a visible but tasteful letter-spacing or kerning change: slower text opens up slightly and faster text tightens slightly, not a no-op.
- Teleprompter reader word styling MUST mirror TPS/editor inline semantics: explicit inline TPS tags control per-word emphasis and color, while section or block emotion sets card context and must not recolor every reader word.
- Teleprompter underline or highlight treatments that span a phrase or block MUST render as one continuous block-level treatment; separate per-word underlines inside the same phrase are forbidden.
- Teleprompter reader text MUST appear on the focal guide immediately when a word or block becomes active; visible post-appearance drift or settling onto the guide is forbidden.
- Teleprompter route styles MUST be present on the first paint; a flash of unstyled or late-styled reader UI during route entry is a regression.
- Teleprompter block transitions MUST stay visually consistent: outgoing cards move upward and incoming cards rise from below in the same direction every time; alternating up/down travel is forbidden, and extra settling, bounce, or intermediate card states are forbidden.
- Teleprompter focus treatment MUST stay visually calm: the active focus word may be emphasized, but surrounding text should be gently dimmed instead of creating a bright moving blot, fake box, or attention-grabbing patch that flies up and down.
- Teleprompter emotion styling may tint the surface or accents, but reader text itself MUST stay easy to read and must not become harsh, over-bright, or saturated enough to hurt readability.
- Learn and Teleprompter playback timing MUST align with real word-by-word progression in the browser: WPM, speed modifiers, and word counting must match the emitted words, and timing work is not done until a browser-level word-sequence check proves it.
- Reader and Learn tokenization MUST treat punctuation-only tokens such as commas, periods, and dashes as punctuation attached to nearby words or pauses, never as standalone counted words.
- App-shell logo navigation MUST always lead to the main home/library screen; it must not deep-link into Go Live, Teleprompter, or another feature-specific route.
- Learn rehearsal speed MUST default to about 250 WPM and stay user-adjustable upward from that baseline; shipping a 300 WPM startup default is too aggressive.
- Go Live `ON AIR` badges and preview live dots MUST appear only while recording or streaming is actually active; idle selected or armed sources must stay visually non-live.
- Learn and Teleprompter are separate screens with separate style ownership; do not bundle RSVP and teleprompter reader feature styles into one shared screen stylesheet or let one page inherit the other page's visual treatment.
- User preferences persistence MUST sit behind a platform-agnostic user-settings abstraction, with browser storage implemented via local storage and room for other platform-specific implementations; theme, teleprompter layout preferences, camera/scene preferences, and similar saved settings belong there instead of ad-hoc feature stores.
- Streaming destination/platform configuration MUST be user-defined and persisted in settings; Settings and Go Live must not ship hardcoded platform instances, seeded destination accounts, or fixed fake provider rows beyond real runtime capabilities.
- Runtime screens must not keep inline seeded operational data, fake demo rows, or screen-local platform/source presets in page/component code; reusable labels and presets belong in shared contracts or catalogs, while rendered rows must come from persisted settings, workspace state, or live session state.
- Build quality gates must stay green under `-warnaserror`.
- GitHub Pages is the expected CI publish target for the standalone WebAssembly app; publish automation must keep the app browser-only and Pages-compatible.
- GitHub Actions MUST keep separate, clearly named workflows for pull-request validation and release automation; vague workflow names are forbidden.
- Pull requests MUST have a dedicated validation workflow that runs the repo build and test gates before merge.
- Releases MUST have a dedicated workflow that produces the release build, creates or updates the release tag, and publishes a GitHub Release with the release artifacts/notes.
- The release workflow MUST be a full staged pipeline: build and tests first, then release publishing, and only then GitHub Pages deployment.
- CI or deployment work is not done when GitHub Actions is merely green; the deployed GitHub Pages app MUST be opened and verified to boot without shell errors.
- GitHub Pages deployment for `prompter.managed-code.com` MUST serve from the custom-domain root with `<base href="/">`; repo-name path prefixes are forbidden.
- Version text shown in the app must come from automated build or release metadata, never from manually edited About copy.
- The runtime must negotiate browser language from supported cultures and default to English.
- Supported runtime cultures are English, Ukrainian, French, Spanish, Portuguese, and Italian.
- Russian must never be added as a supported runtime culture.

### Critical

- Never commit secrets, keys, or connection strings.
- Never skip tests to make a branch green.
- Never weaken a test or analyzer without explicit justification.
- Never introduce mocks, fakes, stubs, or service doubles to hide real behaviour in tests or local flows.
- Never introduce a non-SOLID design unless the exception is explicitly documented under `exception_policy`.
- Never force-push to `main`.
- Never approve or merge on behalf of a human maintainer.
- When the task explicitly needs delivery, the agent may commit, push to `main` or a feature branch, open a PR, and merge it after the required tests and validation commands pass.

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

- exact fidelity with `design`
- thin WASM host boundaries
- browser-realistic UI verification
- domain logic that stays reusable and serializable

### Dislikes

- backend creep in the standalone runtime
- hardcoded fallback reader/test fixtures such as inline `Ready` chunks, fake word models, or synthetic UI state embedded directly in tests when the same behavior can be exercised through shared script fixtures, builders, or production-owned constants
- agent-started local servers taking shared user ports or using ports outside the reserved `5050-5070` agent range
- brittle selectors without `data-testid`
- progress updates that imply a fix is done before there is concrete implementation and verification evidence; keep status factual and let the user verify final behavior personally
- automated test or coverage runs for UI-behavior fixes before the user has manually checked the change locally; wait for the user's confirmation before resuming automation
- mixed-language root README or public entry docs; keep them English-only unless the user explicitly asks otherwise
- design drift from `design`
- made-up About/team content or stale attribution; About must point to real Managed Code ownership and official links
- any visible typing latency in the editor; plain input must feel immediate with no observable delay
- teleprompter controls that fade so much they become hard to see during real reading
- teleprompter starting with a narrowed text width instead of the design-max default
- teleprompter paragraph repositioning, line hopping, or per-word vertical transform updates that make the text jump; `design/teleprompter.html` motion is the required reference, with steady bottom-to-top movement and no extra animation layers beyond the reference
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
- runtime dependencies fetched from random external sources instead of vendored release artifacts
- progress updates that talk about internal skill routing instead of the concrete repo change
- long exploratory work before producing the concrete vendored files the user explicitly asked for
- unexpected browser debugger pause hooks in the default dev launch profile; browser debugging must stay explicit opt-in

## Preferred Skills

- `playwright` for browser verification and UI-flow debugging
