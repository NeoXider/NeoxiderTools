# StateData

**Назначение:** ScriptableObject-состояние для No-Code машины состояний. Содержит имя, списки действий на вход, обновление и выход. Реализует `IState` — можно использовать напрямую в `StateMachine`. Настраивается полностью в Inspector без кода.

**Создать:** Create → Neoxider → State Machine → State Data.

---

## Поля (Inspector)

| Поле | Описание |
|------|----------|
| **State Name** | Имя состояния для идентификации и отображения. По умолчанию `"New State"`. |
| **On Enter Actions** | Список `StateAction` — действия, выполняемые один раз при входе в состояние. |
| **On Update Actions** | Список `StateAction` — действия, выполняемые каждый кадр, пока состояние активно. |
| **On Exit Actions** | Список `StateAction` — действия, выполняемые один раз при выходе из состояния. |

---

## API

| Метод / Свойство | Описание |
|------------------|----------|
| `string StateName { get; set; }` | Имя состояния. |
| `List<StateAction> OnEnterActions { get; }` | Действия при входе. |
| `List<StateAction> OnUpdateActions { get; }` | Действия при обновлении. |
| `List<StateAction> OnExitActions { get; }` | Действия при выходе. |
| `void OnEnter()` | Вызывается машиной состояний при входе. Выполняет все `OnEnterActions`. |
| `void OnUpdate()` | Вызывается каждый кадр. Выполняет все `OnUpdateActions`. |
| `void OnExit()` | Вызывается при выходе. Выполняет все `OnExitActions`. |

---

## Примеры

### No-Code (Inspector)
1. **Create → Neoxider → State Machine → State Data** — создать ассет `IdleState`.
2. Задать **State Name** = `"Idle"`.
3. В **On Enter Actions** добавить `StateAction` (например, `LogAction` с текстом "Entered Idle").
4. В **On Update Actions** добавить действия для каждого кадра.
5. Назначить этот ассет в `StateMachineData` (в массив **States**).

### Код
```csharp
// StateData — это ScriptableObject, его создают через AssetDatabase или меню
// Можно использовать напрямую в StateMachine:
var sm = new StateMachine<IState>();
sm.ChangeState(idleStateData); // StateData реализует IState
```

---

## См. также
- [StateMachineData](StateMachineData.md) — конфигурация машины (набор состояний + переходы)
- [StateAction](../StateAction.md) — базовый класс действий
- [StateMachine](../StateMachine.md) — ядро машины состояний
- ← [StateMachine](../README.md)
