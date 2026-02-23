# План: рука, один предмет и инвентарь с переключением

Два сценария: **одна вещь в руке** (подбор/использование/выброс) и **много предметов** (инвентарь + Selector + рука + использование + выброс).

---

## Сценарий 1: Один предмет в руке

Подбираемый предмет попадает «в руку», в руке одновременно только один предмет. Действия: **использовать** и **выбросить**.

### Компоненты

| Компонент | Роль |
|-----------|------|
| **InventoryComponent** | Один слот: `Max Unique Items = 1`, `Max Total Items = 1`. Хранит текущий предмет в руке. |
| **PickableItem** | Подбор: при сборе добавляет предмет в инвентарь (т.е. «в руку»). Цель — тот же объект, что с InventoryComponent. |
| **InventoryHand** | Показывает префаб текущего предмета в **Hand Anchor**. Без Selector (переключение не нужно). |
| **InventoryDropper** | Выброс: по клавише/событию убирает предмет из инвентаря и спавнит его в мире. |

### Настройка

1. **Инвентарь на 1 слот**  
   Один объект (например, персонаж): **InventoryComponent**  
   - Database (опционально), Save Key при необходимости.  
   - **Max Unique Items = 1**, **Max Total Items = 1**.  
   - Initial State — пусто или один стартовый предмет.

2. **Рука**  
   На том же объекте (или дочернем): **InventoryHand**  
   - **Inventory** — ссылка на этот InventoryComponent.  
   - **Hand Anchor** — Transform точки «руки» (куда ставится модель предмета).  
   - **Selector** — не назначать.  
   - Fallback Hand Prefab — при необходимости.

3. **Подбор**  
   На префабе предмета: **PickableItem**  
   - Item Data / Item Id, Amount = 1.  
   - Target Inventory = тот же InventoryComponent (или Auto Find).  
   - Collect On Trigger 3D/2D — по желанию.

4. **Выброс**  
   На том же объекте, что инвентарь: **InventoryDropper**  
   - **Inventory** — тот же InventoryComponent.  
   - Drop Key (например, G), Drop Selected On Key = true.  
   - В коде/UnityEvent можно вызывать `inventory.DropSelected(1)` или `dropper.DropSelected(1)`.

5. **Использование предмета**  
   На **InventoryHand** подписаться на **OnUseItemRequested(int itemId)**:  
   - аптечка → восстановить здоровье и вызвать `inventory.TryConsume(itemId, 1)`;  
   - ключ → открыть дверь (без траты);  
   - данные предмета при необходимости — `inventory.GetItemData(itemId)`.  
   Вызов «использовать»: из ввода/кнопки вызывать **InventoryHand.UseEquippedItem()**.

### Итог по сценарию 1

- Подбор → предмет в единственный слот инвентаря → рука показывает его в Hand Anchor.  
- **UseEquippedItem()** → OnUseItemRequested → ваша логика (трата через TryConsume при необходимости).  
- Выброс → **InventoryDropper** (DropSelected / по клавише).

---

## Сценарий 2: Много предметов (инвентарь + переключение)

Полный инвентарь, переключение между предметами (Selector), в руке отображается выбранный. Действия: **переключить**, **использовать**, **выбросить**.

### Компоненты

| Компонент | Роль |
|-----------|------|
| **InventoryComponent** | Полный инвентарь (лимиты по желанию), автосохранение. |
| **Selector** | Текущий «слот»: виртуальный Count = число слотов с предметами. Next/Previous меняют выбранный слот. |
| **InventoryHand** | Показывает в Hand Anchor предмет выбранного слота; синхронизация с Selector и SelectedItemId. |
| **InventoryDropper** | Выброс выбранного предмета (SelectedItemId). |
| **PickableItem** | Добавляет предметы в тот же InventoryComponent. |

### Пошаговый план настройки

**Шаг 1. Инвентарь**

- Один объект (менеджер/персонаж): **InventoryComponent**.  
- Database, Save Key, Max Unique / Max Total при необходимости.  
- Load Mode, Initial State — по игре.

**Шаг 2. Рука (точка отображения)**

- Создать дочерний Transform (например, «Hand») — куда ставится модель предмета.  
- На объекте с инвентарём (или на персонаже): **InventoryHand**.  
  - **Inventory** — ссылка на этот InventoryComponent.  
  - **Hand Anchor** = Hand.  
  - **Fallback Hand Prefab** — если у части предметов нет WorldDropPrefab.

**Шаг 3. Selector для переключения**

- На том же объекте (или отдельном): **Selector**.  
  - **Items** не заполнять (режим виртуального Count).  
  - **Loop** = true.  
  - В **InventoryHand** в поле **Selector** указать этот Selector.  
  - **Sync Selector On Inventory Changed** = true.  
  - Count и текущий индекс задаёт InventoryHand по числу слотов с предметами.

**Шаг 4. Ввод переключения**

- Кнопки/ось (колесо, Q/E и т.д.):  
  - «Следующий предмет» → **Selector.Next()**.  
  - «Предыдущий предмет» → **Selector.Previous()**.  
  - Либо напрямую **InventoryHand.SelectNext()** / **SelectPrevious()**.  
- После смены Selector рука и SelectedItemId обновляются автоматически.

**Шаг 5. Выброс**

- На том же объекте: **InventoryDropper**.  
  - **Inventory** — тот же InventoryComponent.  
  - **Drop Selected On Key** = true (выбрасывается текущий в руке).  
  - Клавиша выброса или вызов **DropSelected(amount)** из кода/событий.

**Шаг 6. Использование предмета**

- На **InventoryHand** подписаться на **OnUseItemRequested(int itemId)**.  
  - По itemId выполнить эффект (здоровье, ключ, фонарик и т.д.); данные — `inventory.GetItemData(itemId)` или `hand.EquippedItemData`.  
  - Если предмет расходуемый — вызвать `inventory.TryConsume(itemId, 1)`.  
- Кнопка «Использовать» → **InventoryHand.UseEquippedItem()**.

**Шаг 7. Подбор**

- На префабах предметов: **PickableItem** с тем же Target Inventory (или Auto Find).  
- Подобранный предмет добавляется в инвентарь; при необходимости рука/Selector обновятся (если включена синхронизация).

### Итог по сценарию 2

- Инвентарь хранит все предметы.  
- Selector переключает «текущий слот» (индекс по слотам с count > 0).  
- InventoryHand отображает в Hand Anchor предмет этого слота и держит SelectedItemId синхронизированным.  
- **UseEquippedItem()** → OnUseItemRequested → ваша логика (и при необходимости TryConsume).  
- **InventoryDropper** выбрасывает текущий выбранный предмет.

---

## Общее

- **Использовать**: всегда через **InventoryHand.UseEquippedItem()** и подписку на **OnUseItemRequested**; трату реализуете через **TryConsume** там, где нужно.  
- **Выброс**: **InventoryDropper** (DropSelected / по клавише).  
- **Один предмет в руке**: те же компоненты, инвентарь с Max Unique = 1, Max Total = 1, без Selector.  
- **Много предметов**: полный инвентарь + Selector + InventoryHand + Dropper по плану выше.
