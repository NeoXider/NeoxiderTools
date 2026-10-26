# Native review B: final narrow contract recommendations

Date: 2026-10-23. Same native `gpt-6.1-sol`, reasoning `max`. This is read-only source/diff review before the common correction wave. Only this report is written; no runtime/test/public-document/asset/config changes, Unity/MCP, compiler, test execution or validation processes. Earlier B reports remain unchanged. All acceptance cases below are proposed, not executed.

## 1. FakeLeaderboard: persistent identity and honest legacy migration

Original owner: Neo.Tools.FakeLeaderboard / `neo-audit-1023-b07-01`. Current source is `Assets/Neoxider/Scripts/Tools/FakeLeaderboard/Leaderboard.cs`.

### Exact current types and serializers

- `LeaderboardUser:18–31` is a `[Serializable]` ordinary class with public `int num`, `string name`, `int score`, and `public readonly string id`. Its only explicit constructor generates a GUID for id. It is not a UnityEngine.Object.
- `Leaderboard.player:51`, `users:61` and `sortUsers:58` are ordinary public inline serialized fields: one LeaderboardUser and two List<LeaderboardUser>. There is no SerializeReference or aggregate serialization callback. Unity scene/prefab serialization stores inline copies, not the managed alias between player and a list entry; readonly id is excluded by the current field contract.
- The package's current regression uses `UnityEngine.JsonUtility.FromJson<LeaderboardUser>` for two separate user objects, then explicitly adds the canonical player object to users (`Audit20261023NeoToolsFakeLeaderboardBehaviorTests.cs:85–97`). That does not prove alias/identity after serializing the complete player/users aggregate. The code has no Newtonsoft JSON dependency or FakeLeaderboard aggregate JSON codec.
- `SavePlayerData/LoadPlayerData:338–365` use the Neo.Save.SaveProvider facade with only `<playerSaveKey>_Name` and `_Score`. No bot list, rank or identity is saved by these methods. FileSaveProvider's own JSON envelope is not a leaderboard aggregate serializer. Do not claim a new board snapshot is already being saved.
- IsPlayerUser:188–194 safely matches the canonical reference or a nonempty legacy runtime id. SyncPlayer:196–208 replaces the first recognized row or appends player. This prevents the old name-based corruption but cannot reconnect keyless inline copies, and a persistent field alone would not resolve duplicate-key ambiguity.

**Recoverable authored evidence exists:** `Assets/Neoxider/Prefabs/FakeLeaderboard/Leaderboard.prefab` still contains an old `id` token for player at line52, the users entry at line542, and sortUsers at line1023, all `405a428d-b02a-4479-a1c1-eeeb5adf44e8`. The file also retains per-bot IDs. This is static YAML evidence, not a Unity load result; do not declare all legacy authored identity irrecoverable or automatically rewrite this prefab.

### Minimum recommended change

Preserve the existing readonly field and all other public/serialized names. Add one serialized backing string and read-only property to LeaderboardUser:

```csharp
[SerializeField, FormerlySerializedAs("id")]
private string _persistentId;
public string PersistentId => _persistentId;
```

The current constructor sets the new key from its existing GUID. Getter and sorting never generate a new ID. A narrowly scoped initialization/serialization hook may copy a known nonempty legacy runtime id when the new key is empty, using pure managed data only. Never assign the readonly field through reflection or change its public type/mutability. External code can continue to read id, but PersistentId becomes the documented durable identity; do not promise equality after every legacy deserialization.

FormerlySerializedAs is the minimal candidate to recover the old serialized `id` token into the new field while leaving the ignored readonly declaration intact. Its actual nested prefab/scene and JsonUtility behavior must be exercised on supported Unity versions. Do not assume that rename metadata proves every JSON import path: for explicit raw legacy JSON containing `id`, a small `[Serializable]` legacy DTO with a writable `string id`, name/score/num and an aggregate player/users wrapper can read that token with JsonUtility and map it into the new key. No YAML regex rewrite or general snapshot framework is required. Do not use Unity StableId for these ordinary persistent records.

Canonicalization belongs at the board/aggregate boundary, after player load and before normal synchronization/sort. Recommended evidence order:

