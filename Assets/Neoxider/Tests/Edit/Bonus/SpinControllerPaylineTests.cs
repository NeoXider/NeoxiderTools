using System.Reflection;
using Neo.Bonus;
using NUnit.Framework;
using UnityEngine;

namespace Neo.Editor.Tests
{
    /// <summary>
    ///     Payline-math coverage for the slot machine.
    ///     <para>
    ///         WHAT IS TESTED: the pure win-evaluation unit <see cref="CheckSpin"/>, which is the
    ///         logic <see cref="SpinController"/> delegates to in <c>ProcessSpinResult</c>
    ///         (<c>checkSpin.GetWinningLines</c> + <c>checkSpin.GetMultiplayers</c>) and the
    ///         payout math <c>SpinController.Win</c> performs on top of it
    ///         (<c>payout = round(sum(multipliers) * linePrice)</c>).
    ///     </para>
    ///     <para>
    ///         WHY NOT THE FULL SpinController: SpinController is a ~62KB MonoBehaviour god-class whose
    ///         outcome path runs through coroutines, child Row reels, AudioSources and scene geometry
    ///         that are unavailable / non-deterministic in EditMode. Its win/payout result is produced
    ///         entirely by CheckSpin plus a trivial multiply-and-round, so testing CheckSpin directly with
    ///         a forced, hand-computed symbol matrix gives real, deterministic coverage of the critical
    ///         math without the brittle scene scaffolding.
    ///     </para>
    ///     <para>
    ///         NOT COVERED HERE (would require PlayMode + a built reel scene): the coroutine spin lifecycle,
    ///         Row visual sampling into finalVisuals, ForceNextOutcome plumbing through BuildPlanIdMatrix,
    ///         LineRenderer win-line playback, and money spending via IMoneySpend.
    ///     </para>
    ///     <para>
    ///         CheckSpin win rule (from <c>GetInfoInSequenceLine</c>): walking a payline's columns
    ///         left-to-right, every time an adjacent pair of cells shares the same symbol id that id's
    ///         counter increments (seeded at 2 on first match); a line wins if any id reaches
    ///         <c>SequenceLength</c> (default 3). With no LinesData asset, CheckSpin synthesises one
    ///         horizontal payline per visible window row (line i == row i, y=0 bottom).
    ///     </para>
    /// </summary>
    [TestFixture]
    public class SpinControllerPaylineTests
    {
        private const int Cols = 3;
        private const int Rows = 3;

        /// <summary>
        ///     Builds a fresh CheckSpin using horizontal fallback paylines (no LinesData),
        ///     sequence length 3.
        /// </summary>
        private static CheckSpin NewFallbackCheckSpin()
        {
            CheckSpin cs = new() { isActive = true };
            cs.SetSequenceLength(3);
            // WHY: full window range -> one fallback line per row (line 0=row0, 1=row1, 2=row2).
            cs.SetFallbackPaylineWindowRows(-1, -1);
            return cs;
        }

        /// <summary>
        ///     Converts a human-readable rows-top-to-bottom layout into the engine's
        ///     <c>[col, row]</c> matrix with y=0 at the bottom.
        ///     <paramref name="rowsTopToBottom"/>[0] is the TOP row (highest y).
        /// </summary>
        private static int[,] Grid(params int[][] rowsTopToBottom)
        {
            int rows = rowsTopToBottom.Length;
            int cols = rowsTopToBottom[0].Length;
            int[,] ids = new int[cols, rows];
            for (int rTop = 0; rTop < rows; rTop++)
            {
                int y = rows - 1 - rTop; // WHY: top row -> highest y
                for (int x = 0; x < cols; x++)
                {
                    ids[x, y] = rowsTopToBottom[rTop][x];
                }
            }

            return ids;
        }

        // WHY: Scenario 1: one clear win on the middle payline.
        // Grid (top -> bottom):
        //   y=2: 1 2 3
        //   y=1: 7 7 7   <- triple -> line index 1 wins
        //   y=0: 4 5 6
        // Hand-computed: GetWinningLines -> [1].
        [Test]
        public void GetWinningLines_SingleMiddleRowTriple_ReturnsThatLineOnly()
        {
            CheckSpin cs = NewFallbackCheckSpin();
            int[,] grid = Grid(
                new[] { 1, 2, 3 },
                new[] { 7, 7, 7 },
                new[] { 4, 5, 6 });

            int[] winning = cs.GetWinningLines(grid, Rows);

            Assert.AreEqual(new[] { 1 }, winning,
                "Only the middle horizontal line (row 1) is a triple, so line index 1 must win.");
        }

        // WHY: Scenario 2: no win.
        // Grid (top -> bottom), every row is the a,b,a pattern (no adjacent pair equal):
        //   y=2: 1 2 1
        //   y=1: 3 4 3
        //   y=0: 5 6 5
        // Hand-computed: GetWinningLines -> [] (empty).
        [Test]
        public void GetWinningLines_NoAdjacentMatches_ReturnsEmpty()
        {
            CheckSpin cs = NewFallbackCheckSpin();
            int[,] grid = Grid(
                new[] { 1, 2, 1 },
                new[] { 3, 4, 3 },
                new[] { 5, 6, 5 });

            int[] winning = cs.GetWinningLines(grid, Rows);

            Assert.IsEmpty(winning,
                "No horizontal line has 3 matching symbols (each is a-b-a), so there must be no win.");
        }

