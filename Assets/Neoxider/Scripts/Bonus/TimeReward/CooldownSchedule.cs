using System;
using Neo.Extensions;
using Neo.Save;
using Neo.Tools;

namespace Neo.Bonus
{
    /// <summary>What a claim does with cooldown cycles that elapsed beyond the ones it granted.</summary>
    public enum CooldownBacklogMode
    {
        /// <summary>The next cooldown starts at the claim time; extra elapsed cycles are dropped (CooldownReward).</summary>
        ResetFromClaimTime,

        /// <summary>The last-claim time advances by exactly the granted cycles; the remainder keeps counting (TimeReward).</summary>
        PreserveElapsedRemainder
    }

    /// <summary>
    ///     Plain C# cooldown rule: how many rewards are claimable, how long until the next one, and claiming. Time comes
    ///     from the Tools/Time clock seam, an injected <see cref="INeoClock" /> (a fake one in tests); without one it reads
    ///     the process-wide <see cref="NeoTrustedTime" />, which resists device clock changes and persists its
    ///     high-water mark. Persist <see cref="LastClaimUtc" /> (for example with <see cref="SaveTo" />) to survive restarts.
    ///     <see cref="CooldownReward" /> and the legacy <see cref="TimeReward" /> use the same counting rule.
    /// </summary>
    public sealed class CooldownSchedule
    {
        private readonly INeoClock _clock;
        private float _cooldownSeconds;
        private bool _claimInProgress;

        /// <param name="cooldownSeconds">Cooldown length in seconds; non-finite or negative counts as 0 (never claimable).</param>
        /// <param name="maxClaimsPerTake">-1 grants every accumulated reward per claim; N caps a claim at N.</param>
        /// <param name="availableInitially">When true the first reward is claimable before any claim was made.</param>
        /// <param name="clock">
        ///     Time source (only <see cref="INeoClock.UtcNow" /> is read); null uses the tamper-resistant
        ///     <see cref="NeoTrustedTime" />.
        /// </param>
        public CooldownSchedule(float cooldownSeconds, int maxClaimsPerTake = 1, bool availableInitially = false,
            INeoClock clock = null)
        {
            CooldownSeconds = cooldownSeconds;
            MaxClaimsPerTake = maxClaimsPerTake;
            AvailableInitially = availableInitially;
            _clock = clock;
        }

        /// <summary>Raised after a claim has been recorded, with the number of rewards it granted.</summary>
        public event Action<int> Claimed;

        /// <summary>Cooldown length in seconds (never negative).</summary>
        public float CooldownSeconds
        {
            get => _cooldownSeconds;
            set => _cooldownSeconds = float.IsFinite(value) && value > 0f ? value : 0f;
        }

        /// <summary>-1 grants every accumulated reward per claim; N caps a claim at N; 0 grants nothing.</summary>
        public int MaxClaimsPerTake { get; set; }

        /// <summary>When true the first reward is claimable before any claim was made.</summary>
        public bool AvailableInitially { get; set; }

        /// <summary>How a claim treats cycles beyond the granted ones.</summary>
        public CooldownBacklogMode BacklogMode { get; set; } = CooldownBacklogMode.ResetFromClaimTime;

        /// <summary>True once a claim, <see cref="Restart()" /> or <see cref="Restore" /> set a last-claim time.</summary>
        public bool HasLastClaim { get; private set; }

        /// <summary>UTC time the current cooldown counts from (meaningful when <see cref="HasLastClaim" />).</summary>
        public DateTime LastClaimUtc { get; private set; }

        /// <summary>Current time from the injected clock or <see cref="NeoTrustedTime" />.</summary>
        public DateTime UtcNow => _clock != null ? _clock.UtcNow.EnsureUtc() : NeoTrustedTime.UtcNow;

        /// <summary>
        ///     The counting rule shared by every cooldown reward: full cycles since <paramref name="lastClaimUtc" />,
        ///     capped by <paramref name="maxClaimsPerTake" />. Without a last claim it is 1 when
        ///     <paramref name="availableWithoutClaim" />, otherwise 0. A non-positive cooldown is never claimable.
        /// </summary>
        public static int CountClaimable(bool hasLastClaim, DateTime lastClaimUtc, float cooldownSeconds,
            DateTime nowUtc, int maxClaimsPerTake, bool availableWithoutClaim)
        {
            if (!float.IsFinite(cooldownSeconds) || cooldownSeconds <= 0f)
            {
                return 0;
            }

            if (!hasLastClaim)
            {
                return CooldownRewardExtensions.CapToMaxPerTake(availableWithoutClaim ? 1 : 0, maxClaimsPerTake);
            }

            int accumulated = lastClaimUtc.GetAccumulatedClaimCount(cooldownSeconds, nowUtc);
            return CooldownRewardExtensions.CapToMaxPerTake(accumulated, maxClaimsPerTake);
        }

        /// <summary>Rewards claimable now.</summary>
        public int GetClaimableCount()
        {
            return GetClaimableCount(UtcNow);
        }

