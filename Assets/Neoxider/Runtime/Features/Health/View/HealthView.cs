using UnityEngine;
using UnityEngine.Events;

namespace Neo.Runtime.Features.Health.View
{
    /// <summary>
    /// View component for health system that handles UI updates and events.
    /// </summary>
    public class HealthView : MonoBehaviour, IHealthView
    {
        [SerializeField] private int currentHealth;
        [SerializeField] private int maxHealth;
        [SerializeField] private float healthPercentage;
        [SerializeField] private float healthPercent100;
        [SerializeField] private int healthDelta;

        /// <summary>
        /// Event triggered when health changes
        /// </summary>
        public UnityEvent OnHealthChangedEvent;
        
        /// <summary>
        /// Event triggered when player dies
        /// </summary>
        public UnityEvent<bool> OnPlayerDiedEvent;
        
        /// <summary>
        /// Event triggered when maximum health changes
        /// </summary>
        public UnityEvent<int> OnMaxHealthChangedEvent;
        
        /// <summary>
        /// Event triggered when health percentage changes
        /// </summary>
        public UnityEvent<float> OnHealthPercentageChangedEvent;
        
        /// <summary>
        /// Event triggered when health percent 100 changes
        /// </summary>
        public UnityEvent<float> OnHealthPercent100ChangedEvent;
        
        /// <summary>
        /// Event triggered when health delta changes
        /// </summary>
        public UnityEvent<int> OnHealthDeltaChangedEvent;

        /// <summary>
        /// Initialize the view component
        /// </summary>
        private void Awake()
        {
        }

        public void UpdateHealth(int currentHealth, int maxHealth)
        {
            this.currentHealth = currentHealth;
            this.maxHealth = maxHealth;

            if (maxHealth > 0)
            {
                healthPercentage = (float)currentHealth / maxHealth;
            }
            else
            {
                healthPercentage = 0f;
            }

            healthPercent100 = healthPercentage * 100f;

            OnHealthChangedEvent?.Invoke();
        }

        public void ShowDeath(bool isDead)
        {
            OnPlayerDiedEvent?.Invoke(isDead);
        }

        public void UpdateMaxHealth(int maxHealth)
        {
            this.maxHealth = maxHealth;
            OnMaxHealthChangedEvent?.Invoke(maxHealth);
        }

        public void UpdateHealthPercentage(float percentage)
        {
            healthPercentage = percentage;
            OnHealthPercentageChangedEvent?.Invoke(percentage);
        }

        public void UpdateHealthPercent100(float percent100)
        {
            healthPercent100 = percent100;
            OnHealthPercent100ChangedEvent?.Invoke(percent100);
        }

        public void UpdateHealthDelta(int delta)
        {
            healthDelta = delta;
            OnHealthDeltaChangedEvent?.Invoke(delta);
        }
    }
}
