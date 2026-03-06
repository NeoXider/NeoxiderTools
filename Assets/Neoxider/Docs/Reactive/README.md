# Reactive

Модуль `Reactive` содержит сериализуемые реактивные свойства для `float`, `int` и `bool`, которые можно хранить в полях Unity-объектов и связывать с `UnityEvent`.

## Что входит

- `ReactivePropertyFloat`
- `ReactivePropertyInt`
- `ReactivePropertyBool`
- типизированные события `UnityEventFloat`, `UnityEventInt`, `UnityEventBool`

## Когда использовать

- Нужно хранить значение и реагировать на его изменение без написания отдельного `MonoBehaviour`.
- Нужно подписывать Inspector-события на изменение простого значения.
- Нужно при загрузке данных установить значение без немедленного вызова событий.

## API

У всех трёх типов одинаковый базовый контракт:

- `CurrentValue` - текущее значение только для чтения
- `Value` - свойство чтения и записи; при `set` вызывает `OnChanged`
- `OnChanged` - типизированный `UnityEvent`
- `AddListener(...)`, `RemoveListener(...)`, `RemoveAllListeners()` - удобные обёртки над `UnityEvent`
- `OnNext(value)` - установить значение и уведомить подписчиков
- `SetValueWithoutNotify(value)` - записать значение без события
- `ForceNotify()` - повторно отправить текущее значение в `OnChanged`

## Пример

```csharp
[SerializeField] private ReactivePropertyInt health = new(100);

private void Awake()
{
    health.AddListener(OnHealthChanged);
}

private void OnHealthChanged(int currentHealth)
{
    Debug.Log($"Health changed: {currentHealth}");
}

public void ApplyDamage(int amount)
{
    health.Value -= amount;
}
```

## Ограничения

- Модуль не реализует полноценные reactive-цепочки, operators или `IObservable`.
- В текущей сборке есть только реализации для `float`, `int` и `bool`.
- Это сериализуемые utility-типы, а не отдельные `MonoBehaviour` компоненты.
