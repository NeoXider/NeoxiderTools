using System;

namespace Neo.Bonus
{
    /// <summary>
    ///     Pure bet and payout arithmetic used by <see cref="SpinController" />. Plain C# so code-first slot games,
    ///     servers and tests compute the same prices and payouts as the scene component.
    /// </summary>
    public static class SlotSpinMath
    {
        /// <summary>
        ///     Price of one spin. With <paramref name="pricePerLine" /> the line bet is multiplied by the active payline
        ///     count (at least 1); otherwise the line bet is the whole price. Negative bets count as 0 and the result
        ///     saturates at <see cref="int.MaxValue" />.
        /// </summary>
        public static int ComputeSpinPrice(int lineBet, int activeLines, bool pricePerLine)
        {
            long bet = Math.Max(0, lineBet);
            long total = pricePerLine ? Math.Max(1, activeLines) * bet : bet;
            return (int)Math.Min(int.MaxValue, total);
        }

        /// <summary>
        ///     Coin payout for the winning-line multipliers at <paramref name="lineBet" />: the sum of every finite,
        ///     positive multiplier times the bet, rounded half-to-even and saturated at <see cref="int.MaxValue" />.
        ///     A null array, a non-positive bet or no positive multiplier pays 0.
        /// </summary>
        public static int ComputePayout(float[] lineMultipliers, int lineBet)
        {
            if (lineMultipliers == null || lineBet <= 0)
            {
                return 0;
            }

            double total = 0d;
            for (int i = 0; i < lineMultipliers.Length; i++)
            {
                float multiplier = lineMultipliers[i];
                if (float.IsFinite(multiplier) && multiplier > 0f)
                {
                    total += (double)multiplier * lineBet;
                }
            }

            return (int)Math.Min(int.MaxValue, Math.Round(total, MidpointRounding.ToEven));
        }
    }
}
