# Система логирования Neo.Runtime

> 📘 **См. также:** [CODING_GUIDELINES.md](../CODING_GUIDELINES.md) - полные правила оформления кода для Runtime

## Описание

Система логирования на основе **Serilog** с полной интеграцией в Unity через **VContainer DI**.

## Возможности

- ✅ **Структурированное логирование** - поддержка параметризированных сообщений
- ✅ **Dependency Injection** - автоматический инжект `Serilog.ILogger` во все классы
- ✅ **Unity Console Integration** - все логи отображаются в Unity Console
- ✅ **Файловое логирование** - опциональное сохранение в файл с ротацией
- ✅ **Настраиваемые уровни** - Verbose, Debug, Information, Warning, Error, Fatal
- ✅ **Фильтрация по namespace** - включение/отключение логов для конкретных модулей
- ✅ **Thread-safe** - безопасное использование из любых потоков

## 📋 Правила логирования

### Формат сообщений

**Обязательный формат:** `[ClassName.MethodName] Описание действия`

```csharp
// ✅ Правильно
_logger.Information("[WalletPresenter.Start] Binding {ViewCount} views to models", viewCount);
_logger.Debug("[WalletPresenter.OnBalanceChanged] Currency: {CurrencyId}, Balance: {Balance}", id, balance);
_logger.Warning("[WalletPresenter.OnReachedMax] Wallet full for currency: {CurrencyId}", id);

// ❌ Неправильно
_logger.Information("Binding views"); // Нет контекста
_logger.Debug("Balance changed to {Balance}", balance); // Нет класса и метода
```

### Выбор уровня логирования

| Уровень | Когда использовать | Пример |
|---------|-------------------|---------|
| **Debug** | Детальная информация для отладки, промежуточные значения | `[Shop.CalculateDiscount] Applying discount: {Percent}%` |
| **Information** | Важные события жизненного цикла, старт/стоп операций | `[WalletPresenter.Start] All views successfully bound` |
| **Warning** | Проблемы, которые не мешают работе, но требуют внимания | `[Wallet.Spend] Insufficient funds: {Required} > {Available}` |
| **Error** | Ошибки, которые влияют на функциональность | `[ItemFactory.Create] Failed to load item: {ItemId}` |
| **Fatal** | Критические ошибки, требующие немедленного внимания | `[SaveSystem.Load] Corrupted save file, cannot continue` |

### Когда логировать

✅ **Логируйте:**
- Инициализацию систем (конструкторы, Start)
- Важные изменения состояния (покупки, трансакции)
- Завершение операций (Dispose, Stop)
- Предупреждения о потенциальных проблемах
- Все ошибки с исключениями

❌ **Не логируйте:**
- Каждый кадр Update/FixedUpdate
- Мелкие UI события (hover, click без логики)
- Геттеры/сеттеры простых свойств
- Private методы без бизнес-логики

## Использование

### Инжект логгера через поля (Field Injection)

**⚠️ Используйте Field Injection вместо Constructor Injection:**

```csharp
using Serilog;
using VContainer;
using VContainer.Unity;

public class ShopService
{
    [Inject] private readonly ILogger _logger;
    [Inject] private readonly WalletModel _wallet;

    public bool BuyItem(string itemId, float price)
    {
        _logger.Information("[ShopService.BuyItem] Attempting to buy item: {ItemId} for {Price}", itemId, price);
        
        try
        {
            if (_wallet.Spend("money", price))
            {
                _logger.Information("[ShopService.BuyItem] Purchase successful: {ItemId}", itemId);
                return true;
            }
            
            _logger.Warning("[ShopService.BuyItem] Insufficient funds");
            return false;
        }
        catch (Exception ex)
        {
            _logger.Error(ex, "[ShopService.BuyItem] Failed to buy item: {ItemId}", itemId);
            throw;
        }
    }
}
```

### Примеры по уровням

```csharp
_logger.Debug("[ShopService.CalculatePrice] Base price: {Base}, Discount: {Discount}%", basePrice, discount);

_logger.Information("[WalletPresenter.Start] Binding {ViewCount} views to models", viewCount);
_logger.Information("[SaveSystem.Save] Game saved successfully at {Path}", path);

_logger.Warning("[Wallet.Spend] Insufficient funds for {ItemId}: Required {Required}, Available {Available}", 
    itemId, required, available);

_logger.Error("[ItemFactory.Create] Failed to instantiate item: {ItemId}", itemId);
_logger.Error(exception, "[NetworkManager.Connect] Connection failed to {ServerUrl}", url);

_logger.Fatal(exception, "[SaveSystem.Load] Cannot load save file, game cannot continue");
```

### Структурированные параметры

```csharp
_logger.Information("[Shop.Purchase] Item: {ItemId}, Price: {Price}, Currency: {Currency}", 
    itemId, price, currency);

_logger.Information($"[Shop.Purchase] Item: {itemId}, Price: {price}");

var transaction = new { ItemId = "sword", Price = 100, Currency = "gold" };
_logger.Information("[Shop.Purchase] Transaction: {@Transaction}", transaction);
```

- ✅ Плейсхолдеры `{}` - структурированные данные
- ❌ Интерполяция `$""` - теряется структурность
- `@` перед параметром - сериализация объекта в JSON

## Конфигурация

### LoggingConfig (ScriptableObject)

Создайте конфиг: `Create > Neo > Core > Logging Config`

