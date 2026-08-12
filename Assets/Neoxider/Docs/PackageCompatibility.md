# Package Compatibility

Compatibility notes for the package metadata, the local Unity project, and optional dependencies.

Checked on: 2026-08-12.

## Unity

| Source | Version |
|--------|---------|
| `Assets/Neoxider/package.json` | `version: 10.8.4`, `unity: 6000.0` |
| Local project `ProjectSettings/ProjectVersion.txt` | Unity `6000.3.14f1` |

The UPM minimum was raised from `2022.1` to `6000.0` (Unity 6) starting with `9.8.0` — the package is now developed and validated against Unity 6 only. Projects on Unity 2022 LTS should stay on the last `9.7.x` release.

### Forward compatibility with Unity 6.5 / 6.6 / 6.7

The package is developed on `6000.3` and consumed by projects already on `6000.6`. Audited on 2026-08-12
against the removals and obsolete-to-error changes announced for those releases:

| Area | Change | Package status |
|------|--------|----------------|
| UI Toolkit | `UxmlTraits`, `UxmlFactory`, `Uxml*AttributeDescription` and `IUxmlAttributes` removed in 6.6 | Not affected — the package ships no `.uxml` assets and registers no element through `UxmlFactory`/`UxmlTraits`. Its UI Toolkit usage is limited to editor windows and inspectors that build trees in C#; the one custom element, `GradientRect` in `Scripts/Abilities/Editor/AbilityDesignerUI.cs`, is constructed from C# and paints through `MeshGenerationContext.Allocate`, which is still current in 6.6. |
| UI Toolkit | `UIToolkitInputConfiguration.SetRuntimeInputBackend` removed in 6.6 | Not used. |
| Input System | `com.unity.inputsystem` becomes a built-in engine module in 6.7, deprecating `versionDefines` against it | No `.asmdef` in the package declares a `versionDefines` entry for `com.unity.inputsystem`; the optional Input System integration is reflection-based (`OptionalInputSystemAdapter`) and therefore unaffected. The only `versionDefines` in use target Mirror and DOTween. |
| Obsolete-to-error | ~160 Mecanim/Animation, Physics, Audio, NavMesh and Timeline APIs plus 49 Hierarchy APIs turn into compile errors in 6.7 | None are referenced. Physics code already uses the Unity 6 names (`linearVelocity`, `linearDamping`, `angularDamping`); NavMesh usage is limited to `NavMesh.SamplePosition`/`CalculatePath`/`AllAreas`, which are current. |
| Serialization | Reference cycles in serializable types (`UAC1005`–`UAC1008`) become compile errors in 6.7 | A full graph pass over the 544 serializable types found no self-referencing, mutual or indirect cycles. |
| Domain Reload | Statics are not reset when a consumer enables Enter Play Mode Options | Every module that holds mutable static state resets it from `RuntimeInitializeOnLoadMethod(SubsystemRegistration)`, and on Unity 6.5+ additionally from `[OnExitingPlayMode]`. Keep new static caches, registries, init flags and static events on that pattern. The type carrying the lifecycle attribute must be partial, and so must every enclosing type if it is nested. |

## Package dependencies

| Dependency | Status |
|------------|--------|
| `com.unity.textmeshpro` | **Not a package dependency** since `10.0.1` — TMP ships inside `com.unity.ugui` on Unity 6. Still required at runtime by the TMP/UI helpers. |
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
