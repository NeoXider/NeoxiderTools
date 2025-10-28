# FloatAnimator

**Namespace:** `Neo.Animations`  
**Путь к файлу:** `Assets/Neoxider/Scripts/Animations/FloatAnimator.cs`

## Описание

Универсальный аниматор для float значений. Предоставляет простой способ анимации любого числового значения с различными типами анимации. Компонент автоматически рассчитывает анимированное значение каждый кадр и предоставляет его через публичное свойство и события.

## Ключевые особенности

- **Универсальность**: Анимирует любые float значения
- **Простота**: Один компонент — одна анимация
- **События**: UnityEvents для реакции на изменения
- **Управление**: Методы Play/Pause/Stop для контроля анимации
- **Гибкость**: Поддержка всех типов анимации из AnimationType

## Публичные поля

### Animation Settings
- **animationType** (`AnimationType`) — Тип анимации
- **minValue** (`float`) — Минимальное значение
- **maxValue** (`float`) — Максимальное значение
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
- **OnValueChanged** (`UnityEvent<float>`) — Вызывается при изменении значения
- **OnAnimationStarted** (`UnityEvent`) — Вызывается при запуске анимации
- **OnAnimationStopped** (`UnityEvent`) — Вызывается при остановке анимации
- **OnAnimationPaused** (`UnityEvent`) — Вызывается при паузе анимации

## Публичные свойства

### Только для чтения
- **CurrentValue** (`float`) — Текущее анимированное значение
- **IsPlaying** (`bool`) — Проигрывается ли анимация
- **IsPaused** (`bool`) — Находится ли анимация на паузе

### Для изменения извне
- **MinValue** (`float`) — Минимальное значение
- **MaxValue** (`float`) — Максимальное значение
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

### Простая анимация масштаба
```csharp
public class ScaleAnimator : MonoBehaviour
{
    private FloatAnimator animator;
    
    void Start()
    {
        animator = GetComponent<FloatAnimator>();
        animator.OnValueChanged.AddListener(OnScaleChanged);
    }
    
    void OnScaleChanged(float scale)
    {
        transform.localScale = Vector3.one * scale;
    }
}
```

### Анимация прозрачности
```csharp
public class AlphaAnimator : MonoBehaviour
{
    private FloatAnimator animator;
    private Renderer renderer;
    
    void Start()
    {
        animator = GetComponent<FloatAnimator>();
        renderer = GetComponent<Renderer>();
        animator.OnValueChanged.AddListener(OnAlphaChanged);
    }
    
    void OnAlphaChanged(float alpha)
    {
        Color color = renderer.material.color;
        color.a = alpha;
        renderer.material.color = color;
    }
}
```

### Управление анимацией из кода
```csharp
public class AnimationController : MonoBehaviour
{
    public FloatAnimator animator;
    
    void Update()
    {
        if (Input.GetKeyDown(KeyCode.Space))
        {
            if (animator.IsPlaying)
                animator.Pause();
            else
                animator.Resume();
        }
        
        if (Input.GetKeyDown(KeyCode.R))
        {
            animator.Stop();
            animator.Play();
        }
    }
}
```

## Настройка в инспекторе

1. **Animation Settings**: Выберите тип анимации и настройте диапазон значений
2. **Noise Settings**: Настройте параметры шума для PerlinNoise
3. **Custom Curve**: Создайте кривую для CustomCurve типа
4. **Events**: Подключите методы для реакции на события
5. **Control**: Настройте автоматический запуск

## Советы по использованию

- Используйте события вместо постоянного опроса `CurrentValue`
- Для сложных эффектов комбинируйте несколько FloatAnimator
- Используйте `animationSpeed = 0` для отключения анимации
- Применяйте `RandomizeTime()` для разнообразия в группе объектов
- Используйте `CustomCurve` для точного контроля над формой анимации

## Производительность

- Оптимизирован для использования в Update
- Минимальные вычисления каждый кадр
- События вызываются только при изменении значений
- Автоматическая валидация параметров