**Основные настройки:**
- **Minimum Level** - минимальный уровень (Verbose/Debug/Information/Warning/Error/Fatal)
- **Enable Unity Console** - вывод в Unity Console
- **Enable File Logging** - сохранение в файл

**Файловые настройки:**
- **Max File Size MB** - размер до ротации (1-100 MB)
- **Retained File Count Limit** - сколько дней хранить (1-30)

**Фильтрация:**
- **Enabled Namespaces** - логировать только эти namespace (пусто = все)
- **Disabled Namespaces** - не логировать эти namespace

**Дополнительно:**
- **Show Source Context** - показывать имя класса `[ClassName]`
- **Show Timestamps** - показывать время `[HH:mm:ss]`

### Примеры фильтрации

```
Enabled Namespaces: 
  - Neo.Runtime.Features.Wallet
  - Neo.Shop
→ Логи только от Wallet и Shop

Disabled Namespaces:
  - Neo.Runtime.Features.Health
  - UnityEngine
→ Отключить логи от Health и Unity

Enabled: пусто, Disabled: пусто
→ Логировать всё
```

### Расположение файлов логов

`Application.persistentDataPath/logs/game-YYYYMMDD.log`

Пример: `C:/Users/User/AppData/LocalLow/CompanyName/GameName/logs/game-20250428.log`

## Примеры использования

### Пример: WalletPresenter (реальный код из проекта)

```csharp
using Serilog;
using VContainer;
using VContainer.Unity;

public class WalletPresenter : IStartable, IDisposable
{
    [Inject] private readonly WalletModel _wallet;
    [Inject] private readonly WalletConfig _config;
    [Inject] private readonly ILogger _logger;
    [Inject] private readonly IEnumerable<MoneyViewWithId> _views;

    private readonly CompositeDisposable _disp = new();

    public void Start()
    {
        foreach (var view in _views)
        {
            if (!_config.TryGet(view.CurrencyId, out _))
            {
                _logger.Warning("[WalletPresenter.Start] Unknown CurrencyId: {CurrencyId}", view.CurrencyId);
                continue;
            }

            var model = _wallet.Get(view.CurrencyId);
            
            model.Balance.Subscribe(balance => 
            {
                _logger.Debug("[WalletPresenter.OnBalanceChanged] Currency: {CurrencyId}, Balance: {Balance}", 
                    view.CurrencyId, balance);
                view.UpdateMoney(balance, model.Max.Value);
            }).AddTo(_disp);
        }
    }

    public void Dispose()
    {
        _disp.Dispose();
    }
}
```

### Пример: ShopService

```csharp
public class ShopService
{
    [Inject] private readonly WalletModel _wallet;
    [Inject] private readonly ILogger _logger;

    public bool BuyItem(string itemId, float price)
    {
        _logger.Information("[ShopService.BuyItem] Attempting purchase: {ItemId} for {Price}", itemId, price);
        
        if (_wallet.Spend("money", price))
        {
            _logger.Information("[ShopService.BuyItem] Purchase successful: {ItemId}", itemId);
            return true;
        }
        
        _logger.Warning("[ShopService.BuyItem] Insufficient funds: Required {Price}, Available {Balance}", 
            price, _wallet.Get("money").Balance.Value);
        return false;
    }
}
```

## Лучшие практики

1. **Всегда используйте формат [ClassName.MethodName]**
   ```csharp
   _logger.Information("[ShopService.BuyItem] Purchasing {ItemId}", itemId);
   _logger.Information("Purchasing item {ItemId}", itemId);
   ```
   ✅ С контекстом класса и метода  
   ❌ Без контекста

2. **Используйте плейсхолдеры вместо интерполяции**
   ```csharp
   _logger.Information("[Player.Score] {Name} scored {Points}", name, points);
   _logger.Information($"[Player.Score] {name} scored {points}");
   ```
   ✅ Структурированные параметры  
   ❌ Теряется структурность

3. **Логируйте исключения правильно**
   ```csharp
   try { }
   catch (Exception ex)
   {
       _logger.Error(ex, "[SaveSystem.Save] Failed to save {EntityType} with ID {EntityId}", type, id);
   }
   ```

4. **Выбирайте правильный уровень**
   - **Debug** - детали для отладки, можно отключить
   - **Information** - важные события
   - **Warning** - проблемы без критичных последствий
   - **Error** - ошибки, влияющие на функционал
   - **Fatal** - критические ошибки

5. **Не логируйте чувствительные данные**
   ```csharp
   _logger.Debug("[Auth.Login] Login: {Username} {Password}", username, password);
   _logger.Information("[Auth.Login] Login attempt for user: {Username}", username);
   ```
   ❌ Пароль в логах  
   ✅ Только публичная информация

## Архитектура

```
CoreLifetimeScope
    ↓ регистрирует
LoggerFactory.CreateLogger(LoggingConfig)
    ↓ создает
Serilog.ILogger
    ↓ инжектится через VContainer
Ваши классы (Presenters, Services, Models)
```

**Простота:** Никаких адаптеров, используется `Serilog.ILogger` напрямую!

## Расширение

Для добавления дополнительных Sinks в `LoggerFactory.cs`:

```csharp
public static ILogger CreateLogger(LoggingConfig config)
{
    var logConfig = new LoggerConfiguration()
        .MinimumLevel.Is(config.minimumLevel);

    if (config.enableUnityConsole)
    {
        logConfig.WriteTo.Sink(new UnityConsoleSink(config));
    }

    // Добавьте свой Sink
    logConfig.WriteTo.YourCustomSink();
    
    return logConfig.CreateLogger();
}
```

