# Level

**Что это:** модуль управления прогрессом по уровням и картам. Включает `LevelManager`, `LevelButton`, `TextLevel`, `Map`, а также `SceneFlowController` для загрузки сцен и переходов. Скрипты лежат в `Scripts/Level/`.

**Оглавление:**
- [LevelManager](./LevelManager.md)
- [LevelButton](./LevelButton.md)
- [TextLevel](./TextLevel.md)
- [Map](./Map.md)
- [SceneFlowController](./SceneFlowController.md)

---

## Как использовать

1. Добавьте `LevelManager` на сцену.
2. Настройте массив `Maps`.
3. При необходимости привяжите `LevelButton` и `TextLevel`.
4. Используйте `SceneFlowController`, если нужен переход между сценами по текущему уровню.

## Что входит в модуль

- `LevelManager` — хранит текущий уровень, текущую карту и события изменения.
- `Map` — сериализуемая запись прогресса одной карты.
- `LevelButton` — визуализация состояния уровня в UI.
- `TextLevel` — вывод текущего или максимального уровня в UI.
- `SceneFlowController` — загрузка сцен, пауза, рестарт и переходы.
