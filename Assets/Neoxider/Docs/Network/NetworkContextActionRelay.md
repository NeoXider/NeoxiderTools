# NetworkContextActionRelay

**Что это:** `NeoNetworkComponent`, который переводит обычные `UnityEvent` в **контекстные** сетевые действия: вместо жёсткой ссылки на объект из сценового шаблона игрока (которая всегда указывает на один и тот же `GameObject`) relay по `netId` находит **ту же сетевую сущность на каждом клиенте** и применяет действие к дочерней цели (например `Sphere`).

**Где:** `Assets/Neoxider/Scripts/Network/Core/NetworkContextActionRelay.cs`, меню `Neoxider/Network/Network Context Action Relay`.

## Когда использовать

- Триггер / `PhysicsEvents3D` / `PhysicsEvents2D`: нужно включить дочерний объект **у того игрока, чей коллайдер вошёл**, а не у ссылочного `Sphere` из шаблона.
- UI `Button`: `Button.onClick → Trigger()` с источником контекста `LocalPlayer`.
- `InteractiveObject`, `NeoCondition`, любой `UnityEvent` без параметров: `→ Trigger()` или `TriggerLocalPlayer()` в зависимости от сценария.

## Как настроить (типичный pickup)

1. Повесь `NetworkContextActionRelay` на объект с `NetworkIdentity` (часто тот же объект, что и триггер в мире).
2. Включи **`isNetworked`** и при необходимости **`Network Authority Mode`**.
3. **Context Source:** `Event Argument` — если `PhysicsEvents3D.OnTriggerEnter(Collider)` вызывает `Trigger(Collider)` (динамический аргумент события).
4. **Root Mode:** `Network Identity In Parents` — от коллайдера поднимаемся к корневому игроку с `NetworkIdentity`.
5. **Target Mode:** `Child By Name` (или `Child By Path`, если имён несколько) — например имя `Sphere`.
6. **Action:** `Set Active`, **`Bool Value`:** включено.
7. **Scope:** `All Clients`, чтобы все клиенты увидели визуал у правильного игрока.

В `PhysicsEvents3D` в `On Trigger Enter` укажи цель: компонент `NetworkContextActionRelay`, метод **`Trigger`** (один параметр `Collider` передаётся из события).

## Поля

| Поле | Описание |
|------|-----------|
| **Context Source** | Откуда берётся контекст: `Self`, `Local Player`, `Owner`, `Event Argument`, `Explicit Object`. |
| **Root Mode** | Как получить корень: исходный объект, `NetworkIdentity` в родителях, `NeoNetworkPlayer` в родителях. |
| **Explicit Context** | Объект для режима `Explicit Object`. |
| **Target Mode** | Где искать цель: корень, ребёнок по имени, по пути `Transform.Find`, по типу компонента. |
| **Target Name / Path / Component Type** | Параметры поиска цели. |
| **Action** | `Invoke Events Only`, `Set Active`, `Send Message`, `Invoke Component Method`. |
| **Scope** | Как у `NetworkActionRelay`: `AllClients`, `ServerOnly`, `OthersOnly`. |
| **Authority Mode** | `None` / `OwnerOnly` / `ServerOnly` — проверка отправителя `Command`. |

## События (UnityEvent Bridge)

- **On Network Triggered** — сетевой `UnityEvent` без параметров после валидации на сервере.
- **On Context Resolved (GameObject)** — корневой объект контекста (например игрок).
- **On Target Resolved (GameObject)** — найденная цель после резолва.

Для **персональных** действий используй `On Target Resolved` или встроенный **Action**, а не снова перетаскивай `Sphere` из сценового шаблона в инспекторе.

## Ограничения

- Состояние «включено навсегда» после pickup **не синхронизируется для late join** автоматически: для долгоживущего состояния используй `SyncVar` / `NetworkPropertySync` / отдельный сетевой стейт.
- Для `Counter` и любых других компонентов используйте `Invoke Component Method`: укажите тип компонента, имя метода и режим аргумента.

## См. также

- [NetworkActionRelay](NetworkActionRelay.md)
- [NoCode_Network_Spec](NoCode_Network_Spec.md)
- [Multiplayer_Guide](Multiplayer_Guide.md)
