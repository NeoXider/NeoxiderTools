using System;
using System.Collections.Generic;
using UnityEngine;

namespace Neo.Bonus
{
    /// <summary>
    ///     Accumulate / threshold / claim rules shared by <see cref="Box" /> (float progress) and
    ///     <see cref="BoxProgress" /> (integer progress). A non-positive threshold means a free claim.
    /// </summary>
    public static class BoxProgressRules
    {
        /// <summary>
        ///     True when <paramref name="progress" /> reaches <paramref name="threshold" /> and a claim would actually
        ///     reduce it (a float too large to lose the threshold is not claimable, so it cannot be farmed forever).
        /// </summary>
        public static bool CanClaim(float progress, float threshold)
        {
            return progress >= threshold &&
                   (threshold <= 0f ||
                    (float.IsFinite(progress) && float.IsFinite(threshold) &&
                     GetProgressAfterClaim(progress, threshold) < progress));
        }

        /// <summary>True when <paramref name="progress" /> reaches <paramref name="threshold" />.</summary>
        public static bool CanClaim(long progress, long threshold)
        {
            return progress >= threshold;
        }

        /// <summary>Progress left after one claim; never negative.</summary>
        public static float GetProgressAfterClaim(float progress, float threshold)
        {
            return Mathf.Max(0f, progress - threshold);
        }

        /// <summary>Progress left after one claim; never negative. A non-positive threshold consumes nothing.</summary>
        public static long GetProgressAfterClaim(long progress, long threshold)
        {
            if (threshold <= 0L)
            {
                return progress;
            }

            return progress > threshold ? progress - threshold : 0L;
        }

        /// <summary>Bar fill 0..1; 0 for a non-positive threshold.</summary>
        public static float GetFillRatio(float progress, float threshold)
        {
            return threshold > 0f ? Mathf.Clamp01(progress / threshold) : 0f;
        }

        /// <summary>Bar fill 0..1; 0 for a non-positive threshold.</summary>
        public static float GetFillRatio(long progress, long threshold)
        {
            if (threshold <= 0L || progress <= 0L)
            {
                return 0f;
            }

            return progress >= threshold ? 1f : (float)((double)progress / threshold);
        }
    }

    /// <summary>Result of one <see cref="BoxProgress" /> claim.</summary>
    [Serializable]
    public struct BoxClaimReceipt
    {
        /// <summary>
        ///     Caller-supplied claim id, or null for an automatically numbered claim (identified by <see cref="Sequence" />
        ///     only, so it can never collide with a caller id such as "2").
        /// </summary>
        public string ClaimId;

        /// <summary>1-based claim number; never reused by one progress instance.</summary>
        public long Sequence;

        /// <summary>Progress the claim consumed (the threshold, or 0 for a free claim).</summary>
        public long ConsumedAmount;

        /// <summary>Progress left right after the claim.</summary>
        public long RemainingProgress;

        /// <summary>True when the claim id had already been processed and nothing changed this time.</summary>
        public bool IsReplay;
    }

    /// <summary>Plain data snapshot of a <see cref="BoxProgress" /> for saves (JsonUtility friendly).</summary>
    [Serializable]
    public sealed class BoxProgressState
    {
        /// <summary>Progress at capture time.</summary>
        public long Progress;

        /// <summary>Sequence of the last claim at capture time.</summary>
        public long ClaimSequence;

        /// <summary>Remembered receipts, oldest first; restoring them keeps claim ids idempotent across restarts.</summary>
        public List<BoxClaimReceipt> RecentClaims = new();
    }

    /// <summary>
    ///     Plain C# "piggy bank" core: integer progress (for example cents), a claim threshold and idempotent claims.
    ///     Passing the same claim id twice (a retried request, a replayed save) returns the original receipt without
    ///     consuming progress again. <see cref="Box" /> is the scene/UI component; it keeps float progress and uses
    ///     the same <see cref="BoxProgressRules" />.
    /// </summary>
    public sealed class BoxProgress
    {
        /// <summary>How many recent claim receipts are remembered for idempotency by default.</summary>
        public const int DefaultRememberedClaims = 32;

        private readonly int _rememberedClaims;
        private readonly List<BoxClaimReceipt> _recentClaims;
        private long _threshold;

        /// <param name="threshold">Progress consumed by one claim; 0 or less makes every claim free.</param>
        /// <param name="progress">Initial progress; negative counts as 0.</param>
        /// <param name="rememberedClaims">Receipts kept for idempotency (at least 1).</param>
        public BoxProgress(long threshold, long progress = 0L, int rememberedClaims = DefaultRememberedClaims)
        {
            _threshold = Math.Max(0L, threshold);
            Progress = Math.Max(0L, progress);
            _rememberedClaims = Math.Max(1, rememberedClaims);
            _recentClaims = new List<BoxClaimReceipt>(_rememberedClaims);
        }

        /// <summary>Raised after progress changes: (previous, current).</summary>
        public event Action<long, long> ProgressChanged;

        /// <summary>Raised after a new (non-replayed) claim has consumed its progress.</summary>
        public event Action<BoxClaimReceipt> Claimed;

        /// <summary>Current progress (never negative).</summary>
        public long Progress { get; private set; }

        /// <summary>Progress consumed by one claim; 0 makes every claim free.</summary>
        public long Threshold
        {
            get => _threshold;
            set => _threshold = Math.Max(0L, value);
        }

