using System;
using System.Collections.Generic;
using System.Runtime.ExceptionServices;

namespace Neo.Core.Resources
{
    /// <summary>
    ///     Pure logic: multiple resource pools by id. No UnityEngine.
    /// </summary>
    public sealed class ResourcePoolModel
    {
        private readonly Dictionary<string, PoolRegistration> _pools = new(StringComparer.OrdinalIgnoreCase);
        private readonly List<KeyValuePair<string, PoolRegistration>> _tickBuffer = new();

        /// <summary>Bumped by every AddPool/RemovePool, so a tick only looks registrations up after one happened.</summary>
        private int _registrationVersion;

        /// <summary>
        ///     True while <see cref="_tickBuffer" /> lists exactly the current registrations. AddPool/RemovePool clear
        ///     it, so a steady tick reuses the snapshot instead of enumerating the dictionary again.
        /// </summary>
        private bool _tickBufferCurrent;

        private bool _isTicking;

        private readonly List<PendingNotification> _pendingNotifications = new();
        private int _notificationBatchDepth;
        private bool _isFlushingNotifications;

        /// <summary>Raised after a pool's value or maximum is written, with (id, current, max) of that operation.</summary>
        public event Action<string, float, float> OnResourceChanged;

        /// <summary>Raised once when an operation takes a pool from above zero to zero, after its change notification.</summary>
        public event Action<string> OnResourceDepleted;

        /// <summary>True between <see cref="BeginNotificationBatch" /> and its matching <see cref="EndNotificationBatch" />.</summary>
        public bool IsBatchingNotifications => _notificationBatchDepth > 0;

        /// <summary>
        ///     Starts (or nests) a notification batch: mutations still apply immediately and return their own
        ///     results, but <see cref="OnResourceChanged" /> and <see cref="OnResourceDepleted" /> are queued with
        ///     the values of each operation and delivered in order by the outermost <see cref="EndNotificationBatch" />.
        ///     Use it to pay several costs atomically after validating them with <see cref="CanSpend" />: no listener
        ///     runs between the payments, so none can invalidate a later one. Always end the batch in a <c>finally</c>.
        /// </summary>
        /// <example>
        ///     <code>
        ///     model.BeginNotificationBatch();
        ///     try { model.TrySpend("mana", 10f, out _); model.TrySpend("hp", 5f, out _); }
        ///     finally { model.EndNotificationBatch(); }
        ///     </code>
        /// </example>
        public void BeginNotificationBatch()
        {
            _notificationBatchDepth++;
        }

        /// <summary>
        ///     Ends one <see cref="BeginNotificationBatch" />. The outermost end delivers the queued notifications in
        ///     their original order. Mutations a listener makes during that delivery are queued behind them, so every
        ///     listener sees each pool's notifications in order and the last one carries the live value. An unmatched
        ///     call does nothing. If a listener throws, the rest of the queue is still delivered (the batch's
        ///     operations have already been applied, so dropping their notifications would lose depletion events);
        ///     the first exception is rethrown afterwards with its original stack trace.
        /// </summary>
        public void EndNotificationBatch()
        {
            if (_notificationBatchDepth == 0)
            {
                return;
            }

            _notificationBatchDepth--;
            if (_notificationBatchDepth > 0 || _isFlushingNotifications)
            {
                return;
            }

            ExceptionDispatchInfo firstFailure = null;
            _isFlushingNotifications = true;
            try
            {
                // WHY: index loop: notifications raised by listeners during delivery are appended to this queue
                // and delivered after the older ones, never before them with a newer value.
                for (int i = 0; i < _pendingNotifications.Count; i++)
                {
                    PendingNotification pending = _pendingNotifications[i];
                    try
                    {
                        if (pending.IsDepletion)
                        {
                            OnResourceDepleted?.Invoke(pending.ResourceId);
                        }
                        else
                        {
                            OnResourceChanged?.Invoke(pending.ResourceId, pending.Current, pending.Max);
                        }
                    }
                    catch (Exception exception)
                    {
                        firstFailure ??= ExceptionDispatchInfo.Capture(exception);
                    }
                }
            }
            finally
            {
                _pendingNotifications.Clear();
                _isFlushingNotifications = false;
            }

            firstFailure?.Throw();
        }

        /// <summary>Current value of the pool, or 0 for an unknown or null id.</summary>
        public float GetCurrent(string resourceId)
        {
            return TryGetPool(resourceId, out ResourcePoolEntry e) ? e.Current : 0f;
        }

        /// <summary>Maximum of the pool (0 means uncapped), or 0 for an unknown or null id.</summary>
        public float GetMax(string resourceId)
        {
            return TryGetPool(resourceId, out ResourcePoolEntry e) ? e.Max : 0f;
        }

