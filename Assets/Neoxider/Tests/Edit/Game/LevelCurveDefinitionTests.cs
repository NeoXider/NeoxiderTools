using System;
using System.Collections.Generic;
using System.Reflection;
using Neo.Core.Level;
using Neo.Reactive;
using NUnit.Framework;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Neo.Core.Tests
{
    public class LevelCurveDefinitionTests
    {
        private const int XpPerLevel = 100;
        private const float QuadraticBase = 50f;
        private const float ExpBase = 100f;
        private const float ExpFactor = 1.5f;
        private const float PowerBase = 10f;
        private const float PowerExponent = 2f;

        [Test]
        public void Formula_Linear_LevelFromXp()
        {
            Assert.That(LevelCurveEvaluator.EvaluateLevelByFormula(0, LevelFormulaType.Linear), Is.EqualTo(1));
            Assert.That(LevelCurveEvaluator.EvaluateLevelByFormula(99, LevelFormulaType.Linear), Is.EqualTo(1));
            Assert.That(LevelCurveEvaluator.EvaluateLevelByFormula(100, LevelFormulaType.Linear), Is.EqualTo(2));
            Assert.That(LevelCurveEvaluator.EvaluateLevelByFormula(199, LevelFormulaType.Linear), Is.EqualTo(2));
            Assert.That(LevelCurveEvaluator.EvaluateLevelByFormula(200, LevelFormulaType.Linear), Is.EqualTo(3));
            Assert.That(LevelCurveEvaluator.EvaluateLevelByFormula(500, LevelFormulaType.Linear), Is.EqualTo(6));
        }

        [Test]
        public void Formula_Linear_RequiredXpForLevel()
        {
            Assert.That(LevelCurveEvaluator.GetRequiredXpForLevelFormula(1, LevelFormulaType.Linear), Is.EqualTo(0));
            Assert.That(LevelCurveEvaluator.GetRequiredXpForLevelFormula(2, LevelFormulaType.Linear), Is.EqualTo(100));
            Assert.That(LevelCurveEvaluator.GetRequiredXpForLevelFormula(3, LevelFormulaType.Linear), Is.EqualTo(200));
            Assert.That(LevelCurveEvaluator.GetRequiredXpForLevelFormula(6, LevelFormulaType.Linear), Is.EqualTo(500));
        }

        [Test]
        public void Formula_Linear_MaxLevelCap()
        {
            int level = LevelCurveEvaluator.EvaluateLevelByFormula(10000, LevelFormulaType.Linear, maxLevel: 5);
            Assert.That(level, Is.EqualTo(5));
        }

        [Test]
        public void Formula_LinearWithOffset_LevelFromXp()
        {
            const float offset = 50f;
            Assert.That(
                LevelCurveEvaluator.EvaluateLevelByFormula(0, LevelFormulaType.LinearWithOffset, XpPerLevel, offset),
                Is.EqualTo(1));
            Assert.That(
                LevelCurveEvaluator.EvaluateLevelByFormula(49, LevelFormulaType.LinearWithOffset, XpPerLevel, offset),
                Is.EqualTo(1));
            Assert.That(
                LevelCurveEvaluator.EvaluateLevelByFormula(50, LevelFormulaType.LinearWithOffset, XpPerLevel, offset),
                Is.EqualTo(1));
            Assert.That(
                LevelCurveEvaluator.EvaluateLevelByFormula(150, LevelFormulaType.LinearWithOffset, XpPerLevel, offset),
                Is.EqualTo(2));
            Assert.That(
                LevelCurveEvaluator.EvaluateLevelByFormula(250, LevelFormulaType.LinearWithOffset, XpPerLevel, offset),
                Is.EqualTo(3));
        }

        [Test]
        public void Formula_LinearWithOffset_RequiredXpForLevel()
        {
            const float offset = 50f;
            Assert.That(
                LevelCurveEvaluator.GetRequiredXpForLevelFormula(1, LevelFormulaType.LinearWithOffset, XpPerLevel,
                    offset), Is.EqualTo(50));
            Assert.That(
                LevelCurveEvaluator.GetRequiredXpForLevelFormula(2, LevelFormulaType.LinearWithOffset, XpPerLevel,
                    offset), Is.EqualTo(150));
            Assert.That(
                LevelCurveEvaluator.GetRequiredXpForLevelFormula(3, LevelFormulaType.LinearWithOffset, XpPerLevel,
                    offset), Is.EqualTo(250));
        }

        [Test]
        public void Formula_Quadratic_LevelFromXp()
        {
            // WHY: RequiredXp(level) = base * level^2 => level = sqrt(totalXp/base), 1-based
            Assert.That(
                LevelCurveEvaluator.EvaluateLevelByFormula(0, LevelFormulaType.Quadratic, quadraticBase: QuadraticBase),
                Is.EqualTo(1));
            Assert.That(
                LevelCurveEvaluator.EvaluateLevelByFormula(49, LevelFormulaType.Quadratic,
                    quadraticBase: QuadraticBase), Is.EqualTo(1));
            Assert.That(
                LevelCurveEvaluator.EvaluateLevelByFormula(50, LevelFormulaType.Quadratic,
                    quadraticBase: QuadraticBase), Is.EqualTo(2)); // WHY: 50*1^2=50, 50*2^2=200
            Assert.That(
                LevelCurveEvaluator.EvaluateLevelByFormula(199, LevelFormulaType.Quadratic,
                    quadraticBase: QuadraticBase), Is.EqualTo(2));
            Assert.That(
                LevelCurveEvaluator.EvaluateLevelByFormula(200, LevelFormulaType.Quadratic,
                    quadraticBase: QuadraticBase), Is.EqualTo(3)); // WHY: 50*3^2=450
        }

        [Test]
        public void Formula_Quadratic_RequiredXpForLevel()
        {
            Assert.That(
                LevelCurveEvaluator.GetRequiredXpForLevelFormula(1, LevelFormulaType.Quadratic,
                    quadraticBase: QuadraticBase), Is.EqualTo(0));
            Assert.That(
                LevelCurveEvaluator.GetRequiredXpForLevelFormula(2, LevelFormulaType.Quadratic,
                    quadraticBase: QuadraticBase), Is.EqualTo(50));
            Assert.That(
                LevelCurveEvaluator.GetRequiredXpForLevelFormula(3, LevelFormulaType.Quadratic,
                    quadraticBase: QuadraticBase), Is.EqualTo(200));
        }

        [Test]
        public void Formula_Exponential_LevelFromXp()
        {
            Assert.That(
                LevelCurveEvaluator.EvaluateLevelByFormula(0, LevelFormulaType.Exponential, expBase: ExpBase,
                    expFactor: ExpFactor), Is.EqualTo(1));
            Assert.That(
                LevelCurveEvaluator.EvaluateLevelByFormula(99, LevelFormulaType.Exponential, expBase: ExpBase,
                    expFactor: ExpFactor), Is.EqualTo(1));
            // WHY: RequiredXp(1)=100, RequiredXp(2)=150 => level 2 at 150, level 3 at 225
            Assert.That(
                LevelCurveEvaluator.EvaluateLevelByFormula(150, LevelFormulaType.Exponential, expBase: ExpBase,
                    expFactor: ExpFactor), Is.EqualTo(2));
            Assert.That(
                LevelCurveEvaluator.EvaluateLevelByFormula(224, LevelFormulaType.Exponential, expBase: ExpBase,
                    expFactor: ExpFactor), Is.EqualTo(2));
            Assert.That(
                LevelCurveEvaluator.EvaluateLevelByFormula(225, LevelFormulaType.Exponential, expBase: ExpBase,
                    expFactor: ExpFactor), Is.EqualTo(3));
        }

        [Test]
        public void Formula_Exponential_RequiredXpForLevel()
        {
            double l1 = LevelCurveEvaluator.GetRequiredXpForLevelFormula(1, LevelFormulaType.Exponential,
                expBase: ExpBase, expFactor: ExpFactor);
            double l2 = LevelCurveEvaluator.GetRequiredXpForLevelFormula(2, LevelFormulaType.Exponential,
                expBase: ExpBase, expFactor: ExpFactor);
            Assert.That(l1, Is.EqualTo(100)); // WHY: 100 * 1.5^0
            Assert.That(l2, Is.EqualTo(150)); // WHY: 100 * 1.5^1
        }

        [Test]
        public void Formula_Power_LevelFromXp()
        {
            // WHY: RequiredXp(level) = powerBase * level^powerExponent => 10*level^2
            Assert.That(
                LevelCurveEvaluator.EvaluateLevelByFormula(0, LevelFormulaType.Power, powerBase: PowerBase,
                    powerExponent: PowerExponent), Is.EqualTo(1));
            Assert.That(
                LevelCurveEvaluator.EvaluateLevelByFormula(9, LevelFormulaType.Power, powerBase: PowerBase,
                    powerExponent: PowerExponent), Is.EqualTo(1));
            Assert.That(
                LevelCurveEvaluator.EvaluateLevelByFormula(10, LevelFormulaType.Power, powerBase: PowerBase,
                    powerExponent: PowerExponent), Is.EqualTo(2)); // WHY: 10*1^2=10, 10*2^2=40
            Assert.That(
                LevelCurveEvaluator.EvaluateLevelByFormula(40, LevelFormulaType.Power, powerBase: PowerBase,
                    powerExponent: PowerExponent), Is.EqualTo(3)); // WHY: 10*3^2=90
        }

        [Test]
        public void Formula_Power_RequiredXpForLevel()
        {
            Assert.That(
                LevelCurveEvaluator.GetRequiredXpForLevelFormula(1, LevelFormulaType.Power, powerBase: PowerBase,
                    powerExponent: PowerExponent), Is.EqualTo(0));
            Assert.That(
                LevelCurveEvaluator.GetRequiredXpForLevelFormula(2, LevelFormulaType.Power, powerBase: PowerBase,
                    powerExponent: PowerExponent), Is.EqualTo(10));
            Assert.That(
                LevelCurveEvaluator.GetRequiredXpForLevelFormula(3, LevelFormulaType.Power, powerBase: PowerBase,
                    powerExponent: PowerExponent), Is.EqualTo(40));
        }

        [Test]
        public void Formula_Linear_XpToNextLevel()
        {
            int next = LevelCurveEvaluator.GetXpToNextLevelByFormula(0, LevelFormulaType.Linear);
            Assert.That(next, Is.EqualTo(100));
            next = LevelCurveEvaluator.GetXpToNextLevelByFormula(50, LevelFormulaType.Linear);
            Assert.That(next, Is.EqualTo(50));
            next = LevelCurveEvaluator.GetXpToNextLevelByFormula(100, LevelFormulaType.Linear);
            Assert.That(next, Is.EqualTo(100));
            next = LevelCurveEvaluator.GetXpToNextLevelByFormula(250, LevelFormulaType.Linear);
            Assert.That(next, Is.EqualTo(50));
        }

        [Test]
        public void CurveType_XpToNextLevel_MatchesLevelUpThreshold()
        {
            // WHY: regression for off-by-one — XP to next must hit zero exactly when the level flips.
            Assert.That(LevelCurveEvaluator.GetXpToNextLevel(0, LevelCurveType.Linear), Is.EqualTo(100));
            Assert.That(LevelCurveEvaluator.GetXpToNextLevel(50, LevelCurveType.Linear), Is.EqualTo(50));
            Assert.That(LevelCurveEvaluator.GetXpToNextLevel(100, LevelCurveType.Linear), Is.EqualTo(100));
            Assert.That(LevelCurveEvaluator.EvaluateLevel(100, LevelCurveType.Linear), Is.EqualTo(2));

            Assert.That(
                LevelCurveEvaluator.GetXpToNextLevel(0, LevelCurveType.Quadratic, quadraticBase: 100f),
                Is.EqualTo(100));
            Assert.That(LevelCurveEvaluator.EvaluateLevel(100, LevelCurveType.Quadratic, quadraticBase: 100f),
                Is.EqualTo(2));
            Assert.That(
                LevelCurveEvaluator.GetXpToNextLevel(100, LevelCurveType.Quadratic, quadraticBase: 100f),
                Is.EqualTo(300)); // WHY: level 3 needs 100*2^2 = 400 total

            Assert.That(
                LevelCurveEvaluator.GetXpToNextLevel(0, LevelCurveType.Exponential, expBase: ExpBase,
                    expFactor: ExpFactor), Is.EqualTo(150)); // WHY: level 2 needs 100*1.5^1 = 150 total
            Assert.That(
                LevelCurveEvaluator.EvaluateLevel(150, LevelCurveType.Exponential, expBase: ExpBase,
                    expFactor: ExpFactor), Is.EqualTo(2));
        }

        [Test]
        public void CurveType_GetXpToNextLevel_ConsistentWithEvaluateLevel()
        {
            // WHY: gaining exactly XpToNextLevel XP must advance the level by exactly one.
            foreach (LevelCurveType type in new[]
                     {
                         LevelCurveType.Linear, LevelCurveType.Quadratic, LevelCurveType.Exponential
                     })
            {
                int totalXp = 0;
                for (int step = 0; step < 8; step++)
                {
                    int level = LevelCurveEvaluator.EvaluateLevel(totalXp, type);
                    int toNext = LevelCurveEvaluator.GetXpToNextLevel(totalXp, type);
                    Assert.That(toNext, Is.GreaterThan(0), $"{type}: xpToNext at {totalXp}");
                    Assert.That(LevelCurveEvaluator.EvaluateLevel(totalXp + toNext - 1, type), Is.EqualTo(level),
                        $"{type}: leveled up before the counter reached 0 (totalXp={totalXp}, toNext={toNext})");
                    Assert.That(LevelCurveEvaluator.EvaluateLevel(totalXp + toNext, type), Is.EqualTo(level + 1),
                        $"{type}: did not level up when the counter reached 0 (totalXp={totalXp}, toNext={toNext})");
                    totalXp += toNext;
                }
            }
        }

        [Test]
        public void LevelModel_SetLevel_UnreachableLevel_DoesNotCorruptTotalXp()
        {
            var model = new LevelModel();
            model.SetCurve(LevelCurveType.Custom, customEntries: new List<LevelCurveEntry>
            {
                new(1, 0),
                new(2, 100),
                new(3, 250),
                new(4, 500),
                new(5, 800)
            });

            model.SetLevel(10);

            // WHY: regression — unreachable level used to binary-search to TotalXp = int.MaxValue.
            Assert.That(model.CurrentLevel, Is.EqualTo(5));
            Assert.That(model.TotalXp, Is.EqualTo(800));
        }

        [Test]
        public void Formula_Linear_XpToNextLevel_RespectsMaxLevel()
        {
            int next = LevelCurveEvaluator.GetXpToNextLevelByFormula(500, LevelFormulaType.Linear, maxLevel: 5);
            Assert.That(next, Is.EqualTo(0), "At max level XP to next should be 0");
        }

        [Test]
        public void Definition_FormulaLinear_SetLinear_Works()
        {
            LevelCurveDefinition def = ScriptableObject.CreateInstance<LevelCurveDefinition>();
            try
            {
                def.SetLinear(100);
                Assert.That(def.EvaluateLevel(0), Is.EqualTo(1));
                Assert.That(def.EvaluateLevel(100), Is.EqualTo(2));
                Assert.That(def.EvaluateLevel(250), Is.EqualTo(3));
                Assert.That(def.GetRequiredXpForLevel(2), Is.EqualTo(100));
                Assert.That(def.GetXpToNextLevel(50), Is.EqualTo(50));
            }
            finally
            {
                Object.DestroyImmediate(def);
            }
        }

        [Test]
        public void Definition_Custom_LevelFromXp()
        {
            LevelCurveDefinition def = ScriptableObject.CreateInstance<LevelCurveDefinition>();
            var entries = new List<LevelCurveEntry>
            {
                new(1, 0),
                new(2, 100),
                new(3, 250),
                new(4, 500)
            };
            SetPrivateField(def, "_mode", LevelCurveMode.Custom);
            SetPrivateField(def, "_customEntries", entries);
            try
            {
                Assert.That(def.EvaluateLevel(0), Is.EqualTo(1));
                Assert.That(def.EvaluateLevel(99), Is.EqualTo(1));
                Assert.That(def.EvaluateLevel(100), Is.EqualTo(2));
                Assert.That(def.EvaluateLevel(249), Is.EqualTo(2));
                Assert.That(def.EvaluateLevel(250), Is.EqualTo(3));
                Assert.That(def.EvaluateLevel(499), Is.EqualTo(3));
                Assert.That(def.EvaluateLevel(500), Is.EqualTo(4));
                Assert.That(def.GetRequiredXpForLevel(1), Is.EqualTo(0));
                Assert.That(def.GetRequiredXpForLevel(2), Is.EqualTo(100));
                Assert.That(def.GetRequiredXpForLevel(4), Is.EqualTo(500));
                Assert.That(def.GetXpToNextLevel(0), Is.EqualTo(100));
                Assert.That(def.GetXpToNextLevel(100), Is.EqualTo(150));
            }
            finally
            {
                Object.DestroyImmediate(def);
            }
        }

        [Test]
        public void Definition_Curve_LevelFromXp()
        {
            var curve = new AnimationCurve(
                new Keyframe(1f, 0f),
                new Keyframe(2f, 100f),
                new Keyframe(3f, 250f),
                new Keyframe(4f, 500f));
            LevelCurveDefinition def = ScriptableObject.CreateInstance<LevelCurveDefinition>();
            SetPrivateField(def, "_mode", LevelCurveMode.Curve);
            SetPrivateField(def, "_animationCurve", curve);
            try
            {
                Assert.That(def.EvaluateLevel(0), Is.EqualTo(1));
                Assert.That(def.EvaluateLevel(99), Is.EqualTo(1));
                Assert.That(def.EvaluateLevel(100), Is.EqualTo(2));
                Assert.That(def.EvaluateLevel(249), Is.EqualTo(2));
                Assert.That(def.EvaluateLevel(250), Is.EqualTo(3));
                Assert.That(def.EvaluateLevel(500), Is.EqualTo(4));
                Assert.That(def.GetRequiredXpForLevel(1), Is.EqualTo(0));
                Assert.That(def.GetRequiredXpForLevel(2), Is.EqualTo(100));
                Assert.That(def.GetRequiredXpForLevel(4), Is.EqualTo(500));
            }
            finally
            {
                Object.DestroyImmediate(def);
            }
        }

        [Test]
        public void LevelComponent_AddXp_UsesIncreasingCurveRequirements()
        {
            var curve = new AnimationCurve(
                new Keyframe(1f, 0f),
                new Keyframe(2f, 100f),
                new Keyframe(3f, 250f),
                new Keyframe(4f, 500f));
            LevelCurveDefinition def = ScriptableObject.CreateInstance<LevelCurveDefinition>();
            var host = new GameObject("LevelComponent_AddXp_UsesIncreasingCurveRequirements");
            SetPrivateField(def, "_mode", LevelCurveMode.Curve);
            SetPrivateField(def, "_animationCurve", curve);

            try
            {
                LevelComponent component = host.AddComponent<LevelComponent>();
                component.LevelCurveDefinition = def;
                SetPrivateField(component, "_loadOnAwake", false);

                component.AddXp(175);

                Assert.That(component.TotalXp, Is.EqualTo(175));
                Assert.That(component.Level, Is.EqualTo(2));
                Assert.That(component.XpToNextLevel, Is.EqualTo(75));
            }
            finally
            {
                Object.DestroyImmediate(host);
                Object.DestroyImmediate(def);
            }
        }

        [Test]
        public void LevelComponent_SetLevel_WhenUseXp_SyncsTotalXpToLevelThreshold()
        {
            var host = new GameObject("LevelComponent_SetLevel_WhenUseXp_SyncsTotalXpToLevelThreshold");
            try
            {
                LevelComponent component = host.AddComponent<LevelComponent>();
                SetPrivateField(component, "_loadOnAwake", false);

                component.SetLevel(5);

                Assert.That(component.Level, Is.EqualTo(5));
                Assert.That(component.TotalXp, Is.EqualTo(400));
                // WHY: level 6 needs 500 total XP ((L-1)*100), so 100 remains from 400 — not 200.
                Assert.That(component.XpToNextLevel, Is.EqualTo(100));
            }
            finally
            {
                Object.DestroyImmediate(host);
            }
        }

        [Test]
        public void LevelNoCodeAction_AddXp_DoesNotRaiseLevelUpWhenLevelUnchanged()
        {
            var host = new GameObject("LevelNoCodeAction_AddXp_DoesNotRaiseLevelUpWhenLevelUnchanged");
            try
            {
                LevelComponent component = host.AddComponent<LevelComponent>();
                SetPrivateField(component, "_loadOnAwake", false);
                component.EnsureInitialized();

                LevelNoCodeAction action = host.AddComponent<LevelNoCodeAction>();
                SetPrivateField(action, "_levelProvider", component);
                SetPrivateField(action, "_actionType", LevelNoCodeActionType.AddXp);
                SetPrivateField(action, "_xpAmount", 1);

                int levelUpCount = 0;
                UnityEventInt onLevelUp = GetPrivateField<UnityEventInt>(action, "_onLevelUp");
                onLevelUp.AddListener(_ => levelUpCount++);

                action.Execute();

                Assert.That(component.Level, Is.EqualTo(1));
                Assert.That(levelUpCount, Is.EqualTo(0));
            }
            finally
            {
                Object.DestroyImmediate(host);
            }
        }

        [Test]
        public void Formula_Linear_Consistency_RequiredXpRoundTrip()
        {
            for (int level = 1; level <= 20; level++)
            {
                double required = LevelCurveEvaluator.GetRequiredXpForLevelFormula(level, LevelFormulaType.Linear);
                int totalXp = (int)Math.Ceiling(required);
                int evaluated = LevelCurveEvaluator.EvaluateLevelByFormula(totalXp, LevelFormulaType.Linear);
                Assert.That(evaluated, Is.EqualTo(level),
                    $"Level {level}: requiredXp={required} => evaluated level {evaluated}");
            }
        }

        [Test]
        public void Formula_Quadratic_Consistency_RequiredXpRoundTrip()
        {
            for (int level = 1; level <= 15; level++)
            {
                double required = LevelCurveEvaluator.GetRequiredXpForLevelFormula(level, LevelFormulaType.Quadratic,
                    quadraticBase: QuadraticBase);
                int totalXp = (int)Math.Ceiling(required);
                int evaluated = LevelCurveEvaluator.EvaluateLevelByFormula(totalXp, LevelFormulaType.Quadratic,
                    quadraticBase: QuadraticBase);
                Assert.That(evaluated, Is.EqualTo(level),
                    $"Level {level}: requiredXp={required} => evaluated level {evaluated}");
            }
        }

        [Test]
        public void Formula_Exponential_Consistency_RequiredXpRoundTrip()
        {
            for (int level = 1; level <= 10; level++)
            {
                double required = LevelCurveEvaluator.GetRequiredXpForLevelFormula(level, LevelFormulaType.Exponential,
                    expBase: ExpBase, expFactor: ExpFactor);
                int totalXp = (int)Math.Ceiling(required);
                int evaluated = LevelCurveEvaluator.EvaluateLevelByFormula(totalXp, LevelFormulaType.Exponential,
                    expBase: ExpBase, expFactor: ExpFactor);
                Assert.That(evaluated, Is.EqualTo(level),
                    $"Level {level}: requiredXp={required} => evaluated level {evaluated}");
            }
        }

        [Test]
        public void Formula_Power_Consistency_RequiredXpRoundTrip()
        {
            for (int level = 1; level <= 10; level++)
            {
                double required = LevelCurveEvaluator.GetRequiredXpForLevelFormula(level, LevelFormulaType.Power,
                    powerBase: PowerBase, powerExponent: PowerExponent);
                int totalXp = (int)Math.Ceiling(required);
                int evaluated = LevelCurveEvaluator.EvaluateLevelByFormula(totalXp, LevelFormulaType.Power,
                    powerBase: PowerBase, powerExponent: PowerExponent);
                Assert.That(evaluated, Is.EqualTo(level),
                    $"Level {level}: requiredXp={required} => evaluated level {evaluated}");
            }
        }

        [Test]
        public void LevelModel_SetMaxLevel_WhenNotUsingXp_ClampsLevelAndRaisesEvent()
        {
            // WHY: regression — a lowered cap must clamp a directly-set level and notify listeners,
            // since the XP-only recompute path is skipped when UseXp is false.
            var model = new LevelModel();
            model.SetUseXp(false);
            model.SetLevel(8);
            Assert.That(model.CurrentLevel, Is.EqualTo(8));

            int previous = -1;
            int next = -1;
            model.OnLevelChanged += (p, n) =>
            {
                previous = p;
                next = n;
            };

            model.SetMaxLevel(5);

            Assert.That(model.CurrentLevel, Is.EqualTo(5));
            Assert.That(previous, Is.EqualTo(8));
            Assert.That(next, Is.EqualTo(5));
        }

        [Test]
        public void LevelNoCodeAction_SetLevel_RaisesLevelUpWhenLevelChanges()
        {
            var host = new GameObject("LevelNoCodeAction_SetLevel_RaisesLevelUpWhenLevelChanges");
            try
            {
                LevelComponent component = host.AddComponent<LevelComponent>();
                SetPrivateField(component, "_loadOnAwake", false);
                component.EnsureInitialized();

                LevelNoCodeAction action = host.AddComponent<LevelNoCodeAction>();
                SetPrivateField(action, "_levelProvider", component);
                SetPrivateField(action, "_actionType", LevelNoCodeActionType.SetLevel);
                SetPrivateField(action, "_level", 4);

                int lastLevelUp = -1;
                UnityEventInt onLevelUp = GetPrivateField<UnityEventInt>(action, "_onLevelUp");
                onLevelUp.AddListener(level => lastLevelUp = level);

                action.Execute();

                Assert.That(component.Level, Is.EqualTo(4));
                // WHY: the SetLevel action must mirror the provider level change, not only AddXp.
                Assert.That(lastLevelUp, Is.EqualTo(4));
            }
            finally
            {
                Object.DestroyImmediate(host);
            }
        }

        private static void SetPrivateField(object target, string fieldName, object value)
        {
            FieldInfo fieldInfo = target.GetType().GetField(fieldName, BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.That(fieldInfo, Is.Not.Null, $"Field '{fieldName}' not found on {target.GetType().Name}");
            fieldInfo.SetValue(target, value);
        }

        private static T GetPrivateField<T>(object target, string fieldName)
        {
            FieldInfo fieldInfo = target.GetType().GetField(fieldName, BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.That(fieldInfo, Is.Not.Null, $"Field '{fieldName}' not found on {target.GetType().Name}");
            return (T)fieldInfo.GetValue(target);
        }
    }
}
