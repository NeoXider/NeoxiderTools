# Neoxider Docs — документация и навигация

Добро пожаловать в документацию **NeoxiderTools** (v5.8.5). Здесь собраны ссылки на все модули и инструкции по запуску.

---

## Индекс модулей

| Модуль | Описание | Документация |
|--------|----------|-------------|
| **Animations** | Анимация значений, цветов, Vector3, света | [`Animations/README.md`](./Animations/README.md) |
| **Audio** | AudioManager, микшер, play-on-click | [`Audio/README.md`](./Audio/README.md) |
| **Bonus** | Коллекции, слот-машины, колёса удачи, награды по времени | [`Bonus/README.md`](./Bonus/README.md) |
| **Cards** | Карточные игры (MVP): колода, рука, покер, "Пьяница" | [`Cards/README.md`](./Cards/README.md) |
| **Condition** | No-Code условия: проверка полей компонентов, AND/OR, события. [Demo](../Demo/Scenes/Condition/) | [`Condition/NeoCondition.md`](./Condition/NeoCondition.md) |
| **Editor** | Кастом-инспектор, авто-билд, Scene Saver, утилиты | [`Editor/README.md`](./Editor/README.md) |
| **Extensions** | 300+ extension-методов для C# и Unity API | [`Extensions/README.md`](./Extensions/README.md) |
| **GridSystem** | Генерация сеток, ячейки, спавн объектов | [`GridSystem.md`](./GridSystem.md) |
| **Level** | Менеджер уровней, карта, кнопки | [`Level/LevelManager.md`](./Level/LevelManager.md) |
| **NPC** | Модульная навигация NPC (патруль, преследование, агро) | [`NPC/README.md`](./NPC/README.md) |
| **Parallax** | Параллакс-слои с предпросмотром | [`ParallaxLayer.md`](./ParallaxLayer.md) |
| **PropertyAttribute** | `[Button]`, `[GUIColor]`, `[RequireInterface]`, inject-атрибуты | [`PropertyAttribute/README.md`](./PropertyAttribute/README.md) |
| **Save** | Система сохранений: PlayerPrefs, JSON, `[SaveField]` | [`Save/README.md`](./Save/README.md) |
| **Shop** | Магазин, валюта, покупки | [`Shop/README.md`](./Shop/README.md) |
| **StateMachine** | State Machine + NoCode визуальный редактор состояний | [`StateMachine/StateMachine.md`](./StateMachine/StateMachine.md) |
| **Tools** | Спавнеры, таймеры, физика, ввод, менеджеры, Counter | [`Tools` (подпапки)](./Tools) |
| **UI** | UI-анимации, кнопки, страницы, toggle | [`UI/README.md`](./UI/README.md) |
| **UI Extension** | Готовые UI-префабы: Canvas, Layout, ScrollRect | [`UI Extension/README.md`](./UI%20Extension/README.md) |

### Подмодули Tools

| Подмодуль | Описание | Документация |
|-----------|----------|-------------|
| Tools/Components | Counter, Health, Evade, ScoreManager, TypewriterEffect | [`Tools/Components/README.md`](./Tools/Components/README.md) |
| Tools/Physics | MagneticField, ExplosiveForce, ImpulseZone | [`Tools/Physics/README.md`](./Tools/Physics/README.md) |
| Tools/Move | Follow, KeyboardMover, MouseMover, UniversalRotator | [`Tools/Move/README.md`](./Tools/Move/README.md) |
| Tools/Spawner | Spawner, SimpleSpawner, ObjectPool | [`Tools/Spawner/README.md`](./Tools/Spawner/README.md) |
| Tools/Time | Timer, TimerObject | [`Tools/Time/README.md`](./Tools/Time/README.md) |
| Tools/Input | SwipeController, MultiKeyEventTrigger, MouseEffect | [`Tools/Input`](./Tools/Input) |
| Tools/Managers | GM, EM, Bootstrap, Singleton | [`Tools/Managers/README.md`](./Tools/Managers/README.md) |
| Tools/View | Selector, BillboardUniversal, StarView | [`Tools/View`](./Tools/View) |
| Tools/Dialogue | DialogueController, DialogueData, DialogueUI | [`Tools/Dialogue/README.md`](./Tools/Dialogue/README.md) |
| Tools/Random | ChanceManager, ChanceData | [`Tools/Random`](./Tools/Random) |
| Tools/InteractableObject | InteractiveObject, PhysicsEvents, ToggleObject | [`Tools/InteractableObject`](./Tools/InteractableObject) |
| Tools/FakeLeaderboard | Leaderboard, LeaderboardItem | [`Tools/FakeLeaderboard`](./Tools/FakeLeaderboard) |
| Tools/Debug | FPS, ErrorLogger | [`Tools/Debug`](./Tools/Debug) |
| Tools/Draw | Drawer (рисование линий) | [`Tools/Draw`](./Tools/Draw) |
| Tools/Text | SetText, TimeToText | [`Tools/Text`](./Tools/Text) |

