using System.Collections.Generic;
using System.Reflection;
using Neo.Bonus;
using NUnit.Framework;
using UnityEngine;

namespace Neo.Editor.Tests
{
    /// <summary>
    ///     Covers the economy-driven spin API on <see cref="SpinController"/>:
    ///     <c>BuildEconomyOutcomeMatrix</c> (weighted fill + special-rule conversion along active
    ///     paylines, deterministic picker overload) and <c>EvaluateActivePaylinesWithEconomy</c>.
    /// </summary>
    [TestFixture]
    public class SpinControllerEconomyTests
    {
        private GameObject _root;
        private SpinController _spin;
        private SlotEconomyDefinition _economy;

        [SetUp]
        public void SetUp()
        {
            _root = new GameObject("SpinControllerEconomyTests");
            _spin = _root.AddComponent<SpinController>();

            var rows = new Row[3];
            for (int i = 0; i < rows.Length; i++)
            {
                var rowGo = new GameObject($"Row{i}");
                rowGo.transform.SetParent(_root.transform);
                rows[i] = rowGo.AddComponent<Row>();
            }

            FieldInfo rowsField = typeof(SpinController).GetField("_rows",
                BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.IsNotNull(rowsField, "SpinController._rows field expected");
            rowsField.SetValue(_spin, rows);
        }

        [TearDown]
        public void TearDown()
        {
            Object.DestroyImmediate(_root);
            if (_economy != null)
            {
                Object.DestroyImmediate(_economy);
            }
        }

        private void ConfigureEconomy(bool forceLineOnSpecial, params SlotEconomyDefinition.Symbol[] symbols)
        {
            _economy = ScriptableObject.CreateInstance<SlotEconomyDefinition>();
            FieldInfo symbolsField = typeof(SlotEconomyDefinition).GetField("_symbols",
                BindingFlags.Instance | BindingFlags.NonPublic);
            FieldInfo forceField = typeof(SlotEconomyDefinition).GetField("_forceLineOnSpecial",
                BindingFlags.Instance | BindingFlags.NonPublic);
            symbolsField.SetValue(_economy, symbols);
            forceField.SetValue(_economy, forceLineOnSpecial);

            FieldInfo economyField = typeof(SpinController).GetField("_economy",
                BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.IsNotNull(economyField, "SpinController._economy field expected");
            economyField.SetValue(_spin, _economy);
        }

        private static SlotEconomyDefinition.Symbol Symbol(string name, int id, float weight,
            bool special = false, float money = 0f)
        {
            return new SlotEconomyDefinition.Symbol
                { Name = name, Id = id, Weight = weight, IsSpecial = special, MoneyReward = money };
        }

        [Test]
        public void BuildEconomyOutcomeMatrix_SingleWeightedSymbol_FillsEveryCell()
        {
            ConfigureEconomy(false, Symbol("Cherry", 5, 1f), Symbol("Lemon", 6, 0f));

            int[,] outcome = _spin.BuildEconomyOutcomeMatrix();

            Assert.AreEqual(3, outcome.GetLength(0));
            Assert.AreEqual(_spin.WindowHeight, outcome.GetLength(1));
            foreach (int id in outcome)
            {
                Assert.AreEqual(5, id, "Only the positively weighted symbol may drop.");
            }
        }

        [Test]
        public void BuildEconomyOutcomeMatrix_NoEconomy_ReturnsEmpty()
        {
            Assert.AreEqual(0, _spin.BuildEconomyOutcomeMatrix().Length);
        }

        [Test]
        public void BuildEconomyOutcomeMatrix_SpecialOnActivePayline_ConvertsWholeLine()
        {
            ConfigureEconomy(true,
                Symbol("Cherry", 1, 1f),
                Symbol("Wild", 9, 0f, special: true));

            int[,] lineRows = _spin.GetActivePaylineWindowRowsMatrix();
            Assert.Greater(lineRows.GetLength(0), 0, "Fallback paylines expected.");
            int cols = lineRows.GetLength(1);
            int windowRows = _spin.WindowHeight;

            // Column-major fill order (x outer, y inner): put ONE special where column 1 crosses line 0.
            int specialColumn = 1;
            int specialRow = lineRows[0, specialColumn];
            var queue = new Queue<int>();
            for (int x = 0; x < cols; x++)
            for (int y = 0; y < windowRows; y++)
            {
                queue.Enqueue(x == specialColumn && y == specialRow ? 9 : 1);
            }

            int[,] outcome = _spin.BuildEconomyOutcomeMatrix(() => queue.Dequeue());

            for (int c = 0; c < cols; c++)
            {
                Assert.AreEqual(9, outcome[c, lineRows[0, c]],
                    "One special on the active payline converts the whole line.");
            }

            for (int x = 0; x < cols; x++)
            for (int y = 0; y < windowRows; y++)
            {
                bool onLine = lineRows[0, x] == y;
                if (!onLine)
                {
                    Assert.AreEqual(1, outcome[x, y], "Cells off the payline stay untouched.");
                }
            }
        }

        [Test]
        public void EvaluateActivePaylinesWithEconomy_NoEconomy_ReturnsEmpty()
        {
            Assert.AreEqual(0, _spin.EvaluateActivePaylinesWithEconomy().Length);
        }

        [Test]
        public void EvaluateActivePaylinesWithEconomy_UnsettledGrid_ReportsLoss()
        {
            ConfigureEconomy(false, Symbol("Cherry", 1, 1f, money: 10f));

            SlotEconomyDefinition.LineResult[] results = _spin.EvaluateActivePaylinesWithEconomy();

            Assert.AreEqual(1, results.Length, "One active payline by default.");
            Assert.IsFalse(results[0].IsWin, "An unsettled grid (ids -1) cannot win.");
        }
    }
}