        /// <summary>
        ///     Rewards claimable at <paramref name="nowUtc" />. Does not change state, but returns 0 while a
        ///     <see cref="Claimed" /> listener of the current claim runs.
        /// </summary>
        public int GetClaimableCount(DateTime nowUtc)
        {
            if (_claimInProgress)
            {
                return 0;
            }

            return CountClaimable(HasLastClaim, LastClaimUtc, _cooldownSeconds, nowUtc, MaxClaimsPerTake,
                AvailableInitially);
        }

        /// <summary>Seconds until the next reward from now; 0 when one is claimable.</summary>
        public double GetSecondsUntilNext()
        {
            return GetSecondsUntilNext(UtcNow);
        }

        /// <summary>
        ///     Seconds until the next reward at <paramref name="nowUtc" />; 0 when one is claimable. A schedule that has
        ///     never been claimed or started reports a full cooldown unless it is <see cref="AvailableInitially" />.
        /// </summary>
        public double GetSecondsUntilNext(DateTime nowUtc)
        {
            if (_cooldownSeconds <= 0f)
            {
                return 0d;
            }

            if (!HasLastClaim)
            {
                return AvailableInitially ? 0d : _cooldownSeconds;
            }

            double elapsed = (nowUtc.EnsureUtc() - LastClaimUtc).TotalSeconds;
            if (elapsed < 0d)
            {
                return _cooldownSeconds;
            }

            return elapsed >= _cooldownSeconds ? 0d : _cooldownSeconds - elapsed;
        }

        /// <summary>Claims at the current time.</summary>
        public bool TryClaim(out int grantedCount)
        {
            return TryClaim(UtcNow, out grantedCount);
        }

        /// <summary>
        ///     Claims every reward allowed at <paramref name="nowUtc" />. The cooldown is consumed before
        ///     <see cref="Claimed" /> runs, so a listener that claims again or throws cannot double-grant.
        /// </summary>
        public bool TryClaim(DateTime nowUtc, out int grantedCount)
        {
            grantedCount = GetClaimableCount(nowUtc);
            if (grantedCount < 1)
            {
                grantedCount = 0;
                return false;
            }

            DateTime now = nowUtc.EnsureUtc();
            LastClaimUtc = HasLastClaim && BacklogMode == CooldownBacklogMode.PreserveElapsedRemainder
                ? LastClaimUtc.AdvanceLastClaimTime(grantedCount, _cooldownSeconds)
                : now;
            HasLastClaim = true;

            _claimInProgress = true;
            try
            {
                Claimed?.Invoke(grantedCount);
            }
            finally
            {
                _claimInProgress = false;
            }

            return true;
        }

        /// <summary>Starts a full cooldown from now.</summary>
        public void Restart()
        {
            Restart(UtcNow);
        }

        /// <summary>Starts a full cooldown from <paramref name="nowUtc" />.</summary>
        public void Restart(DateTime nowUtc)
        {
            LastClaimUtc = nowUtc.EnsureUtc();
            HasLastClaim = true;
        }

        /// <summary>Makes exactly one reward claimable at <paramref name="nowUtc" /> (backdates the last claim by one cooldown).</summary>
        public void MakeAvailableNow(DateTime nowUtc)
        {
            DateTime now = nowUtc.EnsureUtc();
            // WHY: whole ticks rounded up (+1) so float-to-tick rounding can never leave the reward a hair short.
            long backTicks = (long)Math.Ceiling((double)_cooldownSeconds * TimeSpan.TicksPerSecond) + 1L;
            LastClaimUtc = now.Ticks > backTicks
                ? now.AddTicks(-backTicks)
                : DateTime.SpecifyKind(DateTime.MinValue, DateTimeKind.Utc);
            HasLastClaim = true;
        }

        /// <summary>Restores a persisted last-claim time.</summary>
        public void Restore(DateTime lastClaimUtc)
        {
            LastClaimUtc = lastClaimUtc.EnsureUtc();
            HasLastClaim = true;
        }

        /// <summary>Forgets the last claim (back to the never-claimed state).</summary>
        public void Clear()
        {
            LastClaimUtc = default;
            HasLastClaim = false;
        }

        /// <summary>
        ///     Writes <see cref="LastClaimUtc" /> as a round-trip UTC string under <paramref name="saveKey" />, the same
        ///     format the legacy <see cref="TimeReward" /> uses for <c>LastRewardTime&lt;key&gt;</c>. Deletes the key when
        ///     there is no last claim.
        /// </summary>
        public void SaveTo(string saveKey)
        {
            if (string.IsNullOrEmpty(saveKey))
            {
                return;
            }

            if (HasLastClaim)
            {
                SaveProvider.SetString(saveKey, LastClaimUtc.ToRoundTripUtcString());
            }
            else
            {
                SaveProvider.DeleteKey(saveKey);
            }
        }

        /// <summary>Reads a value written by <see cref="SaveTo" />. Returns false (state unchanged) when missing or invalid.</summary>
        public bool TryLoadFrom(string saveKey)
        {
            if (string.IsNullOrEmpty(saveKey) || !SaveProvider.HasKey(saveKey))
            {
                return false;
            }

            string raw = SaveProvider.GetString(saveKey, string.Empty);
            if (!raw.TryParseUtcRoundTrip(out DateTime lastClaimUtc))
            {
                return false;
            }

            Restore(lastClaimUtc);
            return true;
        }
    }
}
