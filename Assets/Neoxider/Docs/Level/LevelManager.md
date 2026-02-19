# LevelManager

Синглтон уровней и карт (Map). Хранит текущий уровень и карту, сохраняет прогресс, даёт доступ к LevelButton и переходам на следующий уровень/карту.

**Добавить:** GameObject → Neoxider → Level → LevelManager.

## Основное

- **Save Key** — ключ сохранения.
- **Current Level** / **Maps** / **Map Id** — текущий уровень, массив карт, активная карта.
- **Level Buttons** — кнопки уровней для разблокировки.
- **On Awake Next Level** / **On Awake Next Map** — автоматический переход при старте.

## См. также

- [SceneFlowController](./SceneFlowController.md) — загрузка сцен (sync/async/additive), прогресс, Quit/Restart/Pause.
- [LevelButton](./LevelButton.md)
- [SaveManager](../Save/README.md)
