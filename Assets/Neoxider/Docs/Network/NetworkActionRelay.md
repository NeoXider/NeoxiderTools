# NetworkActionRelay

**Что это:** `NetworkBehaviour` / `MonoBehaviour` компонент для сетевой трансляции любых действий через UnityEvent. Поддерживает множественные каналы с типизированными данными (void/float/string) и выбором scope. Путь: `Scripts/Network/Core/NetworkActionRelay.cs`, пространство имён `Neo.Network`.

**Как использовать:**
1. Добавьте `NetworkActionRelay` на объект с `NetworkIdentity`.
2. Настройте каналы в Inspector (имя, scope, события).
3. Привяжите триггер (кнопка, PhysicsEvent, InteractiveObject) к методу `Trigger()`.

## Ключевые отличия от NetworkEventDispatcher

| Возможность | NetworkEventDispatcher | NetworkActionRelay |
|-------------|----------------------|-------------------|
| Количество каналов | 1 | Любое количество |
| Типизированные данные | ❌ | float, string |
| Выбор scope | ❌ | AllClients, ServerOnly, OthersOnly |
| Rate-limiting | ❌ | ✅ (50ms) |
| Поиск по имени | ❌ | ✅ TriggerByName() |

## Поля

### Channel (каждый канал)

| Поле | Описание |
|------|----------|
| **Channel Name** | Имя канала (для поиска через `TriggerByName()`). |
| **Scope** | Кто получит действие: `AllClients` (все), `ServerOnly` (только сервер), `OthersOnly` (все кроме отправителя). |
| **On Triggered** | UnityEvent без параметров. |
| **On Triggered Float** | UnityEvent с `float` параметром. |
| **On Triggered String** | UnityEvent с `string` параметром. |

## API

| Метод | Описание |
|-------|----------|
| **Trigger()** | Запустить канал 0 (без данных). Удобно для кнопок. |
| **Trigger(int index)** | Запустить канал по индексу. |
| **TriggerFloat(float value)** | Запустить канал 0 с float-значением. |
| **TriggerFloatAt(int index, float value)** | Запустить канал по индексу с float. |
| **TriggerString(string value)** | Запустить канал 0 со строкой. |
| **TriggerStringAt(int index, string value)** | Запустить канал по индексу со строкой. |
| **TriggerByName(string name)** | Найти канал по имени и запустить. |

## Примеры

### Открытие двери (No-Code)
1. На двери: `NetworkActionRelay` с каналом `"open"`, scope = `AllClients`.
2. На рычаге: `InteractiveObject.OnInteract()` → `NetworkActionRelay.Trigger()`.
3. В канале: `onTriggered` → `Animator.SetBool("isOpen", true)`.
Результат: любой игрок дергает рычаг → все видят анимацию двери.

### Подбор предмета (Server Only)
1. На предмете: `NetworkActionRelay` с каналом `"pickup"`, scope = `ServerOnly`.
2. Триггер: `PhysicsEvents3D.OnTriggerEnter` → `NetworkActionRelay.Trigger()`.
3. В канале: `onTriggered` → `InventoryComponent.AddItem()` + `Destroy(gameObject)`.
Результат: сервер добавляет предмет и удаляет объект. Клиенты видят удаление через Mirror.

### Чат (Float / String)
1. `InputField.OnSubmit` → `NetworkActionRelay.TriggerString(text)`.
2. Канал scope = `AllClients`, `onTriggeredString` → `ChatUI.AddMessage()`.

## Без Mirror (Offline)
Если Mirror не установлен, все действия выполняются локально. Компонент ведёт себя как обычный MonoBehaviour.

## См. также
- [NetworkContextActionRelay](NetworkContextActionRelay.md) — контекстные действия на сетевом игроке (триггер/UI без ссылки на template)
- [NetworkOwnerFilter](NetworkOwnerFilter.md) — фильтр по роли перед действием
- [NetworkEventDispatcher](../Tools/Network/NetworkEventDispatcher.md) — legacy версия (один канал)
- [NoCode Network Spec](NoCode_Network_Spec.md) — стандарты
## Authority and scope notes

`Authority Mode` controls who may trigger relay channels over the network:

| Mode | Behavior |
|------|----------|
| `None` | Default NoCode mode. Works on non-owned scene objects. |
| `OwnerOnly` | Only the owning client can send a remote command; host/server is allowed. |
| `ServerOnly` | Remote clients cannot trigger the relay; server/host can. |

Mirror commands still use `requiresAuthority = false`; the relay validates `sender` manually so scene objects do not need ownership setup.

Scope behavior:
- `AllClients`: sends the channel event to every client; dedicated server also invokes locally.
- `ServerOnly`: invokes only on the server/host and does not RPC.
- `OthersOnly`: sends `TargetRpc` to every client except the sender; host-local is excluded when the host triggered it.
