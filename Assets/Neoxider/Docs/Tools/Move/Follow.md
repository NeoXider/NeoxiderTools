# Компонент Follow

## 1. Введение

`Follow` — профессиональный компонент для следования одного объекта за другим с поддержкой множества режимов сглаживания, мёртвой зоны, и ограничений позиции/вращения. Полностью переработан с исправлением критических багов и улучшенной архитектурой.

Используется для камер, следующих за игроком, питомцев, самонаводящихся объектов, и других механик следования.

---

## 2. Описание класса

### Follow
- **Пространство имен**: `Neo.Tools`
- **Путь к файлу**: `Assets/Neoxider/Scripts/Tools/Move/Follow.cs`

**Описание**
Универсальный компонент следования с профессиональной реализацией сглаживания. Размещается на объекте, который должен следовать за целью. Работает в `LateUpdate` для предотвращения дрожания.

---

## 3. Режимы и настройки

### Режимы следования (FollowMode)
- `ThreeD`: Полное 3D следование с учётом всех осей
- `TwoD`: 2D следование, игнорирует ось Z для позиции

### Режимы сглаживания (SmoothMode)
- `None`: Мгновенное перемещение без сглаживания
- `MoveTowards`: Постоянная скорость движения (по умолчанию)
- `Lerp`: Линейная интерполяция (правильная реализация с Clamp01)
- `SmoothDamp`: Плавное затухание через `Vector3.SmoothDamp`
- `Exponential`: Экспоненциальное затухание для естественного движения

---

## 4. Настройки позиции

### Основные параметры
- `target`: Цель для следования
- `followPosition`: Включить/выключить следование за позицией
- `positionSmoothMode`: Режим сглаживания для позиции
- `positionSpeed`: Скорость сглаживания (значение зависит от режима)
- `offset`: Смещение относительно цели

### Deadzone (мёртвая зона)
- `deadzone.enabled`: Включить мёртвую зону
- `deadzone.radius`: Радиус зоны, внутри которой камера не двигается

**Как работает**: Камера начинает двигаться только когда цель выходит за пределы радиуса. Это создаёт стабильную камеру без "дрожания" при небольших движениях игрока.

### Distance Control (управление дистанцией)
- `distanceControl.activationDistance`: Минимальное расстояние для начала следования (0 = нет ограничений)
- `distanceControl.stoppingDistance`: Расстояние, на котором камера останавливается, не доходя до цели (0 = доходит до offset)

**Как работает**:
- **Activation Distance**: Камера начинает следовать только когда расстояние до цели больше этого значения
- **Stopping Distance**: Камера останавливается, не доходя указанное расстояние до целевой позиции (полезно для AI, патрулирования)

### Ограничения позиции
- `limitX`, `limitY`, `limitZ`: Структуры `AxisLimit` для ограничения по каждой оси
  - `enabled`: Включить ограничение
  - `min`, `max`: Минимальное и максимальное значение

### События
- `onStartFollowing`: Вызывается когда начинается следование (выход за пределы activationDistance)
- `onStopFollowing`: Вызывается когда следование останавливается (вход в зону activationDistance)

---

## 5. Настройки вращения

### Основные параметры
- `followRotation`: Включить/выключить следование за вращением
- `rotationSmoothMode`: Режим сглаживания для вращения
- `rotationSpeed`: Скорость сглаживания вращения (по умолчанию 180 градусов/сек для MoveTowards)
- `rotationOffset3D`: Дополнительный поворот для 3D (Euler углы)
- `rotationOffset2D`: Дополнительный угол для 2D (градусы)

### Ограничения вращения
- **3D режим**: `rotationLimitX`, `rotationLimitY` для ограничения углов Euler
- **2D режим**: `rotationLimitZ` для ограничения угла поворота вокруг оси Z

---

## 6. Примеры использования

### Камера за игроком (3D)
```csharp
followMode = ThreeD
followPosition = true
positionSmoothMode = MoveTowards
positionSpeed = 10
offset = (0, 5, -10)

followRotation = true
rotationSmoothMode = MoveTowards
rotationSpeed = 180  // по умолчанию
```

### Камера с мёртвой зоной (2D platformer)
```csharp
followMode = TwoD
followPosition = true
positionSmoothMode = Exponential
positionSpeed = 5
offset = (0, 2, -10)

deadzone.enabled = true
deadzone.radius = 2  // камера не двигается пока игрок в радиусе 2 юнитов
```

### Самонаводящаяся ракета
```csharp
followMode = ThreeD
followPosition = true
positionSmoothMode = Lerp
positionSpeed = 10

followRotation = true
rotationSmoothMode = Exponential
rotationSpeed = 8
```

### Камера с ограничением по уровню
```csharp
followPosition = true
limitX.enabled = true
limitX.min = -50
limitX.max = 50

limitY.enabled = true
limitY.min = 0
limitY.max = 20
```

### AI противник, преследующий игрока
```csharp
followMode = ThreeD
followPosition = true
positionSmoothMode = MoveTowards
positionSpeed = 5

distanceControl.activationDistance = 10  // начинает преследование с 10 юнитов
distanceControl.stoppingDistance = 2     // останавливается на 2 юнита от игрока

onStartFollowing → StartAttackAnimation()
onStopFollowing → StopAttackAnimation()
```

