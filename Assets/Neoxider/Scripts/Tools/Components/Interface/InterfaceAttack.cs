/// <summary>
/// Определяет, что объект может быть излечен.
/// </summary>
public interface IHealable
{
    /// <summary>
    /// Восстанавливает указанное количество здоровья.
    /// </summary>
    /// <param name="amount">Количество восстанавливаемого здоровья.</param>
    void Heal(int amount);
}

/// <summary>
/// Определяет, что объект может получать урон.
/// </summary>
public interface IDamageable
{
    /// <summary>
    /// Наносит указанное количество урона.
    /// </summary>
    /// <param name="amount">Количество получаемого урона.</param>
    void TakeDamage(int amount);
}

/// <summary>
/// Определяет, что состояние объекта может быть полностью восстановлено.
/// </summary>
public interface IRestorable
{
    /// <summary>
    /// Полностью восстанавливает состояние объекта (например, здоровье до максимума).
    /// </summary>
    void Restore();
}

/// <summary>
/// Определяет, что объект может выполнять атаку.
/// </summary>
public interface IAttackable
{
    /// <summary>
    /// Инициирует атаку с указанным уроном.
    /// </summary>
    /// <param name="damage">Урон, который будет нанесен атакой.</param>
    void Attack(int damage);
}
