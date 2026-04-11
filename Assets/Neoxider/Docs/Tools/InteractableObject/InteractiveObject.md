# Компонент Interactive Object

**Что это:** Просто добавьте его на объект с коллайдером или на элемент UI, настройте события в инспекторе — и объект станет интерактивным.

**Как использовать:** см. разделы ниже.

---


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
- `useHoverDetection` (по умолчанию `true`): Включить наведение мыши (hover) по попаданию курсора в collider
- `useMouseInteraction` (по умолчанию `true`): Включить взаимодействие мышью (click / down / up). Наведение может быть включено отдельно.
- `useKeyboardInteraction` (по умолчанию `true`): Включить взаимодействие клавиатурой
- `keyboardInteractionMode`:
  - `ViewOrMouse` (по умолчанию): для клавиатуры требуется взгляд на объект / прямой луч
  - `DistanceOnly`: клавиатура работает только по дистанции, без проверки взгляда
- `requireViewForKeyboardInteraction` (по умолчанию `true`): Клавиатурное взаимодействие доступно только если объект находится в направлении взгляда
- `requireDirectLookRay`: Требовать прямую видимость объекта по лучу от точки проверки
- `includeTriggerCollidersInLookRay` (по умолчанию `true`): Учитывать trigger-коллайдеры в проверке взгляда (важно для интерактивных объектов с Trigger Collider)
- `includeTriggerCollidersInMouseRaycast` (по умолчанию `true`): Учитывать trigger-коллайдеры в луче наведения мыши (hover). Включено — объекты с Trigger Collider реагируют на курсор; выключено — только обычные коллайдеры.
- `targetCollider3D`: Необязательный явный 3D collider для hover/click/look-check. Если не задан, берётся `Collider` на этом объекте или **первый среди дочерних** (`GetComponentInChildren`).
- `targetCollider2D`: Необязательный явный 2D collider для hover/click/look-check. Если не задан, аналогично — на себе или среди детей.
  - Чужие `Trigger`-коллайдеры не блокируют hover/click для обычного collider целевого объекта под ними.
  - Обычные (не trigger) коллайдеры перед объектом по-прежнему блокируют hover/click.

### Screen Center Ray (мобильный прицел)
- **Рекомендуемый способ**: добавьте компонент `InteractionRayProvider` на камеру. Все `InteractiveObject` в сцене автоматически его подхватят.
  - `Mouse` — рейкаст от курсора/пальца (десктоп по умолчанию)
  - `ScreenCenter` — рейкаст от центра экрана (мобильный прицел)
  - `Both` — центр экрана для hover, палец для click (работает везде)
- `useScreenCenterRay` на самом `InteractiveObject` — per-object fallback. Если на камере есть `InteractionRayProvider`, он имеет приоритет.

### Debug
- `drawDebugRay` (по умолчанию `false`): **Постоянно** отрисовывать луч взаимодействия каждый кадр. Цвет меняется в зависимости от состояния:
  - 🔘 **Серый** — объект вне зоны досягаемости
  - 🔵 **Голубой (cyan)** — объект в зоне, но не под курсором / не наведён
  - 🟡 **Жёлтый** — объект наведён (hover)
  - 🟢 **Зелёный** — нажатие / взаимодействие (interact down)
- `drawInteractionRayForOneSecond`: Кратковременный луч только при срабатывании взаимодействия (legacy)
- `interactionRayDrawDuration`: Длительность legacy-луча в секундах (по умолчанию 1 секунда)

### Distance Control (контроль дистанции)
- `interactionDistance` (по умолчанию `3`): Максимальная дистанция взаимодействия в метрах
  - `0` = без ограничений (можно взаимодействовать с любого расстояния)
  - `> 0` = только в пределах указанного радиуса
- `distanceCheckPoint`: Точка отсчёта для проверки дистанции (обычно игрок или камера)
  - Если не указана, используется `Camera.main`
