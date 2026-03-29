# logging-diagnostics.brainstorm

## Problem Framing

The app is hard to debug because failures disappear into WASM browser logs or break async UI flows without consistent instrumentation. The user explicitly asked for:

- `ILogger`-based logging
- clearer error handling, especially around interactive UI operations
- a better debugging story when a page action fails

Today the host does not configure diagnostics explicitly, routed UI pages have almost no structured logging, and there is no global UI error surface for unexpected exceptions.

## Options

### Option 1: Sprinkle `ILogger<T>` into a few pages

Pros:
- small change set
- easy to wire

Cons:
- inconsistent coverage
- no common error surface
- each page would invent its own exception handling

### Option 2: Add a shared diagnostics layer for UI operations plus a global error boundary

Pros:
- consistent logging across pages
- single place for user-facing error state
- easier to extend to more routed screens
- keeps host setup, pages, and error UI aligned

Cons:
- requires a few new shared services/components
- more up-front wiring

## Recommended Direction

Choose Option 2.

Add:

- explicit WASM logging configuration in the host
- a shared diagnostics service for logging and surfacing recoverable UI errors
- a global error boundary component for unhandled rendering exceptions
- page-level use of the diagnostics service for major async flows

## Risks

- over-logging can create noisy browser output
- adding try/catch blocks directly in pages can become repetitive
- new UI error chrome must not break `new-design` fidelity

## Mitigations

- log with structured messages at meaningful boundaries only
- centralize recoverable operation handling in one shared service
- render diagnostics using a small overlay/banner aligned with the existing visual language
