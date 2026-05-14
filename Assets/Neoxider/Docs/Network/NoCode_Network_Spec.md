# NoCode Multiplayer Guidelines (Spec)

Данный документ описывает фундаментальные правила и стандарты для сетевых компонентов NeoxiderTools, чтобы обеспечить автоматическую синхронизацию в стиле "NoCode".

## Правило 1: Булевый переключатель "isNetworked"
Все интерактивные компоненты (Condition, Counter, Selector, PhysicsEvents и т.д.) по умолчанию являются **полностью синхронными и локальными** (`isNetworked = false` или отключенная галочка). 
Таким образом, если разработчик хочет использовать компонент без мультиплеера - он просто добавляет его и все работает без лишних предупреждений (OnValidate учитывает это условие).

Если разработчик ставит галочку **`isNetworked = true`**, компонент начинает автоматически реплицировать свое состояние и события через сервер. Не нужно использовать перечисления (Enums) вроде `ExecutionMode` — должен быть исключительно булевый переключатель.

## Правило 2: Автоматическое выполнение UnityEvents через RPC
Любые публичные события компонента (`UnityEvent`) при активном "isNetworked" должны **вызываться на всех клиентах одновременно** (через Rpc / ClientRpc), а не только у инициализатора события:
- Сервер получает сигнал о действии.
- Сервер рассылает ClientRpc.
- Каждый клиент на своей стороне вызывает `UnityEvent.Invoke()`.

## Правило 3: Принцип "Единого сервера" для создания объектов
Компоненты генерирующие сущности (такие как `Spawner`) при включенном `isNetworked` должны обладать **защитой от дублирования**. Спавн и генерация контента доверяется **исключительно Серверу**, а клиенты делегируют право спавна. 

## Правило 4: Наследование и препроцессор
Любой сетевой скрипт внедряет функционал мультиплеера через директиву `#if MIRROR`.
Если препроцессор Mirror найден:
- Компонент наследуется от `NetworkBehaviour` (вместо `MonoBehaviour`).
- Реализуются Cmd/Rpc функции.
Если препроцессор не найден, компонент остается обычным `MonoBehaviour`.

## Правило 5: Организация структуры и тестов
- При изменении публичных переменных (атрибутов), необходимо убедиться, что они корректно отражаются в **кастомных редакторах (Custom Editor)**.
- Все Unit и PlayMode **сетевые тесты выделяются в отдельную директорию** (`Network`), а остальные делятся в соответствии со своим родительским модулем (Tools, Core, Rpg и т.д.).

## Правило 6: Кастомный инспектор (Custom Editor) для NetworkBehaviour
Mirror имеет свой внутренний инспектор `NetworkBehaviourInspector`. Чтобы его действие не перекрывало красивый интерфейс фоллбэк-инспектора NeoxiderTools, необходимо прописывать явные переопределения.
Когда компонент начинает наследоваться от `NetworkBehaviour` (при `#if MIRROR`), обязательно укажите его точный тип в файле `NeoCustomEditor.cs`:
```csharp
#if MIRROR
[CustomEditor(typeof(ВашСетевойКомпонент), true)]
[CanEditMultipleObjects]
public class ВашСетевойКомпонентNeoEditor : NeoCustomEditor { }
#endif
```

## Правило 7: Разделение глобального и локального (Персонального) стейта
Избегайте создания универсальных `NetworkSingleton` компонентов для ресурсов, если они предназначены для индивидуального использования каждым игроком.
- **Глобальные ресурсы (Общая Казна) / Глобальные переменные:** Используйте `Money` (который является `NetworkSingleton`). Если `isNetworked = true`, этот синглтон синхронизирует значение для всех как единый пул ресурсов.
- **Индивидуальные ресурсы (Личный кошелек) / Личные переменные:** Используйте `Counter` с `isNetworked = true` и вешайте его **непосредственно на сценовый шаблон игрока (`Scene Player Template`)**. Если вы используете обычный Mirror workflow без сценовых NoCode-ссылок, тот же принцип применяется к `Player Prefab`. Так как у каждого клиента спавнится свой уникальный клон игрока с `NetworkIdentity`, то и свой независимый сетевой `Counter` у каждого будет свой.

