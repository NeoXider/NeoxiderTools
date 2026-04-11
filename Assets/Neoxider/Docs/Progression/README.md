# Модуль Progression

Модуль мета-прогрессии для управления опытом (XP), уровнями, деревьями разблокировок (unlock tree) и перками (perk tree). Позволяет создавать глубокую систему развития игрока или аккаунта.

## Содержание
- [Назначение](#назначение)
- [Оглавление файлов](#оглавление-файлов)
- [Как использовать](#как-использовать)
- [Сценарии для игр](#сценарии-для-игр)
- [Примеры использования](#примеры-использования)
- [См. также](#см-также)

---

## Назначение
Модуль решает задачу создания долгосрочной прогрессии, которая сохраняется между сессиями. Он обеспечивает связь между игровыми действиями (получение опыта) и наградами (новые способности, контент, статы).

---

## Оглавление файлов
- [ProgressionManager](./ProgressionManager.md) — главный runtime-компонент и точка входа.
- [ProgressionNoCodeAction](./ProgressionNoCodeAction.md) — no-code bridge для UnityEvent.
- [ProgressionConditionAdapter](./ProgressionConditionAdapter.md) — адаптер условий для `NeoCondition`.

---

## Как использовать

1. **Создание данных**: Создайте asset'ы `LevelCurveDefinition`, `UnlockTreeDefinition` и `PerkTreeDefinition` через меню `Neoxider/Progression`.
2. **Менеджер**: Добавьте `ProgressionManager` на сцену и назначьте созданные asset'ы.
3. **Сохранение**: Настройте `Save Key` для уникальности профиля.
4. **Интерфейс**: Подключите UI к `XpState`, `LevelState` или событиям менеджера.
5. **Геймплей**: Вызывайте `AddXp()` при выполнении задач или убийстве врагов.

---

## Сценарии для игр

### Roguelite / Meta Run
Постоянная прогрессия между забегами.
- `LevelCurveDefinition`: опыт аккаунта.
- `PerkTreeDefinition`: постоянные улучшения статов.
- `UnlockTreeDefinition`: открытие новых видов оружия или режимов.

### RPG / Action RPG
Основной слой развития персонажа.
- Выдача `Perk Points` на каждом уровне.
- Ветки специализаций через `UnlockTree`.

---

## Примеры использования

### 1. Выдача опыта (C# и No-Code)

**Из кода:**
```csharp
using Neo.Progression;
using UnityEngine;

public class QuestReward : MonoBehaviour
{
    [SerializeField] private int xpReward = 500;

    public void CompleteQuest()
    {
        if (ProgressionManager.HasInstance)
        {
            ProgressionManager.I.AddXp(xpReward);
        }
    }
}
```

**Через No-Code:**
1. На объект-триггер добавьте `ProgressionNoCodeAction`.
2. Установите `Action Type = AddXp` и `Amount = 500`.
3. Вызовите метод `Execute()` из UnityEvent (например, после завершения диалога или смерти босса).

### 2. Разблокировка способностей или локаций (C#)

```csharp
using Neo.Progression;
using UnityEngine;

public class SkillUnlocker : MonoBehaviour
{
    [SerializeField] private string skillNodeId = "double_jump";

    public void TryUnlockDoubleJump()
    {
        if (ProgressionManager.I.TryUnlockNode(skillNodeId, out string error))
        {
            Debug.Log("Двойной прыжок разблокирован!");
        }
        else
        {
            Debug.LogWarning($"Не удалось разблокировать: {error}");
        }
    }
}
```

### 3. Покупка перков за очки (C#)

```csharp
using Neo.Progression;
using UnityEngine;

public class PerkShop : MonoBehaviour
{
    public void BuyDamagePerk()
    {
        // Попытка купить перк. Цена спишется автоматически, если хватает очков.
        if (ProgressionManager.I.TryBuyPerk("damage_up_1", out string failReason))
        {
            Debug.Log("Урон увеличен!");
        }
        else
        {
            Debug.LogError($"Ошибка покупки: {failReason}");
        }
    }
}
```

---

## См. также
- [Core Module (Level/Health)](../Core/README.md)
- [RPG Module](../Rpg/README.md)
- [Save System](../Save/README.md)
- ← [Назад к общему оглавлению](../README.md)