### Питомец, следующий за игроком
```csharp
followMode = ThreeD
followPosition = true
positionSmoothMode = SmoothDamp
positionSpeed = 3
offset = (-2, 0, -2)  // позади и сбоку

distanceControl.activationDistance = 3  // начинает догонять с 3 юнитов
distanceControl.stoppingDistance = 1    // останавливается в 1 юните

deadzone.enabled = true
deadzone.radius = 1.5  // не дёргается при небольших движениях
```

---

## 7. Режимы сглаживания - подробно

### MoveTowards (по умолчанию, постоянная скорость)
```csharp
positionSpeed = 10  // юнитов в секунду
```
- Постоянная скорость движения независимо от расстояния
- `Vector3.MoveTowards` перемещает объект с фиксированной скоростью
- Идеально для камер, простого следования, механик "догнать игрока"
- Предсказуемое поведение: скорость всегда одинакова

### Lerp (плавное замедление)
```csharp
positionSpeed = 5  // скорость от 1 до 10
```
- Простой и предсказуемый
- Исправлена критическая ошибка: теперь использует `Clamp01` для предотвращения экстраполяции
- Подходит для камер и UI элементов

### SmoothDamp (самый плавный)
```csharp
positionSpeed = 3  // время затухания: 1/speed секунд
```
- Наиболее плавное и естественное движение
- Автоматически замедляется при приближении к цели
- Идеально для камер, следующих за игроком

### Exponential (профессиональный выбор)
```csharp
positionSpeed = 5  // скорость от 3 до 8
```
- Экспоненциальное затухание для естественной физики
- Независим от framerate
- Используется в AAA играх

### None (для специальных случаев)
- Мгновенная телепортация без сглаживания
- Используйте только если нужна жёсткая привязка

---

## 8. Публичные методы (API для кода)

### Управление целью
- `SetTarget(Transform newTarget)`: Устанавливает новую цель
- `GetTarget()`: Возвращает текущую цель
- `TeleportToTarget()`: Мгновенная телепортация к цели (без сглаживания)

### Управление следованием
- `SetFollowPosition(bool enabled)`: Включить/выключить следование за позицией
- `SetFollowRotation(bool enabled)`: Включить/выключить следование за вращением
- `IsFollowing()`: Возвращает true если сейчас следует

### Управление скоростью и режимами
- `SetPositionSpeed(float speed)`: Установить скорость движения
- `SetRotationSpeed(float speed)`: Установить скорость вращения
- `SetPositionSmoothMode(SmoothMode mode)`: Изменить режим сглаживания позиции
- `SetRotationSmoothMode(SmoothMode mode)`: Изменить режим сглаживания вращения

### Управление дистанцией
- `SetActivationDistance(float distance)`: Установить дистанцию активации
- `SetStoppingDistance(float distance)`: Установить дистанцию остановки
- `GetDistanceToTarget()`: Получить текущее расстояние до цели

### Deadzone
- `SetDeadzoneEnabled(bool enabled)`: Включить/выключить deadzone
- `SetDeadzoneRadius(float radius)`: Установить радиус deadzone

### Offset
- `SetOffset(Vector3 newOffset)`: Изменить смещение относительно цели

---

### Примеры использования из кода

```csharp
// Получить компонент
Follow follow = GetComponent<Follow>();

// Сменить цель следования
follow.SetTarget(newPlayer);

// Увеличить скорость в 2 раза при беге
follow.SetPositionSpeed(normalSpeed * 2f);

// Переключить на плавное следование
follow.SetPositionSmoothMode(Follow.SmoothMode.SmoothDamp);

// AI: начать преследование
follow.SetActivationDistance(0f);  // всегда следует
follow.SetStoppingDistance(2f);     // останавливается на 2 юнитах

// Мгновенно переместить камеру к игроку
follow.TeleportToTarget();

// Проверка состояния
if (follow.IsFollowing())
{
    Debug.Log("Камера движется за целью");
}

// Динамическая настройка deadzone
if (playerRunning)
{
    follow.SetDeadzoneRadius(0.5f);  // меньше deadzone при беге
}
else
{
    follow.SetDeadzoneRadius(2f);    // больше при ходьбе
}
```

---

## 9. Отладка

### Визуализация (showDebugGizmos)
При включении отображает:
- **Зелёная сфера**: радиус deadzone
- **Жёлтая сфера**: радиус activationDistance (вокруг текущей позиции)
- **Красная сфера**: радиус stoppingDistance (вокруг целевой позиции)
- **Голубая линия**: связь между объектом и целью (с учётом offset)

---

## 10. Технические детали

### Исправленные баги
- ❌ **Было**: `Lerp` с `smoothSpeed * Time.smoothDeltaTime` мог давать значения > 1 (экстраполяция)
- ✅ **Стало**: `Lerp` с `Clamp01(smoothSpeed * Time.deltaTime)` (правильная интерполяция)

- ❌ **Было**: Использование нестабильного `Time.smoothDeltaTime`
- ✅ **Стало**: Использование стабильного `Time.deltaTime`

- ❌ **Было**: Проверка лимитов через `Vector2.zero` не работала для реальных нулевых границ
- ✅ **Стало**: Явная проверка `limit.enabled`

### Производительность
- Кеширование ссылок на компоненты
- Раннее прерывание при отсутствии цели
- Оптимизированные вычисления расстояний
