# navigation-spa.brainstorm

## Problem

Screen changes are slow because `app.js` currently routes through `window.location.assign(...)`.

That forces a full browser reload instead of a Blazor client-side route transition.

## Root Cause

- `navigateTo(screenId)` resolves a route and calls `window.location.assign(targetRoute)`
- header and toolbar buttons depend on this function
- every screen change rebuilds the whole WASM runtime

## Direction

Replace reload-based JS navigation with a small bridge into `NavigationManager`.

Keep the `new-design` JS header shell, but send route changes through Blazor SPA navigation and preserve the active script `id` query when moving between editor, learn, and teleprompter.
