# Package Compatibility

**What it is:** the supported Unity versions, the package and host dependencies, and what was verified on which editor. Read it before installing into an existing project or moving a project to another Unity version.

**How to use:**
1. Check the [Unity](#unity) minimum and the [package dependencies](#package-dependencies).
2. Before a Unity upgrade, read [version-sensitive integration](#version-sensitive-integration).
3. Installation steps are in [Getting Started](./GettingStarted.md#installation).

---

## Unity

| Source | Version |
|--------|---------|
| `Assets/Neoxider/package.json` | `version: 12.5.1`, `unity: 6000.0` |
| Local project `ProjectSettings/ProjectVersion.txt` | Unity `6000.3.14f1` |

The UPM minimum is `6000.0` (Unity 6) since `9.8.0`; the package is developed and validated against Unity 6 only. Projects on Unity 2022 LTS should stay on the last `9.7.x` release. The 12.x releases did not change the minimum or the UPM dependencies.

### Recorded verification

The last published verification record is for **11.0.0** (Windows, 26–27 October 2026). A comparable record for 12.x is not published.

| Scope | Recorded result |
|-------|-----------------|
| Unity `6000.3.14f1` (stable-ID branch for 6.0–6.4) and Unity `6000.6.0f1` (branch for 6.5+) | Compilation, discovery and qualified coverage of **4596 EditMode / 726 PlayMode** cases on each editor. |
| Dependency profiles, per editor | GUI **39**, Mirror-absent **6**, NewOnly **17 Edit / 93 Play**. |
| UPM consumer without DOTween Pro | Imports of all three samples of that version and **20/20 Play**. |
| Windows player networking | The three-process Windows player matrix passed for its recorded inputs. |

These groups overlap: do not add them up or read them as one fresh all-pass run. A run verifies its editor, profile and scope, not every Unity 6 minor, mobile target, IL2CPP build or device. The `6000.0` minimum does not claim that every later version was tested. Exact inputs, focused acceptance runs and manual checks are in [Verification 11](../Verification11.md); upgrade notes are in [Migration to 11](./Migration11.md) and [Upgrading to 12](./Migration12.md).

### Version-sensitive integration

Keep compatibility switches in shared helpers and compile each conditional branch with its target Unity version. A source review alone does not establish support for a future editor.

| Area | Current integration | Verify when changing the Unity version |
|------|---------------------|----------------------------------------|
| Object identity | Runtime code uses `NeoDiagnostics.StableId(Object)`; editor diagnostics use `NeoComponentHealth.StableId`. They select `GetInstanceID` on Unity 6.0–6.4 and `GetEntityId` on 6.5+. Missing-reference property checks live in the editor helper too. | Compile on both sides of the switch. Do not repeat version-specific calls at call sites. |
| UI Toolkit | Editor windows and inspectors use UI Toolkit. | Compile the editor assemblies and open the affected windows. |
| Optional integrations | Assembly version defines cover Mirror, DOTween, the mobile haptics backend and Unity. The optional Input System adapter uses reflection. | Check the affected assemblies with each optional dependency present and absent. A fallback path does not validate the enabled branch. |
| Enter Play Mode Options | Static caches, registries and events use module reset hooks such as `SubsystemRegistration`. | Enter and exit Play Mode repeatedly with domain reload disabled, for the modules the project uses. |

## Package dependencies

| Dependency | Status |
|------------|--------|
| `com.unity.ugui` | UPM dependency, pinned `2.0.0` in the package and the development project. Needed by the UI helpers and uGUI demos. |
| `com.unity.ai.navigation` | UPM dependency, pinned `2.0.16`. |
| `com.unity.inputsystem` | UPM dependency, pinned `1.20.1`. |
| `com.unity.textmeshpro` | Not a package dependency since `10.0.1`: TMP ships inside `com.unity.ugui` on Unity 6. The TMP/UI helpers still need it at runtime. |
| UniTask | Required. Install it in the host project; it is not in `package.json` dependencies. |
| DOTween | Required by the tween-based runtime modules. Install it in the host project. |
| DOTween Pro | Optional; `NeoxiderPages` imports without it. |
| Mirror | Optional; required by `Neo.Network` multiplayer flows. |
| MobileHapticFeedback | Optional backend for richer [Haptics](./Haptics/Haptics.md) device feedback. |
| Character Movement Fundamentals | Bundled runtime and editor code under `ThirdParty/CharacterMovementFundamentals`. Its demo scenes are the separate `NeoxiderTools.CharacterMovementDemo` download. |
| `com.neoxider.htmltouitk` | Optional separate package for HTML/CSS → UI Toolkit conversion ([HtmlToUitk](https://github.com/NeoXider/HtmlToUitk)). No NeoxiderTools assembly references it. |
| URP | Optional; depends on the project's render pipeline. |

Host pins, SDK-resolved versions and upgrade constraints: [declared dependency baseline](./Dependencies.md#declared-dependency-baseline). Usage and license pointers: [THIRD-PARTY-NOTICES.md](../THIRD-PARTY-NOTICES.md).

## Distribution

- Git installs use the `upm` branch, which holds only the package. The package declares no UPM samples.
- Each release has `NeoxiderTools.v<ver>.unitypackage` plus one archive per demo, imported into `Assets/NeoxiderDemos/` ([release archives](./Samples.md#release-archives)).
- `python scripts/build_distribution.py upm` writes the `upm` branch; `python scripts/build_distribution.py unitypackages` writes the archives. Both read a committed revision.
- The repository has no GitHub Actions workflows; final archive export and import is a separate release gate.

## Validation host

Test prerequisites and isolated-host rules are in [Testing](./Testing.md#running-tests). The broad test assemblies reference integrations such as Mirror even where a runtime module has a solo path.

## Policy

- Keep the UPM `unity` field equal to the supported floor. Do not lower `6000.0` without a compatibility pass.
- Keep optional third-party integrations guarded, so package-only projects compile without them.
- Demo scenes live outside the package in `Assets/NeoxiderDemos/<name>` and ship as separate archives; `package.json` declares no samples. Package assets must not reference demo GUIDs.
- The `demo` preparation branch does not change the sample layout or publish a package ref.
- Update this page when `package.json`, `Packages/manifest.json` or the install requirements change.

## IL2CPP

[IL2CPP.md](./IL2CPP.md) covers the reflection and code-stripping caveat for `NeoCondition`, `ComponentFloatBinding`, `[SaveField]`, `NetworkPropertySync` and `NetworkReactiveSync`, and the bundled `link.xml` mitigation.
