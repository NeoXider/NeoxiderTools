using System;
using R3;

namespace Neo.Runtime.Features.Money.Model
{
    /// <summary>
    /// Money model: Max == 0 => unlimited wallet (percentage is not calculated).
    /// </summary>
    public class MoneyModel : IDisposable
    {
        /// <summary>Current balance</summary>
        public ReactiveProperty<float> Balance { get; }

        /// <summary>Maximum limit; 0 => no limit</summary>
        public ReactiveProperty<float> Max { get; }

        /// <summary>Fill percentage (0..1). Always 0 when Max == 0</summary>
        public ReactiveProperty<float> Percent { get; }

        /// <summary>Event when limit is reached. Does not trigger when Max == 0</summary>
        public Observable<Unit> OnReachedMax { get; }

        /// <summary>
        /// Constructor for money model
        /// </summary>
        /// <param name="startAmount">Initial amount</param>
        /// <param name="maxAmount">Maximum limit (0 for unlimited)</param>
        public MoneyModel(float startAmount = 0f, float maxAmount = 0f)
        {
            // Negative max is treated as 0 (no limit)
            if (maxAmount < 0f)
            {
                maxAmount = 0f;
            }

            // Initial balance non-negative; clamp by Max only when Max > 0
            float init = Math.Max(0f, startAmount);
            if (maxAmount > 0f && init > maxAmount)
            {
                init = maxAmount;
            }

            Balance = new BindableReactiveProperty<float>(init);
            Max = new BindableReactiveProperty<float>(maxAmount);
            Percent = new BindableReactiveProperty<float>(0f);

            Balance.AsObservable().Subscribe(_ => Recalc());
            Max.AsObservable().Subscribe(_ => Recalc());

            OnReachedMax = Balance.AsObservable()
                .Where(_ => Max.Value > 0f && Balance.Value >= Max.Value)
                .Select(_ => Unit.Default);

            void Recalc()
            {
                if (Max.Value > 0f)
                {
                    Percent.Value = Balance.Value / Max.Value;
                }
                else
                {
                    Percent.Value = 0f;
                }
            }
        }

        /// <summary>
        /// Add funds to wallet
        /// </summary>
        /// <param name="amount">Amount to add</param>
        public void Add(float amount)
        {
            if (amount < 0f)
            {
                throw new ArgumentOutOfRangeException(nameof(amount));
            }

            if (Max.Value > 0f)
            {
                Balance.Value = Math.Min(Max.Value, Balance.Value + amount);
            }
            else
            {
                Balance.Value += amount;
            }
        }

        /// <summary>
        /// Spend funds from wallet
        /// </summary>
        /// <param name="amount">Amount to spend</param>
        /// <returns>True if successful, false if insufficient funds</returns>
        public bool Spend(float amount)
        {
            if (amount < 0f)
            {
                throw new ArgumentOutOfRangeException(nameof(amount));
            }

            if (Balance.Value < amount)
            {
                return false;
            }

            Balance.Value -= amount;
            return true;
        }

        /// <summary>
        /// Reset balance to zero
        /// </summary>
        public void Reset()
        {
            Balance.Value = 0f;
        }

        /// <summary>
        /// Set maximum limit; 0 => unlimited (percentage not calculated)
        /// </summary>
        /// <param name="newMax">New maximum limit</param>
        public void SetMax(float newMax)
        {
            if (newMax < 0f)
            {
                newMax = 0f;
            }

            Max.Value = newMax;

            if (Max.Value > 0f && Balance.Value > Max.Value)
            {
                Balance.Value = Max.Value;
            }
        }

        /// <summary>
        /// Set balance directly
        /// </summary>
        /// <param name="value">New balance value</param>
        public void SetBalance(float value)
        {
            if (value < 0f)
            {
                value = 0f;
            }

            if (Max.Value > 0f)
            {
                Balance.Value = Math.Min(Max.Value, value);
            }
            else
            {
                Balance.Value = value;
            }
        }

        /// <summary>
        /// Dispose resources
        /// </summary>
        public void Dispose()
        {
            Balance.Dispose();
            Max.Dispose();
            Percent.Dispose();
        }
    }
}
