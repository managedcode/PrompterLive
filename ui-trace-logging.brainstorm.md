# UI Trace Logging Brainstorm

## Problem

The app already has diagnostics for recoverable and fatal errors, but routine navigation and library interactions still feel opaque during debugging.

Recent regressions showed that we need better traceability for:

- SPA route transitions
- folder selection and folder-create overlay flow

## Goal

Add lightweight `ILogger` instrumentation to the UI shell and library screen so common actions leave useful breadcrumbs without spamming logs.

## Options

### Option A

Log every input mutation and every render event.

Pros:

- maximum detail

Cons:

- noisy
- hard to read

### Option B

Log only meaningful user-visible transitions and state changes:

- navigation bridge attach
- client-side navigation requests
- route changes
- folder selection
- folder create overlay open/cancel

Pros:

- useful debug breadcrumbs
- small code change
- easy to assert in tests

Cons:

- less granular than full input-level tracing

## Recommended Direction

Option B.

## Risks

- logging too aggressively at `Information` can pollute normal output
- test assertions should not overfit exact wording more than necessary
