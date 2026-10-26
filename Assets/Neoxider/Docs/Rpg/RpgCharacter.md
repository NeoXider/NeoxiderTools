# RpgCharacter

**Universal RPG facade.** One component per character - player, NPC, mob, pet. Replaces the
legacy `RpgCombatant` + `RpgStatsManager` (removed in v8.4.0). Supports any number of resources
(HP / Mana / Stamina / DarkMana / Rage / any `Custom`), any number of stats
(Strength / Defense / FireResist / any `Custom`), buffs (SO + inline), status effects, two growth
modes (Dota-like and Dark-Souls-like), and Save/Load. Multiplayer is an optional adapter rather
than a dependency of the RPG runtime.

**File:** `Assets/Neoxider/Scripts/Rpg/Components/RpgCharacter.cs` - Menu: `Neoxider/RPG/RpgCharacter`.

**Demo:** `Assets/Neoxider/Samples~/Demo/Scenes/RpgCharacterQuickDemo.unity` - open the scene, press Play, and test Damage/Heal/Stamina/DarkMana/Upgrade with on-screen buttons.

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
|-- RpgEffectShelf (runtime)                - single source of truth for buff/status lifetime
|-- RpgCharacterProfileService (plain C#)   - profile validation + serialization
`-- RpgProgressionDefinition (SO, optional) - Dota | Souls | Hybrid + upgrade rules
```

No singleton. Multiple characters per scene is a first-class scenario (player + party + pets + enemies).

`RpgCharacter` keeps the serialized `_resources` authoring array and every established public method,
reactive property, and UnityEvent. At runtime it delegates resource ownership to
`RpgCharacterResourceService`, then translates `ResourceChanged` callbacks into death checks, scene
events, and optional network snapshots. This keeps existing prefabs and callers compatible while making
resource costs, clamping, restore, derived max/regen, pause windows, and tick timing directly testable in
plain C#.

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
| `Heal(float)` | heal HP |
| `Spend(string id, float)` | spend a resource; returns `false` when not enough or a positive cost produces no stored debit |
| `Refill(string, float)` / `Increase(string, float)` | top up a resource |
| `Restore()` / `RestoreResource(string)` | full restore |
| `SetMaxResource(string, float)` / `AddMaxResource(string, float)` | change effective Max; later derived refresh resolves from the unchanged BaseMax |

Changing HP through `Increase`, `Refill`, or `RestoreResource` also updates death/revival state.
`OnRevivedEvent` fires once when a dead character's HP becomes positive.

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
| `AddStatBase(string, float)` / `SetStatBase(string, float)` | menu / inventory |
| `UpgradeStrength()` / `UpgradeDexterity()` / `UpgradeVitality()` / `UpgradeIntelligence()` / `UpgradeEndurance()` | Dark-Souls UI |
| `UpgradeStat(string)` | universal upgrade |

Changing a stat base immediately refreshes resources that regenerate `FromStat`. Upgrade rules
require a positive cost and an existing runtime stat; invalid rules do not consume or create points.
Local `UpgradeStat` returns `true` once the original point debit is accepted. If an `UpgradePointsState`
observer replaces the runtime, that superseded call stops before investing or refreshing the replacement,
and does not refund into it. Ordinary successful upgrades still debit points, invest and refresh stats.

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
`AttackSpeedPercent` is retained in the enum but has no runtime consumer; it does not change cooldowns.
See [buff contracts and callback behaviour](Data/BuffDefinition.md).

### Level / progression

| Method | What it does |
|---|---|
| `SetLevel(int)` / `AddLevel(int)` | raises level. `AllStatsEveryLevel` auto-applies growth; `ManualUpgradePoints` grants upgrade points |
| `ApplyLevelGrowth()` | explicitly applies configured growth at the current level, including deferred/manual modes |
| `AddXp(float)` |  |
| `AddUpgradePoints(int)` |  |
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
the adapter. The existing `NetDamage`, `NetHeal`, `NetSpend`, `NetRefill`,
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
receive one final notification after initialization and optional auto-load. `RuntimeRebuilt` remains
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
