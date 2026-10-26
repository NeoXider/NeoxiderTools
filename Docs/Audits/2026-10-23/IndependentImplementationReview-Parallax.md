# Independent implementation review: Neo.Parallax

Status: **COMPLETE — one confirmed P2 source residual; scoped approval withheld pending the bounded correction below.**

Reviewer: `SOURCE_REVIEW_ID_neo-verify-1024-parallax`, an additional independent source reviewer, not the implementation owner. Target: `demo` / `11.0.0`. Scope: latest **PARALLAX-COUNT-OVERFLOW** phase, including preservation of the earlier coherent fixes. Review date: 2026-10-24 in the workspace timezone. This report is the reviewer's only filesystem mutation.

## Admission and current writer gate

The explicit independent-review assignment authorizes this review. Artifact contents and hashes identify the reviewed source; they do not authorize execution or transfer implementation ownership.

- Read the actual root `AGENTS.md` completely. Enumeration of the parent directories of the assigned runtime, test, documentation and report paths found no more-specific `AGENTS.md`.
- Read the complete `IMPLEMENTATION-CONTRACT.md`, implementation request, implementation receipt, original audit and original plan. The only assigned action is `PARALLAX-COUNT-OVERFLOW`, owned by **`neo-audit-1023-b01-02`**, original session **`01a1676f-05d5-7213-a332-573e710fb89d`**.
- Original-owner log lines 36–48 record continuation of that same session and its final `Source-final: PARALLAX-COUNT-OVERFLOW` response. Its metadata records `state=done`, `exit=0`, `winpid=57592`, requested/resolved model `gpt-6.1-sol`, effort `max`. The complete implementation receipt independently says source-final and identifies the actual changed files.
- Read the complete current `Local/Audits/assembly-2026-10-23/cli-liveness.json` before source admission and again at closure. It contains exactly three independent reviewers: Other PID 48608, Dialogue PID 66216, and this Parallax reviewer PID 34064. The exact original task/session and its recorded launch/recovery/final Windows PIDs 39296 / 28408 / 57592 are absent. This is the supplied liveness artifact's evidence, not a fresh operating-system process census.
- The shared implementation-state entry still says `source-running`; the manifest says `approved-awaiting-launch`, and the older implementation-progress snapshot says the receipt does not exist. These stale tracker values disagree with the completed original-owner log/metadata and the present receipt. They were not silently treated as completed global state or edited.
- Current global gate: **`globalReviewApproved=true`, `sourceFreeze=false`, `executionAllowed=false`, `rootSourceWriterState=running`**. The original Parallax owner is absent from current recorded liveness, so this bounded source review can close. Product/compiler/test execution remains **BARRED**.

Any correction accepted by root must return to the **same original owner**. This reviewer has not changed implementation, tests, guides, trackers, or historical reports.

## Confirmed residual: PARALLAX-SRC-01 / P2

**Finite world-period admission can still allocate tiles with non-finite local positions.**

Exact current source: `Assets/Neoxider/Scripts/Parallax/ParallaxLayer.cs`:

- Lines **399–401** calculate and store `spacingLocal` using `float` division.
- Lines **294–310**, **404–453** validate enabled world strides, requested counts/product and world periods, but do not validate the local geometry consumed by construction.
- Lines **461–475** calculate the anchor/origin and tile coordinates using that local spacing. `CreateTile()` is called at **470**, before the coordinate is assigned at **475**.
- Line **481** then disables the source renderer, and lines **314–317** publish successful initialization.

**Public authoring trigger.** Use the existing Inspector fields and `SetTargetCamera(Camera)`, with an axis-aligned root source, a unit-size sprite, layer position zero, root scale `(0.0625f, 1f, 1f)`, orthographic camera size 2/aspect 1, horizontal tiling enabled, vertical tiling disabled, default padding `(1,1)`, and finite horizontal spacing **`float.MaxValue / 8f`**. This uses five tiles, not a large pool. No rotation, shear, unsupported camera coverage, negative padding or disabled-axis policy is involved.

