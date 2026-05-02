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

## Привязка к числу с другого объекта (No-Code)

Сам **`SetText`** не содержит полей «источник данных»: он только предоставляет методы **`Set(...)`**. Чтобы в инспекторе выбрать **объект → компонент → поле / `ReactivePropertyFloat`** (как в **NeoCondition**), добавьте на тот же **`GameObject`** компонент **`NoCode Bind Text`** из сборки **`Neo.NoCode`** — он читает значение и вызывает **`SetText.Set(float)`** (или пишет в **`TMP_Text`**, если **`SetText`** не назначен). Правила поиска объекта (**Find By Name**, **Source Root**) совпадают с документацией [**NoCode/README.md**](../../NoCode/README.md). В инспекторе **`SetText`**, если **`NoCodeBindText`** отсутствует, показываются подсказка и кнопка **Add NoCode Bind Text**.

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
Повесьте **`SetText`** и при необходимости **`NoCode Bind Text`** на объект с **`TextMeshPro`**. Для вывода числа с другого компонента настройте биндинг в **`NoCodeBindText`** (см. [**NoCode/README.md**](../../NoCode/README.md)). Отдельно: **`SetText`** — **`Separator`**, **`Decimal`**, **`Number Notation`**, из **`ScoreManager`** можно по-прежнему вызывать **`SetText.Set(int)`** через **`UnityEvent`**.

### Пример (Код)
```csharp
[SerializeField] private SetText _goldText;

public void UpdateGold(int amount)
{
    _goldText.Set(amount);
}
```

## См. также
- [**Neo.NoCode — привязка float к UI**](../../NoCode/README.md)
- [TimeToText](TimeToText.md)
- ← [Tools/Text](README.md)
