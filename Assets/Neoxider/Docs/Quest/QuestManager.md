# QuestManager

**Что это:** компонент `QuestManager` (MonoBehaviour). В сцене должен быть один экземпляр. Хранит список состояний квестов (QuestState), принимает и завершает квесты, засчитывает цели, проверяет Start Conditions и вызывает события. Файл: `Assets/Neoxider/Scripts/Quest/QuestManager.cs`, пространство имён: `Neo.Quest`.

**Как с ним работать:**
1. Добавить на GameObject в сцену: **Add Component → Neoxider → Quest → QuestManager**.
2. В **Known Quests** перетащить все QuestConfig, по Id которых будет вызываться AcceptQuest(questId).
3. В **Condition Context** указать GameObject для проверки Start Conditions (обычно игрок). Если пусто — используется gameObject менеджера.
4. Принимать квест: из кода `QuestManager.Instance.AcceptQuest(quest)` или через `QuestNoCodeAction` + UnityEvent (On Click).
5. Засчитывать цели: `CompleteObjective(questId, index)` или `QuestNoCodeAction(CompleteObjective)` по NeoCondition.On True; для KillCount/CollectCount — вызывать NotifyKill(enemyId) / NotifyCollect(itemId) из своей логики.

---

## Доступ

- **QuestManager.Instance** — синглтон. Может быть null до Awake менеджера или после Destroy. Проверяйте на null при отписке от событий и в сценах без менеджера.

---

## Поля в инспекторе

| Поле | Назначение |
|------|------------|
| **Condition Context** | GameObject, передаваемый в ConditionEntry.Evaluate(context) при проверке Start Conditions. |
| **Known Quests** | Список QuestConfig. По нему разрешается questId → конфиг при AcceptQuest(string), GetState(string) и т.д. |
| **Editor Quest Id** / **Editor Objective Index** | Для кнопок в блоке Editor: тестовый приём квеста и зачёт цели по введённому Id и индексу. |

---

## Методы

| Метод | Возврат | Описание |
|-------|---------|----------|
| AcceptQuest(string questId) | bool | Принять по Id. Ищет конфиг в Known Quests, проверяет Start Conditions, создаёт QuestState, вызывает события. |
| AcceptQuest(QuestConfig quest) | bool | То же по ссылке на конфиг. |
| TryAcceptQuest(string questId, out string failReason) | bool | Принять по Id; при неудаче в failReason: "Quest not found.", "Already accepted or completed.", "Start conditions not met." |
| CompleteObjective(string questId, int objectiveIndex) | void | Зачесть цель. Для счётчиков — +1; при достижении RequiredCount цель закрывается. При всех целях выполненных — квест Completed, OnQuestCompleted. |
| CompleteObjective(QuestConfig quest, int objectiveIndex) | void | То же по конфигу. |
| FailQuest(string questId) / FailQuest(QuestConfig quest) | void | Перевести квест в Failed, вызвать OnQuestFailed. |
| ResetQuest(string questId) / ResetQuest(QuestConfig quest) | bool | Удалить состояние квеста из реестра (сброс). Можно заново пройти квест. |
| RestartQuest(string questId) / RestartQuest(QuestConfig quest) | bool | Перезапустить квест: сбросить состояние и снова принять квест. Удобно для повторного прохождения. |
| ResetAllQuests() | void | Полный сброс всех квестов (Active/Completed/Failed). |
| NotifyKill(string enemyId) | void | +1 к прогрессу по целям KillCount с таким TargetId. |
| NotifyCollect(string itemId) | void | +1 к прогрессу по целям CollectCount с таким TargetId. |
| GetState(string questId) / GetState(QuestConfig quest) | QuestState или null | Состояние квеста. |
| IsActive(QuestConfig quest) | bool | Есть состояние со статусом Active. |
| IsCompleted(QuestConfig quest) | bool | Есть состояние со статусом Completed. |
| GetObjectiveProgress(QuestConfig quest, int index) | int | Текущий прогресс по цели. |

**Списки:** AllQuests (все состояния), ActiveQuests (только Active).

---

## События (UnityEvent)

| Событие | Параметры | Когда |
|---------|-----------|--------|
| On Quest Accepted | string questId | Квест принят. |
| On Objective Progress | string, int, int (questId, index, currentCount) | Изменился прогресс по цели. |
| On Objective Completed | string, int (questId, index) | Одна цель перешла в «выполнена». |
| On Quest Completed | string questId | Все цели выполнены. |
| On Quest Failed | string questId | Вызван FailQuest. |
| On Any Quest Accepted | — | Любой квест принят. |
| On Any Quest Completed | — | Любой квест завершён. |

---

## C#-события

- QuestAccepted (QuestConfig)
- ObjectiveProgress (QuestConfig, int objectiveIndex, int currentCount)
- QuestCompleted (QuestConfig)

Подписка в OnEnable, отписка в OnDisable; при отписке проверять Instance != null.

---

## Кнопки в инспекторе (блок Editor)

- **Accept Quest (Editor Id)** — принять квест по полю Editor Quest Id.
- **Complete Objective (Editor)** — зачесть цель по Editor Quest Id и Editor Objective Index.

---

## Рестарт и повторное прохождение

- Для переигрывания проваленного/завершённого квеста используйте `RestartQuest(...)`.
- Для ручного сброса только одного квеста используйте `ResetQuest(...)`, затем `AcceptQuest(...)`.
- Для «новой игры» используйте `ResetAllQuests()`.