1. A known canonical managed reference already in users is authoritative. The same object repeated is a known alias, not a same-name guess.
2. Otherwise accept exactly one users entry with the canonical player's nonempty PersistentId. Replace that slot with canonical player, keeping the player data already loaded from the existing SaveProvider keys. Ignore sortUsers as a derived snapshot and rebuild it; do not count its copy as another source user.
3. No recoverable key means no automatic identity match. Generate independent keys for independent unbound rows only through an explicit initialization/serialization step. Preserve all data; a canonical player may be appended as the current conservative behavior already does. Name, score, num/rank and coincident position are not identity evidence, even if a name currently appears unique.
4. Several distinct source rows with the same candidate key are ambiguous/corrupt input. Do not FindIndex-and-overwrite the first, erase rows, rename bots or highlight all duplicates as the player. Keep the original rows and expose an unresolved/duplicate-key outcome; until explicit repair, identify only the canonical object reference. A dedicated JSON load can reject such input before replacing the current board.

Use the original plan's additive `bool TryBindPlayerEntry(int sourceIndex)` for explicit legacy repair: a valid selected users slot is replaced with the canonical player, preserving its loaded name/score and every other row. Invalid index causes no writes/events. If colliding keys prevent a unique link, this explicit repair may allocate a fresh canonical key for player and the selected slot, leaving all other rows intact; that intentional identity repair must be observable/documented. No matching by display name. `GenerateUserList()` remains an explicit destructive regeneration option; `Sort()` alone is not an identity migration.

The minimum fix does **not** need new SaveProvider identity or snapshot keys: authored canonical identity lives in player/users serialization, while existing saves only overlay name/score. Preserve `_Name`/`_Score`, their load precedence, empty-key disable and provider flush lifecycle. A future `_Snapshot`/`_Id` format needs its own version/precedence/atomic-load contract; do not add it merely to fix inline aliasing.

Later acceptance must roundtrip the **whole aggregate** with the same player initially present in users, then rename/update and observe exactly the canonical player without touching a same-name bot. Include real Unity prefab save/reload plus JsonUtility wrapper roundtrip, legacy YAML with old id, legacy JSON with old id, keyless and duplicate-key data, explicit binding and invalid binding. Verify ordinary sort/rank and old `_Name`/`_Score` saves unchanged. The current separate-object test is useful but insufficient. No automatic asset migration is authorized by this report.

## 2. Money: one named remote-mutation authority policy

Original owner: Neo.Shop / `neo-audit-1023-b06-01`; helper/enum owner Neo.Network `neo-audit-1023-b04-05` and Neo.Network.Contracts `neo-audit-1023-b01-03`. Current `Assets/Neoxider/Scripts/Shop/Money.cs` was read completely with NetworkSingleton, NeoNetworkState.IsAuthorized and the public NetworkAuthorityMode declaration.

### Current flags, policy claims and actual ingress

Money declares public `isNetworked=false:42`, `_persistMoney=true:58` and `_maxMoney=0:63`. There is no allow-client-rewards, allow-client-set or authority-mode field. NetworkSingleton.HasServerAuthority exposes local role only; it does not enforce sender permission automatically. `_dontDestroyOnLoad`/`_setInstanceOnAwake` are singleton ownership settings, not economy authority.

The Money tooltip declares networked mode shared globally and false local; Money.md describes server-confirmed spending, and NetworkSingleton.md:49 recommends authority for economy mutations. The Shop plan explicitly identifies missing reward/set authorization. Numeric guards already reject invalid deposit payloads, but current CmdMoneyOp:685–713 is `[Command(requiresAuthority=false)]`, does not check isNetworked or sender authorization, and accepts Add/AddOverflow, both level accumulator mutations, SetMoney and SetMoneyForLevel. A permitted peer can therefore request its own credit/set amount. Denying only Add/SetMoney would leave the level-accumulator→payout route open.

Unknown MoneyOp values also reach ExecuteOp's no-op switch, consume the rate budget and are broadcast; RefundSpend=7 is already correctly rejected at command ingress. Mirror's actual OnCommandMessage verifies readiness/spawn and only enforces object ownership when requiresAuthority=true, so those generic checks do not supply the missing Money policy.

