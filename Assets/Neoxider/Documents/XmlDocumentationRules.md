# XML Documentation Rules for C# Files

## General Principles

All classes, methods and properties must have XML documentation.
Documentation must be written in English.
Follow the existing project structure.

## Formatting

### Basic Format
```csharp
/// <summary>
/// Brief description of what the method does
/// </summary>
/// <param name="parameterName">Description of parameter</param>
/// <returns>Description of return value (if applicable)</returns>
public void MethodName(string parameterName)
{
}
```

### Class Description
```csharp
/// <summary>
/// Brief description of class
/// </summary>
public class ClassName
{
    /// <summary>
    /// Brief description of property
    /// </summary>
    public int PropertyName { get; set; }
    
    /// <summary>
    /// Description of method
    /// </summary>
    /// <param name="parameter">Description of parameter</param>
    /// <returns>Description of return value</returns>
    public void MethodName(string parameter)
    {
        // Implementation
    }
}
```

## Requirements for XML Comments

1. Use only English language for descriptions
2. Write concise and clear descriptions
3. Specify method parameters using `<param>` tag
4. Describe return values using `<returns>` tag (when applicable)
5. Use proper spelling and punctuation

## XML Comment Structure

### <summary>
```csharp
/// <summary>
/// Brief description of element
/// </summary>
```

### <param>
```csharp
/// <param name="parameterName">
/// Description of parameter
/// </param>
```

### <returns>
```csharp
/// <returns>
/// Description of return value
/// </returns>
```

### <exception>
```csharp
/// <exception cref="System.ArgumentException">
/// Description of exception
/// </exception>
```

## Examples for Unity Project

### MoneyModel Class (How XML Should Look)
```csharp
/// <summary>
/// Money model for managing balance and limits
/// </summary>
public class MoneyModel : Singleton<MoneyModel>
{
    /// <summary>
    /// Current money balance
    /// </summary>
    public float Balance { get; private set; }
    
    /// <summary>
    /// Maximum limit (0 for unlimited)
    /// </summary>
    public float Max { get; private set; }
    
    /// <summary>
    /// Spend money
    /// </summary>
    /// <param name="amount">Amount to spend</param>
    /// <returns>True if successful, false otherwise</returns>
    public bool Spend(float amount)
    {
        // Implementation
    }
    
    /// <summary>
    /// Add money
    /// </summary>
    /// <param name="amount">Amount to add</param>
    public void Add(float amount)
    {
        // Implementation
    }
}
```

### MoneyPresenter Class (How XML Should Look)
```csharp
/// <summary>
/// Presenter for money management
/// </summary>
public class MoneyPresenter : IDisposable
{
    /// <summary>
    /// Presenter constructor
    /// </summary>
    /// <param name="model">Money model</param>
    /// <param name="view">View interface</param>
    [Inject]
    public MoneyPresenter(MoneyModel model, IMoneyView view)
    {
        // Implementation
    }
    
    /// <summary>
    /// Handle balance change
    /// </summary>
    /// <param name="balance">New balance</param>
    private void OnBalanceChanged(float balance)
    {
        // Implementation
    }
}
```

### IMoneyView Interface (How XML Should Look)
```csharp
/// <summary>
/// Interface for money view operations
/// </summary>
public interface IMoneyView
{
    /// <summary>
    /// Update money display
    /// </summary>
    /// <param name="balance">Current balance</param>
    /// <param name="max">Maximum limit</param>
    void UpdateMoney(float balance, float max);
    
    /// <summary>
    /// Show wallet full status
    /// </summary>
    /// <param name="full">True if wallet is full</param>
    void ShowWalletFull(bool full);
    
    /// <summary>
    /// Set limit mode
    /// </summary>
    /// <param name="hasLimit">True if there's a limit</param>
    void SetLimitMode(bool hasLimit);
}
```

### MoneyView Class (How XML Should Look)
```csharp
/// <summary>
/// View for money system
/// </summary>
public class MoneyView : MonoBehaviour, IMoneyView
{
    /// <summary>
    /// Current balance
    /// </summary>
    [SerializeField] private float balance;
    
    /// <summary>
    /// Maximum limit
    /// </summary>
    [SerializeField] private float max;
    
    /// <summary>
    /// Flag indicating if there's a limit
    /// </summary>
    [SerializeField] private bool hasLimit;
    
    /// <summary>
    /// Event for balance change
    /// </summary>
    public UnityEvent OnMoneyChangedEvent;
    
    /// <summary>
    /// Event for wallet full status
    /// </summary>
    public UnityEvent<bool> OnWalletFullEvent; 
    
    /// <summary>
    /// Event for limit mode change
    /// </summary>
    public UnityEvent<bool> OnLimitModeChangedEvent;
}
```

## Writing Recommendations

### For Methods:
- Always describe parameters using `<param>`
- Describe return values using `<returns>` (when applicable)
- Write clear and concise descriptions
- Provide useful information for other developers

### For Properties:
- Briefly describe the purpose of the property
- Specify what value is stored in the property

### For Classes:
- Describe the main purpose of the class
- Mention key features of usage

## Special Cases Examples

### Methods with Exceptions
```csharp
/// <summary>
/// Perform an action
/// </summary>
/// <param name="value">Value to process</param>
/// <exception cref="System.ArgumentNullException">
/// Thrown when value is null
/// </exception>
public void DoSomething(string value)
{
    if (value == null) throw new ArgumentNullException(nameof(value));
    // Implementation
}
```

### Properties with Description
```csharp
/// <summary>
/// Returns current game level
/// </summary>
public int CurrentLevel { get; private set; }
```
