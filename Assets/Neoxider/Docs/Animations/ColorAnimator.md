# ColorAnimator

**Namespace:** `Neo.Animations`  
**Путь к файлу:** `Assets/Neoxider/Scripts/Animations/ColorAnimator.cs`

## Описание

Универсальный аниматор для цветов. Предоставляет простой способ анимации цвета с различными типами анимации. Компонент автоматически рассчитывает анимированный цвет каждый кадр и предоставляет его через публичное свойство и события.

## Ключевые особенности

- **Универсальность**: Анимирует любые цвета
- **Простота**: Один компонент — одна анимация цвета
- **События**: UnityEvents для реакции на изменения цвета
- **Управление**: Методы Play/Pause/Stop для контроля анимации
- **Гибкость**: Поддержка всех типов анимации из AnimationType

## Публичные поля

### Animation Settings
- **animationType** (`AnimationType`) — Тип анимации
- **startColor** (`Color`) — Начальный цвет
- **endColor** (`Color`) — Конечный цвет
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
- **OnColorChanged** (`UnityEvent<Color>`) — Вызывается при изменении цвета
- **OnAnimationStarted** (`UnityEvent`) — Вызывается при запуске анимации
- **OnAnimationStopped** (`UnityEvent`) — Вызывается при остановке анимации
- **OnAnimationPaused** (`UnityEvent`) — Вызывается при паузе анимации

## Публичные свойства

### Только для чтения
- **CurrentColor** (`Color`) — Текущий анимированный цвет
- **IsPlaying** (`bool`) — Проигрывается ли анимация
- **IsPaused** (`bool`) — Находится ли анимация на паузе

### Для изменения извне
- **StartColor** (`Color`) — Начальный цвет
- **EndColor** (`Color`) — Конечный цвет
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

### Анимация цвета материала
```csharp
public class MaterialColorAnimator : MonoBehaviour
{
    private ColorAnimator animator;
    private Renderer renderer;
    
    void Start()
    {
        animator = GetComponent<ColorAnimator>();
        renderer = GetComponent<Renderer>();
        animator.OnColorChanged.AddListener(OnColorChanged);
    }
    
    void OnColorChanged(Color color)
    {
        renderer.material.color = color;
    }
}
```

### Анимация цвета UI элемента
```csharp
public class UIColorAnimator : MonoBehaviour
{
    private ColorAnimator animator;
    private Image image;
    
    void Start()
    {
        animator = GetComponent<ColorAnimator>();
        image = GetComponent<Image>();
        animator.OnColorChanged.AddListener(OnColorChanged);
    }
    
    void OnColorChanged(Color color)
    {
        image.color = color;
    }
}
```

### Анимация цвета текста
```csharp
public class TextColorAnimator : MonoBehaviour
{
    private ColorAnimator animator;
    private Text text;
    
    void Start()
    {
        animator = GetComponent<ColorAnimator>();
        text = GetComponent<Text>();
        animator.OnColorChanged.AddListener(OnColorChanged);
    }
    
    void OnColorChanged(Color color)
    {
        text.color = color;
    }
}
```

### Динамическое изменение цветов
```csharp
public class DynamicColorController : MonoBehaviour
{
    public ColorAnimator animator;
    
    void Update()
    {
        if (Input.GetKeyDown(KeyCode.R))
        {
            // Случайные цвета
            animator.StartColor = Random.ColorHSV();
            animator.EndColor = Random.ColorHSV();
            animator.ResetTime();
        }
        
        if (Input.GetKeyDown(KeyCode.G))
        {
            // Градиент от зеленого к красному
            animator.StartColor = Color.green;
            animator.EndColor = Color.red;
            animator.ResetTime();
        }
    }
}
```

## Настройка в инспекторе

1. **Animation Settings**: Выберите тип анимации и настройте начальный/конечный цвета
2. **Noise Settings**: Настройте параметры шума для PerlinNoise
3. **Custom Curve**: Создайте кривую для CustomCurve типа
4. **Events**: Подключите методы для реакции на события
5. **Control**: Настройте автоматический запуск

## Советы по использованию

- Используйте события вместо постоянного опроса `CurrentColor`
- Для сложных цветовых переходов используйте `CustomCurve`
- Применяйте `RandomizeTime()` для разнообразия в группе объектов
- Используйте HSV цвета для более естественных переходов
- Комбинируйте несколько ColorAnimator для сложных эффектов

## Цветовые эффекты

### Радужный эффект
```csharp
// Настройка в инспекторе:
// StartColor: Red (1,0,0,1)
// EndColor: Red (1,0,0,1) 
// AnimationType: SinWave
// AnimationSpeed: 2.0
```

### Пульсирующий эффект
```csharp
// Настройка в инспекторе:
// StartColor: Белый (1,1,1,1)
// EndColor: Целевой цвет
// AnimationType: Pulsing
// AnimationSpeed: 1.0
```

### Естественное мерцание
```csharp
// Настройка в инспекторе:
// StartColor: Базовый цвет
// EndColor: Более яркий цвет
// AnimationType: PerlinNoise
// AnimationSpeed: 0.5
```

## Производительность

- Оптимизирован для использования в Update
- Минимальные вычисления каждый кадр
- События вызываются только при изменении цвета
- Эффективное сравнение цветов с учетом погрешности
- Автоматическая валидация параметров