### Narrow recommendation following root's selected direction

Use **one** `[SerializeField] NetworkAuthorityMode _clientMutationAuthority = NetworkAuthorityMode.ServerOnly`, with an optional typed `ClientMutationAuthority` property and a clear server-side Inspector label. Do not add competing reward/set/owner booleans. The enum is already public in `Scripts/NetworkContracts/NetworkAuthorityMode.cs`: None=0, OwnerOnly=1, ServerOnly=2; numeric values must not change. Neo.Shop.asmdef already references both Neo.Network.Contracts and Neo.Network, so no new reference is needed for this design.

Enforce the policy only at the server command boundary, before rate limiting or any ExecuteOp/SyncBalance/Rpc:

1. Server's isNetworked must be true.
2. Opcode must be one of the client-requestable existing values0–6. RefundSpend7 remains forbidden regardless of policy; all unknown bytes are rejected, with no broadcast/budget use.
3. Keep existing finite/sign/affordability validation. Do not silently change ordinary server AddLevelMoney/SetLevelMoney adjustments, reward caps, spend results or SetMoney clamping.
4. Policy value itself must be recognized. **Fail closed on an unknown enum value before calling the helper:** current NeoNetworkState.IsAuthorized defaults to true for an unknown mode.
5. Apply existing `NeoNetworkState.IsAuthorized(gameObject, sender, _clientMutationAuthority)`, then the existing per-connection rate limit and operation checks. No permission is gained from client-side inspector changes; only the server component's field decides.

| Policy | Server ingress meaning |
| --- | --- |
| ServerOnly default | Reject remote mutations, including Spend and all reward/set/level operations. Direct server/host APIs remain normal. |
| None explicit migration | Cooperative/trusted-client compatibility: known numerically valid operations from any admitted sender, retaining current shared-wallet behavior. |
| OwnerOnly explicit | Permit the owner of **this Money GameObject's NetworkIdentity**, plus the helper's trusted server/host allowance. A scene wallet with no connectionToClient rejects remote callers. SaveKey, Money.I and sender.identity's unrelated player are not wallet ownership. |

None and OwnerOnly authorize a caller; they do not prove a reward was earned or validate a price/reward intent. Selecting them explicitly trusts the admitted caller with the existing amount-based operations. ServerOnly is the safe default for an untrusted-client economy. A later server-validated purchase/reward-intent adapter is separate work, not a prerequisite for rejecting unsafe default ingress.

### Host/null allowance and unchanged direct paths

Current NeoNetworkState.IsAuthorized:160–180 accepts null or the exact NetworkServer.localConnection for ServerOnly and OwnerOnly, and for remote OwnerOnly compares sender with the target NetworkIdentity.connectionToClient. Preserve that helper contract; do not broaden it to every connection with identity/player state or to every invocation executing on a server. HasServerAuthority is true inside server-side processing of **all** peers and cannot authorize the initiating peer.

Mirror supplies the actual NetworkConnectionToClient to the Command's sender parameter; it is not a client-selected serialized payload. Current `Assets/Mirror/Editor/Weaver/Processors/CommandProcessor.cs`, ProcessCommandInvoke/AddSenderConnection, emits the server-active check and loads the actual invoke connection argument for that parameter. A remote sender cannot become null/the local loopback merely by choosing amount/op. Null is trusted direct server-code context, and the exact loopback is the host allowance; preserve both when reusing this helper. Keep opcode/payload/policy checks even for null/host, especially the command refund ban.

Crucially, the ordinary public Money APIs **already bypass Cmd on server/host**: their forwarding condition is `isNetworked && IsClient && !IsServer`. Direct server/host Add/Set/Spend/level/refund executes local mutation plus its current sync/RPC path. Therefore no special new null bypass is needed to preserve those APIs. Do not put a blanket HasServerAuthority guard on the public APIs: that would also break intended offline/local wallets in a process with Mirror installed. False-isNetworked local behavior and no-Mirror behavior remain unchanged.

