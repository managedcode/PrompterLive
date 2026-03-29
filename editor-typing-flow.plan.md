# editor-typing-flow.plan

Chosen brainstorm: `editor-typing-flow.brainstorm.md`

## Goal

Add browser acceptance coverage that literally types TPS script text into the editor and proves the live authoring flow updates structure state and persists across reload.

## Scope

In scope:
- a dedicated Playwright typing flow test for the editor
- docs/plan updates needed to record the new verification contract

Out of scope:
- editor production refactors unless the typing path reveals a real bug
- non-editor screens

## Constraints And Risks

- use real keyboard typing in the browser instead of JS value injection for the authored TPS content
- keep the test deterministic and reasonably fast
- preserve the standalone `dotnet test` workflow without env vars or manual app startup

## Testing Methodology

Flows covered:
- clear the source editor through keyboard interaction
- type TPS segment/block headers and body text directly in the textarea
- verify structure inspector fields update from typed headers
- verify typed content persists after reload

How they are tested:
- Playwright only, through the live browser runtime

Quality bar:
- the test uses keyboard typing for the script body
- assertions are on visible user-facing outcomes, not only internal state

## Ordered Plan

- [x] Step 1. Write the brainstorm and choose the direction.
- [x] Step 2. Write this plan.
- [x] Step 3. Add a failing editor typing acceptance test.
  Verification: run focused editor UI tests.
- [x] Step 4. Fix any production or test-harness issues exposed by the typing flow.
  Verification: rerun focused editor UI tests.
- [x] Step 5. Run final validation.
  Verification:
  - `dotnet build /Users/ksemenenko/Developer/PrompterLive/PrompterLive.slnx`
  - `dotnet test /Users/ksemenenko/Developer/PrompterLive/tests/PrompterLive.App.UITests/PrompterLive.App.UITests.csproj --filter "FullyQualifiedName~EditorTypingTests|FullyQualifiedName~EditorSourceSyncTests"`
  - `dotnet test /Users/ksemenenko/Developer/PrompterLive/tests/PrompterLive.App.UITests/PrompterLive.App.UITests.csproj`
  - `dotnet format /Users/ksemenenko/Developer/PrompterLive/PrompterLive.slnx`

## Execution Notes

- The new browser typing regression was added in `tests/PrompterLive.App.UITests/EditorTypingTests.cs`.
- The typing flow passed as a real keyboard-driven scenario and proved structure sync plus reload persistence.
- Final verification found a test cleanup gap in the UI suite: several tests closed only the page and left the browser context alive until fixture teardown.
- UI cleanup was tightened by closing the page context in each test, which preserved the real browser flow and kept the full suite deterministic.
- Final verification results:
  - `dotnet build` succeeded with `12 warnings` and `0 errors`.
  - Focused editor UI tests passed: `2/2`.
  - Full UI test suite passed: `17/17` in `3m52s`.
  - `dotnet format` completed; the repo still has existing non-auto-fixable analyzer items outside this task (`IDE0060`, `CA1305`, `CA1826`).

## Baseline Failures

- None recorded before adding the new typing regression.
