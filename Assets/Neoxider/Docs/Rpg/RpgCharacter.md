# RpgCharacter

**Универсальный RPG-фасад.** Один компонент на персонажа — игрок, NPC, моб, питомец. Заменяет
старые `RpgCombatant` + `RpgStatsManager` (удалены в v8.4.0). Поддерживает любое число ресурсов
(HP / Mana / Stamina / DarkMana / Rage / любой `Custom`), любое число статов
(Strength / Defense / FireResist / любой `Custom`), баффы (SO + inline), статусы, два режима роста
(Dota-like и Dark-Souls-like), Save/Load и multiplayer Mirror.

**Файл:** `Assets/Neoxider/Scripts/Rpg/Components/RpgCharacter.cs` · Меню: `Neoxider/RPG/RpgCharacter`.

**Демо:** `Assets/Neoxider/Samples~/Demo/Scenes/RpgCharacterQuickDemo.unity` — откройте сцену, нажмите Play и проверьте Damage/Heal/Stamina/DarkMana/Upgrade кнопками на экране.

---

## Архитектура

```
RpgCharacter : NeoNetworkComponent, IRpgCombatReceiver
├── RpgCharacterTemplate (SO, optional)     — стартовые ресурсы / статы / бафы / прогрессия
├── RpgResourceDefinition[] _resources      — HP / Mana / Stamina / Shield / любой Custom
├── RpgStatDefinition[]     _stats          — Strength / Defense / FireResist / любой Custom
├── BuffDefinition[]        _knownBuffs     — переиспользуемые SO-бафы
├── InlineBuffEntry[]       _inlineBuffs    — одноразовые бафы без SO
├── StatusEffectDefinition[] _knownStatuses — DoT / Slow / Stun
├── RpgEffectShelf (runtime)                — единое управление lifetime бафов / статусов
└── RpgProgressionDefinition (SO, optional) — Dota | Souls | Hybrid + upgrade rules
```

Singleton'а нет. Несколько персонажей на сцене — нормальная ситуация (player + party + pets + врага).

---

## Универсальный ID

`RpgStatId` — это `RpgStatPreset` + опциональный `customId`. В Inspector видишь dropdown с
популярными значениями (`Hp`, `Mana`, `Stamina`, `Shield`, `Strength`, `FireResist`, …) и поле
для собственного id когда выбран `Custom`. То же самое для buff target id.

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

Все методы принимают 0–1 параметр примитивного / SO-типа, попадают в UnityEvent dropdown,
вызываются через `NetworkContextActionRelay.InvokeComponentMethod`, `Button.onClick`,
`PhysicsEvents3D.onTriggerEnter`, `NeoCondition.OnTrue`.

### Урон / лечение / ресурсы

| Метод | Что делает |
|---|---|
| `Damage(float)` | HP с учётом IncomingDamage% + Defense% бафов |
| `DamageType(string, float)` | + специфический resist (`FireResist`, `IceResist`, …) |
| `Heal(float)` | лечение HP |
| `Spend(string id, float)` | расход (Mana / Stamina / Shield / любой ID). `false` если не хватает |
| `Refill(string, float)` / `Increase(string, float)` | пополнение |
| `Restore()` / `RestoreResource(string)` | до Max |
| `SetMaxResource(string, float)` / `AddMaxResource(string, float)` | изменить Max (для бафов на вечную) |

### Шорткаты для NoCode dropdown'а

`SpendMana(float)`, `RefillMana(float)`, `SpendStamina(float)`, `RefillStamina(float)`,
`SpendShield(float)` — отдельные методы для популярных ресурсов, видны в UnityEvent.

### Статы

| Метод | Что делает |
|---|---|
| `GetStat(string)` | финальная сумма base + level + upgrade + buffs |
| `AddStatBase(string, float)` / `SetStatBase(string, float)` | menu / inventory |
| `UpgradeStrength()` / `UpgradeDexterity()` / `UpgradeVitality()` / `UpgradeIntelligence()` / `UpgradeEndurance()` | Dark-Souls UI |
| `UpgradeStat(string)` | универсальный |

### Бафы / статусы

| Метод | Что делает |
|---|---|
| `ApplyBuff(BuffDefinition)` | SO бафф (из библиотеки) |
| `ApplyBuffById(string)` | по id (ищет в SO и inline) |
| `ApplyInlineBuff(int index)` | inline-баф из `_inlineBuffs[index]` — для pickup-эффектов |
| `RemoveBuff(string)` / `ClearAllBuffs()` |  |
| `ApplyStatus(StatusEffectDefinition)` / `ApplyStatusById(string)` |  |
| `RemoveStatus(string)` / `ClearAllStatuses()` |  |
| `HasBuff(string)` / `HasStatus(string)` |  |

