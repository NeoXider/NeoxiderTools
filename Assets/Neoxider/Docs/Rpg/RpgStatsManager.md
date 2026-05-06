# RpgStatsManager

Главный компонент управления состоянием персонажа. Поддерживает динамические статы, баффы, дебаффы и сохранение прогресса.

## Содержание
- [Назначение](#назначение)
- [Поля (Inspector)](#поля-inspector)
- [API](#api)
- [События](#события)
- [Пример использования](#пример-использования)
- [См. также](#см-также)

---

## Назначение
`RpgStatsManager` — это «мозг» RPG-героя. Он хранит текущее HP, рассчитывает множители урона и защиты, управляет временем действия баффов и обеспечивает сохранение всех этих данных между игровыми сессиями.

---

## Поля (Inspector)

| Поле | Описание |
|------|----------|
| **Base HP** | Начальное количество здоровья на 1-м уровне. |
| **Stat Growth** | Ссылка на `RpgStatGrowth` (ScriptableObject) для масштабирования статов. |
| **Save Key** | Ключ для хранения в `PlayerPrefs` или файле (напр. "Player_Battle_Stats"). |
| **Auto Save** | Если включено, сохранение происходит при каждом изменении уровня или смерти. |

---

## API

| Метод | Описание |
|-------|----------|
| **TakeDamage(float val)** | Наносит урон с учетом защиты и активных баффов. |
| **Heal(float val)** | Восстанавливает здоровье. |
| **AddBuff(BuffDefinition buff)** | Применяет временный статовый бонус. |
| **SetLevel(int level)** | Устанавливает новый уровень и пересчитывает статы согласно кривой роста. |
| **GetDamageMultiplier()** | Возвращает итоговый множитель исходящего урона. |

---

## События
Компонент предоставляет UnityEvents для легкой связи с UI и анимациями:
- **OnHealthChanged(float current, float max)** — срабатывает при любом изменении HP.
- **OnDeath** — вызывается при падении здоровья до 0.
- **OnLevelChanged(int level)** — срабатывает при смене уровня.

---

## Пример использования

### Получение множителя урона для меча (C#)
```csharp
using Neo.Rpg;

public void OnAttack()
{
    float multiplier = playerStats.GetOutgoingDamageMultiplier();
    float finalDamage = baseWeaponDamage * multiplier;
    // Наносим урон...
}
```

### Отображение в HealthBar (No-Code)
1. Добавьте `RpgHpBarUI` на ваш Canvas.
2. В коде инициализации или через поиск укажите ссылку на `RpgStatsManager`.

---

## См. также
- [RpgCombatant (для врагов)](./RpgCombatant.md)
- [Buff Definition (SO)](./BuffDefinition.md)
- [← Назад к RPG README](./README.md)


## Дополнительные поля

| Поле | Описание |
|------|----------|
| `1f` | 1f. |
| `AutoSave` | Auto Save. |
| `CanPerformActions` | Can Perform Actions. |
| `CurrentHp` | Current Hp. |
| `DefaultSaveKey` | Default Save Key. |
| `HpPercentState` | Hp Percent State. |
| `HpPercentStateValue` | Hp Percent State Value. |
| `HpState` | Hp State. |
| `HpStateValue` | Hp State Value. |
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
| `SaveKey` | Save Key. |
| `_autoSave` | Auto Save. |
| `_buffDefinitions` | Buff Definitions. |
| `_healthProvider` | Health Provider. |
| `_hpRegenPerSecond` | Hp Regen Per Second. |
| `_levelProvider` | Level Provider. |
| `_onBuffApplied` | On Buff Applied. |
| `_onBuffExpired` | On Buff Expired. |
| `_onDamaged` | On Damaged. |
| `_onDeath` | On Death. |
| `_onHealed` | On Healed. |
| `_onProfileLoaded` | On Profile Loaded. |
| `_onProfileSaved` | On Profile Saved. |
| `_onStatusApplied` | On Status Applied. |
| `_onStatusExpired` | On Status Expired. |
| `_statGrowth` | Stat Growth. |
| `_statusDefinitions` | Status Definitions. |
| `true` | True. |