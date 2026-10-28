# Package Compatibility

Compatibility notes for the package metadata, the local Unity project, and optional dependencies.

Updated for **11.0.0**, 2026-10-27. Windows verification recorded on **26–27 October 2026** covers Unity `6000.3.14f1` and `6000.6.0f1`, including accepted CardGame sizing/gameplay checks and three manual OS-click rounds. Results belong to their tested inputs and qualified unchanged graphs; see [recorded verification and limits](../Verification11.md) and [Migration to 11](./Migration11.md). The tag and release assets identify the published version.

## Unity

| Source | Version |
|--------|---------|
| `Assets/Neoxider/package.json` | `version: 11.0.1`, `unity: 6000.0` |
| Local project `ProjectSettings/ProjectVersion.txt` | Unity `6000.3.14f1` |

The UPM minimum was raised from `2022.1` to `6000.0` (Unity 6) starting with `9.8.0` — the package is now developed and validated against Unity 6 only. Projects on Unity 2022 LTS should stay on the last `9.7.x` release.

### Recorded verification and current acceptance

| Scope | Recorded baseline / focused acceptance / limits |
|------------------------|-----------------------|
| Windows Unity `6000.3.14f1`; stable-ID branch used by 6.0–6.4 | Recorded compilation/discovery and qualified positive coverage of **4596 EditMode / 726 PlayMode** cases. |
| Windows Unity `6000.6.0f1`; stable-ID branch used by 6.5+ | Recorded compilation/discovery and qualified positive coverage of **4596 EditMode / 726 PlayMode** cases. |
| Additional dependency/input profiles | Per SDK: GUI **39**, Mirror-absent **6**, NewOnly **17 Edit / 93 Play**, including one actual pointer case. |
| Current UPM consumer, DOTween Pro absent | Per SDK: fresh P31 public UPM imports of all **three** samples and **20/20 Play**; P30 **28/28 Edit** qualified by unchanged graph identity. |
| Focused Cards/sample acceptance | Per SDK: P27 Cards **149/149 Edit**, P29 Cards + Samples **46/46 Play**, P31 `SampleRuntimeSceneSmokeTests` **10/10**. The P31 scene click requires exactly one round through the actual EventSystem top-graphic raycast and `ExecuteHierarchy`. |
| Current HtmlToUitk acceptance | Fresh CLI **2573 passed/30 NotExecuted/0 failed**, separate pure logic **291/291**, Round3 complete Unity assembly **370/370,0 skipped per SDK**; real CLI/native import/Play UI plus Unity 6.3 OS manual at1280×720. [Exact scope](UI/HtmlToUitkVerification11.md). |
| Windows player networking | P23 genuine **three-process Windows player** matrix passed for its recorded network inputs. |
| Current sample/UI acceptance | Eleven scenes examined manually; CardGame completed three OS-click rounds with tutorial advancement. Piles at scale **0.7** fit Free Aspect and 16:9. Audio UI checks do not establish audible output; OS-level Dice dragging remains inconclusive. |

These groups overlap: do not add them together or describe qualified coverage as one fresh all-pass run. The [verification record](../Verification11.md) identifies source inputs, actual runs and qualification of unchanged inputs. A successful run verifies that Editor/profile and scope, not every Unity 6 minor, mobile target, IL2CPP build or device. The declared `6000.0` minimum is not a claim that every version above it was tested. The dated HTML → UITK report below remains historical module evidence.

### Version-sensitive integration

Legacy release archives are choose-one **Core** (`NeoxiderTools.Core.v11.0.0.unitypackage`, no samples)
or **Complete** (`NeoxiderTools.v11.0.0.unitypackage`, all three visible Samples trees). UPM source remains
Samples~. The converter is a separate `HtmlToUitk.Cli.v11.0.0.zip`, net9 target/LatestMajor on tested.NET 10;
SDK 10.0.301 supports source builds. Final archive export/import is a separate release gate. The hosted
package check passed for 11.0.0; hosted Unity jobs did not run because GameCI secrets were unavailable.
GitHub Actions workflows were subsequently removed. Local native receipts remain separate evidence.

Keep compatibility switches in shared helpers and verify each conditional branch with its target Unity compiler. A source review alone cannot establish support for a future editor version.

