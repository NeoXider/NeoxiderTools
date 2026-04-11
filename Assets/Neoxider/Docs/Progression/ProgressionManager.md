# ProgressionManager

**Что это:** `MonoBehaviour`-менеджер мета-прогрессии из `Scripts/Progression/Runtime/ProgressionManager.cs`. Отвечает за XP, уровни, perk points, unlock nodes, perks и сохранение профиля через `SaveProvider`.

**Как использовать:**
1. Добавьте `ProgressionManager`, `LevelComponent` (и опционально `UnlockContext`, `PerkContext`) на объект сцены (например, игрока или оружие).
2. Назначьте `LevelCurveDefinition`, `UnlockTreeDefinition` и `PerkTreeDefinition`.
3. При необходимости задайте свой `Save Key` (например `Weapon_Sword_Progression`).
4. Вызывайте `AddXp`, `TryUnlockNode`, `TryBuyPerk`, `ResetProgression`, `LoadProfile`, `SaveProfile`.
5. Для UI используйте `XpState`, `LevelState`, `PerkPointsState`, `XpToNextLevelState` или UnityEvent.
6. Для поддержки премиум-треков (BattlePass) используйте метод `ActivatePremium()`.

**Навигация:** [← К Progression](./README.md)

---

## Основные поля

| Поле | Тип | Назначение |
|------|-----|------------|
| `_levelCurve` | `LevelCurveDefinition` | Порог XP для уровней и награды за уровень |
| `_unlockTree` | `UnlockTreeDefinition` | Узлы разблокировки |
| `_perkTree` | `PerkTreeDefinition` | Дерево перков |
| `_saveKey` | `string` | Ключ сохранения профиля |
| `_conditionContext` | `GameObject` | Контекст для `ConditionEntry` |

## Reactive state

| Поле | Тип | Назначение |
|------|-----|------------|
| `XpState` | `ReactivePropertyInt` | Текущее XP |
| `LevelState` | `ReactivePropertyInt` | Текущий уровень |
| `PerkPointsState` | `ReactivePropertyInt` | Доступные perk points |
| `XpToNextLevelState` | `ReactivePropertyInt` | Остаток XP до следующего уровня |

## Основные методы

| Метод | Что делает |
|------|------------|
| `AddXp(int amount)` | Добавляет XP и пересчитывает уровень |
| `AddPerkPoints(int amount)` | Добавляет свободные perk points |
| `TryUnlockNode(string nodeId, out string failReason)` | Пытается разблокировать узел |
| `TryBuyPerk(string perkId, out string failReason)` | Пытается купить перк |
| `HasUnlockedNode(string nodeId)` | Проверяет, разблокирован ли узел |
| `HasPurchasedPerk(string perkId)` | Проверяет, куплен ли перк |
| `ResetProgression()` | Сбрасывает профиль и применяет default states |
| `LoadProfile()` / `SaveProfile()` | Загружает или сохраняет профиль |

## Интеграции

- `SaveProvider` — хранение профиля.
- `ConditionEntry` — условия для unlock nodes и perks.
- `Money`, `Collection`, `QuestManager` — получатели rewards через `ProgressionReward`.

## Когда использовать

- Нужна постоянная мета-прогрессия игрока.
- Нужен общий manager для UI, no-code сценариев и кода.
- Нужен отдельный профиль, не связанный с конкретной сценой.

## Пример кода

```csharp
using Neo.Progression;
using UnityEngine;

public class ProgressionRewardExample : MonoBehaviour
{
    [SerializeField] private ProgressionManager targetProgression;
    [SerializeField] private int xpReward = 50;
    [SerializeField] private string unlockNodeId = "weapon-tier-2";

    public void GrantXp()
    {
        if (targetProgression != null)
        {
            targetProgression.AddXp(xpReward);
        }
    }

    public void UnlockTier()
    {
        if (targetProgression != null)
        {
            targetProgression.TryUnlockNode(unlockNodeId, out _);
        }
    }
    
    public void PurchaseBattlepass()
    {
        if (targetProgression != null)
        {
            // Activate Premium track retroactively granting rewards for past levels
            targetProgression.ActivatePremium();
        }
    }
}
```

## Типичные схемы настройки

### Минимальный metagame

- Только `LevelCurveDefinition`
- без `UnlockTreeDefinition`
- без `PerkTreeDefinition`
- rewards за уровень: валюта или косметика

### Классическая RPG

- `LevelCurveDefinition` + `UnlockTreeDefinition` + `PerkTreeDefinition`
- `GrantedPerkPoints` выдаются за уровни
- perks покупаются за points
- unlock nodes открывают доступ к новым веткам

### Battle Pass

- `LevelCurveDefinition` с несколькими `ProgressionRewards` 
- Часть наград помечена `IsPremiumOnly = true`
- Вызов `ProgressionManager.ActivatePremium()` при покупке премиум-доступа
- Разделение профилей каждого сезона через смена `Save Key`

### Сюжетная игра

- лёгкий `LevelCurveDefinition`
- основной акцент на `UnlockTreeDefinition`
- `ConditionContext` связан с объектом, который содержит сюжетные флаги
