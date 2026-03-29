# Tools

**Что это:** `Tools` — самый большой раздел пакета с reusable gameplay-компонентами: движение, инвентарь, ввод, спавнеры, время, визуальные utility, менеджеры и вспомогательные runtime-системы. Скрипты лежат в `Scripts/Tools/`.

**Оглавление:** используйте индекс подмодулей ниже или быстрый старт по сценариям.

---

## С чего начать

- Нужен инвентарь, подбор предметов, дроп и hand-view: [`Inventory/README.md`](./Inventory/README.md)
- Нужен спавн или object pooling: [`Spawner/README.md`](./Spawner/README.md)
- Нужен movement/input workflow: [`Move/README.md`](./Move/README.md) и [`Input/README.md`](./Input/README.md)
- Нужны пользовательские настройки (графика, экран, мышь, FPS): [`../Settings/README.md`](../Settings/README.md)
- Нужен диалоговый UI flow: [`Dialogue/README.md`](./Dialogue/README.md)
- Нужны utility-компоненты (`Counter`, `AnimatorParameterDriver`, `RpgStatsDamageableBridge`); для нового боя/HP/уклонения — [RPG](../Rpg/README.md): [`Components/README.md`](./Components/README.md)

## Индекс подмодулей

| Подмодуль | Что внутри | Документация |
|-----------|------------|--------------|
| **Components** | `Counter`, `Loot`, `ScoreManager`, `TypewriterEffect`, `RpgStatsDamageableBridge` | [`Components/README.md`](./Components/README.md) |
| **Dialogue** | `DialogueController`, `DialogueData`, `DialogueUI` | [`Dialogue/README.md`](./Dialogue/README.md) |
| **Input** | `MouseEffect`, `MouseInputManager`, `MultiKeyEventTrigger` | [`Input/README.md`](./Input/README.md) |
| **Inventory** | `InventoryComponent`, `InventoryDropper`, `PickableItem` | [`Inventory/README.md`](./Inventory/README.md) |
| **InteractableObject** | `InteractiveObject`, `PhysicsEvents2D`, `PhysicsEvents3D` | [`InteractableObject/README.md`](./InteractableObject/README.md) |
| **Managers** | `Bootstrap`, `EM`, `GM`, `Singleton` | [`Managers/README.md`](./Managers/README.md) |
| **Move** | Контроллеры движения, камеры и курсора | [`Move/README.md`](./Move/README.md) |
| **Physics** | `ExplosiveForce`, `ImpulseZone`, `MagneticField` | [`Physics/README.md`](./Physics/README.md) |
| **Random** | `ChanceManager`, `ChanceSystemBehaviour`, chance data | [`Random/README.md`](./Random/README.md) |
| **Spawner** | `PoolManager`, `NeoObjectPool`, `Spawner` | [`Spawner/README.md`](./Spawner/README.md) |
| **Text** | `SetText`, `TimeToText` | [`Text/README.md`](./Text/README.md) |
| **Time** | `Timer`, `TimerObject` | [`Time/README.md`](./Time/README.md) |
| **View** | `Selector`, `StarView`, billboard/view helpers | [`View/README.md`](./View/README.md) |
| **Debug** | `FPS`, `ErrorLogger`, debug utility компоненты | [`Debug/README.md`](./Debug/README.md) |
| **Draw** | `Drawer` и визуальные drawing/debug helpers | [`Draw/README.md`](./Draw/README.md) |
| **FakeLeaderboard** | `Leaderboard`, `LeaderboardItem`, `LeaderboardMove` | [`FakeLeaderboard/README.md`](./FakeLeaderboard/README.md) |
| **Other** | `CameraShake`, `SpineController`, `RevertAmount` и misc helpers | [`Other/README.md`](./Other/README.md) |

## Отдельные файлы верхнего уровня

| Файл | Назначение |
|------|------------|
| [`CameraAspectRatioScaler.md`](./CameraAspectRatioScaler.md) | Масштабирование под aspect ratio |
| [`UpdateChilds.md`](./UpdateChilds.md) | Утилита обновления дочерних объектов |
