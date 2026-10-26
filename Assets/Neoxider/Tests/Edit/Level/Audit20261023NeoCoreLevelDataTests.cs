using System.Reflection;
using Neo.Core.Level;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Neo.Core.Tests
{
    public sealed class Audit20261023NeoCoreLevelDataTests
    {
        private LevelCurveDefinition _definition;

        [SetUp]
        public void SetUp()
        {
            _definition = ScriptableObject.CreateInstance<LevelCurveDefinition>();
        }

        [TearDown]
        public void TearDown()
        {
            Object.DestroyImmediate(_definition);
        }

        [TestCase(12.25f)]
        [TestCase(12.5f)]
        [TestCase(12.75f)]
        public void Formula_FractionalRequirement_IsEnoughToReachTheRequestedLevel(float requiredXp)
        {
            using (SerializedObject serializedDefinition = new SerializedObject(_definition))
            {
                serializedDefinition.FindProperty("_formulaType").intValue = (int)LevelFormulaType.Quadratic;
                serializedDefinition.FindProperty("_quadraticBase").floatValue = requiredXp;
                serializedDefinition.ApplyModifiedPropertiesWithoutUndo();
            }

            int threshold = _definition.GetRequiredXpForLevel(2);

            Assert.That(threshold, Is.EqualTo(13));
            Assert.That(_definition.EvaluateLevel(threshold - 1), Is.EqualTo(1));
            Assert.That(_definition.EvaluateLevel(threshold), Is.EqualTo(2));
            Assert.That(_definition.GetXpToNextLevel(threshold - 1), Is.EqualTo(1));
        }

        [TestCase(12.25f)]
        [TestCase(12.5f)]
        [TestCase(12.75f)]
        public void Curve_FractionalRequirement_IsEnoughToReachTheRequestedLevel(float requiredXp)
        {
            ConfigureCurve(AnimationCurve.Linear(1f, 0f, 2f, requiredXp));

            int threshold = _definition.GetRequiredXpForLevel(2);

            Assert.That(threshold, Is.EqualTo(13));
            Assert.That(_definition.EvaluateLevel(threshold - 1), Is.EqualTo(1));
            Assert.That(_definition.EvaluateLevel(threshold), Is.EqualTo(2));
            Assert.That(_definition.GetXpToNextLevel(threshold - 1), Is.EqualTo(1));
            Assert.That(_definition.GetXpToNextLevel(threshold), Is.Zero);
        }

        [Test]
        public void Formula_RequirementBeyondIntRange_SaturatesWithoutWrapping()
        {
            _definition.SetLinear(int.MaxValue);

            Assert.That(_definition.GetRequiredXpForLevel(2), Is.EqualTo(int.MaxValue));
            Assert.That(_definition.GetRequiredXpForLevel(3), Is.EqualTo(int.MaxValue));
            Assert.That(_definition.GetRequiredXpForLevel(0), Is.Zero);
        }

        [TestCase(0, int.MaxValue)]
        [TestCase(2000000000, 1000000000)]
        public void Curve_LargeRequirement_SubtractsBeforeSaturating(int totalXp, int expectedRemaining)
        {
            ConfigureCurve(AnimationCurve.Linear(1f, 0f, 2f, 3000000000f));

            Assert.That(_definition.GetRequiredXpForLevel(2), Is.EqualTo(int.MaxValue));
            Assert.That(_definition.EvaluateLevel(totalXp), Is.EqualTo(1));
            Assert.That(_definition.GetXpToNextLevel(totalXp), Is.EqualTo(expectedRemaining));
        }

        [Test]
        public void Curve_LargeIntegerThreshold_DoesNotRoundTotalXpUp()
        {
            ConfigureCurve(AnimationCurve.Linear(1f, 0f, 2f, 16777220f));

            Assert.That(_definition.GetRequiredXpForLevel(2), Is.EqualTo(16777220));
            Assert.That(_definition.EvaluateLevel(16777219), Is.EqualTo(1));
            Assert.That(_definition.GetXpToNextLevel(16777219), Is.EqualTo(1));
            Assert.That(_definition.EvaluateLevel(16777220), Is.EqualTo(2));
        }

        [TestCase(2.4f)]
        [TestCase(2.6f)]
        public void Curve_FractionalEnd_StopsAtLastAuthoredIntegerLevel(float lastKeyTime)
        {
            ConfigureCurve(new AnimationCurve(
                new Keyframe(1f, 0f),
                new Keyframe(2f, 100f),
                new Keyframe(lastKeyTime, 160f)));

            Assert.That(_definition.EvaluateLevel(100), Is.EqualTo(2));
            Assert.That(_definition.EvaluateLevel(int.MaxValue), Is.EqualTo(2));
            Assert.That(_definition.GetXpToNextLevel(100), Is.Zero);

            LevelModel model = new LevelModel();
            model.SetCurveDefinition(_definition);
            model.SetState(100, 1);

            Assert.That(model.CurrentLevel, Is.EqualTo(2));
            Assert.That(model.XpToNextLevel, Is.Zero);
        }

        [TestCase(-1f)]
        [TestCase(0f)]
        [TestCase(0.4f)]
        [TestCase(1f)]
        public void Curve_EndBeforeLevelTwo_DoesNotCreateUnauthoredLevels(float lastKeyTime)
        {
            ConfigureCurve(new AnimationCurve(new Keyframe(lastKeyTime, 0f)));

            Assert.That(_definition.EvaluateLevel(0), Is.EqualTo(1));
            Assert.That(_definition.EvaluateLevel(int.MaxValue), Is.EqualTo(1));
            Assert.That(_definition.GetXpToNextLevel(0), Is.Zero);
        }

        [Test]
        public void Curve_ScanLimit_HasNoNextLevel()
        {
            // Unit tangents keep XP = level - 1 linear instead of flattening near the last sample.
            ConfigureCurve(new AnimationCurve(
                new Keyframe(1f, 0f, 1f, 1f),
                new Keyframe(10000f, 9999f, 1f, 1f),
                new Keyframe(10001f, 10000f, 1f, 1f)));

            Assert.That(_definition.GetRequiredXpForLevel(10000), Is.EqualTo(9999));
            Assert.That(_definition.GetRequiredXpForLevel(10001), Is.EqualTo(10000));
            Assert.That(_definition.EvaluateLevel(9999), Is.EqualTo(10000));
            Assert.That(_definition.GetXpToNextLevel(9999), Is.Zero);
        }

        [Test]
        public void Curve_ExplicitMaxLevel_StopsBeforeAuthoredEnd()
        {
            ConfigureCurve(new AnimationCurve(
                new Keyframe(1f, 0f),
                new Keyframe(2f, 100f),
                new Keyframe(3f, 200f)));

            Assert.That(_definition.EvaluateLevel(99, maxLevel: 2), Is.EqualTo(1));
            Assert.That(_definition.GetXpToNextLevel(99, maxLevel: 2), Is.EqualTo(1));
            Assert.That(_definition.EvaluateLevel(200, maxLevel: 2), Is.EqualTo(2));
            Assert.That(_definition.GetXpToNextLevel(100, maxLevel: 2), Is.Zero);
        }

        [Test]
        public void Curve_EmptyData_ReportsBaseLevelAndNoProgression()
        {
            ConfigureCurve(new AnimationCurve());

            Assert.That(_definition.EvaluateLevel(100), Is.EqualTo(1));
            Assert.That(_definition.GetXpToNextLevel(100), Is.Zero);
            Assert.That(_definition.GetRequiredXpForLevel(2), Is.Zero);
        }

        [Test]
        public void Custom_NullTable_LookupAgreesWithEvaluationFallback()
        {
            using (SerializedObject serializedDefinition = new SerializedObject(_definition))
            {
                serializedDefinition.FindProperty("_mode").intValue = (int)LevelCurveMode.Custom;
                serializedDefinition.ApplyModifiedPropertiesWithoutUndo();
            }

            // Simulate absent serialized data; the authoring API cannot assign a null list.
            FieldInfo customEntriesField = typeof(LevelCurveDefinition).GetField(
                "_customEntries", BindingFlags.Instance | BindingFlags.NonPublic);
            customEntriesField.SetValue(_definition, null);

            Assert.That(_definition.TryGetDefinition(2, out LevelCurveEntry entry), Is.False);
            Assert.That(entry, Is.Null);
            Assert.That(_definition.GetRequiredXpForLevel(2), Is.Zero);
            Assert.That(_definition.EvaluateLevel(100), Is.EqualTo(1));
            Assert.That(_definition.GetXpToNextLevel(100), Is.Zero);
        }

        private void ConfigureCurve(AnimationCurve curve)
        {
            using (SerializedObject serializedDefinition = new SerializedObject(_definition))
            {
                serializedDefinition.FindProperty("_mode").intValue = (int)LevelCurveMode.Curve;
                serializedDefinition.FindProperty("_animationCurve").animationCurveValue = curve;
                serializedDefinition.ApplyModifiedPropertiesWithoutUndo();
            }
        }
    }
}
