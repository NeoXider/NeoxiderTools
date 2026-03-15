using System.Collections.Generic;
using System.Reflection;
using NUnit.Framework;
using Neo.Core.Level;
using UnityEngine;

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

        // --- Linear ---
        [Test]
        public void Formula_Linear_LevelFromXp()
        {
            Assert.That(LevelCurveEvaluator.EvaluateLevelByFormula(0, LevelFormulaType.Linear, XpPerLevel), Is.EqualTo(1));
            Assert.That(LevelCurveEvaluator.EvaluateLevelByFormula(99, LevelFormulaType.Linear, XpPerLevel), Is.EqualTo(1));
            Assert.That(LevelCurveEvaluator.EvaluateLevelByFormula(100, LevelFormulaType.Linear, XpPerLevel), Is.EqualTo(2));
            Assert.That(LevelCurveEvaluator.EvaluateLevelByFormula(199, LevelFormulaType.Linear, XpPerLevel), Is.EqualTo(2));
            Assert.That(LevelCurveEvaluator.EvaluateLevelByFormula(200, LevelFormulaType.Linear, XpPerLevel), Is.EqualTo(3));
            Assert.That(LevelCurveEvaluator.EvaluateLevelByFormula(500, LevelFormulaType.Linear, XpPerLevel), Is.EqualTo(6));
        }

        [Test]
        public void Formula_Linear_RequiredXpForLevel()
        {
            Assert.That(LevelCurveEvaluator.GetRequiredXpForLevelFormula(1, LevelFormulaType.Linear, XpPerLevel), Is.EqualTo(0));
            Assert.That(LevelCurveEvaluator.GetRequiredXpForLevelFormula(2, LevelFormulaType.Linear, XpPerLevel), Is.EqualTo(100));
            Assert.That(LevelCurveEvaluator.GetRequiredXpForLevelFormula(3, LevelFormulaType.Linear, XpPerLevel), Is.EqualTo(200));
            Assert.That(LevelCurveEvaluator.GetRequiredXpForLevelFormula(6, LevelFormulaType.Linear, XpPerLevel), Is.EqualTo(500));
        }

        [Test]
        public void Formula_Linear_MaxLevelCap()
        {
            int level = LevelCurveEvaluator.EvaluateLevelByFormula(10000, LevelFormulaType.Linear, XpPerLevel, maxLevel: 5);
            Assert.That(level, Is.EqualTo(5));
        }

        // --- LinearWithOffset ---
        [Test]
        public void Formula_LinearWithOffset_LevelFromXp()
        {
            const float offset = 50f;
            Assert.That(LevelCurveEvaluator.EvaluateLevelByFormula(0, LevelFormulaType.LinearWithOffset, XpPerLevel, offset), Is.EqualTo(1));
            Assert.That(LevelCurveEvaluator.EvaluateLevelByFormula(49, LevelFormulaType.LinearWithOffset, XpPerLevel, offset), Is.EqualTo(1));
            Assert.That(LevelCurveEvaluator.EvaluateLevelByFormula(50, LevelFormulaType.LinearWithOffset, XpPerLevel, offset), Is.EqualTo(1));
            Assert.That(LevelCurveEvaluator.EvaluateLevelByFormula(150, LevelFormulaType.LinearWithOffset, XpPerLevel, offset), Is.EqualTo(2));
            Assert.That(LevelCurveEvaluator.EvaluateLevelByFormula(250, LevelFormulaType.LinearWithOffset, XpPerLevel, offset), Is.EqualTo(3));
        }

        [Test]
        public void Formula_LinearWithOffset_RequiredXpForLevel()
        {
            const float offset = 50f;
            Assert.That(LevelCurveEvaluator.GetRequiredXpForLevelFormula(1, LevelFormulaType.LinearWithOffset, XpPerLevel, offset), Is.EqualTo(50));
            Assert.That(LevelCurveEvaluator.GetRequiredXpForLevelFormula(2, LevelFormulaType.LinearWithOffset, XpPerLevel, offset), Is.EqualTo(150));
            Assert.That(LevelCurveEvaluator.GetRequiredXpForLevelFormula(3, LevelFormulaType.LinearWithOffset, XpPerLevel, offset), Is.EqualTo(250));
        }

        // --- Quadratic ---
        [Test]
        public void Formula_Quadratic_LevelFromXp()
        {
            // RequiredXp(level) = base * level^2 => level = sqrt(totalXp/base), 1-based
            Assert.That(LevelCurveEvaluator.EvaluateLevelByFormula(0, LevelFormulaType.Quadratic, quadraticBase: QuadraticBase), Is.EqualTo(1));
            Assert.That(LevelCurveEvaluator.EvaluateLevelByFormula(49, LevelFormulaType.Quadratic, quadraticBase: QuadraticBase), Is.EqualTo(1));
            Assert.That(LevelCurveEvaluator.EvaluateLevelByFormula(50, LevelFormulaType.Quadratic, quadraticBase: QuadraticBase), Is.EqualTo(2));  // 50*1^2=50, 50*2^2=200
            Assert.That(LevelCurveEvaluator.EvaluateLevelByFormula(199, LevelFormulaType.Quadratic, quadraticBase: QuadraticBase), Is.EqualTo(2));
            Assert.That(LevelCurveEvaluator.EvaluateLevelByFormula(200, LevelFormulaType.Quadratic, quadraticBase: QuadraticBase), Is.EqualTo(3)); // 50*3^2=450
        }

        [Test]
        public void Formula_Quadratic_RequiredXpForLevel()
        {
            Assert.That(LevelCurveEvaluator.GetRequiredXpForLevelFormula(1, LevelFormulaType.Quadratic, quadraticBase: QuadraticBase), Is.EqualTo(0));
            Assert.That(LevelCurveEvaluator.GetRequiredXpForLevelFormula(2, LevelFormulaType.Quadratic, quadraticBase: QuadraticBase), Is.EqualTo(50));
            Assert.That(LevelCurveEvaluator.GetRequiredXpForLevelFormula(3, LevelFormulaType.Quadratic, quadraticBase: QuadraticBase), Is.EqualTo(200));
        }

        // --- Exponential ---
        [Test]
        public void Formula_Exponential_LevelFromXp()
        {
            Assert.That(LevelCurveEvaluator.EvaluateLevelByFormula(0, LevelFormulaType.Exponential, expBase: ExpBase, expFactor: ExpFactor), Is.EqualTo(1));
            Assert.That(LevelCurveEvaluator.EvaluateLevelByFormula(99, LevelFormulaType.Exponential, expBase: ExpBase, expFactor: ExpFactor), Is.EqualTo(1));
            // RequiredXp(1)=100, RequiredXp(2)=150 => level 2 at 150, level 3 at 225
            Assert.That(LevelCurveEvaluator.EvaluateLevelByFormula(150, LevelFormulaType.Exponential, expBase: ExpBase, expFactor: ExpFactor), Is.EqualTo(2));
            Assert.That(LevelCurveEvaluator.EvaluateLevelByFormula(224, LevelFormulaType.Exponential, expBase: ExpBase, expFactor: ExpFactor), Is.EqualTo(2));
            Assert.That(LevelCurveEvaluator.EvaluateLevelByFormula(225, LevelFormulaType.Exponential, expBase: ExpBase, expFactor: ExpFactor), Is.EqualTo(3));
        }

        [Test]
        public void Formula_Exponential_RequiredXpForLevel()
        {
            double l1 = LevelCurveEvaluator.GetRequiredXpForLevelFormula(1, LevelFormulaType.Exponential, expBase: ExpBase, expFactor: ExpFactor);
            double l2 = LevelCurveEvaluator.GetRequiredXpForLevelFormula(2, LevelFormulaType.Exponential, expBase: ExpBase, expFactor: ExpFactor);
            Assert.That(l1, Is.EqualTo(100)); // 100 * 1.5^0
            Assert.That(l2, Is.EqualTo(150)); // 100 * 1.5^1
        }

        // --- Power ---
        [Test]
        public void Formula_Power_LevelFromXp()
        {
            // RequiredXp(level) = powerBase * level^powerExponent => 10*level^2
            Assert.That(LevelCurveEvaluator.EvaluateLevelByFormula(0, LevelFormulaType.Power, powerBase: PowerBase, powerExponent: PowerExponent), Is.EqualTo(1));
            Assert.That(LevelCurveEvaluator.EvaluateLevelByFormula(9, LevelFormulaType.Power, powerBase: PowerBase, powerExponent: PowerExponent), Is.EqualTo(1));
            Assert.That(LevelCurveEvaluator.EvaluateLevelByFormula(10, LevelFormulaType.Power, powerBase: PowerBase, powerExponent: PowerExponent), Is.EqualTo(2));  // 10*1^2=10, 10*2^2=40
            Assert.That(LevelCurveEvaluator.EvaluateLevelByFormula(40, LevelFormulaType.Power, powerBase: PowerBase, powerExponent: PowerExponent), Is.EqualTo(3));  // 10*3^2=90
        }

        [Test]
        public void Formula_Power_RequiredXpForLevel()
        {
            Assert.That(LevelCurveEvaluator.GetRequiredXpForLevelFormula(1, LevelFormulaType.Power, powerBase: PowerBase, powerExponent: PowerExponent), Is.EqualTo(0));
            Assert.That(LevelCurveEvaluator.GetRequiredXpForLevelFormula(2, LevelFormulaType.Power, powerBase: PowerBase, powerExponent: PowerExponent), Is.EqualTo(10));
            Assert.That(LevelCurveEvaluator.GetRequiredXpForLevelFormula(3, LevelFormulaType.Power, powerBase: PowerBase, powerExponent: PowerExponent), Is.EqualTo(40));
        }

        // --- GetXpToNextLevel (formula) ---
        [Test]
        public void Formula_Linear_XpToNextLevel()
        {
            int next = LevelCurveEvaluator.GetXpToNextLevelByFormula(0, LevelFormulaType.Linear, XpPerLevel);
            Assert.That(next, Is.EqualTo(100));
            next = LevelCurveEvaluator.GetXpToNextLevelByFormula(50, LevelFormulaType.Linear, XpPerLevel);
            Assert.That(next, Is.EqualTo(50));
            next = LevelCurveEvaluator.GetXpToNextLevelByFormula(100, LevelFormulaType.Linear, XpPerLevel);
            Assert.That(next, Is.EqualTo(100));
            next = LevelCurveEvaluator.GetXpToNextLevelByFormula(250, LevelFormulaType.Linear, XpPerLevel);
            Assert.That(next, Is.EqualTo(50));
        }

        [Test]
        public void Formula_Linear_XpToNextLevel_RespectsMaxLevel()
        {
            int next = LevelCurveEvaluator.GetXpToNextLevelByFormula(500, LevelFormulaType.Linear, XpPerLevel, maxLevel: 5);
            Assert.That(next, Is.EqualTo(0), "At max level XP to next should be 0");
        }

        // --- LevelCurveDefinition SO: Formula (SetLinear) ---
        [Test]
        public void Definition_FormulaLinear_SetLinear_Works()
        {
            var def = ScriptableObject.CreateInstance<LevelCurveDefinition>();
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

        // --- LevelCurveDefinition SO: Custom ---
        [Test]
        public void Definition_Custom_LevelFromXp()
        {
            var def = ScriptableObject.CreateInstance<LevelCurveDefinition>();
            var entries = new List<LevelCurveEntry>
            {
                new LevelCurveEntry(1, 0),
                new LevelCurveEntry(2, 100),
                new LevelCurveEntry(3, 250),
                new LevelCurveEntry(4, 500)
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

        // --- LevelCurveDefinition SO: Curve (AnimationCurve) ---
        [Test]
        public void Definition_Curve_LevelFromXp()
        {
            var curve = new AnimationCurve(
                new Keyframe(1f, 0f),
                new Keyframe(2f, 100f),
                new Keyframe(3f, 250f),
                new Keyframe(4f, 500f));
            var def = ScriptableObject.CreateInstance<LevelCurveDefinition>();
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

        // --- Consistency: EvaluateLevel(GetRequiredXpForLevel(L)) >= L ---
        [Test]
        public void Formula_Linear_Consistency_RequiredXpRoundTrip()
        {
            for (int level = 1; level <= 20; level++)
            {
                double required = LevelCurveEvaluator.GetRequiredXpForLevelFormula(level, LevelFormulaType.Linear, XpPerLevel);
                int totalXp = (int)System.Math.Ceiling(required);
                int evaluated = LevelCurveEvaluator.EvaluateLevelByFormula(totalXp, LevelFormulaType.Linear, XpPerLevel);
                Assert.That(evaluated, Is.EqualTo(level), $"Level {level}: requiredXp={required} => evaluated level {evaluated}");
            }
        }

        [Test]
        public void Formula_Quadratic_Consistency_RequiredXpRoundTrip()
        {
            for (int level = 1; level <= 15; level++)
            {
                double required = LevelCurveEvaluator.GetRequiredXpForLevelFormula(level, LevelFormulaType.Quadratic, quadraticBase: QuadraticBase);
                int totalXp = (int)System.Math.Ceiling(required);
                int evaluated = LevelCurveEvaluator.EvaluateLevelByFormula(totalXp, LevelFormulaType.Quadratic, quadraticBase: QuadraticBase);
                Assert.That(evaluated, Is.EqualTo(level), $"Level {level}: requiredXp={required} => evaluated level {evaluated}");
            }
        }

        [Test]
        public void Formula_Exponential_Consistency_RequiredXpRoundTrip()
        {
            for (int level = 1; level <= 10; level++)
            {
                double required = LevelCurveEvaluator.GetRequiredXpForLevelFormula(level, LevelFormulaType.Exponential, expBase: ExpBase, expFactor: ExpFactor);
                int totalXp = (int)System.Math.Ceiling(required);
                int evaluated = LevelCurveEvaluator.EvaluateLevelByFormula(totalXp, LevelFormulaType.Exponential, expBase: ExpBase, expFactor: ExpFactor);
                Assert.That(evaluated, Is.EqualTo(level), $"Level {level}: requiredXp={required} => evaluated level {evaluated}");
            }
        }

        [Test]
        public void Formula_Power_Consistency_RequiredXpRoundTrip()
        {
            for (int level = 1; level <= 10; level++)
            {
                double required = LevelCurveEvaluator.GetRequiredXpForLevelFormula(level, LevelFormulaType.Power, powerBase: PowerBase, powerExponent: PowerExponent);
                int totalXp = (int)System.Math.Ceiling(required);
                int evaluated = LevelCurveEvaluator.EvaluateLevelByFormula(totalXp, LevelFormulaType.Power, powerBase: PowerBase, powerExponent: PowerExponent);
                Assert.That(evaluated, Is.EqualTo(level), $"Level {level}: requiredXp={required} => evaluated level {evaluated}");
            }
        }

        private static void SetPrivateField(object target, string fieldName, object value)
        {
            FieldInfo fieldInfo = target.GetType().GetField(fieldName, BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.That(fieldInfo, Is.Not.Null, $"Field '{fieldName}' not found on {target.GetType().Name}");
            fieldInfo.SetValue(target, value);
        }
    }
}
