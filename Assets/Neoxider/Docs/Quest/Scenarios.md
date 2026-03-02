# Сценарии в инспекторе

**Что это:** пошаговые сценарии настройки квестов в инспекторе: QuestManager, Quest Accept Trigger, Quest Objective Notifier, NeoCondition. Какие объекты создавать, куда вешать компоненты, что подключать в UnityEvent.

**Как использовать:** убедиться, что в сцене есть QuestManager и Known Quests; при Start Conditions назначить Condition Context. Выбрать сценарий ниже и выполнить шаги; проверять кнопками в инспекторе или в игре.

--- Какие объекты создавать, куда вешать компоненты, что подключать в UnityEvent.

**Как с этим работать:**
1. Убедиться, что в сцене есть QuestManager, в Known Quests добавлены нужные QuestConfig, в Condition Context назначен объект (если в квесте есть Start Conditions).
2. Выбрать сценарий ниже и выполнить шаги по порядку.
3. Проверять через кнопки в инспекторе ([Accept Quest], [Notify Complete], кнопки в блоке Editor у QuestManager) или в игре.

---

## Сценарий 1: Принять квест по кнопке

**Цель:** по нажатию UI-кнопки принять выбранный квест.

1. Выбрать объект с компонентом Button (или панель, на которой висит кнопка).
2. **Add Component → Neoxider → Quest → Quest Accept Trigger**.
3. В поле **Quest** перетащить QuestConfig (тот же, что в Known Quests).
4. У Button в **On Click ()** добавить вызов: Object = этот же объект, Function = **QuestAcceptTrigger → AcceptQuest()** (без параметров).

Проверка: нажать **[Accept Quest]** в инспекторе у Quest Accept Trigger или нажать кнопку в игре. Если квест не принимается: конфиг в Known Quests, Id не пустой, квест не принят ранее, все Start Conditions при Evaluate(Condition Context) дают true.

---

## Сценарий 2: Зачесть цель при выполнении условия (NeoCondition)

**Цель:** когда условие (здоровье, счётчик, флаг) становится true — засчитать одну цель квеста.

1. Выбрать или создать GameObject.
2. **Add Component → Neoxider → Condition → NeoCondition**. Настроить **Conditions** (объект, компонент, свойство, оператор, порог) по [NeoCondition](../Condition/NeoCondition.md).
3. На тот же объект **Add Component → Neoxider → Quest → Quest Objective Notifier**.
4. В Notifier: **Quest** = ваш QuestConfig, **Objective Index** = индекс цели (0, 1, 2, … — порядок в Objectives конфига).
5. В NeoCondition в **Events → On True** добавить вызов: Object = этот объект, Function = **QuestObjectiveNotifier → NotifyComplete()**.

При первом срабатывании NeoCondition.On True менеджер засчитает указанную цель. Проверка: кнопка **[Notify Complete]** у Notifier.

---

## Сценарий 3: Показать UI при завершении квеста

**Цель:** при завершении любого квеста (все цели выполнены) показать панель или воспроизвести анимацию.

1. Выбрать QuestManager в сцене.
2. В инспекторе найти **On Any Quest Completed** (UnityEvent без аргументов).
3. Добавить вызов: перетащить UI-объект (панель, попап), выбрать функцию показа/анимации. Параметры не передаются.

Если нужна разная реакция по questId: использовать **On Quest Completed** (UnityEvent&lt;string&gt;) и подключить метод своего скрипта с одним параметром string (questId).

---

## Сценарий 4: Реакция на принятие квеста (звук, подсказка)

1. У QuestManager найти **On Any Quest Accepted** (без аргументов).
2. Добавить вызов: звук, анимация, показ подсказки. Срабатывает при любом принятии квеста.

---

## Сценарий 5: Обновить UI при выполнении одной цели

**Цель:** когда выполнена одна цель (например «собрать ключ») — сразу поставить галочку в списке целей.

1. У QuestManager найти **On Objective Completed** (UnityEvent&lt;string, int&gt;: questId, objectiveIndex).
2. Подключить метод своего скрипта с двумя параметрами (string, int). В методе по questId и objectiveIndex обновить нужный элемент UI.

---

## Сценарий 6: Реакция на провал квеста

1. У QuestManager найти **On Quest Failed** (UnityEvent&lt;string&gt;).
2. Подключить вызов: сообщение «Квест провален», скрытие квеста в журнале, звук и т.д. Событие вызывается только при явном вызове FailQuest (из кода или другого компонента).

---

## Кнопки в инспекторе

- **QuestManager**, блок Editor: **Editor Quest Id**, **Editor Objective Index**, кнопки **Accept Quest (Editor Id)** и **Complete Objective (Editor)** — тест приёма и зачёта цели без игровых действий.
- **Quest Accept Trigger:** [Accept Quest].
- **Quest Objective Notifier:** [Notify Complete].

---

## Ограничения

- Разная реакция на завершение разных квестов (по questId) — нужен метод в своём скрипте с параметром string, подключённый к On Quest Completed.
- Проверка «доступен ли квест» для отображения в UI — в модуле нет метода «проверить без принятия»; реализовывать в коде (оценка условий или свои флаги).
- Сложная выдача наград или ветвление по диалогу — удобнее в коде по событиям QuestCompleted / ObjectiveProgress.
