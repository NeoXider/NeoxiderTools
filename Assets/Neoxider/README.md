# Neoxider — коллекция мощных инструментов для Unity

## [v5.2.8](./CHANGELOG.md)

> «Мне хотелось иметь библиотеку, которая ускоряет рутину, не превращая проект в чёрный ящик. Поэтому я собрал Neoxider.»

Neoxider — это экосистема из более чем 150 модулей: UI, бонусы, экономика, ObjectPool, Spine, параллакс и многое другое. Пакет вырос на реальных проектах, поэтому он одинаково полезен и для быстрого прототипа, и для долгой поддержки коммьюнити‑проектов.

---

## Чем примечателен Neoxider

- **Production-ready**: каждая подсистема поставляется с примерами, документацией и продуманными интеграциями.
- **No-code там, где нужно**: большинство компонентов настраиваются через инспектор и UnityEvent, но при этом остаются расширяемыми.
- **Документация внутри**: у каждого модуля есть собственный README в `Assets/Neoxider/Docs/...`.

---

## Как ориентироваться

| Каталог | Что внутри | Документация |
|---------|------------|---------------|
| `Audio` | Менеджеры звука, настройки микшера, play-on-click | [`Docs/Audio.md`](./Docs/Audio.md) |
| `Bonus` | Коллекции, слот-машины, колёса удачи | [`Docs/Bonus.md`](./Docs/Bonus.md) |
| `Editor` | Атрибуты редактора, инспекторные тулзы | [`Docs/Editor.md`](./Docs/Editor.md) |
| `Extensions` | Расширения C# и Unity API | [`Docs/Extensions/README.md`](./Docs/Extensions/README.md) |
| `GridSystem` | Сетки, перемещение по ячейкам, NavMesh‑интеграция | [`Docs/GridSystem.md`](./Docs/GridSystem.md) |
| `Level` | Уровни, карты, прогресс игрока | [`Docs/Level/LevelManager.md`](./Docs/Level/LevelManager.md) |
| `Parallax` | Универсальный параллакс с предпросмотром | [`Docs/ParallaxLayer.md`](./Docs/ParallaxLayer.md) |
| `Save` | Система сохранений с атрибутами `[SaveField]` | [`Docs/Save.md`](./Docs/Save.md) |
| `Shop` | Магазин, валюта, кэшбэк | [`Docs/Shop.md`](./Docs/Shop.md) |
| `Tools` | Огромный набор «кирпичиков»: спавнеры, таймеры, SpineController и др. | [`Docs/Tools/README.md`](./Docs/Tools/README.md) |
| `UI` | UI-анимации, кнопки, страницы, прогресс-бары | [`Docs/UI/README.md`](./Docs/UI/README.md) |

Полный список — в соответствующих подпапках `Docs`. Каждый markdown содержит быстрый старт и примеры.

---

## Зависимости

### Обязательные зависимости

- **Unity:** 2022.1 или выше
- **TextMeshPro:** автоматически устанавливается через Package Manager

### Основные зависимости (для Runtime модулей)

Эти зависимости требуются для работы модулей `Runtime/` (логирование, DI, реактивное программирование):

