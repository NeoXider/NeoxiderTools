# Reactive

Модуль `Reactive` содержит маленькие реактивные свойства: Inspector-friendly типы для `float`, `int` и `bool`, плюс code-first generic `ReactiveProperty<T>` для любых C# типов.

## Что входит

- `ReactivePropertyFloat`
- `ReactivePropertyInt`
- `ReactivePropertyBool`
- `ReactiveProperty<T>` для code-first сценариев
- типизированные события `UnityEventFloat`, `UnityEventInt`, `UnityEventBool`

## Когда использовать

- Нужно хранить значение и реагировать на его изменение без написания отдельного `MonoBehaviour`.
- Нужно подписывать Inspector-события на изменение простого значения.
- Нужно при загрузке данных установить значение без немедленного вызова событий.

## API

У всех типов одинаковый базовый контракт:

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

## Generic

`ReactiveProperty<T>` поддерживает любые value/reference типы в C# коде:

```csharp
private readonly ReactiveProperty<string> state = new("Idle");

private void Awake()
{
    state.AddListener(OnStateChanged);
    state.Value = "Run";
}
```

Для сериализации в Inspector используйте конкретные wrappers (`ReactivePropertyFloat`, `ReactivePropertyInt`, `ReactivePropertyBool`) или создавайте свой concrete-класс поверх `ReactivePropertyBase<T, TEvent>`. Unity плохо поддерживает открытые generic-типы в сериализованных полях.

## Mirror

`ReactiveProperty<T>` не делает тип автоматически сетевым. Для Mirror используйте `[SyncVar(hook = ...)]` на стороне `NetworkBehaviour` и прокидывайте значение через `NetworkReactivePropertyBridge.SetFromNetwork(...)`.

Важно: generic bridge принимает любой `T` только на стороне Reactive API. Mirror синхронизирует только типы, которые поддержаны SyncVar-сериализатором Mirror, либо типы с зарегистрированными custom serializers.

## Ограничения

- Модуль не реализует полноценные reactive-цепочки, operators или `IObservable`.
- Inspector-friendly wrappers в текущей сборке есть только для `float`, `int` и `bool`.
- Это сериализуемые utility-типы, а не отдельные `MonoBehaviour` компоненты.
