using UnityEngine;
using UnityEngine.Events;

namespace Neo.Runtime.Features.Money.View
{
    /// <summary>
    /// View for money system
    /// </summary>
    public class MoneyView : MonoBehaviour, IMoneyView
    {
        [Header("Values")] [SerializeField] private float balance;
        [SerializeField] private float max;
        [SerializeField] private float percent; // 0..1
        [SerializeField] private float percent100; // 0..100
        [SerializeField] private float delta;
        [SerializeField] private bool hasLimit;

        [Header("Events")]
        /// <summary>
        /// Event when money changes
        /// </summary>
        public UnityEvent OnMoneyChangedEvent;

        /// <summary>
        /// Event when wallet full state changes
        /// </summary>
        public UnityEvent<bool> OnWalletFullEvent;

        /// <summary>
        /// Event when maximum money changes
        /// </summary>
        public UnityEvent<float> OnMaxMoneyChangedEvent;

        /// <summary>
        /// Event when money percentage changes (use only when hasLimit == true)
        /// </summary>
        public UnityEvent<float> OnMoneyPercentageChangedEvent; // use only when hasLimit == true

        /// <summary>
        /// Event when money percent 100 changes (use only when hasLimit == true)
        /// </summary>
        public UnityEvent<float> OnMoneyPercent100ChangedEvent; // use only when hasLimit == true

        /// <summary>
        /// Event when money delta changes
        /// </summary>
        public UnityEvent<float> OnMoneyDeltaChangedEvent;

        /// <summary>
        /// Event when limit mode changes (show/hide percent UI)
        /// </summary>
        public UnityEvent<bool> OnLimitModeChangedEvent; // show/hide percent UI

        /// <summary>
        /// Set limit mode
        /// true - has limit, false - unlimited (hide percentages)
        /// </summary>
        /// <param name="hasLimit">True if has limit</param>
        public void SetLimitMode(bool hasLimit)
        {
            this.hasLimit = hasLimit;
            if (!hasLimit)
            {
                percent = 0f;
                percent100 = 0f;
            }

            OnLimitModeChangedEvent?.Invoke(hasLimit);
        }

        /// <summary>
        /// Update money display
        /// </summary>
        /// <param name="balance">Current balance</param>
        /// <param name="max">Maximum limit</param>
        public void UpdateMoney(float balance, float max)
        {
            this.balance = balance;
            this.max = max;

            if (hasLimit && max > 0f)
            {
                percent = max > 0f ? balance / max : 0f;
                percent100 = percent * 100f;
            }
            else
            {
                percent = 0f;
                percent100 = 0f;
            }

            OnMoneyChangedEvent?.Invoke();
        }

        /// <summary>
        /// Show wallet full indicator
        /// Responds only when limit is active
        /// </summary>
        /// <param name="full">True if wallet is full</param>
        public void ShowWalletFull(bool full)
        {
            OnWalletFullEvent?.Invoke(full);
        }

        /// <summary>
        /// Update maximum money display
        /// </summary>
        /// <param name="max">Maximum limit</param>
        public void UpdateMaxMoney(float max)
        {
            this.max = max;
            OnMaxMoneyChangedEvent?.Invoke(max);
        }

        /// <summary>
        /// Update money percentage display (0..1)
        /// Called only when hasLimit == true
        /// </summary>
        /// <param name="percentage">Percentage value 0..1</param>
        public void UpdateMoneyPercentage(float percentage)
        {
            if (!hasLimit)
            {
                return;
            }

            percent = percentage;
            OnMoneyPercentageChangedEvent?.Invoke(percentage);
        }

        /// <summary>
        /// Update money percent display (0..100)
        /// Called only when hasLimit == true
        /// </summary>
        /// <param name="p100">Percent value 0..100</param>
        public void UpdateMoneyPercent100(float p100)
        {
            if (!hasLimit)
            {
                return;
            }

            percent100 = p100;
            OnMoneyPercent100ChangedEvent?.Invoke(p100);
        }

        /// <summary>
        /// Update money delta (if used)
        /// </summary>
        /// <param name="delta">Delta value</param>
        public void UpdateMoneyDelta(float delta)
        {
            this.delta = delta;
            OnMoneyDeltaChangedEvent?.Invoke(delta);
        }
    }
}