# LightAnimator

**Namespace:** `Neo.Tools.View`  
**Путь к файлу:** `Assets/Neoxider/Scripts/Tools/View/LightAnimator.cs`

## Описание

Компонент для анимации источников света (Light и Light2D). Поддерживает различные типы анимации интенсивности и цвета. Автоматически определяет тип источника света и применяет анимацию соответствующим образом.

## Ключевые особенности

- **Универсальность**: Работает с Light и Light2D компонентами
- **Автоопределение**: Автоматически находит и использует доступный источник света
- **События**: UnityEvents для реакции на изменения интенсивности и цвета
- **Управление**: Методы Play/Pause/Stop для контроля анимации
- **Гибкость**: Поддержка всех типов анимации из AnimationType

## Публичные поля

### Animation Settings
- **animationType** (`AnimationType`) — Тип анимации
- **minIntensity** (`float`) — Минимальная интенсивность
- **maxIntensity** (`float`) — Максимальная интенсивность
- **animationSpeed** (`float`) — Скорость анимации (0 = анимация отключена)

### Noise Settings
- **noiseScale** (`float`) — Масштаб шума для PerlinNoise
- **use2DNoise** (`bool`) — Использовать 2D шум вместо 1D
- **noiseOffset** (`Vector2`) — Дополнительное смещение шума

### Color Settings
- **changeColor** (`bool`) — Изменять ли цвет света
- **targetColor** (`Color`) — Целевой цвет
- **colorBlendSpeed** (`float`) — Скорость смешивания цветов

### Custom Curve
- **customCurve** (`AnimationCurve`) — Пользовательская кривая для CustomCurve типа

### Control
- **playOnStart** (`bool`) — Автоматически запускать анимацию при старте

### Debug Settings
- **enableDebugging** (`bool`) — Включить отладочные сообщения

### Events
- **OnIntensityChanged** (`UnityEvent<float>`) — Вызывается при изменении интенсивности
- **OnColorChanged** (`UnityEvent<Color>`) — Вызывается при изменении цвета
- **OnAnimationStarted** (`UnityEvent`) — Вызывается при запуске анимации
- **OnAnimationStopped** (`UnityEvent`) — Вызывается при остановке анимации
- **OnAnimationPaused** (`UnityEvent`) — Вызывается при паузе анимации

## Публичные свойства

### Только для чтения
- **CurrentIntensity** (`float`) — Текущая интенсивность света
- **CurrentColor** (`Color`) — Текущий цвет света
- **IsPlaying** (`bool`) — Проигрывается ли анимация
- **IsPaused** (`bool`) — Находится ли анимация на паузе

### Для изменения извне
- **MinIntensity** (`float`) — Минимальная интенсивность
- **MaxIntensity** (`float`) — Максимальная интенсивность
- **AnimationSpeed** (`float`) — Скорость анимации
- **AnimationType** (`AnimationType`) — Тип анимации
- **TargetColor** (`Color`) — Целевой цвет

## Публичные методы

### Play()
Запустить анимацию. Устанавливает `IsPlaying = true` и `IsPaused = false`.

### Stop()
Остановить анимацию. Устанавливает `IsPlaying = false` и `IsPaused = false`.

### Pause()
Поставить анимацию на паузу. Работает только если анимация проигрывается.

### Resume()
Снять с паузы. Работает только если анимация на паузе.

### ResetToOriginal()
Сбросить к исходным значениям света (интенсивность и цвет).

### ResetTime()
Сбросить время анимации к нулю.

### RandomizeTime()
Установить случайное начальное время анимации.

## Примеры использования

### Простая анимация света
```csharp
public class LightController : MonoBehaviour
{
    private LightAnimator animator;
    
    void Start()
    {
        animator = GetComponent<LightAnimator>();
        animator.OnIntensityChanged.AddListener(OnIntensityChanged);
        animator.OnColorChanged.AddListener(OnColorChanged);
    }
    
    void OnIntensityChanged(float intensity)
    {
        Debug.Log($"Light intensity changed to: {intensity}");
    }
    
    void OnColorChanged(Color color)
    {
        Debug.Log($"Light color changed to: {color}");
    }
}
```