        /// <summary>Number of claims made so far (also the sequence of the last receipt).</summary>
        public long ClaimSequence { get; private set; }

        /// <summary>True when a claim would succeed now.</summary>
        public bool CanClaim => BoxProgressRules.CanClaim(Progress, _threshold);

        /// <summary>Bar fill 0..1.</summary>
        public float FillRatio => BoxProgressRules.GetFillRatio(Progress, _threshold);

        /// <summary>
        ///     Adds <paramref name="amount" /> (negative removes). Saturates at <see cref="long.MaxValue" /> and never goes
        ///     below 0. Returns the new progress.
        /// </summary>
        public long Add(long amount)
        {
            long next;
            if (amount >= 0L)
            {
                next = Progress > long.MaxValue - amount ? long.MaxValue : Progress + amount;
            }
            else
            {
                next = amount == long.MinValue || Progress + amount < 0L ? 0L : Progress + amount;
            }

            SetProgress(next);
            return Progress;
        }

        /// <summary>Sets progress directly (negative counts as 0).</summary>
        public void SetProgress(long value)
        {
            long previous = Progress;
            Progress = Math.Max(0L, value);
            if (previous != Progress)
            {
                ProgressChanged?.Invoke(previous, Progress);
            }
        }

        /// <summary>Claims once with an automatically numbered receipt.</summary>
        public bool TryClaim(out BoxClaimReceipt receipt)
        {
            return TryClaim(null, out receipt);
        }

        /// <summary>
        ///     Claims once. When <paramref name="claimId" /> was already processed, returns true with the original
        ///     receipt marked <see cref="BoxClaimReceipt.IsReplay" /> and changes nothing. A null or empty id makes an
        ///     automatically numbered claim that is never matched as a replay. Progress is consumed before
        ///     <see cref="Claimed" /> runs, so a throwing listener cannot make the same claim pay twice.
        /// </summary>
        public bool TryClaim(string claimId, out BoxClaimReceipt receipt)
        {
            if (string.IsNullOrEmpty(claimId))
            {
                claimId = null;
            }
            else if (TryGetReceipt(claimId, out receipt))
            {
                receipt.IsReplay = true;
                return true;
            }

            if (!CanClaim)
            {
                receipt = default;
                return false;
            }

            long previous = Progress;
            long remaining = BoxProgressRules.GetProgressAfterClaim(previous, _threshold);
            long sequence = ++ClaimSequence;
            receipt = new BoxClaimReceipt
            {
                ClaimId = claimId,
                Sequence = sequence,
                ConsumedAmount = previous - remaining,
                RemainingProgress = remaining,
                IsReplay = false
            };

            Progress = remaining;
            Remember(receipt);

            if (previous != remaining)
            {
                ProgressChanged?.Invoke(previous, remaining);
            }

            Claimed?.Invoke(receipt);
            return true;
        }

        /// <summary>True when <paramref name="claimId" /> is among the remembered processed claims.</summary>
        public bool HasProcessedClaim(string claimId)
        {
            return TryGetReceipt(claimId, out _);
        }

        /// <summary>Remembered receipt for <paramref name="claimId" />, if any. Null or empty ids never match.</summary>
        public bool TryGetReceipt(string claimId, out BoxClaimReceipt receipt)
        {
            if (!string.IsNullOrEmpty(claimId))
            {
                for (int i = _recentClaims.Count - 1; i >= 0; i--)
                {
                    if (string.Equals(_recentClaims[i].ClaimId, claimId, StringComparison.Ordinal))
                    {
                        receipt = _recentClaims[i];
                        return true;
                    }
                }
            }

            receipt = default;
            return false;
        }

        /// <summary>Copies progress, claim sequence and remembered receipts into a new save snapshot.</summary>
        public BoxProgressState Capture()
        {
            BoxProgressState state = new() { Progress = Progress, ClaimSequence = ClaimSequence };
            state.RecentClaims.AddRange(_recentClaims);
            return state;
        }

        /// <summary>Replaces progress, claim sequence and remembered receipts from a snapshot. Raises no events.</summary>
        public void Restore(BoxProgressState state)
        {
            _recentClaims.Clear();
            if (state == null)
            {
                Progress = 0L;
                ClaimSequence = 0L;
                return;
            }

            Progress = Math.Max(0L, state.Progress);
            ClaimSequence = Math.Max(0L, state.ClaimSequence);
            if (state.RecentClaims == null)
            {
                return;
            }

            int start = Math.Max(0, state.RecentClaims.Count - _rememberedClaims);
            for (int i = start; i < state.RecentClaims.Count; i++)
            {
                BoxClaimReceipt claim = state.RecentClaims[i];
                claim.IsReplay = false;
                if (string.IsNullOrEmpty(claim.ClaimId))
                {
                    // WHY: JsonUtility writes a null string as "", which must stay an automatic (unmatched) claim.
                    claim.ClaimId = null;
                }

                _recentClaims.Add(claim);
                if (claim.Sequence > ClaimSequence)
                {
                    ClaimSequence = claim.Sequence;
                }
            }
        }

        private void Remember(BoxClaimReceipt receipt)
        {
            if (_recentClaims.Count >= _rememberedClaims)
            {
                _recentClaims.RemoveAt(0);
            }

            _recentClaims.Add(receipt);
        }
    }
}
