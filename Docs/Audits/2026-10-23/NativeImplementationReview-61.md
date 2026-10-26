# NI61 — independent source review of C's metadata-v2 helper

Disposition: **SCOPED SOURCE APPROVAL; no confirmed residual source defect in the admitted typed-projection/read-only diagnostic revision.** This is not compilation, selftest, native capture, schema-equivalence, repair or launch approval. I authored the separate preparer/launcher/guard, not this C-owned helper. Root confirmed C stopped before this review. Only this report was written; no code, utility, parser, probe, compiler, Unity, test, validator or process operation was executed.

Both artifacts were read completely, with hashes checked at initial and final read:

| Artifact | Full read coverage | Actual SHA256 |
| --- | --- | --- |
| [NeoAssetSchemaMaintenance.cs](D:/Git/NeoxiderTools/Local/Audits/assembly-2026-10-23/unity/maintenance-source/asset-schema-v2/NeoAssetSchemaMaintenance.cs) | 800 lines /54,624 bytes; every declaration, branch and helper | `34a65bd8af2d6e9462613eb0457c4c8cf48111e05213b551eabfd9edb0c23b52` |
| [HelperRevisionPlan.md](D:/Git/NeoxiderTools/Local/Audits/assembly-2026-10-23/unity/maintenance-source/asset-schema-v2/HelperRevisionPlan.md) | 27 lines /8,740 bytes; all policy, evidence and execution limitations | `5963f4390eebc4ea007e9175bb9b69d5f1b21583e75a3612d31e5cd0d973e72a` |

Read map: Run/admission/diagnostic/write/exit1–288; importer/prefab/supplemental capture290–487; full raw capture/typed projection489–591; all seven selftests594–627; strict native reader/persistent references629–695; schema/provenance/raw-block/inventory/path/receipt helpers697–800. Existing v1 CaptureObject351–370 was also read as a read-only dependency for the retention delta. The previously retained ba515 before-state comparison is historical diagnostic evidence, never an after-migration oracle.

## Typed semantic projection — source closed

CaptureObject489–535 still retains complete native JSON and every successfully read raw property encoding, including internal pointer children, in the original evidence arrays. It separately collects native property path/type/kind and whether Property actually returned a canonical ObjectReference value. The semantic array366–374 is a distinct projection; raw arrays/JSON are not rewritten or silently replaced by it.

ReferenceInternalDescendants550–570 receives properties for ONE captured object. It first validates unique paths and admits only `SerializedPropertyType.ObjectReference` parents with successful canonicalization and a valid persistent GUID/nonzero localID token or successful null token. Only subsequent paths beginning with that exact parent's `.` descendant boundary are excluded. The parent itself remains compared. Parent discovery precedes filtering, so child-first/sorted raw order cannot hide a parent. There is no global suffix/name rule for `.m_FileID`, no cross-object parent ownership and no exclusion merely because an integer or managed-reference-shaped field has the same name.

The real Property reader640–648 requires a zero instance-ID sentinel for an actual null pointer and otherwise calls Reference682–688, which requires a persistent object and successful native GUID/nonzero localID lookup. Unresolved/nonpersistent references throw. Any property failure in diagnostic mode records its gap and prevents ALL exclusions for that captured object523; semanticComplete374 becomes false. Strict capture propagates the exception. Unsupported serialization kinds remain blocked678. ManagedReference-shaped pure projection records remain compared, while actual unsupported ManagedReference production capture is not newly enabled.

Final equivalence260–263 compares complete before/after semantic arrays, retaining parent GUID/localID/reference identity alongside all other values. Raw native JSON/arrays remain forensic output, rather than an equality oracle over unstable internal integers. The 4,182 historical cross-host raw differences motivate this narrow ownership rule; neither those differences nor the author's projected82-pair analysis establish migration equivalence or a v2 execution pass.

## Seven source-invoked guard controls — all UNEXECUTED

Run178 calls RunProjectionGuardSelfTests before explicit maintenance writes237. Labels are assigned only if every Need in the pure guard function completes. These are operator projection controls, not a replacement for native assets or a product test run:

| Control594–627 | Concrete positive/negative source oracle |
| --- | --- |
| canonical-parent-retained | Child-first baseline produces exactly one entry containing the persistent parent token. |
| internal-child-only-and-child-first | Changing the internal child integer and input ordering leaves projection equal. |
| parent-guid-change-detected | Changing parent GUID makes the projected array unequal. |
| parent-localid-change-detected | Changing parent localID makes the projected array unequal. |
| user-integer-retained | A same-named ordinary integer without an ObjectReference parent changes equality. |
| managed-reference-child-retained | A ManagedReference-shaped parent does not grant child exclusion. |
| unresolved-parent-blocked | Failed canonicalization, even with a null-shaped token, must throw. |

