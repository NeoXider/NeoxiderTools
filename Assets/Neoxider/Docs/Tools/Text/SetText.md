# SetText

**Назначение:** Компонент для вывода форматированных числовых и текстовых значений в `TMP_Text`. Поддерживает разделители тысяч, десятичные знаки, нотации (Grouped, Scientific, Short), проценты, валюту, `BigInteger` и анимацию смены числа через DOTween.

## Поля (Inspector)

| Поле | Описание |
|------|----------|
| **Text** | Ссылка на `TMP_Text` (авто-назначение). |
| **Separator** | Разделитель тысяч (по умолчанию `.`). |
| **Decimal** | Количество знаков после запятой (0–10). |
| **Number Notation** | Стиль нотации: `Grouped` (1.000), `Scientific`, `Short` (1K, 1M). |
| **Start Add / End Add** | Префикс и суффикс для итогового текста. |
| **Time Anim** | Длительность анимации перехода числа (DOTween). |
| **Ease** | Кривая анимации. |

## API

| Метод / Свойство | Описание |
|------------------|----------|
| `void Set(int value)` | Установить целое число (с анимацией). |
| `void Set(float value)` | Установить дробное число (с анимацией). |
| `void Set(string value)` | Установить строку (без анимации). |
| `void SetPercentage(float value, bool addSign)` | Установить процент (0–100). |
| `void SetCurrency(float value, string symbol)` | Установить валюту с символом. |
| `void SetBigInteger(BigInteger value)` | Установить большое число. |
| `void Clear()` | Очистить текст. |

## Unity Events

| Событие | Аргументы | Описание |
|---------|-----------|----------|
| `OnTextUpdated` | `string` | Вызывается после каждого обновления текста. |

## Примеры

### Пример No-Code (в Inspector)
Повесьте `SetText` на объект с `TextMeshPro`. Настройте `Separator = " "`, `Decimal = 0`, `Number Notation = Short`. Подключите событие из `ScoreManager.OnScoreChanged` к `SetText.Set(int)`. Теперь при изменении счёта текст обновится с анимацией.

### Пример (Код)
```csharp
[SerializeField] private SetText _goldText;

public void UpdateGold(int amount)
{
    _goldText.Set(amount);
}
```

## См. также
- [TimeToText](TimeToText.md)
- ← [Tools/Text](README.md)
