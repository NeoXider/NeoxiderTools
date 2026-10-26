using System.Collections.Generic;
using Neo.Core.Level;
using NUnit.Framework;

namespace Neo.Editor.Tests
{
    public sealed class Audit20261023NeoCoreLevelTests
    {
        [Test]
        public void NewModel_ExposesDefaultLinearProgress()
        {
            LevelModel model = new LevelModel();

            Assert.That(model.CurrentLevel, Is.EqualTo(1));
            Assert.That(model.TotalXp, Is.Zero);
            Assert.That(model.XpToNextLevel, Is.EqualTo(100));
        }

        [Test]
        public void SetUseXp_UpdatesProgressWhenSwitchingModes()
        {
            LevelModel model = new LevelModel();
            model.SetState(250, 1);
            model.SetUseXp(false);
            model.SetLevel(8);

            Assert.That(model.XpToNextLevel, Is.Zero);
            Assert.That(model.CurrentLevel, Is.EqualTo(8));

            model.SetUseXp(true);

            Assert.That(model.CurrentLevel, Is.EqualTo(3));
            Assert.That(model.TotalXp, Is.EqualTo(250));
            Assert.That(model.XpToNextLevel, Is.EqualTo(50));
        }

        [Test]
        public void AddXp_AtIntegerLimit_ReportsOnlyStoredXpWithoutWrapping()
        {
            LevelModel model = new LevelModel();
            model.SetState(int.MaxValue - 5, 1);
            int added = 0;
            int eventTotal = 0;
            int events = 0;
            model.OnXpGained += (amount, total) =>
            {
                added = amount;
                eventTotal = total;
                events++;
            };

            model.AddXp(10);
            model.AddXp(10);

            Assert.That(model.TotalXp, Is.EqualTo(int.MaxValue));
            Assert.That(model.CurrentLevel, Is.EqualTo(21_474_837));
            Assert.That(added, Is.EqualTo(5));
            Assert.That(eventTotal, Is.EqualTo(int.MaxValue));
            Assert.That(events, Is.EqualTo(1));
        }

        [Test]
        public void AddXp_NestedXpListener_PreservesEachOperationsLevelPayloadAndEventOrder()
        {
            LevelModel model = new LevelModel();
            List<string> events = new List<string>();
            bool nested = false;
            model.OnXpGained += (added, total) =>
            {
                events.Add($"XP:{added}:{total}");
                if (!nested)
                {
                    nested = true;
                    model.AddXp(100);
                }
            };
            model.OnLevelChanged += (previous, current) => events.Add($"Level:{previous}:{current}");

            model.AddXp(100);

            CollectionAssert.AreEqual(new string[]
            {
                "XP:100:100", "XP:100:200", "Level:2:3", "Level:1:2"
            }, events);
            Assert.That(model.TotalXp, Is.EqualTo(200));
            Assert.That(model.CurrentLevel, Is.EqualTo(3));
            Assert.That(model.XpToNextLevel, Is.EqualTo(100));
        }

        [Test]
        public void AddXp_XpListenerResetsState_DoesNotSuppressItsCommittedLevelTransition()
        {
            LevelModel model = new LevelModel();
            List<string> events = new List<string>();
            model.OnXpGained += (added, total) =>
            {
                events.Add($"XP:{added}:{total}");
                model.SetState(0, 1);
            };
            model.OnLevelChanged += (previous, current) => events.Add($"Level:{previous}:{current}");

            model.AddXp(100);

            CollectionAssert.AreEqual(new string[] { "XP:100:100", "Level:1:2" }, events);
            Assert.That(model.TotalXp, Is.Zero);
            Assert.That(model.CurrentLevel, Is.EqualTo(1));
            Assert.That(model.XpToNextLevel, Is.EqualTo(100));
        }