        // WHY: Scenario 3: multi-line win.
        // Grid (top -> bottom):
        //   y=2: 8 8 8   <- triple -> line index 2
        //   y=1: 1 2 3
        //   y=0: 9 9 9   <- triple -> line index 0
        // Hand-computed: GetWinningLines -> [0, 2] (ascending line order).
        [Test]
        public void GetWinningLines_TopAndBottomTriples_ReturnsBothLinesAscending()
        {
            CheckSpin cs = NewFallbackCheckSpin();
            int[,] grid = Grid(
                new[] { 8, 8, 8 },
                new[] { 1, 2, 3 },
                new[] { 9, 9, 9 });

            int[] winning = cs.GetWinningLines(grid, Rows);

            Assert.AreEqual(new[] { 0, 2 }, winning,
                "Bottom row (line 0) and top row (line 2) are both triples; lines are returned ascending.");
        }

        // WHY: countLine gating: only the first N paylines are evaluated.
        // Same grid as Scenario 3 (lines 0 and 2 win) but only 1 active line ->
        // only line index 0 is evaluated, so the result is [0].
        [Test]
        public void GetWinningLines_CountLineLimitsEvaluatedPaylines()
        {
            CheckSpin cs = NewFallbackCheckSpin();
            int[,] grid = Grid(
                new[] { 8, 8, 8 },
                new[] { 1, 2, 3 },
                new[] { 9, 9, 9 });

            int[] winning = cs.GetWinningLines(grid, 1);

            Assert.AreEqual(new[] { 0 },
                winning,
                "With countLine=1 only line 0 is evaluated; line 2's triple is outside the active set.");
        }

        // WHY: Multiplier + payout math.
        // Symbol id 7 pays x5 for a count of 3. Scenario-1 grid wins line 1 with three 7s.
        // GetMultiplayers -> [5]. SpinController.Win then computes:
        //   payout = round( sum(multipliers) * linePrice ).
        // With a single line bet of linePrice = 10 -> payout = round(5 * 10) = 50.
        [Test]
        public void GetMultiplayers_ReturnsConfiguredMultiplier_AndPayoutMatchesHandComputed()
        {
            CheckSpin cs = NewFallbackCheckSpin();
            AssignMultiplierData(cs, symbolId: 7, count: 3, mult: 5f);

            int[,] grid = Grid(
                new[] { 1, 2, 3 },
                new[] { 7, 7, 7 },
                new[] { 4, 5, 6 });

            int[] winning = cs.GetWinningLines(grid, Rows);
            Assert.AreEqual(new[] { 1 }, winning, "Sanity: middle line must be the winner.");

            float[] mult = cs.GetMultiplayers(grid, Rows, winning);
            Assert.AreEqual(1, mult.Length, "Exactly one winning line -> exactly one multiplier.");
            Assert.AreEqual(5f, mult[0], 1e-5f, "Symbol 7 at count 3 is configured to pay x5.");

            // WHY: reproduce SpinController.Win payout math for a single bet line at price 10.
            const int linePrice = 10;
            float moneyWin = 0f;
            foreach (float t in mult)
            {
                moneyWin += t * linePrice;
            }

            int payout = Mathf.Max(0, Mathf.RoundToInt(moneyWin));
            Assert.AreEqual(50, payout, "round(5 * 10) = 50.");
        }

        [Test]
        public void GetMultiplayers_NoMultiplierData_DefaultsToOne()
        {
            // WHY: without SpriteMultiplayerData every matched symbol pays x1 (GetMultiplier fallback).
            CheckSpin cs = NewFallbackCheckSpin();
            int[,] grid = Grid(
                new[] { 1, 2, 3 },
                new[] { 7, 7, 7 },
                new[] { 4, 5, 6 });

            int[] winning = cs.GetWinningLines(grid, Rows);
            float[] mult = cs.GetMultiplayers(grid, Rows, winning);

            Assert.AreEqual(new[] { 1f }, mult,
                "With no multiplier asset a winning line defaults to a x1 multiplier.");
        }

        private static void AssignMultiplierData(CheckSpin cs, int symbolId, int count, float mult)
        {
            SpriteMultiplayerData data = ScriptableObject.CreateInstance<SpriteMultiplayerData>();

            SpriteMultiplayerData.SpritesMultiplier table = new()
            {
                spriteMults = new[]
                {
                    new SpriteMultiplayerData.IdMult
                    {
                        id = symbolId,
                        countMult = new[]
                        {
                            new SpriteMultiplayerData.CountMultiplayer { count = count, mult = mult }
                        }
                    }
                }
            };

            // WHY: _spritesMultiplier is a private serialized field exposed read-only via spritesMultiplier.
            typeof(SpriteMultiplayerData)
                .GetField("_spritesMultiplier", BindingFlags.NonPublic | BindingFlags.Instance)
                ?.SetValue(data, table);

            cs.SpritesMultiplierData = data;
        }
    }
}
