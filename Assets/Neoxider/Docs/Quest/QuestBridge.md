# Quest NoCode Action

**Что это:** универсальный мост для вызова квестового API из UnityEvent без кода.
Основной компонент: `QuestNoCodeAction` (`Scripts/Quest/Bridge/QuestNoCodeAction.cs`).

**Как использовать:** см. блоки ниже (параметры, метод, подключение) и [Scenarios](Scenarios.md).

---

**Quest NoCode Action** (`Bridge/QuestNoCodeAction.cs`)
- Назначение: универсальный no-code компонент для большинства runtime-действий с квестами.
- Параметры:
  - **Action Type**: `Accept`, `CompleteObjective`, `Fail`, `Restart`, `Reset`, `ResetAll`.
  - **Quest**: нужен для всех действий, кроме `ResetAll`.
  - **Objective Index**: используется в `CompleteObjective`.
  - **Flow Config**: опционально; если задан и действие = `Accept`, применяется проверка `QuestFlowConfig.CanAcceptQuest(...)`.
- Метод: `Execute()` — без аргументов (под UnityEvent).
- События:
  - **On Success** (`UnityEvent`)
  - **On Failed** (`UnityEvent<string>`)
  - **On Result Message** (`UnityEvent<string>`)

### No-code покрытие (что можно без кода)

- Принять квест — `QuestNoCodeAction(Accept)`.
- Засчитать цель — `QuestNoCodeAction(CompleteObjective)`.
- Провалить квест — `QuestNoCodeAction(Fail)`.
- Перезапустить квест — `QuestNoCodeAction(Restart)`.
- Сбросить один квест — `QuestNoCodeAction(Reset)`.
- Сбросить все квесты — `QuestNoCodeAction(ResetAll)`.
- Ограничить принятие по последовательности — через `QuestFlowConfig` + `QuestNoCodeAction(Accept)` с Flow Config.

Пошаговые сценарии настройки в инспекторе — [Scenarios](Scenarios.md).
