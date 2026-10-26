# Neo.Tools.FakeLeaderboard — implementation receipt

Status: **source-final for the 2026-10-26 P6 mapped-member correction and native mapping diagnostic**. This is a concrete source correction with unexecuted regression sources; compilation, actual token capture and corrected-source native acceptance are **PENDING** root review/new global freeze. P6 disproved closure of escaped-name handling in the preceding P5 source. Prior delivery records below remain history.

Date: 2026-10-26. Same original actor/session `neo-audit-1023-b07-01`; target `demo` / `11.0.0`, `gpt-6.1-sol`, reasoning `max`.

## P6: mapped JSON names on shipped Mono — source correction, native acceptance pending

Read the complete current implementation contract/request and `Local/Audits/assembly-2026-10-23/p6-fake-native-name-assessment.md` (SHA-256 `0d1d47e0efd80356215bef7f5603e1a38824b0b14855ee7bd80812b3c4627073`). Before editing, root's read-only `implementation-state.json` recorded phase `p6-actual-failure-source-corrections`, `sourceFreeze: false`, `executionAllowed: false`, and execution stopped at `2026-10-25T20:44:01.728376+00:00` after all 12 P6 native jobs were terminal. No job/process inspection or control was performed by this actor.

### Actual native evidence read

| Artifact | SHA-256 / exact owned result |
|----------|-----------------------------|
| `Local/Audits/assembly-2026-10-23/unity/final/p6/u63/Receipts/20-corrected-families-edit.xml` | `7ab8b3bbb4a4cee8a460b601fed07370dc9d2c17293f3c9ec06f834c50ba31e5`; serialization fixture **56 = 50 passed / 6 failed**. |
| `Local/Audits/assembly-2026-10-23/unity/final/p6/u66/Receipts/20-corrected-families-edit.xml` | `e40644c808b54070bd57f6020976894adafe814378fc5ed14bd89e9da01f69e1`; serialization fixture **56 = 50 passed / 6 failed**. |

Read all six complete failed leaf payloads on **both** engines. IDs `4854`, `4857`, `4858` reject valid escaped player/users empty input, escaped root-array payload, and escaped DTO payload/ownership respectively: expected `true`, actual `false` at frozen fixture lines 352/384/407. IDs `4831`/`4832` accept plain-plus-escaped duplicate root arrays in both orders, and `4842` accepts duplicate player-key rows when one `id` member is escaped: expected `false`, actual `true` at line 335. Those three negative cases first establish a genuine ordinary empty-array import and its player key/count at lines 329–333. The other 50 historical fixture cases pass. Payload assertions following the three failed positive admissions were not reached; their intended outcomes cannot be claimed from P6.

The frozen input is `unity/snapshots/final-20261025T194318Z/source`. Its codec `93f39fbb46a0786743d3757e1b6a3bf43df9ca5a8183d9dbda2930c6c240ac57` and fixture `16e2062c81e937b00589de348b2aa2627fc64a74f75ce7d2f2595dcd28c4b84e` match the pre-edit current bytes. The last JsonUtility-to-typed-reader replacement therefore did **not** close this compatibility gap.

### Supported cause and bounded correction

