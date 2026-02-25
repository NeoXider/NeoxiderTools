# InventoryHand

Компонент **«рука»** (equipped slot): один выбранный предмет из инвентаря отображается в заданной точке сцены (например рука персонажа). Переключение влево/вправо по слотам с ненулевым количеством. Удобная интеграция с **Selector** для No-Code переключения (кнопки, колёсико мыши).

## Назначение

- Выбор одного предмета из инвентаря как «текущий в руке».
- Отображение 3D-модели (префаба) этого предмета в точке `Hand Anchor`.
- Переключение слота: **SelectNext** / **SelectPrevious** (с зацикливанием).
- Синхронизация с **InventoryComponent.SelectedItemId** (для дропа, условий, UI).
- Опциональная связка с **Selector**: переключение через Selector.Next/Previous (например с UI или вводом).

## Поля

| Поле | Описание |
|------|----------|
| **Inventory** | Инвентарь. Если не назначен и включён Auto Find — `InventoryComponent.FindDefault()`. |
| **Hand Anchor** | Transform, в котором появляется экземпляр префаба выбранного предмета (например рука). Если пусто — используется transform компонента. |
| **Selector** | Опционально. Selector в режиме Count: количество слотов = число предметов в инвентаре с count > 0. При изменении выбора в Selector обновляется слот руки и визуал. |
| **Dropper** | Опционально. InventoryDropper для дропа предмета из руки: по клавише (G) и вызов DropEquipped(). У назначенного Dropper ввод по клавише временно отключается, чтобы дроп обрабатывала только рука. |
| **Fallback Hand Prefab** | Префаб по умолчанию, если у предмета в базе нет WorldDropPrefab. |
| **Scale In Hand Mode** | **Fixed** — множитель Hand Scale Fixed. **Relative** (по умолчанию) — дельта 1 + Hand Scale Offset; удобно с HandView на предметах. |
| **Hand Scale Fixed** | Множитель масштаба в руке. Используется при Scale In Hand Mode = Fixed. |
| **Hand Scale Offset** | Дельта масштаба: множитель = 1 + offset. Используется при Scale In Hand Mode = Relative. |
| **Disable Colliders In Hand** | При включении (по умолчанию) у предмета в руке отключаются все Collider и Collider2D (на объекте и детях). Отключите, если коллайдеры нужны в руке. |
| **Sync Selector On Inventory Changed** | При изменении инвентаря обновлять Selector.Count и зажимать текущий индекс. |
| **Allow Empty Slot** | При пустом инвентаре — пустой слот (Count=1). При наличии предметов — разрешить индекс **-1** (ничего не в руке): SetSlotIndex(-1) или Selector.Previous() с 0; у Selector включите **Allow Empty Effective Index**. |
| **Allow Drop Input** | По нажатию клавиши дропа (например G) сбрасывать предмет из руки через Dropper. Имеет смысл при назначенном Dropper. |
| **Drop Key** | Клавиша выброса предмета из руки (по умолчанию G). |
| **Allow Use Input** | По нажатию клавиши применения вызывать UseEquippedItem(). |
| **Use Key** | Клавиша применения предмета в руке (по умолчанию E). |

## Физика и коллайдеры предмета в руке

У экземпляра префаба в Hand Anchor при спавне **отключается физика**; коллайдеры — опционально:
- **Rigidbody** — `isKinematic = true`; **Rigidbody2D** — `simulated = false` (поиск по объекту и детям). Предмет не падает и не толкает.
- **Collider** и **Collider2D** — при включённой настройке **Disable Colliders In Hand** (по умолчанию) у всех коллайдеров на объекте и детях выставляется `enabled = false`, чтобы объект в руке не участвовал в столкновениях. Настройку можно отключить, если коллайдеры в руке нужны.
При сбросе через Dropper создаётся новый экземпляр в мире с включённой физикой и коллайдерами.

## Вьюшка руки (HandView)

На префаб предмета (тот же, что **WorldDropPrefab**) можно повесить компонент **HandView**: смещение позиции, поворота и базовый масштаб в руке. Рука при отображении ищет HandView на экземпляре и применяет эти значения первыми; затем применяется общий масштаб руки (Fixed или Relative). Подробнее: [HandView.md](./HandView.md).

## Масштаб в руке

- Если на предмете есть **HandView**, базовый масштаб берётся из **HandView.Scale In Hand** (иначе 1). Поверх применяется общий масштаб руки.
- **Scale In Hand Mode**: **Relative** (по умолчанию) — множитель 1 + **Hand Scale Offset** (дельта; удобно с HandView). **Fixed** — множитель **Hand Scale Fixed**.
- Итог: `итоговый масштаб = базовый (из HandView или 1) × handScale`.

## События