### Управление анимацией из кода
```csharp
public class LightManager : MonoBehaviour
{
    public LightAnimator[] lights;
    
    void Update()
    {
        if (Input.GetKeyDown(KeyCode.Space))
        {
            foreach (var light in lights)
            {
                if (light.IsPlaying)
                    light.Pause();
                else
                    light.Resume();
            }
        }
        
        if (Input.GetKeyDown(KeyCode.R))
        {
            foreach (var light in lights)
            {
                light.Stop();
                light.Play();
            }
        }
    }
}
```

### Динамическое изменение параметров
```csharp
public class DynamicLightController : MonoBehaviour
{
    public LightAnimator animator;
    
    void Update()
    {
        if (Input.GetKeyDown(KeyCode.Alpha1))
        {
            // Мерцание
            animator.AnimationType = AnimationType.RandomFlicker;
            animator.MinIntensity = 0.1f;
            animator.MaxIntensity = 2.0f;
            animator.AnimationSpeed = 5.0f;
        }
        
        if (Input.GetKeyDown(KeyCode.Alpha2))
        {
            // Пульсация
            animator.AnimationType = AnimationType.Pulsing;
            animator.MinIntensity = 0.5f;
            animator.MaxIntensity = 1.5f;
            animator.AnimationSpeed = 2.0f;
        }
        
        if (Input.GetKeyDown(KeyCode.Alpha3))
        {
            // Естественное мерцание
            animator.AnimationType = AnimationType.PerlinNoise;
            animator.MinIntensity = 0.3f;
            animator.MaxIntensity = 1.8f;
            animator.AnimationSpeed = 1.0f;
        }
    }
}
```

## Настройка в инспекторе

1. **Animation Settings**: Выберите тип анимации и настройте диапазон интенсивности
2. **Noise Settings**: Настройте параметры шума для PerlinNoise
3. **Color Settings**: Включите изменение цвета и выберите целевой цвет
4. **Custom Curve**: Создайте кривую для CustomCurve типа
5. **Events**: Подключите методы для реакции на события
6. **Control**: Настройте автоматический запуск

## Типичные применения

### Мерцание свечи
- **AnimationType**: PerlinNoise
- **MinIntensity**: 0.3
- **MaxIntensity**: 1.2
- **AnimationSpeed**: 0.8
- **NoiseScale**: 2.0

### Пульсирующий неоновый свет
- **AnimationType**: Pulsing
- **MinIntensity**: 0.5
- **MaxIntensity**: 2.0
- **AnimationSpeed**: 3.0
- **ChangeColor**: true

### Дрожащий свет
- **AnimationType**: RandomFlicker
- **MinIntensity**: 0.1
- **MaxIntensity**: 1.5
- **AnimationSpeed**: 8.0

### Плавное затухание
- **AnimationType**: Exponential
- **MinIntensity**: 0.0
- **MaxIntensity**: 1.0
- **AnimationSpeed**: 0.5

## Советы по использованию

- Используйте события вместо постоянного опроса значений
- Для сложных эффектов комбинируйте несколько LightAnimator
- Используйте `animationSpeed = 0` для отключения анимации
- Применяйте `RandomizeTime()` для разнообразия в группе источников света
- Используйте `CustomCurve` для точного контроля над формой анимации

## Поддерживаемые компоненты

- **Light** (стандартный Unity Light)
- **Light2D** (Universal Render Pipeline)

Компонент автоматически определяет доступный тип источника света и использует соответствующий интерфейс доступа.

## Производительность

- Оптимизирован для использования в Update
- Минимальные вычисления каждый кадр
- События вызываются только при изменении значений
- Эффективное сравнение значений с учетом погрешности
- Автоматическая валидация параметров