# RpgCharacter

**What it is:** the universal RPG character facade (`MonoBehaviour`, namespace `Neo.Rpg.Components`,
`Scripts/Rpg/Components/RpgCharacter.cs`, menu `Neoxider/RPG/RpgCharacter`). One component per player,
NPC, mob or pet: any number of resources (HP / Mana / Stamina / DarkMana / Rage / any `Custom`), any
number of stats, SO and inline buffs, status effects, Dota-like and Souls-like growth, Save/Load and an
optional network adapter. It replaces the legacy `RpgCombatant` + `RpgStatsManager` (removed in v8.4.0).

**How to use:**
1. Add `RpgCharacter` to the actor and assign an `RpgCharacterTemplate` (or fill `Resources`/`Stats` inline).
2. Call `Damage`, `Heal`, `Spend`, `ApplyBuffById`, `SetLevel`, `UpgradeStat` from C#, UnityEvents or
   `RpgNoCodeAction`.
3. Read state through `HpState`, `GetResourceCurrentState(id)`, `GetStatState(id)` or the
   `RpgResourceBinding` / `RpgStatBinding` UI components.
4. Optional: register `IRpgDamageModifier` hooks for custom damage rules, subscribe to `DamageApplied`,
   add `RpgCharacterNetworkAdapter` for multiplayer.

**Demo:** `Scenes/RpgCharacterQuickDemo.unity` in the Demo sample (`Assets/Neoxider/Samples~/Demo/` in the
release layout, `Assets/Neoxider/Samples/Demo/` while samples are being developed). The whole demo is saved in
the scene (authored by `RpgCharacterQuickDemoBuilder`, menu `Neoxider/Demo/Rebuild Active Demo Scene`), so the
wiring is visible without Play Mode: select `Rpg Demo Player` for its `RpgCharacterTemplate` asset, the
`Resource · HP` / `Stamina` / `DarkMana` rows for `RpgResourceBinding` events driving labels and bars, the
`NoCode ...` buttons for `RpgNoCodeAction`, and the `Condition · Low HP` row for a `NeoCondition` reading
`HpPercentValue`. In Play Mode every C# button logs the `RpgCharacter` call it makes; the Guard toggle registers
an `IRpgDamageModifier`, and enemy hits are reported through `DamageApplied`.

```csharp
RpgCharacter hero = GetComponent<RpgCharacter>();
hero.Damage(25f);
if (hero.Spend("Mana", 10f))
{
    hero.ApplyBuffById("Haste");
}

hero.HpPercentState.AddListener(percent => hpBar.fillAmount = percent);
```

---

## Architecture

```
RpgCharacter : MonoBehaviour, IRpgCombatReceiver, INeoOptionalNetworked
|-- RpgCharacterTemplate (SO, optional)     - start resources / stats / buffs / progression
|-- RpgResourceDefinition[] _resources      - HP / Mana / Stamina / Shield / any Custom
|-- RpgStatDefinition[]     _stats          - Strength / Defense / FireResist / any Custom
|-- BuffDefinition[]        _knownBuffs     - re-usable SO buffs
|-- InlineBuffEntry[]       _inlineBuffs    - one-off buffs without SOs
|-- StatusEffectDefinition[] _knownStatuses - DoT / Slow / Stun
|-- RpgCharacterResourceService (plain C#)  - resource state, mutations, queries + regen clocks
|-- RpgCharacterStatService (plain C#)      - stat runtimes, upgrade rules, invested points, resolvers
|-- RpgCharacterProgressionService (plain C#) - level / XP / upgrade points / applied-growth anchor
|-- RpgEffectShelf (runtime)                - single source of truth for buff/status lifetime
|-- RpgModifierMath (static)                - outgoing / incoming / movement / attack-speed multipliers
|-- RpgCharacterProfileService (plain C#)   - profile validation + serialization
`-- RpgProgressionDefinition (SO, optional) - Dota | Souls | Hybrid + upgrade rules
```

No singleton. Multiple characters per scene is a first-class scenario (player + party + pets + enemies).

`RpgCharacter` keeps the serialized authoring arrays and every established public method, reactive
property, and UnityEvent. At runtime it delegates state to plain C# services and keeps only orchestration
(publication order, re-entrancy guards, network routing, save/load):

| Service | Owns |
|---|---|
| `RpgCharacterResourceService` | resource pools, spend/increase/decrease/restore, derived Max/regen, pause windows, regen clocks; raises `ResourceChanged` |
| `RpgCharacterStatService` | stat runtimes, upgrade-rule lookup, invested points, `ResolveStat` / `ResolveResourceMax` / `ResolveRegen`, capture/restore rows |
| `RpgCharacterProgressionService` | level, XP, upgrade points and the applied-growth level, with overflow-safe arithmetic |
| `RpgEffectShelf` | active buffs/statuses, stacking, expiry, status ticks, modifier projection |
| `RpgModifierMath` | allocation-free aggregation of projected modifiers into combat multipliers |

All services are usable without a scene (for tests, servers or custom actors); the per-frame tick and the
damage path are allocation-free once warm, as long as the UnityEvent listeners themselves do not allocate.

---

## Universal ID

`RpgStatId` = `RpgStatPreset` + optional `customId`. The inspector shows a dropdown with common
values (`Hp`, `Mana`, `Stamina`, `Shield`, `Strength`, `FireResist`, ...) plus a custom-string field
that activates when you pick `Custom`. Same for buff target id.

```csharp
// 1. Preset
new RpgStatId(RpgStatPreset.Hp)            // value = "HP" (RpgResourceId.Hp)

