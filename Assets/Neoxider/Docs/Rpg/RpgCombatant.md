# RpgCombatant

Облегченная версия системы параметров для NPC, разрушаемых объектов и врагов, не требующих сохранения.

## Содержание
- [Назначение](#назначение)
- [Поля (Inspector)](#поля-inspector)
- [API](#api)
- [События](#события)
- [Пример использования](#пример-использования)
- [См. также](#см-также)

---

## Назначение
`RpgCombatant` используется там, где не нужен полноценный `RpgStatsManager` с сохранением прогресса. Он идеально подходит для стаи монстров, ящиков или стен. Позволяет задать HP, уровень и обрабатывать входящий урон.

---

## Поля (Inspector)

| Поле | Описание |
|------|----------|
| **Initial HP** | Количество здоровья при спавне. |
| **Level** | Влияет на сложность (если подключен `StatGrowth`). |
| **Armor** | Поглощение входящего урона (фиксированное число). |
| **Immortal** | Если включено, объект получает события о попаданиях, но здоровье не уменьшается. |
| **Auto Grant XP** | Если включено, при смерти этот объект передаст опыт в `ProgressionManager` игрока. |

---

## API

| Метод | Описание |
|-------|----------|
| **TakeDamage(float damage, GameObject source)** | Основной метод получения урона. |
| **SetLevel(int level)** | Динамическое изменение уровня существа. |
| **RestoreHP(float val)** | Лечение. |
| **GetHealthPercent()** | Возвращает процент HP (0..1) для UI. |

---

## События
- **OnHealthChanged(float val)** — текущее здоровье.
- **OnDeath** — вызывается при смерти.
- **OnHit** — вызывается при любом полученном уроне.

---

## Пример использования

### Настройка «Взрывающейся бочки» (No-Code)
1. Добавьте `RpgCombatant`.
2. В секции **Logic** привяжите событие `OnDeath` к вызову вашего метода `Explosion.Bang()`.
3. Установите `Initial HP = 1`.

### Передача опыта игроку
Включите галочку **Auto Grant XP To Player**. Теперь при убийстве этого NPC через `MeleeWeapon` или `AuraWeapon`, игрок автоматически получит опыт в `ProgressionManager`.

---

## См. также
- [RpgStatsManager (для игрока)](./RpgStatsManager.md)
- [DemoNpcUI (авто-полоска HP)](./DemoNpcUI.md)
- [← Назад к RPG README](./README.md)


## Дополнительные поля

| Поле | Описание |
|------|----------|
| `100f` | 100f. |
| `ActiveBuffs` | Active Buffs. |
| `ActiveStatuses` | Active Statuses. |
| `CanPerformActions` | Can Perform Actions. |
| `CurrentHp` | Current Hp. |
| `HpPercentState` | Hp Percent State. |
| `HpPercentStateValue` | Hp Percent State Value. |
| `HpState` | Hp State. |
| `HpStateValue` | Hp State Value. |
| `InvulnerableState` | Invulnerable State. |
| `InvulnerableStateValue` | Invulnerable State Value. |
| `IsDead` | Is Dead. |
| `IsInvulnerable` | Is Invulnerable. |
| `LevelState` | Level State. |
| `LevelStateValue` | Level State Value. |
| `MaxHp` | Max Hp. |
| `OnBuffApplied` | On Buff Applied. |
| `OnBuffExpired` | On Buff Expired. |
| `OnDamaged` | On Damaged. |
| `OnHealed` | On Healed. |
| `OnStatusApplied` | On Status Applied. |
| `OnStatusExpired` | On Status Expired. |
| `_buffDefinitions` | Buff Definitions. |
| `_healthProvider` | Health Provider. |
| `_hpRegenPerSecond` | Hp Regen Per Second. |
| `_levelProvider` | Level Provider. |
| `_onBuffApplied` | On Buff Applied. |
| `_onBuffExpired` | On Buff Expired. |
| `_onDamaged` | On Damaged. |
| `_onDeath` | On Death. |
| `_onHealed` | On Healed. |
| `_onStatusApplied` | On Status Applied. |
| `_onStatusExpired` | On Status Expired. |
| `_onXpRewardGenerated` | On Xp Reward Generated. |
| `_regenInterval` | Regen Interval. |
| `_statGrowth` | Stat Growth. |
| `_statusDefinitions` | Status Definitions. |
| `_xpRewardOverride` | Xp Reward Override. |
| `true` | True. |