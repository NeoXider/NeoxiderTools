# PickableItem

`PickableItem` — компонент подбираемого предмета в мире.

## Что умеет

- Подбор по триггеру 3D (`OnTriggerEnter`) и/или 2D (`OnTriggerEnter2D`).
- Ручной вызов `Collect()` из кнопок, `InteractiveObject`, `PhysicsEvents` через UnityEvent.
- Дополнительные алиасы методов подбора: `Pickup()`, `PickupFromGameObject(...)`, `PickupFromCollider(...)`, `PickupFromCollider2D(...)`.
- Выдача предмета в `InventoryComponent`.
- Фильтр по тегу собирающего объекта.
- Опциональная валидация: у сборщика должен быть `InventoryComponent` (на объекте или в родителях).
- Поведение после подбора: отключить коллайдеры, деактивировать объект, уничтожить объект.
- **Активация в руке**: при применении предмета в руке (`InventoryHand.UseEquippedItem()`) у экземпляра в Hand Anchor вызывается **Activate()**; срабатывает **OnActivate** — подпишите для эффекта (звук, частицы, логика использования).

По умолчанию приоритет проверки такой:
1. наличие `InventoryComponent` у сборщика (основной фильтр),
2. тег (`Required Collector Tag`) как вторичный фильтр, если он задан.

## Поля

| Поле | Описание |
|------|----------|
| `Item Data` | ScriptableObject предмета (`InventoryItemData`). |
| `Item Id` | fallback id, если `Item Data` не задан. |
| `Amount` | Количество предмета. |
| `Target Inventory` | Явная ссылка на инвентарь. |
| `Auto Find Inventory` | Искать `InventoryComponent.FindDefault()` при пустой ссылке. |
| `Collect On Trigger 3D/2D` | Автосбор через триггеры. |
| `Required Collector Tag` | Вторичный фильтр по тегу (пусто = фильтр отключён). |
| `Require Collector Inventory` | Основной фильтр (по умолчанию включен): без `InventoryComponent` у сборщика подбор отклоняется. |
| `Search Collector Inventory In Parents` | Искать инвентарь у родителей объекта-сборщика. |
| `Use Collector Inventory As Target` | Использовать инвентарь сборщика как target (удобно для нескольких игроков). |
| `Collect Only Once` | Предотвращает повторный сбор. |

## События

| Событие | Аргументы | Когда вызывается |
|---------|-----------|------------------|
| **OnActivate** | — | При активации предмета (вызов Activate(), в т.ч. при применении в руке). Подпишите для эффекта использования. |
| `OnCollectStarted` | — | Перед попыткой выдать предмет. |
| `OnCollected` | `(itemId, addedAmount)` | Успешный подбор. |
| `OnCollectFailed` | — | Не удалось подобрать (нет инвентаря, лимиты и т.д.). |
| `OnAfterCollectDespawn` | — | Перед destroy/deactivate после успешного подбора. |

## Методы для UnityEvent

- **Activate()** — вызвать активацию (вызывает OnActivate). InventoryHand вызывает его у экземпляра в руке при UseEquippedItem().

- `Collect()`
- `Pickup()`
- `CollectFromGameObject(GameObject collector)`
- `CollectFromCollider(Collider collider3D)`
- `CollectFromCollider2D(Collider2D collider2D)`
- `PickupFromGameObject(GameObject collector)`
- `PickupFromCollider(Collider collider3D)`
- `PickupFromCollider2D(Collider2D collider2D)`

## Пример связки с InteractiveObject

1. На объект предмета добавьте `PickableItem`.
2. Добавьте `InteractiveObject`.
3. В `InteractiveObject.onClick` назначьте `PickableItem.Collect`.
4. Теперь предмет подбирается по клику (без дополнительного кода).
