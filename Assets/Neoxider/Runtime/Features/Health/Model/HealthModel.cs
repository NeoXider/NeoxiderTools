using System;
using R3;

namespace Neoxider.Runtime.Features.Health.Model
{
    /// <summary>
    /// Health model for character with reactive properties and events.
    /// </summary>
    public class HealthModel
    {
        /// <summary>
        /// Current health value
        /// </summary>
        public ReactiveProperty<int> CurrentCurrent { get; }
        
        /// <summary>
        /// Maximum health value
        /// </summary>
        public ReactiveProperty<int> Max { get; }
        
        /// <summary>
        /// Percentage of current health from maximum (0-1)
        /// </summary>
        public ReactiveProperty<float> Percent { get; }
        
        /// <summary>
        /// Event that occurs when health reaches zero
        /// </summary>
        public Observable<Unit> OnDead { get; }

        /// <summary>
        /// Constructor for health model
        /// </summary>
        /// <param name="maxHp">Maximum health points</param>
        /// <param name="hp">Initial health points (default 0)</param>
        public HealthModel(int maxHp, int hp = 0)
        {
            if (maxHp <= 0)
            {
                hp = maxHp;
            }

            int newHp = hp > maxHp || hp == 0 ? maxHp : hp;
            CurrentCurrent = new BindableReactiveProperty<int>(newHp);
            Max = new BindableReactiveProperty<int>(maxHp);
            Percent = new BindableReactiveProperty<float>();

            CurrentCurrent.AsObservable().Subscribe(_ => Recalc());
            Max.AsObservable().Subscribe(_ => Recalc());
            OnDead = CurrentCurrent.AsObservable()
                .Where(h => h <= 0)
                .Select(_ => Unit.Default);

            void Recalc()
            {
                Percent.Value = (float)CurrentCurrent.Value / Max.Value;
            }
        }

        /// <summary>
        /// Deal damage to character
        /// </summary>
        /// <param name="amount">Damage amount</param>
        public void TakeDamage(int amount)
        {
            if (amount < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(amount));
            }

            CurrentCurrent.Value = Math.Max(0, CurrentCurrent.Value - amount);
        }

        /// <summary>
        /// Heal the character
        /// </summary>
        /// <param name="amount">Healing amount</param>
        public void Heal(int amount)
        {
            if (amount < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(amount));
            }

            CurrentCurrent.Value = Math.Min(Max.Value, CurrentCurrent.Value + amount);
        }

        /// <summary>
        /// Restore health to maximum level
        /// </summary>
        public void Restore()
        {
            CurrentCurrent.Value = Max.Value;
        }

        /// <summary>
        /// Set new maximum health value
        /// </summary>
        /// <param name="newMax">New maximum value</param>
        public void SetMaxHealth(int newMax)
        {
            if (newMax <= 0)
            {
                throw new ArgumentOutOfRangeException(nameof(newMax));
            }

            Max.Value = newMax;
        }

        /// <summary>
        /// Set specific health amount
        /// </summary>
        /// <param name="health">Health amount to set</param>
        public void SetHealth(int health)
        {
            if (health < 0)
            {
                health = 0;
            }

            CurrentCurrent.Value = Math.Min(Max.Value, health);
        }

        /// <summary>
        /// Dispose model resources
        /// </summary>
        public void Dispose()
        {
            CurrentCurrent.Dispose();
            Max.Dispose();
            Percent.Dispose();
        }
    }
}
