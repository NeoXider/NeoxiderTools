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
- **Индивидуальные ресурсы (Личный кошелек) / Личные переменные:** Используйте `Counter` с `isNetworked = true` и вешайте его **непосредственно на префаб Игрока (Player Prefab)**. Так как у каждого клиента спавнится свой уникальный клон игрока с `NetworkIdentity`, то и свой независимый сетевой `Counter` у каждого будет свой.

Для модификации этих значений из внешних триггеров без прямого указания ссылки (чтобы система сама нашла нужный кошелек) используйте компонент-обертку **`ModifyCounterByKey`**. Он умеет искать и `Money` (глобальную казну), и `Counter` по их уникальному строковому параметру `SaveKey`.

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

## Правило 10: Универсальные NoCode сетевые компоненты
Для быстрого добавления мультиплеера к **любой** механике без кода используйте:

| Компонент | Назначение | Меню |
|-----------|-----------|------|
| **NetworkPropertySync** | Автосинхронизация любого поля/свойства через Reflection (Float/Int/Bool/String/Vector3) | `Neoxider/Network/Network Property Sync` |
| **NetworkActionRelay** | Многоканальный broadcast UnityEvent (void/float/string) с выбором scope (AllClients, ServerOnly, OthersOnly) | `Neoxider/Network/Network Action Relay` |
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


