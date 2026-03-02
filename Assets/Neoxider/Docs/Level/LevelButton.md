# LevelButton

**Что это:** компонент кнопки выбора уровня. Отображает номер уровня, доступность (Activ), переключает визуал (Closes/Opens), работает с [LevelManager](./LevelManager.md). Пространство имён: `Neo.Level`. Файл: `Scripts/Level/LevelButton.cs`.

**Как использовать:** добавить на кнопку уровня, назначить **Level**, **Activ**, **Level Manager**; при необходимости — объекты в Closes/Opens и события OnChangeVisual/OnDisableVisual/OnEnableVisual/OnCurrentVisual.

---

## Поля и события

- **Level** — номер уровня.
- **Activ** — доступен ли уровень для прохождения.
- **Level Manager** — ссылка на синглтон LevelManager.
- **Closes / Opens** — объекты, скрываемые или показываемые при смене визуала кнопки.
- **OnChangeVisual / OnDisableVisual / OnEnableVisual / OnCurrentVisual** — события для обновления отображения (закрыт/открыт/текущий уровень).

## См. также

- [LevelManager](./LevelManager.md)
- [SceneFlowController](./SceneFlowController.md)
