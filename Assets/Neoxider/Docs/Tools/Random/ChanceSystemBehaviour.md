# Chance System Behaviour

`ChanceSystemBehaviour` — компонент сцены, оборачивающий `ChanceManager`. Позволяет настраивать шансы в инспекторе, подписываться на результаты без кода (UnityEvent) и при необходимости брать конфигурацию из `ChanceData`.

---

## Ключевые особенности

- **Инлайн-редактор** списка шансов: веса, подписи, флаг Locked, авто-нормализация.
- **Загрузка из ChanceData** при запуске сцены (опционально).
- **Несколько событий для No-Code:**
  - **On Id Generated (int)** — выбранный индекс.
  - **On Index And Weight Selected (int, float)** — индекс и нормализованная вероятность (0..1).
  - **On Roll Complete** — вызывается один раз после каждого броска (обновление UI, звук и т.д.).
  - **Events By Index** — список событий по одному на исход: при выпадении индекса N вызывается событие на позиции N (без кода: «при 0 — одно действие, при 1 — другое»).
- **Из кода:** `LastSelectedIndex`, `LastSelectedEntry`, `EvaluateAndNotify()`, `GetNormalizedWeight(int)`, `GetOrAddEventForIndex(int)`.
- **Log Debug Once** — в редакторе при включении флажка следующая валидация логирует выбранную запись и вероятность.

---

## Поля и события

| Поле / событие | Описание |
|----------------|----------|
| **Manager** | Сериализованный ChanceManager (записи, нормализация). |
| **Chance Data** | Опциональный ScriptableObject; при наличии конфигурация копируется в Awake. |
| **On Id Generated** | Событие с выбранным индексом (int). |
| **On Index And Weight Selected** | Событие (int index, float normalizedWeight). |
| **On Roll Complete** | Событие без аргументов — после каждого броска. |
| **Events By Index** | Список UnityEvent: при выборе индекса i вызывается элемент списка с индексом i (если есть). |
| **Log Debug Once** | Одноразовый лог результата в консоль (только редактор). |

---

## Методы

| Метод | Описание |
|-------|----------|
| `GenerateId()` | Бросок, вызов всех событий, возврат выбранного индекса. |
| `GenerateVoid()` | То же, без возврата (удобно вызывать из кнопки / UnityEvent). |
| `GetId()` | Возвращает выбранный индекс без вызова событий. |
| `Evaluate()` | Возвращает выбранную запись (Entry) без событий. |
| `EvaluateAndNotify()` | Бросок + вызов всех событий, возврат Entry. |
| `SetResultAndNotify(int index)` | Задать результат по индексу и вызвать события (для детерминированного/внешнего выбора). |
| `GetNormalizedWeight(int index)` | Нормализованная вероятность записи. |
| `GetOrAddEventForIndex(int index)` | Доступ к событию по индексу из кода (при необходимости список расширяется). |
| `AddChance` / `SetChance` / `RemoveChance` / `ClearChances` / `Normalize` | Прокси к ChanceManager. |

После `GenerateId()` или `EvaluateAndNotify()` доступны свойства **LastSelectedIndex** и **LastSelectedEntry**.

---

## Примеры

### No-Code: кнопка «Крутить» и разный лут по индексу

1. Создайте пустой GameObject, добавьте **ChanceSystemBehaviour**.
2. В **Manager** добавьте 3 записи (например веса 0.5, 0.3, 0.2), подписи: «Обычный», «Редкий», «Легенда».
3. В **Events By Index** установите размер 3. В элемент 0 перетащите объект/метод для исхода «Обычный» (например активация префаба или воспроизведение звука). В элемент 1 — для «Редкий», в элемент 2 — для «Легенда».
4. Создайте кнопку (UI Button). В **On Click** добавьте вызов: объект с ChanceSystemBehaviour → **ChanceSystemBehaviour.GenerateVoid**.
5. При нажатии на кнопку выполняется бросок и вызывается только то событие в **Events By Index**, индекс которого выпал. Без единой строки кода.