        [Test]
        public void AddXp_XpListenerChangesState_DoesNotInventALevelTransitionForTheOuterOperation()
        {
            LevelModel model = new LevelModel();
            List<string> events = new List<string>();
            model.OnXpGained += (added, total) =>
            {
                events.Add($"XP:{added}:{total}");
                model.SetState(200, 1);
            };
            model.OnLevelChanged += (previous, current) => events.Add($"Level:{previous}:{current}");

            model.AddXp(50);

            CollectionAssert.AreEqual(new string[] { "XP:50:50" }, events);
            Assert.That(model.TotalXp, Is.EqualTo(200));
            Assert.That(model.CurrentLevel, Is.EqualTo(3));
            Assert.That(model.XpToNextLevel, Is.EqualTo(100));
        }

        [Test]
        public void SetLevel_SearchesTheEntireXpRange()
        {
            LevelModel model = new LevelModel();
            model.SetCurve(LevelCurveType.Linear, xpPerLevel: 1_000_000_000);

            model.SetLevel(3);

            Assert.That(model.CurrentLevel, Is.EqualTo(3));
            Assert.That(model.TotalXp, Is.EqualTo(2_000_000_000));
            Assert.That(model.XpToNextLevel, Is.EqualTo(1_000_000_000));
        }

        [Test]
        public void SetLevel_UnreachableCustomLevel_FindsHighestThresholdAboveHalfIntegerRange()
        {
            LevelModel model = new LevelModel();
            model.SetCurve(LevelCurveType.Custom, customEntries: new List<ILevelCurveEntry>
            {
                new Entry(2, 1_500_000_000), new Entry(3, 2_000_000_000)
            });

            model.SetLevel(4);

            Assert.That(model.CurrentLevel, Is.EqualTo(3));
            Assert.That(model.TotalXp, Is.EqualTo(2_000_000_000));
            Assert.That(model.XpToNextLevel, Is.Zero);
        }

        [Test]
        public void Linear_AtIntegerLimit_SaturatesLevelAndHonorsCap()
        {
            Assert.That(LevelCurveEvaluator.EvaluateLevel(int.MaxValue, LevelCurveType.Linear, xpPerLevel: 1),
                Is.EqualTo(int.MaxValue));
            Assert.That(LevelCurveEvaluator.EvaluateLevel(int.MaxValue, LevelCurveType.Linear,
                xpPerLevel: 1, maxLevel: 5), Is.EqualTo(5));
            Assert.That(LevelCurveEvaluator.GetXpToNextLevel(int.MaxValue, LevelCurveType.Linear, xpPerLevel: 1),
                Is.Zero);
            Assert.That(LevelCurveEvaluator.GetXpToNextLevelByFormula(int.MaxValue, LevelFormulaType.Linear,
                xpPerLevel: 1), Is.Zero);
        }

        [TestCase(LevelFormulaType.Power)]
        [TestCase(LevelFormulaType.PolynomialSingle)]
        public void Power_AtIntegerLimit_SaturatesLevel(LevelFormulaType formula)
        {
            Assert.That(LevelCurveEvaluator.EvaluateLevelByFormula(int.MaxValue, formula,
                powerBase: 1f, powerExponent: 1f), Is.EqualTo(int.MaxValue));
            Assert.That(LevelCurveEvaluator.GetXpToNextLevelByFormula(int.MaxValue, formula,
                powerBase: 1f, powerExponent: 1f), Is.Zero);
        }

        [Test]
        public void Quadratic_LargeExactThreshold_ResolvesTheBoundaryWithoutFloatRounding()
        {
            const int threshold = 2_147_395_600;
            Assert.That(LevelCurveEvaluator.GetRequiredXpForLevelFormula(46_341, LevelFormulaType.Quadratic,
                quadraticBase: 1f), Is.EqualTo((double)threshold));
            Assert.That(LevelCurveEvaluator.EvaluateLevel(threshold - 1, LevelCurveType.Quadratic,
                quadraticBase: 1f), Is.EqualTo(46_340));
            Assert.That(LevelCurveEvaluator.EvaluateLevel(threshold, LevelCurveType.Quadratic,
                quadraticBase: 1f), Is.EqualTo(46_341));
            Assert.That(LevelCurveEvaluator.EvaluateLevelByFormula(threshold, LevelFormulaType.Quadratic,
                quadraticBase: 1f), Is.EqualTo(46_341));
        }

