# Компонент Interactive Object

## 1. Введение

`InteractiveObject` — универсальный компонент для создания интерактивных объектов без написания кода. Поддерживает мышь, клавиатуру, проверку дистанции и множество событий. Работает с UI, 2D и 3D объектами.

Просто добавьте его на объект с коллайдером или на элемент UI, настройте события в инспекторе — и объект станет интерактивным.

---

## 2. Описание класса

### InteractiveObject
- **Пространство имен**: `Neo.Tools`
- **Путь к файлу**: `Assets/Neoxider/Scripts/Tools/InteractableObject/InteractiveObject.cs`

**Описание**
Компонент использует встроенную систему событий Unity (Event System) для отслеживания взаимодействий и вызова UnityEvent при различных действиях игрока.

---

## 3. Настройки

### Основные параметры
- `interactable`: Включить/выключить всю интерактивность объекта

### Interaction Settings (тип взаимодействия)
- `useMouseInteraction` (по умолчанию `true`): Включить взаимодействие мышью (hover, click)
- `useKeyboardInteraction` (по умолчанию `true`): Включить взаимодействие клавиатурой

### Distance Control (контроль дистанции)
- `interactionDistance` (по умолчанию `2`): Максимальная дистанция взаимодействия в метрах
  - `0` = без ограничений (можно взаимодействовать с любого расстояния)
  - `> 0` = только в пределах указанного радиуса
- `distanceCheckPoint`: Точка отсчёта для проверки дистанции (обычно игрок или камера)
  - Если не указана, используется `Camera.main`

### Down/Up — Mouse Binding
- `downUpMouseButton`: Какая кнопка мыши генерирует `onInteractDown/Up` (Left/Right/Middle)

### Down/Up — Keyboard Binding
- `keyboardKey`: Клавиша для `onInteractDown/Up` (по умолчанию `E`)

### Click Settings
- `doubleClickThreshold`: Время для распознавания двойного клика (0 = отключено)

---

## 4. События (UnityEvent)

### Hover Events (наведение)
- `onHoverEnter`: Курсор вошёл в область объекта
- `onHoverExit`: Курсор покинул область объекта

### Click Events (клики)
- `onClick`: Левый клик
- `onDoubleClick`: Двойной левый клик
- `onRightClick`: Правый клик
- `onMiddleClick`: Средний клик (колёсико)

### Down/Up Events (нажатие/отпускание)
- `onInteractDown`: Нажатие (мышь или клавиша)
- `onInteractUp`: Отпускание (мышь или клавиша)

### Distance Events (дистанция)
- `onEnterRange`: Игрок вошёл в зону взаимодействия
- `onExitRange`: Игрок вышел из зоны взаимодействия

---

## 5. Настройка сцены

### Event System
Убедитесь, что в сцене есть `EventSystem` (создаётся автоматически при добавлении Canvas).

### Raycasters
Компонент автоматически добавляет необходимые Raycaster'ы:
- **UI объекты**: Используется `GraphicRaycaster` на Canvas
- **2D объекты**: Автоматически добавляется `Physics 2D Raycaster` на камеру
- **3D объекты**: Автоматически добавляется `Physics Raycaster` на камеру

### Коллайдер
Для не-UI объектов нужен коллайдер (`BoxCollider`, `SphereCollider`, `BoxCollider2D`).

---

## 6. Примеры использования

### Дверь с проверкой дистанции
```csharp
// Настройки в инспекторе:
interactionDistance = 2       // можно открыть с 2 метров
distanceCheckPoint = Player   // проверка от игрока
keyboardKey = E

// События:
onEnterRange → ShowHint("Нажми E чтобы открыть")
onExitRange → HideHint()
onInteractDown → OpenDoor()
```

### Предмет, который можно подобрать
```csharp
// Настройки:
interactionDistance = 1.5     // можно поднять с 1.5 метров
useMouseInteraction = true
useKeyboardInteraction = true
keyboardKey = E

// События:
onEnterRange → HighlightItem()
onExitRange → RemoveHighlight()
onClick → PickUpItem()
onInteractDown → PickUpItem()  // клавиша E или клик
```

### NPC с диалогом
```csharp
// Настройки:
interactionDistance = 3       // можно говорить с 3 метров
useKeyboardInteraction = true
keyboardKey = E

// События:
onInteractDown → StartDialogue()
onEnterRange → ShowInteractionPrompt("Поговорить с NPC")
onExitRange → HidePrompt()
```

### Кнопка в UI (без дистанции)
```csharp
// Настройки:
interactionDistance = 0       // без ограничений
useMouseInteraction = true
useKeyboardInteraction = false

// События:
onHoverEnter → PlayHoverSound()
onClick → ActivateButton()
onDoubleClick → SpecialAction()
```

### Сундук с визуализацией радиуса
```csharp
// Настройки:
interactionDistance = 2
keyboardKey = E

// События:
onEnterRange → ShowOpenPrompt()
onExitRange → HideOpenPrompt()
onInteractDown → OpenChest()

// Визуализация:
// В Scene view отображается голубая сфера радиусом 2 метра
```

---

## 7. Публичный API (свойства для кода)

### Управление дистанцией
- `InteractionDistance { get; set; }`: Максимальная дистанция взаимодействия (0 = без ограничений)
- `DistanceCheckPoint { get; set; }`: Точка отсчёта для проверки дистанции

### Управление типами взаимодействия
- `UseMouseInteraction { get; set; }`: Включить/выключить взаимодействие мышью
- `UseKeyboardInteraction { get; set; }`: Включить/выключить взаимодействие клавиатурой

### Получение информации (read-only)
- `IsInInteractionRange { get; }`: true если в зоне взаимодействия
- `DistanceToCheckPoint { get; }`: Текущая дистанция до точки проверки
- `IsHovered { get; }`: true если объект под курсором

---

### Примеры использования API

```csharp
InteractiveObject interactive = GetComponent<InteractiveObject>();

// Увеличить радиус взаимодействия при апгрейде
interactive.InteractionDistance = 5f;

// Отключить мышь, оставить только клавиатуру
interactive.UseMouseInteraction = false;

// Динамическая проверка дистанции
if (interactive.IsInInteractionRange)
{
    Debug.Log("Игрок может взаимодействовать");
}

// Показать дистанцию в UI
distanceText.text = $"Расстояние: {interactive.DistanceToCheckPoint:F1}м";

// Проверка hover для подсветки
if (interactive.IsHovered)
{
    objectRenderer.material.color = Color.yellow;
}

// Сменить точку проверки дистанции
interactive.DistanceCheckPoint = player.transform;

// Временно отключить клавиатуру во время диалога
interactive.UseKeyboardInteraction = false;
```

---

## 8. Визуализация

При выборе объекта в редакторе (если `interactionDistance > 0`):
- **Голубая сфера**: радиус взаимодействия вокруг объекта
- Помогает визуально настроить комфортную дистанцию

---

## 9. Особенности

### Автоматическая настройка
- Автоматически добавляет необходимые Raycaster'ы на камеру
- Использует `Camera.main` как точку проверки дистанции по умолчанию
- Проверяет наличие EventSystem и предупреждает если его нет

### Оптимизация
- Использует `sqrMagnitude` для проверки дистанции (без корня)
- Проверка дистанции только когда `interactionDistance > 0`
- События срабатывают только при изменении состояния

### Совместимость
- Работает с UI, 2D и 3D объектами
- Поддерживает мышь и клавиатуру одновременно
- События могут срабатывать параллельно от разных источников
