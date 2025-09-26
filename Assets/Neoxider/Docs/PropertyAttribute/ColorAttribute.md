
# [Color]

**Пространство имен:** `Neo`
**Путь:** `Scripts/PropertyAttribute/ColorAtribute.cs`

## Описание

Атрибут `[Color]` — это простая, но полезная утилита для визуальной организации инспектора. Он позволяет раскрасить фон полей, что помогает визуально группировать связанные свойства или привлекать внимание к важным настройкам.

## Как использовать

Поместите атрибут `[Color]` над любым сериализуемым полем в вашем компоненте.

```csharp
public class PlayerStats : MonoBehaviour
{
    [Color(ColorEnum.SoftGreen)]
    public int health = 100;

    [Color(ColorEnum.SoftBlue)]
    public float mana = 50f;
}
```

## Способы задания цвета

Цвет можно указать двумя способами:

### 1. Через перечисление `ColorEnum`

Для удобства и единообразия в атрибуте предопределен набор мягких, приятных для глаз цветов. Это предпочтительный способ использования.

**Пример:**
```csharp
[Color(ColorEnum.SoftYellow)]
public string playerName = "Neo";
```

**Доступные цвета в `ColorEnum`:**
- `SoftRed`
- `SoftGreen`
- `SoftBlue`
- `SoftYellow`
- `SoftGray`
- `SoftPurple`
- `SoftCyan`
- `SoftOrange`

### 2. Через значения RGBA

Вы можете задать любой цвет, указав его компоненты R, G, B и, опционально, A (прозрачность). Значения должны быть в диапазоне от `0.0` до `1.0`.

**Пример:**
```csharp
// Ярко-оранжевый цвет
[Color(1.0, 0.5, 0.0)] 
public Transform targetTransform;
```
