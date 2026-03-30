# AGENTS.md

Project: `PrompterLive`
Stack: `.NET 10`, Blazor WebAssembly, Razor Class Library, xUnit, bUnit, Playwright

## Current Shape

`PrompterLive` is a standalone browser-first WebAssembly app.

- `src/PrompterLive.App` is the only runnable host.
- `src/PrompterLive.Shared` contains routed Razor UI, exact `new-design` styling, and browser interop.
- `src/PrompterLive.Core` contains TPS, RSVP, preview, workspace, media-scene, and streaming domain logic.
- `tests/` contains all automated test projects.
- `new-design/` is the visual and interaction source of truth.

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

## Rules to Follow (Mandatory)

### Commands

- `build`: `dotnet build /Users/ksemenenko/Developer/PrompterLive/PrompterLive.slnx -warnaserror`
- `test`: `dotnet test /Users/ksemenenko/Developer/PrompterLive/PrompterLive.slnx`
- `format`: `dotnet format /Users/ksemenenko/Developer/PrompterLive/PrompterLive.slnx`
- `coverage`: `dotnet test /Users/ksemenenko/Developer/PrompterLive/PrompterLive.slnx --collect:"XPlat Code Coverage"`

For this `.NET` repo:

- tests run on `VSTest` through `Microsoft.NET.Test.Sdk`
- `format` is direct `dotnet format`, not `--verify-no-changes` and not a wrapper
- coverage uses the VSTest `coverlet.collector` / `XPlat Code Coverage` collector
- `LangVersion` is not pinned; use the SDK default unless the repo intentionally changes it later

Useful focused commands:

- app run: `cd /Users/ksemenenko/Developer/PrompterLive/src/PrompterLive.App && dotnet run`
- core tests: `dotnet test /Users/ksemenenko/Developer/PrompterLive/tests/PrompterLive.Core.Tests/PrompterLive.Core.Tests.csproj`
- component tests: `dotnet test /Users/ksemenenko/Developer/PrompterLive/tests/PrompterLive.App.Tests/PrompterLive.App.Tests.csproj`
- ui tests: `dotnet test /Users/ksemenenko/Developer/PrompterLive/tests/PrompterLive.App.UITests/PrompterLive.App.UITests.csproj`
- playwright browser install: `node /Users/ksemenenko/Developer/PrompterLive/tests/PrompterLive.App.UITests/bin/Debug/net10.0/.playwright/package/cli.js install chromium`

Browser test execution rules:

- Use one `dotnet test` process at a time for the browser suite.
- The browser suite self-hosts the built WASM assets on a dynamically assigned loopback HTTP origin.
- Each browser-suite host startup MUST request a fresh OS-assigned loopback port via `http://127.0.0.1:0`. Never pin or reuse a fixed browser-test port across runs.
- Inside that single process, the browser suite may run up to `4` parallel xUnit workers.
- Do not run `PrompterLive.App.UITests` in parallel with another `dotnet build` or `dotnet test` command.
- If a prior build already ran, prefer `dotnet test ... --no-build` for the browser suite.
- Browser UI scenarios are the primary acceptance gate for this repo. Component and core tests are supporting layers, not the release bar.
- Major user flows MUST be covered by long Playwright scenarios that execute real browser interactions end to end.
- Major browser scenarios MUST capture screenshot artifacts under `output/playwright/`.

Do not override the production app runtime URL with `--urls` or random ports. Media permissions are origin-bound, so local development must stay on the stable launch-settings origin. If `dotnet run` fails because that port is already in use, stop the existing dev-server process instead of switching the app host to a new port. The browser-test harness is the exception: it must resolve a fresh dynamic loopback port and propagate the actual origin into Playwright `BaseURL` and permission grants.

Selector and constant rules:

- UI contracts MUST expose stable `data-testid` hooks for any flow covered by automated tests.
- Browser and component tests MUST prefer `data-testid` selectors over text, role-name, CSS-class, or DOM-shape selectors.
- If a stable `data-testid` exists, raw `GetByText`, `GetByRole(... Name = ...)`, `.Locator(".class")`, and `[data-testid='literal']` selectors are forbidden.
- Routes, route patterns, test ids, DOM ids, storage keys, keyboard shortcuts, seeded values, wait durations, and other repeated test inputs MUST come from named constants.
- URLs in tests MUST come from shared route helpers or constants, never inline literals.
- Magic numbers in tests are forbidden. Put timeouts, delays, counts, percentages, and seeded numeric inputs behind named constants.
- Prefer production-owned UI contract constants in `PrompterLive.Shared.Contracts` over duplicating selector strings in test projects.
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