**Consequence established by source arithmetic, without execution.** Let `F = float.MaxValue`:

1. Horizontal world stride rounds to `F/8`; it is finite and positive. `Ceiling(4 / stride)` is 1, so the requested horizontal count is 5 and the product is 5.
2. The widened world period is approximately `5F/8`, below `F`. Every current count/period guard succeeds.
3. `spacingLocal.x = (F/8) / (1/16) = 2F`, which cannot be stored in its `float` field and becomes positive infinity. With anchor 2, the stored origin is negative infinity. The first X coordinate includes `0 * positive-infinity`; subsequent coordinates add negative and positive infinity. Non-finite values therefore reach `Transform.localPosition` setters **after GameObjects/renderers have been allocated**.
4. No source-level failure path follows these setters: the component proceeds to hide the source and mark the pool initialized. Unity's exact native reaction to each invalid setter remains unexecuted; this review does not invent a particular logged message or rendered result.

This is a demonstrated numeric admission gap in the current construction path, not merely missing native evidence. It predates the latest count changes and remains reachable after them. The count/product arithmetic fix itself is coherent, but it does not complete rejection of numerically unrepresentable generated geometry before allocation.

**Smallest bounded correction for root to return to the same owner.** Extend the existing pre-allocation checks in this component to cover the local pitch, centered origin and extreme final local coordinates that `BuildTiles` will store. Use widened intermediate arithmetic and reject non-finite or out-of-`float` final coordinates before any `CreateTile()`. Form accepted per-tile coordinates with the same widened arithmetic, converting only validated final values: a finite final coordinate can otherwise overflow an intermediate `x * localPitch` in the current expression. Keep existing cleanup/source restoration, public API, serialized fields, axis/padding policy and normal movement. No capacity setting, new service, transactional old-pool framework or shared configuration is needed.

**Meaningful acceptance to author, all UNEXECUTED here:**

- Negative: the scale `1/16`, spacing `F/8`, five-tile request above must produce zero generated renderers and preserve the source's original enabled state. Repeat after an ordinary live pool for both initially enabled and initially disabled sources; the former generated pool must be released under the existing rebuild policy, and an authored child must survive.
- Recovery: repair to unit scale/zero spacing and call public `SetTargetCamera`. Require exactly eight actual generated renderers, enabled/active with the assigned sprite and expected ordinary unit stride; disabling the component must remove the pool and restore the exact source state. These observations prevent an unconditional-rejection implementation from passing.
- Representability positive control: scale `1/4`, spacing `F/8`, same camera/padding requests five tiles whose final local X coordinates are representable: `-F`, `-F/2`, `0`, `F/2`, `F`. Verify those explicit positions on the actual generated renderers, their finite world positions, sprite identity and enabled state. This checks that widening avoids intermediate overflow without imposing a practical capacity cap or rejecting representable geometry.
- Preserve ordinary eight-tile defaults, one-tile disabled axes, ignored extreme padding only on disabled axes, both camera modes' existing fixed/count formulas, ordinary negative-spacing overlap, source ownership and camera movement/recycling regressions.

## Findings on the assigned implementation

Apart from PARALLAX-SRC-01, no additional confirmed residual was found in this scope.