### Level / progression

| Метод | Что делает |
|---|---|
| `SetLevel(int)` / `AddLevel(int)` | поднимает уровень. При `AllStatsEveryLevel` — авто-применение growth. При `ManualUpgradePoints` — выдача очков |
| `AddXp(float)` |  |
| `AddUpgradePoints(int)` |  |
| `CanUpgradeStat(string)` / `GetUpgradeLevel(string)` |  |

### Invulnerability

| Метод | Что делает |
|---|---|
| `LockInvulnerable()` / `UnlockInvulnerable()` | стек (Evade controller использует) |
| `SetInvulnerable(bool)` | direct |

### Network shortcuts

Когда `isNetworked = true` и клиент НЕ сервер — `NetDamage`, `NetHeal`, `NetSpend`,
`NetRefill`, `NetApplyBuffById`, `NetApplyInlineBuff`, `NetApplyStatusById`, `NetAddLevel`
шлют `[Command]` на сервер. Сервер применяет и пушит snapshot SyncVar — все клиенты получают.
Если используешь обычные `Damage` / `Heal` / etc. — они тоже работают, но только локально.

### Save / Load

`SaveProfile()` / `LoadProfile()` / `ResetProfile()` — пишет в `PlayerPrefs[_saveKey]`. Сохраняет
все ресурсы / статы / upgrade points / активные бафы / статусы по id (универсально, без жёстко
зашитых полей).

### Reactive shortcuts

`HpState`, `HpPercentState`, `MaxHpState`, `ManaState`, `ManaPercentState`, `StaminaState`,
`StaminaPercentState`, `LevelState`, `UpgradePointsState`, `XpState`, `IsDeadState`,
`InvulnerableState`.

Универсальные: `GetResourceCurrentState("DarkMana")`, `GetResourceMaxState(id)`,
`GetResourcePercentState(id)`, `GetStatState("Strength")`.

---

## Inspector

| Header | Что внутри |
|---|---|
| **Template** | `RpgCharacterTemplate` (SO) + `applyTemplateOnAwake` flag. Импортит resources / stats / прогрессию из архетипа. |
| **Resources** | список `RpgResourceDefinition`. Каждый — id (preset / Custom), start current / max, регуляции, лимиты, регенерация (`Flat / Percent / FromStat / *PerTick` + паузы после spend / damage). |
| **Stats** | список `RpgStatDefinition`. Каждый — id, base, optional level growth. |
| **Effects** | `_knownBuffs[]` (SO), `_inlineBuffs[]` (без SO), `_knownStatuses[]` (SO). |
| **Progression** | `RpgProgressionDefinition` SO + опциональный `LevelComponent`. |
| **Persistence** | save key, load on awake, autosave. |
| **Authority** | `None` / `OwnerOnly` / `ServerOnly` — фильтр Command'ов. |
| **Events** | OnDamaged / OnHealed / OnDeath / OnRevived / OnBuffApplied / OnBuffExpired / OnStatusApplied / OnStatusExpired / OnLevelChanged / OnResourceChanged(id, value) / OnStatChanged(id, value) / OnProfileSaved / OnProfileLoaded. |

---

## Дочерние компоненты для UI / NoCode

### `RpgResourceBinding`
Drop на UI GameObject, drag `RpgCharacter`, pick resource id (например `Custom = "DarkMana"`).
UnityEvent `OnCurrent(float)` / `OnMax(float)` / `OnPercent(float)` идут в Slider / TMP_Text без кода.

### `RpgStatBinding`
То же для статов. `OnValue(float)`.

---

## NoCode-сценарии

### Pickup +20 max HP на 60 секунд
1. На игроке: `RpgCharacter` с одним `InlineBuffEntry`:
   - `id = "BigHpBoost"`, `duration = 60`
   - `Modifiers[0]`: `BuffStatType = AddResourceMaxFlat`, `TargetId = Hp`, `Value = 20`
2. На триггер-кубе: `NetworkContextActionRelay`:
   - `Action = InvokeComponentMethod`, `Component = RpgCharacter`, `Method = ApplyInlineBuff`, `Argument = 0`

### Зелье в инвентаре восстанавливает 50 Stamina
`Button.onClick` → `RpgCharacter.RefillStamina(50)`.

