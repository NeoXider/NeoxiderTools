# QuestState

**Что это:** класс `QuestState` (не MonoBehaviour). Хранит состояние одного квеста в рантайме: QuestId, Status (NotStarted, Active, Completed, Failed), прогресс и флаги выполнения по каждой цели. Экземпляры создаёт и хранит только QuestManager. Файл: `Assets/Neoxider/Scripts/Quest/QuestState.cs`, пространство имён: `Neo.Quest`.

**Как с ним работать:**
1. Не создавать вручную (`new QuestState(...)`). Состояние появляется при успешном AcceptQuest в QuestManager.
2. Получать только через менеджер: `QuestState state = QuestManager.Instance.GetState(questId);` или `GetState(questConfig);`. Если квест не принимался — GetState вернёт null.
3. Читать: **state.QuestId**, **state.Status**, **state.GetObjectiveProgress(index)**, **state.IsObjectiveCompleted(index)**, **state.ObjectiveCount**.
4. Менять статус или прогресс только через методы QuestManager (CompleteObjective, FailQuest). Через QuestState менять ничего нельзя.
5. Для сейва: сериализовать список QuestState (класс [Serializable]); при загрузке восстанавливать состояния в менеджере (см. [Saving](Saving.md)).

---

## Поля и свойства

| Член | Описание |
|------|----------|
| QuestId | string. Id квеста. |
| Status | QuestStatus: NotStarted, Active, Completed, Failed. |
| ObjectiveCount | Количество целей. |

---

## Методы

| Метод | Возврат | Описание |
|-------|---------|----------|
| GetObjectiveProgress(int index) | int | Прогресс по цели (для счётчиков — число; индекс 0..ObjectiveCount−1). |
| IsObjectiveCompleted(int index) | bool | true, если цель уже выполнена. |

---

## Использование в UI

По Status показывать квест как активный/завершённый/проваленный. По GetObjectiveProgress(i) и IsObjectiveCompleted(i) строить строки вида «2/3 гоблинов», «Ключ: выполнено». Требуемое число для «N/M» брать из QuestConfig.Objectives[i].RequiredCount.
