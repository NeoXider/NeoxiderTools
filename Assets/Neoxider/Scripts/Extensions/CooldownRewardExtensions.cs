using System;

namespace Neo.Extensions
{
    /// <summary>
    ///     Helpers for cooldown-based reward logic: accumulated claims and advancing last-claim time.
    /// </summary>
    public static class CooldownRewardExtensions
    {
        /// <summary>
        ///     Gets the number of full cooldown cycles elapsed since the last claim.
        /// </summary>
        /// <param name="lastClaimUtc">UTC time of last claim.</param>
        /// <param name="cooldownSeconds">Cooldown duration in seconds.</param>
        /// <param name="nowUtc">Current UTC time.</param>
        /// <returns>Number of claims that can be given (0 or more).</returns>
        public static int GetAccumulatedClaimCount(this DateTime lastClaimUtc, float cooldownSeconds, DateTime nowUtc)
        {
            if (cooldownSeconds <= 0f)
            {
                return 0;
            }

            DateTime last = lastClaimUtc.EnsureUtc();
            DateTime now = nowUtc.EnsureUtc();
            double elapsed = (now - last).TotalSeconds;
            if (elapsed < 0)
            {
                return 0;
            }

            return (int)(elapsed / cooldownSeconds);
        }

        /// <summary>
        ///     Caps the claim count by max per take. Use -1 for no limit.
        /// </summary>
        /// <param name="accumulated">Number of accumulated claims.</param>
        /// <param name="maxPerTake">Maximum to give in one take; -1 = all.</param>
        /// <returns>Capped count (0 or more).</returns>
        public static int CapToMaxPerTake(int accumulated, int maxPerTake)
        {
            if (accumulated <= 0)
            {
                return 0;
            }

            if (maxPerTake < 0)
            {
                return accumulated;
            }

            return Math.Min(accumulated, maxPerTake);
        }

        /// <summary>
        ///     Returns the new last-claim UTC time after giving the specified number of claims.
        /// </summary>
        /// <param name="lastClaimUtc">Current last claim time.</param>
        /// <param name="claimsGiven">Number of claims just given.</param>
        /// <param name="cooldownSeconds">Cooldown duration in seconds.</param>
        /// <returns>New last-claim time to persist.</returns>
        public static DateTime AdvanceLastClaimTime(this DateTime lastClaimUtc, int claimsGiven, float cooldownSeconds)
        {
            if (claimsGiven <= 0 || cooldownSeconds <= 0f)
            {
                return lastClaimUtc.EnsureUtc();
            }

            return lastClaimUtc.EnsureUtc().AddSeconds(claimsGiven * cooldownSeconds);
        }
    }
}