The first future authorized Run must actually compile/execute these controls and retain its receipt. No labels or passed-selftest receipt have been produced by this review. Automatic initial Unity import necessarily precedes Run; its seeded-byte checks168–171 are independent of these pre-explicit-write controls.

## Exact Icosphere read-only supplement — source closed; repair blocked

The additional route341–357 is selected only for the exact frozen Icosphere path, original GUID1fe56a0e685b8434ebfeb53c69b59a5e and ModelImporter/profile. It adds a separate attempted/complete flag while keeping the original missing-reference gap and semanticComplete decision. New supplemental exceptions/gaps remain partial evidence, not a successful before-state.

CaptureIcosphereVisible416–487 observes the native Material/ProBuilderDefault key match count/reference, expected missing GUID883a7db7f994aab478a4380ad50eda70 path and whether localID2100000 exists. It requires the actual persistent Model prefab root, records full hierarchy/component order/serialized state through Walk, verifies loaded GameObject/Component subassets fall within that hierarchy, captures full native/serialized state of the other loaded subassets, requires at least one Mesh and renderer, and records Mesh counts/topology plus every ordered renderer shared-material slot identity/count. Bound materials, including external persistent materials, receive full state capture. `sharedMaterials` is read; there is no material-instantiating accessor or setter.

Precision: matchingRemaps/fileID/path are **observations**, not an asserted single-null-remap repair precondition. supplementalModelComplete means graph capture without additional capture gaps; it does not mean a remap is unused, repair-safe or admitted. Native evidence must still establish the exact one-key missing mapping, actual visible bindings/subassets and all other importer state before any future candidate can be reviewed. The old importer-only before.nativeJson cannot supply that graph. Zero or unexpected native match/binding observations must be reported honestly and assessed by root; this read-only code contains no repair decision based on them.

No RemoveRemap/AddRemap call, repair flag, generic unresolved-reference bypass, material creation or mapping rewrite exists. Raw externalObjects equality remains mandatory262,715–730. The strict existing unresolved-pointer guard still prevents Icosphere and the complete canonical upgrade from reaching the first write. Any eventual exact-key removal needs separate C/root source policy, new native evidence/authority and a fresh run; neither this report nor the helper plan grants it.

## Source/protocol and preservation integration — source closed

- Run103 restricts execution to canonical only. All314 input records remain counted as232 canonical/82 comparative; copybackEligible must match that role156–160. No legacy execution/write/copyback path is admitted. Authorization explicitly denies product tests/copyback/warm Library and requires canonical232 scope, reviewed ledgers, unique run/token, short owned project and matching company/product/native persistence117–137.
- Dynamic NEW manifest/input hashes reject the old2d12/3e6 inputs. SchemaVersion2, v2 kind, authoringOnly/legacyComparativeOnly, snapshot-root/source paths, manifest/source-request SHA and preparation's frozen/source identities are bound138–167. These fields align with the separately authored v2 utilities. Their NI59 inventory correction/independent NI63 review is a separate admission scope; NI61 does not self-approve my utility code or a root capture.
- All232 strict before captures and native-error/Assets invariance checks precede explicit writes223–238. Diagnostic returns221 before reserialization/ImportAsset, records no writes/roundtrips, and retains capture gaps. Actual startup errors before Run still require full operator log review; a helper capture status is not a globally clean import claim.
- Metadata operations retain source binary/body SHA; prefab-content operations retain meta SHA. Before/after persistent GUID lookup in both directions, raw external map, AssetOrigin/provenance, importer/user/bundle/platform data, subasset identities, hierarchy/component order and collider paths remain represented and checked. Only named target output files enter the Assets change allowlist252–273; unexpected changes/new files and insufficient/missing schema versions fail.
- Every target is explicitly ForceUpdate/ForceSynchronousImport round-tripped248 after native reserialization, before new captures. DTOs retain strings rather than pre-write Unity objects. Native errors stop later batches/imports; final receipt failure forces nonzero exit281–286. No blanket SaveAssets or manual YAML/schema edit was added.

Remaining evidence is **UNEXECUTED**: v2 compilation, seven guard controls, new corrected CURRENT sealed source/prepared profile/namespace admission, complete232 read-only native states including Icosphere visible Mesh/material graph, inventory positive/negative controls, any schema roundtrip/equivalence and any repair/copyback. First future native mode remains diagnostic-only under a NEW explicit root authorization. Source approval is limited to these reviewed helper/plan pins; general product execution and writes remain closed.