- **[R3](https://github.com/Cysharp/R3)** — реактивное программирование (через NuGetForUnity)
- **[VContainer](https://github.com/hadashiA/VContainer)** — Dependency Injection контейнер (через Git URL)
- **[MessagePipe](https://github.com/Cysharp/MessagePipe)** — система сообщений Pub/Sub (через Git URL)
- **[MessagePipe.VContainer](https://github.com/Cysharp/MessagePipe)** — адаптер MessagePipe для VContainer (через Git URL)
- **[Serilog](https://serilog.net/)** — структурированное логирование (через NuGetForUnity)
- **[Serilog.Sinks.File](https://github.com/serilog/serilog-sinks-file)** — файловый вывод логов (через NuGetForUnity)

### Опциональные зависимости

- **DOTween** (по желанию) — для анимаций UI компонентов
- **Spine Unity Runtime** — для модулей Spine (только для `SpineController`)

---

## Установка зависимостей

### ⚠️ Важно о зависимостях

Unity Package Manager **не может автоматически устанавливать зависимости через Git URL** из `package.json` пакета. Все зависимости нужно устанавливать **вручную** в проекте пользователя.

> 💡 **Примечание:** Только `TextMeshPro` установится автоматически, так как это стандартный Unity пакет. Остальные зависимости требуют ручной установки.

### Ручная установка зависимостей

**Все зависимости нужно устанавливать вручную** в вашем проекте. Unity Package Manager не может автоматически установить зависимости через Git URL.

#### 1. Установка VContainer, MessagePipe (через Git URL)

Эти пакеты **требуют ручной установки** в `Packages/manifest.json` вашего проекта:

**Через Package Manager UI:**
1. Откройте `Window > Package Manager`
2. Нажмите `+` → `Add package from git URL...`
3. Добавьте по очереди:
   ```
   https://github.com/hadashiA/VContainer.git?path=VContainer/Assets/VContainer
   https://github.com/Cysharp/MessagePipe.git?path=src/MessagePipe.Unity/Assets/Plugins/MessagePipe
   https://github.com/Cysharp/MessagePipe.git?path=src/MessagePipe.Unity/Assets/Plugins/MessagePipe.VContainer
   ```

**Через manifest.json:**
Откройте `Packages/manifest.json` и добавьте зависимости в секцию `dependencies`:

```json
{
  "dependencies": {
    "jp.hadashikick.vcontainer": "https://github.com/hadashiA/VContainer.git?path=VContainer/Assets/VContainer",
    "com.cysharp.messagepipe": "https://github.com/Cysharp/MessagePipe.git?path=src/MessagePipe.Unity/Assets/Plugins/MessagePipe",
    "com.cysharp.messagepipe.vcontainer": "https://github.com/Cysharp/MessagePipe.git?path=src/MessagePipe.Unity/Assets/Plugins/MessagePipe.VContainer"
  }
}
```

> ⚠️ **Важно:** Эти зависимости **НЕ установятся автоматически** при установке пакета. Их нужно добавить вручную в `manifest.json` вашего проекта.

#### 2. Установка R3 и Serilog (через NuGetForUnity)

R3 и Serilog устанавливаются через NuGet, а не через Unity Package Manager:

1. **Установите NuGetForUnity:**
   - Откройте `Window > Package Manager`
   - Нажмите `+` → `Add package from git URL...`
   - Введите: `https://github.com/GlitchEnzo/NuGetForUnity.git`
   - Или скачайте с [GitHub](https://github.com/GlitchEnzo/NuGetForUnity/releases)

2. **Установите пакеты через NuGet:**
   - Откройте `NuGet > Manage NuGet Packages`
   - Найдите и установите:
     - `R3` (последняя версия, обычно 1.3.0+)
     - `Serilog` (последняя версия, обычно 4.3.0+)
     - `Serilog.Extensions.Logging` (обычно 9.0.2+)
     - `Serilog.Sinks.File` (обычно 7.0.0+)

**Альтернативный способ (через packages.config):**
Создайте или обновите файл `Assets/packages.config`:
```xml
<?xml version="1.0" encoding="utf-8"?>
<packages>
  <package id="R3" version="1.3.0" />
  <package id="Serilog" version="4.3.0" />
  <package id="Serilog.Extensions.Logging" version="9.0.2" />
  <package id="Serilog.Sinks.File" version="7.0.0" />
</packages>
```
Затем Unity автоматически установит их через NuGetForUnity.

#### 3. Опциональные зависимости

**DOTween:**
- Скачайте с [официального сайта](http://dotween.demigiant.com/download.php) или через Asset Store
- Импортируйте в проект через `Assets > Import Package > Custom Package...`

**Spine Unity Runtime:**
- Установите через Asset Store: [Spine Unity Runtime](https://assetstore.unity.com/packages/tools/animation/spine-unity-2d-skeletal-animation-56455)
- Или через официальный сайт: [esotericsoftware.com](http://esotericsoftware.com/)

---

## Быстрый старт

1. **Установите NuGetForUnity** (требуется для R3 и Serilog).
2. **Установите R3 и Serilog через NuGetForUnity** (требуется для Runtime модулей).
3. **Установите VContainer и MessagePipe через Git URL** в `manifest.json`.
4. **Импортируйте папку `Assets/Neoxider`** в свой проект (если устанавливаете вручную).
5. **Добавьте системный префаб** `Assets/Neoxider/Prefabs/--System--.prefab` в сцену — он подключает менеджеры событий и UI.
6. **Подключите нужные подсистемы**: компоненты находятся в папках `Scripts/…`, а примеры и готовые конфигурации — в `Demo/` и `Prefabs/`.
7. **Изучите документацию**: откройте соответствующий README в `Docs`, чтобы настроить модуль за несколько минут.

> 💡 **Совет:** Для работы Runtime модулей обязательно установите:
> - **R3 и Serilog** через NuGetForUnity
> - **VContainer и MessagePipe** через Git URL в `manifest.json`

---

## Топовые модули

- **SpineController** — фасад для Spine с UnityEvent-обёртками, автозаполнением анимаций/скинов и отключением, если Spine Runtime отсутствует.
- **ParallaxLayer** — параллакс с предпросмотром, зазорами, рандомизацией и автоматической переработкой тайлов.
- **Drawer** — инструмент рисования линий с поддержкой LineRenderer/EdgeCollider, сглаживанием и UnityEvent для креативных механик.
- **DialogueManager** — диалоговая система с персонажами, портретами и событиями на каждой реплике.
- **SwipeController** — обработчик свайпов (мышь, тач, геймпад) с UnityEvent и фильтрами расстояния.
- **MouseEffect** — добавляет «сочности» вашему UI, создавая визуальные эффекты, следующие за курсором. Позволяет спавнить префабы по клику, рисовать трейлы и привязывать объекты к мыши, работая в связке с `MouseInputManager`.
- **ChanceManager** — декларативная система вероятностей для лута, рулеток и других случайностей.
- **ObjectPool / Spawner** — расширяемый пул с волнами, задержками, случайным выбором префабов и инспекторной настройкой.
- **FakeLeaderboard** — динамический лидерборд с анимацией, сортировкой, автозаполнением и UI-примерами.
- **SetText** — мощный компонент для `TextMeshPro`, который анимирует изменение чисел (очки, валюта) и автоматически форматирует их, добавляя разделители, знаки валют и проценты. Управляется через один метод `Set()` и настраивается в инспекторе.
- **MovementToolkit** — набор контроллеров движения (клава/мышь, 2D/3D, follow-камеры, ограничители экрана).
- **InteractiveObject** — база для зон и триггеров, позволяет строить взаимодействия без кода.
- **Timer / TimerObject** — таймеры с паузой, повтором, сериализацией и событиями прогресса.
- **LightAnimator** — универсальный аниматор для `Light` и `Light2D` (URP), позволяющий создавать эффекты мерцания, пульсации и органичного свечения с помощью шума Перлина без кода.

---

## Лицензия

Neoxider Tools распространяется под кастомной коммерческой лицензией.

Проект можно использовать бесплатно, в том числе в коммерческих целях. Отчисления (роялти) в размере 0.5% требуются только в том случае, если ваш продукт, использующий эту библиотеку, зарабатывает более $10,000.

Полный текст лицензии (на английском языке) находится в файле `LICENSE.md`.

---

## FAQ

**Можно использовать выборочно?** Да, импортируйте только нужные папки: зависимости указаны в документации.

**Есть примеры сцен?** Да, в папке `Demo`. Там набор минимальных сцен для проверки каждого крупного модуля.

**Работает с 3D?** Большинство систем — да. Исключение: чисто 2D-ориентированные решения вроде `ParallaxLayer`.

---

## Поддержка и вклад

Neoxider развивается. Если нашли баг или хотите предложить модуль — открывайте issue/PR. В ответ мы стараемся документировать все изменения и предоставляем руководство по миграции.

Удачи в разработке и приятного продакшена!
