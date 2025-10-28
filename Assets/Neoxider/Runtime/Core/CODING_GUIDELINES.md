# Правила оформления кода Neo.Runtime

## Dependency Injection

### ✅ Field Injection (Рекомендуется)

Используйте Field Injection с атрибутом `[Inject]` на полях:

```csharp
public class WalletPresenter : IStartable, IDisposable
{
    [Inject] private readonly WalletModel _wallet;
    [Inject] private readonly WalletConfig _config;
    [Inject] private readonly ILogger _logger;
    [Inject] private readonly IEnumerable<MoneyViewWithId> _views;

    private readonly CompositeDisposable _disp = new();

    public void Start()
    {
        _logger.Information("[WalletPresenter.Start] Started");
    }
}
```

**Преимущества:**
- ✅ Меньше boilerplate кода
- ✅ Проще читать и поддерживать
- ✅ Автоматическая валидация VContainer
- ✅ Легче добавлять новые зависимости

### ❌ Constructor Injection (Не рекомендуется)

Избегайте инжекта через конструктор:

```csharp
// ❌ Плохо - много boilerplate
[Inject]
public WalletPresenter(WalletModel wallet, WalletConfig config, ILogger logger)
{
    _wallet = wallet ?? throw new ArgumentNullException(nameof(wallet));
    _config = config ?? throw new ArgumentNullException(nameof(config));
    _logger = logger ?? throw new ArgumentNullException(nameof(logger));
}
```

## Структура класса

### Порядок членов класса

```csharp
public class ExampleClass : IStartable, IDisposable
{
    [Inject] private readonly ILogger _logger;
    [Inject] private readonly SomeModel _model;
    
    private readonly CompositeDisposable _disp = new();
    private int _counter;
    
    public int Counter => _counter;
    
    private void Awake() { }
    private void Start() { }
    
    void IStartable.Start() { }
    
    public void DoSomething() { }
    
    private void HelperMethod() { }
    
    public void Dispose() { }
}
```

**Порядок членов:**
1. Инжектируемые зависимости
2. Приватные поля
3. Публичные свойства
4. Unity lifecycle методы
5. VContainer lifecycle методы
6. Публичные методы
7. Приватные методы
8. Cleanup (Dispose)

## Логирование

### Формат сообщений

**Обязательный формат:** `[ClassName.MethodName] Описание`

```csharp
_logger.Information("[WalletPresenter.Start] Binding {ViewCount} views", count);
_logger.Debug("[ShopService.CalculatePrice] Price: {Price}, Discount: {Discount}%", price, discount);
_logger.Warning("[Wallet.Spend] Insufficient funds: {Required} > {Available}", req, avail);
_logger.Error(ex, "[SaveSystem.Load] Failed to load save file: {Path}", path);
```

### Уровни логирования

| Уровень | Использование |
|---------|---------------|
| **Debug** | Детальная отладочная информация |
| **Information** | Жизненный цикл системы (Start, Stop, Init) |
| **Warning** | Проблемы без критичных последствий |
| **Error** | Ошибки, влияющие на функционал |
| **Fatal** | Критические ошибки системы |

### Когда логировать

✅ **Логируйте:**
- Важные операции (покупки, сохранения)
- Предупреждения и ошибки

❌ **Не логируйте:**
- Lifecycle события (Start, Dispose) обычно бесполезно
- Update/FixedUpdate каждый кадр
- Геттеры/сеттеры
- Приватные helper методы

## Naming Conventions

### Поля

```csharp
private readonly ILogger _logger;
private int _counter;

[Inject] private readonly WalletModel _wallet;
```

- Приватные поля: `camelCase` с подчеркиванием `_`
- Инжектируемые поля: всегда `readonly`

### Методы

```csharp
public void DoSomething() { }

private void HelperMethod() { }

private void OnBalanceChanged(float balance) { }
```

- Public методы: `PascalCase`
- Private методы: `PascalCase`
- Event handlers: префикс `On`

### Классы

```csharp
public class WalletPresenter { }
public class MoneyModel { }
public interface ILogger { }
```

- Классы: `PascalCase`
- Интерфейсы: `I` + `PascalCase`

## Reactive Programming (R3)

### Подписки

Всегда используйте `CompositeDisposable` для управления подписками:

```csharp
public class ExamplePresenter : IDisposable
{
    [Inject] private readonly SomeModel _model;
    
    private readonly CompositeDisposable _disp = new();

    public void Start()
    {
        _model.Value
            .AsObservable()
            .Subscribe(OnValueChanged)
            .AddTo(_disp);
    }

    private void OnValueChanged(int value)
    {
    }

    public void Dispose()
    {
        _disp.Dispose();
    }
}
```

## Документация и комментарии

### XML комментарии