The independent assessment identifies the actual loaded Mono `System.Runtime.Serialization.dll`, SHA `1152ef687dbe39e0ccb6b9a68f6501004415a729072df29abab3bc7c66840217`, MVID `8c16867d-ce0c-4131-9701-5151b019bc02`, and reports matching installed IL. I also read the corresponding primary [Unity Mono XmlJsonReader source](https://raw.githubusercontent.com/Unity-Technologies/mono/unity-main/mcs/class/referencesource/System.Runtime.Serialization/System/Runtime/Serialization/Json/XmlJsonReader.cs): `ParseAndSetLocalName`, `SetJsonNameWithMapping` and `GetAttribute`. Its mapping places names that require mapping in an `item` element/namespace with an unnamespaced `item` attribute; the getter decodes that JSON name. This is source/binary-context evidence, not a captured native token tuple or a corrected-source pass.

The old empty-namespace-only dispatch skipped mapped player/users/id names. That explains all six failure directions: required fields disappear, escaped duplicate arrays evade duplicate detection, and an escaped player ID becomes keyless. The completed runtime change adds **one private `GetJsonMemberName` accessor**, used at both aggregate and user-field dispatch. It retains ordinary empty-namespace LocalName; only exact LocalName `item` plus NamespaceURI `item` reads `GetAttribute("item", string.Empty)`. Missing attributes/other namespaces remain unknown. Existing exact case-sensitive comparisons decide whether the returned name is recognized. The decoded attribute is not decoded a second time.

No global replacement, XmlConvert aliasing, raw-payload rewrite, vendor patch, new parser/dependency/framework or shared Save-parser change was made. Array presence/type/root scope, duplicate detection, full-input validation, atomic null outputs, optional/null/default fields, Int32 bounds, null slots and canonical ownership code remain unchanged. Public/serialized API, lowercase private DTO wire fields, save format/keys, event/I/O behavior and all GUIDs remain intact.

### Regression sources and public guide

All **56 existing case identities and assertions**, including the exact six failed oracles, remain. The source diff against P6 contains no removed or weakened fixture body or case. Three additional rejected aggregates prove that XML-looking `_x0075_sers`, literal backslash-u member text and an escaped `users key` remain nonaliases; the existing positive precondition and both-null rejection oracles apply to each.

Because actual reader tuples were not captured in P6, the new standalone `LegacyRawJson_ReaderMemberMappingReportsExactDecodedNames` test reads **13 bounded member tokens** through the public JSON-reader API. It reports the actual reader type/assembly/MVID and each LocalName/NamespaceURI/unnamespaced-item tuple through NUnit output, then asserts exact expected names. It accepts an ordinary direct decoded name or the exact mapped representation, never suffix/prefix/XML-name aliases. Controls cover ordinary users, leading/middle escapes, player and all four DTO fields, space/colon custom keys, literal backslash-u, case and XML-looking names, plus exact token count/end-of-input. It does not call the private production accessor or replace the six semantic regressions. This diagnostic is authored and **UNEXECUTED**; future native output is needed to establish the actual tuples.

`Assets/Neoxider/Docs/Tools/FakeLeaderboard/Leaderboard.md` now explicitly states single decoding, literal text preservation and exact-case/nonalias behavior. It retains the existing import format and runtime API guidance.

### Final current source hashes and handoff

| Path | SHA-256 |
|------|---------|
| `Assets/Neoxider/Scripts/Tools/FakeLeaderboard/LeaderboardLegacyJson.cs` | `2f78c5724665b0ad106f93a7f08a332a22c1835891190abc3acb446e9bc5217e` |
| `Assets/Neoxider/Tests/Edit/Tools/FakeLeaderboard/Audit20261023NeoToolsFakeLeaderboardSerializationTests.cs` | `f68f25d11c58c9d98cfe908975b2164e26a305dc6f03d50337bc9289a1c41750` |
| `Assets/Neoxider/Docs/Tools/FakeLeaderboard/Leaderboard.md` | `19e8c524371df20796983dceb8cbcdafec3109db2d4a6254eda79819d3d7d173` |

Actual work: cmd.exe/Python source/artifact reads and byte hashing, primary-source browsing, complete six-failure XML reads on each engine, scoped edits and manual source-diff review against the frozen P6 files. No Unity/compiler/test/product-checker/process-control/install/service/child/Git operation was executed. No existing acceptance was changed to fit reader behavior.

The primary relocation map was checked before every edit; all four edited codec/fixture/guide/receipt files are unreserved, primary extraction is source-final and has no remaining source dependency. No Fixtures/shared reference, primary source rule, foreign assembly, shared tracker or other owner's source was edited. No Save defect is asserted: its reported passing middle-character cases do not establish first-character behavior, which remains a separate owner's task.

**No cross-owner source dependency remains.** Root/native-B review and a new freeze must precede compilation and complete fixture execution on both Unity 6.3/6.6. Required evidence is all original 56 cases with strict six-failure oracles, the three additional rejection inputs and actual mapping output, including custom/literal names remaining nonaliases. Existing payload/identity/bounds/null-slot/atomic-output assertions must all be reached and pass natively. Current exact token tuples, compilation and corrected-source results remain unresolved; no six-case closure or stage pass is claimed. Source writes stop with this receipt.

## P5 E13: actual failure and completed source correction

Read the current implementation contract/request and `Local/Audits/assembly-2026-10-23/p5-source-correction-common.md`. The frozen source snapshot is `Local/Audits/assembly-2026-10-23/unity/snapshots/final-20261025T151934Z/source`, snapshot SHA-256 `956e2aa29ea85e7dea90df498a70392083af4a067058cbcbe24891105966efd1`. This action continues the existing edits; it does not infer completion from the S3 receipt.

Historical P5 native artifacts actually read, including the exact assigned leaf failure and same-run positive controls:

| Artifact | SHA-256 / relevant outcome |
|----------|---------------------------|
| `Local/Audits/assembly-2026-10-23/unity/final/p5/u63/Receipts/20-full-edit-graphics.xml` | `2fc329874dff049e845eebd01635deb1f445359fd951fe82536c79cb2bb7debd`; FakeLeaderboard selection: 63 passed, 1 failed. |
| `Local/Audits/assembly-2026-10-23/unity/final/p5/u66/Receipts/20-full-edit-graphics.xml` | `adeb027f9f14e1c34c8973767f9a6d3420c7c188f809ea26283ebda32d31a74b`; FakeLeaderboard selection: 63 passed, 1 failed. |
| `Local/Audits/assembly-2026-10-23/unity/final/p5/u63/Receipts/21-full-play-graphics.xml` | `7ae7f99a5f648cf31a86a445bdd59761bfc3390d4bcf9d4d3e1371a6e86d681c`; no FakeLeaderboard test-case entries. |
| `Local/Audits/assembly-2026-10-23/unity/final/p5/u66/Receipts/21-full-play-graphics.xml` | `73d18c2935644dd92c8fecd6cd1415c5d8f9443b9f0fd3a7afadf2ce9974cc69`; no FakeLeaderboard test-case entries. |
| `Local/Audits/assembly-2026-10-23/final-edit-p5-triage.json` | `59c218396e4723537d93c2110b5c0bbc7051f4550fdbbc2b6196e5316f75a9d6`; E13 assigns this codec compatibility defect to this same actor. |

Both Edit receipts fail test-case `4807`, `Audit20261023NeoToolsFakeLeaderboardSerializationTests.LegacyRawJson_EscapedRootArrayKeyPreservesUserPayload`, at frozen fixture line 365: expected `true`, actual `false`. Its root array key is the JSON escape `\u0075sers`. Ordinary writable-ID conversion, independent keyless rows, all five optional-player/explicit-empty-array inputs and the full existing rejected-aggregate matrix passed on both frozen hosts. These are real positive preconditions for the importer and evidence that the S3 gate fixed absent arrays, while escaped-name payload binding still failed. The two Play receipts supply no additional module pass evidence.

### Completed owned edits

- `Assets/Neoxider/Scripts/Tools/FakeLeaderboard/LeaderboardLegacyJson.cs`: replaced the structural-gate/raw-JsonUtility split with bounded DTO reading through the existing `JsonReaderWriterFactory` / `XmlDictionaryReader`. Required root-array presence, decoded member names and string payloads now come from the same structured token stream. Exactly one root `users` array is required, including an empty one; duplicate plain/escaped spellings, wrong kinds, nested/string decoys, malformed/trailing input and duplicate matching player keys still reject. Unknown members, including custom keys and `sortUsers`, are skipped without renaming. There is no text replacement, shared Save-parser edit, new dependency or general parser framework.
- The existing private lowercase DTO fields, public method signature, identity conversion/ownership rules and atomic publication remain. `player` must be an object; its ID/name/score/num are optional, with absent ID/name null and absent integers zero. String/null fields and Int32 integer payloads are read directly; invalid typed payloads reject instead of publishing partial data. Null source slots and source ordering survive. No board, provider, settings, asset or save key is changed by this codec.
- `Assets/Neoxider/Tests/Edit/Tools/FakeLeaderboard/Audit20261023NeoToolsFakeLeaderboardSerializationTests.cs`: retained the exact E13 test identity and every original ID/name/score/num/count/reference oracle. Added 17 rejection inputs covering decoded duplicate arrays in both orders, escaped wrong kinds/case/nested decoys, custom keys, decoded duplicate player identities, absent/null player and invalid typed payloads after valid data. Each rejection still first proves a valid empty-array import, then requires `false` and both outputs null. Two additional positive parameter cases cover escaped player/users names with an empty array and explicit null optional strings. One new payload test covers escaped root/DTO names and IDs, newline/Unicode string decoding, preserved literal `\\u0075sers` and quoted array text, exact Int32 extremes and num values, null source slot, source count/order, bot independence and canonical player ownership over stale source payload. All prior migration, repair, prefab and lifecycle cases remain.
- `Assets/Neoxider/Docs/Tools/FakeLeaderboard/Leaderboard.md`: updated the canonical raw-import contract for decoded member names, ignored custom keys, optional fields, typed payloads and unchanged required-array rejection. Saved format, serialized/public API, GUIDs and Name/Score precedence remain intact.

Final current source SHA-256 after these edits:

| Path | SHA-256 |
|------|---------|
| `Assets/Neoxider/Scripts/Tools/FakeLeaderboard/LeaderboardLegacyJson.cs` | `93f39fbb46a0786743d3757e1b6a3bf43df9ca5a8183d9dbda2930c6c240ac57` |
| `Assets/Neoxider/Tests/Edit/Tools/FakeLeaderboard/Audit20261023NeoToolsFakeLeaderboardSerializationTests.cs` | `16e2062c81e937b00589de348b2aa2627fc64a74f75ce7d2f2595dcd28c4b84e` |
| `Assets/Neoxider/Docs/Tools/FakeLeaderboard/Leaderboard.md` | `1dda9fd9ad4a2a9c470a714e7253b55a55e7843bad3ba67f9f0d4bad128d2cd4` |

Pre-correction hashes were respectively `a14b352a2b8f7c27d171b40c4f9b86d0fddd4c7d11b248745a68bb6fd55fee70`, `ee9f5d4f90a72a693881d9e116c9cae7ad970355093fa8882d6269254468cab2` and `6cb5d2ffd3f24681b411da5ec816fa03efd9008026a30cebe6569d5798c1a66f`. A manual diff against the frozen P5 files reviewed the bounded runtime replacement and additive fixture changes; no existing fixture case/assertion was removed or weakened. These source reads and hashes are not compiler or test results.

### Ownership, validation performed and unresolved native evidence

Read `runtime-fixture-relocation-reservations.json` before each edit; it reports primary `neo-audit-1023-b03-04` source-final and no remaining extraction dependency. None of the four edited codec/fixture/doc/receipt paths is reserved. The map read has SHA-256 `14b7a974c8a8a861a666b65d43971f42e4f97b21c01734daacd466f1e88fc3a7`. No Fixtures helper/shared reference, primary scanner, shared Save parser, other assembly, shared tracker, original audit/plan or metadata was edited. The private serialized DTO wire fields remain lowercase; effective-visibility scanner ownership remains with primary. There is no pending cross-owner source dependency for E13.

Actual work was cmd.exe/Python file reading, historical XML/JSON inspection, source-diff review, scoped patches and byte hashing. No Unity/compiler/test/product-checker/install/service/commit/push/child actor was started; the current parser and fixtures were not executed. Current source is **not** claimed to pass either engine, and the historical 63-pass selections do not validate this correction.

After all writers finish and root records a new freeze, the native operator must compile on both Unity 6.3/6.6 and rerun E13 plus the complete serialization fixture, preserving all existing positive/negative/count/ownership/payload oracles. Decoded field binding, exact escaped string/integer payloads, valid empty arrays, duplicate/wrong-kind/nested/custom-key rejection and no partial outputs remain unresolved native acceptance for the current source. Broader aggregate/prefab/identity/lifecycle integration also remains root's verification responsibility. No lifecycle bypass, tolerance/default adjustment, error whitelist, ignore or lost case was used. Source writes stop with this receipt.

## S3 recovery: actual failure and completed correction

Root reported that the preceding S3 source attempt ended with CLI exit **3**. The older source-final receipt was therefore not completion evidence for this action. On resumption, `LeaderboardLegacyJson.cs` still had SHA-256 `46e91eedbf43612dca286ad6ab7d0f390819d297a24cfa5c54fc9d7b1eb2be86`, matching the frozen failing source; the explicit-array gate was absent. This recovery performed the runtime/doc/fixture edits below rather than declaring the old delivery complete. Current read/edit/bookkeeping commands completed without a repeated resource/API failure.

Historical native evidence actually read:

| Artifact | SHA-256 / relevant outcome |
|----------|---------------------------|
| `Local/Audits/assembly-2026-10-23/unity/final/s3/u63/Receipts/22-full-edit-graphics.xml` | `7c6d24e11012ed5903fff8f24d603e8130ec4bcae262af3761f4cf772a6e57d2`. FakeLeaderboard selection: 43 passed, 1 failed on that frozen source. |
| `Local/Audits/assembly-2026-10-23/unity/final/s3/u63/Receipts/21-full-play-graphics.xml` | `2b15216472bdc287418567ed947899346c6a134b33658b07533fa9a1d157dc3a`. No FakeLeaderboard test-case entries; this is not additional module pass evidence. |
| `Local/Audits/assembly-2026-10-23/full-edit-failure-action-index.json` | `df467fde346b0a1f7eca09a82b972f2e7532b7274758981969400628b1770e97`. Assigned group `E70`, same owner, action `J`, frozen fixture line 298. |

Exact failure: test-case `4842`, `Audit20261023NeoToolsFakeLeaderboardSerializationTests.LegacyRawJson_RejectedAggregatePublishesNoPartialOutput("{\"player\":{\"id\":\"p\"}}")`, expected `false`, actual `true`. The native `JsonUtility` materialized absent `users` as an empty list, so the DTO null guard did not establish wire-field presence. In the same native receipt, `LegacyRawJson_ReadsWritableIdTokensAndIgnoresDerivedSnapshot` and `LegacyRawJson_KeylessRowsStayIndependent` passed, establishing a real positive importer precondition before attributing this failure to production semantics. Those old results do not validate the new source.

### Current S3 source changes

- `LeaderboardLegacyJson.TryDeserialize` now structurally checks one explicit root `users` array before the existing DTO conversion. It uses the standard .NET `JsonReaderWriterFactory` / disposable `XmlDictionaryReader`, skips entire values, verifies the complete object, and returns null outputs for malformed shape or JSON. No substring/regex presence test, new parser framework, optional UI dependency, package or shared asmdef change was added. Existing generated `Neo.Tools.FakeLeaderboard.csproj` references include `System.Runtime.Serialization.Json`, `System.Runtime.Serialization.Primitives` and `System.Xml.ReaderWriter`; this is reference inspection, not compilation evidence.
- Valid `users:[]` remains accepted. Nested names, quoted text, `null`, string/object/scalar values, a non-object root and malformed trailing content do not qualify. Repeated root `users` members are rejected as ambiguous rather than relying on last-wins DTO behavior. Existing typed player/null validation and optional player ID/name/score/num are unchanged; wire keys remain lowercase.
- The original four rejected-input cases, all existing assertions and the positive migration/identity/prefab cases remain. Every rejection case now first imports a valid explicit empty array and observes its player ID and empty collection, so an importer that rejects everything cannot pass the negative matrix. Added cases cover nested/string decoys, wrong kinds/case/root, duplicate members and malformed bodies/trailing content. Added positive cases cover explicit empty arrays, optional player fields, ordering/whitespace/nested metadata, and an escaped root key with exact user identity/name/score/num/count assertions.
- The owned canonical `Assets/Neoxider/Docs/Tools/FakeLeaderboard/Leaderboard.md` documents explicit root-array presence, empty-array validity, structural validation and unchanged player optionality. Public/serialized APIs, Name/Score saves, settings, GUIDs and prior persistent-identity edits remain intact.

Current changed-source SHA-256, read after the completed edits:

| Path | SHA-256 |
|------|---------|
| `Assets/Neoxider/Scripts/Tools/FakeLeaderboard/LeaderboardLegacyJson.cs` | `a14b352a2b8f7c27d171b40c4f9b86d0fddd4c7d11b248745a68bb6fd55fee70` |
| `Assets/Neoxider/Tests/Edit/Tools/FakeLeaderboard/Audit20261023NeoToolsFakeLeaderboardSerializationTests.cs` | `ee9f5d4f90a72a693881d9e116c9cae7ad970355093fa8882d6269254468cab2` |
| `Assets/Neoxider/Docs/Tools/FakeLeaderboard/Leaderboard.md` | `6cb5d2ffd3f24681b411da5ec816fa03efd9008026a30cebe6569d5798c1a66f` |

### Reservations, scanner coordination and remaining native debt

The reservation map was read before each edit. It reports primary `neo-audit-1023-b03-04` source-final, no remaining extraction dependency, and none of these four edited runtime/fixture/doc/receipt paths is in its reserved/changed/helper file lists. No Fixtures helper, caller extraction, shared reference, primary source-rule fixture, vendor target, shared tracker or other actor's source was edited. No owned work remains deferred for helper extraction.

The S3 `ModulePrinciplesTests.RuntimePublicMembers_UsePascalCase` failure also reports the six lowercase fields inside the **private** `LegacyAggregate` / `LegacyUser` DTO types. These are serialized wire members, not effectively public APIs. Primary `Neo.Editor.Tests.implementation.md` reports its separate `E67` effective-visibility scanner correction. That primary-owned source fix and its future native verification remain with `b03-04`/root; this actor preserved all DTO names and added no public forwarders or suppression. This is the explicit cross-owner scanner handoff, not a local change to the source rule.

No Unity/compiler/test/checker/service/install/commit/push/child process was started in this recovery. Python was used only to read historical XML/JSON evidence, inspect reservations and compute artifact hashes. No product parser or fixture was executed. The old source-phase statements below are retained history, not post-correction receipts.

After ALL writers finish and root records a new freeze, the native operator must compile on 6.3/6.6 and rerun the exact E70 case, the expanded serialization fixture and effective-public-API scanner, retaining full positive/negative/payload/count oracles. New empty-array/escaped-key/structural-reader cases, cross-version BCL availability and all current integrated outcomes remain unverified. Existing S3 passes, including authored-token and aggregate/lifecycle cases in the Edit receipt, are historical evidence only. Source work is finished; stop writes and hand execution back to root.

## Initial 2026-10-24 implementation context — retained history

The following sections describe the original `LEADERBOARD-PERSISTENT-IDENTITY` source delivery and its then-unexecuted acceptance. They do not replace the S3 failure/correction record above.

Read [IMPLEMENTATION-CONTRACT](./IMPLEMENTATION-CONTRACT.md), the [assigned request](./Neo.Tools.FakeLeaderboard.implementation-request.md), [NativeReview-B-contracts](./NativeReview-B-contracts.md), and the existing audit/plan context. The [audit](./Neo.Tools.FakeLeaderboard.md) and [plan](./Neo.Tools.FakeLeaderboard.plan.md) remain historical evidence and were not rewritten. Earlier coherent ranking, save, view and movement fixes remain intact.

## Exact changes

| File | Work performed |
|------|----------------|
| `Assets/Neoxider/Scripts/Tools/FakeLeaderboard/Leaderboard.cs` | Retained `public readonly string id`; added `[SerializeField, FormerlySerializedAs("id")]` backing key and `PersistentId`; pure managed serialization callback copy; reference-first / unique-key canonicalization; duplicate/unresolved queries; explicit repair; derived snapshot rebuild; conservative Inspector handling of unresolved source data. |
| `Assets/Neoxider/Scripts/Tools/FakeLeaderboard/LeaderboardLegacyJson.cs` | New bounded reader for the old raw aggregate JSON `player`/`users` shape with writable `id` tokens. No general snapshot store or live-board loader. |
| `Assets/Neoxider/Tests/Edit/Tools/FakeLeaderboard/Audit20261023NeoToolsFakeLeaderboardSerializationTests.cs` | New aggregate, identity, repair, Inspector and real prefab acceptance sources. |
| `Assets/Neoxider/Tests/Edit/Tools/FakeLeaderboard/Audit20261023NeoToolsFakeLeaderboardLifecycleTests.cs` | Added a natural `Start` scenario proving that existing saved Name/Score overlay the canonical player before detached source rebinding. Existing lifecycle cases retained. |
| `Assets/Neoxider/Docs/Tools/FakeLeaderboard/README.md` | Canonical entry now explains persistent identity, explicit repair and the bounded legacy JSON API. |
| `Assets/Neoxider/Docs/Tools/FakeLeaderboard/Leaderboard.md` | Exact public contracts, reference/key precedence, unresolved-data behavior, repair semantics, old authored tokens/raw JSON, unchanged save precedence. |

New package metadata:

- `LeaderboardLegacyJson.cs.meta`: `aa4ee4a8f0464087be3de975ed079efa`.
- `Audit20261023NeoToolsFakeLeaderboardSerializationTests.cs.meta`: `16a8331cd2184fbfb392b3fcb48c1c05`.

No other actor's source/tests/docs, existing prefab/scene, asmdef, package/version configuration, shared tracker or old audit/plan was edited. The existing legacy `FakeLeaderboardBehaviorTests.cs` has concurrent owner changes and was left untouched. Centrally removed NUnit annotations were not restored; no new `[NonParallelizable]` annotation was introduced.

## Additive API and resolved contract

```csharp
string LeaderboardUser.PersistentId { get; }
bool Leaderboard.IsPlayerIdentityResolved { get; }
bool Leaderboard.HasDuplicatePlayerKey { get; }
bool Leaderboard.TryBindPlayerEntry(int sourceIndex);
bool LeaderboardLegacyJson.TryDeserialize(string json, out LeaderboardUser player,
    out List<LeaderboardUser> users);
```

1. The constructor initializes the persistent key from the existing GUID. Serialization hooks only copy a known nonempty runtime ID into an absent key; no Unity object lookup, reflection assignment or ID generation happens in getters/sorting. Explicit generation/repair can initialize a key.
2. A canonical managed reference in `users` wins. Otherwise, exactly one distinct source object with a nonempty matching persistent key can be replaced with `player`. Its current name/score remain authoritative, including the existing SaveProvider overlay performed in `Start`. `sortUsers` is derived and rebuilt.
3. No matching key or several distinct matching rows causes no arbitrary replacement. Names, score, rank and position never supply identity. Ordinary synchronization may append the canonical player; the old unresolved rows survive. Duplicate-key status remains observable even when that canonical reference makes ordinary lookup safe. Same-object aliases are known references, not distinct key collisions.
4. `OnValidate` refreshes ranking but does not truncate/regenerate a nonempty unresolved/colliding source list. Explicit regeneration is still available. This prevents an Inspector refresh from erasing the very legacy rows that need intentional repair.
5. `TryBindPlayerEntry` validates player/source/index/non-null slot before mutation or events. A valid call repairs only the selected slot and sorts; it does not stage or flush save data. Missing/colliding keys can be initialized/renewed. All unselected source slots, names, scores and keys remain intact; ranks are recomputed normally.

### Deliberate explicit-repair details

If another source slot already contains the current canonical object, changing that object's key in place would also change that unselected row through the alias. Explicit repair therefore creates a new canonical copy, retaining name/score and the readonly legacy ID value, and gives that copy a fresh persistent key. The selected slot receives the new `board.player`; all other source references remain unchanged. Callers retaining the canonical object should read `board.player` again after this explicit operation. Ordinary synchronization never forks the canonical object.

This keeps intentional repair stable across the next aggregate roundtrip instead of leaving two newly detached source copies with the repaired player's key. Pre-existing repeated canonical references remain in the source list but are ranked once in `sortUsers`; no source slot is erased as inferred ownership.

## Migration scope and unchanged persistence

`FormerlySerializedAs("id")` is the authored-token recovery candidate. The shipped prefab still provides concrete old-token evidence (`405a428d-b02a-4479-a1c1-eeeb5adf44e8`, prefab GUID `433a7e0b84016c148888c749f1f360e1`), but nested Unity recovery is not declared passed merely because the attribute exists.

For explicit raw old JSON, `LeaderboardLegacyJson.TryDeserialize` uses a writable legacy DTO. It requires non-null `player` and `users`, reads optional name/score/num plus old `id`, ignores `sortUsers`, and reconnects one matching keyed source entry. Keyless rows remain independent. Malformed/missing aggregate data or several matching player-key rows rejects with both outputs null. It does not alter a live board, read saves, emit events, infer names, support a new snapshot schema or rewrite YAML. Install its outputs before `Start` when the existing save overlay should win.

The existing `<playerSaveKey>_Name` / `_Score`, load order, empty-key disable and provider flush lifecycle are unchanged. No `_Id` / `_Snapshot` save keys, automatic prefab resave or broad headless framework were added. The headless model/snapshot proposal remains a proposal in the historical plan.

## Authored acceptance — UNEXECUTED

The new serialization fixture contains 16 authored cases by source inspection:

- Whole player/users/sortUsers JsonUtility roundtrip, detached inline copies, rename/update, one canonical binding and an independent same-name opponent.
- Canonical reference priority over an earlier keyed copy; only that canonical row is highlighted.
- Duplicate-key preservation through ordinary synchronization, observable unresolved state, explicit repair preserving every other row, unchanged readonly ID value, and successful second aggregate roundtrip of the repaired data.
- Known canonical aliases rank once without deleting source slots.
- Keyless equal name/score/rank never matches automatically; explicit repair creates a durable key.
- Two invalid-index cases preserve source, key, snapshot reference and event count.
- Real SerializedObject/Inspector validation dispatch preserves ambiguous data despite a requested count change.
- Writable-token legacy JSON conversion and ignored derived snapshot; independent keyless JSON rows; four rejected-input cases with no partial output.
- Temporary task-owned prefab save/reload through `PrefabUtility`, plus loading the shipped legacy YAML prefab by GUID and observing its recovered key without saving that asset.

One additional lifecycle acceptance source enters Play Mode and lets normal `Start` apply the existing Name/Score keys before canonical rebinding; the same-name bot and source count remain unchanged and no new identity/snapshot key exists.

Temporary prefab creation/deletion occurs only when the coordinator later executes that fixture. No asset operation was executed during this source phase. Fixtures restore their owned objects, in-memory provider and Editor RNG where applicable; runner/world isolation across Play transitions remains the central operator's contract, not a claim supplied by an unavailable NUnit annotation.

## Actual review, dependencies and evidence debt

Performed: small source/doc/reference reads through `cmd.exe`, direct source-diff review, approved scoped edits, and Python UUID generation for new metadata. Package APIs reused include existing `SaveProvider`, random/formatting extensions, LINQ ordering and ordinary JsonUtility/PrefabUtility contracts. No independent domain identity primitive in the existing three-file inventory solves the serialized inline-reference problem; Unity StableId was deliberately not substituted for persistent record identity.

No Unity/MCP process, compiler, test runner, build, product validator/checker, installer, service, git commit/push or child actor was started. No current pass/discovery claim is made. Earlier audit static receipts do not validate this new source. Metadata collision, package/docs/UTF-8, compile and runtime checks remain pending behind the global writer barrier.

**Cross-owner dependencies:** none pending for source completion. Existing `Neo.Save`, `Neo.Tools.Managers`, `Neo.Extensions`, Unity.TextMeshPro and Editor TestRunner references suffice by source/reference inspection; no shared asmdef edit requested. Root should integrate their final concurrent changes before execution.

After all writers finish, the coordinator must compile/discover/run on the installed Unity 6.3 and 6.6 hosts and collect actual outcomes for aggregate JsonUtility, original-token prefab import, new prefab roundtrip, raw legacy JSON, SaveProvider precedence and existing ranking/movement regressions. Failed rename-token acceptance must return to this same actor; do not silently rewrite user assets or claim raw DTO coverage proves native authored-token recovery. No new version-specific branch was introduced. Main/user projects, sample layout and release configuration remain untouched by this worker.