        /// <summary>True when the pool is at zero or the id is unknown.</summary>
        public bool IsDepleted(string resourceId)
        {
            return GetCurrent(resourceId) <= 0f;
        }

        /// <summary>
        ///     Registers (or replaces) a pool. Invalid current/max values are normalized, current is clamped to a positive
        ///     max, and the regen/heal timers start from zero. The entry stays shared with the caller.
        /// </summary>
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

            // WHY: a fresh registration (with zeroed regen/heal timers) distinguishes registrations even when the
            // caller reuses the same entry under the same id.
            _pools[resourceId] = new PoolRegistration(entry);
            _registrationVersion++;
            InvalidateTickBuffer();
        }

        /// <summary>Removes a pool and its timers; a pending tick skips it. Unknown or null ids are ignored.</summary>
        public void RemovePool(string resourceId)
        {
            if (resourceId == null)
            {
                return;
            }

            if (_pools.Remove(resourceId))
            {
                _registrationVersion++;
                InvalidateTickBuffer();
            }
        }

        private void InvalidateTickBuffer()
        {
            _tickBufferCurrent = false;
            // WHY: a running tick still iterates the snapshot and clears it in its finally; outside a tick the stale
            // snapshot is dropped at once, so it never keeps a removed registration alive.
            if (!_isTicking)
            {
                _tickBuffer.Clear();
            }
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

        /// <summary>
        ///     Pays the complete <paramref name="amount" /> or nothing: fails with a reason (and no change) when the balance,
        ///     the decrease limit or float precision cannot cover it. Raises Changed, then Depleted when it empties the pool.
        /// </summary>
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
                NotifyDepleted(resourceId);
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

            return CanCover(entry, entry.Current, amount, out failReason);
        }

        /// <summary>
        ///     Checks <see cref="CanSpend" /> for every cost as if they were paid in list order: costs on the same pool
        ///     (ids compare case-insensitively) must be covered by the balance the earlier ones leave, while the
        ///     decrease limit applies to each cost on its own. Changes nothing and reserves nothing. An empty list
        ///     succeeds; finite nonpositive costs are skipped without requiring a registered resource.
        /// </summary>
        /// <exception cref="ArgumentNullException"><paramref name="costs" /> is null.</exception>
        public bool CanSpendAll(IReadOnlyList<ResourceCost> costs, out string failReason)
        {
            return ValidateSpendAll(costs, out failReason, out _);
        }

        /// <summary>
        ///     <see cref="CanSpendAll(IReadOnlyList{ResourceCost},out string)" /> that also reports the index of the
        ///     first cost that cannot be paid (-1 on success), e.g. to name the missing resource in the UI.
        /// </summary>
        /// <exception cref="ArgumentNullException"><paramref name="costs" /> is null.</exception>
        public bool CanSpendAll(IReadOnlyList<ResourceCost> costs, out string failReason, out int failedIndex)
        {
            return ValidateSpendAll(costs, out failReason, out failedIndex);
        }

        /// <summary>
        ///     Pays every cost or none: validates the whole list like <see cref="CanSpendAll(IReadOnlyList{ResourceCost},out string)" />,
        ///     then pays the costs in order inside one notification batch, so no listener runs between two payments
        ///     and none can invalidate a later one. Listeners then receive each payment's Changed (and Depleted)
        ///     notification in order. Failure returns false with the first failing cost's reason and changes nothing.
        ///     A warm call does not allocate.
        /// </summary>
        /// <example>
        ///     <code>
        ///     costBuffer.Clear();
        ///     costBuffer.Add(new ResourceCost(RpgResourceId.Mana, 30f));
        ///     costBuffer.Add(new ResourceCost(RpgResourceId.Hp, 10f));
        ///     if (resources.TrySpendAll(costBuffer, out string reason)) CastBloodSpell();
        ///     </code>
        /// </example>
        /// <exception cref="ArgumentNullException"><paramref name="costs" /> is null.</exception>
        public bool TrySpendAll(IReadOnlyList<ResourceCost> costs, out string failReason)
        {
            return TrySpendAll(costs, out failReason, out _);
        }

        /// <summary>
        ///     <see cref="TrySpendAll(IReadOnlyList{ResourceCost},out string)" /> that also reports the index of the
        ///     first cost that cannot be paid (-1 on success).
        /// </summary>
        /// <exception cref="ArgumentNullException"><paramref name="costs" /> is null.</exception>
        public bool TrySpendAll(IReadOnlyList<ResourceCost> costs, out string failReason, out int failedIndex)
        {
            if (!ValidateSpendAll(costs, out failReason, out failedIndex))
            {
                return false;
            }

            BeginNotificationBatch();
            try
            {
                for (int i = 0; i < costs.Count; i++)
                {
                    ResourceCost cost = costs[i];
                    // WHY: the list was validated with the same sequential arithmetic and no listener runs inside
                    // the batch, so every payment succeeds.
                    TrySpend(cost.ResourceId, cost.Amount, out _);
                }
            }
            finally
            {
                EndNotificationBatch();
            }

            return true;
        }

