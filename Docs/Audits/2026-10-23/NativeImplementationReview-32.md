# Native implementation review 32 — Parallax local geometry correction

Status: **COMPLETE — PARALLAX-SRC-01 source-closed; no additional confirmed bounded residual. All execution UNEXECUTED.** Same independent native `gpt-6.1-sol` / `max` session. Only this report was written; earlier reports remain immutable. No source/tests/public-doc/config/other-report edits, Unity/compiler/tests/syntax/checkers, children/services/process control or commits.

## Admission and exact coverage

Root confirms original Parallax correction process50108 exited/DONE0, with no original writer in fresh liveness before the separate primary-test-driver launch. Read complete current metadata: same original session `01a1676f-05d5-7213-a332-573e710fb89d`, requested/resolved `gpt-6.1-sol`/max, state done/exit0; its wrapper record names winpid74324, not a live-writer assertion. Current local liveness lists only exact Neo.Editor.Tests task b03-04/PID25388; Parallax b01-02 is absent. Its source-final receipt and hashes are stable at closing read `2026-10-23T22:48:34Z`. No process was controlled. Current shared gate remains sourceFreeze=false/executionAllowed=false/rootSourceWriterState=running; this is bounded source admission, not global execution authorization.

Personally read the complete independent132-line finding/report, latest62-line original receipt and36-line request; complete current737-line ParallaxLayer source and its full diff against ca198556; complete685-line prefixed regression including all old/new bodies/helpers/setup/teardown; both complete guides66/13 lines and their diffs. The same-session implementation contract remains the approved baseline. Historical audit148/plan52 fingerprints match the independent report; they are retained historical artifacts rather than claimed fresh whole-document reads in NI32. Legacy fixture/asmdefs/meta are identity-preserved context, not new executions. No unrelated writer, demo controller or other module is approved here.

## Source closure of PARALLAX-SRC-01

Original owner/task remains **Neo.Parallax / neo-audit-1023-b01-02**. Current `Assets/Neoxider/Scripts/Parallax/ParallaxLayer.cs` implements the accepted correction without a new public API or capacity setting:

- **Preallocation order:** Initialise247–321 first releases its old owned pool through Cleanup(false), resolves/snapshots its actual source, computes cell size, then checks enabled world stride, ComputeTileCounts and ComputeLocalGeometry. BuildTiles316 runs only after both succeed. Source is hidden508 and initialization published321 only after construction. Rejection leaves/restores the captured source state and permits public SetTargetCamera retry.
- **Pitch:** ComputeCellSize402–405 stores double local pitch using double division by absolute lossy scale. Disabled axes select literal0d without evaluating unused spacing/division. There is no stored float spacingLocal and therefore no disabled-axis zero-times-infinite-spacing expression.
- **Representability:** ComputeLocalGeometry460–486 requires positive enabled pitch within float range (NaN/infinity fail), computes the same integer anchor as before and first/last coordinates entirely in double, then bounds both endpoint magnitudes and finite Z. Positive pitch means every interior generated position lies within those endpoint bounds. Origins publish only after validation; count minima/product representation are already checked. No arbitrary practical cap is introduced.
- **Accepted construction:** BuildTiles497–502 evaluates origin + index × pitch in double, converts only final validated coordinates to Vector3 floats, then calls CreateTile. It does not cast the uncentered index × pitch first. Thus finite centered endpoints remain accepted even where an uncentered float intermediate would overflow.
- **Count/motion compatibility:** ComputeTileCounts408–457 retains widened padding, orthographic viewport/ceiling, fixed perspective fallback, individual count/product and finite stored world-period checks. Disabled axes request one/period0. Camera binding,15 serialized fields/defaults, MovedFrom, source transforms/render settings, multiplier direction and scaled auto-scroll remain. UpdateParallaxOffset670–680 and one whole-period wrap per tile/axis682–723 do not consume the new local pitch fields or add per-frame work. No repeated loop proportional to teleport distance or scene search is introduced.

For the exact accepted trigger (F=float.MaxValue, root X scale1/16, spacingF/8, unit sprite, orthographic size2/aspect1/default padding), count remains5 and world period remains finite, but local pitch is2F. The new pitch guard rejects **before CreateTile**. At scale1/8 local pitchF is finite but centered endpoints±2F fail the endpoint guard. At scale1/4 local pitchF/2 and final local coordinates[-F,-F/2,0,F/2,F] pass; each uncentered product and centering stay in double before final conversion. This is source arithmetic/control-flow evidence, not a native setter/log/rendering result.

