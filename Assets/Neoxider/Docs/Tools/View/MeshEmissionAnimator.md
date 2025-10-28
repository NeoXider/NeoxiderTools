# MeshEmission

**Namespace:** `Neo.Tools.View`  
**Путь к файлу:** `Assets/Neoxider/Scripts/Tools/View/MeshEmission.cs`

## Описание

Компонент для синхронизации эмиссии меша с источником света. Копирует интенсивность и цвет от Light компонента в реальном времени, применяя дополнительные настройки синхронизации. Идеально подходит для создания эффектов, где эмиссия материала должна следовать за источником света.

## Ключевые особенности

- **Синхронизация**: Копирует интенсивность и цвет от Light компонента
- **Гибкость**: Настройки копирования (интенсивность, цвет, множители)
- **Задержка**: Возможность добавить задержку синхронизации
- **Сглаживание**: Кривая для плавной синхронизации
- **Автопоиск**: Автоматический поиск источника света
- **События**: UnityEvents для реакции на изменения эмиссии

## Публичные поля

### Sync Mode
- **syncWithLight** (`bool`) — Включить синхронизацию с источником света
- **targetLight** (`Light`) — Источник света для синхронизации (может быть на другом объекте)

### Sync Settings
- **syncIntensity** (`bool`) — Синхронизировать интенсивность
- **syncColor** (`bool`) — Синхронизировать цвет
- **intensityMultiplier** (`float`) — Множитель интенсивности (1.0 = точно как у света)
- **syncDelay** (`float`) — Задержка синхронизации в секундах
- **syncCurve** (`AnimationCurve`) — Кривая для сглаживания синхронизации

### Control
- **playOnStart** (`bool`) — Автоматически запускать синхронизацию при старте

### Debug Settings
- **enableDebugging** (`bool`) — Включить отладочные сообщения

### Events
- **OnIntensityChanged** (`UnityEvent<float>`) — Вызывается при изменении интенсивности эмиссии
- **OnColorChanged** (`UnityEvent<Color>`) — Вызывается при изменении цвета эмиссии
- **OnAnimationStarted** (`UnityEvent`) — Вызывается при запуске синхронизации
- **OnAnimationStopped** (`UnityEvent`) — Вызывается при остановке синхронизации
- **OnAnimationPaused** (`UnityEvent`) — Вызывается при паузе синхронизации

## Публичные свойства

### Только для чтения
- **CurrentIntensity** (`float`) — Текущая интенсивность эмиссии
- **CurrentColor** (`Color`) — Текущий цвет эмиссии
- **IsPlaying** (`bool`) — Проигрывается ли синхронизация
- **IsPaused** (`bool`) — Находится ли синхронизация на паузе

### Для изменения извне
- **SyncWithLight** (`bool`) — Синхронизируется ли с источником света
- **TargetLight** (`Light`) — Источник света для синхронизации
- **IntensityMultiplier** (`float`) — Множитель интенсивности

## Публичные методы

### Play()
Запустить синхронизацию. Устанавливает `IsPlaying = true` и `IsPaused = false`.

### Stop()
Остановить синхронизацию. Устанавливает `IsPlaying = false` и `IsPaused = false`.

### Pause()
Поставить синхронизацию на паузу. Работает только если синхронизация проигрывается.

### Resume()
Снять с паузы. Работает только если синхронизация на паузе.

### ResetToOriginal()
Сбросить к исходным значениям эмиссии.

### ResetSyncTime()
Сбросить время синхронизации к нулю.

### FindAndAttachLight()
Найти и привязать источник света на том же объекте или дочерних.

## Примеры использования

### Простая синхронизация эмиссии
```csharp
public class EmissionController : MonoBehaviour
{
    private MeshEmissionAnimator animator;
    
    void Start()
    {
        animator = GetComponent<MeshEmissionAnimator>();
        animator.OnIntensityChanged.AddListener(OnIntensityChanged);
        animator.OnColorChanged.AddListener(OnColorChanged);
    }
    
    void OnIntensityChanged(float intensity)
    {
        Debug.Log($"Emission intensity changed to: {intensity}");
    }
    
    void OnColorChanged(Color color)
    {
        Debug.Log($"Emission color changed to: {color}");
    }
}
```