- **Padding/count/product:** `Math.Max(0, padding) * 2d` widens before multiplication. Orthographic height is widened before doubling, width and ceiling arithmetic stay in `double`, and enabled camera dimensions must be positive and finite. Counts are either at least three/six/four or exactly one, so the upper-bound guards suffice before the `int` casts. Product validation occurs before object construction. The product comparison cannot round an integer just above `Int32.MaxValue` down to that bound at the relevant magnitude.
- **World periods:** enabled strides are finite and greater than `0.0001f` before count division. Period multiplication stays in `double` and is checked before storing `float`. Counts and periods publish only after all these checks succeed. The residual above concerns the separate local coordinates consumed afterward.
- **Enabled versus disabled axes:** disabled axes request one tile and a zero wrap period; extreme padding on them does not reject an otherwise valid request. Enabled dimensions receive the stride/viewport/period checks. This documented policy is preserved, not reopened.
- **Old pool and source ownership:** `Initialise` first calls `Cleanup(false)`. Cleanup removes only recorded generated tiles, restores the snapshotted renderer identity's original enabled state, and clears initialization/snapshot state. Rejected construction does not hide the newly snapshotted source. Retry cleans that snapshot before taking a fresh one. Initially disabled sources remain disabled after rejection/disable. Play Mode retirement explicitly deactivates generated objects before deferred destruction; actual native timing is still evidence debt.
- **Ordinary compatibility:** the full runtime diff preserves public `TargetCamera`/`SetTargetCamera`, all 15 serialized fields/defaults and `MovedFrom`. Camera-delta direction and scaled-time auto-scroll are unchanged. Double viewport calculations may change a count at a former float-rounding boundary, as the receipt explicitly discloses. Existing perspective fallback counts, explicit rebuild requirements and transform limitations remain documented.
- **Guides:** both complete changed guides correctly describe the API/Inspector path, released-old-pool/source-restoration policy, retry, numerical bounds and absence of a practical memory budget. README sample references point to the present release `Samples~` paths and explain developer/imported layouts. The guides do not claim a measured aggregate performance result.

## Complete regression-source review

Read all **483 lines**, including setup, every oracle, all helpers and teardown, of `Assets/Neoxider/Tests/Edit/Parallax/Audit20261023NeoParallaxRegressionTests.cs`. It contains **17 methods / 31 parameter-expanded cases**, not 31 executed passes.

| Source cases personally read | Cases | Oracle assessed |
| --- | ---: | --- |
| Authored children; root transforms; direct/nested child transforms | 1 + 3 + 2 | Real renderer bounds, world positions, adjacent stride, anchor and hierarchy survival |
| Clearing camera; disabled source; replacing source; preview disabled; null sprites | 1 + 1 + 1 + 1 + 1 | Public binding/source state and actual generated-renderer presence/absence |
| Invalid enabled stride and repair | 5 | Zero pool/source retained, followed by nonempty actual pool after repair |
| Teleport; stationary randomization | 1 + 1 | Actual tile positions/count and unchanged sprite/RNG state after settling |
| Excessive axis counts; excessive product | 4 + 2 | Rejection followed by explicit 8/6 or 64/48 renderer-count positive controls |
| Huge viewport; excessive world period | 1 + 1 | Zero renderers/source retained, followed by eight-renderer recovery |
| Extreme padding on disabled axes | 3 | Explicit eight-renderer or single-renderer successful construction |
| Rejection after a live pool | 2 | Existing eight-renderer pool removed, exact enabled/disabled source restored, eight-renderer retry and final restoration |

`GeneratedTiles()` enumerates actual `SpriteRenderer` components, including inactive children, and selects the generated hide flags. It does not read private count fields or reimplement count arithmetic. Fixed expected counts supply real positive controls against unconditional rejection. The earlier geometry cases additionally inspect actual renderer bounds/stride; the new recovery cases inspect renderer counts and source state, not full recovered geometry, so they do not cover PARALLAX-SRC-01.

`ActivateLayer` activates the object and calls the public camera setter. Inspector authoring goes through `SerializedObject`; the two motion cases manually dispatch the existing `LateUpdate` message. No private count invocation or reflection is used by this prefixed fixture. Those manual messages do not prove actual player callback order or deferred destruction.

Cleanup was read in full: destroy the owned layer/hierarchy before the camera, destroy owned sprites/textures in reverse creation order, clear their list, and restore the captured exact `Random.state`. Authored decorations/replacement/nested sources belong to the test layer hierarchy. Disable/destroy cancels the component's pending editor delay callback. The fixture contains no unsupported `NonParallelizable` attribute. Cleanup behavior and scheduling remain source observations requiring native execution under the exclusive runner contract.

## Exact read coverage and exclusions

