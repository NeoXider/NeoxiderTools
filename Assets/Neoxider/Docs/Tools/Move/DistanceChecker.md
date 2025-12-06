# Компонент Distance Checker

## 1. Введение

`DistanceChecker` — оптимизированный компонент для отслеживания расстояния между объектами. Работает как "датчик приближения", вызывая события при входе и выходе из зоны. Использует квадрат расстояния для повышения производительности.

Полезен для активации AI врагов, запуска диалогов, детекции игрока и других механик, основанных на расстоянии.

---

## 2. Описание класса

### DistanceChecker
- **Пространство имен**: `Neo.Tools`
- **Путь к файлу**: `Assets/Neoxider/Scripts/Tools/Move/DistanceChecker.cs`

**Описание**
Компонент измеряет расстояние между двумя объектами и вызывает события при пересечении порога. Использует оптимизированные вычисления через `sqrMagnitude` для экономии ресурсов.

**Ключевые поля**

### Режимы обновления (UpdateMode)
- `EveryFrame`: Проверка каждый кадр (по умолчанию)
- `FixedInterval`: Проверка с фиксированным интервалом (экономия ресурсов)

### Настройки объектов
- `currentObject`: Исходный объект (по умолчанию использует transform компонента)
- `targetObject`: Целевой объект для измерения расстояния

### Настройки расстояния
- `distanceThreshold`: Пороговое расстояние для срабатывания событий
- `updateMode`: Режим обновления
- `updateInterval`: Интервал обновления в секундах (для режима `FixedInterval`)
- `enableContinuousTracking`: Включает непрерывное отслеживание с событием `onDistanceChanged`

### События
- `onApproach`: Вызывается при входе в радиус
- `onDepart`: Вызывается при выходе из радиуса
- `onDistanceChanged`: Вызывается при изменении расстояния (если включен `enableContinuousTracking`)

### Отладка
- `showDebugGizmos`: Визуализация радиуса и линии до цели в Scene view

**Публичные методы (API для кода)**

### Получение информации
- `GetCurrentDistance()`: Возвращает текущее расстояние до цели
- `IsWithinDistance()`: Возвращает true, если цель в пределах порога

### Управление
- `SetTarget(Transform newTarget)`: Сменить целевой объект
- `SetDistanceThreshold(float threshold)`: Изменить пороговое расстояние
- `SetUpdateMode(UpdateMode mode)`: Изменить режим обновления
- `SetUpdateInterval(float interval)`: Установить интервал для FixedInterval режима
- `SetContinuousTracking(bool enabled)`: Включить/выключить непрерывное отслеживание
- `ForceCheck()`: Принудительная проверка дистанции

---

### Примеры использования из кода

```csharp
DistanceChecker checker = GetComponent<DistanceChecker>();

// Сменить цель на ближайшего врага
checker.SetTarget(FindClosestEnemy());

// Увеличить радиус детекции при тревоге
checker.SetDistanceThreshold(alertRadius);

// Оптимизация: переключить на редкую проверку для дальних объектов
if (checker.GetCurrentDistance() > 50f)
{
    checker.SetUpdateMode(DistanceChecker.UpdateMode.FixedInterval);
    checker.SetUpdateInterval(0.5f);  // проверка 2 раза в секунду
}
else
{
    checker.SetUpdateMode(DistanceChecker.UpdateMode.EveryFrame);
}

// Принудительная проверка после телепортации
player.Teleport(newPosition);
checker.ForceCheck();

// Включить отслеживание для UI
checker.SetContinuousTracking(true);
checker.onDistanceChanged.AddListener(distance => 
{
    distanceText.text = $"Расстояние: {distance:F1}м";
});

// Проверка состояния
if (checker.IsWithinDistance())
{
    Debug.Log("Игрок в зоне!");
}
```

---

## 3. Примеры использования

### Активация врага при приближении игрока
```csharp
// На объекте врага
1. Добавить DistanceChecker
2. targetObject = игрок
3. distanceThreshold = 10
4. onApproach → EnemyAI.StartChasing()
5. onDepart → EnemyAI.StopChasing()
```

### Оптимизированная проверка множества объектов
Для множества врагов используйте `FixedInterval`:
```csharp
updateMode = FixedInterval
updateInterval = 0.2f  // проверка 5 раз в секунду
```
Это снижает нагрузку на CPU в 6 раз по сравнению с проверкой каждый кадр.

### Отображение расстояния в UI
```csharp
enableContinuousTracking = true
onDistanceChanged → UpdateDistanceText(float distance)
```

### Детекция нескольких зон
Создайте несколько `DistanceChecker` с разными порогами:
```csharp
DistanceChecker1: threshold = 5  → "Враг атакует"
DistanceChecker2: threshold = 15 → "Враг замечает"
DistanceChecker3: threshold = 30 → "Враг насторожен"
```

---

## 4. Оптимизация

### Производительность
Компонент использует `sqrMagnitude` вместо `Vector3.Distance`, что избегает операции извлечения квадратного корня. Это даёт прирост производительности ~20-30%.

### Режимы обновления
- **EveryFrame**: Используйте для критичных проверок (атака игрока, ближний бой)
- **FixedInterval**: Используйте для некритичных проверок (AI патрулирование, фоновые NPC)

### Визуализация
При включении `showDebugGizmos` в Scene view отображается:
- Сфера радиуса (зелёная если цель внутри, красная если снаружи)
- Жёлтая линия от источника до цели
