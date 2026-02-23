# State Machine без кода (No-Code): пошаговая настройка

Логика автомата настраивается через `StateMachineData`, предикаты переходов и компонент `StateMachineBehaviour`.
**ScriptableObject не может хранить ссылки на объекты сцены** — поэтому в SO настраивают только логику (состояния, переходы, какое свойство проверять), а **какой GameObject читать** задаётся на компоненте в сцене (раздел **Context for conditions**).

---

## 1. Компонент на сцене

1. Выберите GameObject (например, персонажа).
2. Добавьте компонент: **Component -> Neoxider -> Tools -> State Machine Behaviour**.
3. В `References` укажите `StateMachineData`.

---

## 2. Настройки компонента

- `State Machine Data` — ссылка на конфигурацию (SO).
- **Context for conditions** — массив GameObjects сцены для условий переходов. Элемент 0 = слот Override1, элемент 1 = Override2, … (до 5). Ссылки на сцену задаются **только здесь**, не в SO.
- `Auto Evaluate Transitions` — автоматическая проверка переходов каждый кадр.
- `Show State In Insp` — отображение текущего состояния в инспекторе.
- `Enable Debug Log` — логирование переходов.

### 2.1 События компонента

Секция `Events`:

- `On Initialized`
- `On State Entered`
- `On State Exited`
- `On State Changed` (`from`, `to`)
- `On Transition Evaluated` (`transitionName`, `result`)

### 2.2 Runtime controls

Секция `Controls`:

- `Reload Data`
- `Evaluate Now`
- `Go To Initial State`
- `Change State` (ручной выбор state из списка)

---

## 3. Конфигурация StateMachineData

Выберите ассет `StateMachineData` и настройте:

### 3.1 Состояния (`States`)

- Добавьте элементы списка.
- В каждый элемент назначьте `StateData`.
- В `StateData` настройте `On Enter`, `On Update`, `On Exit` действия.

### 3.2 Начальное состояние (`Initial State`)

- Укажите стартовый `StateData`.

### 3.3 Переходы (`Transitions`)

- Добавьте переходы кнопкой `Add Transition`.
- Для каждого перехода заполните:
  - `Name`
  - `From State`
  - `To State`
  - `Priority`
  - `Is Enabled`
- Нажмите `Edit Conditions` для настройки условий.

---

## 4. Условия переходов

Условия определяют, когда переход из одного состояния в другое разрешён. Без условий переход считается **всегда доступным** (срабатывает первым по приоритету).

### 4.1 Добавление условий (всё в SO, без ссылок на сцену)

1. В ассете `StateMachineData` у нужного перехода нажмите **Edit Conditions**.
2. В окне **Edit Transition** нажмите **Add Condition** → **Neoxider/Condition Entry**.
3. Настройте предикат **только данными из SO**:
   - **Context Slot** — с какого объекта читать: **Owner** = объект с `StateMachineBehaviour`; **Override1** … **Override5** = элементы из списка **Context for conditions** на компоненте в сцене (индексы 0…4). В SO хранится только номер слота, не ссылка на сцену.
   - **Condition Entry** — как в NeoCondition: **Source Object оставьте пустым** (будет использован контекст из слота), выберите **Component** и **Property**, оператор и порог.

Ссылки на объекты сцены задаются **на компоненте** в инспекторе: раздел **Context for conditions** — перетащите туда нужные GameObjects (например, игрок, враг, точка). В условиях в SO выберите **Override1**, **Override2** и т.д., чтобы использовать эти объекты.

Все условия перехода объединяются по логике **И** (AND).

### 4.2 Свойства текущего состояния для NeoCondition

`ConditionEntry` может читать свойства компонента `StateMachineBehaviourBase`:

- `CurrentStateName` (string)
- `PreviousStateName` (string)
- `CurrentStateElapsedTime` (float)
- `StateChangeCount` (int)
- `HasCurrentState` (bool)

Пример (в Condition Entry в SO):
- **Context Slot** = Owner
- Source Object = пусто
- Source Mode = Component, Component = StateMachineBehaviourBase, Property = CurrentStateName
- Compare = Equal, Threshold String = "Run"

### 4.3 Если условия не срабатывают

- **Context Slot** = Owner — читаем с объекта, на котором висит StateMachine. Для другого объекта: в сцене в **Context for conditions** на компоненте добавьте этот объект и выберите в условии **Override1** (или Override2, … по индексу).
- В **Condition Entry** оставьте **Source Object** пустым — контекст возьмётся из слота. Заполните только Component, Property, Compare и порог.
- Убедитесь, что **From State** и **To State** у перехода совпадают с состояниями в списке States.
- Включите **Enable Debug Log** и смотрите консоль; **On Transition Evaluated** показывает результат проверки перехода.

---

## 5. Пример сценария Idle/Run

- `idle` и `run` — два `StateData`.
- Переход `idle -> run`: условие `Speed > 0`.
- Переход `run -> idle`: условие `Speed <= 0`.
- `Auto Evaluate Transitions = ON`.

---

## Сводка

| Что сделать | Где |
|-------------|-----|
| Повесить автомат на объект | `StateMachineBehaviour` |
| Указать конфигурацию | `References -> State Machine Data` |
| Включить авто-переходы | `Settings -> Auto Evaluate Transitions` |
| Настроить состояния | `StateMachineData -> States` |
| Настроить переходы | `StateMachineData -> Transitions` |
| Настроить условия | У перехода нажать **Edit Conditions** → **Add Condition** → Neoxider Condition |
| С какого объекта читать условие | **Context Slot** в условии (Owner / Override1..5); объекты для Override — в **Context for conditions** на компоненте в сцене |
| Подписаться на события | `StateMachineBehaviour -> Events` |
| Ручная отладка | `StateMachineBehaviour -> Controls` (в Play Mode) |
