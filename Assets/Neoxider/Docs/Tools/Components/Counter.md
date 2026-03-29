# Counter

**Что это:** компонент счётчика: число (Int/Float), Add/Subtract/Multiply/Divide/Set, события при изменении и **Send**, отдельные события **OnLoaded** после чтения сохранения, опции **Load On Start** и **Send On Start**, метод **LoadFromSave()**. Сохранение через SaveProvider опционально. Пространство имён `Neo.Tools`, файл `Scripts/Tools/Components/Counter.cs`.

**Как использовать:** добавить на GameObject, выбрать режим; подписать **OnValueChanged** и/или **OnLoaded** и **Send**; при сохранении задать ключ и при необходимости отредактировать **Load On Start** / **Invoke Events On Load** / **Send On Start**. См. «Быстрый старт» и раздел «Порядок в Start».

---

## Быстрый старт

1. Добавьте компонент Counter на GameObject.
2. Режим **Int** или **Float**, при необходимости начальное значение в **Value**.
3. Подпишите **OnValueChangedInt** / **OnValueChangedFloat** или **Value.OnChanged** на [SetText](../Text/SetText.md) или свой метод.
4. Если включено сохранение: при необходимости **OnLoadedInt** / **OnLoadedFloat** — только когда значение прочитано из SaveProvider (отличие от «любого изменения»).
5. **Send On Start** — один вызов **Send()** в конце `Start` (по Payload); **Load On Start** — чтение сохранения в начале `Start` (при включённом Save).
6. Add/Subtract привязать к кнопке или вызывать из кода.

**Из кода:** `counter.Add(10);` затем `counter.Send();` — отправит значение подписчикам Send. `counter.LoadFromSave();` — ручная загрузка, если **Load On Start** выключен.

---

## 1. Введение

`Counter` хранит число в **Value** (ReactivePropertyFloat), предоставляет арифметику и **Send** с типизированными событиями (**OnSendInt** / **OnSendFloat**, плюс **OnSend** как float). При включённом **Save** значение пишется при изменении и может читаться в **Start** (**Load On Start**, по умолчанию вкл.). После **Load()** всегда вызываются **OnLoadedInt** или **OnLoadedFloat** (в зависимости от режима); при **Invoke Events On Load** дополнительно срабатывают те же события, что и при изменении значения. В конце **Start** опционально вызывается **Send()** (**Send On Start**). Публичные методы отмечены **Button** и доступны в инспекторе.

---

## 2. Описание класса

| | |
|--|--|
| **Пространство имён** | `Neo.Tools` |
| **Файл** | `Assets/Neoxider/Scripts/Tools/Components/Counter.cs` |
| **Базовый класс** | `MonoBehaviour` |

### Ключевые особенности

- Режимы **Int** / **Float** и округление в Int.
- **Send** с **CounterSendPayload**: Counter, Score, Money.
- **OnRepeatByCounterValue** — N вызовов по текущему значению (опционально при изменении и/или при Send).
- **Save** + **Load On Start** + **Invoke Events On Load** + **OnLoaded*** + **LoadFromSave()**.
- **Send On Start** — финальный `Send()` в `Start`.

### Порядок в `Start`

Выполняется только при включённом **Save** и непустом **Save Key** и **Load On Start**:

1. `Load()` из SaveProvider  
2. **OnLoadedInt** или **OnLoadedFloat**  
3. Если **Invoke Events On Load** — `Value.ForceNotify()`, **OnValueChanged***, **Value.OnChanged**

Если **Save** выключен или **Load On Start** выключен — шаги 1–3 не выполняются (значение остаётся из Inspector до **LoadFromSave()**, если Save включён).

Затем, если включён **Send On Start**, вызывается **Send()** (значение счётчика не меняется).

---

## 3. Enum'ы

### CounterValueMode

- **Int** — целое, операции округляются.
- **Float** — float.

### CounterSendPayload

Источник для `Send()` без аргумента:

- **Counter** — текущее счётчика.
- **Score** — `ScoreManager.I.Score`.
- **Money** — `Neo.Shop.Money.I.money`.

---

## 4. Настройки в инспекторе

| Поле | Описание |
|------|----------|
| **Value Mode** | Int или Float. |
| **Value** | ReactivePropertyFloat: значение и **OnChanged**. |
| **Send Payload** | Counter / Score / Money для `Send()` без аргумента. |

**Repeat Event**

| Поле | Описание |
|------|----------|
| Invoke Repeat … on value changed | **OnRepeatByCounterValue** N раз при изменении (N = значение счётчика, ≥ 0). |
| Invoke Repeat … on Send | То же при **Send()**. |

**Save**

| Поле | По умолчанию | Описание |
|------|----------------|----------|
| Save enabled | выкл. | Сохранять при изменении через SaveProvider. |
| Save key | `"Counter"` | Уникальный ключ. |
| **Load On Start** | вкл. | Читать сохранение в `Start`. Выкл. — значение из Inspector до **LoadFromSave()**. |
| **Invoke Events On Load** | вкл. | После загрузки вызвать события как при изменении (**OnValueChanged***, **Value**). |
| **Send On Start** | выкл. | В конце `Start` один раз **Send()**. |

**События — изменение значения**

- **OnValueChangedInt** / **OnValueChangedFloat** — при смене значения счётчика (в т.ч. после загрузки, если **Invoke Events On Load**).

**События — загрузка из сохранения**

- **OnLoadedInt** / **OnLoadedFloat** — один раз сразу после **Load()** (в `Start` при условиях выше или при **LoadFromSave()**). Не дублируют смысл «любое изменение» — только факт чтения сохранения.

**События — Send**

- **OnSendInt** / **OnSendFloat**, **OnSend** (float) — при **Send()** / **Send(value)**.

**Прочее**

- **OnRepeatByCounterValue** — см. Repeat Event.

---

## 5. Публичные свойства

- **Value** — `ReactivePropertyFloat` (значение + **OnChanged**).
- **ValueInt**, **ValueFloat** — только чтение.

---

## 6. Публичные методы

| Метод | Описание |
|-------|----------|
| Add / Subtract / Multiply / Divide / Set | Изменение значения. |
| **Send()** | События Send по Payload; счётчик не меняется. |
| **Send(int/float)** | Send с явным числом. |
| **LoadFromSave()** | **Load()** + **OnLoaded*** + при **Invoke Events On Load** — уведомление как при изменении. |

Остальные публичные методы доступны кнопками в инспекторе.

---

## 7. Unity Events (сводка)

| Событие | Когда |
|---------|--------|
| `Value.OnChanged` | Любое изменение числа (float). |
| `OnValueChangedInt` / `OnValueChangedFloat` | Изменение в соответствующем режиме. |
| `OnLoadedInt` / `OnLoadedFloat` | После **Load()** из SaveProvider. |
| `OnSendInt` / `OnSendFloat`, `OnSend` | При **Send**. |
| `OnRepeatByCounterValue` | N раз по настройкам Repeat Event. |

---

## 8. Сохранение

При **Save enabled** и заданном ключе:

- В **Start** (если **Load On Start**) или по **LoadFromSave()** — чтение через `SaveProvider.GetFloat`.
- При изменении значения — `SetFloat` + `Save`.

Ключи должны быть уникальны (например `"Counter_Coins"`).

---

## См. также

- [SetText](../Text/SetText.md) — отображение числа в UI.
- [Tools/Components README](./README.md) — оглавление компонентов Tools.
