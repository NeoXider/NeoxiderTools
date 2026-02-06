# NeoxiderTools

**Коллекция 150+ готовых инструментов для Unity** — быстрая разработка игр без лишней сложности.

Версия: **5.8.1** | Unity: **2022.1+** | Namespace: `Neo`

- [GitHub](https://github.com/NeoXider/NeoxiderTools)
- [Changelog](./CHANGELOG.md)
- [Документация](./Docs/README.md)
- [PROJECT_SUMMARY](./PROJECT_SUMMARY.md)

---

## No-Code условия — проектируй логику без единой строчки кода

С компонентом **NeoCondition** вы можете строить сложную игровую логику прямо в Inspector:

- **Проверяйте любые данные** — HP, очки, состояние объекта, любое поле любого компонента
- **Комбинируйте условия** — AND/OR логика, инверсия, несколько проверок в одном компоненте
- **Реагируйте на изменения** — события `OnTrue`, `OnFalse`, `OnResult` подключаются к любым UnityEvent
- **Работайте с префабами** — находите объекты по имени, настраивайте условия до спавна через Prefab Preview
- **Без написания кода** — выбирайте компонент, свойство, оператор и порог из выпадающих списков

> Пример: «Когда `Health.Hp <= 0` — показать Game Over» — одна строка в Inspector, ноль строк в коде.

Подробнее: [NeoCondition Documentation](./Docs/Condition/NeoCondition.md)

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

| Пакет | Как подключить |
|-------|---------------|
| TextMeshPro | Устанавливается автоматически через UPM |
| AI Navigation | Устанавливается автоматически через UPM |
| DOTween (опционально) | Asset Store или [GitHub](https://github.com/Demigiant/dotween) |
| Odin Inspector (опционально) | Asset Store — расширенный инспектор, все компоненты работают и без него |

---

## Быстрый старт

1. Импортируйте пакет
2. Добавьте системный префаб `Prefabs/--System--.prefab` в сцену (менеджеры событий, UI)
3. Перетаскивайте нужные компоненты из инспектора — большинство работает без кода через UnityEvent

---

## Модули

| Модуль | Описание | Документация |
|--------|----------|-------------|
| **Animations** | Анимация значений, цветов, Vector3 | [Docs](./Docs/Animations/README.md) |
| **Audio** | AudioManager, микшер, play-on-click | [Docs](./Docs/Audio/README.md) |
| **Bonus** | Коллекции, слот-машины, колесо удачи, награды по времени | [Docs](./Docs/Bonus/README.md) |
| **Cards** | Карточные игры (MVP): колода, рука, покер, "Пьяница" | [Docs](./Docs/Cards/README.md) |
| **Condition** | No-Code условия: проверка полей компонентов, AND/OR, события | [Docs](./Docs/Condition/NeoCondition.md) |
| **Extensions** | 300+ extension-методов для C# и Unity API | [Docs](./Docs/Extensions/README.md) |
| **GridSystem** | Генерация сеток, ячейки, спавн объектов | [Docs](./Docs/GridSystem.md) |
| **Level** | Менеджер уровней, карта, кнопки | [Docs](./Docs/Level/LevelManager.md) |
| **NPC** | Модульная навигация NPC (патруль, преследование, агро) | [Docs](./Docs/NPC/README.md) |
| **Parallax** | Параллакс-слои с предпросмотром | [Docs](./Docs/ParallaxLayer.md) |
| **Save** | Система сохранений: PlayerPrefs, JSON, атрибут `[SaveField]` | [Docs](./Docs/Save/README.md) |
| **Shop** | Магазин, валюта, покупки | [Docs](./Docs/Shop/README.md) |
| **StateMachine** | State Machine + NoCode визуальный редактор | [Docs](./Docs/StateMachine/StateMachine.md) |
| **Tools** | Спавнеры, таймеры, физика, ввод, менеджеры, Counter и др. | [Docs](./Docs/Tools) |
| **UI** | UI-анимации, кнопки, страницы, toggle | [Docs](./Docs/UI/README.md) |
| **PropertyAttribute** | `[Button]`, `[GUIColor]`, `[RequireInterface]`, inject-атрибуты | [Docs](./Docs/PropertyAttribute/README.md) |
| **Editor** | Кастом-инспектор, авто-билд, Scene Saver | [Docs](./Docs/Editor/README.md) |

### Опциональные модули (UPM Samples)

Устанавливаются через **Package Manager -> Neoxider Tools -> Samples -> Import**:

| Модуль | Описание |
|--------|----------|
| **Demo Scenes** | Демо-сцены и примеры использования |
| **NeoxiderPages** | PageManager — система страниц/экранов (UIPage, UIKit) |

> **Для разработчиков пакета:** сэмплы лежат в папке `Samples` (Demo, NeoxiderPages). Хук в `scripts/git-hooks/` не даёт закоммитить папку `Samples~` — в репозитории должна быть только `Samples` для совместимости UPM на Windows (см. `scripts/git-hooks/README.md`).

---

## Примеры использования

### NeoCondition (No-Code условия)

1. Добавьте `NeoCondition` на GameObject (Add Component -> Neo -> Condition -> NeoCondition)
2. Добавьте условие кнопкой **+**
3. Выберите **Source Object**, **Component** и **Property** из dropdown
4. Задайте оператор сравнения и порог
5. Подключите события **On True** / **On False** — готово, без кода!

### Counter (без кода)

1. Добавьте компонент `Counter` на GameObject
2. Настройте режим (Int/Float), начальное значение
3. Подключите события `OnValueChangedInt` / `OnSendInt` к вашим объектам в инспекторе
4. Вызывайте методы `Add`, `Subtract`, `Set` через UnityEvent других компонентов

### Timer

1. Добавьте `TimerObject` на GameObject
2. Укажите длительность, looping, режим
3. Подпишитесь на `OnTimerComplete`, `OnTimerUpdate` в инспекторе

### Save

```csharp
// Сохранить значение
SaveProvider.SetInt("score", 100);
SaveProvider.Save();

// Загрузить значение
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

Нашли проблему или есть предложения? Создайте [issue](https://github.com/NeoXider/NeoxiderTools/issues) или PR в основном репозитории.