// 2. Custom
new RpgStatId("DarkMana")                  // preset = Custom, value = "DarkMana"

// implicit conversions
RpgStatId id = RpgStatPreset.Stamina;
string key = id;                           // "Stamina"
```

---

## Public API (UnityEvent-friendly)

Single-argument shortcuts appear in UnityEvent dropdowns. Multi-argument methods such as
`Spend(string, float)` are intended for C# or a configured `RpgNoCodeAction`; choose that bridge
when a scene event needs both an ID and an amount.

### Damage / heal / resources

| Method | What it does |
|---|---|
| `Damage(float)` | HP using buff/status IncomingDamage% + Defense% modifiers |
| `DamageType(string, float)` | + specific resist (`FireResist`, `IceResist`, ...) |
| `ApplyDamage(RpgDamageInfo)` | full pipeline with the hit source; returns the HP actually removed |
| `AddDamageModifier(IRpgDamageModifier)` / `RemoveDamageModifier(...)` | code hooks into the damage pipeline (see below) |
| `Heal(float)` | heal HP |
| `Spend(string id, float)` | spend a resource; returns `false` when not enough or a positive cost produces no stored debit |
| `Refill(string, float)` / `Increase(string, float)` | top up a resource |
| `Restore()` / `RestoreResource(string)` | full restore |
| `SetMaxResource(string, float)` / `AddMaxResource(string, float)` | change effective Max; later derived refresh resolves from the unchanged BaseMax |

Changing HP through `Increase`, `Refill`, or `RestoreResource` also updates death/revival state.
`OnRevivedEvent` fires once when a dead character's HP becomes positive.

### Damage pipeline

`Damage`, `DamageType`, `ApplyDamage` and `IRpgCombatReceiver.TakeDamage` (used by attacks, projectiles
and contact damage) run the same steps:

1. Network routing (when `isNetworked` and an adapter is present); the hit source is not transmitted.
2. Non-positive or non-finite amounts and invulnerability reject the hit.
3. Buff/status multiplier for the damage type (`GetIncomingDamageMultiplier`).
4. Registered `IRpgDamageModifier`s in registration order. Each receives the original `RpgDamageInfo`
   (raw amount, type, source) and the running amount; negative results clamp to 0, non-finite results are
   ignored, destroyed component modifiers are skipped. Adding/removing modifiers during a hit affects the
   next hit only.
5. HP decrease. The decrease raises `OnResourceChangedEvent` at once, and a lethal decrease also raises
   `OnDeathEvent` at this point, before any damage event.
6. `OnDamagedEvent(applied)`, then the C# event `DamageApplied(info, applied)`, then the regen pause.
   A fully absorbed hit raises neither event. Kill-credit or "last hit" logic in `DamageApplied` must
   therefore expect `IsDead` to be already `true` for the killing blow.

```csharp
public sealed class ShieldAbsorb : MonoBehaviour, IRpgDamageModifier
{
    [SerializeField] private RpgCharacter _owner;
    [SerializeField] private float _shield = 50f;

