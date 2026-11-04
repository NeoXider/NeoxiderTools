# RPG networking

**What it is:** the optional Mirror transport for `RpgCharacter` (`RpgCharacterNetworkAdapter`, replicated
profile snapshots and server commands) and `RpgAttackController` (`RpgAttackNetworkAdapter`, input authority
and projectile spawning), assembly `Neo.Rpg.Network`.

**How to use:**
1. Add `RpgCharacterNetworkAdapter` beside a networked `RpgCharacter` and enable **RpgCharacter → isNetworked**.
2. Add `RpgAttackNetworkAdapter` beside an `RpgAttackController` whose projectiles must spawn on the server.
3. Run gameplay on the server; clients receive one complete snapshot per changed step. Group several mutations
   with `RpgCharacter.DeferSnapshotPublication()` (see [Snapshot publication](#snapshot-publication)).

---

`Neo.Rpg.Network` is the optional Mirror transport for the local [RpgCharacter](RpgCharacter.md)
runtime and [RpgAttackController](RpgAttackController.md). Local RPG gameplay needs no adapter.
The transport-neutral interfaces remain in `Neo.Rpg`; gameplay assemblies need a reference to
`Neo.Rpg.Network` only when they directly use its concrete adapter types.

## Character setup

1. Configure `RpgCharacter` resources, stats, effect catalogs, and persistence as usual.
2. Add `RpgCharacterNetworkAdapter` to the same object. With Mirror, `NetworkIdentity` is required
   automatically. Configure and register the network prefab through your normal Mirror workflow.
3. Enable **RpgCharacter → isNetworked** before spawning the character. This is the character's
   networking switch; do not configure the adapter's inherited `isNetworked` field independently.
4. Keep **Allow Client State Commands** disabled for server-authoritative gameplay. If a legacy game
   explicitly requires client state commands, enable it and choose the character's `AuthorityMode`.
   `OwnerOnly` validates ownership; `ServerOnly` rejects remote callers; `None` permits any sender.

The adapter copies the character switch during `Awake` and refreshes it before client startup applies
late-join state. Incoming commands and snapshots check the current character switch. Turning it off
lets the character run locally and prevents subsequent network snapshots from overwriting that local
state. Turning it back on does not itself request a fresh snapshot: configure it before spawn, or wait
for a subsequent server update.

## C# API

Use the normal character API for server gameplay. Existing `Net*` compatibility methods remain
available on both the character and its adapter:

```csharp
using Neo.Network;
using Neo.Rpg.Components;

RpgCharacter character = GetComponent<RpgCharacter>();
if (!NeoNetworkState.IsNetworkActive || NeoNetworkState.IsServer)
{
    character.NetDamage(25f);
}
```

The adapter exposes `NetDamage`, `NetDamageType`, `NetHeal`, `NetSpend`, `NetRefill`,
`NetApplyBuffById`, `NetApplyInlineBuff`, `NetApplyStatusById`, and `NetAddLevel`.
Local adapter `Net*` calls also resolve their required character on inactive objects before `Awake`.
Network commands still require a normally initialized, spawned Mirror object.

`IRpgCharacterNetworkAdapter.TryRoute(RpgCharacterNetworkCommand)` returns `true` when the request is
sent through the client transport, rather than executed locally. It is not an acknowledgment that
the server accepted the mutation. The server applies the character's opt-in/authority policy and the
inherited per-sender rate limit. Host, server, and offline calls execute locally once.

The command type travels as a raw integer. The server drops a value that is not a named
`RpgCharacterNetworkCommandType` (`RpgCharacterNetworkCommand.IsKnownType`) after the rate-limit and
authority checks, so it neither executes nor publishes a snapshot. Numeric payloads go through the same
character validation as local calls: NaN or infinite damage, healing, costs, Max values, XP and stat bases
are rejected (see [RpgCharacter](RpgCharacter.md#non-finite-values)).

`RpgCharacter.ApplyLevelGrowth()` explicitly applies the current level's configured growth. Its
`RpgCharacterNetworkCommandType.ApplyLevelGrowth` command uses the additive value **23**, after
`SetInvulnerable = 22`. The adapter dispatches it through the same character opt-in, authority and
rate-limit checks as other trusted state commands. Use the character method from server gameplay,
or deliberately enable the legacy client path. Calling it on a pure client does not bypass admission
or acknowledge that growth was applied. Peers must support this additive command; no protocol
negotiation or acknowledgment was added.

`NotifyStateChanged()` publishes a serialized `RpgCharacterProfileData` on the server when character
networking is enabled. The character calls it once per completed step (see
[Snapshot publication](#snapshot-publication)); an executed adapter command publishes exactly once. `SuppressLocalSimulation` suppresses client regeneration and effect simulation
for networked characters; authoritative state comes from snapshots. Offline calls use the local API,
including when Mirror is installed but no network session is active.

## Snapshots and late join

The adapter replicates the complete profile, including resources, stat bases, progression, active
effects, death, and invulnerability state. Initial client startup and later SyncVar updates apply the
same payload. Configure matching resource/stat identifiers and effect catalogs on all peers; applying
a profile does not create missing resource definitions. Custom resource IDs are serialized as JSON,
including IDs containing punctuation.

Character-owned snapshot notifications are deferred during initialization, rebuild and profile
restoration. The final notification follows derived-state resolution and configured auto-load, so
the adapter captures the completed profile. `RuntimeRebuilt` still occurs **before** optional auto-load:
use it for rebinding runtime sources, and use the loaded/final state for persistence-dependent UI.
This publication boundary does not make individual resource reactive callbacks transactional.

### Snapshot publication

Every publication captures the whole profile and serializes it to JSON on the server, so the character
publishes once per completed step instead of once per changed value:

| Step | Publications |
|------|--------------|
| One frame of regeneration, buff/status expiry and status damage ticks | 1 (only when something changed) |
| `ApplyDamage` / `Damage` (a lethal hit changes HP and death) | 1 |
| `Heal` (including a revive), `Restore()` (every resource), `SetLevel`, `UpgradeStat`, `RefreshAllDerived` | 1 |
| One executed adapter command (`Net*` on the host/server, a remote `CmdExecute`) | 1, also for a no-op command |
| Initialization, `RebuildRuntime`, `ApplyProfile` / auto-load | 1 |

Before 12.0.0 a regenerating character serialized its profile once **per regenerating resource per frame**
(a per-tick regen after a long frame even once per delivered tick), and a lethal remote hit three times.

To group your own mutations, open a scope. Nested scopes, including the ones the character opens itself,
publish one complete snapshot when the outermost scope ends, and nothing when nothing changed:

```csharp
using Neo.Rpg.Components;

using (character.DeferSnapshotPublication())
{
    character.Damage(25f);
    character.AddXp(10f);
    character.ApplyBuffById("Rage");
}
// One snapshot with the final HP, XP and buff is published here.
```

Only the transport publication is deferred: UnityEvents, reactive states and `DamageApplied` still fire in
place, so UI on the server reacts immediately. Dispose each scope exactly once (prefer `using`); a second
`Dispose` of the same scope and a `default` scope do nothing. Custom `IRpgCharacterNetworkAdapter`
implementations get the same batching: `NotifyStateChanged()` runs once per scope.

### Version 2 resource and effect restoration

The payload remains version **2**. A known saved resource row restores its clamped **effective Max**
before Current, and percentage regeneration uses that restored Max. Missing rows follow the normal
resolver; unknown IDs do not create resources, and duplicate known rows use the last saved value.
For example, a snapshot of HP **130/160** restores HP **130/160**, percent **0.8125**, and 10%-of-Max
regeneration **16/s**, without temporarily clamping Current to the template's lower Max.

Saved effective Max does not become `BaseMax`. The override exists only for that restoration; a later
ordinary derived refresh recomputes Max from BaseMax, upgrades and active modifiers. A base100 actor
with a +50 Max buff can therefore restore saved Max160 and later resolve Max150. Use existing authored
upgrade investments when growth must survive ordinary refresh; durable resource-base fields/profile
v3 are a separate feature.

`AddResourceCurrentFlat` grants are produced by explicit accepted buff application/positive stack
increments. A network snapshot restores the resulting Current and active entries; it does not apply
the buff again, emit a fresh applied event, or grant Current again. Replaying the same snapshot must
preserve its balances. Effect removal or a normal derived refresh does not replay the grant either.

### Version 2 applied-growth limitation

Version 2 stores level and stat bases, but **does not store the applied-growth anchor**. On load or
snapshot application, `AllStatsEveryLevel` and `Hybrid` use the saved level only when
`autoApplyGrowthOnLevelUp` is enabled. `None`, `ManualUpgradePoints`, and deferred automatic modes
start from anchor1 instead. An explicitly applied deferred anchor on the server therefore does not
round-trip through this payload; the receiver can have different derived growth despite equal level
and stat bases. The adapter preserves this core policy and does not call `ApplyLevelGrowth` while
restoring a snapshot. Choose automatic growth when that reconstruction is required, or define an
explicit game policy for manual/deferred restoration; a pure-client growth call routes back to the
server through the same command path.

Profile configuration belongs in reusable data assets; the adapter belongs on the scene object or
network prefab. See [RpgCharacterNetworkAdapter](RpgCharacterNetworkAdapter.md) for serialized migration
from the earlier character component that inherited `NeoNetworkComponent`.

## Attack setup

Add `RpgAttackNetworkAdapter` beside `RpgAttackController`. It supplies the
`IRpgAttackNetworkAdapter` input and projectile policy:

- Both input authority and client projectile blocking resolve `NetworkIdentity` from the source's
  parent hierarchy, then fall back to the adapter's parent hierarchy. A separate local damage source
  therefore cannot bypass the network actor's projectile restriction.
- Client-only peers block network actor projectile creation. Server gameplay must initiate those
  attacks; this component does not forward attack requests to the server.
- `SpawnProjectile(GameObject)` registers server-created projectiles carrying `NetworkIdentity`
  with Mirror. Register their prefabs with Mirror as usual.
- Actors without an identity keep local input and projectile behavior. Without Mirror, input is
  allowed and projectile networking is inert.

## Scene and UnityEvent workflow

Existing UnityEvents targeting `RpgCharacter.Net*` remain valid. Wire damage, healing, resources, or
effect actions to those methods; adding the adapter supplies the transport without replacing the
character's typed API. Client UI should react to character reactive state/events after authoritative
snapshots arrive. A UnityEvent call cannot grant authority or bypass the server's networking switch.
UnityEvents can also target `RpgCharacter.ApplyLevelGrowth`, or use the matching `RpgNoCodeAction`
operation, through the same typed API. The version 2 growth-restoration policy above still applies.

For a purely local character, omit the adapters or leave the character networking switch disabled.
Mirror scene identities also follow the package's [optional networking lifecycle](../Network/README.md).

## Demo

The Network demo scene (`Samples~/Demo/Scenes/Network/NetworkDemo.unity`) has a server-authoritative
`Shared Boss`: `RpgCharacter` (template asset `Data/Network/NetworkDemoBoss.asset`, **isNetworked** on,
client state commands off) with `NetworkIdentity` and `RpgCharacterNetworkAdapter`. Its Hit and Revive
buttons trigger ServerOnly channels of a `NetworkActionRelay`; on the server those channels call
`RpgCharacterNetworkAdapter.NetDamage` and `RpgCharacter.Restore`, and every peer's HP bar follows the
replicated snapshot. See [Network demo scene](../Network/README.md#demo-scene).
