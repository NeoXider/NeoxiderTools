public interface IHealable
{
    void Heal(int amount);
}

public interface IDamageable
{
    void TakeDamage(int amount);
}

public interface IRestorable
{
    void Restore();
}

public interface IAttackable
{
    void Attack(int damage);
}