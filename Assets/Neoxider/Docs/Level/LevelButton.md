# LevelButton

Кнопка уровня для выбора и разблокировки уровней. Работает в связке с [LevelManager](./LevelManager.md).

- **Пространство имён:** `Neo.Level`
- **Путь:** `Assets/Neoxider/Scripts/Level/LevelButton.cs`

## Основное

- **Level** — номер уровня.
- **Activ** — доступен ли уровень для прохождения.
- **Level Manager** — ссылка на синглтон LevelManager.
- **Closes / Opens** — объекты, скрываемые или показываемые при смене визуала кнопки.
- **OnChangeVisual / OnDisableVisual / OnEnableVisual / OnCurrentVisual** — события для обновления отображения (закрыт/открыт/текущий уровень).

## См. также

- [LevelManager](./LevelManager.md)
- [SceneFlowController](./SceneFlowController.md)
