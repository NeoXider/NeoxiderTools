# Native implementation review 38 — two source3 fixture corrections

Independent native C review, same session; **source-only**. Scope: completed `Neo.Core.Level.Data` and `Neo.Rpg.Network` source3 corrections. Root supplied stopped original actors and pinned source-final receipts. Other writers remain outside this review. Only this report was written; no Unity, compilation, tests, parser/checker, product/config edit, process operation or child agent was used. This review does not cover the reviewer's own Local asset-maintenance helper.

Comparison baseline: `Local/Audits/assembly-2026-10-23/unity/snapshots/final-20261024T002340Z/source`, manifest SHA-256 `2d12ae058b989ce064e86a8d7db8223ecb65241d5dcd19babfb2fd7082adfc70`. Repository base: `D:/Git/NeoxiderTools`.

## Complete artifact reads and current fingerprints

Both implementation receipts were read completely, along with both complete current fixtures, every source3 diff hunk and the relevant existing provider/Mirror methods. No test presence or author declaration was treated as a current execution pass.

| Current artifact | Lines / bytes | SHA-256 |
| --- | --- | --- |
| `Docs/Audits/2026-10-23/Neo.Core.Level.Data.implementation.md` | 117 / 13417 | `6b870cddd98788b68a0d727f7c66536c78f59273d7ff6a4e49171bc5bcd41a80` |
| `Assets/Neoxider/Tests/Edit/Level/Audit20261023NeoCoreLevelDataTests.cs` | 198 / 8263 | `b4753e7fd6928e3680bd5637579b669b901fc0a4e77e4037609f29dcb857dd17` |
| `Docs/Audits/2026-10-23/Neo.Rpg.Network.implementation.md` | 217 / 23463 | `6ef348e034671aaa95e58808a3d554eddb3f15625bcee1346dbea76c2e6e42f3` |
| `Assets/Neoxider/Tests/Play/Network/Audit20261023NeoRpgNetworkRegressionTests.cs` | 561 / 25811 | `329c84c95d07216fd7a0f7e26ec5c77a313389ed8b9e40b88937fef75b46b098` |

Frozen fixture hashes respectively: `6dfde6b543707b27ffe605370d0abbd9e8cfbff6e8ef893de0204824d6fe9877` and `db24665a0a5399dab1fb7b75ee80980bb92809547e58a2fccd4dc397373617fe`. These distinguish corrected sources from the actual earlier executions described in the receipts.

## Neo.Core.Level.Data — source-supported

The only fixture-body change is `Curve_ScanLimit_HasNoNextLevel`, lines 129–140. The old zero-tangent flattened Hermite arrangement is replaced directly with keys `(1,0)`, `(10000,9999)`, `(10001,10000)`, each having explicit in/out tangents1. Both unit secants/tangents describe the intended linear control `XP=level-1`; no helper, component, Ignore or production-policy accommodation was added.

The decisive oracles remain exact: `EvaluateLevel(9999)==10000` and `GetXpToNextLevel(9999)==0`. New public preview assertions require level10000's threshold9999 and level10001's threshold10000. Thus the curve still authors a real next level **beyond** the scan cap; it was not shortened to make zero remaining XP vacuous. Existing `LevelCurveDefinition.cs:127–180` still scans only up to10000 and returns no next XP at that cap; its public preview at lines206–227 still evaluates an authored level outside the scan window.

All11 existing test methods/20 authored cases, including fractional requirements/endpoints, large integer boundary/subtract-before-saturation, explicit max, empty curve and null Custom table controls, remain. Setup/teardown still owns and destroys the temporary ScriptableObject. No assertion was weakened to a range or arbitrary tolerance. The receipt truthfully separates its earlier19/20 native result from this uncompiled correction. Native interpolation itself is not executed or certified here.

Disposition: **CORE-LEVEL-CURVE-SCAN-CONTROL source correction supported; no bounded residual identified.**

## Neo.Rpg.Network — source-supported

Only three owned fixture hunks changed relative to source3:

- `CreateCharacter`, lines409–421, makes the newly created empty object inactive before adding its explicit NetworkIdentity, RpgCharacter and adapter. It assigns the real public `Template` and intended networking flag before one activation of the completed graph. The old eager ApplyTemplate/rebuild path is removed from this ordinary factory. Exact `adapter.netIdentity Is.SameAs(identity)` is asserted after activation. This uses native lifecycle, without manually invoking Awake or patching Mirror caches.
- `DispatchCommand`, lines445–464, writes both int parameters through `writer.Write<int>`. The same five fields, order, signature/hash, injected sender and registered receive delegate remain; the exact `reader.Remaining==0` assertion remains.
- `SpawnCharacter`, lines527–534, adds the same identity oracle before the existing assetId/server-spawn/isServer/nonzero-netId checks. No other body or lifecycle branch changed.

