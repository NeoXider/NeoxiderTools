# InventoryDropper

`InventoryDropper` — отдельный подключаемый модуль дропа для `InventoryComponent`.

## Что делает

- Удаляет предмет из инвентаря (`DropById`, `DropData`, `DropSelected`).
- Поддерживает дроп по горячей клавише (по умолчанию `G`), с master-переключателем `CanDrop`.
- Спавнит префаб предмета в мире (`WorldDropPrefab` из `InventoryItemData` или fallback префаб).
- Опционально добавляет физику:
  - `Rigidbody` / `Rigidbody2D`,
  - коллайдер при отсутствии,
  - импульс броска.
- Опционально конфигурирует `PickableItem` у заспавненного объекта.

## Подключение

1. Добавьте `InventoryDropper` на объект (обычно рядом с `InventoryComponent`).
2. В `InventoryComponent` назначьте поле `Dropper`.
3. Настройте физику/коллайдеры и префабы.
4. Вызывайте `InventoryComponent.DropById(...)` или `InventoryDropper.DropById(...)`.

## API: все варианты дропа из кода

| Метод | Описание |
|-------|----------|
| `DropById(int itemId, int amount = 1)` | Дроп по id. |
| `DropByIdOne(int itemId)` | Дроп 1 шт. по id. |
| `DropFirst(int amount = 1)` | Дроп **первого** предмета в инвентаре (по порядку снимка). |
| `DropLast(int amount = 1)` | Дроп **последнего** предмета в инвентаре (по порядку снимка). |
| `DropSelected(int amount = 1)` | Дроп выбранного предмета (при включённом «Drop Next When Empty» при пустом выбранном — дроп первого доступного). |
| `DropConfiguredById()` | Дроп по настройкам инспектора (id и amount; id = -1 = последний). |
| `DropData(InventoryItemData itemData, int amount = 1)` | Дроп по данным предмета (через itemId из data). |
| `SetDropEnabled(bool)` | Включить/выключить дроп в рантайме. |
| `SetDropItemId(int)` | Задать id для режима по кнопке и для DropConfiguredById (-1 = последний предмет). |

## Свойство AllowDropInput и интеграция с InventoryHand

- **AllowDropInput** (get/set) — разрешать ли дроп по клавише. Если Dropper назначен в **InventoryHand** (поле Dropper), Hand при включении выставляет `AllowDropInput = false`, чтобы по клавише G дроп обрабатывала только рука (Hand вызывает DropEquipped). При отключении Hand значение восстанавливается.
- Без Hand можно оставить `Allow Drop Input = true` — тогда по G дропается выбранный предмет (SelectedItemId).

## Настройки ввода (по умолчанию)

- `Allow Drop Input` = `true`
- `Can Drop` = `true`
- `Drop Key` = `G`
- `Drop Selected On Key` = `true` — по нажатию ключа дропается выбранный предмет.
- `Drop Item Id On Key` — id предмета, если не дропаем выбранный. **-1** = дропать **последний** предмет в инвентаре (по порядку снимка).
- **Drop Next When Empty** = `true` — если текущий предмет (выбранный или заданный id) закончился (count 0), дропать следующий доступный предмет из инвентаря. Удобно для непрерывного дропа: один тип закончился — сразу дропается следующий.
- `Drop Amount On Key` = `1`

## Универсальное поведение

- **Выбранный предмет закончился** → при включённом «Drop Next When Empty» дропается первый предмет с count > 0 из снимка инвентаря.
- **Id = -1** (в настройке или через `SetDropItemId(-1)`) → дропается последний предмет в снимке (последний с count > 0).
- `DropSelected` и `DropConfiguredById()` используют ту же логику: при пустом целевом предмете и включённом «Drop Next When Empty» подставляется следующий доступный предмет.
