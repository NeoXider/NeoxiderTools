# Velora CTA framebuffer — source phase, 2026-11-02

**Current operator result:** corrected graphical Unity 6.3 run r3 is **2/2 PASS**, followed by the full affected fixture **16/16 PASS**, zero skips, exit0. The preserved depth-zero r1/r2 remain failed. In this profile the framebuffer correction restores the blue CTA and readable label without product changes or weaker assertions. The source-phase sections below describe the pre-execution handoff; the operator receipt is appended at the end.

## Approved source change

Owned source: `Assets/HtmlToUitkShowcase/Tests/PlayMode/VeloraShowcaseInteractionTests.cs`.

- Both initial and resized targets now request depth **24**, replacing depth zero. The same constructor/color-format defaults remain; no allocator abstraction is needed for these two sites.
- Before CTA readback, assert the target is actually created, its actual depth/stencil format contains depth (`GraphicsFormatUtility.GetDepthBits > 0`), and it supports stencil (`GraphicsFormatUtility.IsStencilFormat`). These APIs exist in exact Unity 6000.3.14f1; no nonexistent `GetStencilBits` call is introduced.
- Diagnostic output adds actual target `depth`, `depthStencilFormat`, and panel `clearDepthStencil`. The panel's default clearing and `depthClearValue` remain untouched.
- Existing optional `NEO_VELORA_CAPTURE_DIR` capture, PNG/hash logging, texture cleanup, index math, scheduling, all original paint/contrast/glyph/pointer/modal assertions, and all 16 UnityTest methods are preserved. No product assets, styles, geometry, controller, dependencies, assembly definitions, or shared configuration changed.

Source-final SHA256: `bdb01e74490b7442b326a1ab46f115a0559e22faf405e58981ed9df862196be6`.

Reversing only this phase's import/comment, two depth arguments, prerequisite assertions, and diagnostic fields reproduces the executed r2 source SHA256 exactly: `19c360a07b0eee2482ee5749d9cd33add2f494b76e24545f92817c5d478736e1`.

## Source support and remaining uncertainty

Unity 6000.3.14f1 documents that UI Toolkit masking uses depth-test failure for stencil push/pop; incorrect buffer conditions can expose mask geometry or hide normal meshes. Its `clearDepthStencil` default is true. This supports supplying a real depth/stencil framebuffer while keeping default clearing. It does not prove this specific failure's cause before rerunning. [Version-matched PanelSettings source](https://github.com/Unity-Technologies/UnityCsReference/blob/6000.3.14f1/Modules/UIElements/Core/GameObjects/PanelSettings.cs#L537), [official clearDepthStencil documentation](https://docs.unity.com/en-us/engine/6000.3/script-reference/unityengine/uielements/panelsettings/cleardepthstencil).

