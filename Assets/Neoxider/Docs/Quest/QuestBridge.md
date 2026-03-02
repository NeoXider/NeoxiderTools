# Quest Accept Trigger и Quest Objective Notifier

**Что это:** два моста для вызова квестового API из UnityEvent: QuestAcceptTrigger — AcceptQuest по конфигу; QuestObjectiveNotifier — CompleteObjective по конфигу и индексу цели. Файлы: `Scripts/Quest/Bridge/QuestAcceptTrigger.cs`, `QuestObjectiveNotifier.cs`.

**Как использовать:** см. блоки ниже (параметры, метод, подключение) и [Scenarios](Scenarios.md).

---

**Quest Accept Trigger** (`Bridge/QuestAcceptTrigger.cs`)
- Назначение: вызов `QuestManager.AcceptQuest(quest)` из UnityEvent.
- Параметры: **Quest** (QuestConfig). Конфиг должен быть в Known Quests у QuestManager.
- Метод: `AcceptQuest()` — без аргументов. Кнопка в инспекторе: [Accept Quest].
- Подключение: Button On Click (или любой UnityEvent) → этот объект → `QuestAcceptTrigger.AcceptQuest()`.

**Quest Objective Notifier** (`Bridge/QuestObjectiveNotifier.cs`)
- Назначение: вызов `QuestManager.CompleteObjective(questId, objectiveIndex)` из UnityEvent.
- Параметры: **Quest** (QuestConfig), **Objective Index** (int, 0-based — порядок в Objectives конфига).
- Метод: `NotifyComplete()` — без аргументов. Кнопка в инспекторе: [Notify Complete].
- Подключение: NeoCondition On True / OnTriggerEnter / кнопка и т.д. → этот объект → `QuestObjectiveNotifier.NotifyComplete()`.

Пошаговые сценарии настройки в инспекторе — [Scenarios](Scenarios.md).
