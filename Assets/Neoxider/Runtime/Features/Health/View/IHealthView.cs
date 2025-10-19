using System;
using UnityEngine.Events;

namespace Neo.Runtime.Features.Health.View
{
    /// <summary>
    /// Interface for health view that defines UI update methods.
    /// </summary>
    public interface IHealthView
    {
        /// <summary>
        /// Update health values in UI
        /// </summary>
        /// <param name="currentHealth">Current health value</param>
        /// <param name="maxHealth">Maximum health value</param>
        void UpdateHealth(int currentHealth, int maxHealth);

        /// <summary>
        /// Show death state in UI
        /// </summary>
        /// <param name="isDead">Death status</param>
        void ShowDeath(bool isDead);

        /// <summary>
        /// Update maximum health value in UI
        /// </summary>
        /// <param name="maxHealth">Maximum health value</param>
        void UpdateMaxHealth(int maxHealth);

        /// <summary>
        /// Update health percentage in UI
        /// </summary>
        /// <param name="percentage">Health percentage value</param>
        void UpdateHealthPercentage(float percentage);

        /// <summary>
        /// Update health percent 100 in UI
        /// </summary>
        /// <param name="percent100">Health percent 100 value</param>
        void UpdateHealthPercent100(float percent100);

        /// <summary>
        /// Update health delta in UI
        /// </summary>
        /// <param name="delta">Health delta value</param>
        void UpdateHealthDelta(int delta);
    }
}