        private bool ValidateSpendAll(IReadOnlyList<ResourceCost> costs, out string failReason, out int failedIndex)
        {
            if (costs == null)
            {
                throw new ArgumentNullException(nameof(costs));
            }

            for (int i = 0; i < costs.Count; i++)
            {
                ResourceCost cost = costs[i];
                float amount = cost.Amount;
                failedIndex = i;
                if (float.IsNaN(amount) || float.IsInfinity(amount))
                {
                    failReason = "Amount must be finite.";
                    return false;
                }

                if (amount <= 0f)
                {
                    continue;
                }

                if (!TryGetPool(cost.ResourceId, out ResourcePoolEntry entry))
                {
                    failReason = "Unknown resource.";
                    return false;
                }

                // WHY: simulate the earlier payments on the same pool with TrySpend's own arithmetic, so a list that
                // validates is exactly a list whose sequential TrySpend calls all succeed.
                float current = entry.Current;
                for (int previous = 0; previous < i; previous++)
                {
                    ResourceCost earlier = costs[previous];
                    if (earlier.Amount > 0f &&
                        string.Equals(earlier.ResourceId, cost.ResourceId, StringComparison.OrdinalIgnoreCase))
                    {
                        current -= earlier.Amount;
                        if (current < 0f)
                        {
                            current = 0f;
                        }
                    }
                }

                if (!CanCover(entry, current, amount, out failReason))
                {
                    return false;
                }
            }

            failReason = null;
            failedIndex = -1;
            return true;
        }

        private static bool CanCover(ResourcePoolEntry entry, float current, float amount, out string failReason)
        {
            if (current < amount)
            {
                failReason = "Not enough resource.";
                return false;
            }

            // WHY: the decrease limit caps one operation; with the balance check above this equals
            // ClampDecrease(entry, amount) < amount.
            if (entry.MaxDecreaseAmount >= 0f && amount > entry.MaxDecreaseAmount)
            {
                failReason = "Amount exceeds decrease limit.";
                return false;
            }

            float candidateCurrent = (float)(current - amount);
            if (!(candidateCurrent < current))
            {
                failReason = "Amount does not decrease resource.";
                return false;
            }

            failReason = null;
            return true;
        }