**Обязательны для:**
- Публичных классов
- Публичных методов
- Публичных свойств
- Интерфейсов

```csharp
/// <summary>
/// Presenter for wallet system that manages currency views.
/// </summary>
public class WalletPresenter : IStartable, IDisposable
{
    /// <summary>
    /// Starts the presenter and binds all views to models.
    /// </summary>
    public void Start()
    {
    }
}
```

### Обычные комментарии

**❌ Не используйте:**
```csharp
// Плохо - очевидные комментарии
private int _counter; // Счетчик

// Плохо - описание что делает код (должно быть понятно из кода)
// Увеличиваем счетчик на 1
_counter++;

// Плохо - устаревшие комментарии
// Временное решение (написано 2 года назад)
```

**✅ Используйте только для:**

1. **Сложной неочевидной логики** с объяснением "почему так":
```csharp
// Используем binary search вместо linear, т.к. список отсортирован
// и содержит >10000 элементов. Производительность критична здесь.
int index = BinarySearch(sortedList, target);
```

2. **TODO задачи:**
```csharp
// TODO: Добавить кэширование после оптимизации памяти (задача #1234)
// TODO: Рефакторинг в следующей версии - убрать legacy код
```

3. **Важные предупреждения:**
```csharp
// WARNING: Не вызывать из Update - очень дорогая операция!
public void RecalculateAllPaths() { }

// HACK: Временный workaround для Unity bug #12345. Удалить после Unity 2024.2
```

**Правило:** Если можно переписать код так, чтобы он был понятен без комментария - переписывайте, не комментируйте!

## SOLID принципы

### Single Responsibility

Один класс - одна ответственность:

```csharp
public class WalletPresenter { }

public class WalletModel { }

public class WalletPresenterAndModel { }
```

- ✅ WalletPresenter - только презентация
- ✅ WalletModel - только бизнес-логика  
- ❌ WalletPresenterAndModel - смешанная ответственность

### Dependency Inversion

Зависимости через абстракции:

```csharp
[Inject] private readonly ILogger _logger;

private readonly UnityDebugLogger _logger;
```

- ✅ Зависимость через интерфейс
- ❌ Прямая зависимость на конкретный класс

## Error Handling

### Исключения

```csharp
try
{
}
catch (Exception ex)
{
    _logger.Error(ex, "[ClassName.MethodName] Operation failed with {Param}", param);
    throw;
}
```

### Валидация

VContainer автоматически проверяет инжектируемые зависимости:

```csharp
[Inject] private readonly ILogger _logger;

public void DoWork()
{
    _logger.Information("[Class.DoWork] Working");
}
```

VContainer гарантирует, что инжектируемые поля не `null` - проверки не нужны.

## Примеры

### Полный пример Presenter

```csharp
using System;
using Serilog;
using R3;
using VContainer;
using VContainer.Unity;

namespace Neo.Runtime.Features.Example
{
    /// <summary>
    /// Presenter for example feature.
    /// </summary>
    public class ExamplePresenter : IStartable, IDisposable
    {
        [Inject] private readonly ExampleModel _model;
        [Inject] private readonly IExampleView _view;
        [Inject] private readonly ILogger _logger;

        private readonly CompositeDisposable _disp = new();

        public void Start()
        {
            _model.Value
                .AsObservable()
                .Subscribe(OnValueChanged)
                .AddTo(_disp);
        }

        private void OnValueChanged(int value)
        {
            _logger.Debug("[ExamplePresenter.OnValueChanged] Value changed: {Value}", value);
            _view.UpdateValue(value);
        }

        public void Dispose()
        {
            _disp.Dispose();
        }
    }
}
```

### Полный пример Model

```csharp
using System;
using R3;

namespace Neo.Runtime.Features.Example
{
    /// <summary>
    /// Model for example feature with reactive properties.
    /// </summary>
    public class ExampleModel : IDisposable
    {
        public ReactiveProperty<int> Value { get; }

        public ExampleModel(int initialValue = 0)
        {
            Value = new BindableReactiveProperty<int>(initialValue);
        }

        public void Increment()
        {
            Value.Value++;
        }

        public void Dispose()
        {
            Value.Dispose();
        }
    }
}
```

## Инструменты

### Рекомендуемые расширения

- **ReSharper** / **Rider** - для рефакторинга и code analysis
- **SonarLint** - для статического анализа
- **Code Cleanup** - автоформатирование

### Code Style Settings

Используйте `.editorconfig` для единообразия:

```ini
# Отступы
indent_style = space
indent_size = 4

# Переносы строк
end_of_line = crlf

# C# правила
csharp_new_line_before_open_brace = all
csharp_prefer_braces = true:warning
```

---

**Следуйте этим правилам для поддержания чистого и консистентного кода!**

