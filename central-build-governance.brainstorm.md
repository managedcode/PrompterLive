# Central Build Governance Brainstorm

## Problem

Package versions and common build properties are currently repeated across project files.

That causes:

- duplicated package version declarations
- drift risk between app, shared, core, and test projects
- no single repo-level place for target framework, analyzer policy, and app version metadata
- no pinned SDK file for the expected `dotnet` toolchain

## Goal

Adopt a clean central build layout:

- `Directory.Packages.props` for package versions
- `Directory.Build.props` for shared build settings
- `global.json` for the actual SDK pin

## Options

### Option A

Only add `Directory.Packages.props`.

Pros:

- smallest change

Cons:

- leaves target framework and build policy duplicated
- does not solve the SDK-version question

### Option B

Add `Directory.Packages.props`, `Directory.Build.props`, and `global.json`, then remove repeated settings from the `.csproj` files.

Pros:

- single source of truth for package versions
- single source of truth for shared project properties
- correct file for SDK version pinning

Cons:

- slightly broader repo churn

## Recommended Direction

Option B.

## Risks

- CPM can surface restore warnings such as `NU1507` when multiple package sources exist
- moving properties too aggressively can accidentally override project-specific behavior
- setting warnings-as-errors globally would break the current repo because existing analyzers still report warnings