        /// <summary>Removes up to <paramref name="amount" /> (limited by the balance and decrease limit) and returns the actual delta.</summary>
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
                NotifyDepleted(resourceId);
            }

            return actual;
        }

        /// <summary>
        ///     Adds up to <paramref name="amount" /> (limited by the increase limit and the max headroom) and returns the
        ///     actual delta. A depleted pool only heals with <see cref="ResourcePoolEntry.IgnoreCanHeal" />.
        /// </summary>
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

        /// <summary>Sets current to max (0 for an uncapped pool) and notifies.</summary>
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

        /// <summary>Sets the maximum (negative becomes 0 = uncapped), clamps current to it and notifies. Non-finite values are ignored.</summary>
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
        ///     A warm tick does not allocate, reuses its registration snapshot until AddPool/RemovePool, and does
        ///     only an addition and a comparison for a timer that completes no cycle this frame. A long frame
        ///     applies every elapsed cycle at once (each one capped by the increase limit). A nested call from a
        ///     listener is ignored. Non-finite or nonpositive <paramref name="deltaTime" /> does nothing.
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
                // WHY: iterate a snapshot of the registrations: callbacks can remove pools or re-register the very
                // same entry under its id while the tick runs. It is rebuilt only after AddPool/RemovePool, so a
                // steady tick does not enumerate the dictionary again.
                if (!_tickBufferCurrent)
                {
                    _tickBuffer.Clear();
                    foreach (KeyValuePair<string, PoolRegistration> pool in _pools)
                    {
                        _tickBuffer.Add(pool);
                    }

                    _tickBufferCurrent = true;
                }

                int snapshotVersion = _registrationVersion;
                int count = _tickBuffer.Count;
                for (int i = 0; i < count; i++)
                {
                    KeyValuePair<string, PoolRegistration> pool = _tickBuffer[i];
                    string id = pool.Key;
                    PoolRegistration registration = pool.Value;
                    ResourcePoolEntry e = registration.Entry;
                    if (!IsCurrentRegistration(id, registration, snapshotVersion))
                    {
                        continue;
                    }

                    float regenInterval = e.RegenInterval;
                    if (e.RegenPerSecond > 0f && regenInterval > 0f)
                    {
                        double accum = registration.RegenAccum + (double)deltaTime;
                        // WHY: most frames complete no cycle. For 0 <= accum < interval the division floors to 0 and
                        // accum % interval is accum exactly, so skipping both changes no result.
                        if (accum < regenInterval)
                        {
                            registration.RegenAccum = (float)accum;
                        }
                        else
                        {
                            double cycles = Math.Floor(accum / regenInterval);
                            registration.RegenAccum = (float)(accum % regenInterval);
                            if (cycles > 0d)
                            {
                                ApplyScheduledIncrease(id, e,
                                    ClampIncrease(e, e.RegenPerSecond * (double)regenInterval) * cycles);
                            }
                        }
                    }

                    float healDelay = e.HealDelay;
                    if (healDelay > 0f && e.HealAmount > 0f &&
                        IsCurrentRegistration(id, registration, snapshotVersion))
                    {
                        double timer = registration.HealTimer + (double)deltaTime;
                        if (timer < healDelay)
                        {
                            registration.HealTimer = (float)timer;
                        }
                        else
                        {
                            double cycles = Math.Floor(timer / healDelay);
                            registration.HealTimer = (float)(timer % healDelay);
                            if (cycles > 0d)
                            {
                                ApplyScheduledIncrease(id, e, ClampIncrease(e, e.HealAmount) * cycles);
                            }
                        }
                    }
                }
            }
            finally
            {
                // WHY: a callback that added or removed a pool left the snapshot stale; drop it so it does not keep
                // the removed registration alive until the next tick.
                if (!_tickBufferCurrent)
                {
                    _tickBuffer.Clear();
                }

                _isTicking = false;
            }
        }

        private void ApplyScheduledIncrease(string resourceId, ResourcePoolEntry e, double amount)
        {
            // WHY: a full pool still completes regen/heal cycles; without this gate every cycle raised
            // OnResourceChanged with an unchanged value (and HealthComponent.OnChanged once per interval).
            if (amount > 0d)
            {
                ApplyIncrease(resourceId, e, amount);
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
            if (resourceId != null && _pools.TryGetValue(resourceId, out PoolRegistration registration))
            {
                entry = registration.Entry;
                return true;
            }

            entry = null;
            return false;
        }

        private bool IsCurrentRegistration(string resourceId, PoolRegistration registration, int snapshotVersion)
        {
            // WHY: with no AddPool/RemovePool since the snapshot every snapshot registration is still current, so
            // the common tick skips two case-insensitive dictionary lookups per pool.
            if (snapshotVersion == _registrationVersion)
            {
                return true;
            }

            return _pools.TryGetValue(resourceId, out PoolRegistration current) &&
                   ReferenceEquals(registration, current);
        }

        private void NotifyChanged(string resourceId, ResourcePoolEntry e)
        {
            // WHY: during the flush a listener's mutation must queue too; notifying it at once would let the older
            // queued notification of the same pool arrive afterwards and leave observers on an out-of-date value.
            if (_notificationBatchDepth > 0 || _isFlushingNotifications)
            {
                _pendingNotifications.Add(new PendingNotification(resourceId, false, e.Current, e.Max));
                return;
            }

            OnResourceChanged?.Invoke(resourceId, e.Current, e.Max);
        }

        private void NotifyDepleted(string resourceId)
        {
            if (_notificationBatchDepth > 0 || _isFlushingNotifications)
            {
                _pendingNotifications.Add(new PendingNotification(resourceId, true, 0f, 0f));
                return;
            }

            OnResourceDepleted?.Invoke(resourceId);
        }

        /// <summary>One queued notification of a batch, with the values of the operation that raised it.</summary>
        private readonly struct PendingNotification
        {
            public readonly string ResourceId;
            public readonly bool IsDepletion;
            public readonly float Current;
            public readonly float Max;

            public PendingNotification(string resourceId, bool isDepletion, float current, float max)
            {
                ResourceId = resourceId;
                IsDepletion = isDepletion;
                Current = current;
                Max = max;
            }
        }

        /// <summary>
        ///     One AddPool call: the entry plus its regen/heal remainders. A replaced registration keeps its own
        ///     object, so a tick snapshot can tell it apart from the new one even when the entry is the same.
        /// </summary>
        private sealed class PoolRegistration
        {
            public readonly ResourcePoolEntry Entry;
            public float RegenAccum;
            public float HealTimer;

            public PoolRegistration(ResourcePoolEntry entry)
            {
                Entry = entry;
            }
        }
    }
}
