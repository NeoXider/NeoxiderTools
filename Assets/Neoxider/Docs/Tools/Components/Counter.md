# Универсальный счётчик (Counter)

## 1. Введение

`Counter` — компонент для хранения числового значения (целого или дробного), его изменения (Add/Subtract/Multiply/Divide/Set) и отправки значения через событие Send. Поддерживает два режима: **Int** и **Float**. В зависимости от режима вызываются типизированные события: **OnValueChangedInt** / **OnValueChangedFloat** при изменении и **OnSendInt** / **OnSendFloat** при Send. Опционально сохраняет значение при изменении через **SaveProvider** по строковому ключу (как в Money); по умолчанию сохранение выключено. Все публичные методы доступны как кнопки в инспекторе (Odin `[Button]` или `[ButtonAttribute]`).

---

## Быстрый старт (No-Code)

1. Добавьте компонент Counter на GameObject.
2. Выберите режим **Int** или **Float**, при необходимости задайте начальное значение в **Value**.
3. Подпишите **Value.OnChanged** (или OnValueChangedInt / OnValueChangedFloat по типу) на компонент вывода текста (например [SetText](../Text/SetText.md)) или свой метод — значение будет обновляться при любом изменении.
4. Вызов **Add** / **Subtract** привяжите к кнопке (On Click) или другому UnityEvent — счётчик изменится без кода.

**Из кода:** `counter.Add(10);` затем `counter.Send();` — отправит текущее значение подписчикам.

---

## 2. Описание класса

### Counter
- **Пространство имён**: `Neo.Tools`
- **Путь к файлу**: `Assets/Neoxider/Scripts/Tools/Components/Counter.cs`
- **Наследуется от**: `MonoBehaviour`

**Описание**:
Универсальный счётчик: хранит число (по умолчанию 0) в реактивном поле **Value** (ReactivePropertyFloat), методы Add/Subtract/Multiply/Divide/Set, Send с событием. События по типу числа: в режиме Int вызываются OnValueChangedInt и OnSendInt, в режиме Float — OnValueChangedFloat и OnSendFloat. Подписка на любое изменение (float) — через **Value.OnChanged**. Опционально сохраняет значение через SaveProvider по ключу (по умолчанию выключено).

### Ключевые особенности
- **Два режима значения**: Int (целое) или Float (дробное). В режиме Int значение после операций округляется.
- **Методы изменения**: `Add`, `Subtract`, `Multiply`, `Divide`, `Set` (int и float). При делении на 0 значение не меняется.
- **События по типу**: в режиме Int вызываются `OnValueChangedInt`/`OnSendInt`, в Float — `OnValueChangedFloat`/`OnSendFloat`. При любом изменении вызывается также `Value.OnChanged` (float). При Send — `OnSend` (float).
- **Send без аргумента**: вызывает типизированное событие со значением по Payload (Counter / ScoreManager.Score / Money.money).
- **Send с аргументом**: `Send(int)` или `Send(float)` — отправка заданного числа без изменения счётчика.
- **Сохранение (опционально)**: галочка «сохранять» (по умолчанию выключена), строка-ключ для SaveProvider (как в Money). При изменении значения число сохраняется через `SaveProvider.SetFloat(key, value)` и `SaveProvider.Save()`; при Start загружается через `SaveProvider.GetFloat(key, defaultValue)`. Ключ должен быть уникальным для каждого счётчика.
- **Вызов событий при загрузке**: опция **Invoke Events On Load** (по умолчанию **вкл.**). Если включена, после загрузки значения в Start вызываются те же события, что и при изменении (Value.OnChanged, OnValueChangedInt / OnValueChangedFloat), чтобы UI и подписчики сразу отобразили загруженное значение. Без этой опции загрузка произойдёт, но подписанный текст или логика не обновятся до первого изменения счётчика.
- **Кнопки в инспекторе**: для всех публичных методов (Odin или `ButtonAttribute`).

---

## 3. Enum'ы

### CounterValueMode
- `Int` — целочисленный режим (значение округляется).
- `Float` — дробный режим.

### CounterSendPayload
Определяет, какое значение передаётся в события Send при вызове `Send()` без аргумента:
- `Counter` — текущее значение счётчика (по умолчанию).
- `Score` — текущий счёт `ScoreManager.I.Score`.
- `Money` — текущее значение `Neo.Shop.Money.I.money`.

