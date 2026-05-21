# AnimationFly

**Назначение:** синглтон для анимации «полёта» бонусов, валюты, предметов или UI-иконок к цели. Компонент создаёт префабы и двигает их по дуге через DOTween.

Поддерживаются сценарии:

- объект из мира летит в UI;
- UI-объект летит в UI;
- UI-объект летит к объекту в мире;
- объект из мира летит к объекту в мире.

## Подключение

1. Добавьте компонент `Neoxider > UI > AnimationFly` на объект сцены.
2. Заполните `Bonus Prefab List`: `Bonus Type`, `Prefab`, `End Pos`.
3. Если эффект должен лететь в Canvas, задайте `Parent Canvas`, `Spawn Parent` и поставьте `Spawn Space = Canvas`.
4. Для сложных сцен задавайте `Default Start Space` / `Default End Space` явно, чтобы не полагаться на `Auto`.

## Быстрые сценарии

### Монета из мира летит в UI-счётчик

1. `Spawn Space = Canvas`.
2. `Parent Canvas` указывает на игровой Canvas.
3. `Spawn Parent` — контейнер внутри Canvas.

```csharp
AnimationFly.I.PlayByTypeWorldToCanvas(0, amount, worldPickupTransform, moneyTextRectTransform);
```

### UI-иконка летит в UI-счётчик

```csharp
AnimationFly.I.PlayByTypeCanvasToCanvas(0, amount, sourceRectTransform, targetRectTransform);
```

### UI-объект летит к объекту в мире

```csharp
AnimationFly.I.PlayByTypeCanvasToWorld(0, amount, sourceRectTransform, worldTargetTransform);
```

### Полёт в мире

```csharp
AnimationFly.I.PlayByTypeWorldToWorld(0, amount, startTransform, endTransform);
```

## Основные поля (Inspector)

| Поле | Описание |
|------|----------|
| `bonusPrefabList` | Список типов бонусов, префабов и целей по умолчанию. |
| `bonusType` | Числовой тип бонуса для поиска через `PlayByType...` / `Execute(...)`. |
| `prefab` | Префаб летящего объекта. Для UI обычно нужен `RectTransform`. |
| `endPos` | Цель по умолчанию для старого API `Execute(type, count, start)`. |
| `endSpace` | Пространство цели: `Auto`, `World`, `Canvas`, `Screen`. |
| `defaultStartSpace` | Как читать стартовую позицию в общих `Play(...)` и старых `Execute(...)`. |
| `defaultEndSpace` | Как читать конечную позицию в общих `Play(...)` и старых `Execute(...)`. |
| `spawnSpace` | Где создавать и двигать объект: `Auto`, `World`, `Canvas`. |
| `parentCanvas` | Canvas для конвертации координат. |
| `spawnParent` | Родитель созданных объектов. Для UI обычно контейнер внутри Canvas. |
| `animationCamera` | Камера для конвертации world/screen/canvas. Если не задана, используется камера Canvas или `Camera.main`. |
| `useAnchoredPositionForUI` | Для UI двигает `RectTransform.anchoredPosition`, а не world position. |
| `flyDuration` | Длительность полёта. |
| `delayBetweenBonuses` | Задержка между созданием нескольких объектов. |
| `countMultiplier` | Множитель количества созданных объектов. |
| `maxBonusCount` | Максимальное количество объектов за один вызов. |
| `arcStrength` | Сила дуги. |
| `middlePoint` | Положение средней точки дуги от 0 до 1. |
| `multY` | Множитель вертикальной части дуги. |
| `easyStart` / `easyEnd` | Ease первой и второй половины полёта. |
| `startRandomOffset` | Случайный разброс старта. |
| `endRandomOffset` | Случайный разброс цели. |
| `middleRandomOffset` | Случайный разброс средней точки дуги. |
| `rotateDuringFlight` | Вращать объект во время полёта. |
| `rotationDegrees` | Угол вращения за полёт. |
| `setAsLastSibling` | Поднимать созданный UI-объект поверх соседей. |
| `destroyOnComplete` | Уничтожать объект после прилёта. Выключите для ручного пула через `onEnd`. |
| `scaleMult` | Множитель масштаба созданного объекта. |
| `ignoreZ` | Обнулять Z у старта и цели. |
| `useUnscaledTime` | Использовать unscaled time для паузы/меню. |
| `isWorldSpace` | Старое поле совместимости. Если `endSpace = Auto`, `true` трактует цель как `World`. |

## NoCode

- Для простых сцен используйте `Bonus Prefab List` и старые методы `Execute(type, count, start)`.
- Если цель находится на Canvas, задайте `End Space = Canvas`.
- Если цель находится в мире, задайте `End Space = World`.
- Для UnityEvent удобнее выбирать явные методы `PlayByTypeWorldToCanvas`, `PlayByTypeCanvasToCanvas`, `PlayByTypeCanvasToWorld`, `PlayByTypeWorldToWorld`.
- Если список `Bonus Prefab List` меняется во время игры, вызовите `RefreshPrefabCache()`.

## Публичный API

```csharp
AnimationFly.I.PlayByType(
    type: 0,
    bonusCount: 5,
    start: startTransform,
    end: endTransform,
    startSpace: AnimationFlyCoordinateSpace.World,
    endSpace: AnimationFlyCoordinateSpace.Canvas);

AnimationFly.I.Play(
    prefab,
    bonusCount: 5,
    start: startTransform,
    end: endTransform,
    startSpace: AnimationFlyCoordinateSpace.World,
    endSpace: AnimationFlyCoordinateSpace.Canvas,
    parent: canvasContainer,
    onStart: spawned => spawned.SetActive(true),
    onEnd: spawned => Debug.Log("Arrived"));
```

`Auto` определяет Canvas по `RectTransform` или наличию `Canvas` в родителях. Если важна предсказуемость, задавайте `World` / `Canvas` явно.

## См. также

- [UI](./UI.md)
- [Money](../Shop/Money.md)
