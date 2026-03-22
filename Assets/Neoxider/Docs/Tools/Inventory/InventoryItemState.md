# Состояние предмета в инвентаре (IInventoryItemState)

**Что это:** контракт и утилиты для сериализации **уникального** состояния экземпляра предмета (JSON внутри `InventoryItemInstance.ComponentStates`), одна запись на компонент с ключом `InventoryStateKey`. Используется вместе с `InventoryItemData.Supports Instance State`. Файлы: `Core/IInventoryItemState.cs`, `Runtime/InventoryItemStateBehaviour.cs`, `Runtime/InventoryItemStateUtility.cs`.

## Зачем два разных типа

| Что | Роль |
|-----|------|
| **InventoryItemStateBehaviour** | Твой скрипт на префабе предмета. Ты наследуешь класс и пишешь **что** сохранять (патроны, прочность, текст ключа) в `CaptureInventoryState` и **как** вернуть это в `RestoreInventoryState`. Без этого инвентарь не знает про твои поля — это единственное место с игровой логикой предмета. |
| **InventoryItemStateUtility** | Общий служебный код **не** для наследования. Он обходит все `IInventoryItemState` на префабе, вызывает у каждого Capture при подборе и Restore при спавне из инвентаря. `PickableItem` / `InventoryDropper` / рука пользуются им внутри — тебе руками вызывать не обязательно, если только не делаешь свой пайплайн. |

Коротко: **Behaviour = данные одного типа предмета; Utility = один раз «собрать все такие компоненты с префаба» при pickup/drop.**

**Как использовать:**
1. В `InventoryItemData` включите **Supports Instance State**, **Max Stack** обычно = 1.
2. На префаб мира (`World Drop Prefab`) добавьте наследника `InventoryItemStateBehaviour` (или `MonoBehaviour` с `IInventoryItemState`) и реализуйте `CaptureInventoryState` / `RestoreInventoryState`.
3. При необходимости задайте уникальный ключ в инспекторе (поле **Inventory State Key** у базового behaviour); иначе ключ = полное имя типа.
4. Подбор: `PickableItem` при сборе вызывает захват состояния с иерархии префаба (если предмет помечен как instance-based).
5. Выброс: `InventoryDropper` спавнит префаб и восстанавливает состояние по сохранённому payload.
6. Сохранение: payload хранится внутри JSON контейнера (`InventorySaveData`) по **Save Key** `InventoryComponent`, отдельные ключи SaveProvider на каждый предмет не нужны.

### Пошаговое создание такого предмета (чеклист)

1. **ScriptableObject:** создай `Inventory Item Data` (меню *Neoxider → Tools → Inventory*), задай **Item Id**, включи **Supports Instance State**, **Max Stack** = `1` (или другое осмысленное для нестакаемого экземпляра), назначь **World Drop Prefab** — префаб объекта в мире.
2. **Префаб мира:** на корне (или дочерних объектах) повесь **PickableItem** (`Item Data` или тот же id), настрой подбор (триггер / `Collect`).
3. **Состояние:** на том же префабе добавь свой класс от **InventoryItemStateBehaviour** (или `MonoBehaviour` + `IInventoryItemState`) и реализуй `CaptureInventoryState` / `RestoreInventoryState` (обычно `JsonUtility` или свой формат строки).
4. **База:** добавь этот `InventoryItemData` в **Inventory Database**, чтобы лимиты стака и lookup работали.
5. **Игра:** положи префаб в сцену или выдай предмет кодом (`AddItemData` / `AddItemInstance`); при сохранении контейнера состояние уйдёт в общий blob по **Save Key**.

Ниже — пример «пистолет с патронами» и таблица API.

---

## Интерфейс IInventoryItemState

| Член | Описание |
|------|----------|
| **InventoryStateKey** | Строковый ключ; должен совпадать при capture и restore. |
| **CaptureInventoryState()** | Вернуть JSON (или другую сериализуемую строку). |
| **RestoreInventoryState(string json)** | Применить сохранённую строку к объекту в мире / в руке. |

## InventoryItemStateBehaviour

Абстрактный `MonoBehaviour`: задаёт ключ по умолчанию и требует реализовать capture/restore. Удобно для No-Code привязки в инспекторе.

## InventoryItemStateUtility

| Метод | Описание |
|-------|----------|
| **HasState(GameObject root)** | Есть ли на иерархии реализации интерфейса. |
| **CaptureInstance(root, itemId, count)** | Собрать все компоненты состояния в новый `InventoryItemInstance`. |
| **RestoreInstance(root, instance)** | Раздать JSON по компонентам с совпадающим ключом. |

## Пример механики: оружие с патронами в магазине

**Настройка данных:**

1. `InventoryItemData` «Pistol»: **Supports Instance State** = да, **Max Stack** = 1, **World Drop Prefab** = префаб пистолета в мире.

**Настройка префаба:**

1. На префабе скрипт-наследник `InventoryItemStateBehaviour`.
2. В `CaptureInventoryState` сериализуйте поля (например `JsonUtility.ToJson(new Data { Mag = _ammo })`).
3. В `RestoreInventoryState` — десериализация и применение к полям / UI.

**Инвентарь:**

1. Контейнер игрока: `InventoryComponent`, **Slot Grid** или **Aggregated**, уникальный **Save Key**.
2. После `Load()` патроны и апгрейды восстанавливаются из того же blob, что и слоты.

## См. также

- [InventoryComponent](./InventoryComponent.md) — раздел instance-based
- [PickableItem](./PickableItem.md)
- [InventoryDropper](./InventoryDropper.md)
- [InventoryHand](./InventoryHand.md)

← [Tools/Inventory](README.md)
