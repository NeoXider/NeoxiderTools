# AnimationUtils

**Namespace:** `Neo.Animations`  
**Путь к файлу:** `Assets/Neoxider/Scripts/Animations/AnimationUtils.cs`

## Описание

Статический класс, предоставляющий универсальные методы для расчета анимированных значений по времени. Является ядром системы анимаций и используется всеми аниматорами для получения анимированных значений.

## Ключевые особенности

- **Статические методы**: Не создает объектов, высокая производительность
- **Универсальность**: Поддерживает анимацию любых типов данных
- **Гибкость**: Множество параметров для точной настройки
- **Безопасность**: Корректная обработка граничных случаев (speed = 0)

## Публичные методы

### GetPerlinNoiseValue
```csharp
public static float GetPerlinNoiseValue(
    float animationTime,
    float speed,
    Vector2 randomOffset,
    Vector2 noiseOffset,
    float noiseScale,
    bool use2DNoise)
```
**Описание:** Получает значение шума Перлина для анимации.  
**Возвращает:** Значение шума от 0 до 1  
**Параметры:**
- `animationTime` — текущее время анимации
- `speed` — скорость анимации
- `randomOffset` — случайное смещение для уникальности
- `noiseOffset` — дополнительное смещение шума
- `noiseScale` — масштаб шума
- `use2DNoise` — использовать 2D шум вместо 1D

### GetTargetValue
```csharp
public static float GetTargetValue(
    AnimationType type,
    float min,
    float max,
    float animationTime,
    float speed,
    bool use2DNoise,
    Vector2 randomOffset,
    Vector2 noiseOffset,
    float noiseScale,
    AnimationCurve customCurve = null)
```
**Описание:** Получает целевое значение анимации на основе типа и времени.  
**Возвращает:** Анимированное значение между min и max  
**Параметры:**
- `type` — тип анимации
- `min` — минимальное значение
- `max` — максимальное значение
- `animationTime` — текущее время анимации
- `speed` — скорость анимации (если 0, возвращает min)
- `use2DNoise` — использовать 2D шум для PerlinNoise
- `randomOffset` — случайное смещение
- `noiseOffset` — смещение шума
- `noiseScale` — масштаб шума
- `customCurve` — пользовательская кривая для CustomCurve типа

### GetColorBlendFactor
```csharp
public static float GetColorBlendFactor(
    float animationTime,
    float speed,
    float blendSpeed)
```
**Описание:** Получает фактор смешивания цвета для плавного перехода между цветами.  
**Возвращает:** Фактор смешивания от 0 до 1  
**Параметры:**
- `animationTime` — текущее время анимации
- `speed` — скорость анимации
- `blendSpeed` — скорость смешивания цветов

### ApplyToLight
```csharp
public static void ApplyToLight(
    ILightAccessor accessor,
    float targetIntensity,
    Color originalColor,
    bool changeColor,
    Color targetColor,
    float colorBlendFactor)
```
**Описание:** Применяет анимированные значения к источнику света.  
**Параметры:**
- `accessor` — интерфейс доступа к свету
- `targetIntensity` — целевая интенсивность
- `originalColor` — исходный цвет
- `changeColor` — изменять ли цвет
- `targetColor` — целевой цвет
- `colorBlendFactor` — фактор смешивания цвета

### ApplyToMesh
```csharp
public static void ApplyToMesh(
    Material mat,
    float targetIntensity,
    Color originalEmission,
    bool changeColor,
    Color targetColor,
    float colorBlendFactor)
```
**Описание:** Применяет анимированные значения к материалу меша для эмиссии.  
**Параметры:**
- `mat` — материал меша
- `targetIntensity` — целевая интенсивность
- `originalEmission` — исходный цвет эмиссии
- `changeColor` — изменять ли цвет
- `targetColor` — целевой цвет
- `colorBlendFactor` — фактор смешивания цвета

### GetAnimatedFloat
```csharp
public static float GetAnimatedFloat(
    AnimationType type,
    float min,
    float max,
    float animationTime,
    float speed,
    AnimationCurve customCurve = null)
```
**Описание:** Получает анимированное float значение.  
**Возвращает:** Анимированное значение  
**Параметры:**
- `type` — тип анимации
- `min` — минимальное значение
- `max` — максимальное значение
- `animationTime` — текущее время анимации
- `speed` — скорость анимации
- `customCurve` — пользовательская кривая для CustomCurve

### GetAnimatedColor
```csharp
public static Color GetAnimatedColor(
    AnimationType type,
    Color colorA,
    Color colorB,
    float animationTime,
    float speed,
    AnimationCurve customCurve = null)
```
**Описание:** Получает анимированный цвет путем интерполяции между двумя цветами.  
**Возвращает:** Анимированный цвет  
**Параметры:**
- `type` — тип анимации
- `colorA` — первый цвет
- `colorB` — второй цвет
- `animationTime` — текущее время анимации
- `speed` — скорость анимации
- `customCurve` — пользовательская кривая для CustomCurve

### GetAnimatedVector3
```csharp
public static Vector3 GetAnimatedVector3(
    AnimationType type,
    Vector3 vectorA,
    Vector3 vectorB,
    float animationTime,
    float speed,
    AnimationCurve customCurve = null)
```
**Описание:** Получает анимированный Vector3 путем интерполяции между двумя векторами.  
**Возвращает:** Анимированный вектор  
**Параметры:**
- `type` — тип анимации
- `vectorA` — первый вектор
- `vectorB` — второй вектор
- `animationTime` — текущее время анимации
- `speed` — скорость анимации
- `customCurve` — пользовательская кривая для CustomCurve

## Интерфейс ILightAccessor

```csharp
public interface ILightAccessor
{
    float Intensity { get; set; }
    Color Color { get; set; }
    string ImplName { get; }
}
```

**Описание:** Интерфейс для работы с различными типами источников света (Light, Light2D).  
**Свойства:**
- `Intensity` — интенсивность света
- `Color` — цвет света
- `ImplName` — название реализации (для отладки)

## Примеры использования

### Простая анимация числа
```csharp
void Update()
{
    float animatedValue = AnimationUtils.GetAnimatedFloat(
        AnimationType.PerlinNoise,
        0f, 1f,
        Time.time, 2f
    );
    
    transform.localScale = Vector3.one * animatedValue;
}
```

### Анимация цвета
```csharp
void Update()
{
    Color animatedColor = AnimationUtils.GetAnimatedColor(
        AnimationType.SmoothTransition,
        Color.red, Color.blue,
        Time.time, 1f
    );
    
    GetComponent<Renderer>().material.color = animatedColor;
}
```

### Анимация позиции
```csharp
void Update()
{
    Vector3 animatedPosition = AnimationUtils.GetAnimatedVector3(
        AnimationType.SinWave,
        Vector3.zero, Vector3.up * 2f,
        Time.time, 1f
    );
    
    transform.position = animatedPosition;
}
```

## Советы по оптимизации

- Используйте `speed = 0` для отключения анимации
- Кэшируйте часто используемые значения
- Для сложных эффектов комбинируйте несколько вызовов
- Используйте события компонентов вместо постоянного опроса значений

## Производительность

Все методы оптимизированы для использования в Update:
- Статические методы без создания объектов
- Минимальные вычисления в критическом пути
- Эффективные математические функции Unity
- Корректная обработка граничных случаев
