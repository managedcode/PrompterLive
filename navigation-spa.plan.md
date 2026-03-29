# navigation-spa.plan

Chosen brainstorm: `navigation-spa.brainstorm.md`

## Goal

Remove full page reloads during screen changes and restore SPA route transitions.

## Scope

In scope:
- JS-to-Blazor navigation bridge
- preserve script `id` query between editor, learn, and teleprompter
- UI regression test for reload-free navigation

Out of scope:
- page-level data pipeline redesign
- backend work

## Ordered Steps

- [x] Confirm the root cause in `app.js`
- [x] Add a `NavigationManager` bridge in `MainLayout`
- [x] Route `navigateTo(...)` through Blazor SPA navigation
- [x] Add a browser regression test for no-reload navigation
- [x] Run targeted verification

## Validation Notes

- `MainLayout` now exposes a JS-invokable `NavigateClient` bridge and re-initializes the design shell on route changes.
- `app.js` preserves `?id=...` for editor, learn, and teleprompter routes and uses the Blazor bridge instead of `window.location.assign(...)` when available.
- `NavigationFlowTests.ScreenNavigation_UsesSpaRoutingWithoutReloadingBrowserContext` proves the browser context is preserved across editor -> learn -> teleprompter -> editor transitions.
- Focused routed-flow verification is green:
  - `dotnet test /Users/ksemenenko/Developer/PrompterLive/tests/PrompterLive.App.UITests/PrompterLive.App.UITests.csproj --no-build --filter "FullyQualifiedName~NavigationFlowTests|FullyQualifiedName~LibraryScreen_NavigatesIntoEditorAndSettings|FullyQualifiedName~EditorAndLearnScreens_ExposeExpectedInteractiveControls"`
- Full browser acceptance is also green:
  - `dotnet test /Users/ksemenenko/Developer/PrompterLive/tests/PrompterLive.App.UITests/PrompterLive.App.UITests.csproj --no-build`
  - Result: `20` tests passed in `3 m 37 s`

## Verification

- `dotnet build /Users/ksemenenko/Developer/PrompterLive/PrompterLive.slnx`
- `dotnet test /Users/ksemenko/Developer/PrompterLive/tests/PrompterLive.App.UITests/PrompterLive.App.UITests.csproj --filter "FullyQualifiedName~Navigation"`
