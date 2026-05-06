# SceneFlowController

**Назначение:** Удобная обертка над стандартным `SceneManager` Unity. Поддерживает синхронную, асинхронную и аддитивную загрузку сцен. Позволяет легко настроить UI-экран загрузки (Progress Bar, Text) прямо в Инспекторе, без написания кода загрузки.

## Подключение

1. Добавьте `Add Component > Neoxider > Level > SceneFlowController` на объект (например, на кнопку Play или менеджер сцены).
2. Выберите режим загрузки `_loadMode` (обычно `Async` для больших сцен).
3. Укажите индекс (`_sceneBuildIndex`) или имя сцены.
4. Настройте UI (ссылки на Slider, Text, Panel), чтобы игрок видел прогресс.

## Основные настройки (Inspector)

| Поле | Описание |
|------|----------|
| `_loadMode` | `Sync` (замораживает игру), `Async` (фоновая загрузка), `AsyncManual` (ждет команды активации), `Additive` (поверх текущей). |
| `_sceneBuildIndex` | Индекс сцены в Build Settings (если не используется имя). |
| `_sceneName` | Имя сцены (используется, если `_useSceneName` = true). |
| `_activateOnReady` | Для режима `Async`. Если `true`, сцена переключится сразу как загрузится на 100%. |
| `_loadOnStart` | Автоматически начать загрузку при спавне этого объекта. |
| `_progressPanel` | `GameObject` окна загрузки, который автоматически включится в начале и выключится в конце загрузки. |
| `_sliderProgress`, `_imageProgress` | UI элементы для отображения полоски загрузки. |
| `_textProgress`, `_textMeshProgress` | Текст для вывода процентов (`_progressTextFormat` = Percent). |

## Использование

```csharp
// Если настроено в инспекторе, просто вызываем без аргументов:
sceneFlowController.LoadScene();

// Или передаем имя/индекс напрямую:
sceneFlowController.LoadScene("Level_1");

// Быстрый рестарт текущей сцены:
sceneFlowController.Restart();
```

## См. также
- [LevelManager](LevelManager.md) - Логический прогресс уровней.
- [Корень модуля](../README.md)