        [Test]
        public void LinearWithOffset_LargeXp_PreservesFractionalOffset()
        {
            Assert.That(LevelCurveEvaluator.EvaluateLevelByFormula(16_777_216, LevelFormulaType.LinearWithOffset,
                xpPerLevel: 1, constantOffset: 0.5f), Is.EqualTo(16_777_216));
            Assert.That(LevelCurveEvaluator.GetXpToNextLevelByFormula(16_777_216,
                LevelFormulaType.LinearWithOffset, xpPerLevel: 1, constantOffset: 0.5f), Is.EqualTo(1));
        }

        [Test]
        public void XpToNextLevel_SubtractsBeforeConvertingLargeThresholdToInteger()
        {
            Assert.That(LevelCurveEvaluator.GetXpToNextLevel(int.MaxValue - 10, LevelCurveType.Quadratic),
                Is.EqualTo(838_863));
            Assert.That(LevelCurveEvaluator.GetXpToNextLevelByFormula(int.MaxValue - 10, LevelFormulaType.Quadratic),
                Is.EqualTo(838_863));
            Assert.That(LevelCurveEvaluator.GetXpToNextLevel(0, LevelCurveType.Exponential,
                expBase: 2_000_000_000f, expFactor: 2f), Is.EqualTo(int.MaxValue));
            Assert.That(LevelCurveEvaluator.GetXpToNextLevelByFormula(0, LevelFormulaType.Exponential,
                expBase: 2_000_000_000f, expFactor: 2f), Is.EqualTo(int.MaxValue));
        }

        [Test]
        public void XpToNextLevel_UsesTheSameMinimumParametersAsEvaluation()
        {
            Assert.That(LevelCurveEvaluator.GetXpToNextLevel(0, LevelCurveType.Quadratic,
                quadraticBase: 0f), Is.EqualTo(1));
            Assert.That(LevelCurveEvaluator.GetXpToNextLevel(0, LevelCurveType.Exponential,
                expBase: 0f, expFactor: 0f), Is.EqualTo(1));
            Assert.That(LevelCurveEvaluator.EvaluateLevelByFormula(0, LevelFormulaType.LinearWithOffset,
                constantOffset: float.NaN), Is.EqualTo(1));
            Assert.That(LevelCurveEvaluator.GetXpToNextLevelByFormula(0, LevelFormulaType.LinearWithOffset,
                constantOffset: float.NaN), Is.EqualTo(100));
        }

        [Test]
        public void CustomNextLevel_SkipsEntriesThatCannotIncreaseTheLevel()
        {
            List<ILevelCurveEntry> entries = new List<ILevelCurveEntry>
            {
                new Entry(2, 100), new Entry(2, 200), null, new Entry(3, 400)
            };

            Assert.That(LevelCurveEvaluator.EvaluateLevel(100, LevelCurveType.Custom, customEntries: entries),
                Is.EqualTo(2));
            Assert.That(LevelCurveEvaluator.GetXpToNextLevel(100, LevelCurveType.Custom, customEntries: entries),
                Is.EqualTo(300));
            Assert.That(LevelCurveEvaluator.EvaluateLevel(400, LevelCurveType.Custom, customEntries: entries),
                Is.EqualTo(3));
        }

        private sealed class Entry : ILevelCurveEntry
        {
            public Entry(int level, int requiredXp)
            {
                Level = level;
                RequiredXp = requiredXp;
            }

            public int Level { get; }
            public int RequiredXp { get; }
        }
    }
}