The exact public API declarations were inspected in [GraphicsFormatUtility.bindings.cs](https://github.com/Unity-Technologies/UnityCsReference/blob/6000.3.14f1/Runtime/Export/Graphics/GraphicsFormatUtility.bindings.cs#L65). Existing depth-zero capture recipes are not evidence of correct stencil setup; only the appropriate built-in framebuffer requirement is reused. No external helper was run or copied.

## Preserved native evidence

The original r1 baseline used source SHA256 `5a90b705abc5bf75b9d99be9e5270453a1ea114567a39c004e8a3a736f08c4cb`; its two background failures remain recorded in [r1 results.xml](/D:/Temp/Neoxider-Audit-20261101/native-20261102/play-cta-u63-r1/results.xml).

Root's capture-enabled r2 used source `19c360...`, Unity **6000.3.14f1 / D3D11 / Linear**, PID **50676**, natural exit **2**. Both target and readback were `R8G8B8A8_SRGB`. The two tests each reported background **0/8664**, raw/linear/gamma/mirrored **0/0/0/0**, and diagnostic glyph count **5244**. Background assertions failed before glyph assertions and pointer/modal checks; those later checks did not execute.

Inspected [r2 launch.json](/D:/Temp/Neoxider-Audit-20261101/native-20261102/play-cta-u63-r2/launch.json), [results.xml](/D:/Temp/Neoxider-Audit-20261101/native-20261102/play-cta-u63-r2/results.xml), [editor.log](/D:/Temp/Neoxider-Audit-20261101/native-20261102/play-cta-u63-r2/editor.log), [result-summary.json](/D:/Temp/Neoxider-Audit-20261101/native-20261102/play-cta-u63-r2/result-summary.json), and preference-restoration receipt. The three raw receipt hashes match the summary; it records all **332** frozen inputs and **12** protected paths unchanged. The restoration receipt reports exact original preference names/kinds/data restored.

Worker independently viewed both exact target PNGs and verified their byte counts and hashes against the task and capture log:

| Capture | Bytes | SHA256 |
| --- | --- | --- |
| [Desktop 1280×900](/D:/Temp/Neoxider-Audit-20261101/native-20261102/play-cta-u63-r2/readback/velora-cta-readback-1280x900.png) | 239393 | `8eb4a42380b801f60d341a679d234bf35885a1cb0b327b75bebebba376af1ef5` |
| [Mobile 390×844](/D:/Temp/Neoxider-Audit-20261101/native-20261102/play-cta-u63-r2/readback/velora-cta-readback-390x844.png) | 116616 | `84e443099e42624056f4cc71efb1e3b65f63da78615e3eff0e88cf0c9116c7a5` |

Both images show the white rounded CTA capsule without a readable label, despite resolved blue. This establishes a rendering problem in the sampled target; it does not establish a normal Game View/product defect. The sampled frame is opaque and nonuniform. A high white-pixel diagnostic count does not prove glyph readability. The earlier historical 14/16 result and separate Game View observation remain historical evidence, not substitute target captures.

## Next root checks and worker limits

1. Root and the same independent native/source reviewer review the bounded diff, then freeze the new inputs. Root has owner authorization for hidden graphical batch while the foreign Editor stays open; worker remains source-only.
2. Run the exact same desktop/mobile two-test filter as r2, saving r3 launch/XML/log and fresh outside-project `NEO_VELORA_CAPTURE_DIR` output. Inspect actual created depth/stencil format and default clearing in diagnostics; verify and compare both full-frame PNGs with preserved depth-zero r2. Do not change formats, timing, thresholds, or product visuals during this comparison.
3. If both tests pass, root runs the full affected **16-test PlayMode fixture**, since both shared allocation sites changed. If evidence instead requires product changes, obtain the appropriate ownership before editing them.

Worker verification: static scope/restoration-hash review, 16-test method count, UTF-8 and whitespace checks passed. Existing diagnostic indices remain unchanged. Tests authored: prerequisite assertions in the existing CTA tests only. **No worker compiler, tests, Editor/Player, build, Unity MCP, or process controls were run.** Native r3 and the full fixture are pending. Physical input, ordinary Game View, video, visual parity, and confirmed causation remain unverified. No persistent worker process was started or left running.

## Root/operator execution after source acceptance

Root ran r3 and the full fixture using the accepted `bdb01e...` source: Unity 6000.3.14f1, D3D11, Linear, hidden graphical batch. Both natural exits were zero. R3 executed exactly two cases, both Passed; the subsequent full fixture executed all 16, all Passed, with no skips. Requested depth24 resolved to actual depth32 / `D32_SFloat_S8_UInt`, `clearDepthStencil=True`. Color/readback stayed `R8G8B8A8_SRGB`. Desktop background matches are 7708/8664 with 624 glyph pixels; mobile 7707/8664 with 630 glyph pixels. The synthetic pointer/modal assertions after paint also completed.

Root viewed both exact r3 PNGs: blue CTA and readable white text replaced the white mask capsule seen in r2. The unchanged thresholds/index/timing/styles and independently reviewed framebuffer-only correction identify the missing depth/stencil prerequisite in this sampled-panel setup. This does not establish a normal Game View/product styling defect. Both runs retained all332 frozen inputs and12 protected paths, and original HTML preferences were restored; private values are excluded. Raw results, prelaunch identities and full-frame PNGs are in [HtmlCtaStencil evidence](Evidence/HtmlCtaStencil-20261102.zip). Physical input, ordinary Game View/camera, real-time motion/video, browser parity and other graphics/platform profiles remain open.
