# RpgCharacter

**Universal RPG facade.** One component per character вЂ” player, NPC, mob, pet. Replaces the
legacy `RpgCombatant` + `RpgStatsManager` (removed in v8.4.0). Supports any number of resources
(HP / Mana / Stamina / DarkMana / Rage / any `Custom`), any number of stats
(Strength / Defense / FireResist / any `Custom`), buffs (SO + inline), status effects, two growth
modes (Dota-like and Dark-Souls-like), Save/Load and Mirror multiplayer.

**File:** `Assets/Neoxider/Scripts/Rpg/Components/RpgCharacter.cs` В· Menu: `Neoxider/RPG/RpgCharacter`.

**Demo:** `Assets/Neoxider/Samples~/Demo/Scenes/RpgCharacterQuickDemo.unity` вЂ” open the scene, press Play, and test Damage/Heal/Stamina/DarkMana/Upgrade with on-screen buttons.

---

## Architecture

```
RpgCharacter : NeoNetworkComponent, IRpgCombatReceiver
в”њв”Ђв”Ђ RpgCharacterTemplate (SO, optional)     вЂ” start resources / stats / buffs / progression
в”њв”Ђв”Ђ RpgResourceDefinition[] _resources      вЂ” HP / Mana / Stamina / Shield / any Custom
в”њв”Ђв”Ђ RpgStatDefinition[]     _stats          вЂ” Strength / Defense / FireResist / any Custom
в”њв”Ђв”Ђ BuffDefinition[]        _knownBuffs     вЂ” re-usable SO buffs
в”њв”Ђв”Ђ InlineBuffEntry[]       _inlineBuffs    вЂ” one-off buffs without SOs
в”њв”Ђв”Ђ StatusEffectDefinition[] _knownStatuses вЂ” DoT / Slow / Stun
в”њв”Ђв”Ђ RpgEffectShelf (runtime)                вЂ” single source of truth for buff/status lifetime
в””в”Ђв”Ђ RpgProgressionDefinition (SO, optional) вЂ” Dota | Souls | Hybrid + upgrade rules
```

No singleton. Multiple characters per scene is a first-class scenario (player + party + pets + enemies).

---

## Universal ID

`RpgStatId` = `RpgStatPreset` + optional `customId`. The inspector shows a dropdown with common
values (`Hp`, `Mana`, `Stamina`, `Shield`, `Strength`, `FireResist`, вЂ¦) plus a custom-string field
that activates when you pick `Custom`. Same for buff target id.

```csharp
// 1. Preset
new RpgStatId(RpgStatPreset.Hp)            // value = "Hp"

// 2. Custom
new RpgStatId("DarkMana")                  // preset = Custom, value = "DarkMana"

// implicit conversions
RpgStatId id = RpgStatPreset.Stamina;
string key = id;                           // "Stamina"
```

---

## Public API (UnityEvent-friendly)

Every method takes 0вЂ“1 primitive / SO parameter вЂ” they appear in UnityEvent dropdowns and can be
called from `NetworkContextActionRelay.InvokeComponentMethod`, `Button.onClick`,
`PhysicsEvents3D.onTriggerEnter`, `NeoCondition.OnTrue`.

### Damage / heal / resources

| Method | What it does |
|---|---|
| `Damage(float)` | HP using buff/status IncomingDamage% + Defense% modifiers |
| `DamageType(string, float)` | + specific resist (`FireResist`, `IceResist`, вЂ¦) |
| `Heal(float)` | heal HP |
| `Spend(string id, float)` | spend a resource; returns `false` when not enough |
| `Refill(string, float)` / `Increase(string, float)` | top up a resource |
| `Restore()` / `RestoreResource(string)` | full restore |
| `SetMaxResource(string, float)` / `AddMaxResource(string, float)` | change Max (buffs / upgrades) |

### NoCode shortcuts

`SpendMana(float)`, `RefillMana(float)`, `SpendStamina(float)`, `RefillStamina(float)`,
`SpendShield(float)` вЂ” explicit methods for the common pools, visible in UnityEvent dropdowns.

### Stats

| Method | What it does |
|---|---|
| `GetStat(string)` | final value = base + level + upgrade + buffs |
| `AddStatBase(string, float)` / `SetStatBase(string, float)` | menu / inventory |
| `UpgradeStrength()` / `UpgradeDexterity()` / `UpgradeVitality()` / `UpgradeIntelligence()` / `UpgradeEndurance()` | Dark-Souls UI |
| `UpgradeStat(string)` | universal upgrade |

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

### Level / progression

| Method | What it does |
|---|---|
| `SetLevel(int)` / `AddLevel(int)` | raises level. `AllStatsEveryLevel` auto-applies growth; `ManualUpgradePoints` grants upgrade points |
| `AddXp(float)` |  |
| `AddUpgradePoints(int)` |  |
| `CanUpgradeStat(string)` / `GetUpgradeLevel(string)` |  |

### Invulnerability

| Method | What it does |
|---|---|
| `LockInvulnerable()` / `UnlockInvulnerable()` | stack (used by Evade controller) |
| `SetInvulnerable(bool)` | direct |

### Network shortcuts

When `isNetworked = true` and the caller is a remote client, `NetDamage`, `NetHeal`, `NetSpend`,
`NetRefill`, `NetApplyBuffById`, `NetApplyInlineBuff`, `NetApplyStatusById`, `NetAddLevel`
dispatch a `[Command]` to the server. The server applies the change and pushes a snapshot
SyncVar; every client receives. If you use the plain `Damage` / `Heal` / etc., they apply
locally only.

### Save / Load

