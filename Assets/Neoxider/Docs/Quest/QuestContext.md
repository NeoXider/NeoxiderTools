# QuestContext

**Что это:** простой marker-компонент (`MonoBehaviour`) для объекта, который используется как контекст при проверке `Start Conditions` в `QuestManager`.

**Зачем нужен:** когда `QuestManager.AcceptQuest(...)` проверяет `ConditionEntry`, он передаёт в `Evaluate(context)` объект из поля `Condition Context`. `QuestContext` помогает явно отметить этот объект в сцене.

---

## Как использовать

1. Добавьте `QuestContext` на объект игрока (или world/game-state объект).
2. В `QuestManager` назначьте этот объект в поле `Condition Context`.
3. В `QuestConfig.StartConditions` настройте проверки через `NeoCondition` API.

---

## Примечания

- Компонент не содержит логики и используется как marker для удобства и читаемости сцены.
- Если `Condition Context` в `QuestManager` не задан, менеджер использует свой `gameObject`.

---

## См. также

- [QuestManager](QuestManager.md)
- [QuestConfig](QuestConfig.md)
- [Condition / NeoCondition](../Condition/NeoCondition.md)