This is an intentional 11.0 migration for pure-client networked requests. Existing assets/new components with the new field absent must demonstrably receive ServerOnly, not enum-zero None; test actual serialized asset import rather than assuming a missing field's initializer behavior. Explicitly choose None for legacy cooperative scenes or OwnerOnly on owned wallets. Current client methods have no correlated rejection acknowledgement: TrySpend can still report RequestedServerAuthority, and void reward APIs return without local mutation. Document that default permission can reject those requests; do not add local fallthrough or claim a new purchase/reward receipt exists.

Later acceptance: invoke actual woven ingress for every opcode with ServerOnly/None/OwnerOnly, foreign owner/unowned identity/local host/trusted null, false isNetworked, unknown op and unknown policy. Denial must leave CurrentMoney/AllMoney/LevelMoney/LastChange/persistence/events/RPC and rate budget unchanged. Ordinary offline/server APIs, cap/overflow/refund, pending-client classification and late-join/RPC behavior remain separate assertions. No networking execution occurred here.

## 3. GM: confirmed stale notification and minimum generation fence

Original owner Neo.Tools.Managers / `neo-audit-1023-b07-06`. Root explicitly accepted the generation correction as mandatory during this review. `Assets/Neoxider/Scripts/Tools/Managers/GM.cs:43–69` publishes OnStateChange synchronously inside the setter; Pause:202–203 invokes EM.Pause only afterward. EM helpers resolve and synchronously invoke their UnityEvents.

Concrete current trace: State=Game, timeScale0.5; a one-shot OnStateChange(Pause) listener calls Resume. Pause captures0.5/sets0, setter publishes Pause, nested Resume restores Game/time0.5 and publishes Resume, then the outer Pause unconditionally publishes Pause. Final state/time look correct while a listener driven by the last Pause/Resume event can end paused. The new PauseStateListener regression checks only state/time, so it does not observe this trace. The reverse nested Resume→Pause can similarly leave a stale Resume tail.

**Minimum correction:** maintain a private nonserialized state revision and capture the revision belonging to the requested transition **before** invoking any external OnStateChange listener. After it returns, publish the matching EM.Pause/Resume only if the same revision still owns the state and the manager is alive. Checking final State alone fails for Pause→Resume→Pause; capturing the current revision only after the setter returns also fails because it captures the nested transition's revision.

A small private state-assignment helper can implement the existing setter behavior and return its locally captured revision. The public State setter delegates to it; Pause/Resume use its returned ownership token to fence their tails. Increment on distinct state changes; preserve documented same-value OnStateChange publication without overwriting `_lastState`. A same-value reassignment is not a different transition. Invalidate on destruction or check actual Unity lifetime before the pending tail. No public API, serialized state, queue/controller or new scene wiring is required.

Keep current time effects before notifications, exact zero/other captured time restoration, event-only mode, duplicate Pause no-op and `_lastState` semantics. The reverse Resume fence is part of the same minimal fix. Other GM methods have similar state→EM tails; reuse the primitive where a callback demonstrably supersedes their transition, but do not claim universal nested-transition safety from only Pause/Resume. Deliberate multi-stage Menu/StartGame flows need their own ownership checks rather than a global queued-state redesign.

The fence suppresses an **unstarted** stale event batch. It cannot retract a UnityEvent already in progress or stop its remaining listeners when its first listener requests another state. Preserve synchronous UnityEvents; do not advertise atomic observer delivery or cancel individual serialized listeners. That boundary belongs in documentation alongside the new tail-abandonment rule.

Later acceptance must assert complete event traces and observer state/time: Pause-state listener Resume produces no trailing Pause; Resume-state listener Pause produces no trailing Resume; A→B→A gives only the latest transition's tail; same-state notification preserves Resume target and normal one Pause/one Resume; destroying the manager from OnStateChange emits no late tail; a throwing callback leaves no guard stuck. Keep the existing exact time-scale tests and add event assertions rather than replacing them. No test was executed or changed.

## Handoff

Recommended narrow work: persistent ID plus safe legacy-token/explicit binding migration; one server-enforced NetworkAuthorityMode on Money defaulting ServerOnly; private GM revision fencing. Root decides the remaining deliberate migration details and routes implementation to the same original owners. This report authorizes no asset rewrite, new snapshot subsystem, broad economy transaction framework or queued state machine. Only `Docs/Audits/2026-10-23/NativeReview-B-contracts.md` was created.
