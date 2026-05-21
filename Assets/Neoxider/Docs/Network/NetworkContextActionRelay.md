# NetworkContextActionRelay

**Что это:** `NeoNetworkComponent`, который переводит обычные `UnityEvent` в **контекстные** сетевые действия: вместо жёсткой ссылки на объект из сценового шаблона игрока (которая всегда указывает на один и тот же `GameObject`) relay по `netId` находит **ту же сетевую сущность на каждом клиенте** и применяет действие к дочерней цели (например `Sphere`).

**Где:** `Assets/Neoxider/Scripts/Network/Core/NetworkContextActionRelay.cs`, меню `Neoxider/Network/Network Context Action Relay`.

**Inspector:** кастомный (`Assets/Neoxider/Editor/Network/NetworkContextActionRelayEditor.cs`) — наследует `CustomEditorBase` как у `NeoCondition`, рисует секции Context / Target / Action / Networking / Diagnostics / Editor Helpers / Events, выбор компонента и метода через reflection-дропдауны (общие хелперы `ComponentBindingInspectorShared` — те же что у NeoCondition).

## Когда использовать

- **Pickup** на сцене: триггер исчезает у всех при касании (`ContextSource = Self`, `Action = SetActive(false)`).
- **Buff на игрока**: триггер включает дочерний объект **у того игрока, чей коллайдер вошёл** (`ContextSource = Event Argument`, `Target Mode = Child By Name`).
- Несколько relay'ев на одном триггере: на одном `NetworkIdentity` можно повесить два и более `NetworkContextActionRelay` — pickup-self + бафф-игрока. Сообщения различаются по `relayComponentIndex` (Mirror `NetworkBehaviour.ComponentIndex`).
- UI `Button`: `Button.onClick → Trigger()` с источником контекста `LocalPlayer`.
- `InteractiveObject`, `NeoCondition`, любой `UnityEvent` без параметров: `→ Trigger()` или `TriggerLocalPlayer()` в зависимости от сценария.

## Pickup-паттерн (общий объект исчезает у всех)

1. Поставь `NetworkContextActionRelay` на сцен-объект с `NetworkIdentity` (трюк-куб).
2. **Context Source:** `Self`, **Root Mode:** `Source Object`, **Target Mode:** `Root`.
3. **Action:** `Set Active`, **Bool Value:** `false`.
4. **Scope:** `All Clients`.
5. Wire `PhysicsEvents3D.onTriggerEnter → Trigger(Collider)`.

## Buff-паттерн (Sphere у вошедшего игрока)

1. Второй `NetworkContextActionRelay` (на том же объекте, если нужно вместе с pickup).
2. **Context Source:** `Event Argument`, **Root Mode:** `Network Identity In Parents`.
3. **Target Mode:** `Child By Name`, **Target Name:** `Sphere`.
4. **Action:** `Set Active`, **Bool Value:** `true`.
5. **Scope:** `All Clients`, **Trigger Only For Local Context:** `true` (рекомендуется — фильтр по входящему коллайдеру, режет дубликаты от чужой физики).

> **⚠ Внимание:** если цель — дочерний объект под `First Person Camera` (или другой объект из `NeoNetworkPlayer._localOnlyObjects`), он будет невидим у удалённых игроков (`activeInHierarchy = false` из-за выключенного родителя). Переноси цель в часть, остающуюся активной у remote-игрока.

## Поля

| Поле | Описание |
|------|-----------|
| **Is Networked** | Включает сетевую диспетчеризацию. При `false` действие применяется только локально. |
| **Context Source** | Откуда берётся контекст: `Self`, `Local Player`, `Owner`, `Event Argument`, `Explicit Object`. |
| **Root Mode** | Как получить корень: исходный объект, `NetworkIdentity` в родителях, `NeoNetworkPlayer` в родителях. |
| **Explicit Context** | Объект для режима `Explicit Object`. |
| **Target Mode** | Где искать цель: корень, ребёнок по имени, по пути `Transform.Find`, по типу компонента. |
| **Target Name / Path / Component Type** | Параметры поиска цели. Для `Child By Component` — full type name; в кастомном инспекторе доступен dropdown по Preview Target. |
| **Include Inactive** | Включать неактивные дочерние при поиске. |
| **Action** | `Invoke Events Only`, `Set Active`, `Send Message`, `Invoke Component Method`. |
| **Bool / Float / String Value** | Аргументы действия. В инспекторе показывается только то поле, что относится к выбранному `Action` / `Method Argument Mode`. |
| **Send Message Name** | Имя метода для `Send Message`. |
| **Method Component Type / Method Name / Method Argument Mode** | Для `Invoke Component Method` — компонент, метод и тип аргумента. В инспекторе — dropdown через reflection. |
| **Scope** | `AllClients` / `ServerOnly` / `OthersOnly`. |
| **Authority Mode** | `None` / `OwnerOnly` / `ServerOnly` — проверка отправителя `Command`. |
| **Trigger Only For Local Context** | (по умолчанию `true`) — относится к input-стороне: на каждом клиенте `OnTriggerEnter` срабатывает для реплицированных коллайдеров; фильтр пропускает дальше только клиента-владельца входящего коллайдера, чтобы сервер не получал N дубликатов. |
| **Verbose Logging** | Трасса для отладки: `Trigger → Send → OnServer → Broadcast → OnClient → Apply`. |
| **Editor Preview Target** | Editor-only ссылка для dropdown'ов; рантаймом не используется. |

## События (UnityEvent Bridge)

- **On Network Triggered** — сетевой `UnityEvent` без параметров после валидации на сервере.
- **On Context Resolved (GameObject)** — корневой объект контекста (например игрок).
- **On Target Resolved (GameObject)** — найденная цель после резолва.

Для **персональных** действий используй `On Target Resolved` или встроенный **Action**, а не снова перетаскивай `Sphere` из сценового шаблона в инспекторе.

## Как работает сеть

1. Клиент вызывает `Trigger(...)` → `relayComponentIndex` берётся из `NetworkBehaviour.ComponentIndex` (одинаковый на всех пирах из-за детерминированного порядка).
2. Чистый клиент шлёт `NetworkContextActionMessage(relayNetId, relayComponentIndex, contextNetId)` на сервер.
3. Сервер применяет действие локально (host) + рассылает сообщение всем клиентам (кроме host-local, чтобы не было double-apply).
4. Каждый клиент по `(relayNetId, componentIndex)` находит **именно** этот relay через `NetworkIdentity.NetworkBehaviours[componentIndex]` и применяет действие к своей копии `contextNetId`.

Замена `[ClientRpc]` на прямой `NetworkConnection.Send`: не зависит от observer-листа `NetworkIdentity` (AOI / interest management), поэтому действие гарантированно доходит до каждого подключенного клиента.

## Ограничения

- Состояние «включено навсегда» после pickup **не синхронизируется для late join** автоматически: для долгоживущего состояния используй `SyncVar` / `NetworkPropertySync` / отдельный сетевой стейт.
- Цель действия должна быть в части иерархии, активной у remote-игроков (не под `_localOnlyObjects`).

## См. также

- [NetworkActionRelay](NetworkActionRelay.md)
- [NoCode_Network_Spec](NoCode_Network_Spec.md)
- [Multiplayer_Guide](Multiplayer_Guide.md)
