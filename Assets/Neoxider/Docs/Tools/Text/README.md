# Текстовые утилиты (Text)

Этот раздел содержит компоненты для работы с текстом, в частности с `TextMeshPro`.

Начиная с `v5.8.6`, `SetText` поддерживает универсальное форматирование чисел:
- нотации `Plain`, `Grouped`, `IdleShort`, `Scientific`;
- гибкое округление (`ToEven`, `AwayFromZero`, `ToZero`, `ToPositiveInfinity`, `ToNegativeInfinity`);
- единый API расширений для `int`, `float`, `double`, `BigInteger` (`ToPrettyString`, `ToIdleString`).

### Быстрый пример (Extensions)

```csharp
using Neo.Extensions;

NumberFormatOptions options = NumberFormatOptions.IdleShort;
options.Decimals = 2;
options.RoundingMode = NumberRoundingMode.AwayFromZero;

string money = 1234567.891f.ToPrettyString(options); // 1.23M
```

Подробнее по нотациям, округлению и API `SetText` см. в [`SetText.md`](./SetText.md).

## Файлы

- [SetText](./SetText.md): Мощный компонент для форматирования и анимированного отображения чисел, валюты и процентов.
- [TimeToText](./TimeToText.md): Утилита для преобразования времени в секундах в отформатированную строку (например, `ММ:СС`).
