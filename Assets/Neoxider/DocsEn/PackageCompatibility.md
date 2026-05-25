# Package Compatibility

Compatibility notes for the package metadata, the local Unity project, and optional dependencies.

## Unity

| Source | Version |
|--------|---------|
| `Assets/Neoxider/package.json` | `version: 8.6.0`, `unity: 2022.1` |
| Local project `ProjectSettings/ProjectVersion.txt` | Unity `6000.3.14f1` |

The package keeps the lower UPM minimum at Unity 2022.1 so it can be consumed by older supported projects. The current repository is validated against Unity 6 project files.

## Package dependencies

| Dependency | Status |
|------------|--------|
| `com.unity.textmeshpro` | Required by UI/text helpers. |
| `com.unity.ai.navigation` | Required by navigation-facing components. |
| `com.cysharp.unitask` | Required by async modules. |
| DOTween | Required by runtime modules; installed as a third-party package in this project. |
| DOTween Pro | Optional; required only by selected sample/UI workflows. |
| Mirror | Optional; required by `Neo.Network` multiplayer flows. |
| URP | Optional; project/render-pipeline dependent. |

## Policy

- Do not raise the UPM `unity` field just because the development project uses Unity 6.
- Keep optional third-party integrations guarded so offline/package-only projects still compile when the optional dependency is absent.
- For `8.6.0`, the package version changed but the minimum Unity version and package dependency floor stayed unchanged.
- Update this page when `package.json`, `Packages/manifest.json`, or the documented install requirements change.