### Опциональные модули

| Модуль | Путь | Документация |
|--------|------|-------------|
| **NeoxiderPages** | `Assets/NeoxiderPages/` | `Assets/NeoxiderPages/Docs/README.md` |

### Демо-сцены

| Демо | Сцена | Описание |
|------|-------|----------|
| **Chance System** | `Demo/Scenes/Tools/ChanceSystemExample.unity` | Система шансов |
| **Attributes** | `Demo/Scenes/Tools/AttributeExample.unity` | Демонстрация PropertyAttribute |
| **Dialogue** | `Demo/Scenes/Tools/Dialogue.unity` | Система диалогов |
| **Draw** | `Demo/Scenes/Tools/DrawExample.unity` | Рисование линий |
| **Mouse Effector** | `Demo/Scenes/Tools/MouseEffectorExample.unity` | Эффекты мыши |

Каждый markdown содержит быстрый старт и примеры использования.

---

## Быстрый старт

1. Подготовьте зависимости
   - Unity 2022+
   - DOTween (для ряда анимационных и игровых модулей)
   - UniTask (асинхронное программирование)
   - Spine Unity Runtime (по желанию) — для модулей Spine
2. Импортируйте папку `Assets/Neoxider` в проект
3. Добавьте системный префаб `Assets/Neoxider/Prefabs/--System--.prefab` в сцену — он подключает менеджеры событий и UI
4. Подключите нужные подсистемы
   - Компоненты: `Assets/Neoxider/**/Scripts`
   - Примеры/префабы: `Assets/Neoxider/**/Demo`, `Assets/Neoxider/**/Prefabs`
5. Изучите документацию модуля
   - Откройте соответствующий README в таблице выше и следуйте разделу «Быстрый старт» внутри модуля

### Установка через Unity Package Manager (Git URL)

Если вы хотите подключить только содержимое `Assets/Neoxider` как пакет:

```
https://github.com/NeoXider/NeoxiderTools.git?path=Assets/Neoxider
```

Зависимости, устанавливаемые через UPM:
- UniTask: `https://github.com/Cysharp/UniTask.git?path=src/UniTask/Assets/Plugins/UniTask`
- DOTween: через Asset Store (`DG.Tweening`)

---

## Подсказки по интеграции

- Системный префаб `--System--.prefab` должен находиться ровно один раз в активной сцене.
- Большинство компонентов настраиваются в инспекторе и могут работать без кода; расширение через события и публичные API.
- Для тяжёлых игровых объектов используйте пул (`Tools/Spawner`, `ObjectPool`) — это ускоряет инстансинг и уменьшает GC.
- В UI‑модулях широко используются анимации и состояния — проверяйте примеры в `UI/README.md`.

---

## Поддержка

Если нашли проблему или есть предложения по улучшению — создайте issue/PR в основном репозитории. Мы стремимся держать документацию актуальной и предоставлять понятные примеры к каждому модулю.



