# Neoxider Docs — документация и навигация

Добро пожаловать в документацию Neoxider. Здесь собраны ссылки на ключевые разделы и краткие инструкции по запуску.

---

## Как ориентироваться

| Каталог | Что внутри | Документация |
|---------|------------|---------------|
| `Animations` | Универсальная система анимации значений, цветов, векторов и источников света | [`./Animations/README.md`](./Animations/README.md) |
| `Audio` | Менеджеры звука, настройки микшера, play-on-click | [`./Audio/README.md`](./Audio/README.md) |
| `Bonus` | Коллекции, слот‑машины, колёса удачи | [`./Bonus/README.md`](./Bonus/README.md) |
| `Editor` | Атрибуты редактора, инспекторные тулзы | [`./Editor/README.md`](./Editor/README.md) |
| `Extensions` | Расширения C# и Unity API | [`./Extensions/README.md`](./Extensions/README.md) |
| `GridSystem` | Сетки, перемещение по ячейкам, NavMesh‑интеграция | [`./GridSystem.md`](./GridSystem.md) |
| `Level` | Уровни, карты, прогресс игрока | [`./Level/LevelManager.md`](./Level/LevelManager.md) |
| `Parallax` | Универсальный параллакс с предпросмотром | [`./ParallaxLayer.md`](./ParallaxLayer.md) |
| `Save` | Система сохранений с атрибутами `[SaveField]` | [`./Save/README.md`](./Save/README.md) |
| `Shop` | Магазин, валюта, кэшбэк | [`./Shop/README.md`](./Shop/README.md) |
| `Tools` | Набор «кирпичиков»: спавнеры, таймеры, менеджеры и др. | [`./Tools/README.md`](./Tools/README.md) |
| `UI` | UI‑анимации, кнопки, страницы, прогресс‑бары | [`./UI/README.md`](./UI/README.md) |

Полный список — в соответствующих подпапках `Docs`. Каждый markdown содержит быстрый старт и примеры.

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



