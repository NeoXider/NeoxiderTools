using System;
using System.Collections.Generic;

namespace Neo.Core.Resources
{
    /// <summary>
    ///     Pure logic: multiple resource pools by id. No UnityEngine.
    /// </summary>
    public sealed class ResourcePoolModel
    {
        private readonly Dictionary<string, float> _healTimer = new(StringComparer.OrdinalIgnoreCase);
        private readonly Dictionary<string, ResourcePoolEntry> _pools = new(StringComparer.OrdinalIgnoreCase);
        private readonly Dictionary<string, float> _regenAccum = new(StringComparer.OrdinalIgnoreCase);
        private readonly Dictionary<string, object> _registrationTokens = new(StringComparer.OrdinalIgnoreCase);
        private readonly List<(string Id, ResourcePoolEntry Entry, object Registration)> _tickBuffer = new();
        private bool _isTicking;

        public event Action<string, float, float> OnResourceChanged; // WHY: args are (id, current, max)
        public event Action<string> OnResourceDepleted;

        public float GetCurrent(string resourceId)
        {
            return TryGetPool(resourceId, out ResourcePoolEntry e) ? e.Current : 0f;
        }

        public float GetMax(string resourceId)
        {
            return TryGetPool(resourceId, out ResourcePoolEntry e) ? e.Max : 0f;
        }

        public bool IsDepleted(string resourceId)
        {
            return GetCurrent(resourceId) <= 0f;
        }

        public void AddPool(string resourceId, ResourcePoolEntry entry)
        {
            if (string.IsNullOrEmpty(resourceId) || entry == null)
            {
                return;
            }

            entry.Max = float.IsNaN(entry.Max) || float.IsInfinity(entry.Max) || entry.Max < 0f ? 0f : entry.Max;
            entry.Current = float.IsNaN(entry.Current) || float.IsInfinity(entry.Current) || entry.Current < 0f
                ? 0f
                : entry.Current;
            if (entry.Max > 0f && entry.Current > entry.Max)
            {
                entry.Current = entry.Max;
            }

            _pools[resourceId] = entry;
            // A fresh token distinguishes registrations even when the caller reuses the same entry.
            _registrationTokens[resourceId] = new object();
            _regenAccum[resourceId] = 0f;
            _healTimer[resourceId] = 0f;
        }

        public void RemovePool(string resourceId)
        {
            if (resourceId == null)
            {
                return;
            }

            _pools.Remove(resourceId);
            _registrationTokens.Remove(resourceId);
            _regenAccum.Remove(resourceId);
            _healTimer.Remove(resourceId);
        }

        /// <summary>
        ///     Checks the current balance, decrease limit, and effective stored decrease using the same rules as TrySpend.
        ///     Does not change values, timers, or events, and does not reserve the resource.
        ///     Finite nonpositive amounts succeed without requiring a registered resource.
        /// </summary>
        public bool CanSpend(string resourceId, float amount, out string failReason)
        {
            return ValidateSpend(resourceId, amount, out _, out failReason);
        }

        public bool TrySpend(string resourceId, float amount, out string failReason)
        {
            if (!ValidateSpend(resourceId, amount, out ResourcePoolEntry e, out failReason))
            {
                return false;
            }

            if (amount <= 0f)
            {
                return true;
            }

            float previous = e.Current;
            e.Current -= amount;
            if (e.Current < 0f)
            {
                e.Current = 0f;
            }

            bool becameDepleted = previous > 0f && e.Current <= 0f;
            NotifyChanged(resourceId, e);
            if (becameDepleted)
            {
                OnResourceDepleted?.Invoke(resourceId);
            }

            return true;
        }

        private bool ValidateSpend(string resourceId, float amount, out ResourcePoolEntry entry, out string failReason)
        {
            entry = null;
            failReason = null;
            if (float.IsNaN(amount) || float.IsInfinity(amount))
            {
                failReason = "Amount must be finite.";
                return false;
            }

            if (amount <= 0f)
            {
                return true;
            }

            if (!TryGetPool(resourceId, out entry))
            {
                failReason = "Unknown resource.";
                return false;
            }

            if (entry.Current < amount)
            {
                failReason = "Not enough resource.";
                return false;
            }

            if (ClampDecrease(entry, amount) < amount)
            {
                failReason = "Amount exceeds decrease limit.";
                return false;
            }

            float candidateCurrent = (float)(entry.Current - amount);
            if (!(candidateCurrent < entry.Current))
            {
                failReason = "Amount does not decrease resource.";
                return false;
            }

            return true;
        }

        public float Decrease(string resourceId, float amount)
        {
            if (float.IsNaN(amount) || float.IsInfinity(amount) || amount <= 0f ||
                !TryGetPool(resourceId, out ResourcePoolEntry e))
            {
                return 0f;
            }

            float previous = e.Current;
            e.Current -= ClampDecrease(e, amount);
            if (e.Current < 0f)
            {
                e.Current = 0f;
            }

            float actual = previous - e.Current;
            bool becameDepleted = previous > 0f && e.Current <= 0f;
            NotifyChanged(resourceId, e);
            if (becameDepleted)
            {
                OnResourceDepleted?.Invoke(resourceId);
            }

            return actual;
        }

        public float Increase(string resourceId, float amount)
        {
            if (float.IsNaN(amount) || float.IsInfinity(amount) || amount <= 0f ||
                !TryGetPool(resourceId, out ResourcePoolEntry e))
            {
                return 0f;
            }

            return ApplyIncrease(resourceId, e, ClampIncrease(e, amount));
        }