Для модификации этих значений из внешних триггеров без прямого указания ссылки (чтобы система сама нашла нужный кошелек) используйте компонент-обертку **`ModifyCounterByKey`**. Он умеет искать и `Money` (глобальную казну), и `Counter` по их уникальному строковому параметру `SaveKey`.

Для действий над **дочерними объектами конкретного игрока** (оружие, `Sphere`, визуал), когда событие приходит от коллайдера/UI и нельзя хранить прямую ссылку на объект из сценового `Scene Player Template`, используйте **`NetworkContextActionRelay`** (см. `NetworkContextActionRelay.md`): по `netId` игрока на каждом клиенте находится его runtime-копия и применяется действие к найденной цели.

## Правило 8: Серверная валидация (Server-Side Validation)
Каждый `[Command]` обязан содержать минимальную серверную защиту:
- **Rate-limiting** — отклонять команды, приходящие чаще чем `CmdRateLimit` (50ms по умолчанию).
- **Логическая валидация** — например, `CmdSpend` проверяет `CanSpend(amount)` на сервере, а не доверяет клиенту.
- **Параметр sender** — каждый Cmd получает `NetworkConnectionToClient sender = null` для возможной идентификации отправителя.

```csharp
[Command(requiresAuthority = false)]
private void CmdSetValue(float newValue, NetworkConnectionToClient sender = null)
{
    if (Time.time - _lastCmdTime < CmdRateLimit) return;
    _lastCmdTime = Time.time;
    // ... apply
}
```

### Authority mode for NoCode scene objects
NoCode multiplayer components should use `NetworkAuthorityMode` instead of raw Mirror ownership checks:

| Mode | Behavior |
|------|----------|
| `None` | Default. Any client/server trigger is accepted; works on non-owned scene objects. |
| `OwnerOnly` | Remote client commands are accepted only when `sender == NetworkIdentity.connectionToClient`. Host/server is allowed. |
| `ServerOnly` | Only server/host-originated actions are accepted. Remote client commands are rejected. |

Commands should remain `[Command(requiresAuthority = false)]`; validation is done manually through `NeoNetworkState.IsAuthorized(gameObject, sender, authorityMode)`.

## Правило 9: Late-Join синхронизация
Компоненты с `isNetworked = true` обязаны использовать `[SyncVar]` для хранения авторитетного значения на сервере. При подключении нового клиента значение автоматически доставляется через Mirror, а `OnStartClient()` применяет его в локальное состояние:

```csharp
[SyncVar] private float _syncValue;

public override void OnStartClient()
{
    base.OnStartClient();
    if (isNetworked && !isServer) ApplyValueLocally(_syncValue);
}
```

### ReactiveProperty late-join
Reactive variables work with the same rule when the authoritative value is stored in a `[SyncVar]`.
The `ReactiveProperty*` object itself is not a Mirror SyncVar. Keep a primitive SyncVar (`float`, `int`, `bool`) and apply it into the reactive variable:

```csharp
[SyncVar] private float _syncValue;
public ReactivePropertyFloat Value = new();

private void ApplyValueLocally(float value)
{
    NetworkReactivePropertyBridge.SetFromNetwork(Value, value);
}

public override void OnStartClient()
{
    base.OnStartClient();
    if (isNetworked && !isServer) ApplyValueLocally(_syncValue);
}
```

In the editor, replicated UnityEvents and replicated reactive values are marked when `isNetworked` is enabled. The marker means the field is driven by Cmd/Rpc or SyncVar late-join logic, not just a local UnityEvent.