Cleanup647–668 still removes only recorded generated tiles, restores the snapshotted renderer identity and original enabled state, and leaves authored children alone. Rebuild preserves current authoring transforms via Cleanup(false). Play cleanup deactivates old tiles before deferred destruction; Edit cleanup destroys immediately. Disable/destroy cancels the queued editor preview callback. These existing ownership mechanics were not replaced by transactional old-pool retention or a resource framework. Huge representable requests can still exhaust memory; that documented proposal was not made mandatory.

## All new regression bodies and ordinary controls

The existing prefixed fixture now has22 methods/41 expanded cases: all31 prior cases plus five methods/10 new cases. **Removing only new lines413–614 (202 lines, including the new readiness helper) reproduces exact prior fixture SHA256 `5e1a9191466392e2425d72f7c570c3a1b6d4649005cf397ca26f4252855c58c1`.** All prior setup/cleanup/17 methods/31 cases and helpers are byte-preserved, not just equivalent counts.

| New body | Authored observable acceptance, UNEXECUTED |
|---|---|
| LocalGeometry_RejectsUnrepresentablePitch_AndRecovers417–465 (4 cases) | Enabled/disabled source × cold/live pool. Actual live precondition requires8 active/enabled assigned-sprite renderers. Exact1/16/F/8 trigger produces zero generated renderers, exact source state and surviving authored child/parent/local position/activity. Repair to unit scale/zero spacing requires8 actual working renderers, native unit bounds/stride/anchor through AssertTilesMatchTemplate; disable removes pool/restores exact source and authored child. |
| LocalGeometry_RejectsUnrepresentableCenteredEndpoints_AndRecovers468–492 | Exact1/8/F/8 rejects despite finite pitch. Same object repairs to8 active assigned-sprite renderers with real unit geometry and normal source/disable cleanup. |
| LocalGeometry_RepresentableCenteredPositions_AvoidIntermediateOverflow495–525 | Exact1/4/F/8 must create5, not refuse everything. Sorts actual renderers and asserts explicit local vectors[-F,-F/2,0,F/2,F] and world vectors[-F/4,-F/8,0,F/8,F/4], all Y/Z0. Exact renderer state/sprite and finite local/world components are required; disable restores source. Expected coordinates are explicit constants, not the runtime formula copied into an oracle. |
| LocalGeometry_DisabledAxesIgnoreUnrepresentableUnusedPitch530–561 (3 cases) | Disabled X/Y spacing which formerly overflowed local float spacing cannot reject the valid other-axis8/single-tile1 request. Actual renderer state/sprite/all finite components and source-anchored disabled coordinate0 required, with cleanup/source restoration. |
| LocalGeometry_NegativeSpacing_PreservesOrdinaryOverlap564–591 | Spacing−0.5 produces12 actual unit-width renderers, explicit first−2.5/last3, half-unit adjacent stride/Y0 and ordinary cleanup. This guards positive-pitch validation from banning ordinary overlap. |

New AssertGeneratedTilesReady593–613 obtains actual child SpriteRenderers through the retained generated-hide-flags enumeration, checks exact nonempty count, active/enabled state, assigned sprite identity and every local/world component within finite float range. Recovery also calls the retained native renderer-bounds/stride/anchor helper638–660. These are public/native-object outcomes, not private count/pitch/initialized flags or a mocked allocation counter.

All retained bodies were also read: authored children; signed root and direct/nested source transforms; camera clear/rebind/replacement; disabled source/editor preview; null variants/stride rejection with repair; real tile-position teleport and stationary RNG; count/product/world-period/viewport rejection; disabled padding and live-pool recovery. The two old motion cases manually dispatch LateUpdate and therefore do not establish native player callback order. The31 old plus10 new source inventory is not discovered/executed41-pass evidence.

Complete setup creates only its own camera/layer/assets, configures via SerializedObject before activation and uses explicit public camera injection. Cleanup destroys owned layer hierarchy before camera/assets, reverses sprite/texture creation order, clears resources and restores exact captured Random.state. Disable/destroy cancels pending edit callback through runtime cleanup. No unsupported NonParallelizable, global input/time change, foreign service takeover, private geometry invocation or new fixture/framework is added.

## Guides, unchanged identities and limits

Both complete current guides/diffs correctly document preallocation count/world/local limits, double centering, disabled spacing, exact source restoration/retry, C# and Inspector paths and practical memory limits. README retains canonical component entry and actual Samples~/developer/imported paths; no demo runtime acceptance is inferred from those references. Public API and all15 serialized defaults remain, and original namespace migration attribute is retained. New double fields are private/nonserialized implementation state. No new package asset/GUID is added by this correction.

