# SceneFlowController

## Назначение

Универсальный компонент для загрузки сцен и базовых действий приложения: загрузка по индексу или имени, синхронно или асинхронно, с отображением прогресса (текст, Slider, Image), событием «готов к переходу» и действиями Quit, Restart, Pause. Режим загрузки и активации задаётся полями в Inspector; методы загрузки параметров режима не принимают.

**Добавить:** Add Component → Neoxider → Level → SceneFlowController (или GameObject → Neoxider → Level → SceneFlowController).

## Режимы загрузки (SceneFlowLoadMode)

| Режим | Поведение |
|-------|------------|
| **Sync** | Синхронная загрузка, сцена подменяется сразу. |
| **Async** | Асинхронная загрузка с автоактивацией при готовности. |
| **AsyncManual** | Асинхронная загрузка без автоактивации; по готовности вызывается OnReadyToProceed, затем по кнопке — ProceedScene(). |
| **Additive** | Загрузка сцены в режиме Additive (текущая сцена не выгружается). |

## Поля в Inspector

### Сцена
- **Load Mode** — режим загрузки (Sync, Async, AsyncManual, Additive).
- **Scene Build Index** — индекс сцены в Build Settings.
- **Scene Name** — имя сцены (для LoadScene() при Use Scene Name = true).
- **Use Scene Name** — при вызове LoadScene() без аргументов: true = грузить по Scene Name, false = по Scene Build Index.
- **Activate On Ready** — для Async: активировать сцену сразу по готовности; для AsyncManual не используется.

### Прогресс UI
- **Text Progress** (UnityEngine.UI.Text) — опционально, текст прогресса.
- **Text Mesh Progress** (TMP) — опционально, текст прогресса.
- **Slider Progress** — опционально, ползунок 0..1.
- **Image Progress** — опционально, fillAmount 0..1.
- **Progress Text Format** — Plain или Percent (например «Loading... 45%»).
- **Progress Prefix**, **Ready To Proceed Text** — строки для отображения.
- **Progress Panel** — GameObject панели загрузки (включается в начале, выключается по завершении).

### События
- **On Load Started** — в начале загрузки.
- **On Progress** (float) — прогресс 0..1 при асинхронной загрузке.
- **On Ready To Proceed** — загрузка готова, ждёт вызова ProceedScene() (показать кнопку «Продолжить»).
- **On Load Completed** — сцена загружена и активирована.

## Публичные методы

| Метод | Описание |
|-------|----------|
| `LoadScene(int buildIndex)` | Загрузка по build index. Режим из настроек. |
| `LoadScene(string sceneName)` | Загрузка по имени. Режим из настроек. |
| `LoadScene()` | Загрузка по полям компонента (имя или индекс). Для кнопки без аргументов. |
| `Restart()` | Перезагрузка текущей сцены. |
| `Quit()` | Выход из приложения. |
| `Pause(bool active)` | Пауза (Time.timeScale). |
| `ProceedScene()` | Активация асинхронно загруженной сцены. Вызывать после OnReadyToProceed. |

## Пример: кнопка «Продолжить» после async загрузки

1. Load Mode = AsyncManual.
2. На кнопку «Продолжить» в OnClick повесить вызов `SceneFlowController.ProceedScene()`.
3. В On Ready To Proceed показать панель с этой кнопкой (или включить GameObject кнопки).

## Миграция с UIReady

- Заменить компонент UIReady на SceneFlowController (Neoxider/Level/SceneFlowController).
- Поля ALS (gameObjectLoad, animator, textProgress, loadEndText) → Progress Panel, при необходимости Animator на дочернем объекте, Text/TMP и строки в Progress Prefix / Ready To Proceed Text.
- LoadScene(int) / LoadScene() / Restart / Quit / Pause / ProceedScene() — имена и назначение совпадают; LoadScene(string) в SceneFlowController добавлен.
- Режим загрузки задаётся в Inspector (Load Mode), не передаётся в методы.

См. также: [UIReady](../UI/UIReady.md) (устаревший), [LevelManager](./LevelManager.md).