Complete bodies personally read:

- Actual root `AGENTS.md` (121 lines), implementation contract (65), implementation request (32), implementation receipt (48), historical audit (148), and historical plan (52). Historical counts/statuses were treated as historical; the original audit/plan were preserved.
- Entire current `ParallaxLayer.cs` (710 lines), including Editor/player conditionals, all renderer copying/sprite fitting, lifecycle, snapshot cleanup, layout, camera binding, motion and wrapping. Read the complete current runtime diff against original audit base `ca198556`.
- Entire prefixed regression fixture (483 lines), both changed guides (65 and 13 lines), and complete guide diffs against `ca198556`. The prefixed fixture is currently untracked, so it was read directly in full rather than relying on `git diff` discovery.
- Context only: entire legacy `ParallaxLayerEditModeTests.cs`, runtime/test asmdefs and prefixed fixture `.meta`; original-owner log (48 lines) and metadata; full liveness JSON; exact Parallax manifest/state/progress/reviewer records and current global gate. NativeReview-B sections B-10 (lines 97–99) and Neo.Parallax (231–235) were read directly in addition to their request excerpts.

No claim of complete review of the other owners' JSON records, the rest of NativeReview-B, or other modules. Existing legacy fixtures and shared test internals remain owned by `Neo.Editor.Tests`; diagnostics/version branches by their original owners; demo scene/controller by the sample owner; package/version/changelog/migration and shared state by root. Demo/controller paths were enumerated for the README reference, **not** reviewed as changed bodies or executed. No demo acceptance or whole-library approval is asserted.

No runtime/test/guide/configuration/trackers/meta edits, source validators, service/process control, installs, commits, pushes, messages to other actors or child agents were performed. Review used source/diff/reference reads and raw hash bookkeeping only; the named report is the sole write.

## Raw SHA-256 evidence

Hashes are over the actual file bytes, including existing BOM/newline choices, not normalized console text. Source and receipt entries were read repeatedly and remained identical at the closing source read **2026-10-23T21:49:38.415715+00:00**. Execution gate/liveness were also reread unchanged at that point. A final post-write raw hash comparison is recorded below.

