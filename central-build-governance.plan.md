# Central Build Governance Plan

Reference brainstorm: `central-build-governance.brainstorm.md`

## Goal

Centralize package versions and shared build settings while keeping the current runtime and test behavior unchanged.

## Scope

In scope:

- add `Directory.Packages.props`
- add `Directory.Build.props`
- add `global.json`
- remove duplicated package versions and shared properties from project files
- document the new build-governance shape

Out of scope:

- package upgrades
- analyzer cleanup
- runtime behavior changes

## Testing Methodology

- build the full solution after centralization
- run the core, component, and UI suites against the centralized configuration
- run formatting after the build/test pass

## Ordered Plan

- [x] Add root-level build-governance files.
- [x] Move package versions into `Directory.Packages.props`.
- [x] Move shared target/analyzer/version settings into `Directory.Build.props`.
- [x] Pin the SDK in `global.json`.
- [x] Remove duplicated settings from the `.csproj` files.
- [x] Update durable docs to point to the new canonical config files.
- [x] Run validation:
  - `dotnet format /Users/ksemenenko/Developer/PrompterLive/PrompterLive.slnx`
  - `dotnet build /Users/ksemenenko/Developer/PrompterLive/PrompterLive.slnx`
  - `dotnet test /Users/ksemenenko/Developer/PrompterLive/tests/PrompterLive.Core.Tests/PrompterLive.Core.Tests.csproj --no-build`
  - `dotnet test /Users/ksemenenko/Developer/PrompterLive/tests/PrompterLive.App.Tests/PrompterLive.App.Tests.csproj --no-build`
  - `dotnet test /Users/ksemenenko/Developer/PrompterLive/tests/PrompterLive.App.UITests/PrompterLive.App.UITests.csproj --no-build`
  - `dotnet test /Users/ksemenenko/Developer/PrompterLive/PrompterLive.slnx`

## Validation Notes

- `dotnet format` completed successfully. It reported existing non-fixable diagnostics such as `IDE0060`, `CA1305`, and `CA1826`, but did not fail.
- `dotnet build` succeeded with `0 warnings` and `0 errors`.
- `PrompterLive.Core.Tests`: `21/21` passed.
- `PrompterLive.App.Tests`: `35/35` passed.
- `PrompterLive.App.UITests`: `20/20` passed in `3 m 36 s`.
- Full `dotnet test /Users/ksemenenko/Developer/PrompterLive/PrompterLive.slnx` passed after the centralization changes.