| Area | Current integration | Verification needed when changing Unity version |
|------|---------------------|------------------------------------------------|
| Object identity | Runtime code uses `NeoDiagnostics.StableId(Object)`; editor diagnostics use `NeoComponentHealth.StableId`. The helpers select `GetInstanceID` for Unity 6.0–6.4 and `GetEntityId` for 6.5+. Missing-reference property checks are centralized in the editor helper as well. | Compile on both sides of the version switch; do not duplicate version-specific calls at usage sites. |
| UI Toolkit | The package uses UI Toolkit in editors and in the [HTML → UITK runtime/editor module](./UI/HtmlToUitk.md). Its external CLI generates UXML/USS; runtime behavior includes controls, actions, and lifecycle. | Compile the editor and exercise generated UI in the intended player target. The [HTML → UITK verification report](https://github.com/NeoXider/NeoxiderTools/blob/main/Docs/Plans/HtmlToUitk-Verification-20261014.md) records a dated module test slice, not package-wide or mobile certification. |
| Optional integrations | Assembly version defines currently cover Mirror, DOTween, the mobile haptics backend, and Unity. The optional Input System adapter uses reflection. | Check the relevant assemblies with each optional dependency present and absent; a fallback path does not validate the enabled branch. |
| Enter Play Mode Options | Static caches, registries, and events use module-specific reset hooks such as `SubsystemRegistration`. | Exercise repeated enter/exit with domain reload disabled for the modules used by the host project. See each module's lifecycle notes. |

## Package dependencies

| Dependency | Status |
|------------|--------|
| `com.unity.textmeshpro` | **Not a package dependency** since `10.0.1` — TMP ships inside `com.unity.ugui` on Unity 6. Still required at runtime by the TMP/UI helpers. |
| `com.unity.ai.navigation` | Package and development-project manifest pin `2.0.16`. |
| `com.unity.inputsystem` | Package and development-project manifest pin `1.20.1`. |
| `com.unity.ugui` | Package and development-project manifest pin `2.0.0`; needed by imported uGUI samples and UI helpers. |
| UniTask | Required external host-project dependency for async-heavy modules; not listed in `package.json.dependencies`. |
| DOTween | Required external host-project dependency for tween-based runtime modules. |
| DOTween Pro | Optional for project-specific UI animation workflows; `NeoxiderPages` imports without it. |
| Mirror | Optional; required by `Neo.Network` multiplayer flows. |
| MobileHapticFeedback | Optional backend for the [Haptics](./Haptics/Haptics.md) module; richer device feedback requires installing it separately. |
| Character Movement Fundamentals | Bundled runtime/editor dependency under `ThirdParty/CharacterMovementFundamentals`; demo content is the optional **Character Movement Fundamentals** sample. |
| URP | Optional; project/render-pipeline dependent. |

See the [declared dependency baseline](./Dependencies.md#declared-dependency-baseline) for host pins, SDK-resolved versions and upgrade constraints, and [THIRD-PARTY-NOTICES.md](../THIRD-PARTY-NOTICES.md) for usage and license pointers.

## Validation host

Use the [EditMode/isolation guide](./Testing.md) and [PlayMode guide](./PlayModeTesting.md) for the selected suite's prerequisites. Broad test assemblies reference integrations such as Mirror even when a single runtime module has a solo path. UPM `testables` exposes package tests; it does not establish that every fixture supports an arbitrary installed-package layout.

Static-reset and Play Mode transition fixtures belong in an isolated test host with task-owned save and PlayerPrefs storage. They can affect providers, singleton state, time, and editor lifecycle. Record skipped capabilities separately from executed behavior.

## Policy

- Keep the UPM `unity` field in sync with the actually-supported floor; it was deliberately raised to `6000.0` for `9.8.0` — do not lower it without a corresponding compatibility pass.
- Keep optional third-party integrations guarded so offline/package-only projects still compile when the optional dependency is absent.
- The current release layout uses `Assets/Neoxider/Samples~`, matching the three `package.json.samples[].path` entries. During sample development the source root can be `Assets/Neoxider/Samples`; before UPM release restore `Samples~` and verify the matching package paths.
- After importing through Unity Package Manager, Unity copies samples into `Assets/Samples/NeoxiderTools/<version>/<sample name>/...`; validation supports that imported root as well.
- The `demo` preparation branch does not itself change sample layout or establish a published package ref.
- Update this page when `package.json`, `Packages/manifest.json`, or the documented install requirements change.

## IL2CPP

See [IL2CPP.md](./IL2CPP.md) for the reflection/code-stripping caveat affecting `NeoCondition`, `ComponentFloatBinding`, `[SaveField]`, `NetworkPropertySync`, and `NetworkReactiveSync`, and the bundled `link.xml` mitigation.
