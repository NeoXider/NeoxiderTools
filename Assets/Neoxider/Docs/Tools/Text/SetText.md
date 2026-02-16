# Компонент SetText

## 1. Введение

`SetText` — компонент для `TextMeshPro`, который форматирует числа и плавно анимирует переходы значений через DOTween. Подходит для денег, очков, урона, процентов и idle-чисел с очень большими значениями.

---

## 2. Класс и назначение

### SetText
- **Пространство имен**: `Neo.Tools`
- **Путь к файлу**: `Assets/Neoxider/Scripts/Tools/Text/SetText.cs`

Компонент объединяет:
- визуальный вывод в `TMP_Text`;
- форматирование (`Plain`, `Grouped`, `IdleShort`, `Scientific`);
- режим округления;
- префиксы/суффиксы;
- анимацию чисел.

---

## 3. Настройки в Inspector

### Text Component
- `text` (`TMP_Text`) — целевой текстовый компонент.

### Formatting
- `separator` (`string`) — разделитель групп разрядов.
- `decimal` (`int`) — число знаков после запятой.
- `numberNotation` (`NumberNotation`) — стиль вывода числа.
- `roundingMode` (`NumberRoundingMode`) — стратегия округления.
- `trimTrailingZeros` (`bool`) — убирать хвостовые нули в дробной части.
- `decimalSeparator` (`string`) — разделитель дробной части.
- `startAdd` (`string`) — префикс для `Set(string)`.
- `endAdd` (`string`) — суффикс для `Set(string)`.
- `indexOffset` (`int`) — смещение для целых значений в `Set(int)`.

### Animation
- `_timeAnim` (`float`) — длительность анимации.
- `_ease` (`Ease`) — easing-функция DOTween.
- `_onEnableAnim` (`bool`) — поведение анимации при disabled/enable объекта.

---

## 4. Нотации чисел

Нотация выбирается через `numberNotation` в `SetText` или в `NumberFormatOptions.Notation`.

### `Plain`
Прямой вывод числа без суффиксов и без группировки.

Пример (`1234567.89`, `decimals=2`):
- `1234567.89`

### `Grouped`
Вывод с группировкой разрядов.

Пример (`1234567.89`, `separator=" "`, `decimalSeparator=","`):
- `1 234 567,89`

### `IdleShort`
Короткая idle-нотация с суффиксами (`K`, `M`, `B`, `T`, `Qa`, `Qi`...).

Примеры:
- `950` -> `950`
- `1_200` -> `1.2K`
- `12_345_678` -> `12.35M` (при `decimals=2`)

### `Scientific`
Научная нотация вида `mantissa e exponent`.

Пример (`1234567`, `decimals=2`):
- `1.23e6`

---

## 5. Режимы округления

Режим задается через `roundingMode` (`SetText`) или `NumberFormatOptions.RoundingMode`.

- `ToEven` — банковское округление (к ближайшему четному на .5).
- `AwayFromZero` — округление от нуля.
- `ToZero` — усечение дробной части к нулю.
- `ToPositiveInfinity` — округление вверх (`Ceiling`).
- `ToNegativeInfinity` — округление вниз (`Floor`).

Пример для `-12.345` при `decimals = 2`:
- `ToEven` -> `-12.34`
- `AwayFromZero` -> `-12.35`
- `ToZero` -> `-12.34`
- `ToPositiveInfinity` -> `-12.34`
- `ToNegativeInfinity` -> `-12.35`

---

## 6. Публичный API SetText

### Свойства
- `Separator` (`string`)
- `DecimalPlaces` (`int`)
- `NumberNotationStyle` (`NumberNotation`)
- `RoundingMode` (`NumberRoundingMode`)
- `IndexOffset` (`int`)

### Методы
- `Set(int value)` — вывод целого (с учетом `indexOffset`) с анимацией.
- `Set(float value)` — вывод `float` с анимацией и форматированием.
- `Set(string value)` — прямой вывод строки без числового форматтера.
- `SetPercentage(float value, bool addPercentSign = true)`
- `SetCurrency(float value, string currencySymbol = "$")`
- `SetBigInteger(BigInteger value)`
- `SetBigInteger(string value)`
- `SetFormatted(float value, NumberFormatOptions options)`
- `SetFormatted(BigInteger value, NumberFormatOptions options)`
- `Clear()`

### События
- `OnTextUpdated` — вызывается при установке текста через `Set(string)`.

---

## 7. Примеры

### Extension API (без SetText)

```csharp
using Neo.Extensions;
using System.Numerics;

NumberFormatOptions idle = NumberFormatOptions.IdleShort;
idle.Decimals = 2;
idle.RoundingMode = NumberRoundingMode.AwayFromZero;
idle.Prefix = "$";

string s1 = 12500.ToPrettyString(idle);               // "$12.5K"
string s2 = 987654321f.ToIdleString(1);               // "987.7M"
string s3 = BigInteger.Parse("999999999999999999999")
    .ToPrettyString(idle);                            // idle-строка с крупным суффиксом
```

### SetText (Inspector + код)

```csharp
using Neo.Extensions;
using Neo.Tools;
using System.Numerics;

public class DemoTextUsage : UnityEngine.MonoBehaviour
{
    public SetText moneyText;

    private void Start()
    {
        moneyText.NumberNotationStyle = NumberNotation.IdleShort;
        moneyText.RoundingMode = NumberRoundingMode.ToEven;
        moneyText.DecimalPlaces = 2;

        moneyText.Set(15234.567f);                    // анимированный вывод
        moneyText.SetCurrency(15234.567f, "$");       // "$15.23K"

        BigInteger huge = BigInteger.Parse("123456789012345678901234567890");
        moneyText.SetBigInteger(huge);                // вывод без анимации
    }
}
```

### Точечная переопределяемая настройка через `SetFormatted`

```csharp
NumberFormatOptions sci = new NumberFormatOptions(
    NumberNotation.Scientific,
    decimals: 3,
    roundingMode: NumberRoundingMode.ToPositiveInfinity,
    trimTrailingZeros: true,
    groupSeparator: ",",
    decimalSeparator: ".",
    prefix: "",
    suffix: " dmg");

setText.SetFormatted(1234567.891f, sci);              // "1.235e6 dmg"
```
