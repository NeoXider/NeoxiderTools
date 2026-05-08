# NetworkPropertySync

**Что это:** `NetworkBehaviour` / `MonoBehaviour` компонент для автоматической синхронизации любого поля или свойства любого компонента через Reflection. Поддерживает типы Float, Int, Bool, String, Vector3. Путь: `Scripts/Network/Core/NetworkPropertySync.cs`, пространство имён `Neo.Network`.

**Как использовать:**
1. Добавьте `NetworkPropertySync` на объект с `NetworkIdentity`.
2. В поле **Target Component** перетащите компонент, чьё поле нужно синхронизировать.
3. В поле **Field Name** введите имя поля или свойства (регистрозависимое).
4. Выберите **Value Type** (Float / Int / Bool / String / Vector3).
5. Выберите **Direction** (ServerToClients или OwnerToServer).
6. Настройте **Sync Interval** (по умолчанию 0.1s) и **Threshold**.

---

## Поля

| Поле | Тип | Описание |
|------|-----|----------|
| `_targetComponent` | `Component` | Компонент, чьё поле будет синхронизироваться |
| `_fieldName` | `string` | Имя поля или свойства (public или private) |
| `_valueType` | `SyncValueType` | Тип данных: `Float`, `Int`, `Bool`, `String`, `Vector3` |
| `_direction` | `SyncPropertyDirection` | `ServerToClients` — сервер пишет, клиенты читают. `OwnerToServer` — владелец пишет, сервер раздаёт |
| `_syncInterval` | `float` | Интервал проверки изменений (секунды, по умолчанию 0.1) |
| `_threshold` | `float` | Минимальное изменение для синхронизации (для Float/Int/Vector3, по умолчанию 0.01) |

## События

| Событие | Тип | Описание |
|---------|-----|----------|
| `onValueChanged` | `UnityEvent` | Вызывается когда синхронизированное значение изменилось на этом клиенте |

## Late-Join

Компонент использует `[SyncVar]` для каждого типа данных. Новые клиенты автоматически получают актуальное значение через `OnStartClient`.

## Примеры

### Синхронизация HP
```
GameObject: Enemy
├── HealthComponent (_currentHp : float)
├── NetworkPropertySync
│   ├── Target: HealthComponent
│   ├── Field: _currentHp
│   ├── Type: Float
│   ├── Direction: ServerToClients
│   └── Interval: 0.05s
└── NetworkIdentity
```

### Синхронизация StateMachine
```
GameObject: GameState
├── StateMachine (currentStateIndex : int)
├── NetworkPropertySync
│   ├── Target: StateMachine
│   ├── Field: currentStateIndex
│   ├── Type: Int
│   └── Direction: ServerToClients
└── NetworkIdentity
```

### Синхронизация имени игрока
```
GameObject: Player
├── PlayerProfile (DisplayName : string)
├── NetworkPropertySync
│   ├── Target: PlayerProfile
│   ├── Field: DisplayName
│   ├── Type: String
│   └── Direction: OwnerToServer
└── NetworkIdentity
```

## См. также
- [NetworkActionRelay](NetworkActionRelay.md) — синхронизация действий (событий)
- [NeoNetworkComponent](NeoNetworkComponent.md) — базовый класс
- [NoCode Network Spec](NoCode_Network_Spec.md) — Правило 10