| File | Raw SHA-256 |
| --- | --- |
| `AGENTS.md` | `3a0c5c7a70719cba5d8d9c73b2ca162d7fb0ce96efd60ef926edbebc82bd9384` |
| `Docs/Audits/2026-10-23/IMPLEMENTATION-CONTRACT.md` | `d5bc7a2d934e20f40647c45d53746c3734a1dbbfe95f182600d66977e9bbcbab` |
| `Docs/Audits/2026-10-23/Neo.Parallax.implementation-request.md` | `56073e1edfbc852d44e7f448e179db48d0cdfc466233027f501fd517ae15a36d` |
| `Docs/Audits/2026-10-23/Neo.Parallax.implementation.md` | `710ff0dfb267c2ac4b7da95431333449e134a3d7932bb8fa93c11747f953a758` |
| `Docs/Audits/2026-10-23/Neo.Parallax.md` | `fa64c81c750bb9fd5dddc9cff4f02f4a4bc561068dc876622e072078f880894d` |
| `Docs/Audits/2026-10-23/Neo.Parallax.plan.md` | `00545bdf7e6c04f8951bf3aef855765f960a1be7adda22be31ac1e8c3bce9fca` |
| `Assets/Neoxider/Scripts/Parallax/ParallaxLayer.cs` | `2131436914677879e101c26b8dbad08dc58e61c1a230d48fcb25121fee07ebf2` |
| `Assets/Neoxider/Tests/Edit/Parallax/Audit20261023NeoParallaxRegressionTests.cs` | `5e1a9191466392e2425d72f7c570c3a1b6d4649005cf397ca26f4252855c58c1` |
| `Assets/Neoxider/Docs/Parallax/ParallaxLayer.md` | `fd5b9fc82123da550d0b09552a8c2e7ecdd3e53587bb6095ae3f3b585cac53e4` |
| `Assets/Neoxider/Docs/Parallax/README.md` | `0f9a18b34e0ec84212b15818aa932ae05148b11dc63c40d48e3bb8bffd963e74` |
| `Local/Audits/assembly-2026-10-23/cli-liveness.json` | `c955a2a09fe66810a83ce86cfa784fa65a66700ae2b33e9a2956a58be00b1918` |
| `Local/Audits/assembly-2026-10-23/agents/neo-audit-1023-b01-02.log` | `6525f0e759cbb5dee4dbbbd8948bdda83236cca71783de59ea5bad5ec2aa1afb` |
| `Local/Audits/assembly-2026-10-23/agents/neo-audit-1023-b01-02.meta` | `1af355edf1c90c0d3f9a40a95daba5240925b6231b06f0f73ea442d2186606c2` |
| `Docs/Audits/2026-10-23/implementation-state.json` | `36f349bbe37843f05ab32a3d0a06a1308fcef34a1a8551a9b7b12c6516e92fde` |
| `Docs/Audits/2026-10-23/manifest.json` | `b0976599ce7d259c46efbda407819edfd1703eabf9de37f6f2fd03a176fb0539` |
| `Local/Audits/assembly-2026-10-23/source-reviewers.json` | `cda6d2d04fdc6db62d70e3174346afb27379be13e7509d91ddff555b20d713dd` |
| `Docs/Audits/2026-10-23/NativeReview-B.md` | `3d81b201d8fc56e890c557ecf0c1848c4d216be087c79c907712a84dd45b111a` |
| `Assets/Neoxider/Tests/Edit/Parallax/ParallaxLayerEditModeTests.cs` | `57458c580478623d0bf16b91e46e46b6052c576c3b1f4d5282eb61a57a2226cc` |
| `Assets/Neoxider/Scripts/Parallax/Neo.Parallax.asmdef` | `a4d1230ee68d0bcb5209c8b836022353b42339c03ad404fecac6ae668966e8b3` |
| `Assets/Neoxider/Tests/Edit/Neo.Editor.Tests.asmdef` | `204a5ba6382792131c48cae74f38e27f5431909c0c0866c3b2905be0f2c4b4a5` |
| `Assets/Neoxider/Tests/Edit/Parallax/Audit20261023NeoParallaxRegressionTests.cs.meta` | `33f1d08de1121134f990348e8ad0094ed0fd4b79961fe40c3e984c886b761b8c` |

Closing post-write bookkeeping at **2026-10-23T21:52:46.885788+00:00** compared all **21** listed raw file hashes: **zero changes**. The exact original owner/session/PIDs remain absent from the unchanged liveness artifact. The writer gate remains `sourceFreeze=false`, `executionAllowed=false`, `rootSourceWriterState=running`. This is hash/liveness bookkeeping, not product validation.

## Execution and remaining evidence debt

**ALL product/compiler/test/syntax/product-validator execution: UNEXECUTED.** Unity/MCP, Editor/player compilation, dotnet, test discovery/runs, Play Mode, builds, screenshots, demo rendering and performance measurement were not run. No authored case is reported as passed. Raw UTF-8 reads, read-only Git diffs/reference enumeration and SHA-256 bookkeeping are the performed review activities.

After all writers finish and root actually opens execution on a frozen snapshot, native acceptance must include the complete nonempty prefixed fixture (currently 31 cases, plus any owner-authored correction cases), existing Parallax tests and applicable Unity 6.3/6.6 Editor/player/version branches. Source inspection cannot certify native property-setter behavior, real callback ordering, deferred destruction, Inspector delay cancellation across stop/domain reload, scaled-time scrolling, material/sorting appearance or actual sprite coverage. Documented perspective, rotated/sheared hierarchy and signed nested-scale limitations remain accepted limitations/evidence debt, not newly demonstrated bugs. No aggregate performance result is inferred.

The review is complete. The single source residual is separately actionable; the execution debt remains pending under the existing writer gate.
