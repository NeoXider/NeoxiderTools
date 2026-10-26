using System;
using System.Collections.Generic;
using System.Reflection;
using Neo.Bonus;
using NUnit.Framework;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Neo.Editor.Tests
{
    public sealed class Audit20261023NeoBonusSlotTests
    {
        private readonly List<Object> _assets = new();
        private GameObject _root;

        [SetUp]
        public void SetUp()
        {
            _root = new GameObject(nameof(Audit20261023NeoBonusSlotTests));
        }

        [TearDown]
        public void TearDown()
        {
            Object.DestroyImmediate(_root);
            foreach (Object asset in _assets)
            {
                Object.DestroyImmediate(asset);
            }

            _assets.Clear();
        }

        [Test]
        public void CheckSpin_MissingCellSentinels_DoNotWin_WhileValidRunsStillPay()
        {
            CheckSpin check = new();
            int[,] missing = { { -1 }, { -1 }, { -1 } };
            Assert.That(check.GetWinningLines(missing, 1), Is.Empty);
            Assert.That(check.GetMultiplayers(missing, 1), Is.Empty);

            int[,] mixed = { { -1 }, { 4 }, { 4 }, { 4 } };
            CollectionAssert.AreEqual(new[] { 0 }, check.GetWinningLines(mixed, 1));
            CollectionAssert.AreEqual(new[] { 1f }, check.GetMultiplayers(mixed, 1));
        }

        [Test]
        public void CheckSpin_NoGrid_ProducesEmptyWinningAndPayoutResults()
        {
            CheckSpin check = new();
            Assert.That(check.GetWinningLines(null, 1), Is.Empty);
            Assert.That(check.GetMultiplayers(null, 1), Is.Empty);
            Assert.That(check.GetWinningLines(new int[3, 0], 1), Is.Empty);
        }

        [TestCase(false)]
        [TestCase(true)]
        public void AddAndRemoveLine_WithoutVisualObjects_CycleOnlyThroughDefinedPaylines(bool emptyVisualArray)
        {
            SpinController spin = _root.AddComponent<SpinController>();
            Row row = NewRow(3);
            spin.Rows = new[] { row };
            spin.VisibleWindowRows = 3;
            spin.betsData = NewAsset<BetsData>();
            if (emptyVisualArray)
            {
                SetField(spin, "_lineSlot", new VisualSlotLines { lines = Array.Empty<GameObject>() });
            }

            spin.ActivePaylineCount = 1;
            spin.AddLine();
            Assert.That(spin.ActivePaylineCount, Is.EqualTo(2));
            Assert.That(spin.CurrentSpinPrice, Is.EqualTo(20));
            spin.AddLine();
            Assert.That(spin.ActivePaylineCount, Is.EqualTo(3));
            spin.AddLine();
            Assert.That(spin.ActivePaylineCount, Is.EqualTo(1));
            spin.RemoveLine();
            Assert.That(spin.ActivePaylineCount, Is.EqualTo(3));
            Assert.That(spin.CurrentSpinPrice, Is.EqualTo(30));
        }

        [Test]
        public void LargeBetAcrossMultipleLines_RemainsChargeableInsteadOfWrappingToFree()
        {
            SpinController spin = _root.AddComponent<SpinController>();
            spin.Rows = new[] { NewRow(3) };
            spin.betsData = NewAsset<BetsData>();
            SetField(spin.betsData, "_bets", new[] { int.MaxValue });
            spin.ActivePaylineCount = 3;

            Assert.That(spin.CurrentSpinPrice, Is.EqualTo(int.MaxValue));
            Assert.That(spin.TryPayForSpin(), Is.False, "A positive price requires a wallet.");
        }

        [Test]
        public void VisibleMatrix_WithAnEmptyVisualEntry_ResolvesTheMatchingAssignedSymbol()
        {
            Row row = NewRow(1);
            SlotVisualData symbol = new() { id = 7 };
            row.SetVisuals(symbol);
            SpinController spin = _root.AddComponent<SpinController>();
            spin.Rows = new[] { row };
            spin.VisibleWindowRows = 1;
            spin.allSpritesData = NewAsset<SpritesData>();
            SetField(spin.allSpritesData, "_visuals", new SlotVisualData[] { null, symbol });

            int[,] ids = spin.GetElementIDsMatrix();

            Assert.That(ids.GetLength(0), Is.EqualTo(1));
            Assert.That(ids.GetLength(1), Is.EqualTo(1));
            Assert.That(ids[0, 0], Is.EqualTo(7));
        }

        [Test]
        public void Layout_WithAnUnassignedColumn_StillConfiguresTheAssignedReel()
        {
            SpinController spin = _root.AddComponent<SpinController>();
            Row row = NewRow(1);
            SetField(spin, "_setSpace", true);
            spin.Rows = new Row[] { null, row };
            spin.VisibleWindowRows = 2;

            Assert.That(row.countSlotElement, Is.EqualTo(2));
            Assert.That(row.spaceY, Is.EqualTo(1f));
            Assert.That(spin.GetRuntimeSnapshot().ColumnCount, Is.EqualTo(2));
        }

        [Test]
        public void DestroyedReelElement_DoesNotPreventVisibleRecoveryOrForcedSettlement()
        {
            Row row = NewRow(1);
            SlotElement destroyed = row.SlotElements[0];
            SlotElement live = row.SlotElements[1];
            Object.DestroyImmediate(destroyed.gameObject);
            live.transform.localPosition = Vector3.up * 100f; // Force visible-bucket recovery.
            SlotVisualData visual = new() { id = 9 };
            row.SetVisuals(visual);
            Assert.That(live.id, Is.EqualTo(9));
            Assert.That(row.GetVisibleBottomUp()[0], Is.SameAs(live));
            SpritesData sprites = NewAsset<SpritesData>();
            SetField(sprites, "_visuals", new[] { visual });

            row.Spin(sprites, new[] { 9 });
            Assert.That(row.CompleteSpinImmediately(), Is.True);
            Assert.That(row.is_spinning, Is.False);
            Assert.That(row.GetVisibleBottomUp()[0].id, Is.EqualTo(9));
            Assert.That(row.CompleteSpinImmediately(), Is.False);
        }

        [Test]
        public void MultiplierGeneration_OnAFreshAsset_ProducesRowsForAssignedSymbolsOnly()
        {
            SpritesData sprites = NewAsset<SpritesData>();
            SetField(sprites, "_visuals", new SlotVisualData[] { null, new() { id = 7 } });
            SpriteMultiplayerData multipliers = NewAsset<SpriteMultiplayerData>();
            SetField(multipliers, "_spritesData", sprites);
            SetField(multipliers, "_spritesMultiplier", null);
            SetField(multipliers, "_minCount", 3);
            SetField(multipliers, "_maxCount", 4);
            SetField(multipliers, "_generate", true);
            // OnValidate is the existing authoring entry point; inspect its generated public data.
            MethodInfo validate = typeof(SpriteMultiplayerData).GetMethod("OnValidate",
                BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.That(validate, Is.Not.Null);
            validate.Invoke(multipliers, null);

            Assert.That(multipliers.spritesMultiplier.spriteMults.Length, Is.EqualTo(1));
            SpriteMultiplayerData.IdMult symbol = multipliers.spritesMultiplier.spriteMults[0];
            Assert.That(symbol.id, Is.EqualTo(7));
            Assert.That(symbol.countMult.Length, Is.EqualTo(2));
            Assert.That(symbol.countMult[0].count, Is.EqualTo(3));
            Assert.That(symbol.countMult[1].count, Is.EqualTo(4));
            Assert.That(symbol.countMult[1].mult, Is.EqualTo(1f));
        }

        [Test]
        public void LargeFiniteWeights_PreserveTheirRatios_AndNeverPickADisabledTail()
        {
            SlotEconomyDefinition economy = NewAsset<SlotEconomyDefinition>();
            SetField(economy, "_symbols", new[]
            {
                new SlotEconomyDefinition.Symbol { Id = 10, Weight = float.MaxValue },
                new SlotEconomyDefinition.Symbol { Id = 20, Weight = float.MaxValue },
                new SlotEconomyDefinition.Symbol { Id = 30, Weight = 0f }
            });

            Assert.That(economy.PickWeightedId(null, 0.25f), Is.EqualTo(10));
            Assert.That(economy.PickWeightedId(null, 0.75f), Is.EqualTo(20));
            Assert.That(economy.PickWeightedId(null, 1f), Is.EqualTo(20));
        }

        [TestCase(float.NaN)]
        [TestCase(float.PositiveInfinity)]
        public void NonFiniteWeights_CannotPoisonThePickerOrEnableZeroWeightSymbols(float invalidWeight)
        {
            SlotEconomyDefinition economy = NewAsset<SlotEconomyDefinition>();
            SetField(economy, "_symbols", new[]
            {
                new SlotEconomyDefinition.Symbol { Id = 10, Weight = invalidWeight },
                new SlotEconomyDefinition.Symbol { Id = 20, Weight = 1f },
                new SlotEconomyDefinition.Symbol { Id = 30, Weight = 0f }
            });

            Assert.That(economy.PickWeightedId(null, 0.25f), Is.EqualTo(20));
            Assert.That(economy.PickWeightedId(null, 1f), Is.EqualTo(20));
        }

        [Test]
        public void NormalizeLargeOverrideWeights_PreservesAUsableDistribution_WithoutEditingTheEconomy()
        {
            SlotEconomyDefinition economy = NewAsset<SlotEconomyDefinition>();
            SetField(economy, "_symbols", new[]
            {
                new SlotEconomyDefinition.Symbol { Id = 10, Weight = 1f },
                new SlotEconomyDefinition.Symbol { Id = 20, Weight = 1f }
            });
            SlotSymbolWeightOverrides weights = new() { Enabled = true };
            weights.SyncWith(economy);
            weights.SetWeight(10, float.MaxValue);
            weights.SetWeight(20, float.MaxValue);

            Assert.That(weights.NormalizeWeights(), Is.True);
            Assert.That(weights.TryGetWeight(10, out float first), Is.True);
            Assert.That(weights.TryGetWeight(20, out float second), Is.True);
            Assert.That(first, Is.EqualTo(0.5f));
            Assert.That(second, Is.EqualTo(0.5f));
            Assert.That(weights.PickWeightedId(economy, 0.75f), Is.EqualTo(20));
            Assert.That(economy.Get(10).Weight, Is.EqualTo(1f));
        }

        [Test]
        public void WheelWithoutATransform_DoesNotStartOrConsumeItsSingleUse()
        {
            WheelFortune wheel = _root.AddComponent<WheelFortune>();
            SetField(wheel, "items", new[] { _root });

            wheel.Spin();

            Assert.That(wheel.State, Is.EqualTo(WheelFortune.SpinState.Idle));
            Assert.That(wheel.canUse, Is.True);
        }

        [Test]
        public void WheelAuthoring_EmptyPrizeSlots_PreserveSectorIndicesAndArrangeAssignedPrizes()
        {
            WheelFortune wheel = _root.AddComponent<WheelFortune>();
            GameObject prize = new("Prize");
            prize.transform.SetParent(_root.transform, false);
            SetField(wheel, "items", new GameObject[] { null, prize });
            MethodInfo validate = typeof(WheelFortune).GetMethod("OnValidate",
                BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.That(validate, Is.Not.Null);

            validate.Invoke(wheel, null);

            Assert.That(prize.transform.localPosition.x, Is.EqualTo(0f).Within(0.001f));
            Assert.That(prize.transform.localPosition.y, Is.EqualTo(-200f).Within(0.001f));
            Assert.That(wheel.GetPrize(0), Is.Null);
            Assert.That(wheel.GetPrize(1), Is.SameAs(prize));
        }

        private Row NewRow(int visibleRows)
        {
            GameObject rowObject = new("Row");
            rowObject.transform.SetParent(_root.transform, false);
            Row row = rowObject.AddComponent<Row>();
            row.countSlotElement = visibleRows;
            row.spaceY = 10f;
            row.offsetY = 0f;
            for (int i = 0; i < visibleRows * 2; i++)
            {
                GameObject cell = new("Cell" + i);
                cell.transform.SetParent(rowObject.transform, false);
                cell.AddComponent<SlotElement>();
            }

            row.ApplyLayout();
            return row;
        }

        private T NewAsset<T>() where T : ScriptableObject
        {
            T asset = ScriptableObject.CreateInstance<T>();
            _assets.Add(asset);
            return asset;
        }

        private static void SetField(object target, string name, object value)
        {
            FieldInfo field = target.GetType().GetField(name, BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.That(field, Is.Not.Null, name);
            field.SetValue(target, value);
        }
    }
}
