# LevelManager

**Что это:** `LevelManager` — singleton-компонент для работы с картами и уровнями. Он хранит текущий уровень, активную карту, прогресс по `Map[]`, обновляет `LevelButton` и рассылает события изменения уровня/карты. Файл: `Scripts/Level/LevelManager.cs`, пространство имён: `Neo.Level`.

**Как использовать:**
1. Добавьте `LevelManager` на сцену.
2. Настройте `Save Key`, массив `Maps` и список `Level Buttons`.
3. При необходимости включите `On Awake Next Level` и `On Awake Next Map`.
4. Используйте `TextLevel` и `LevelButton` как UI-подписчиков на события менеджера.

---

## Публичные свойства

| Свойство | Описание |
|----------|----------|
| `MaxLevel` | Максимальный открытый уровень текущей карты. |
| `MapId` | Индекс активной карты. |
| `CurrentLevel` | Текущий выбранный уровень. |
| `Map` | Текущая карта (`Map`) или `null`, если карта невалидна. |

## События

- `OnChangeLevel`
- `OnChangeMap`
- `OnChangeMaxLevel`

## Основные методы

| Метод | Описание |
|-------|----------|
| `SetLastMap()` | Переключает на последнюю незавершённую карту. |
| `GetLastIdMap()` | Возвращает индекс последней незавершённой карты. |
| `GetLastLevelId()` | Возвращает максимальный открытый уровень текущей карты. |
| `SetMapId(int id)` | Переключает текущую карту с валидацией индекса. |
| `NextLevel()` | Переходит к следующему уровню. |
| `SetLastLevel()` | Выбирает последний доступный уровень текущей карты. |
| `Restart()` | Повторно выставляет текущий уровень. |
| `SaveLevel()` | Сохраняет прогресс текущей карты, если текущий уровень совпадает с открытым уровнем карты. |
| `GetLoopLevel(int idLevel, int count)` | Возвращает безопасный loop-индекс уровня. |

## Важные детали текущей версии

- `SetMapId()` теперь валидирует диапазон и вызывает `OnChangeMap` с реальным `MapId`.
- Если `Maps` пустой, менеджер создаёт fallback-карту в `OnValidate()`.
- `Map` возвращает `null`, если индекс карты невалиден.
- `GetLoopLevel()` безопасно обрабатывает `count <= 0`.
- `OnChangeMaxLevel` вызывается не только при инициализации, но и при смене карты.

## UI-связки

- `LevelButton` использует `LevelManager` для визуализации открытых, текущих и закрытых уровней.
- `TextLevel` может выводить текущий или максимальный уровень через `OnChangeLevel` / `OnChangeMaxLevel`.
- `SceneFlowController` можно использовать для загрузки сцен на основе выбранного уровня.

## См. также

- [README](./README.md)
- [Map](./Map.md)
- [TextLevel](./TextLevel.md)
- [SceneFlowController](./SceneFlowController.md) — загрузка сцен (sync/async/additive), прогресс, Quit/Restart/Pause.
- [LevelButton](./LevelButton.md)
- [SaveManager](../Save/README.md)