`SaveProfile()` / `LoadProfile()` / `ResetProfile()` вЂ” writes to `PlayerPrefs[_saveKey]`.
Persists every resource / stat / upgrade-points / active buff / active status by id (universal,
no hardcoded fields).

### Reactive shortcuts

`HpState`, `HpPercentState`, `MaxHpState`, `ManaState`, `ManaPercentState`, `StaminaState`,
`StaminaPercentState`, `LevelState`, `UpgradePointsState`, `XpState`, `IsDeadState`,
`InvulnerableState`.

Generic: `GetResourceCurrentState("DarkMana")`, `GetResourceMaxState(id)`,
`GetResourcePercentState(id)`, `GetStatState("Strength")`.

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
| **Authority** | `None` / `OwnerOnly` / `ServerOnly` вЂ” Command sender filter. |
| **Events** | OnDamaged / OnHealed / OnDeath / OnRevived / OnBuffApplied / OnBuffExpired / OnStatusApplied / OnStatusExpired / OnLevelChanged / OnResourceChanged(id, value) / OnStatChanged(id, value) / OnProfileSaved / OnProfileLoaded. |

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
`Button.onClick` в†’ `RpgCharacter.RefillStamina(50)`.

### Game-over when HP < 30%
`NeoCondition`:
- `Source = RpgCharacter`, `Property = HpPercentValue`, `op = <`, `threshold = 0.3`
- `OnTrue` в†’ `GameOverPanel.SetActive(true)`

### Stamina bar in UI without code
`Slider` + `RpgResourceBinding` (`Character = Player`, `ResourceId = Stamina`) в†’
`OnPercent в†’ Slider.value`.

### Poison zone (DoT)
`PhysicsEvents3D.OnTriggerStay` в†’ `RpgCharacter.ApplyStatusByName("Poison")`.

### Dark Souls-style upgrades
`RpgProgressionDefinition` with `growthMode = ManualUpgradePoints`,
`upgradeRules = [{ statId = Vitality, increasePerPoint = 1, derivedResourceModifiers = [{ Hp, AddMaxFlat, 15 }] }]`.
UI button в†’ `RpgCharacter.UpgradeVitality()`.

### Dota-style auto-growth
`RpgProgressionDefinition` with `growthMode = AllStatsEveryLevel`. On level-up from
`LevelComponent`, every stat with `affectedByLevel=true` is recomputed.

### Two manas (Mana + DarkMana)
In `_resources[]`: `Mana` (preset) + `DarkMana` (Custom string). Dark spell:
`Button.onClick` в†’ `RpgCharacter.Spend("DarkMana", 25)` or through `NetworkContextActionRelay`.

---

## Multiplayer

`RpgCharacter : NeoNetworkComponent`. Enable `isNetworked` in the inspector and the component
becomes server-authoritative:

1. **Server authority.** Changes via `NetDamage` / `NetHeal` / `Net*` ride a `[Command]` to the server.
2. **Snapshot SyncVar.** Server serializes every resource / stat / buff / status / level / xp /
   upgradePoints / isDead / invulLocks into one snapshot string. Clients receive via
   `[SyncVar(hook)]` and restore local state.
3. **Authority Mode** вЂ” `None` / `OwnerOnly` (only the owning client) / `ServerOnly`.
4. **Late join.** When a new client connects, `ApplyNetworkState` (inherited from
   `NeoNetworkComponent`) applies the latest snapshot.

Multiplayer test:
- Host + remote via `NetworkManagerHUD` / `NeoNetworkManager`.
- Pickup trigger on the scene with `NetworkContextActionRelay.InvokeComponentMethod в†’
  RpgCharacter.ApplyInlineBuff(0)` вЂ” both players see the effect.

---

## NPC

An NPC is the same `RpgCharacter` вЂ” no separate component.

1. On the enemy prefab: `RpgCharacter` + `RpgCharacterTemplate` (e.g. "Orc"):
   - resources: HP 80, Stamina 50
   - stats: Strength 10, Defense 5
2. + `NpcRpgCombatBrain` (`_character` field points to this `RpgCharacter`)
3. + `RpgAttackController` (`_characterSource` в†’ this `RpgCharacter`)
4. + `RpgDeathHandler` (auto-attaches, listens to `OnDeath`)
5. + UI through `RpgResourceBinding` + `SetProgress` for the HP bar and `NoCodeFormattedText` for `HP / MaxHP` text.

---

## Melee / ranged combat

### Melee
- `RpgContactDamage` (`selfCharacter` в†’ this character) + `targetTag = "Enemy"` вЂ” damage by proximity.
- Alternative: `MeleeWeapon` (MonoBehaviour subclass) + trigger collider в†’
  `target.GetComponentInParent<RpgCharacter>().Damage(amount)`.

### Ranged
- `RpgAttackController` with `RpgAttackDefinition` (`deliveryType = Projectile`).
- `RpgProjectile` spawns from `_projectileSpawnPoint`; on hit в†’
  `Damage` on the target's `RpgCharacter`.

### Aura / AoE
- `AuraWeapon` (extends `MeleeWeapon`) вЂ” radius damage on a tick.

---

## See also

- [RpgCharacterTemplate](RpgCharacterTemplate.md) вЂ” SO archetype
- [RpgProgressionDefinition](RpgProgressionDefinition.md) вЂ” growth modes
- [RpgResourceBinding](RpgResourceBinding.md) вЂ” NoCode UI binding
- [RpgStatBinding](RpgStatBinding.md)
- [BuffDefinition](Data/BuffDefinition.md), [InlineBuffEntry](InternalTypes.md)
- [Multiplayer_Guide](../Network/Multiplayer_Guide.md)
