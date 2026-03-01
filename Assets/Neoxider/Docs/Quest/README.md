# Quest — модуль квестов

Система квестов с поддержкой **NoCode** (инспектор, UnityEvent, NeoCondition) и **кода** (типобезопасный API по ссылке на QuestConfig). Условия старта и цели «по условию» задаются через [NeoCondition](../Condition/NeoCondition.md) (ConditionEntry). State Machine не используется.

---

## Возможности

- **Один конфиг на квест** — ScriptableObject (QuestConfig): цели, условия старта, описание.
- **Типы целей** — CustomCondition (триггер/NeoCondition), KillCount, CollectCount, ReachPoint, Talk.
- **Условия старта** — список ConditionEntry, проверка по контексту (игрок/мир) в QuestManager.
- **NoCode** — принять квест по кнопке (QuestAcceptTrigger), зачесть цель по условию (QuestObjectiveNotifier + NeoCondition.OnTrue).
- **Код** — перегрузки AcceptQuest(QuestConfig), GetState(QuestConfig), события с QuestConfig.

---

## Быстрый старт

1. **Создайте конфиг квеста:** ПКМ в Project → **Create → Neoxider → Quest → Quest Config**. Заполните Id, Title, Description, добавьте цели (Objectives) и при необходимости условия старта (Start Conditions).
2. **Добавьте QuestManager в сцену:** GameObject → Add Component → **Neoxider → Quest → QuestManager**. В список **Known Quests** перетащите ваш QuestConfig. В **Condition Context** укажите объект для проверки условий (например, игрока); можно добавить на игрока компонент [QuestContext](QuestContext.md) как маркер.
3. **Принять квест (NoCode):** на кнопку или панель добавьте **Quest Accept Trigger**, укажите QuestConfig, в OnClick вызовите **AcceptQuest()**.
4. **Зачесть цель по условию (NoCode):** на объект добавьте **NeoCondition** (нужные условия) и **Quest Objective Notifier** (QuestConfig + индекс цели). В NeoCondition → OnTrue добавьте вызов **NotifyComplete()** у Notifier.

---

## Структура модуля

| Папка/файл | Описание |
|------------|----------|
| `QuestConfig.cs` | ScriptableObject: описание квеста, цели, условия старта. |
| `QuestObjectiveData.cs` | Данные одной цели (тип, targetId, requiredCount, ConditionEntry). |
| `QuestStatus.cs` | enum: NotStarted, Active, Completed, Failed. |
| `QuestState.cs` | Runtime-состояние квеста (прогресс по целям). |
| `QuestManager.cs` | Реестр квестов, AcceptQuest, CompleteObjective, события. |
| `QuestContext.cs` | Маркер объекта-контекста для условий (опционально). |
| `Bridge/QuestObjectiveNotifier.cs` | NoCode: NeoCondition.OnTrue → CompleteObjective. |
| `Bridge/QuestAcceptTrigger.cs` | NoCode: кнопка → AcceptQuest. |

---

## Документация

- [QuestConfig](QuestConfig.md) — поля конфига, типы целей, условия старта.
- [QuestManager](QuestManager.md) — API, события, контекст.
- [QuestState](QuestState.md) — состояние квеста, прогресс целей.
- [NoCode](NoCode.md) — сценарии без кода: кнопка, условие, UI при завершении.
- [Code](Code.md) — использование из C#: перегрузки, события, примеры.
- [Saving](Saving.md) — опциональная интеграция с системой сохранений.

---

## Зависимости

- **Neo.Condition** — ConditionEntry, IConditionEvaluator (проверка условий старта и целей по условию).
- Сохранение прогресса квестов — опционально, см. [Saving](Saving.md).
