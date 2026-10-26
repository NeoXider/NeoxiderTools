# Neo.Extensions: approved implementation request

Original actor: `/root/audit_extensions`. Target `demo` / `11.0.0`. Root has read your complete audit and plan.

Read `IMPLEMENTATION-CONTRACT.md` first. Do not execute tests/build/validators during this phase.

Documentation and your original fixture RNG isolation ONLY. Numeric extra owns8 runtime files, Unity extra owns23; do not overlap runtime edits. Integrate both supplemental guides and existing preview/coroutine/depth pages.

## Assigned review actions

### EXTENSIONS-DOC-LINKS (P2; approved-source-correction)

Link NumericEdgeCases.md and LifecycleAndOwnership.md from canonical entry; reconcile consumer depth migration and raw native stop boundary.

### INPUT-POINTER-API (P2; approved-source-correction)

UIUtils directly reads legacy Input; add coordinate overload/caller buffer and upper Input adapter if New-only failure confirmed, preserving legacy APIs and dependency direction.

## Relevant full native sections

Source: NativeReview-A.md

### A-09 — P3: primary Extensions layout regression leaks Unity RNG state

Owner: `Neo.Extensions`, original native task `/root/audit_extensions`. Current source: `Assets/Neoxider/Tests/Edit/Extensions/Audit20261023ExtensionsTests.cs`, `Layout_OneShotSource_IsEnumeratedOnce`, `sphere` case. It calls `LayoutUtils.GetSphereSurface`, whose implementation consumes `UnityEngine.Random.onUnitSphere` at `Assets/Neoxider/Scripts/Extensions/LayoutUtils.cs:140`. The fixture creates/destroys its objects and deletes its PlayerPrefs key, but never snapshots/restores `UnityEngine.Random.state`.

Expected: a regression fixture leaves the prior shared RNG state intact. Actual: running the sphere case changes subsequent global random draws, making unrelated tests depend on execution order. Smallest repair: snapshot RNG state in setup and restore it in teardown, including failed cases, as the Numeric fixture already does. This is a source-level isolation defect; no order-dependent runtime failure was executed or claimed.
Source: NativeReview-A.md

### Neo.Extensions — source review complete with supplemental scopes

Original owner: `/root/audit_extensions`. Full reads:

- `Docs/Audits/2026-10-23/Neo.Extensions.md` — `58a02121d629f5aba4d22625f36b773f5a2ab7db9d513e4cb7edd93726f0655f`.
- `Docs/Audits/2026-10-23/Neo.Extensions.plan.md` — `4f4ffeb4c337b9bf9fe3867cbb695cd6b3d71faf171c51d133aa4795a1d28bda`.
- `Assets/Neoxider/Tests/Edit/Extensions/Audit20261023ExtensionsTests.cs` — `824a02760362c1cd2c14d8b8a308eab15a7b4b9a12940bd67aba58011b3a7bd0`.

All changed source hunks across the combined 18 modified utility files were inspected, including the supplemental changes listed below. The full main fixture, changed primary public-guide hunks and both new supplemental guides were read. **Completed fixes appear correct:** finite duration parsing, int Wrap/PingPong, bool fallback, singleton gradient, tracked event replacement, single-pass bounds, one-shot layouts, four-corner viewport bounds and canvas-local conversion. Main fixture assertions observe public outputs and enumeration count; objects/PlayerPrefs key are cleaned. Its RNG cleanup omission is A-09.

Aggregate cost: explicit formatting returns strings and may create BigInteger digit buffers; layouts now allocate an element snapshot, point list and closure per call, trading bounded managed work for correct one-shot semantics. Combined bounds still allocates Unity renderer arrays and LINQ iterators, although it no longer traverses the source multiple times. `UIUtils.IsPointerOverUI` constructs PointerEventData/list and invokes all EventSystem raycasters per call; it should not be described as allocation-free or as a shared interaction query. No autonomous per-frame manager was added. Diagnostics throttle-key growth and preview cache cardinality are profiling/retention leads when their opt-in paths are used, not newly demonstrated runtime failures.

The versioned string-array codec is a **future proposal**. It must keep explicit legacy format selection and bounded decode; it does not justify automatic save rewriting. **Needs evidence / bounded composition correction:** UIUtils still reads legacy `Input.mousePosition` at line 24. An additive screen-position/caller-buffer overload plus an upper-layer `Neo.Tools.Input` adapter can support New-only projects without creating an Extensions→Input cycle. Keep legacy signatures and specify absence-of-device/EventSystem behavior; this phase did not run an alternate input configuration. The two supplemental guide backlinks and affected existing coroutine/preview/depth guide updates remain primary-owner documentation integration work.

## Completion receipt

Write `Docs/Audits/2026-10-23/Neo.Extensions.implementation.md` with exact actions/files/additive API/dependencies, migration details and authored acceptance (UNEXECUTED). If blocked on another additive API, complete independent files and identify it precisely for root continuation. Do not edit shared review-action/manifest/other actor reports.
