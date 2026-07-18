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

        public event Action<string, float, float> OnResourceChanged; // WHY: args are (id, current, max)
        public event Action<string> OnResourceDepleted;

        public float GetCurrent(string resourceId)
        {
            return _pools.TryGetValue(resourceId, out ResourcePoolEntry e) ? e.Current : 0f;
        }

        public float GetMax(string resourceId)
        {
            return _pools.TryGetValue(resourceId, out ResourcePoolEntry e) ? e.Max : 0f;
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

            _pools[resourceId] = entry;
            _regenAccum[resourceId] = 0f;
            _healTimer[resourceId] = 0f;
        }

        public void RemovePool(string resourceId)
        {
            _pools.Remove(resourceId);
            _regenAccum.Remove(resourceId);
            _healTimer.Remove(resourceId);
        }

        public bool TrySpend(string resourceId, float amount, out string failReason)
        {
            failReason = null;
            if (amount <= 0f)
            {
                return true;
            }

            if (!_pools.TryGetValue(resourceId, out ResourcePoolEntry e))
            {
                failReason = "Unknown resource.";
                return false;
            }

            if (e.Current < amount)
            {
                failReason = "Not enough resource.";
                return false;
            }

            float previous = e.Current;
            float actual = ClampDecrease(e, amount);
            e.Current -= actual;
            if (e.Current < 0f)
            {
                e.Current = 0f;
            }

            NotifyChanged(resourceId, e);
            if (previous > 0f && e.Current <= 0f)
            {
                OnResourceDepleted?.Invoke(resourceId);
            }

            return true;
        }

        public float Decrease(string resourceId, float amount)
        {
            if (amount <= 0f || !_pools.TryGetValue(resourceId, out ResourcePoolEntry e))
            {
                return 0f;
            }

            float previous = e.Current;
            float actual = ClampDecrease(e, amount);
            e.Current -= actual;
            if (e.Current < 0f)
            {
                e.Current = 0f;
            }

            NotifyChanged(resourceId, e);
            if (previous > 0f && e.Current <= 0f)
            {
                OnResourceDepleted?.Invoke(resourceId);
            }

            return actual;
        }

        public float Increase(string resourceId, float amount)
        {
            if (amount <= 0f || !_pools.TryGetValue(resourceId, out ResourcePoolEntry e))
            {
                return 0f;
            }

            if (e.Current <= 0f && !e.IgnoreCanHeal)
            {
                return 0f;
            }

            float actual = ClampIncrease(e, amount);
            e.Current += actual;
            if (e.Max > 0f && e.Current > e.Max)
            {
                e.Current = e.Max;
            }

            NotifyChanged(resourceId, e);
            return actual;
        }

        public void Restore(string resourceId)
        {
            if (!_pools.TryGetValue(resourceId, out ResourcePoolEntry e))
            {
                return;
            }

            e.Current = e.Max;
            NotifyChanged(resourceId, e);
        }

        /// <summary>
        ///     Directly sets the current value, clamped to [0, Max]. Bypasses the regen/heal gates —
        ///     use for loading saved state, revives and scripted adjustments, not for normal healing.
        /// </summary>
        public void SetCurrent(string resourceId, float value)
        {
            if (!_pools.TryGetValue(resourceId, out ResourcePoolEntry e))
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
            if (!_pools.TryGetValue(resourceId, out ResourcePoolEntry e))
            {
                return;
            }

            e.Max = max < 0f ? 0f : max;
            if (e.Current > e.Max)
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
            foreach (KeyValuePair<string, ResourcePoolEntry> kv in _pools)
            {
                string id = kv.Key;
                ResourcePoolEntry e = kv.Value;

                if (e.RegenPerSecond > 0f && e.RegenInterval > 0f)
                {
                    _regenAccum[id] = _regenAccum.GetValueOrDefault(id) + deltaTime;
                    if (_regenAccum[id] >= e.RegenInterval)
                    {
                        _regenAccum[id] = 0f;
                        Increase(id, e.RegenPerSecond * e.RegenInterval);
                    }
                }

                if (e.HealDelay > 0f && e.HealAmount > 0f)
                {
                    _healTimer[id] = _healTimer.GetValueOrDefault(id) + deltaTime;
                    if (_healTimer[id] >= e.HealDelay)
                    {
                        _healTimer[id] = 0f;
                        Increase(id, e.HealAmount);
                    }
                }
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

        private static float ClampIncrease(ResourcePoolEntry e, float amount)
        {
            if (e.MaxIncreaseAmount < 0f)
            {
                return amount;
            }

            if (amount > e.MaxIncreaseAmount)
            {
                amount = e.MaxIncreaseAmount;
            }

            // WHY: Max <= 0 means uncapped everywhere else; skipping the headroom clamp keeps
            // Increase from turning negative and draining the pool.
            if (e.Max > 0f)
            {
                float space = e.Max - e.Current;
                if (space < amount)
                {
                    amount = space < 0f ? 0f : space;
                }
            }

            return amount;
        }

        private void NotifyChanged(string resourceId, ResourcePoolEntry e)
        {
            OnResourceChanged?.Invoke(resourceId, e.Current, e.Max);
        }
    }
}