### GameOver когда HP < 30%
`NeoCondition`:
- `Source = RpgCharacter`, `Property = HpPercentValue`, `op = <`, `threshold = 0.3`
- `OnTrue` → `GameOverPanel.SetActive(true)`

### Stamina-бар в UI без кода
`Slider` + `RpgResourceBinding` (`Character = Player`, `ResourceId = Stamina`) →
`OnPercent → Slider.value`.

### Зона яда (DoT)
`PhysicsEvents3D.OnTriggerStay` → `RpgCharacter.ApplyStatusByName("Poison")`.

### Dark Souls-style апгрейды
Прогрессия: `RpgProgressionDefinition` с `growthMode = ManualUpgradePoints`,
`upgradeRules = [{ statId = Vitality, increasePerPoint = 1, derivedResourceModifiers = [{ Hp, AddMaxFlat, 15 }] }]`.
UI кнопка → `RpgCharacter.UpgradeVitality()`.

### Dota-style auto-growth
`RpgProgressionDefinition` с `growthMode = AllStatsEveryLevel`. На levelUp от `LevelComponent`
все статы с `affectedByLevel=true` авто-пересчитаются.

### Две маны (Mana + DarkMana)
В `_resources[]`: `Mana` (preset) + `DarkMana` (Custom string). Заклинание тьмы:
`Button.onClick` → `RpgCharacter.Spend("DarkMana", 25)` или через `NetworkContextActionRelay`.

---

## Multiplayer

`RpgCharacter : NeoNetworkComponent`. Включи `isNetworked` в Inspector — компонент становится
сетевым:

1. **Server is authority.** Изменения через `NetDamage` / `NetHeal` / `Net*` идут на сервер.
2. **Snapshot SyncVar.** Сервер сериализует все ресурсы / статы / бафы / статусы / level / xp /
   upgradePoints / isDead / invulLocks в строку snapshot. Все клиенты получают через
   `[SyncVar(hook)]` и восстанавливают локальное состояние.
3. **Authority Mode** — `None` / `OwnerOnly` (только клиент-владелец) / `ServerOnly` (только сервер).
4. **Late join.** Когда новый клиент подключается, `ApplyNetworkState` (наследуется от
   `NeoNetworkComponent`) применяет последний snapshot.

Test multiplayer:
- Host + remote через `NetworkManagerHUD` / `NeoNetworkManager`.
- Триггер pickup на сцене с `NetworkContextActionRelay.InvokeComponentMethod →
  RpgCharacter.ApplyInlineBuff(0)` — оба игрока видят результат.

---

## NPC

NPC — это тот же `RpgCharacter`, отдельных компонентов нет.

1. На префабе врага: `RpgCharacter` + `RpgCharacterTemplate` (например "Orc"):
   - resources: HP 80, Stamina 50
   - stats: Strength 10, Defense 5
2. + `NpcRpgCombatBrain` (поле `_character` → этот `RpgCharacter`)
3. + `RpgAttackController` (`_characterSource` → этот `RpgCharacter`)
4. + `RpgDeathHandler` (auto-attaches и слушает `OnDeath`)
5. + `RpgHpBarUI` (drop на дочерний Canvas → авто-привязка по родителю)

---

## Бой ближний / дальний

### Melee
- `RpgContactDamage` (`selfCharacter` → этот персонаж) + `targetTag = "Enemy"`. Урон по близости.
- Альтернатива: `MeleeWeapon` (наследник `MonoBehaviour`) + collider trigger → вызывает
  `target.GetComponentInParent<RpgCharacter>().Damage(amount)`.

### Ranged
- `RpgAttackController` с `RpgAttackDefinition` (deliveryType = `Projectile`).
- `RpgProjectile` спавнится из `_projectileSpawnPoint`, при попадании → `Damage` на target's `RpgCharacter`.

### Aura / AoE
- `AuraWeapon` (наследник `MeleeWeapon`) — урон в радиусе с тиком.

---

## См. также

- [RpgCharacterTemplate](RpgCharacterTemplate.md) — SO архетип
- [RpgProgressionDefinition](RpgProgressionDefinition.md) — режимы роста
- [RpgResourceBinding](RpgResourceBinding.md) — NoCode UI binding
- [RpgStatBinding](RpgStatBinding.md)
- [BuffDefinition](BuffDefinition.md), [InlineBuffEntry](InlineBuffEntry.md)
- [Multiplayer_Guide](../Network/Multiplayer_Guide.md)
