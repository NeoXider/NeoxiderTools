# RandomRange

**Что это:** MonoBehaviour для генерации случайного числа в заданном диапазоне [Min, Max]. Режимы Int (целое, включительно) и Float. Результат доступен через реактивное свойство и события; подходит для NeoCondition и no-code (например, случайное число аномалий 0–5 на уровень).

**Как использовать:**
1. Добавьте **RandomRange** на GameObject.
2. Задайте **Value Mode** (Int / Float), **Min** и **Max**.
3. Вызывайте **Generate()** по событию (например, при старте уровня или по таймеру). Текущее значение — в **Value**, **ValueInt** / **ValueFloat**.
4. В **NeoCondition** укажите этот компонент и поле **ValueInt** или **ValueFloat** для сравнения (например, «ValueInt &gt;= 1» для «есть хотя бы одна аномалия»).
5. При необходимости подпишитесь на **OnGeneratedInt** / **OnGeneratedFloat** для логики после генерации.

---

## Поля и свойства

| Имя | Тип | Назначение |
|-----|-----|------------|
| ValueMode | RandomRangeValueMode | Режим: Int (целое, min..max включительно) или Float. |
| Min / Max | float (get/set) | Границы диапазона. Для Int округляются. |
| Value | ReactivePropertyFloat | Текущее значение после последнего Generate(); подписка через OnChanged. |
| ValueInt | int | Текущее значение как int (для NeoCondition и привязок). |
| ValueFloat | float | Текущее значение как float. |

## События

| Событие | Когда вызывается | Параметры |
|---------|-------------------|-----------|
| OnGeneratedInt | После Generate() | int — новое целое значение. |
| OnGeneratedFloat | После Generate() | float — новое значение. |

## Методы

| Сигнатура | Возврат | Описание |
|-----------|---------|----------|
| Generate() | void | Генерирует новое значение в [Min, Max], обновляет Value и вызывает события. |
| SetMin(int) / SetMax(int) | void | Устанавливают границы (int). |
| SetMin(float) / SetMax(float) | void | Устанавливают границы (float). |

## Пример: случайное число аномалий на уровень

- **RandomRange**: Mode = Int, Min = 0, Max = 5.
- При старте уровня вызвать **Generate()** (например, из **TimerObject** или сцены).
- **NeoCondition**: объект — этот RandomRange, поле **ValueInt**, оператор «GreaterOrEqual», порог 1 → при «число аномалий ≥ 1» включить таймер спавна аномалий или активировать логику уровня.

## См. также

- [NeoCondition](../../Condition/NeoCondition.md) — проверка по ValueInt / ValueFloat.
- [Selector](../View/Selector.md) — сценарий аномалий.
- [Пример: игра про аномалии](../../Examples/AnomalyGame.md).