### No-Code: обновить текст после броска

1. На том же объекте с **ChanceSystemBehaviour** подпишите **On Roll Complete**.
2. В список события добавьте вызов метода, который обновляет текст (например **SetText.Set** или ваш компонент с методом `void RefreshResult()`).
3. После каждого броска (кнопка или вызов **GenerateVoid**) сначала сработают события по индексу, затем **On Roll Complete** — можно обновить надпись «Выпало: Редкий» по **LastSelectedIndex** из другого скрипта или показать общий текст.

### No-Code: индекс и вероятность в UI

1. Подпишите **On Index And Weight Selected (int, float)**.
2. В визуальном скриптинге или в компоненте с методом `void OnChance(int index, float probability)` используйте оба параметра: например вывести «Исход 2, шанс 20%» (probability * 100).

---

### Код: бросок и результат

```csharp
[SerializeField] private ChanceSystemBehaviour chanceSystem;

void Roll()
{
    chanceSystem.GenerateId(); // бросок + вызов всех событий
    int index = chanceSystem.LastSelectedIndex;
    var entry = chanceSystem.LastSelectedEntry;
    Debug.Log($"Выпало: {entry?.Label}, индекс {index}");
}
```

### Код: подписка на события из скрипта

```csharp
[SerializeField] private ChanceSystemBehaviour chanceSystem;

void Start()
{
    chanceSystem.OnIndexSelected.AddListener(OnIndexSelected);
    chanceSystem.OnRollComplete.AddListener(OnRollComplete);
}

void OnIndexSelected(int index)
{
    Debug.Log($"Выбран индекс {index}");
}

void OnRollComplete()
{
    Debug.Log("Бросок завершён");
}
```

### Код: только получить результат без событий

```csharp
[SerializeField] private ChanceSystemBehaviour chanceSystem;

void RollQuiet()
{
    int index = chanceSystem.GetId();           // бросок без вызова событий
    var entry = chanceSystem.Evaluate();        // или сразу запись
    if (entry != null)
        SpawnLoot(entry.Label);
}
```

### Код: детерминированный результат (тесты, реплеи)

```csharp
chanceSystem.SetResultAndNotify(1); // «выпал» индекс 1 — вызовятся все события как после обычного броска
```

### Код: ChanceManager без компонента

```csharp
var manager = new ChanceManager(0.5f, 0.3f, 0.2f);
if (manager.TryEvaluate(out int index, out var entry))
    Debug.Log($"{entry.Label} (index {index})");
```

---

## Сценарии без кода (кратко)

1. **Один общий обработчик по индексу** — подписать **On Id Generated**, в обработчике по int выполнить логику.
2. **Разные действия на каждый исход** — заполнить **Events By Index**, вызывать **GenerateVoid** (например с кнопки).
3. **После любого броска** — подписать **On Roll Complete**.
4. **Индекс + вероятность в UI** — подписать **On Index And Weight Selected**.

---

## Быстрый старт

1. Добавьте компонент на GameObject.
2. В **Manager** настройте записи (веса, подписи, Locked).
3. При необходимости укажите **Chance Data** или оставьте локальную конфигурацию.
4. Подпишите нужные события (On Id Generated, Events By Index, On Roll Complete и т.д.) или вызывайте `GenerateId()` / `GenerateVoid()` из другого компонента или кнопки.
5. Для отладки включите **Log Debug Once**.

---

## Полезно знать

- Компонент вызывает `Sanitize()` и `EnsureUniqueIds()` в Awake и OnValidate.
- Собственный генератор случайных чисел: `Manager.RandomProvider = () => (float)systemRandom.NextDouble();`.
- Поведение нормализации задаётся в ChanceManager (см. документацию Chance Manager).

Подробнее о структуре записей и весах — в **Chance Manager**.
