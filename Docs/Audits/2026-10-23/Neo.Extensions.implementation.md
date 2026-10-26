# Neo.Extensions: implementation receipt

Date: 2026-10-23. Same original `/root/audit_extensions` session, `gpt-6.1-sol`, reasoning `max`. Branch `demo`, target package 11.0.0. Read IMPLEMENTATION-CONTRACT.md, Neo.Extensions.implementation-request.md, current AGENTS.md, original audit/plan, both supplemental guides and the relevant current API bodies.

**Status: fully SOURCE-FINAL.** All assigned documentation/RNG work, including final pointer-coordinate/input-adapter documentation integration, is complete against the published source and both owner receipts. No source API dependency remains for this owner. All acceptance is **UNEXECUTED**. This is not a Unity compilation, test, package-validation or release pass.

## Actions completed

### EXTENSIONS-DOC-LINKS

Canonical README now links both existing companion topics, NumericEdgeCases.md and LifecycleAndOwnership.md, and summarizes their numeric/lifecycle/coordinate contracts. Those supplemental files already link back to README and were read without editing their owners' text.

Existing coroutine/preview/screen guides now describe the reviewed contracts rather than referring users to a one-line placeholder or an incomplete lifecycle example:

- CoroutineHandle.Stop is the tracked cancellation/disposal path. Raw native StopCoroutine/StopAllCoroutines cannot promise automatic handle completion. Component destruction, hierarchy deactivation, component-only disable, synchronous completion, global fallback and native yield phases are distinguished. The C# scheduling/cancellation example uses explicit types and the same methods scene UnityEvents can invoke.
- Preview sprites/textures distinguish borrowed renderer/image/AssetPreview resources from generated cache-owned Sprites. Views reacquire generated fallbacks after clearing; generated previews must not become persistent icon data. Cache clearing does not destroy authored sprites/textures. No automatic asset conversion is introduced.
- Screen-edge placement uses camera pixelRect and near-plane-relative depth for both projection modes; GetWorldScreenBounds keeps camera-relative distance and four-corner AABB behavior. An explicit `oldCameraDistance - nearClipPlane` migration example and ScreenPositioner backlink are provided. Its world-Z preservation option is a separate post-projection behavior; assets are not rewritten.

English descriptions and Russian summaries are included. No framework, codec, scope or registry is introduced. Runtime APIs documented here are implemented by their original supplemental/Input owners, without runtime edits by this main owner.

### INPUT-POINTER-API: completed documentation integration

Read the source-final Extensions-Coroutines-Unity and Neo.Tools.Input receipts and actual UIUtils, UIInputCompat, MouseInputCompat and InputBackendCompat bodies. UIUtils.md and canonical README now document the published coordinate overloads, compatible mouse adapter, caller-owned buffer clearing/count/null behavior, unavailable mouse/EventSystem results and per-query PointerEventData allocation. Legacy UIUtils cursor APIs retain their legacy input behavior; Both/New-only callers explicitly choose the upper Input adapter or supply coordinates. EventSystem.RaycastAll can include configured physics raycasters; no pure-graphic filtering or shared-query/zero-allocation claim is made.

### Native A-09: original fixture RNG isolation

Audit20261023ExtensionsTests now captures UnityEngine.Random.state in SetUp and restores it in a TearDown finally block after existing object/PlayerPrefs cleanup. The sphere layout case no longer intentionally leaks shared RNG progression into later fixtures, including failed assertions or an exception in cleanup. Existing behavior assertions and all 32 authored cases are retained; no test was executed and no unsupported parallelization attribute was added.

## Exact files changed in this implementation batch

| Path | Change |
| --- | --- |
| `Assets/Neoxider/Docs/Extensions/README.md` | Canonical companion links, numeric boundary backlink, corrected lifecycle/preview/depth summary, Russian navigation/migration note. |
| `Assets/Neoxider/Docs/Extensions/CoroutineExtensions.md` | Actual overload/handle surface, owner and native-stop boundary, explicit-type C#/scene cancellation example, LifecycleAndOwnership backlink. |
| `Assets/Neoxider/Docs/Extensions/PrefabPreviewExtensions.md` | Preview API/ownership/player-build/cache lifecycle and persistent-data guidance, LifecycleAndOwnership backlink. |
| `Assets/Neoxider/Docs/Extensions/ScreenExtensions.md` | Point/edge/bounds contracts, viewport and orthographic depth migration, consumer/UI/lifecycle links. |
| `Assets/Neoxider/Docs/Extensions/UIUtils.md` | Exact coordinate/caller-buffer and compatible-mouse APIs, legacy migration, buffer/allocation/raycaster boundaries, canvas-local conversion and Input/lifecycle backlinks. |
| `Assets/Neoxider/Tests/Edit/Extensions/Audit20261023ExtensionsTests.cs` | Random.State snapshot in SetUp and unconditional finally restoration in TearDown. |
| `Docs/Audits/2026-10-23/Neo.Extensions.implementation.md` | This receipt. |