    private void OnEnable() => _owner.AddDamageModifier(this);
    private void OnDisable() => _owner.RemoveDamageModifier(this);

    public float ModifyIncomingDamage(RpgCharacter target, RpgDamageInfo info, float amount)
    {
        float absorbed = Mathf.Min(_shield, amount);
        _shield -= absorbed;
        return amount - absorbed;
    }
}
```

Resource spending uses the same current-value clamp for admission and mutation. A positive cost rounded
away by float precision (100000000 minus 1) is rejected before reactive/resource events, regen pause or
transport publication. A positive stored decrement remains accepted even if rounded (cost 5 at that
balance stores an 8-unit decrement); ordinary fractional costs remain supported.

### NoCode shortcuts

`SpendMana(float)`, `RefillMana(float)`, `SpendStamina(float)`, `RefillStamina(float)`,
`SpendShield(float)` - explicit methods for the common pools, visible in UnityEvent dropdowns.

### Stats

| Method | What it does |
|---|---|
| `GetStat(string)` | final value = base + level + upgrade + buffs |
| `AddStatBase(string, float)` / `SetStatBase(string, float)` | menu / inventory; NaN, infinite or overflowing values are ignored |
| `UpgradeStrength()` / `UpgradeDexterity()` / `UpgradeVitality()` / `UpgradeIntelligence()` / `UpgradeEndurance()` | Dark-Souls UI |
| `UpgradeStat(string)` | universal upgrade |

Changing a stat base immediately refreshes resources that regenerate `FromStat`. Upgrade rules
require a positive cost and an existing runtime stat; invalid rules do not consume or create points.
Local `UpgradeStat` returns `true` once the original point debit is accepted. If an `UpgradePointsState`
observer replaces the runtime, that superseded call stops before investing or refreshing the replacement,
and does not refund into it. Ordinary successful upgrades still debit points, invest and refresh stats.

#### Non-finite values

A single NaN would make every comparison on a stat or resource false (never dead, never full) and spread to
every derived Max and percent, so the character never stores one:

- `Damage`, `Heal`, `Spend`, `Refill`/`Increase`/`Decrease`, `SetMaxResource`/`AddMaxResource` and `AddXp`
  reject NaN and infinite amounts.
- `AddStatBase`/`SetStatBase` (and `RpgCharacterStatService.AddBase`/`SetBase`) return without a change or a
  refresh for NaN, infinite or overflowing values; `RestoreBases` skips such rows of a corrupted save.
- `RpgResourceRuntime.SetCurrent`/`SetMax` and `RpgStatRuntime.SetCurrent` keep their previous value when a
  resolver produces NaN (for example from a NaN buff value in data). Infinity still clamps to the limits.

The same rules protect the server from client state commands (see [RPG networking](Network.md#c-api)).

### Buffs / statuses

| Method | What it does |
|---|---|
| `ApplyBuff(BuffDefinition)` | SO buff (from library) |
| `ApplyBuffById(string)` | by id (looks up SO and inline) |
| `ApplyInlineBuff(int index)` | inline entry from `_inlineBuffs[index]` (pickup buffs) |
| `RemoveBuff(string)` / `ClearAllBuffs()` |  |
| `ApplyStatus(StatusEffectDefinition)` / `ApplyStatusById(string)` |  |
| `RemoveStatus(string)` / `ClearAllStatuses()` |  |
| `HasBuff(string)` / `HasStatus(string)` |  |

Legacy damage, defense, typed defense, and movement buff aliases apply every clamped active stack.
`GetMovementSpeedMultiplier()` multiplies movement buffs by active status movement multipliers.
Movement controllers must read this value; applying a status does not move a Transform itself.

`HpRegenPerSecond` adds units/second to canonical `"HP"`, ignoring the modifier target ID. Its clamped
stacks compose with `RegenFlat` before `RegenPercent`; enabled/mode/alive/full/pause gates still apply.
`AddResourceCurrentFlat` delivers a signed raw resource delta once per newly accepted effective stack,
after derived limits and before `OnBuffAppliedEvent`. Nonstack/capped refresh, removal, expiry and
profile/snapshot restore deliver no grant. Both SO and inline buffs follow this contract.
`AttackSpeedPercent` is exposed through `GetAttackSpeedMultiplier()` (`1 + percent / 100`, clamped at 0).
It changes cooldowns only for an `RpgAttackController` with **Scale Cooldown By Attack Speed** enabled
(off by default, so existing content is unchanged).
See [buff contracts and callback behaviour](Data/BuffDefinition.md).

### Level / progression

| Method | What it does |
|---|---|
| `SetLevel(int)` / `AddLevel(int)` | raises level. `AllStatsEveryLevel` auto-applies growth; `ManualUpgradePoints` grants upgrade points |
| `ApplyLevelGrowth()` | explicitly applies configured growth at the current level, including deferred/manual modes |
| `AddXp(float)` | positive finite amounts only; NaN/Infinity are ignored |
| `AddUpgradePoints(int)` | saturates at 0 and `int.MaxValue` instead of wrapping |
| `CanUpgradeStat(string)` / `GetUpgradeLevel(string)` |  |

Automatic growth is recalculated for upward and downward `SetLevel` changes. Upgrade points are
granted only for upward changes in manual/hybrid progression modes.
Manual, `None`, missing progression and deferred modes keep an applied-growth anchor: unrelated buff,
upgrade or base-stat refreshes do not apply a newer level. An explicit apply commits that level. On v2
profile restore the anchor is the saved level for automatic modes and level 1 otherwise; deferred/manual
callers must explicitly apply again after load. [Progression details](RpgProgressionDefinition.md).

### Invulnerability

| Method | What it does |
|---|---|
| `LockInvulnerable()` / `UnlockInvulnerable()` | stack (used by Evade controller) |
| `SetInvulnerable(bool)` | direct |

### Network shortcuts

Add `RpgCharacterNetworkAdapter` from the optional `Neo.Rpg.Network` assembly and enable
`RpgCharacter.isNetworked`. Existing character mutation APIs then route remote-client calls through
the adapter. A destroyed adapter component is dropped automatically and commands run locally again. The existing `NetDamage`, `NetHeal`, `NetSpend`, `NetRefill`,
`NetApplyBuffById`, `NetApplyInlineBuff`, `NetApplyStatusById`, and `NetAddLevel` compatibility methods
remain on `RpgCharacter` and use the same adapter path.
Without the adapter every API remains local and the RPG assembly has no Mirror dependency.

### Save / Load

`SaveProfile()` / `LoadProfile()` / `ResetProfile()` use the configured `SaveProvider` and `_saveKey`.
Persists every resource / stat / upgrade-points / active buff / active status by id (universal,
no hardcoded fields).

Use `CaptureProfile()` / `ApplyProfile(RpgCharacterProfileData)` for storage-independent snapshots.
Applying a profile clears upgrade counts absent from that profile. Buff stacks are preserved by
`RpgCharacterProfileService.Serialize` / `Deserialize`, which also sanitize null lists and entries.

The payload stays at **version 2**. For known resource IDs, the last saved row restores effective Max
before Current and percent-based regen. A saved `160 Max / 130 Current` completes as `160 / 130`, with
a normalized percent state of 0.8125 and a 10%-per-second rate of 16. Rows missing from the profile use the ordinary
resolver; unknown IDs create no pools. A restored buff Max of 120 is not added to itself on repeated loads.
`BaseMax` is never replaced with saved effective Max. A later ordinary derived refresh intentionally
recomputes from `BaseMax` and active modifiers, so a temporary effective-Max override can disappear then.

Initialization, rebuild and profile application suppress intermediate transport notifications. Adapters
receive one final notification after initialization and optional auto-load. The same rule covers one frame
of regeneration/effect ticks, a hit, a heal, `Restore()`, `SetLevel`, `UpgradeStat`, `RefreshAllDerived` and
any `DeferSnapshotPublication()` scope (see [Multiplayer](#multiplayer)). `RuntimeRebuilt` remains
before auto-load and is suitable for rebinding states; it does not mean the saved profile is loaded.
Use `OnProfileLoadedEvent` for storage-load completion. Callback-driven replacement stops stale derived
passes before later writes/events; repeatedly requested rebuilds have bounded immediate retries.

### Reactive shortcuts

`HpState`, `HpPercentState`, `MaxHpState`, `ManaState`, `ManaPercentState`, `StaminaState`,
`StaminaPercentState`, `LevelState`, `UpgradePointsState`, `XpState`, `IsDeadState`,
`InvulnerableState`.

Generic: `GetResourceCurrentState("DarkMana")`, `GetResourceMaxState(id)`,
`GetResourcePercentState(id)`, `GetStatState("Strength")`.

`RebuildRuntime()` and `ApplyTemplate(...)` replace resource/stat reactive objects. Subscribe to
`RuntimeRebuilt` to detach old listeners and acquire the new objects. Resource/stat binding components
do this automatically, including when the character initializes after the UI enables.
Disabled or rebound bindings suppress callbacks already captured by the former reactive notification.
Disabling/rebinding from an initial Current callback also suppresses the former Max/Percent continuation.

---

## Inspector

| Header | Contents |
|---|---|
| **Template** | `RpgCharacterTemplate` SO + `applyTemplateOnAwake`. Imports resources / stats / progression from the archetype. |
| **Resources** | `RpgResourceDefinition` list. Each: id (preset / Custom), start current / max, regen rules (`Flat / Percent / FromStat / *PerTick` + pause-after-spend / damage). |
| **Stats** | `RpgStatDefinition` list. Each: id, base, optional level growth. |
| **Effects** | `_knownBuffs[]` (SO), `_inlineBuffs[]` (no SO), `_knownStatuses[]` (SO). |
| **Progression** | `RpgProgressionDefinition` SO + optional `LevelComponent`. |
| **Persistence** | save key, load on awake, autosave. |
| **Authority** | `None` / `OwnerOnly` / `ServerOnly` - Command sender filter. |
| **Events** | OnDamaged / OnHealed / OnDeath / OnRevived / OnBuffApplied / OnBuffExpired / OnStatusApplied / OnStatusExpired / OnLevelChanged / OnResourceChanged(id, value) / OnStatChanged(id, value) / OnProfileSaved / OnProfileLoaded. |

C# events: `RuntimeRebuilt` (runtime replaced, rebind cached states) and `DamageApplied(RpgDamageInfo, float)`.

---

## Helper components for UI / NoCode

### `RpgResourceBinding`
Drop on a UI GameObject, drag the `RpgCharacter`, pick a resource id (e.g. `Custom = "DarkMana"`).
UnityEvent `OnCurrent(float)` / `OnMax(float)` / `OnPercent(float)` go to Slider / TMP_Text without code.
For text that combines several values, use the generic `NoCodeFormattedText` instead of a RPG-only UI wrapper.

### `RpgStatBinding`
Same idea for stats. `OnValue(float)`.

---

## NoCode scenarios

### Pickup grants +20 max HP for 60s
1. On the player: `RpgCharacter` with one `InlineBuffEntry`:
   - `id = "BigHpBoost"`, `duration = 60`
   - `Modifiers[0]`: `BuffStatType = AddResourceMaxFlat`, `TargetId = Hp`, `Value = 20`
2. On the pickup trigger: `NetworkContextActionRelay`:
   - `Action = InvokeComponentMethod`, `Component = RpgCharacter`, `Method = ApplyInlineBuff`, `Argument = 0`

### Potion restores 50 Stamina
`Button.onClick` -> `RpgCharacter.RefillStamina(50)`.

### Game-over when HP < 30%
`NeoCondition`:
- `Source = RpgCharacter`, `Property = HpPercentValue`, `op = <`, `threshold = 0.3`
- `OnTrue` -> `GameOverPanel.SetActive(true)`

### Stamina bar in UI without code
`Slider` + `RpgResourceBinding` (`Character = Player`, `ResourceId = Stamina`) ->
`OnPercent -> Slider.value`.

### Poison zone (DoT)
`PhysicsEvents3D.OnTriggerStay` -> `RpgCharacter.ApplyStatusById("Poison")`.

### Dark Souls-style upgrades
`RpgProgressionDefinition` with `growthMode = ManualUpgradePoints`,
`upgradeRules = [{ statId = Vitality, increasePerPoint = 1, derivedResourceModifiers = [{ Hp, AddMaxFlat, 15 }] }]`.
UI button -> `RpgCharacter.UpgradeVitality()`.

### Dota-style auto-growth
`RpgProgressionDefinition` with `growthMode = AllStatsEveryLevel`. On level-up from
`LevelComponent`, every stat with `affectedByLevel=true` is recomputed.

### Two manas (Mana + DarkMana)
In `_resources[]`: `Mana` (preset) + `DarkMana` (Custom string). Dark spell:
`Button.onClick` -> `RpgCharacter.Spend("DarkMana", 25)` or through `NetworkContextActionRelay`.

---

## Multiplayer

`RpgCharacter` itself is networking-agnostic. Add `RpgCharacterNetworkAdapter` on the same object
(it requires `NetworkIdentity` when Mirror is present), then enable `RpgCharacter.isNetworked`:

1. **Server authority.** Character mutation calls and adapter `Net*` shortcuts ride a rate-limited
   `[Command]` to the server.
2. **Snapshot SyncVar.** Server serializes every resource / stat / buff / status / level / xp /
   upgradePoints / isDead / invulLocks into one snapshot string. Clients receive via
   `[SyncVar(hook)]` and restore local state.
3. **Authority Mode** - `None` / `OwnerOnly` (only the owning client) / `ServerOnly`.
4. **Late join.** `RpgCharacterNetworkAdapter.ApplyNetworkState` applies the latest snapshot.
5. **One snapshot per step.** A frame of regeneration and effect ticks, a hit (including death), a heal,
   `Restore()`, a level-up, an upgrade and one adapter command each publish one complete snapshot, not
   one per changed value. Group your own mutations with `DeferSnapshotPublication()`:

```csharp
using (character.DeferSnapshotPublication())
{
    character.Damage(25f);
    character.AddXp(10f);
}
// One snapshot with the final HP and XP is published here.
```

Nested scopes publish once when the outermost ends and nothing when nothing changed. Only the transport
publication waits; UnityEvents, reactive states and `DamageApplied` still fire in place. Details:
[RPG networking](Network.md#snapshot-publication).

Migration from the old inherited network component: existing scenes keep the serialized
`isNetworked`, `_authorityMode`, and `_allowClientStateCommands` values on `RpgCharacter`, but networked
prefabs must add `RpgCharacterNetworkAdapter`. Existing UnityEvents targeting `RpgCharacter.Net*` remain
valid. Local/offline prefabs require no changes.

Multiplayer test:
- Host + remote via `NetworkManagerHUD` / `NeoNetworkManager`.
- Pickup trigger on the scene with `NetworkContextActionRelay.InvokeComponentMethod ->
  RpgCharacter.ApplyInlineBuff(0)` - both players see the effect.

---

## NPC

An NPC is the same `RpgCharacter` - no separate component.

1. On the enemy prefab: `RpgCharacter` + `RpgCharacterTemplate` (e.g. "Orc"):
   - resources: HP 80, Stamina 50
   - stats: Strength 10, Defense 5
2. + `NpcRpgCombatBrain` (`_character` field points to this `RpgCharacter`)
3. + `RpgAttackController` (`_characterSource` -> this `RpgCharacter`)
4. + `RpgDeathHandler` (auto-attaches, listens to `OnDeath`)
5. + UI through `RpgResourceBinding` + `SetProgress` for the HP bar and `NoCodeFormattedText` for `HP / MaxHP` text.

---

## Melee / ranged combat

### Melee
- `RpgContactDamage` (`selfCharacter` -> this character) + `targetTag = "Enemy"` - damage by proximity.
- Alternative: `MeleeWeapon` (MonoBehaviour subclass) + trigger collider ->
  `target.GetComponentInParent<RpgCharacter>().Damage(amount)`.

### Ranged
- `RpgAttackController` with `RpgAttackDefinition` (`deliveryType = Projectile`).
- `RpgProjectile` spawns from `_projectileSpawnPoint`; on hit ->
  `Damage` on the target's `RpgCharacter`.

### Aura / AoE
- `AuraWeapon` (extends `MeleeWeapon`) - radius damage on a tick.

---

## See also

- [RpgCharacterTemplate](RpgCharacterTemplate.md) - SO archetype
- [RpgProgressionDefinition](RpgProgressionDefinition.md) - growth modes
- [RpgResourceBinding](RpgResourceBinding.md) - NoCode UI binding
- [RpgStatBinding](RpgStatBinding.md)
- [BuffDefinition](Data/BuffDefinition.md), [InlineBuffEntry](InternalTypes.md)
- [Multiplayer_Guide](../Network/Multiplayer_Guide.md)
- [RpgCharacterNetworkAdapter](RpgCharacterNetworkAdapter.md)
- [Runtime services and internal types](InternalTypes.md)
