# NI70 — Inventory P5 nullable wire codec

Disposition: **SOURCE-HOLD for NI70-01 (P2); other bounded P5 contracts source-supported.** Root admitted SAME original `neo-audit-1023-b07-05` stopped/not live; actual meta is done/exit0, SHA `52f9a2ef76bcf95088aec2c6ddb26fb2221682716ddbbf4f5169cb3c0e2fc4a8`. No other moving owner/release file was reviewed. Only this report was written; no Unity/compiler/test/checker, process/child operation or product edit occurred.

Read the complete current312-line receipt, complete202-line codec/288-line persistence, every changed/new regression hunk and all nine new variants, relevant legacy/layout/admission/fault controls, and both complete owned guides. Baseline is immutable p5 source `956e2aa29ea85e7dea90df498a70392083af4a067058cbcbe24891105966efd1`; broader unchanged Inventory coverage is retained, not re-audited.

| Current repository path | Actual SHA256 |
| --- | --- |
| `Docs/Audits/2026-10-23/Neo.Tools.Inventory.implementation.md` | `7d6d403bb3e8cca66e4f461f24aac99e739fb689a6410f7adebb9e3d3cfd54db` |
| `Assets/Neoxider/Scripts/Tools/Inventory/Core/InventorySaveData.cs` | `8b3767a535f9fb3c70f7938faa83b8d82bc14137c0123f8413a2202d9581032b` |
| `Assets/Neoxider/Scripts/Tools/Inventory/Runtime/InventoryComponent.Persistence.cs` | `a8985741c3ab1a08e370052d1f25d2f5e3a92f891b43cfd8d1430660a96bcb36` |
| `Assets/Neoxider/Tests/Edit/Tools/Inventory/Audit20261023NeoToolsInventoryRuntimeTests.cs` | `d22b7c8dedd0e5cefea6f72cfd3ccc06295bdcbe06f836301ce0d4e77b334ec2` |
| `Assets/Neoxider/Tests/Edit/Tools/Inventory/Audit20261023NeoToolsInventoryCommitTests.cs` | `24369e0fc0905400131d48aed486092cb10b7aacc279c81c1900d2b5716eba30` |
| `Assets/Neoxider/Docs/Tools/Inventory/InventoryComponent.md` | `f1886dada8ad08d4b848241f2529228109e7c5ae92a3a1cedea17f81bafd4258` |
| `Assets/Neoxider/Docs/Tools/Inventory/InternalTypes.md` | `b370da728cc70ab4075ff4b9785e5b60dc2ee1a400f265fe7ee5955d5d7fffd4` |

## NI70-01 — omitted slot ItemId becomes a valid item0

Owner: SAME Inventory b07-05. `InventorySaveData.cs:185–189` uses one zero-default WireRow for both Records and Slots. `FromJson:84–86` explicitly copies its default ItemId0 into InventorySlotState, overriding that domain type's existing ItemId=-1 sentinel (`InventorySlotState.cs:12`). Records instead have default ItemId0, so the two row defaults are materially different.

Concrete unexecuted source repro, with no initial content/database restriction:

```json
{"Version":1,"StorageMode":1,"SlotCapacity":1,"Slots":[{"Count":1,"Instance":{"ItemId":0,"Count":0}}]}
```

The SLOT ItemId is absent. The entirely zero optional instance becomes null, but the wire row contributes ItemId0/Count1; the converted slot is nonempty and component import can grant one simple item0. Expected missing slot id retains the -1 empty/invalid sentinel and grants nothing. The unchanged admission accepts nonnegative item0 when unrestricted (`InventoryComponent.Grid.cs:153–173`, `InventoryComponent.cs:274–281`); this is not stopped later. Current SlotState SHA `eeddc4a1e803a1841ef8662c3795afce3cdff33d0539c21d8b572806b141e9b3` and Record SHA `29732fd3ea57c8d1f7a1a3c0a51b0f21cda3233fbbe1f454310676c115e4a71a` equal p5. No native execution was used to infer the grant.

Minimal correction accepted by root: a private SLOT wire with ItemId=-1, retaining RECORD default0 and optional-instance zero numeric defaults. Preserve explicit simple ItemId0/Count1 and real id0/keyless instances. Add literal missing-slot-id decode/load negatives plus explicit id0 positive and legacy record-default control; keep all current nullable forms, nonempty raw Count0 precedence and payload cases. Clarify generic zero-default wording in owned guides if necessary. No new schema/key/version, public-domain default change, broad Count1 normalization or framework is required.

## Other bounded source-supported behavior

- Public `ToJson(bool prettyPrint=false)` and static `FromJson(string)` are additive. Existing public fields/domain Count1 remain. Explicit root metadata, row id/count, identity/version/component payloads and legacy lists are mirrored; missing lists become empty collections and unknown fields retain JsonUtility handling. Optional instances normalize only the fully empty wire shape. Decode preserves raw fields without Clone/id generation/count admission, so explicit id0/Count1 and nonempty identity/version/payload retain precedence over their wrapper. The earlier direct-JsonUtility callback remains narrow and is honestly documented as insufficient for Count1 materialization. NI70-01 prevents claiming all omitted row defaults are already preserved.
- Persistence differs from p5 ONLY at Save31 (`data.ToJson`) and read116 (`InventorySaveData.FromJson`). Layout/capacity restoration, complete physical topology, legacy Entries/Instances import and initial/merge markers remain. Provider GetString is still outside the parse catch; SetString then OnSaved ordering, first-fault paired finalization and lack of multi-key/disk atomicity are unchanged. No provider-failure execution is claimed.
- All old semantic assertions/case variants remain; the removed assertion delta is only the disproved raw-domain Count0 observation, replaced by actual P5 Count1. An independent callback-free zero-default wire probe still checks zero/null output and a real Count1 identity positive. Four literal absent/null/empty/zero optional-instance variants, three nonempty Count0 metadata/payload precedence variants and two real id0 load/save/load modes provide meaningful controls. Backend/capacity, empty/simple/instance, invalid-wrapper/overcapacity, legacy/merge metadata and opaque payload oracles are retained. These tests do not yet cover NI70-01's omitted SLOT id.
- Six paired observer-fault cases preserve exact exception identity, fault callback1, both OnSaved/write counts1, committed physical state and serialized source-empty/simple-target checks. Their new fresh component reloads assert empty source/zero instances and exact simple target item7/count1 in slot2. No observer is removed to manufacture success. Other commit/drop contexts only switch their existing JSON calls to the codec; the authored provider/fault semantics are not weakened.
- Both owned guides explain code-first and scene persistence, ambiguity of externally encoded id0/keyless/Count1, canonical codec use and unchanged JSON/version1. Root separately owns Migration11/CHANGELOG/README integration; those moving files were not approved here. All six file metas are byte-identical to p5; the previously accepted lifetime fixture remains `b9dfb2fa35aa40636d9d2e2b766c2dddf2881cec18a57973373f4e62fb7641a2`.

NI70-01 was sent to root promptly and is the only confirmed residual from this full bounded review. All current receipt/source/doc hashes were rechecked stable. Corrected private-wire native serialization, all20 prior P5 failures, nine new variants, required sentinel controls and both-version integrated/provider/cleanup evidence remain **UNEXECUTED/OPEN** until SAME-author correction, root review and a new global freeze.
