# Test Selector Constants Plan

## Goal

Refactor the active WASM test layer so it relies on shared constants and stable `data-testid` selectors instead of repeated string literals, fragile text selectors, raw URLs, and unexplained magic numbers.

## Scope

In scope:

- root and local `AGENTS.md` updates for test-selector and constant rules
- shared test constants/helpers for routes, selectors, URLs, timings, and repeated text contracts
- refactor of `tests/PrompterLive.App.Tests`
- refactor of `tests/PrompterLive.App.UITests`
- add `data-testid` hooks in app markup where browser tests still rely on text

Out of scope:

- full repo-wide elimination of every literal in all production code in one pass
- non-test architectural refactors outside the touched UI surfaces

## Constraints And Risks

- Keep the suite standalone-WASM only.
- Preserve current green runtime behavior while tightening selectors.
- Avoid parallel browser-suite execution; the suite owns its self-hosted origin.
- Keep the diff understandable enough for the coming larger refactor.

## Testing Methodology

- Component tests must select page-owned DOM through shared constants, not inline strings.
- Browser tests must prefer `GetByTestId` and route constants over `GetByText` and raw paths.
- Timings, URLs, regex fragments, and repeated labels must move to named constants.
- Verification commands:
  - `dotnet build /Users/ksemenenko/Developer/PrompterLive/PrompterLive.slnx`
  - `dotnet test /Users/ksemenko/Developer/PrompterLive/tests/PrompterLive.App.Tests/PrompterLive.App.Tests.csproj`
  - `dotnet test /Users/ksemenko/Developer/PrompterLive/tests/PrompterLive.App.UITests/PrompterLive.App.UITests.csproj --no-build`
  - `dotnet format /Users/ksemenko/Developer/PrompterLive/PrompterLive.slnx`

## Baseline

- [x] Review root and local `AGENTS.md`
- [ ] Audit inline route/selectors/text/timing literals in active test projects
- [ ] Run baseline build/tests after plan creation

## Ordered Steps

- [ ] Add policy updates to `AGENTS.md` files for `data-testid`-first selectors and named constants in tests
- [ ] Introduce shared test constants/helpers for routes, selectors, URLs, regex fragments, and timing values
- [ ] Refactor `PrompterLive.App.Tests` to use shared constants/helpers
- [ ] Refactor `PrompterLive.App.UITests` to use shared constants/helpers and reduce text-based selectors
- [ ] Add missing `data-testid` hooks in app markup where needed for stable browser automation
- [ ] Run build, app tests, UI tests, and format; fix regressions

## Failing Tests Tracking

- [ ] No known failing tests at plan creation

## Done Criteria

- [ ] active test projects stop hardcoding repeated routes and selector strings inline
- [ ] browser suite primarily uses `data-testid`
- [ ] repeated timings and URLs are named constants
- [ ] AGENTS policy documents the rules clearly
- [ ] build, app tests, UI tests, and format are green
