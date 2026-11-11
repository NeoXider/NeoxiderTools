# RpgCharacter

**What it is:** the universal RPG character (`MonoBehaviour`, namespace `Neo.Rpg.Components`,
`Scripts/Rpg/Components/RpgCharacter.cs`, menu `Neoxider/RPG/RpgCharacter`). One component per player, NPC, mob
or pet holds any number of resources (HP, Mana, Stamina, any `Custom`) and stats, SO and inline buffs, status
effects, Dota-like or Souls-like growth, Save/Load and an optional network adapter.

**How to use:**
1. Add `RpgCharacter` to the actor and assign an `RpgCharacterTemplate`, or fill `Resources` and `Stats` inline.
2. Call `Damage`, `Heal`, `Spend`, `ApplyBuffById`, `SetLevel`, `UpgradeStat` from C#, UnityEvents or
   `RpgNoCodeAction`.
3. Read state through `HpState`, `GetResourceCurrentState(id)`, `GetStatState(id)`, or the `RpgResourceBinding` /
   `RpgStatBinding` UI components.
4. Optional: register `IRpgDamageModifier` hooks, subscribe to `DamageApplied`, add `RpgCharacterNetworkAdapter`
   for multiplayer.

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
|-- RpgCharacterResourceService (plain C#)  - resource pools, mutations, derived Max/regen, regen clocks
|-- RpgCharacterStatService (plain C#)      - stat runtimes, upgrade rules, invested points, resolvers
|-- RpgCharacterProgressionService (plain C#) - level / XP / upgrade points / applied-growth anchor
|-- RpgEffectShelf (runtime)                - active buffs/statuses, stacking, expiry, ticks
|-- RpgModifierMath (static)                - outgoing / incoming / movement / attack-speed multipliers
|-- RpgCharacterProfileService (plain C#)   - profile validation + serialization
`-- RpgProgressionDefinition (SO, optional) - Dota | Souls | Hybrid + upgrade rules
```

- No singleton: several characters per scene (player, party, pets, enemies) is the normal case.
- The component keeps orchestration only: publication order, re-entrancy guards, network routing, save/load.
  State lives in the plain C# services, which also work without a scene (tests, servers, custom actors).
- The per-frame tick and the damage path do not allocate once warm, as long as your UnityEvent listeners do not.
- It replaces the removed `RpgCombatant` and `RpgStatsManager`.

See [Runtime services and internal types](InternalTypes.md).

---

## Universal ID

`RpgStatId` = `RpgStatPreset` + optional `customId`. The inspector shows a dropdown of common values (`Hp`,
`Mana`, `Stamina`, `Shield`, `Strength`, `FireResist`, ...) and a custom-string field for `Custom`. Buff target
ids work the same way.

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

Single-argument methods appear in UnityEvent dropdowns. Multi-argument methods such as `Spend(string, float)` are
for C# or a configured `RpgNoCodeAction`.

### Damage / heal / resources

| Method | What it does |
|---|---|
| `Damage(float)` | Damages HP with buff/status IncomingDamage% and Defense% applied. |
| `DamageType(string, float)` | Same, plus a specific resist (`FireResist`, `IceResist`, ...). |
| `ApplyDamage(RpgDamageInfo)` | Full pipeline with the hit source; returns the HP actually removed. |
| `AddDamageModifier(IRpgDamageModifier)` / `RemoveDamageModifier(...)` | Code hooks into the damage pipeline. |
| `Heal(float)` | Heals HP. |
| `Spend(string id, float)` | Spends a resource; `false` when there is not enough or a positive cost stores no debit. |
| `Refill(string, float)` / `Increase(string, float)` | Tops up a resource. |
| `Restore()` / `RestoreResource(string)` | Full restore. |
| `SetMaxResource(string, float)` / `AddMaxResource(string, float)` | Changes effective Max; a later derived refresh recomputes from the unchanged `BaseMax`. |

HP changes through `Increase`, `Refill` or `RestoreResource` also update death state. `OnRevivedEvent` fires once
when a dead character's HP becomes positive.

A positive cost that float precision rounds away (1 from 100000000) is rejected before any event, regen pause or
transport publication. A positive stored decrement is accepted even if rounded (cost 5 at that balance stores an
8-unit decrement). Ordinary fractional costs work as usual.

### Damage pipeline

`Damage`, `DamageType`, `ApplyDamage` and `IRpgCombatReceiver.TakeDamage` (used by attacks, projectiles and
contact damage) run the same steps:

1. Network routing, when `isNetworked` and an adapter is present. The hit source is not transmitted.
2. Non-positive or non-finite amounts and invulnerability reject the hit.
3. Buff/status multiplier for the damage type (`GetIncomingDamageMultiplier`).
4. Registered `IRpgDamageModifier`s in registration order. Each gets the original `RpgDamageInfo` (raw amount,
   type, source) and the running amount. Negative results clamp to 0, non-finite results are ignored, destroyed
   component modifiers are skipped. Adding or removing modifiers during a hit affects the next hit.
5. HP decrease. It raises `OnResourceChangedEvent` at once; a lethal decrease also raises `OnDeathEvent` here,
   before any damage event.
6. `OnDamagedEvent(applied)`, then the C# event `DamageApplied(info, applied)`, then the regen pause. A fully
   absorbed hit raises neither event. On the killing blow `IsDead` is already `true` inside `DamageApplied`.

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

### NoCode shortcuts

`SpendMana(float)`, `RefillMana(float)`, `SpendStamina(float)`, `RefillStamina(float)`, `SpendShield(float)`:
explicit methods for the common pools, visible in UnityEvent dropdowns.

### Stats

| Method | What it does |
|---|---|
| `GetStat(string)` | Final value = base + level + upgrade + buffs. |
| `AddStatBase(string, float)` / `SetStatBase(string, float)` | For menus and inventory; NaN, infinite or overflowing values are ignored. |
| `UpgradeStrength()` / `UpgradeDexterity()` / `UpgradeVitality()` / `UpgradeIntelligence()` / `UpgradeEndurance()` | Souls-style upgrade buttons. |
| `UpgradeStat(string)` | Universal upgrade. |

- Changing a stat base immediately refreshes resources that regenerate `FromStat`.
- Upgrade rules need a positive cost and an existing runtime stat; invalid rules neither consume nor create points.
- `UpgradeStat` returns `true` once the point debit is accepted, then invests and refreshes stats. If an
  `UpgradePointsState` listener replaces the runtime during the call, the call stops and refunds nothing.

#### Non-finite values

A single NaN would make every comparison false (never dead, never full) and spread to every derived Max and
percent, so the character never stores one:

- `Damage`, `Heal`, `Spend`, `Refill` / `Increase` / `Decrease`, `SetMaxResource` / `AddMaxResource` and `AddXp`
  reject NaN and infinite amounts.
- `AddStatBase` / `SetStatBase` (and `RpgCharacterStatService.AddBase` / `SetBase`) do nothing for NaN, infinite
  or overflowing values; `RestoreBases` skips such rows of a corrupted save.
- `RpgResourceRuntime.SetCurrent` / `SetMax` and `RpgStatRuntime.SetCurrent` keep their previous value when a
  resolver produces NaN, for example from a NaN buff value in data. Infinity still clamps to the limits.

The same rules protect the server from client state commands ([RPG networking](Network.md#c-api)).

### Buffs / statuses

| Method | What it does |
|---|---|
| `ApplyBuff(BuffDefinition)` | Applies an SO buff. |
| `ApplyBuffById(string)` | Applies by id, looking up SO and inline buffs. |
| `ApplyInlineBuff(int index)` | Applies `_inlineBuffs[index]` (pickup buffs). |
| `RemoveBuff(string)` / `ClearAllBuffs()` | Removes buffs. |
| `ApplyStatus(StatusEffectDefinition)` / `ApplyStatusById(string)` | Applies a status. |
| `RemoveStatus(string)` / `ClearAllStatuses()` | Removes statuses. |
| `HasBuff(string)` / `HasStatus(string)` | Queries. |

Modifier rules (SO and inline buffs alike):

- Legacy damage, defense, typed defense and movement buff aliases apply every clamped active stack.
- `GetMovementSpeedMultiplier()` multiplies movement buffs by the active statuses' movement multipliers. Movement
  controllers must read it; a status does not move a Transform itself.
- `HpRegenPerSecond` adds units per second to canonical `"HP"`, ignoring the modifier target id. Its stacks
  compose with `RegenFlat` before `RegenPercent`; the enabled, mode, alive, full and pause gates still apply.
- `AddResourceCurrentFlat` delivers a signed raw delta once per newly accepted stack, after derived limits and
  before `OnBuffAppliedEvent`. Refresh of a non-stacking or capped buff, removal, expiry and profile restore
  deliver nothing.
- `AttackSpeedPercent` is read through `GetAttackSpeedMultiplier()` (`1 + percent / 100`, clamped at 0). It changes
  cooldowns only on an `RpgAttackController` with **Scale Cooldown By Attack Speed** on (off by default).

See [buff contracts and callback behaviour](Data/BuffDefinition.md).

### Level / progression

| Method | What it does |
|---|---|
| `SetLevel(int)` / `AddLevel(int)` | Sets the level. `AllStatsEveryLevel` applies growth; `ManualUpgradePoints` grants upgrade points. |
| `ApplyLevelGrowth()` | Applies the configured growth at the current level, also in deferred and manual modes. |
| `AddXp(float)` | Positive finite amounts only. |
| `AddUpgradePoints(int)` | Saturates at 0 and `int.MaxValue` instead of wrapping. |
| `CanUpgradeStat(string)` / `GetUpgradeLevel(string)` | Queries. |

- Automatic growth is recalculated when `SetLevel` goes up or down. Upgrade points are granted only on the way up,
  in manual and hybrid modes.
- Manual, `None`, missing-progression and deferred modes keep an applied-growth anchor: buff, upgrade or base-stat
  refreshes do not apply a newer level. An explicit `ApplyLevelGrowth()` commits it.
- After a profile restore the anchor is the saved level in automatic modes and level 1 otherwise, so deferred and
  manual callers apply again after load.

See [Progression details](RpgProgressionDefinition.md).

### Invulnerability

| Method | What it does |
|---|---|
| `LockInvulnerable()` / `UnlockInvulnerable()` | Stacked lock (used by the Evade controller). |
| `SetInvulnerable(bool)` | Direct switch. |

### Save / Load

- `SaveProfile()` / `LoadProfile()` / `ResetProfile()` use the configured `SaveProvider` and `_saveKey`. Every
  resource, stat, upgrade point, active buff and active status is stored by id.
- `CaptureProfile()` / `ApplyProfile(RpgCharacterProfileData)` make storage-independent snapshots. Applying a
  profile clears upgrade counts absent from it.
- `RpgCharacterProfileService.Serialize` / `Deserialize` keep buff stacks and sanitize null lists and entries.

The payload is version 2. A known resource row restores effective Max before Current and percent regen, so a saved
`160 Max / 130 Current` loads as `160 / 130`. Rows missing from the profile use the ordinary resolver; unknown ids
create no pools. `BaseMax` is never replaced, so a later derived refresh recomputes from it and a temporary Max
override can disappear then. Details:
[Version 2 restoration](Network.md#version-2-resource-and-effect-restoration).

**Events around initialization and load.** Initialization, rebuild and profile application send one final
transport notification instead of intermediate ones. `RuntimeRebuilt` fires before auto-load and is meant for
rebinding states; use `OnProfileLoadedEvent` to know the saved profile is loaded.

### Reactive shortcuts

`HpState`, `HpPercentState`, `MaxHpState`, `ManaState`, `ManaPercentState`, `StaminaState`, `StaminaPercentState`,
`LevelState`, `UpgradePointsState`, `XpState`, `IsDeadState`, `InvulnerableState`.

Generic: `GetResourceCurrentState("DarkMana")`, `GetResourceMaxState(id)`, `GetResourcePercentState(id)`,
`GetStatState("Strength")`.

`RebuildRuntime()` and `ApplyTemplate(...)` replace the resource and stat reactive objects. Subscribe to
`RuntimeRebuilt` to drop old listeners and take the new objects. The binding components do this automatically,
also when the character initializes after the UI. A binding that is disabled or rebound mid-notification gets no
further callbacks from the old objects.

---

## Inspector

| Header | Contents |
|---|---|
| **Template** | `RpgCharacterTemplate` SO + `applyTemplateOnAwake`. Imports resources, stats and progression. |
| **Resources** | `RpgResourceDefinition` list: id (preset or Custom), start current and max, regen (`Flat` / `Percent` / `FromStat` / `*PerTick`, pause after spend or damage). |
| **Stats** | `RpgStatDefinition` list: id, base, optional level growth. |
| **Effects** | `_knownBuffs[]` (SO), `_inlineBuffs[]` (no SO), `_knownStatuses[]` (SO). |
| **Progression** | `RpgProgressionDefinition` SO + optional `LevelComponent`. |
| **Persistence** | Save key, load on awake, autosave. |
| **Authority** | `None` / `OwnerOnly` / `ServerOnly`: command sender filter. |
| **Events** | See the list below. |

UnityEvents: OnDamaged, OnHealed, OnDeath, OnRevived, OnBuffApplied, OnBuffExpired, OnStatusApplied,
OnStatusExpired, OnLevelChanged, OnResourceChanged(id, value), OnStatChanged(id, value), OnProfileSaved,
OnProfileLoaded.

C# events: `RuntimeRebuilt` (runtime replaced, rebind cached states) and `DamageApplied(RpgDamageInfo, float)`.

---

## Helper components for UI / NoCode

- **`RpgResourceBinding`** — put it on a UI object, assign the `RpgCharacter`, pick a resource id (for example
  `Custom = "DarkMana"`). `OnCurrent(float)`, `OnMax(float)` and `OnPercent(float)` drive a Slider or TMP_Text
  without code. For text that combines several values use `NoCodeFormattedText`.
- **`RpgStatBinding`** — the same for stats, with `OnValue(float)`.

---

## NoCode scenarios

**Pickup grants +20 max HP for 60 s.**
1. On the player: `RpgCharacter` with one `InlineBuffEntry`: `id = "BigHpBoost"`, `duration = 60`,
   `Modifiers[0]`: `BuffStatType = AddResourceMaxFlat`, `TargetId = Hp`, `Value = 20`.
2. On the pickup trigger: `NetworkContextActionRelay` with `Action = InvokeComponentMethod`,
   `Component = RpgCharacter`, `Method = ApplyInlineBuff`, `Argument = 0`.

**Potion restores 50 Stamina.** `Button.onClick` -> `RpgCharacter.RefillStamina(50)`.

**Game over below 30% HP.** `NeoCondition` with `Source = RpgCharacter`, `Property = HpPercentValue`, `op = <`,
`threshold = 0.3`; `OnTrue` -> `GameOverPanel.SetActive(true)`.

**Stamina bar without code.** `Slider` + `RpgResourceBinding` (`Character = Player`, `ResourceId = Stamina`),
`OnPercent -> Slider.value`.

**Poison zone (DoT).** `PhysicsEvents3D.OnTriggerStay` -> `RpgCharacter.ApplyStatusById("Poison")`.

**Souls-style upgrades.** `RpgProgressionDefinition` with `growthMode = ManualUpgradePoints`,
`upgradeRules = [{ statId = Vitality, increasePerPoint = 1, derivedResourceModifiers = [{ Hp, AddMaxFlat, 15 }] }]`.
A UI button calls `RpgCharacter.UpgradeVitality()`.

**Dota-style auto-growth.** `RpgProgressionDefinition` with `growthMode = AllStatsEveryLevel`. On a level-up from
`LevelComponent`, every stat with `affectedByLevel = true` is recomputed.

**Two manas.** In `_resources[]`: `Mana` (preset) and `DarkMana` (Custom). A dark spell button calls
`RpgCharacter.Spend("DarkMana", 25)`, or goes through `NetworkContextActionRelay`.

---

## Multiplayer

`RpgCharacter` is networking-agnostic. For multiplayer add `RpgCharacterNetworkAdapter` from the optional
`Neo.Rpg.Network` assembly (it requires `NetworkIdentity` when Mirror is present) and enable
`RpgCharacter.isNetworked`. Without the adapter every API stays local and the RPG assembly has no Mirror
dependency.

- **Server authority.** Mutation calls on a remote client ride a rate-limited `[Command]` to the server. The
  `NetDamage`, `NetHeal`, `NetSpend`, `NetRefill`, `NetApplyBuffById`, `NetApplyInlineBuff`, `NetApplyStatusById`
  and `NetAddLevel` methods on `RpgCharacter` use the same path.
- **Snapshot.** The server serializes resources, stats, buffs, statuses, level, XP, upgrade points, death and
  invulnerability locks into one snapshot; clients restore it from a `[SyncVar(hook)]`. Late joiners get the
  latest snapshot through `RpgCharacterNetworkAdapter.ApplyNetworkState`.
- **Authority Mode:** `None`, `OwnerOnly` (the owning client only) or `ServerOnly`.
- A destroyed adapter is dropped automatically and commands run locally again.
- **One snapshot per step.** A frame of regeneration and effect ticks, a hit (death included), a heal,
  `Restore()`, a level-up, an upgrade and one adapter command each publish one snapshot. Group your own mutations:

```csharp
using (character.DeferSnapshotPublication())
{
    character.Damage(25f);
    character.AddXp(10f);
}
// One snapshot with the final HP and XP is published here.
```

Nested scopes publish once when the outermost ends, and nothing when nothing changed. Only the transport waits:
UnityEvents, reactive states and `DamageApplied` fire in place. Details:
[RPG networking](Network.md#snapshot-publication). Upgrading from the old inherited network component:
[RpgCharacterNetworkAdapter](RpgCharacterNetworkAdapter.md).

To test: run host and remote through `NetworkManagerHUD` or `NeoNetworkManager`, and put a pickup with
`NetworkContextActionRelay.InvokeComponentMethod -> RpgCharacter.ApplyInlineBuff(0)` in the scene; both players
see the effect.

---

## NPC

An NPC is the same `RpgCharacter`; there is no separate component.

1. On the enemy prefab: `RpgCharacter` + `RpgCharacterTemplate` (for example "Orc": HP 80, Stamina 50,
   Strength 10, Defense 5).
2. Add `NpcRpgCombatBrain` with `_character` pointing to this `RpgCharacter`.
3. Add `RpgAttackController` with `_characterSource` pointing to this `RpgCharacter`.
4. Add `RpgDeathHandler`; it attaches itself and listens to `OnDeath`.
5. UI: `RpgResourceBinding` + `SetProgress` for the HP bar and `NoCodeFormattedText` for `HP / MaxHP` text.

---

## Melee / ranged combat

- **Melee:** `RpgContactDamage` (`selfCharacter` -> this character, `targetTag = "Enemy"`) damages by proximity.
  Alternative: `MeleeWeapon` + a trigger collider -> `target.GetComponentInParent<RpgCharacter>().Damage(amount)`.
- **Ranged:** `RpgAttackController` with an `RpgAttackDefinition` (`deliveryType = Projectile`). `RpgProjectile`
  spawns from `_projectileSpawnPoint` and calls `Damage` on the target's `RpgCharacter` on hit.
- **Aura / AoE:** `AuraWeapon` (extends `MeleeWeapon`) deals radius damage on a tick.

---

## Demo scene

`Scenes/RpgCharacterQuickDemo.unity` in the Demo download (`Assets/NeoxiderDemos/Demo/`). Everything is saved in
the scene. Look at `Rpg Demo Player` (its `RpgCharacterTemplate`), the `Resource · HP` / `Stamina` / `DarkMana`
rows (`RpgResourceBinding` events driving labels and bars), the `NoCode ...` buttons (`RpgNoCodeAction`) and the
`Condition · Low HP` row (`NeoCondition` reading `HpPercentValue`). In Play Mode each C# button logs its call, the
Guard toggle registers an `IRpgDamageModifier`, and enemy hits are reported through `DamageApplied`.

## See also

- [RpgCharacterTemplate](RpgCharacterTemplate.md) — SO archetype
- [RpgProgressionDefinition](RpgProgressionDefinition.md) — growth modes
- [RpgResourceBinding](RpgResourceBinding.md) — NoCode UI binding
- [RpgStatBinding](RpgStatBinding.md)
- [BuffDefinition](Data/BuffDefinition.md), [InlineBuffEntry](InternalTypes.md)
- [Multiplayer_Guide](../Network/Multiplayer_Guide.md)
- [RpgCharacterNetworkAdapter](RpgCharacterNetworkAdapter.md)
- [Runtime services and internal types](InternalTypes.md)
