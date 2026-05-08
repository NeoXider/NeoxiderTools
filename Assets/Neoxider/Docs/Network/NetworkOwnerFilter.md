# NetworkOwnerFilter

**Что это:** `NetworkBehaviour` / `MonoBehaviour` компонент для фильтрации выполнения действий по сетевой роли (LocalPlayer, Server, Everyone). Путь: `Scripts/Network/Core/NetworkOwnerFilter.cs`, пространство имён `Neo.Network`.

**Как использовать:**
1. Добавьте `NetworkOwnerFilter` на объект.
2. Выберите **Mode** (LocalPlayerOnly, ServerOnly, Everyone).
3. Привяжите триггер к `Filter()`, а действие — к `onAllowed`.

---

## Поля

| Поле | Описание |
|------|----------|
| **Mode** | Режим фильтрации: `LocalPlayerOnly`, `ServerOnly`, `Everyone`. |
| **On Allowed** | UnityEvent — вызывается, если текущая роль прошла фильтр. |
| **On Denied** | UnityEvent — вызывается, если текущая роль НЕ прошла фильтр. |

## Режимы (OwnerFilterMode)

| Режим | Описание |
|-------|----------|
| **LocalPlayerOnly** | Пропускает только если текущий клиент является владельцем (isLocalPlayer / isOwned) объекта. |
| **ServerOnly** | Пропускает только если текущая среда — сервер (или хост). |
| **Everyone** | Пропускает всегда (no-op, для наглядности в цепочке). |

## API

| Метод | Описание |
|-------|----------|
| **Filter()** | Проверить роль и вызвать `onAllowed` или `onDenied`. Привязывайте к триггеру. |
| **IsAllowed()** | Возвращает `true`/`false` без вызова событий. Для использования из кода. |

## Примеры

### Только локальный игрок открывает инвентарь
```
Кнопка OnClick() → NetworkOwnerFilter.Filter()
                    ├── onAllowed → InventoryUI.Show()
                    └── onDenied → (ничего)
```
Mode = `LocalPlayerOnly`. Только владелец префаба видит свой инвентарь.

### Только сервер спавнит врагов
```
Timer.OnInterval() → NetworkOwnerFilter.Filter()
                     ├── onAllowed → Spawner.Spawn()
                     └── onDenied → (ничего)
```
Mode = `ServerOnly`. Враги создаются только на сервере, клиенты получают их через Mirror.

### Цепочка с NetworkActionRelay
```
InteractiveObject.OnInteract()
  → NetworkOwnerFilter.Filter() [LocalPlayerOnly]
      → onAllowed → NetworkActionRelay.Trigger() [AllClients]
          → onTriggered → Door.Open()
```
Фильтр гарантирует, что Command отправит только владелец, а Relay разошлёт всем.

## Без Mirror (Offline)
В соло-режиме `Filter()` всегда вызывает `onAllowed`.

## См. также
- [NetworkActionRelay](NetworkActionRelay.md) — сетевой broadcast действий
- [NoCode Network Spec](NoCode_Network_Spec.md) — стандарты