        private float ApplyIncrease(string resourceId, ResourcePoolEntry e, double amount)
        {
            if (e.Current <= 0f && !e.IgnoreCanHeal)
            {
                return 0f;
            }

            // WHY: Return the delta actually added to Current (capped by MaxIncreaseAmount AND by the
            // Max headroom), so callers like HealthComponent.OnHeal report the real heal, not an overheal.
            float before = e.Current;
            e.Current = (float)Math.Min(float.MaxValue, e.Current + amount);
            if (e.Max > 0f && e.Current > e.Max)
            {
                e.Current = e.Max;
            }

            float actual = e.Current - before;
            NotifyChanged(resourceId, e);
            return actual;
        }

        public void Restore(string resourceId)
        {
            if (!TryGetPool(resourceId, out ResourcePoolEntry e))
            {
                return;
            }

            e.Current = e.Max;
            NotifyChanged(resourceId, e);
        }

        /// <summary>
        ///     Directly sets the current value, clamped to [0, Max] when Max is positive (otherwise uncapped).
        ///     Non-finite values are ignored. Bypasses the regen/heal gates —
        ///     use for loading saved state, revives and scripted adjustments, not for normal healing.
        /// </summary>
        public void SetCurrent(string resourceId, float value)
        {
            if (float.IsNaN(value) || float.IsInfinity(value) || !TryGetPool(resourceId, out ResourcePoolEntry e))
            {
                return;
            }

            float clamped = value < 0f ? 0f : value;
            if (e.Max > 0f && clamped > e.Max)
            {
                clamped = e.Max;
            }

            e.Current = clamped;
            NotifyChanged(resourceId, e);
        }

        public void SetMax(string resourceId, float max)
        {
            if (float.IsNaN(max) || float.IsInfinity(max) || !TryGetPool(resourceId, out ResourcePoolEntry e))
            {
                return;
            }

            e.Max = max < 0f ? 0f : max;
            if (e.Max > 0f && e.Current > e.Max)
            {
                e.Current = e.Max;
            }

            NotifyChanged(resourceId, e);
        }

        /// <summary>
        ///     Tick regen and discrete heal timers. Call from component Update with deltaTime.
        /// </summary>
        public void Tick(float deltaTime)
        {
            if (_isTicking || float.IsNaN(deltaTime) || float.IsInfinity(deltaTime) || deltaTime <= 0f)
            {
                return;
            }

            _isTicking = true;
            try
            {
                // Snapshot registrations: callbacks can re-register the very same entry under its id.
                foreach (KeyValuePair<string, ResourcePoolEntry> pool in _pools)
                {
                    _tickBuffer.Add((pool.Key, pool.Value, _registrationTokens[pool.Key]));
                }

                for (int i = 0; i < _tickBuffer.Count; i++)
                {
                    (string id, ResourcePoolEntry e, object registration) = _tickBuffer[i];
                    if (!IsCurrentRegistration(id, registration))
                    {
                        continue;
                    }

                    if (e.RegenPerSecond > 0f && e.RegenInterval > 0f)
                    {
                        double accum = _regenAccum.GetValueOrDefault(id) + (double)deltaTime;
                        double cycles = Math.Floor(accum / e.RegenInterval);
                        _regenAccum[id] = (float)(accum % e.RegenInterval);
                        if (cycles > 0d)
                        {
                            ApplyIncrease(id, e, ClampIncrease(e, e.RegenPerSecond * (double)e.RegenInterval) * cycles);
                        }
                    }

                    if (!IsCurrentRegistration(id, registration))
                    {
                        continue;
                    }

                    if (e.HealDelay > 0f && e.HealAmount > 0f)
                    {
                        double timer = _healTimer.GetValueOrDefault(id) + (double)deltaTime;
                        double cycles = Math.Floor(timer / e.HealDelay);
                        _healTimer[id] = (float)(timer % e.HealDelay);
                        if (cycles > 0d)
                        {
                            ApplyIncrease(id, e, ClampIncrease(e, e.HealAmount) * cycles);
                        }
                    }
                }
            }
            finally
            {
                _tickBuffer.Clear();
                _isTicking = false;
            }
        }

        private static float ClampDecrease(ResourcePoolEntry e, float amount)
        {
            // WHY: never report more removed than the pool held (overkill), regardless of the limit setting.
            if (e.Current < amount)
            {
                amount = e.Current;
            }

            if (e.MaxDecreaseAmount >= 0f && amount > e.MaxDecreaseAmount)
            {
                amount = e.MaxDecreaseAmount;
            }

            return amount;
        }

        private static double ClampIncrease(ResourcePoolEntry e, double amount)
        {
            if (e.MaxIncreaseAmount >= 0f && amount > e.MaxIncreaseAmount)
            {
                amount = e.MaxIncreaseAmount;
            }

            // WHY: Max <= 0 means uncapped everywhere else; skipping the headroom clamp keeps
            // Increase from turning negative and draining the pool.
            if (e.Max > 0f)
            {
                double space = e.Max - (double)e.Current;
                if (space < amount)
                {
                    amount = space < 0f ? 0f : space;
                }
            }

            return amount;
        }

        private bool TryGetPool(string resourceId, out ResourcePoolEntry entry)
        {
            if (resourceId == null)
            {
                entry = null;
                return false;
            }

            return _pools.TryGetValue(resourceId, out entry);
        }

        private bool IsCurrentRegistration(string resourceId, object registration)
        {
            return _registrationTokens.TryGetValue(resourceId, out object current) &&
                   ReferenceEquals(registration, current);
        }

        private void NotifyChanged(string resourceId, ResourcePoolEntry e)
        {
            OnResourceChanged?.Invoke(resourceId, e.Current, e.Max);
        }
    }
}