- `viewCheckPoint`: Точка/трансформ для проверки направления взгляда
  - Рекомендуется назначать камеру (`Camera.main.transform`)
- `ignoreDistancePointHierarchyColliders` (по умолчанию `true`): Игнорировать коллайдеры иерархии `distanceCheckPoint` (например, капсулу игрока и риг камеры)
- `checkObstacles` (по умолчанию `true`): Проверка препятствий (стен) между объектом и точкой проверки
  - Использует raycast для обнаружения блокирующих коллайдеров
  - Если между объектом и точкой проверки есть препятствие, взаимодействие блокируется
  - Если выключить, препятствия игнорируются для дистанции, для keyboard look-ray (`requireDirectLookRay`) **и** для луча мыши: hover/click срабатывают при попадании в коллайдер объекта, даже если ближе к камере есть обычный (не-trigger) коллайдер
  - Если включить, луч мыши требует, чтобы ваш объект был ближе по лучу, чем любой чужой **не-trigger** коллайдер (иначе курсор «не доходит» через стены/пол)
  - Отключите для UI элементов или специальных случаев
- `obstacleLayers`: Слои, которые блокируют взаимодействие (используется при `checkObstacles = true`)
  - По умолчанию все слои блокируют взаимодействие
- `includeTriggerCollidersInObstacleCheck` (по умолчанию `false`): Учитывать trigger-коллайдеры в проверке препятствий
  - Включите, если преграды или зоны-блокеры сделаны через Trigger Collider
  - Выключите, если trigger-зоны не должны мешать взаимодействию

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
- `onHoverChanged(bool)`: Состояние наведения изменилось (`true` = enter, `false` = exit)

Наведение (`hover`) срабатывает **само по попаданию курсора в collider**, без нажатия `E` или кнопок мыши.

- если `interactionDistance = 0`, наведение работает **без ограничения по дистанции**;
- если `interactionDistance > 0`, наведение требует попадания в collider **и** чтобы **точка попадания луча** была в радиусе (не только центр коллайдера) — так на границе дистанции не «ломается» сценарий «уже навёлся и подошёл ближе».

Проверка препятствий (`checkObstacles`) влияет и на **hover**, и на click мышью (линия видимости по лучу), плюс на дистанцию и keyboard look-ray — см. описание `checkObstacles` выше.

### Как считается click мышью

- Click / Down / Up по мыши требуют **реального текущего попадания курсора в collider** объекта.
- Если луч мыши не попал в collider объекта, событие click не вызывается, даже если объект находится в радиусе `interactionDistance`.
- При **`checkObstacles = true`**: чужой обычный collider ближе по лучу блокирует hover/click; чужой trigger — нет.
- При **`checkObstacles = false`**: достаточно попадания в ваш collider по лучу (в сочетании с лимитом дистанции, если он задан).

### Как считается `E`

- В режиме `DistanceOnly` клавиатура проверяет только дистанцию.
- В режиме `ViewOrMouse` клавиатура больше **не опирается на hover**; используется направление взгляда и, если включено `requireDirectLookRay`, прямой луч до объекта.

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

По умолчанию коллайдер ищется на **этом объекте**, затем среди **дочерних** (типичный случай: скрипт на корне, `SphereCollider` Is Trigger на дочернем пустом объекте).

Если на детях несколько коллайдеров и нужен конкретный, задайте **`targetCollider3D`** / **`targetCollider2D`** явно.

---

## 6. Примеры использования

### Дверь с проверкой дистанции
```csharp
// Настройки в инспекторе:
interactionDistance = 3       // можно открыть с 3 метров
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
requireViewForKeyboardInteraction = true
requireDirectLookRay = true
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
checkObstacles = false        // отключить проверку препятствий для UI
useMouseInteraction = true
useKeyboardInteraction = false

// События:
onHoverEnter → PlayHoverSound()
onClick → ActivateButton()
onDoubleClick → SpecialAction()
```