### Управление синхронизацией из кода
```csharp
public class EmissionManager : MonoBehaviour
{
    public MeshEmissionAnimator[] emissionAnimators;
    
    void Update()
    {
        if (Input.GetKeyDown(KeyCode.Space))
        {
            foreach (var animator in emissionAnimators)
            {
                if (animator.IsPlaying)
                    animator.Pause();
                else
                    animator.Resume();
            }
        }
        
        if (Input.GetKeyDown(KeyCode.R))
        {
            foreach (var animator in emissionAnimators)
            {
                animator.Stop();
                animator.Play();
            }
        }
    }
}
```

### Динамическое изменение параметров синхронизации
```csharp
public class DynamicEmissionController : MonoBehaviour
{
    public MeshEmissionAnimator animator;
    
    void Update()
    {
        if (Input.GetKeyDown(KeyCode.Alpha1))
        {
            // Синхронизировать только интенсивность
            animator.syncIntensity = true;
            animator.syncColor = false;
            animator.intensityMultiplier = 1.0f;
        }
        
        if (Input.GetKeyDown(KeyCode.Alpha2))
        {
            // Синхронизировать только цвет
            animator.syncIntensity = false;
            animator.syncColor = true;
        }
        
        if (Input.GetKeyDown(KeyCode.Alpha3))
        {
            // Синхронизировать все с множителем
            animator.syncIntensity = true;
            animator.syncColor = true;
            animator.intensityMultiplier = 2.0f; // Эмиссия в 2 раза ярче света
        }
    }
}
```

### Автопоиск источника света
```csharp
public class AutoLightFinder : MonoBehaviour
{
    public MeshEmissionAnimator animator;
    
    void Start()
    {
        // Автоматически найти источник света
        animator.FindAndAttachLight();
        
        if (animator.TargetLight != null)
        {
            Debug.Log($"Found light: {animator.TargetLight.gameObject.name}");
        }
        else
        {
            Debug.LogWarning("No light found!");
        }
    }
}
```

## Настройка в инспекторе

1. **Sync Mode**: Включите синхронизацию и назначьте источник света
2. **Sync Settings**: Настройте что синхронизировать и с какими параметрами
3. **Events**: Подключите методы для реакции на события
4. **Control**: Настройте автоматический запуск

## Типичные применения

### Светящийся объект рядом с лампой
- **syncWithLight**: true
- **syncIntensity**: true
- **syncColor**: true
- **intensityMultiplier**: 0.8 (немного тусклее света)

### Эмиссия как отражение света
- **syncWithLight**: true
- **syncIntensity**: true
- **syncColor**: false (сохранить исходный цвет)
- **intensityMultiplier**: 0.5

### Задержанная реакция на свет
- **syncWithLight**: true
- **syncDelay**: 0.5 (задержка 0.5 секунды)
- **syncCurve**: AnimationCurve.EaseInOut(0, 0, 1, 1)

### Эмиссия ярче света
- **syncWithLight**: true
- **syncIntensity**: true
- **intensityMultiplier**: 2.0 (в 2 раза ярче)

## Советы по использованию

- Используйте события вместо постоянного опроса значений
- Применяйте `intensityMultiplier` для настройки яркости эмиссии относительно света
- Используйте `syncDelay` и `syncCurve` для создания эффектов задержки
- Применяйте `FindAndAttachLight()` для автоматического поиска источника света
- Комбинируйте несколько MeshEmissionAnimator для сложных эффектов

## Требования

- **MeshRenderer**: Компонент должен быть на том же объекте
- **Material**: Материал должен поддерживать эмиссию (ключ `_EMISSION`)
- **Light**: Источник света для синхронизации (может быть на другом объекте)

## Производительность

- Оптимизирован для использования в Update
- Минимальные вычисления каждый кадр
- События вызываются только при изменении значений
- Эффективное сравнение значений с учетом погрешности
- Автоматическая валидация параметров
