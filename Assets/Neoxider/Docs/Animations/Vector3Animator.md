# Vector3Animator

**Namespace:** `Neo.Animations`  
**Путь к файлу:** `Assets/Neoxider/Scripts/Animations/Vector3Animator.cs`

## Описание

Универсальный аниматор для Vector3 значений. Предоставляет простой способ анимации позиции, масштаба, поворота и других Vector3 параметров с различными типами анимации. Компонент автоматически рассчитывает анимированный вектор каждый кадр и предоставляет его через публичное свойство и события.

## Ключевые особенности

- **Универсальность**: Анимирует любые Vector3 значения
- **Простота**: Один компонент — одна анимация вектора
- **События**: UnityEvents для реакции на изменения вектора
- **Управление**: Методы Play/Pause/Stop для контроля анимации
- **Гибкость**: Поддержка всех типов анимации из AnimationType

## Публичные поля

### Animation Settings
- **animationType** (`AnimationType`) — Тип анимации
- **startVector** (`Vector3`) — Начальный вектор
- **endVector** (`Vector3`) — Конечный вектор
- **animationSpeed** (`float`) — Скорость анимации (0 = анимация отключена)

### Noise Settings
- **noiseScale** (`float`) — Масштаб шума для PerlinNoise
- **use2DNoise** (`bool`) — Использовать 2D шум вместо 1D
- **noiseOffset** (`Vector2`) — Дополнительное смещение шума

### Custom Curve
- **customCurve** (`AnimationCurve`) — Пользовательская кривая для CustomCurve типа

### Control
- **playOnStart** (`bool`) — Автоматически запускать анимацию при старте

### Events
- **OnVectorChanged** (`UnityEvent<Vector3>`) — Вызывается при изменении вектора
- **OnAnimationStarted** (`UnityEvent`) — Вызывается при запуске анимации
- **OnAnimationStopped** (`UnityEvent`) — Вызывается при остановке анимации
- **OnAnimationPaused** (`UnityEvent`) — Вызывается при паузе анимации

## Публичные свойства

### Только для чтения
- **CurrentVector** (`Vector3`) — Текущий анимированный вектор
- **IsPlaying** (`bool`) — Проигрывается ли анимация
- **IsPaused** (`bool`) — Находится ли анимация на паузе

### Для изменения извне
- **StartVector** (`Vector3`) — Начальный вектор
- **EndVector** (`Vector3`) — Конечный вектор
- **AnimationSpeed** (`float`) — Скорость анимации
- **AnimationType** (`AnimationType`) — Тип анимации

## Публичные методы

### Play()
Запустить анимацию. Устанавливает `IsPlaying = true` и `IsPaused = false`.

### Stop()
Остановить анимацию. Устанавливает `IsPlaying = false` и `IsPaused = false`.

### Pause()
Поставить анимацию на паузу. Работает только если анимация проигрывается.

### Resume()
Снять с паузы. Работает только если анимация на паузе.

### ResetTime()
Сбросить время анимации к нулю.

### RandomizeTime()
Установить случайное начальное время анимации.

## Примеры использования

### Анимация позиции
```csharp
public class PositionAnimator : MonoBehaviour
{
    private Vector3Animator animator;
    
    void Start()
    {
        animator = GetComponent<Vector3Animator>();
        animator.OnVectorChanged.AddListener(OnPositionChanged);
    }
    
    void OnPositionChanged(Vector3 position)
    {
        transform.position = position;
    }
}
```

### Анимация масштаба
```csharp
public class ScaleAnimator : MonoBehaviour
{
    private Vector3Animator animator;
    
    void Start()
    {
        animator = GetComponent<Vector3Animator>();
        animator.OnVectorChanged.AddListener(OnScaleChanged);
    }
    
    void OnScaleChanged(Vector3 scale)
    {
        transform.localScale = scale;
    }
}
```

### Анимация поворота
```csharp
public class RotationAnimator : MonoBehaviour
{
    private Vector3Animator animator;
    
    void Start()
    {
        animator = GetComponent<Vector3Animator>();
        animator.OnVectorChanged.AddListener(OnRotationChanged);
    }
    
    void OnRotationChanged(Vector3 rotation)
    {
        transform.rotation = Quaternion.Euler(rotation);
    }
}
```

### Анимация смещения материала
```csharp
public class MaterialOffsetAnimator : MonoBehaviour
{
    private Vector3Animator animator;
    private Renderer renderer;
    
    void Start()
    {
        animator = GetComponent<Vector3Animator>();
        renderer = GetComponent<Renderer>();
        animator.OnVectorChanged.AddListener(OnOffsetChanged);
    }
    
    void OnOffsetChanged(Vector3 offset)
    {
        renderer.material.mainTextureOffset = new Vector2(offset.x, offset.y);
    }
}
```

### Комбинированная анимация
```csharp
public class MultiVectorAnimator : MonoBehaviour
{
    public Vector3Animator positionAnimator;
    public Vector3Animator scaleAnimator;
    public Vector3Animator rotationAnimator;
    
    void Start()
    {
        positionAnimator.OnVectorChanged.AddListener(pos => transform.position = pos);
        scaleAnimator.OnVectorChanged.AddListener(scale => transform.localScale = scale);
        rotationAnimator.OnVectorChanged.AddListener(rot => transform.rotation = Quaternion.Euler(rot));
    }
    
    void Update()
    {
        if (Input.GetKeyDown(KeyCode.Space))
        {
            // Запустить все анимации одновременно
            positionAnimator.Play();
            scaleAnimator.Play();
            rotationAnimator.Play();
        }
    }
}
```

## Настройка в инспекторе

1. **Animation Settings**: Выберите тип анимации и настройте начальный/конечный векторы
2. **Noise Settings**: Настройте параметры шума для PerlinNoise
3. **Custom Curve**: Создайте кривую для CustomCurve типа
4. **Events**: Подключите методы для реакции на события
5. **Control**: Настройте автоматический запуск

## Типичные применения

### Движение объектов
- **StartVector**: Начальная позиция
- **EndVector**: Конечная позиция
- **AnimationType**: SmoothTransition или SinWave

### Пульсирующий масштаб
- **StartVector**: Минимальный масштаб (например, 0.8, 0.8, 0.8)
- **EndVector**: Максимальный масштаб (например, 1.2, 1.2, 1.2)
- **AnimationType**: Pulsing

### Вращение
- **StartVector**: Начальный угол (например, 0, 0, 0)
- **EndVector**: Конечный угол (например, 0, 360, 0)
- **AnimationType**: SinWave или SmoothTransition

### Дрожание
- **StartVector**: Центральная позиция
- **EndVector**: Позиция с небольшим смещением
- **AnimationType**: PerlinNoise

## Советы по использованию

- Используйте события вместо постоянного опроса `CurrentVector`
- Для сложных движений используйте `CustomCurve`
- Применяйте `RandomizeTime()` для разнообразия в группе объектов
- Комбинируйте несколько Vector3Animator для сложных эффектов
- Используйте разные типы анимации для разных осей

## Производительность

- Оптимизирован для использования в Update
- Минимальные вычисления каждый кадр
- События вызываются только при изменении вектора
- Эффективное сравнение векторов с учетом погрешности
- Автоматическая валидация параметров
