# ReactiveProperty

**Назначение:** Сериализуемая реактивная переменная в стиле R3. Хранит значение и вызывает `UnityEvent` при изменении. Подходит для привязки в Inspector (No-Code) и из кода. Три готовых типа: `ReactivePropertyFloat`, `ReactivePropertyInt`, `ReactivePropertyBool`.

---

## Поля (Inspector)

| Поле | Описание |
|------|----------|
| **Value** | Текущее значение. Изменение через Inspector вызовет `OnChanged`. |
| **On Changed** | `UnityEvent<T>` — срабатывает при изменении значения. Можно привязать любой метод в Inspector. |

---

## API

| Метод / Свойство | Описание |
|------------------|----------|
| `T Value { get; set; }` | Текущее значение. При `set` сравнивает с предыдущим; если отличается — вызывает `OnChanged`. |
| `T CurrentValue { get; }` | То же значение, но только для чтения. |
| `TEvent OnChanged { get; }` | `UnityEvent<T>` — подписка на изменения (через Inspector или `AddListener`). |
| `void AddListener(UnityAction<T> call)` | Подписаться на изменения из кода. |
| `void RemoveListener(UnityAction<T> call)` | Отписаться. |
| `void RemoveAllListeners()` | Удалить всех подписчиков. |
| `void OnNext(T value)` | Установить значение (аналог `Value = value`). |
| `void SetValueWithoutNotify(T value)` | Установить значение **без** вызова `OnChanged` (например, при загрузке). |
| `void ForceNotify()` | Принудительно вызвать `OnChanged` с текущим значением. |

---

## Готовые типы

| Класс | Тип значения | Тип события |
|-------|-------------|-------------|
| `ReactivePropertyFloat` | `float` | `UnityEventFloat` |
| `ReactivePropertyInt` | `int` | `UnityEventInt` |
| `ReactivePropertyBool` | `bool` | `UnityEventBool` |

---

## Примеры

### No-Code (Inspector)
1. Добавить `ReactivePropertyFloat` как поле компонента.
2. В Inspector задать начальное значение в **Value**.
3. В **On Changed** привязать метод, например `Slider.value` или `Text.SetText`.
4. При изменении `Value` из любого скрипта — привязанный метод вызовется автоматически.

### Код
```csharp
[SerializeField] private ReactivePropertyInt score = new(0);

void Start()
{
    score.AddListener(OnScoreChanged);
    score.Value = 10; // вызовет OnScoreChanged(10)
}

void OnScoreChanged(int newScore)
{
    Debug.Log($"Счёт: {newScore}");
}

void LoadFromSave(int savedScore)
{
    score.SetValueWithoutNotify(savedScore); // не вызовет событие
}
```

---

## См. также
- ← [Reactive](README.md)
