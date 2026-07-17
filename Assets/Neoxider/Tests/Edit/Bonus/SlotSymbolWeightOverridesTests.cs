using System.Reflection;
using Neo.Bonus;
using NUnit.Framework;
using UnityEngine;

namespace Neo.Editor.Tests
{
    /// <summary>
    ///     Covers the per-machine symbol weight override table over
    ///     <see cref="SlotEconomyDefinition"/>: disabled fallback, sync against reordered/changed
    ///     symbol lists, zero/negative weights, normalization, and deterministic weighted selection.
    /// </summary>
    [TestFixture]
    public class SlotSymbolWeightOverridesTests
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

        private static SlotEconomyDefinition.Symbol Symbol(string name, int id, float weight)
        {
            return new SlotEconomyDefinition.Symbol { Name = name, Id = id, Weight = weight };
        }

        private void Configure(params SlotEconomyDefinition.Symbol[] symbols)
        {
            if (_economy == null)
            {
                _economy = ScriptableObject.CreateInstance<SlotEconomyDefinition>();
            }

            FieldInfo symbolsField = typeof(SlotEconomyDefinition).GetField("_symbols",
                BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.IsNotNull(symbolsField, "SlotEconomyDefinition._symbols field expected");
            symbolsField.SetValue(_economy, symbols);
        }

        private SlotSymbolWeightOverrides CreateSynced()
        {
            var overrides = new SlotSymbolWeightOverrides();
            overrides.SyncWith(_economy);
            return overrides;
        }

        [Test]
        public void DisabledOverride_UsesDefinitionWeights()
        {
            Configure(Symbol("Cherry", 1, 0f), Symbol("Lemon", 2, 5f));
            SlotSymbolWeightOverrides overrides = CreateSynced();
            overrides.SetWeight(1, 100f); // WHY: would win if the override were active
            overrides.Enabled = false;

            for (int i = 0; i <= 10; i++)
            {
                Assert.AreEqual(2, overrides.PickWeightedId(_economy, i / 10f),
                    "With the override disabled only the definition-weighted symbol may drop.");
            }
        }

        [Test]
        public void EnabledOverride_ReplacesDefinitionWeights()
        {
            Configure(Symbol("Cherry", 1, 5f), Symbol("Lemon", 2, 0f));
            SlotSymbolWeightOverrides overrides = CreateSynced();
            overrides.Enabled = true;
            overrides.SetWeight(1, 0f);
            overrides.SetWeight(2, 3f);

            for (int i = 0; i <= 10; i++)
            {
                Assert.AreEqual(2, overrides.PickWeightedId(_economy, i / 10f),
                    "With the override enabled the local table decides the drop.");
            }
        }

        [Test]
        public void SyncWith_SeedsEntriesFromDefinitionWeights()
        {
            Configure(Symbol("Cherry", 1, 2f), Symbol("Lemon", 2, 3f));
            SlotSymbolWeightOverrides overrides = CreateSynced();

            Assert.AreEqual(2, overrides.Entries.Count);
            Assert.IsTrue(overrides.TryGetWeight(1, out float w1) && Mathf.Approximately(w1, 2f));
            Assert.IsTrue(overrides.TryGetWeight(2, out float w2) && Mathf.Approximately(w2, 3f));
        }

        [Test]
        public void SyncWith_ReorderedSymbols_KeepsWeightsById()
        {
            Configure(Symbol("Cherry", 1, 1f), Symbol("Lemon", 2, 1f));
            SlotSymbolWeightOverrides overrides = CreateSynced();
            overrides.SetWeight(1, 7f);
            overrides.SetWeight(2, 9f);

            Configure(Symbol("Lemon", 2, 1f), Symbol("Cherry", 1, 1f));
            overrides.SyncWith(_economy);

            Assert.AreEqual(2, overrides.Entries.Count);
            Assert.AreEqual(2, overrides.Entries[0].SymbolId, "Entries reorder to match the definition.");
            Assert.IsTrue(overrides.TryGetWeight(1, out float w1) && Mathf.Approximately(w1, 7f));
            Assert.IsTrue(overrides.TryGetWeight(2, out float w2) && Mathf.Approximately(w2, 9f));
        }

        [Test]
        public void SyncWith_ChangedSymbolList_AddsNewAndDropsStale()
        {
            Configure(Symbol("Cherry", 1, 1f), Symbol("Lemon", 2, 1f));
            SlotSymbolWeightOverrides overrides = CreateSynced();
            overrides.SetWeight(2, 5f);

            Configure(Symbol("Cherry", 1, 1f), Symbol("Jackpot", 3, 0.5f));
            bool changed = overrides.SyncWith(_economy);

            Assert.IsTrue(changed);
            Assert.AreEqual(2, overrides.Entries.Count);
            Assert.IsFalse(overrides.TryGetWeight(2, out _), "Stale entry must be dropped.");
            Assert.IsTrue(overrides.TryGetWeight(3, out float w3) && Mathf.Approximately(w3, 0.5f),
                "New symbol seeds from its definition weight.");
        }

        [Test]
        public void SyncWith_NullEconomy_ClearsEntries()
        {
            Configure(Symbol("Cherry", 1, 1f));
            SlotSymbolWeightOverrides overrides = CreateSynced();

            bool changed = overrides.SyncWith(null);

            Assert.IsTrue(changed);
            Assert.AreEqual(0, overrides.Entries.Count);
        }

        [Test]
        public void MissingEntry_FallsBackToDefinitionWeight()
        {
            Configure(Symbol("Cherry", 1, 4f));
            var overrides = new SlotSymbolWeightOverrides { Enabled = true }; // WHY: never synced, no entries

            Assert.AreEqual(1, overrides.PickWeightedId(_economy, 0.5f),
                "Symbols without a local entry keep their definition weight even when enabled.");
        }

        [Test]
        public void ZeroWeight_DisablesSymbol()
        {
            Configure(Symbol("Cherry", 1, 1f), Symbol("Lemon", 2, 1f));
            SlotSymbolWeightOverrides overrides = CreateSynced();
            overrides.Enabled = true;
            overrides.SetWeight(1, 0f);

            for (int i = 0; i <= 10; i++)
            {
                Assert.AreEqual(2, overrides.PickWeightedId(_economy, i / 10f));
            }
        }

        [Test]
        public void NegativeWeight_ClampsToZero()
        {
            Configure(Symbol("Cherry", 1, 1f), Symbol("Lemon", 2, 1f));
            SlotSymbolWeightOverrides overrides = CreateSynced();
            overrides.Enabled = true;
            overrides.SetWeight(1, -5f);

            Assert.IsTrue(overrides.TryGetWeight(1, out float w1));
            Assert.AreEqual(0f, w1, "SetWeight clamps negatives to 0.");
            Assert.AreEqual(2, overrides.PickWeightedId(_economy, 0.99f));
        }

        [Test]
        public void AllWeightsZero_FallsBackToFirstSymbol()
        {
            Configure(Symbol("Cherry", 7, 1f), Symbol("Lemon", 8, 1f));
            SlotSymbolWeightOverrides overrides = CreateSynced();
            overrides.Enabled = true;
            overrides.SetWeight(7, 0f);
            overrides.SetWeight(8, 0f);

            Assert.AreEqual(7, overrides.PickWeightedId(_economy, 0.5f),
                "Same fallback as SlotEconomyDefinition: first symbol id when no weight is positive.");
        }

        [Test]
        public void NormalizeWeights_PositiveWeightsSumToOne()
        {
            Configure(Symbol("Cherry", 1, 1f), Symbol("Lemon", 2, 1f), Symbol("Jackpot", 3, 1f));
            SlotSymbolWeightOverrides overrides = CreateSynced();
            overrides.SetWeight(1, 6f);
            overrides.SetWeight(2, 2f);
            overrides.SetWeight(3, 0f);

            Assert.IsTrue(overrides.NormalizeWeights());

            overrides.TryGetWeight(1, out float w1);
            overrides.TryGetWeight(2, out float w2);
            overrides.TryGetWeight(3, out float w3);
            Assert.AreEqual(0.75f, w1, 1e-5f);
            Assert.AreEqual(0.25f, w2, 1e-5f);
            Assert.AreEqual(0f, w3, "Zero-weight (disabled) entries stay at 0 after normalization.");
        }

        [Test]
        public void NormalizeWeights_NoPositiveWeight_ReturnsFalse()
        {
            Configure(Symbol("Cherry", 1, 1f));
            SlotSymbolWeightOverrides overrides = CreateSynced();
            overrides.SetWeight(1, 0f);

            Assert.IsFalse(overrides.NormalizeWeights());
        }

        [Test]
        public void WeightedSelection_RespectsOverrideProportions()
        {
            Configure(Symbol("Cherry", 1, 1f), Symbol("Lemon", 2, 1f));
            SlotSymbolWeightOverrides overrides = CreateSynced();
            overrides.Enabled = true;
            overrides.SetWeight(1, 3f); // WHY: 75% of the total
            overrides.SetWeight(2, 1f); // WHY: 25% of the total

            Assert.AreEqual(1, overrides.PickWeightedId(_economy, 0.10f));
            Assert.AreEqual(1, overrides.PickWeightedId(_economy, 0.70f));
            Assert.AreEqual(2, overrides.PickWeightedId(_economy, 0.80f));
            Assert.AreEqual(2, overrides.PickWeightedId(_economy, 1.00f));
        }
    }
}
