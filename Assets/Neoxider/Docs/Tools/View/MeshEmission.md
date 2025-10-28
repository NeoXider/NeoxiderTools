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

### Color Enhancement
- **whiteThreshold** (`float`) — Интенсивность, при которой начинается усиление интенсивности для осветления
- **whiteRange** (`float`) — Диапазон усиления интенсивности (от whiteThreshold до whiteThreshold + whiteRange)

### Visibility
- **emissionCutoff** (`float`) — Порог, ниже которого эмиссия полностью отключается (0.3 по умолчанию, диапазон 0-5)

## Публичные свойства

### Только для чтения
- **CurrentIntensity** (`float`) — Текущая интенсивность эмиссии
- **CurrentColor** (`Color`) — Текущий цвет эмиссии

## Публичные методы

### ResetToOriginal()
Сбросить к исходным значениям эмиссии. Восстанавливает оригинальный цвет и интенсивность материала.

### FindAndAttachLight()
Найти и привязать источник света на том же объекте или дочерних. Автоматически ищет компонент `Light` и устанавливает его как `targetLight`.

## Примеры использования

### Простая синхронизация эмиссии
```csharp
public class EmissionController : MonoBehaviour
{
    private MeshEmission emission;
    
    void Start()
    {
        emission = GetComponent<MeshEmission>();
        
        // Автоматически находит Light на объекте или детях
        emission.FindAndAttachLight();
        
        // Настройка параметров синхронизации
        emission.syncIntensity = true;
        emission.syncColor = true;
        emission.intensityMultiplier = 1.5f;
    }
}
```

### Мониторинг состояния эмиссии
```csharp
public class EmissionManager : MonoBehaviour
{
    public MeshEmission emission;
    
    void Update()
    {
        // Получение текущих значений
        float currentIntensity = emission.CurrentIntensity;
        Color currentColor = emission.CurrentColor;
        
        Debug.Log($"Current emission - Intensity: {currentIntensity}, Color: {currentColor}");
        
        if (Input.GetKeyDown(KeyCode.R))
        {
            emission.ResetToOriginal();
        }
    }
}
```

### Динамическое изменение параметров синхронизации
```csharp
public class DynamicEmissionController : MonoBehaviour
{
    public MeshEmission emission;
    
    void Update()
    {
        if (Input.GetKeyDown(KeyCode.Alpha1))
        {
            // Синхронизировать только интенсивность
            emission.syncIntensity = true;
            emission.syncColor = false;
            emission.intensityMultiplier = 1.0f;
        }
        
        if (Input.GetKeyDown(KeyCode.Alpha2))
        {
            // Синхронизировать только цвет
            emission.syncIntensity = false;
            emission.syncColor = true;
        }
        
        if (Input.GetKeyDown(KeyCode.Alpha3))
        {
            // Синхронизировать все с множителем и настройками осветления
            emission.syncIntensity = true;
            emission.syncColor = true;
            emission.intensityMultiplier = 2.0f; // Эмиссия в 2 раза ярче света
            emission.whiteThreshold = 5f; // Усиление интенсивности начинается с 5
            emission.whiteRange = 15f; // Максимальное усиление при интенсивности 20
        }
    }
}
```

### Автопоиск источника света
```csharp
public class AutoLightFinder : MonoBehaviour
{
    public MeshEmission emission;
    
    void Start()
    {
        // Автоматически найти источник света
        emission.FindAndAttachLight();
        
        if (emission.TargetLight != null)
        {
            Debug.Log($"Found light: {emission.TargetLight.gameObject.name}");
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
- **whiteThreshold**: 5.0 (усиление интенсивности с интенсивности 5)
- **whiteRange**: 15.0 (максимальное усиление при интенсивности 20)

## Особенности реализации

### Автоматическое отключение эмиссии
Когда интенсивность света опускается ниже порога `emissionCutoff` (0.3 по умолчанию), компонент полностью отключает эмиссию:
- Устанавливает черный цвет эмиссии
- Отключает keyword `_EMISSION` в шейдере
- Устанавливает флаг `EmissiveIsBlack` для GI
- Предотвращает вклад в Bloom и Global Illumination

Это гарантирует, что при выключенном свете материал действительно не светится.

### Поддержка разных Render Pipeline
Компонент автоматически поддерживает:
- **Built-in RP** — использует `_EmissionColor`
- **URP** — использует `_EmissionColor`
- **HDRP** — использует `_EmissiveColor`

### Оптимизация
- Кэширование Shader Property ID для быстрого доступа
- Условная проверка наличия свойств в материале
- Опциональное обновление Dynamic GI

## Советы по использованию

- Применяйте `intensityMultiplier` для настройки яркости эмиссии относительно света
- Настройте `whiteThreshold` и `whiteRange` для контроля усиления интенсивности при высоких значениях
- Используйте `emissionCutoff` для точного контроля порога видимости
- Применяйте `FindAndAttachLight()` для автоматического поиска источника света
- Комбинируйте несколько MeshEmission для сложных эффектов

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