### Объект с проверкой препятствий
```csharp
// Настройки:
interactionDistance = 5       // можно взаимодействовать с 5 метров
checkObstacles = true         // включена проверка препятствий (по умолчанию)
obstacleLayers = Walls        // только стены блокируют взаимодействие
distanceCheckPoint = Player

// События:
onEnterRange → ShowHint("Нажми E")
onInteractDown → Interact()   // работает только если нет стены между игроком и объектом
```

### Сундук с визуализацией радиуса
```csharp
// Настройки:
interactionDistance = 3
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

### Мобильный режим
- `UseScreenCenterRay { get; set; }`: Per-object fallback — рейкаст от центра экрана. **Предпочитайте `InteractionRayProvider` на камере.**

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
- Проверка препятствий выполняется только при `checkObstacles = true`
- События срабатывают только при изменении состояния

### Совместимость
- Работает с UI, 2D и 3D объектами
- Поддерживает мышь и клавиатуру одновременно
- События могут срабатывать параллельно от разных источников
- Для клавиатуры можно ограничить интеракцию по направлению взгляда (angle + line-of-sight)
- Поддерживает чтение клавиши интеракции (`keyboardKey`) как через Legacy Input Manager, так и через New Input System (если пакет установлен)

### Проверка препятствий
- По умолчанию включена проверка препятствий (`checkObstacles = true`)
- Использует raycast для обнаружения блокирующих коллайдеров между объектом и точкой проверки
- Автоматически исключает коллайдер самого объекта из проверки
- Работает с 2D и 3D физикой
- Настройка слоев через `obstacleLayers` позволяет контролировать, какие объекты блокируют взаимодействие
- Отключите `checkObstacles` для UI элементов или когда взаимодействие через стены допустимо

---

## 10. Мобильная платформа (Android / iOS)

### Базовая совместимость
Unity автоматически маппит первое касание экрана как мышку (`Input.mousePosition` / `Input.GetMouseButton(0)`), поэтому базовое взаимодействие тапом работает из коробки.

### Режим «прицел в центре» — InteractionRayProvider
Добавьте компонент **`InteractionRayProvider`** на камеру:

```csharp
// Получить/создать провайдер на главной камере:
var provider = InteractionRayProvider.FindOnMainCamera();
provider.Mode = InteractionRayProvider.RayMode.ScreenCenter; // прицел
provider.Mode = InteractionRayProvider.RayMode.Mouse;        // классика
provider.Mode = InteractionRayProvider.RayMode.Both;          // центр для hover, палец для click
```

**Как это работает:**
1. Рейкаст hover идёт из `cam.ScreenPointToRay(center)` — куда смотрит прицел
2. Игрок свайпает для вращения камеры → прицел наводится на объект → hover срабатывает
3. Тап → click срабатывает по объекту под прицелом
4. Все проверки дистанции и препятствий работают так же, как на десктопе

### Подключение джойстика к контроллерам

Оба контроллера (`PlayerController3DPhysics`, `PlayerController2DPhysics`) поддерживают внешний ввод через API:

```csharp
// 3D контроллер:
PlayerController3DPhysics controller3D;

// Подключить on-screen joystick:
controller3D.SetMoveInput(joystick.Direction);  // Vector2 (x=strafe, y=forward)
controller3D.SetLookInput(lookPad.Delta);       // Vector2 (x=yaw, y=pitch)
controller3D.SetJumpInput();                    // кнопка прыжка
controller3D.SetRunInput(runToggle.isOn);        // кнопка бега

// Отключить внешний ввод (вернуть к клавиатуре):
controller3D.SetMoveInput(null);
controller3D.SetLookInput(null);
```

```csharp
// 2D контроллер:
PlayerController2DPhysics controller2D;

controller2D.SetMoveInput(joystick.Direction);  // Vector2 или float (-1..1)
controller2D.SetJumpInput();                    // кнопка прыжка
controller2D.SetRunInput(true);                 // кнопка бега
```
