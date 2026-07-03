# Package Compatibility

Compatibility notes for the package metadata, the local Unity project, and optional dependencies.

Checked on: 2026-07-03.

## Unity

| Source | Version |
|--------|---------|
| `Assets/Neoxider/package.json` | `version: 9.8.2`, `unity: 6000.0` |
| Local project `ProjectSettings/ProjectVersion.txt` | Unity `6000.3.14f1` |

The UPM minimum was raised from `2022.1` to `6000.0` (Unity 6) starting with `9.8.0` — the package is now developed and validated against Unity 6 only. Projects on Unity 2022 LTS should stay on the last `9.7.x` release.

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

See [THIRD-PARTY-NOTICES.md](../THIRD-PARTY-NOTICES.md) for what each dependency is used for and license pointers.

## Policy

- Keep the UPM `unity` field in sync with the actually-supported floor; it was deliberately raised to `6000.0` for `9.8.0` — do not lower it without a corresponding compatibility pass.
- Keep optional third-party integrations guarded so offline/package-only projects still compile when the optional dependency is absent.
- During active development samples live under `Assets/Neoxider/Samples`; before UPM release they move back to `Assets/Neoxider/Samples~`, while `package.json.samples[].path` remains release-facing (`Samples~/...`).
- After importing through Unity Package Manager, Unity copies samples into `Assets/Samples/NeoxiderTools/<version>/<sample name>/...`; validation supports that imported root as well.
- Update this page when `package.json`, `Packages/manifest.json`, or the documented install requirements change.

## IL2CPP

See [IL2CPP.md](./IL2CPP.md) for the reflection/code-stripping caveat affecting `NeoCondition`, `ComponentFloatBinding`, `[SaveField]`, `NetworkPropertySync`, and `NetworkReactiveSync`, and the bundled `link.xml` mitigation.
