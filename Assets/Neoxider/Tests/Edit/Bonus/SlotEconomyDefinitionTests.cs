using System.Reflection;
using Neo.Bonus;
using NUnit.Framework;
using UnityEngine;

namespace Neo.Editor.Tests
{
    /// <summary>
    ///     Covers the 9.7.0 slot economy SO: payline evaluation, the special-symbol
    ///     line conversion and the weighted symbol picker.
    /// </summary>
    [TestFixture]
    public class SlotEconomyDefinitionTests
    {
        private SlotEconomyDefinition _economy;

        [TearDown]
        public void TearDown()
        {
            if (_economy != null)
            {
                Object.DestroyImmediate(_economy);
            }
        }

        private static SlotEconomyDefinition.Symbol Symbol(
            string name, int id, float money = 0f, int bonus = 0, bool special = false, float weight = 1f)
        {
            return new SlotEconomyDefinition.Symbol
            {
                Name = name,
                Id = id,
                MoneyReward = money,
                BonusReward = bonus,
                IsSpecial = special,
                Weight = weight
            };
        }

        private void Configure(bool forceLineOnSpecial, params SlotEconomyDefinition.Symbol[] symbols)
        {
            _economy = ScriptableObject.CreateInstance<SlotEconomyDefinition>();

            FieldInfo symbolsField = typeof(SlotEconomyDefinition).GetField("_symbols",
                BindingFlags.Instance | BindingFlags.NonPublic);
            FieldInfo forceField = typeof(SlotEconomyDefinition).GetField("_forceLineOnSpecial",
                BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.IsNotNull(symbolsField, "SlotEconomyDefinition._symbols field expected");
            Assert.IsNotNull(forceField, "SlotEconomyDefinition._forceLineOnSpecial field expected");

            symbolsField.SetValue(_economy, symbols);
            forceField.SetValue(_economy, forceLineOnSpecial);
        }

        [Test]
        public void EvaluateLine_FullLine_PaysSymbolRewards()
        {
            Configure(false,
                Symbol("Empty", 0),
                Symbol("Cherry", 1, 50f, 2));

            SlotEconomyDefinition.LineResult result = _economy.EvaluateLine(new[] { 1, 1, 1 });

            Assert.IsTrue(result.IsWin);
            Assert.AreEqual(50f, result.MoneyReward);
            Assert.AreEqual(2, result.BonusReward);
            Assert.IsFalse(result.SpecialTriggered);
        }

        [Test]
        public void EvaluateLine_MixedLine_Loses()
        {
            Configure(false,
                Symbol("Cherry", 1, 50f),
                Symbol("Lemon", 2, 20f));

            SlotEconomyDefinition.LineResult result = _economy.EvaluateLine(new[] { 1, 2, 1 });

            Assert.IsFalse(result.IsWin);
            Assert.IsNull(result.Symbol);
            Assert.AreEqual(0f, result.MoneyReward);
        }

        [Test]
        public void EvaluateLine_FullLineOfZeroPayoutSymbol_IsNotAWin()
        {
            Configure(false,
                Symbol("Empty", 0));

            SlotEconomyDefinition.LineResult result = _economy.EvaluateLine(new[] { 0, 0, 0 });

            Assert.IsFalse(result.IsWin, "a line of a symbol with no payouts must not count as a win");
        }

        [Test]
        public void EvaluateLine_OneSpecialOnLine_WinsAsSpecial_WhenForced()
        {
            Configure(true,
                Symbol("Cherry", 1, 50f),
                Symbol("Lemon", 2, 20f),
                Symbol("Jackpot", 9, 500f, special: true));

            SlotEconomyDefinition.LineResult result = _economy.EvaluateLine(new[] { 1, 9, 2 });

            Assert.IsTrue(result.IsWin);
            Assert.IsTrue(result.SpecialTriggered);
            Assert.AreEqual(9, result.Symbol.Id);
            Assert.AreEqual(500f, result.MoneyReward);
        }

        [Test]
        public void EvaluateLine_OneSpecialOnLine_Loses_WhenNotForced()
        {
            Configure(false,
                Symbol("Cherry", 1, 50f),
                Symbol("Lemon", 2, 20f),
                Symbol("Jackpot", 9, 500f, special: true));

            SlotEconomyDefinition.LineResult result = _economy.EvaluateLine(new[] { 1, 9, 2 });

            Assert.IsFalse(result.IsWin);
            Assert.IsFalse(result.SpecialTriggered);
        }

        [Test]
        public void ApplySpecialRule_ConvertsWholeLine()
        {
            Configure(true,
                Symbol("Cherry", 1, 50f),
                Symbol("Jackpot", 9, 500f, special: true));

            int[] line = { 1, 9, 1 };
            _economy.ApplySpecialRule(line);

            Assert.AreEqual(new[] { 9, 9, 9 }, line);
        }

        [Test]
        public void ApplySpecialRule_DoesNothing_WhenDisabledOrNoSpecialOnLine()
        {
            Configure(false,
                Symbol("Cherry", 1, 50f),
                Symbol("Jackpot", 9, 500f, special: true));

            int[] line = { 1, 9, 1 };
            _economy.ApplySpecialRule(line);
            Assert.AreEqual(new[] { 1, 9, 1 }, line, "disabled rule must not touch the line");

            Configure(true,
                Symbol("Cherry", 1, 50f),
                Symbol("Jackpot", 9, 500f, special: true));

            int[] cleanLine = { 1, 1, 1 };
            _economy.ApplySpecialRule(cleanLine);
            Assert.AreEqual(new[] { 1, 1, 1 }, cleanLine, "line without a special must stay intact");
        }

        [Test]
        public void PickWeightedId_NeverPicksZeroWeightSymbols()
        {
            Configure(false,
                Symbol("Disabled", 1, weight: 0f),
                Symbol("Only", 2, weight: 5f));

            for (int i = 0; i < 100; i++)
            {
                Assert.AreEqual(2, _economy.PickWeightedId());
            }
        }

        [Test]
        public void PickWeightedId_AllWeightsZero_FallsBackToFirstSymbol()
        {
            Configure(false,
                Symbol("First", 7, weight: 0f),
                Symbol("Second", 8, weight: 0f));

            Assert.AreEqual(7, _economy.PickWeightedId());
        }

        [Test]
        public void Get_And_GetSpecial_LookUpSymbols()
        {
            Configure(true,
                Symbol("Cherry", 1, 50f),
                Symbol("Jackpot", 9, special: true));

            Assert.AreEqual("Cherry", _economy.Get(1).Name);
            Assert.IsNull(_economy.Get(42));
            Assert.AreEqual(9, _economy.GetSpecial().Id);
        }
    }
}
