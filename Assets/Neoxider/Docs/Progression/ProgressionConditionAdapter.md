# ProgressionConditionAdapter

**Что это:** `MonoBehaviour`-адаптер из `Scripts/Progression/Bridge/ProgressionConditionAdapter.cs`, который превращает состояние прогрессии в проверяемое условие для `NeoCondition` и других систем, работающих через `IConditionEvaluator`.

**Как использовать:**
1. Добавьте `ProgressionConditionAdapter` на объект.
2. Назначьте `ProgressionManager` или оставьте пустым для singleton.
3. Выберите `Evaluation Mode`.
4. Заполните `Node Id`, `Perk Id` или `Threshold`.
5. Используйте `EvaluateCurrent()` напрямую или настройте `NeoCondition` на вызов через контекст.

**Навигация:** [← К Progression](./README.md)

---

## Режимы проверки

| Evaluation Mode | Что проверяет |
|----------------|---------------|
| `HasUnlockedNode` | Есть ли разблокированный узел |
| `HasPurchasedPerk` | Куплен ли перк |
| `LevelAtLeast` | Достигнут ли уровень не ниже `Threshold` |
| `XpAtLeast` | Набрано ли XP не меньше `Threshold` |
| `PerkPointsAtLeast` | Есть ли нужное количество perk points |

## Поля

| Поле | Назначение |
|------|------------|
| `_manager` | Явная ссылка на `ProgressionManager` |
| `_mode` | Тип проверки |
| `_nodeId` | ID узла |
| `_perkId` | ID перка |
| `_threshold` | Числовой порог |
| `_invert` | Инверсия результата |

## Когда полезен

- Открывать UI только после достижения уровня.
- Включать кнопку перка только после открытия prerequisite-узла.
- Реагировать на состояние прогрессии через существующий `Condition` workflow.
