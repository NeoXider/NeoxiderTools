# QuestState

Runtime-состояние одного квеста: идентификатор, статус и прогресс по целям. Не MonoBehaviour; экземпляры создаёт и хранит [QuestManager](QuestManager.md). Получать состояние только через **QuestManager.GetState(questId)** или **GetState(QuestConfig)**.

- **Пространство имён:** `Neo.Quest`
- **Путь:** `Assets/Neoxider/Scripts/Quest/QuestState.cs`

---

## Назначение

- Хранит текущий **статус** квеста (NotStarted, Active, Completed, Failed).
- Хранит **прогресс по каждой цели**: для счётчиков — накопленное значение, плюс флаг «цель выполнена».
- Сериализуется для сохранения (см. [Saving](Saving.md)); при загрузке состояния восстанавливаются в QuestManager.

---

## Поля и свойства

| Член | Описание |
|------|----------|
| **QuestId** | ID квеста (из QuestConfig.Id). |
| **Status** | QuestStatus: NotStarted, Active, Completed, Failed. |
| **ObjectiveCount** | Количество целей (размер списков прогресса). |

---

## Методы

| Метод | Описание |
|-------|----------|
| **GetObjectiveProgress(int index)** | Текущий прогресс по цели (для счётчиков — число; для выполненных целей значение уже не меняется). |
| **IsObjectiveCompleted(int index)** | true, если цель с данным индексом выполнена. |

---

## Откуда брать

- Только через **QuestManager**:
  - `QuestState state = QuestManager.Instance.GetState(questId);`
  - `QuestState state = QuestManager.Instance.GetState(questConfig);`
- Создавать QuestState напрямую в коде не нужно — менеджер создаёт их при AcceptQuest.

---

## Сериализация для сейва

Структура состояния (QuestId, Status, прогресс и флаги по целям) сериализуема. Варианты сохранения и загрузки описаны в [Saving](Saving.md).