- [src/PrompterLive.App/AGENTS.md](/Users/ksemenenko/Developer/PrompterLive/src/PrompterLive.App/AGENTS.md)
- [src/PrompterLive.Core/AGENTS.md](/Users/ksemenenko/Developer/PrompterLive/src/PrompterLive.Core/AGENTS.md)
- [src/PrompterLive.Shared/AGENTS.md](/Users/ksemenenko/Developer/PrompterLive/src/PrompterLive.Shared/AGENTS.md)
- [tests/PrompterLive.Core.Tests/AGENTS.md](/Users/ksemenenko/Developer/PrompterLive/tests/PrompterLive.Core.Tests/AGENTS.md)
- [tests/PrompterLive.App.Tests/AGENTS.md](/Users/ksemenenko/Developer/PrompterLive/tests/PrompterLive.App.Tests/AGENTS.md)
- [tests/PrompterLive.App.UITests/AGENTS.md](/Users/ksemenenko/Developer/PrompterLive/tests/PrompterLive.App.UITests/AGENTS.md)

### Maintainability Limits

- `file_max_loc`: `400`
- `type_max_loc`: `200`
- `function_max_loc`: `50`
- `max_nesting_depth`: `3`
- `exception_policy`: `Document any justified exception in the nearest ADR, feature doc, or local AGENTS.md with the reason, scope, and removal/refactor plan.`

Local `AGENTS.md` files may tighten these values, but they must not loosen them without an explicit root-level exception.

### Task Delivery

- Start from [docs/Architecture.md](/Users/ksemenenko/Developer/PrompterLive/docs/Architecture.md) and the nearest local `AGENTS.md`.
- Treat [docs/Architecture.md](/Users/ksemenenko/Developer/PrompterLive/docs/Architecture.md) as the architecture map for every non-trivial task.
- Read [docs/Architecture.md](/Users/ksemenenko/Developer/PrompterLive/docs/Architecture.md) before implementation to identify the owning component, the allowed boundary, where code should be added, and where related code should be searched first.
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
- [docs/Architecture.md](/Users/ksemenenko/Developer/PrompterLive/docs/Architecture.md) is the required global map and the first stop for agents.
- [docs/Architecture.md](/Users/ksemenenko/Developer/PrompterLive/docs/Architecture.md) MUST describe all major components and feature slices with what they are, why they exist, where they live, what they own, and what they must not own.
- [docs/Architecture.md](/Users/ksemenenko/Developer/PrompterLive/docs/Architecture.md) MUST document the app structure, design principles, and code-placement principles clearly enough that contributors can use it to decide where new code belongs and where existing behavior should be found.
- [docs/Architecture.md](/Users/ksemenenko/Developer/PrompterLive/docs/Architecture.md) MUST contain Mermaid diagrams for:
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
- For `PrompterLive`, prioritize browser UI tests first, then supporting component/core tests only where they help isolate failures.
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
- Design boundaries so real behaviour can be tested through public interfaces.
- The repo-root `.editorconfig` is the source of truth for formatting, naming, style, and analyzer severity. Use nested `.editorconfig` files only when they clearly serve a subtree-specific purpose.

Repo-specific design rules:

- Keep the solution in `PrompterLive.slnx`.
- Keep all production projects under `src/`.
- Keep all test projects under `tests/`.
- Keep shared build settings in `Directory.Build.props`.
- Keep shared package versions in `Directory.Packages.props`.
- Keep the pinned SDK version in `global.json`.
- Treat `new-design/index.html`, `new-design/tokens.css`, `new-design/components.css`, `new-design/styles.css`, and `new-design/app.js` as the exact design reference.
- Do not re-invent the UI when the answer should be “port the markup and classes from `new-design`”.
- Do not introduce a server host for the app runtime.
- Preserve stable `data-testid` selectors on core flows because the Playwright suite depends on them.
- Keep UI routes in shared route constants and keep `data-testid` names in shared UI contract constants.
- Build quality gates must stay green under `-warnaserror`.
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

- exact fidelity with `new-design`
- thin WASM host boundaries
- browser-realistic UI verification
- domain logic that stays reusable and serializable

### Dislikes

- backend creep in the standalone runtime
- random-port local startup
- brittle selectors without `data-testid`
- design drift from `new-design`

## Preferred Skills

- `playwright` for browser verification and UI-flow debugging