## Правило 10: Универсальные NoCode сетевые компоненты
Для быстрого добавления мультиплеера к **любой** механике без кода используйте:

| Компонент | Назначение | Меню |
|-----------|-----------|------|
| **NetworkPropertySync** | Автосинхронизация любого поля/свойства через Reflection (Float/Int/Bool/String/Vector3) | `Neoxider/Network/Network Property Sync` |
| **NetworkActionRelay** | Многоканальный broadcast UnityEvent (void/float/string) с выбором scope (AllClients, ServerOnly, OthersOnly) | `Neoxider/Network/Network Action Relay` |
| **NetworkContextActionRelay** | Контекстные сетевые действия: `Trigger()` / `Trigger(Collider)` + поиск цели внутри сетевого игрока по имени/пути/компоненту (без ссылки на template) | `Neoxider/Network/Network Context Action Relay` |
| **NetworkOwnerFilter** | Фильтр-проверка роли (LocalPlayer, Server, Everyone) перед вызовом действия | `Neoxider/Network/Network Owner Filter` |
| **NeoNetworkDiscovery** | LAN-обнаружение серверов (обёртка Mirror NetworkDiscovery) | `Neoxider/Network/Neo Network Discovery` |
| **NeoLobbyManager** | Лобби с ready-проверкой (обёртка Mirror NetworkRoomManager) | `Neoxider/Network/Neo Lobby Manager` |
| **NeoLobbyPlayer** | Игрок в лобби с кнопкой готовности | `Neoxider/Network/Neo Lobby Player` |
| **NetworkEventDispatcher** | Простой broadcast одного UnityEvent (legacy, для совместимости) | `Neoxider/Tools/Network/Network Event Dispatcher` |

## Правило 11: Наследование от NeoNetworkComponent
Все новые сетевые NoCode компоненты (не-синглтоны) должны наследоваться от `NeoNetworkComponent`, а не напрямую от `NetworkBehaviour`. Базовый класс предоставляет:
- `isNetworked` — булевый переключатель (Правило 1)
- `RateLimitCheck()` — защита от спама (Правило 8)
- `ApplyNetworkState()` — шаблон для late-join (Правило 9)
- `ShouldDispatchToServer()` / `ShouldBroadcastRpc()` — хелперы для dispatch-паттерна

Для синглтон-менеджеров используйте `NetworkSingleton<T>` с аналогичными утилитами.


## Правило 12: Сценовый игрок как NoCode-шаблон

NoCode-проекты часто настраивают игрока прямо в сцене: камеры, UI, UnityEvents, биндинги, ссылки на менеджеры и дочерние объекты уже привязаны в Inspector. Поэтому prefab-only flow Mirror не всегда удобен.

Для такого сценария используйте `NeoNetworkManager`:

- включите `Use Scene Player Template`;
- назначьте сценовый объект игрока в `Scene Player Template`;
- оставьте `Disable Scene Player Template` включённым;
- на объекте игрока должен быть `NetworkIdentity`.

Поведение:

1. Сценовый объект является только шаблоном.
2. При старте сети шаблон выключается.
3. Сервер создаёт активную копию для каждого подключения и вызывает `NetworkServer.AddPlayerForConnection`.
4. Клиенты создают свои копии через Mirror spawn handler с тем же стабильным runtime id.

Требование: у сервера и всех клиентов должна быть одинаковая сцена с тем же назначенным `Scene Player Template`. Если игрок не зависит от сценовых NoCode-ссылок, используйте обычный Mirror `Player Prefab`.

## Implementation note: shared inheritance

Do not duplicate class-level `#if MIRROR` inheritance blocks in every NoCode component.
Non-singleton networked NoCode components should inherit from `NeoNetworkComponent`.
Singleton managers should inherit from `NetworkSingleton<T>`.
Component scripts may still wrap Mirror-only fields and Cmd/Rpc methods with `#if MIRROR`.
