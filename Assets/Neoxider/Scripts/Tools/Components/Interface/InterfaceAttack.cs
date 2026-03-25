/// <summary>
///     Marks an object as healable.
/// </summary>
public interface IHealable
{
    /// <summary>
    ///     Restores the specified amount of health.
    /// </summary>
    /// <param name="amount">Amount of health to restore.</param>
    void Heal(int amount);
}

/// <summary>
///     Marks an object as able to take damage.
/// </summary>
public interface IDamageable
{
    /// <summary>
    ///     Applies the specified amount of damage.
    /// </summary>
    /// <param name="amount">Damage amount.</param>
    void TakeDamage(int amount);
}

/// <summary>
///     Marks an object whose state can be fully restored.
/// </summary>
public interface IRestorable
{
    /// <summary>
    ///     Fully restores the object's state (e.g. health to maximum).
    /// </summary>
    void Restore();
}

/// <summary>
///     Marks an object that can perform an attack.
/// </summary>
public interface IAttackable
{
    /// <summary>
    ///     Initiates an attack with the specified damage.
    /// </summary>
    /// <param name="damage">Damage dealt by the attack.</param>
    void Attack(int damage);
}
