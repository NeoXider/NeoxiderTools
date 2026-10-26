# NI62 — NeoxiderPages bounded prefab repair

2026-10-25. Independent native C review: **SOURCE-CLOSED at the pins below; no confirmed residual/blocker. Native acceptance UNEXECUTED.** Original owner: `neo-audit-1023-b07-06`; root admitted successful stopped completion.
Current full Managers receipt: `Docs/Audits/2026-10-23/Neo.Tools.Managers.implementation.md`,247 lines, SHA256 `884cb616fc1882f945e2f025b574bfc46db549ea3eff8ad5ce722e1c6afb1660`.
Baseline: frozen source3 manifest `2d12ae058b989ce064e86a8d7db8223ecb65241d5dcd19babfb2fd7082adfc70`. All nine HEAD bodies match that baseline after read-only LF/CRLF normalization; no asset encoding/EOL was rewritten.
Actual raw source3→current diff is deletion-only: **124 lines /4029 bytes**. It removes12 demonstrably undefined m_Component entries, plus7 valid but proven-inert DOTweenVisualManager documents and their7 own component entries.
Every retained document ID/order/body is byte-preserved after only those component-entry deletions. No other valid component, animation configuration, hierarchy, RectTransform, sprite/material, public field, event or wiring was removed/replaced.
Full scoped source enumeration: **21 prefabs,558 unique per-file documents,355 explicit local component refs;0 unresolved refs, duplicate component entries/document IDs or Pro-GUID occurrences**. The other12 prefabs and all110 sample metadata files are byte-identical to source3.
Shop Page Variant was read completely: unchanged SHA256 `40950cdc22fd18259010959273f5d76a8a4b4020444d0877388acbeb180dce71`, same source prefab, no added/removed components or override of a removed manager. Current Assets serialized prefab/unity/asset reference search found no remaining reference to any removed manager document ID.
Actual vendor: `Assets/Plugins/Demigiant/DOTweenPro/DOTweenPro.dll`,16384 bytes, SHA256 `db5225f6db87969a03b48a56e2dc3828b38384adca44da0f89248a2bb483b056`; actual meta GUID `aa0b1eebb5db27a419fa4564bbe5c9c5`.
My independent raw PE/CLI reader reproduced TypeDef2 DOTweenVisualManager, Awake/Update/OnEnable/OnDisable IL and enum constants. Awake at RVA0x2050 calls MethodSpec1 `Component.GetComponent<TypeDef11 ABSAnimationComponent>` on this object and caches only that component. MD4 MonoScript type-ID calculation independently gives **40306178**.
All7 authored hosts contain only RectTransform/CanvasRenderer/UI.Image plus that manager and the listed undefined entries; no same-object ABSAnimationComponent driver exists. All7 presets are0/Custom, enable2/Restart, disable0/None. Actual Restart IL null-guards the cached driver; Update's restart-from-spawn path is not enabled. Thus no configured active animation/timing/event behavior is stripped.
Root-preserved visible static evidence: `Local/Audits/assembly-2026-10-23/managers-dotween-static-tool-evidence.txt`, SHA256 `f5a7c3e04658c0fd042376a0c17f4177bb45782a6deed7e0bd345d6da3c0120f`; critical bytes/tokens were cross-checked against the actual DLL, with **no vendor assembly loading/execution**.

Current prefab pins; paths relative to `Assets/Neoxider/Samples~/NeoxiderPages/Prefabs/Page/`:

| Prefab | SHA256 |
| --- | --- |
| `_Page base.prefab` | `515df0c42c26842994db3ae8c4d9ade98b8635860d2d1eb43c9a728a38191ac7` |
| `Example/End Page.prefab` | `b44d43a81bae8ae854279971f0bced815c5da2e4d8cc76a3157c1ef4e20318fa` |
| `Example/Game Page.prefab` | `944d9b64c7e9b3b72ceadb4e5d19f79806b3aa3a2c0a2d1f8463375e3c940387` |
| `Example/Lose Page.prefab` | `21012d28cbe3d35fef9bb208695662fc5966de9337f3b09d5978229da469f6bc` |
| `Example/Page Load.prefab` | `fd69616f6172eb817ddc99958cd3be3528457621f947f08852b8b2655e5cb897` |
| `Example/Pause Page.prefab` | `1ef611cdd370aa1b246d3d8739d09473697384dbd70a58c44129914e3bb4be13` |
| `Example/Privacy Page.prefab` | `7c041adf414ffc193248a5204b1fd62304d2dca1e5b99857f8870f8c56658959` |
| `Example/Shop Page.prefab` | `0368024b5b521b7eb8b162987773ecefc65479dabddd4de21fc550b1432188ca` |
| `Example/Win Page.prefab` | `7b585dc2b252914f091d87f965803cf18ac6c3f907eda787a42980c06dd0367c` |

GM runtime remains source3-identical: `Assets/Neoxider/Scripts/Tools/Managers/GM.cs`=`92e31e196583d879b57a28142d9cf68710a452e11bc32ecc039295ec7fa8b7d8`. Prior GM test bodies are unchanged; their known primary-owned probe extraction is separate from this asset repair. No runtime/test/source-helper/vendor/meta/other-owner patch was made here.
**Pending:** NEW current-source seal, both-version native UPM three-sample import, every Pages prefab/variant load, no-Pro capability lane, absence of PPtr/duplicate/missing-script warnings, actual page navigation/events/rendering and retained GM/UI acceptance. Old source3/ba515 imports and source counts do not certify these new bytes; imported sample copies require re-import.
Only this report was written. Work was source/diff/reference/byte/hash enumeration and static binary-data inspection; no Unity/MCP, compiler, tests, product checker, vendor execution, process probe/control, install/service, product edit, helper edit, commit/push or child agent was run. Source closure is not a global freeze/native permission grant.
