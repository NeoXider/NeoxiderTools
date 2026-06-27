# Package Compatibility

Compatibility notes for the package metadata, the local Unity project, and optional dependencies.

Checked on: 2026-06-19.

## Unity

| Source | Version |
|--------|---------|
| `Assets/Neoxider/package.json` | `version: 9.5.2`, `unity: 2022.1` |
| Local project `ProjectSettings/ProjectVersion.txt` | Unity `6000.3.14f1` |

The package keeps the lower UPM minimum at Unity 2022.1 so it can be consumed by older supported projects. The current repository is validated against Unity 6 project files.

## Package dependencies

| Dependency | Status |
|------------|--------|
| `com.unity.textmeshpro` | Package dependency `3.0.6`; required by TMP/UI helpers. |
| `com.unity.ai.navigation` | Package dependency `1.1.7`; local Unity 6 project uses `2.0.11`. |
| `com.unity.inputsystem` | Package dependency `1.14.2`; local Unity 6 project uses `1.19.0`. |
| `com.unity.ugui` | Package dependency `1.0.0`; needed by imported uGUI samples and UI helpers. |
| UniTask | Required external host-project dependency for async-heavy modules; not listed in `package.json.dependencies`. |
| DOTween | Required external host-project dependency for tween-based runtime modules. |
| DOTween Pro | Optional for project-specific UI animation workflows; `NeoxiderPages` imports without it. |
| Mirror | Optional; required by `Neo.Network` multiplayer flows. |
| URP | Optional; project/render-pipeline dependent. |

## Policy

- Do not raise the UPM `unity` field just because the development project uses Unity 6.
- Keep optional third-party integrations guarded so offline/package-only projects still compile when the optional dependency is absent.
- For `9.5.2`, the package version changed but the minimum Unity version stayed unchanged; `com.unity.ugui` is declared for imported uGUI samples.
- During active development samples live under `Assets/Neoxider/Samples`; before UPM release they move back to `Assets/Neoxider/Samples~`, while `package.json.samples[].path` remains release-facing (`Samples~/...`).
- After importing through Unity Package Manager, Unity copies samples into `Assets/Samples/NeoxiderTools/<version>/<sample name>/...`; validation supports that imported root as well.
- Update this page when `package.json`, `Packages/manifest.json`, or the documented install requirements change.