Legacy110-line fixture, existing regression metadata GUID `74fb57c7c53a473b9156d2c31f23dc09`, runtime asmdef and shared Edit asmdef are byte-identical to the independent report. Scoped tracked diff enumeration shows only the declared runtime/two guides; the already-added prefixed fixture was read directly in full, not omitted because Git treats it as untracked. Historical audit/plan/independent report unchanged. No undeclared source deletion or sample/config/settings/serialized migration found.

**Remaining central evidence, all UNEXECUTED:** frozen-source compilation/discovery/nonempty41-case plus legacy Parallax execution on contracted Unity hosts; actual native getters/setters at extreme finite positions, logs/rendering/sprite coverage and active/enabled state; ordinary live camera/auto-scroll behavior; Play deferred destruction, callback ordering, editor delay cancellation and stop/domain-reload cleanup. Signed nested-scale/rotation/shear/perspective coverage limits and large representable allocation risk remain documented. Source preflight is not an all-world-coordinate/renderer/platform numeric guarantee or aggregate performance result. No additional bounded correction is requested from this pass.

## Raw read fingerprints

SHA256 over file bytes, not normalized console text. Source/receipt/guide/fixture hashes stable at closing22:48:34Z; all named historical/context identities match the earlier independent report where described.

| Path | SHA256 |
|---|---|
| Docs/Audits/2026-10-23/IndependentImplementationReview-Parallax.md | `66373f8da178750579420f0fd2392be0f65d4823aaed9890d5e97943ed49b893` |
| Docs/Audits/2026-10-23/Neo.Parallax.implementation.md | `d574a7fdd0dfdc2d8fe42c5434488613fa5898ee63e101bebe9a44a84373efcb` |
| Docs/Audits/2026-10-23/Neo.Parallax.implementation-request.md | `e2774952e97e2d6e4bac13f43e51a40057959bf232d1e17208de533cff11d1f2` |
| Docs/Audits/2026-10-23/Neo.Parallax.md (historical identity) | `fa64c81c750bb9fd5dddc9cff4f02f4a4bc561068dc876622e072078f880894d` |
| Docs/Audits/2026-10-23/Neo.Parallax.plan.md (historical identity) | `00545bdf7e6c04f8951bf3aef855765f960a1be7adda22be31ac1e8c3bce9fca` |
| Assets/Neoxider/Scripts/Parallax/ParallaxLayer.cs | `cd3ee3ee2f9d7bd72c282f07549e0ea366a15a827e3a5b0dc8185961d3331e6d` |
| Assets/Neoxider/Tests/Edit/Parallax/Audit20261023NeoParallaxRegressionTests.cs | `588ad53a81a21c0c4a5a389a5d8c2d27aa0aacac7ec79aa80a325d966f2d8f4e` |
| Assets/Neoxider/Docs/Parallax/ParallaxLayer.md | `e536547ef5d99f6e10d718df53e10618edf0ce90e199530a358b27e2ee75b829` |
| Assets/Neoxider/Docs/Parallax/README.md | `32da81863e65f23fd31f2de65306b1413eae6f6c04543e32d6b576d37fa600a4` |
| Assets/Neoxider/Tests/Edit/Parallax/ParallaxLayerEditModeTests.cs (retained) | `57458c580478623d0bf16b91e46e46b6052c576c3b1f4d5282eb61a57a2226cc` |
| Assets/Neoxider/Tests/Edit/Parallax/Audit20261023NeoParallaxRegressionTests.cs.meta | `33f1d08de1121134f990348e8ad0094ed0fd4b79961fe40c3e984c886b761b8c` |
| Assets/Neoxider/Scripts/Parallax/Neo.Parallax.asmdef | `a4d1230ee68d0bcb5209c8b836022353b42339c03ad404fecac6ae668966e8b3` |
| Assets/Neoxider/Tests/Edit/Neo.Editor.Tests.asmdef | `204a5ba6382792131c48cae74f38e27f5431909c0c0866c3b2905be0f2c4b4a5` |
| Local/Audits/assembly-2026-10-23/agents/neo-audit-1023-b01-02.meta | `5e0548d63c3155c21101bf11f5a6c901a939739519c3709f5bfd942fcf923a50` |
| Local/Audits/assembly-2026-10-23/cli-liveness.json | `b279dab4845e4d3456f02baa7c51ed65ebaef023ba3641741df41610e4341789` |
