# Quest — модуль квестов

**Что это:** папка документации по модулю квестов. Описывает скрипты в `Scripts/Quest/` и как с ними работать.

**Как с этим работать:**
- Нужно создать квест → открой [QuestConfig](QuestConfig.md).
- Нужно принять/зачесть квест в сцене → открой [QuestManager](QuestManager.md) и при необходимости [QuestBridge](QuestBridge.md).
- Нужно прочитать прогресс или сохранить → [QuestState](QuestState.md), [Saving](Saving.md).
- Настраиваешь в инспекторе (кнопки, UnityEvent) → [Сценарии в инспекторе](Scenarios.md).
- Пишешь код (AcceptQuest, события, NotifyKill) → [Code](Code.md).

**Навигация:** [← К Docs](../README.md) · оглавление — таблица ниже

---

## Оглавление документов

| Документ | О чём |
|----------|--------|
| [QuestConfig](QuestConfig.md) | Ассет Quest Config (ScriptableObject): поля, типы целей, условия старта. |
| [QuestManager](QuestManager.md) | Компонент QuestManager в сцене: методы, события, Condition Context, Known Quests. |
| [QuestState](QuestState.md) | Класс состояния квеста: откуда брать, что читать, сериализация. |
| [QuestBridge](QuestBridge.md) | Компоненты Quest Accept Trigger и Quest Objective Notifier. |
| [Scenarios](Scenarios.md) | Пошаговые сценарии в инспекторе: кнопка принятия, цель по условию, UI по событиям. |
| [Code](Code.md) | Вызовы из C#: AcceptQuest, CompleteObjective, события, NotifyKill/NotifyCollect. |
| [Saving](Saving.md) | Сохранение и загрузка списка QuestState. |

---

## Поток данных

```
QuestConfig (SO)  →  Known Quests в QuestManager  →  AcceptQuest(id) проверяет Start Conditions по Condition Context
                                                         ↓
                                              Создаётся QuestState, события OnQuestAccepted
                                                         ↓
CompleteObjective(id, index) / NotifyKill / NotifyCollect  →  обновление прогресса, при всех целях — OnQuestCompleted
```

Конфиг задаёт «что сделать»; менеджер хранит состояния и вызывает события. Состояние получать только через `QuestManager.GetState(...)`.

---

## Структура кода

| Файл | Назначение |
|------|------------|
| `Scripts/Quest/QuestConfig.cs` | ScriptableObject квеста. |
| `Scripts/Quest/QuestObjectiveData.cs` | Одна цель: Type, TargetId, RequiredCount, Condition. |
| `Scripts/Quest/QuestStatus.cs` | enum: NotStarted, Active, Completed, Failed. |
| `Scripts/Quest/QuestState.cs` | Состояние одного квеста (прогресс, флаги). |
| `Scripts/Quest/QuestManager.cs` | Компонент в сцене: реестр состояний, Accept/Complete/Fail, события. |
| `Scripts/Quest/QuestContext.cs` | Маркер объекта для Condition Context (опционально). |
| `Scripts/Quest/Bridge/QuestAcceptTrigger.cs` | Вызов AcceptQuest из UnityEvent (например On Click). |
| `Scripts/Quest/Bridge/QuestObjectiveNotifier.cs` | Вызов CompleteObjective из UnityEvent (например NeoCondition.On True). |

Зависимость: **Neo.Condition** (ConditionEntry) для Start Conditions и опционально для целей по условию.