All package paths above already exist, so no new package metadata is needed. Existing metadata and the user-owned DocsEn.meta are untouched. Original audits/plans and independent B review reports remain historical evidence and are not rewritten.

## Resolved cross-owner APIs / integration ownership

The main owner edits **none** of the 31 runtime files. Extensions-Numeric-Time-Random owns 8; Extensions-Coroutines-Unity owns 23, including UIUtils. Neo.Tools.Input consumes the coordinate path above Extensions without a reverse dependency.

Both source dependencies have landed and were read directly:

- `Neo.Extensions.UIUtils.GetUIElementsAtPosition(Vector2 screenPosition) : List<RaycastResult>`.
- `Neo.Extensions.UIUtils.GetUIElementsAtPosition(Vector2 screenPosition, List<RaycastResult> results) : int`; clears the supplied list, returns count, clears/returns zero without EventSystem and rejects null with ArgumentNullException.
- `Neo.Tools.UIInputCompat.GetUIElementsUnderCursor() : List<RaycastResult>`, `GetUIElementsUnderCursor(List<RaycastResult> results) : int`, `IsPointerOverUI() : bool`; uses MouseInputCompat.TryGetPosition(out Vector3), clears on unavailable mouse, and consumes the exact lower coordinate API.
- `Neo.Tools.InputBackendCompat.UsesLegacyInput` and `ResetBackendSelection()` are the common backend-selection surface, documented by the Input owner.

The historical proposed RaycastUI/optional EventSystem signature is not the implemented API and is not advertised. Root owns the discrete UnityEngine.UI reference integration in Neo.Tools.Input.asmdef; this owner makes no asmdef edit or compile claim. No New-only configuration was run here; backend/device behavior remains central acceptance, not an unresolved documentation/source API dependency.

ScreenPositioner source and its current consumer guide were read. The main owner supplies the helper-side migration guidance only; Neo.Tools.Move owns the consumer `_depth` documentation/configuration. Inventory owns removal of generated preview assignment from persistent icon data; the shared cache ownership rule is documented here without crossing into its source. No scenes/prefabs/samples/settings/asmdefs are changed.

## Authored acceptance / UNEXECUTED

1. Execute the existing 32-case fixture after global source freeze. Snapshot an outer RNG sentinel, run the sphere case through setup/teardown on success and intentional assertion failure, and observe the original Random.state/next draws after cleanup. Other layout assertions and owned-object cleanup must remain intact.
2. From canonical README navigate to both companion guides and back. Coroutine, preview and screen pages agree with them; all new local links resolve. English/Russian examples describe currently available APIs and no proposal is presented as implemented.
3. Execute the supplemental coroutine cases: handle.Stop twice, raw native stop then handle.Stop, owner-component destruction, inactive hierarchy, component-only disable, synchronous completion and native wait phases. Observe state plus cleanup/order; source reading is not native lifecycle evidence.
4. Execute preview cache acceptance with authored renderer/image assets and Editor fallback: only generated Sprites are released; borrowed assets survive; a view rebind obtains a valid preview after clearing/domain reset. Inventory's persistent icon remains authored data. Test Edit/Play deferred destruction and domain reload disabled centrally.
5. Compare orthographic/perspective edge placement at the same desired camera distance using the explicit near-relative conversion, offset pixelRect and a rotated camera. Verify ScreenPositioner world-Z policy separately; no automatic serialized depth migration. Existing four-corner bounds and canvas-local tests remain part of acceptance.
6. Execute the now-published coordinate/input API acceptance with supplied coordinates, absent EventSystem and a reused caller buffer; check legacy/Both/New-only adapters, no device, configured graphic/physics raycast semantics, null-buffer exceptions and allocation claims. Exact APIs are documented from source; execution remains UNEXECUTED.
7. Root compiles and performs nonempty discovery/execution on the installed representative Unity 6.3/6.6 hosts after **every** writer finishes, then runs package/docs/UTF-8/meta/link validation. Any unavailable native branch/capability stays explicitly unverified.

## Actual work and execution barrier

Performed only source/document reads, reference enumeration, scoped diff inspection and the authorized edits above. A PowerShell read process failed with Stack overflow; a small subsequent read succeeded, and after root's memory-health instruction further reads used one cmd.exe process at a time. This is an environment/read incident, not a Unity/runtime result.

No Unity/MCP, compiler/build/test/product-checker/validator, install/service, commit/push or child agent was started. No automated static validation is claimed for this new batch. Prior audit checks apply to their historical snapshots only. All assigned source/documentation work is complete; every execution acceptance case above remains **UNEXECUTED**.