---

## 4. Настройки в инспекторе

- `_valueMode` (`CounterValueMode`): Режим значения — Int или Float.
- **Value** (`ReactivePropertyFloat`): Реактивное поле — текущее значение (по умолчанию 0) и событие **OnChanged** при изменении. Подписка на изменение значения (float) — через `Value.OnChanged`.
- `_sendPayload` (`CounterSendPayload`): Источник значения для `Send()` без аргумента (Counter / Score / Money).
- **Сохранение**
  - `_saveEnabled` (`bool`): Включить сохранение при изменении (по умолчанию выключено).
  - `_saveKey` (`string`): Ключ для SaveProvider (уникальный для каждого счётчика), по умолчанию `"Counter"`.
  - `_invokeEventsOnLoad` (`bool`): При загрузке в Start вызывать события изменения (чтобы UI применил загруженное значение). По умолчанию **вкл.** Отключите, если не хотите уведомлять подписчиков при старте.
- **События по типу**
  - `OnValueChangedInt` (`UnityEvent<int>`): Вызывается при изменении значения в режиме Int.
  - `OnValueChangedFloat` (`UnityEvent<float>`): Вызывается при изменении значения в режиме Float.
  - `OnSendInt` (`UnityEvent<int>`): Вызывается при Send() в режиме Int.
  - `OnSendFloat` (`UnityEvent<float>`): Вызывается при Send() в режиме Float.
- **События общие**
  - `OnSend` (`UnityEvent<float>`): Вызывается при Send() (float).

---

## 5. Публичные свойства

- **Value** (`ReactivePropertyFloat`): Реактивное поле (значение + OnChanged). Для подписки на любое изменение используйте `Value.OnChanged`.
- `ValueInt` (`int`): Текущее значение счётчика как целое (в режиме Int — округлённое). Только для чтения.
- `ValueFloat` (`float`): Текущее значение счётчика как float. Только для чтения.

---

## 6. Публичные методы

- `Add(int amount)` / `Add(float amount)`: Увеличивает счётчик на указанное значение.
- `Subtract(int amount)` / `Subtract(float amount)`: Уменьшает счётчик на указанное значение.
- `Multiply(int factor)` / `Multiply(float factor)`: Умножает счётчик на множитель.
- `Divide(int divisor)` / `Divide(float divisor)`: Делит счётчик на делитель; при делении на 0 значение не меняется.
- `Set(int value)` / `Set(float value)`: Устанавливает значение счётчика.
- `Send()`: Вызывает события Send со значением по Payload (Counter/Score/Money). Счётчик не изменяется.
- `Send(float valueToSend)` / `Send(int valueToSend)`: Вызывает события Send с переданным числом. Счётчик не изменяется.

Все методы доступны как кнопки в инспекторе.

---

## 7. Unity Events

- **Значение (реактивное поле)**  
  - `Value.OnChanged` (`UnityEvent<float>`): при любом изменении значения счётчика.  
- **По типу (в зависимости от режима)**  
  - `OnValueChangedInt` (`UnityEvent<int>`): при изменении в режиме Int.  
  - `OnValueChangedFloat` (`UnityEvent<float>`): при изменении в режиме Float.  
  - `OnSendInt` (`UnityEvent<int>`): при Send() в режиме Int.  
  - `OnSendFloat` (`UnityEvent<float>`): при Send() в режиме Float.  
- **Общие**  
  - `OnSend` (`UnityEvent<float>`): при вызове `Send()` (значение по Payload или переданное в `Send(value)`).

---

## 8. Сохранение (как в Money)

При включённой опции сохранения (`_saveEnabled`) и заданном ключе `_saveKey`:

- **При старте**: значение загружается через `SaveProvider.GetFloat(_saveKey, Value.CurrentValue)`. Если включено **Invoke Events On Load** (по умолчанию да), после загрузки вызываются события Value.OnChanged, OnValueChangedInt / OnValueChangedFloat — подписчики (например SetText) сразу отображают загруженное значение.
- **При изменении значения**: сохраняется через `SaveProvider.SetFloat(_saveKey, Value.CurrentValue)` и `SaveProvider.Save()`.

Используется тот же SaveProvider, что и в Money (например PlayerPrefs или провайдер из настроек). Ключ должен быть уникальным для каждого счётчика в проекте (например `"Counter_Coins"`, `"Counter_Level"`).
