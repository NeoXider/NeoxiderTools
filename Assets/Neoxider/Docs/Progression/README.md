# Progression

**Что это:** модуль мета-прогрессии для `XP`, уровней, `unlock tree`, `perk tree` и постоянного профиля игрока. Скрипты находятся в `Scripts/Progression/`.

**Оглавление:**
- [ProgressionManager](./ProgressionManager.md) — главный runtime-компонент и точка входа.
- [ProgressionNoCodeAction](./ProgressionNoCodeAction.md) — no-code bridge для UnityEvent.
- [ProgressionConditionAdapter](./ProgressionConditionAdapter.md) — адаптер условий для `NeoCondition` и других систем.

**Навигация:** [← К Docs](../README.md)

---

## Как использовать

1. Создайте asset'ы `LevelCurveDefinition`, `UnlockTreeDefinition` и `PerkTreeDefinition` через меню `Neoxider/Progression`.
2. Добавьте `ProgressionManager` на сцену и назначьте нужные definitions.
3. Настройте `Save Key`, если для проекта нужен отдельный профиль или несколько профилей.
4. Подключите UI к `XpState`, `LevelState`, `PerkPointsState`, `XpToNextLevelState` или к UnityEvent менеджера.
5. Вызывайте `AddXp`, `TryUnlockNode`, `TryBuyPerk` из кода или через `ProgressionNoCodeAction`.
6. Для no-code проверок используйте `ProgressionConditionAdapter` или свойства менеджера через `NeoCondition`.

## Что входит в модуль

- `ProgressionManager` — загрузка, сохранение, расчёт уровней, покупка перков, разблокировка узлов.
- `LevelCurveDefinition` — пороги XP, выдача perk points и награды за уровень.
- `UnlockTreeDefinition` — узлы разблокировки, prerequisite-связи, условия и награды.
- `PerkTreeDefinition` — покупаемые перки, стоимость в perk points, зависимости и награды.
- `ProgressionReward` — data-driven reward dispatch в `Money`, `Collection`, `Quest`, XP и perk points.

## Persistence

- Профиль хранится через `SaveProvider`, а не через сценовый `SaveManager`.
- Это делает модуль независимым от иерархии сцены и пригодным для мета-прогрессии.
- По умолчанию используется ключ `ProgressionV2.Profile`, но его можно поменять в `ProgressionManager`.

## Сценарии для игр

### Arcade / Hypercasual

Подходит, когда прогрессия нужна как лёгкий мета-слой между сессиями.

- `LevelCurveDefinition`: 5-15 уровней, быстрые пороги XP.
- `UnlockTreeDefinition`: открытие новых скинов, арен, бустеров.
- `PerkTreeDefinition`: лучше держать маленьким или отключить совсем.
- `Save Key`: один общий профиль на игрока.

Рекомендуемые настройки:
- быстрый рост `RequiredXp`
- rewards за уровень: `Money`, `UnlockCollectionItem`
- минимальное число prerequisite-связей

### RPG / Action RPG

Подходит, когда нужен основной слой развития персонажа.

- `LevelCurveDefinition`: длинная кривая уровней с выдачей `perk points`.
- `UnlockTreeDefinition`: классовые ветки, специализации, доступ к системам.
- `PerkTreeDefinition`: основное дерево билдов.
- rewards: `AcceptQuest`, `Money`, дополнительные `PerkPoints`

Рекомендуемые настройки:
- уровни растут плавно, без резких скачков в начале
- `RequiredUnlockNodeIds` использовать для gate между ветками
- `ConditionEntry` использовать для сюжетных или системных требований

### Strategy / Base Builder

Подходит для мета-прогрессии аккаунта, технологий и построек.

- `UnlockTreeDefinition`: технологии, здания, новые юниты
- `PerkTreeDefinition`: глобальные бонусы фракции или командира
- `LevelCurveDefinition`: account progression или commander progression

Рекомендуемые настройки:
- unlock nodes делать узлами технологий
- perks использовать для пассивных баффов
- rewards за уровень: валюта, доступ к новым узлам, запуск квестов

### Narrative / Adventure

Подходит, когда прогрессия нужна как мягкий gate для контента.

- `UnlockTreeDefinition`: главы, локации, сюжетные флаги
- `PerkTreeDefinition`: можно не использовать или оставить небольшим
- `ConditionEntry`: удобно для сюжетных требований

Рекомендуемые настройки:
- делать упор на `UnlockTreeDefinition`
- rewards чаще вести в `Quest`
- XP можно выдавать за прохождение ключевых событий, а не за бой

### Roguelite / Meta Run

Подходит для постоянной прогрессии между забегами.

- `LevelCurveDefinition`: account XP или profile XP
- `PerkTreeDefinition`: постоянные мета-улучшения
- `UnlockTreeDefinition`: новые режимы, оружие, стартовые бонусы

Рекомендуемые настройки:
- `ResetProgression()` не использовать как часть run reset
- run-прогресс хранить отдельно от `Progression V2`
- meta rewards выдавать между сессиями через `SaveProvider`

## Рекомендуемые настройки по частям

### LevelCurveDefinition

- Для короткой игры: 5-10 уровней, частые rewards.
- Для длинной игры: 20+ уровней, `GrantedPerkPoints` не на каждом уровне.
- Первый уровень лучше оставлять на `0 XP`.

### UnlockTreeDefinition

- Используйте `UnlockedByDefault` только для базовых узлов.
- Не перегружайте node prerequisites длинными цепочками без явной пользы.
- Если узел зависит от сюжета или сцены, используйте `ConditionEntry`.

### PerkTreeDefinition

- `Cost` должен масштабироваться медленнее, чем растёт XP, иначе дерево перестанет ощущаться живым.
- `RequiredUnlockNodeIds` удобно использовать как gate между специализациями.
- Для маленьких игр держите дерево на 5-12 перков, для крупных можно идти в 20+.

## Примеры использования

### Выдать XP из кода

```csharp
using Neo.Progression;

public class EnemyRewardExample : MonoBehaviour
{
    [SerializeField] private int xpReward = 25;

    public void GrantReward()
    {
        if (ProgressionManager.Instance != null)
        {
            ProgressionManager.Instance.AddXp(xpReward);
        }
    }
}
```

### Разблокировать узел из события

```csharp
using Neo.Progression;

public class QuestUnlockExample : MonoBehaviour
{
    [SerializeField] private string nodeId = "chapter-2";

    public void UnlockChapter()
    {
        if (ProgressionManager.Instance == null)
        {
            return;
        }

        ProgressionManager.Instance.TryUnlockNode(nodeId, out _);
    }
}
```

### No-code сценарий

1. Добавьте `ProgressionNoCodeAction`.
2. Выберите `Action Type = AddXp`.
3. Укажите `XP Amount = 100`.
4. Привяжите `Execute()` к `Button.onClick`, `Quest` или `UnityEvent`.

## Демонстрация

Работа модуля Progression с получением опыта и ростом уровней представлена в сцене `Samples~/Demo/Progression_Demo.unity`.
