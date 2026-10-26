# README and documentation entry review

Date: 2026-10-23. Baseline: `ca198556` (`origin/main` at dispatch), package `10.19.0`.

## Scope

Documentation-only review of the repository/package entry points and the onboarding pages they use. No runtime, editor, sample, scene, prefab, package metadata, or project settings were changed by this worker. Existing SVG hero, layers, module-shape, and four-ways sections were retained. The HTML/CSS → UI Toolkit section retains its feature scope and dated verification claims.

## Findings and fixes

| Finding | Evidence | Change |
|---------|----------|--------|
| The first-scene instructions named a nonexistent `--System--.prefab`. | The tracked asset is `Assets/Neoxider/Prefabs/-System--.prefab`. | Corrected the path and explained when a manager bootstrap is useful. |
| The entry points treated scene/singleton setup as the primary programmer workflow. | `ResourcePoolModel` / `ResourcePoolEntry` are ordinary C# types without `UnityEngine`; `MergeResolver` and `StateMachine<TState>` also expose APIs without a scene wrapper. | Added separate runtime, component, and NoCode entry routes and a complete, explicitly typed resource-model example. |
| Dependency guidance disagreed with package contents. | `package.json` lists AI Navigation, Input System, and uGUI, not a standalone TextMeshPro dependency. `ThirdParty/CharacterMovementFundamentals` is bundled; DOTween and UniTask are external requirements. | Corrected dependencies and bundled-code wording, added DOTween setup to Getting Started, and linked the optional haptics backend. |
| Version/sample lists had drifted. | Package version is `10.19.0`; `samples` contains Demo Scenes, NeoxiderPages, and Character Movement Fundamentals. | Updated the docs index and sample lists, distinguished hidden source/development/imported roots, and supplied current imported paths. |
| The audio tutorial used the old single-clip entry shape and legacy keyboard input. | `SoundEntry` exposes an ID and clips array; `AM.Play(string)` is public. | Updated authoring to named sound IDs, demonstrated an explicit AM reference, and used a public playback method callable by code or a UI event. |
| Module navigation bypassed canonical entries and omitted important areas. | Audio/Bonus have README entries; Inventory and HTML → UITK have module guides. | Routed Audio/Bonus to their READMEs and added Inventory, HTML → UITK, and the movement sample to the docs index. |
| Compatibility prose asserted package-wide future-version conclusions from an old source review. | Current modules include runtime UI Toolkit and additional assembly version defines; this worker did not run Unity. | Replaced blanket claims with concrete integration facts and the checks required for each target compiler/player. Kept the dated HTML → UITK evidence separately scoped. |
| Package README visuals and repository-only CLI/report links escaped the installed UPM directory. | `Images/readme`, `Tools/HtmlToUitk`, and repository audit/QA docs are outside `Assets/Neoxider`. | Preserved the visuals and section while using repository URLs for those external assets/pages. Package-local module links remain relative. |

## Substantial improvement proposal

Extend the existing package-integrity workflow with a documentation contract derived from `package.json`: validate README version badges, required dependency names, all sample display names/source paths, the imported-version examples, and canonical module links. Validate both the repository root and the installed-package boundary, so a link that exists only outside the UPM payload must use an explicit repository URL. This would catch the version, sample, dependency, and out-of-package navigation drift found together in this review.

The new workflow routes establish an API-first onboarding path now. A useful next step is to compile selected canonical README snippets through the existing EditMode test assemblies and publish a small per-module matrix of runtime API, component wrapper, NoCode entry, and sample. This can grow inside the existing `Docs` tree; a duplicate `DocsEn` tree would create another source of drift. Neither proposed automation was implemented in this documentation-only scope.

## Changed files

- [Repository README](../../../README.md)
- [Package README](../../../Assets/Neoxider/README.md)
- [Docs index](../../../Assets/Neoxider/Docs/README.md)
- [Getting Started](../../../Assets/Neoxider/Docs/GettingStarted.md)
- [Package Compatibility](../../../Assets/Neoxider/Docs/PackageCompatibility.md)
- [Sample Scenes](../../../Assets/Neoxider/Docs/Samples.md)
- This report.

## Validation

- Compared names, requirements, sample paths, example API signatures, version helpers, and relevant menu labels against package metadata and source files.
- Scoped `git diff --check` completed successfully for the six edited package/repository documentation files.
- A separate read-only text check, including this new report, passed trailing-whitespace and balanced-code-fence checks for all seven scoped files.
- Scoped Python documentation check passed: seven files decoded as strict UTF-8 with no replacement characters; all 228 local targets/fragments and nine repository URL targets mapped to local files resolved. Twenty-five other external URL occurrences were counted but not HTTP-tested.
- Metadata/sample consistency passed: current version in all six entry/guide files, all three declared sample names in the four onboarding/sample lists, all declared sample source directories present, and the three documented example scenes plus the bootstrap prefab present.

## Remaining limits

No Unity editor, player build, package installation, or EditMode/PlayMode execution was started. Source/API checks do not establish runtime behavior or a Unity-version compatibility matrix. External URLs were not HTTP-tested; package README SVG rendering requires access to GitHub's raw-content host. Historical HTML → UITK results remain historical evidence. Existing user-owned `Assets/Neoxider/DocsEn.meta` was not changed, and no new package file or `.meta` was introduced.