Оба события передают один параметр **int itemId** (удобно для NoCode-подписок в инспекторе). Данные предмета по itemId: `inventory.GetItemData(itemId)` или `InventoryHand.EquippedItemData`.

| Событие | Аргументы | Когда |
|---------|-----------|--------|
| **OnEquippedChanged** | `(int itemId)` | После смены выбранного слота (в т.ч. через SelectNext/Previous/SetSlotIndex). |
| **OnUseItemRequested** | `(int itemId)` | При вызове UseEquippedItem(). Подпишите для эффекта (здоровье, дверь и т.д.); при расходе — inventory.TryConsume(itemId, 1). |

## API

| Метод | Описание |
|-------|----------|
| **UseEquippedItem()** | «Применить» предмет в руке: вызывает OnUseItemRequested(itemId), затем у экземпляра в руке — PickableItem.Activate() (если есть). Вызывайте из кода/кнопки или по клавише Use (E). |
| **DropEquipped(int amount = 1)** | Выбросить предмет в руке через назначенный InventoryDropper. Возвращает количество выброшенных; 0, если Dropper не назначен или предмета нет. По клавише G (при Allow Drop Input) вызывается автоматически. |
| **SelectNext()** | Следующий слот (по порядку снимка инвентаря), с зацикливанием. |
| **SelectPrevious()** | Предыдущий слот, с зацикливанием. |
| **SetSlotIndex(int index)** | Установить слот по индексу. При Allow Empty Slot допустим **-1** (ничего не в руке); иначе 0 .. count−1. |
| **RefreshSlotFromInventory()** | Пересчитать слот из инвентаря, синхронизировать Selector и отобразить предмет в руке. |

| Свойство | Описание |
|----------|----------|
| **SlotIndex** | Текущий индекс слота. |
| **EquippedItemId** | ItemId выбранного предмета (−1 если пусто). Для NeoCondition: Source = Component → InventoryHand, Property = EquippedItemId. |
| **EquippedItemData** | InventoryItemData выбранного предмета. |

## Интеграция с Selector

1. Добавьте на объект **Selector** (без заполнения Items — используется виртуальный Count).
2. В **InventoryHand** укажите этот Selector в поле **Selector**.
3. Включите **Sync Selector On Inventory Changed**.
4. Кнопки/ввод: вызывайте **Selector.Next()** и **Selector.Previous()** (или подпишитесь на них в инспекторе). Selector выдаёт индекс, InventoryHand переводит его в выбранный слот и обновляет Hand Anchor.

Рекомендуется для Selector: **Loop** = true. Чтобы можно было выбрать «ничего не в руке» (индекс -1), включите у Selector **Allow Empty Effective Index** и в руке — **Allow Empty Slot**; тогда Previous с 0 даёт -1, в руке ничего не отображается, EquippedItemId = −1.

## Пример настройки

1. Инвентарь: **InventoryComponent** на персонаже или менеджере.
2. Рука: пустой GameObject (например «Hand») как дочерний к персонажу.
3. На тот же объект (или на персонажа) добавьте **InventoryHand**: Hand Anchor = Hand, Inventory = ссылка на инвентарь.
4. Для переключения с клавиш/геймпада: добавьте **Selector** (Count задаётся автоматически), в поле InventoryHand → Selector укажите этот Selector. На кнопки «след. / пред.» вызывайте `Selector.Next()` и `Selector.Previous()`.

## Связанные компоненты и сценарии

- **InventoryDropper** — выброс предмета из руки: назначьте Dropper в Hand; по клавише (G) и из кода вызывайте **DropEquipped()**. Hand при включении отключает у Dropper ввод по клавише, чтобы дроп по G обрабатывала только рука. Без Hand — Dropper сам обрабатывает G и дропает SelectedItemId.
- **HandView** — вьюшка руки на префабе предмета: офсеты позиции/поворота и базовый масштаб в руке; рука применяет их первой, затем общий масштаб (дельта или фиксированный).
- **PickableItem** — подбор в тот же инвентарь; при применении в руке у экземпляра вызывается **Activate()** (если компонент есть), срабатывает **OnActivate**. При одном слоте (Max Unique = 1) — режим «одна вещь в руке».
- **План сценариев** (один предмет в руке / много предметов + Selector): [InventoryHand_Plan.md](./InventoryHand_Plan.md).

## Связанные методы InventoryComponent

- **GetNonEmptySlotCount()** — число слотов с count > 0.
- **GetItemIdAtSlotIndex(int slotIndex)** — itemId по индексу слота.
- **SelectedItemId** / **SelectedItemCount** — синхронизируются с текущим слотом руки (для дропа и условий).
- **TryConsume(itemId, 1)** — в подписчике OnUseItemRequested для расходуемых предметов.
