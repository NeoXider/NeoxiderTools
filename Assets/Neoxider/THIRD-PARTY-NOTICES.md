# Third-Party Notices

NeoxiderTools does **not** bundle any of the packages below — none of their code ships inside this package. They are optional or required *dependencies* you install yourself (via Package Manager / Asset Store) because specific NeoxiderTools modules call into their public API. This file lists what's referenced, why, and where to get the license terms for each.

| Package | Used by | Required? | License | Source |
|---------|---------|-----------|---------|--------|
| [UniTask](https://github.com/Cysharp/UniTask) | Async-heavy modules (Cards, Dialogue, Timers, animation await flows) | Required | MIT | Install via UPM Git URL — see package README |
| [DOTween](https://dotween.demigiant.com/) (HOTween v2) | UI/gameplay tweening used across runtime modules | Required | Custom (free) — DOTween's own license, see the Asset Store listing / [dotween.demigiant.com](https://dotween.demigiant.com/) | Unity Asset Store |
| [DOTween Pro](https://assetstore.unity.com/packages/tools/visual-scripting/dotween-pro-32416) | Optional, project-specific UI animation workflows (NeoxiderPages sample) | Optional | Commercial (Asset Store) | Unity Asset Store |
| [Mirror Networking](https://github.com/MirrorNetworking/Mirror) | `Neo.Network` — all multiplayer NoCode bridges. Every affected script compiles and runs solo without it (`#if MIRROR`) | Optional | MIT | Unity Asset Store / GitHub |
| [Spine Unity Runtime](http://esotericsoftware.com/spine-unity-download) | Optional Spine skeletal-animation integrations | Optional | Spine Runtimes License (separate Spine Editor license required for use) | esotericsoftware.com |
| [Odin Inspector](https://odininspector.com/) | Optional — package components work fully without it; if present, some inspectors adapt to it | Optional | Commercial (Asset Store) | Unity Asset Store |
| [Markdown Renderer](https://github.com/NeoXider/MarkdownRenderer) | Optional — richer Markdown preview for `[NeoDoc]` documentation links in the Inspector | Optional | See the linked repository | GitHub |

## Why this matters

Unity's own packages referenced automatically via UPM (`com.unity.textmeshpro`, `com.unity.ai.navigation`, `com.unity.inputsystem`, `com.unity.ugui`) are covered by the standard Unity Companion License / Unity package terms and are not listed above.

If you redistribute a build made with NeoxiderTools, you are responsible for complying with the license of whichever third-party packages you actually installed and used — this file is a pointer, not a substitute for reading those licenses yourself.

See [Requirements](./README.md#requirements) for which of these are required vs. optional for a given module.
