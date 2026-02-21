# NeoxiderTools

**Коллекция 150+ готовых инструментов для Unity** — быстрая разработка игр без лишней сложности.

**Версия 6.0.7** · Unity 2022.1+ · Namespace `Neo`

- [GitHub](https://github.com/NeoXider/NeoxiderTools)
- [Changelog](./CHANGELOG.md)
- [Документация](./Docs/README.md)
- [PROJECT_SUMMARY](./PROJECT_SUMMARY.md)

---

## No-Code условия — логика без кода

**NeoCondition** — собирайте условия прямо в Inspector:

- **Поля и свойства** — HP, очки, счёт, состояние объекта, любые int/float/bool/string компонента или GameObject
- **Методы с аргументом** — вызов методов вида `GetCount(int)`: выберите метод в Property, укажите Argument (itemId и т.п.), сравните с порогом или с другой переменной. Удобно для проверки «количество предмета в инвентаре ≥ N» без кода
- **AND/OR** — несколько условий в одном компоненте, инверсия (NOT)
- **События** — `OnTrue`, `OnFalse`, `OnResult` подключаются к любым UnityEvent
- **Поиск по имени и префабы** — Find By Name, Prefab Preview для настройки до спавна

> Примеры: «Health.Hp ≤ 0 → Game Over»; «InventoryComponent.GetCount(itemId) ≥ 3 → открыть дверь» — всё в Inspector.

Подробнее: [NeoCondition](./Docs/Condition/NeoCondition.md)

---

## Установка

### Unity Package Manager (Git URL)

```
https://github.com/NeoXider/NeoxiderTools.git?path=Assets/Neoxider
```

Window -> Package Manager -> **+** -> Add package from git URL.

### Ручная установка

Скопируйте папку `Assets/Neoxider` в ваш Unity-проект.

### Зависимости

| Пакет                           | Как подключить                                                                                        |
|---------------------------------|-------------------------------------------------------------------------------------------------------|
| TextMeshPro                     | Устанавливается автоматически через UPM                                                               |
| AI Navigation                   | Устанавливается автоматически через UPM                                                               |
| DOTween (опционально)           | Asset Store или [GitHub](https://github.com/Demigiant/dotween)                                        |
| Odin Inspector (опционально)    | Asset Store — расширенный инспектор, все компоненты работают и без него                               |
| Markdown Renderer (опционально) | Для отображения документации в инспекторе (блок «Documentation» и кнопка «Open in window»). См. ниже. |

**Установка Markdown Renderer (опционально):** Window → Package Manager → **+** → Add package from git URL → вставить:

```
https://github.com/NeoXider/MarkdownRenderer.git
```

Без этого пакета **ничего не ломается**: блок «Documentation» в инспекторе по-прежнему показывает превью и кнопку «Open
in window» (открывает .md в стандартном инспекторе или выделяет ассет в Project). Пакет подключается только через
рефлексию при нажатии «Open in window».

---

## Быстрый старт

1. Импортируйте пакет (UPM Git URL или копирование папки `Assets/Neoxider`)
2. Добавьте в сцену системный префаб `Prefabs/--System--.prefab` (менеджеры, UI)
3. Добавляйте компоненты через **Add Component → Neoxider** — большинство настраивается без кода через Inspector и UnityEvent

---

## Модули

| Модуль                | Описание                                                         | Документация                                |
|-----------------------|------------------------------------------------------------------|---------------------------------------------|
| **Animations**        | Анимация значений, цветов, Vector3                               | [Docs](./Docs/Animations/README.md)         |
| **Audio**             | AudioManager, микшер, play-on-click                              | [Docs](./Docs/Audio/README.md)              |
| **Bonus**             | Коллекции, слот-машины, колесо удачи, награды по времени         | [Docs](./Docs/Bonus/README.md)              |
| **Cards**             | Карточные игры (MVP): колода, рука, покер, "Пьяница"             | [Docs](./Docs/Cards/README.md)              |
| **Condition**         | No-Code условия: поля, свойства, методы с аргументом (int/float/string), AND/OR, события | [Docs](./Docs/Condition/NeoCondition.md)    |
| **Extensions**        | 300+ extension-методов для C# и Unity API                        | [Docs](./Docs/Extensions/README.md)         |
| **GridSystem**        | Универсальные сетки: shape/origin/pathfinding, Match3, TicTacToe | [Docs](./Docs/GridSystem.md)                |
| **Level**             | Менеджер уровней, карта, кнопки                                  | [Docs](./Docs/Level/LevelManager.md)        |
| **NPC**               | Модульная навигация NPC (патруль, преследование, агро)           | [Docs](./Docs/NPC/README.md)                |
| **Parallax**          | Параллакс-слои с предпросмотром                                  | [Docs](./Docs/ParallaxLayer.md)             |
| **Save**              | Система сохранений: PlayerPrefs, JSON, атрибут `[SaveField]`     | [Docs](./Docs/Save/README.md)               |
| **Shop**              | Магазин, валюта, покупки                                         | [Docs](./Docs/Shop/README.md)               |
| **StateMachine**      | State Machine + NoCode визуальный редактор                       | [Docs](./Docs/StateMachine/StateMachine.md) |
| **Tools**             | Спавнеры, таймеры, физика, ввод, Counter, **Inventory** (подбор, дроп, NeoCondition) | [Docs](./Docs/Tools)                        |
| **UI**                | UI-анимации, кнопки, страницы, toggle                            | [Docs](./Docs/UI/README.md)                 |
| **PropertyAttribute** | `[Button]`, `[GUIColor]`, `[RequireInterface]`, inject-атрибуты  | [Docs](./Docs/PropertyAttribute/README.md)  |
| **Editor**            | Кастом-инспектор, авто-билд, Scene Saver                         | [Docs](./Docs/Editor/README.md)             |

### Опциональные модули (UPM Samples)

Устанавливаются через **Package Manager -> Neoxider Tools -> Samples -> Import**:

| Модуль            | Описание                                              |
|-------------------|-------------------------------------------------------|
| **Demo Scenes**   | Демо-сцены и примеры использования                    |
| **NeoxiderPages** | PageManager — система страниц/экранов (UIPage, UIKit) |

> **Для разработчиков пакета:** сэмплы лежат в папке `Samples` (Demo, NeoxiderPages). Хук в `scripts/git-hooks/` не даёт
> закоммитить папку `Samples~` — в репозитории должна быть только `Samples` для совместимости UPM на Windows (см.
`scripts/git-hooks/README.md`).

---

## Примеры использования

### NeoCondition (No-Code условия)

1. Добавьте **NeoCondition** (Add Component → Neo → Condition → NeoCondition)
2. Кнопка **+** → выберите **Source Object**, **Component** и **Property** (или метод с аргументом, например `GetCount (int) → Int [method]` и укажите **Argument**)
3. Задайте оператор (≥, ==, …) и порог или сравнение с **Other Object**
4. Подключите **On True** / **On False** — логика без кода готова

### Counter (без кода)

1. Добавьте компонент `Counter` на GameObject
2. Настройте режим (Int/Float), начальное значение
3. Подключите события `OnValueChangedInt` / `OnSendInt` к вашим объектам в инспекторе
4. Вызывайте методы `Add`, `Subtract`, `Set` через UnityEvent других компонентов

### Timer

1. Добавьте `TimerObject` на GameObject
2. Укажите длительность, looping, режим
3. Подпишитесь на `OnTimerComplete`, `OnTimerUpdate` в инспекторе

### Inventory + NeoCondition (количество предмета)

1. На объекте — **InventoryComponent**, на другом (или том же) — **NeoCondition**
2. Условие: Source = Component → InventoryComponent → Property = **GetCount (int) → Int [method]** → Argument = itemId (например 5) → Compare ≥ 3
3. On True → открыть дверь, выдать квест и т.д. Без кода.

### Save

```csharp
SaveProvider.SetInt("score", 100);
SaveProvider.Save();
int score = SaveProvider.GetInt("score", 0);
```

---

## Структура проекта

```
Assets/Neoxider/
  Scripts/       # Runtime-код (238 скриптов, 33 asmdef)
  Editor/        # Editor-инструменты
  Docs/          # Документация по модулям
  Demo/          # Примеры
  Prefabs/       # Готовые префабы
  Resources/     # Настройки
  Shaders/       # Шейдеры (UI Blur и др.)
  Sprites/       # UI иконки
```

---

## Поддержка

Нашли проблему или есть предложения? Создайте [issue](https://github.com/NeoXider/NeoxiderTools/issues) или PR в
основном репозитории.