The lifecycle arrangement matches the actual frozen vendor source: `Assets/Mirror/Core/NetworkIdentity.cs:41–44` has execution order-1, `:297–314` caches the complete NetworkBehaviour graph and assigns each netIdentity, and `:333–339` does that once in Awake. The provider's frozen `RpgCharacter.cs:153` Template setter merely stores the field; `:204–214` initializes through Awake/OnEnable. Configuration therefore precedes native initialization and cache construction. The deliberately inactive first-initialization source branch at fixture233–239 remains inactive through facade initialization and is activated only at its existing286–288 point.

The serializer change matches the actual Mirror selection mechanism, rather than hardcoding a guessed encoding: `NetworkWriterExtensions.cs:31` is fixed-width WriteInt, while `:46` marks WriteVarInt with WeaverPriority; `NetworkReaderExtensions.cs:51` supplies the matching prioritized reader. `Editor/Weaver/Writers.cs:34–58` prefers that registration and `:328–357` assigns it to Writer<T>.write. `NetworkWriter.cs:221–232` invokes that registered delegate from Write<T>. Production command signature/wire policy is unchanged; successful native weaving remains required.

All18 MIRROR-on authored cases remain, including the existing pre-Awake case; MIRROR-off retains that same local case. Full-body reads confirmed retention of networking opt-out/toggle, disabled-command budget, trusted-state opt-in and same-frame rate controls; both init/rebuild branches and intermediate-payload/event order; source/receiver HP130/Max160, BaseMax100, percent/regen, resource/stat/XP/points, buff identity/no grant replay/no death and ordinary-refresh checks; all five growth-anchor modes; projectile identity-fallback/local-object controls; exact snapshot/command consumption. The new identity checks strengthen setup without replacing substantive oracles. Owned object/asset/save-key teardown, session shutdown and previous Transport restoration are unchanged.

Disposition: **G04–G06 fixture corrections source-supported; no bounded residual identified.** Dummy transport/delegate invocation and public client-lifecycle replay remain single-process tests, with no new claim of remote delivery, peer authority, reconnect or separate-client acceptance.

## Unchanged production, documentation and metadata

Current bytes equal frozen source3 for all entries below; prior broader source coverage is retained rather than repeated as a whole-module audit.

| Artifact | SHA-256 |
| --- | --- |
| `Scripts/Core/Level/Data/LevelCurveDefinition.cs` below Assets/Neoxider | `7dc74c2fd574a81f173af1387537a79eea886c213af55f8b00705949027ab9e9` |
| `Scripts/Core/Level/Data/LevelCurveEntry.cs` | `a99d988916153baca18f0a2bc0d438ce5eade964a9fe58e5cf1148827b0c6b1d` |
| `Scripts/RpgNetwork/RpgAttackNetworkAdapter.cs` | `a9e143727c687e901abefaea8f175d76957049baa30da1884652d65057de9163` |
| `Scripts/RpgNetwork/RpgCharacterNetworkAdapter.cs` | `41d917bed933ae330e35bec822df5e103b60786667284c75ea12753479c660cb` |
| `Docs/Core/Level/Data/CurveEvaluation.md` | `cc603a74168c5ca0a950732407c5b364b0fb564974b237649ba6dc8233638578` |
| `Docs/Rpg/Network.md` | `8f6817a269856155a53dde0192c54e0ad1f40482e298572210e781f39eacbe34` |
| Data fixture `.cs.meta` | `15599683dd5fcd7ddcd58cdcc6c9b420e9cca054335bc5f6eab11fd04553d5b3` |
| RPG Network fixture `.cs.meta` | `565abaec7d1f8055e32f80c226cc3c53f511c613fa0d8f3b4586835866e7485e` |

## Evidence boundary

Both assigned corrections are **approved at source level only** within this bounded review. No additional confirmed defect or acceptance weakening was found. Closing reads retained the four current artifact hashes above. The receipt-recorded old Data19/20 and RPG Network7/18 results remain historical failures, not corrected-source passes. Fresh compilation/weaving, nonempty discovery and all20/18 cases on the installed Unity versions must follow the global writer barrier/new root freeze. Shared fixture integration and separate-peer evidence remain centrally owned; this report grants no execution, copyback or global release approval.
