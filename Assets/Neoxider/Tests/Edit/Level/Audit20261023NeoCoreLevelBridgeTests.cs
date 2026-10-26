using System.Collections.Generic;
using Neo.Core.Level;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Neo.Core.Tests
{
    public sealed class Audit20261023NeoCoreLevelBridgeTests
    {
        private readonly List<GameObject> _hosts = new();
        private LevelCurveDefinition _curve;

        [SetUp]
        public void SetUp()
        {
            _curve = ScriptableObject.CreateInstance<LevelCurveDefinition>();
            _curve.SetLinear(100);
        }

        [TearDown]
        public void TearDown()
        {
            foreach (GameObject host in _hosts)
            {
                if (host != null)
                {
                    Object.DestroyImmediate(host);
                }
            }

            _hosts.Clear();
            Object.DestroyImmediate(_curve);
        }

        [TestCase(false)]
        [TestCase(true)]
        public void Condition_MissingProvider_LastResultMatchesReturnedValue(bool invert)
        {
            LevelConditionAdapter adapter = CreateHost().AddComponent<LevelConditionAdapter>();
            ConfigureCondition(adapter, LevelConditionEvaluationMode.LevelAtLeast, 1, invert);

            Assert.That(adapter.Evaluate(null), Is.EqualTo(invert));
            Assert.That(adapter.LastResult, Is.EqualTo(invert));
            Assert.That(adapter.EvaluateCurrent(), Is.EqualTo(invert));
            Assert.That(adapter.LastResult, Is.EqualTo(invert));
        }

        [TestCase(false)]
        [TestCase(true)]
        public void Condition_DestroyedProvider_ReplacesPreviousResult(bool invert)
        {
            LevelComponent provider = CreateProvider(CreateHost());
            provider.SetLevel(2);
            LevelConditionAdapter adapter = CreateHost().AddComponent<LevelConditionAdapter>();
            ConfigureCondition(adapter, LevelConditionEvaluationMode.LevelAtLeast, 2, invert, provider);
            Assert.That(adapter.Evaluate(null), Is.EqualTo(!invert));

            Object.DestroyImmediate(provider.gameObject);

            Assert.That(adapter.Evaluate(null), Is.EqualTo(invert));
            Assert.That(adapter.LastResult, Is.EqualTo(invert));
        }

        [TestCase(LevelConditionEvaluationMode.LevelAtLeast, 3, true)]
        [TestCase(LevelConditionEvaluationMode.LevelAtLeast, 4, false)]
        [TestCase(LevelConditionEvaluationMode.XpAtLeast, 225, true)]
        [TestCase(LevelConditionEvaluationMode.XpAtLeast, 226, false)]
        [TestCase(LevelConditionEvaluationMode.XpToNextLevelAtMost, 75, true)]
        [TestCase(LevelConditionEvaluationMode.XpToNextLevelAtMost, 74, false)]
        public void Condition_ComparisonModes_RespectInclusiveBoundaryAndInversion(
            LevelConditionEvaluationMode mode, int threshold, bool expected)
        {
            GameObject host = CreateHost();
            LevelComponent provider = CreateProvider(host);
            provider.AddXp(225);
            LevelConditionAdapter adapter = host.AddComponent<LevelConditionAdapter>();
            ConfigureCondition(adapter, mode, threshold, false);

            Assert.That(adapter.EvaluateCurrent(), Is.EqualTo(expected));
            Assert.That(adapter.LastResult, Is.EqualTo(expected));

            ConfigureCondition(adapter, mode, threshold, true);

            Assert.That(adapter.EvaluateCurrent(), Is.EqualTo(!expected));
            Assert.That(adapter.LastResult, Is.EqualTo(!expected));
        }

        [Test]
        public void Condition_ExplicitProviderTakesPriority_ThenFallsBackToContext()
        {
            LevelComponent context = CreateProvider(CreateHost());
            context.SetLevel(2);
            LevelComponent explicitProvider = CreateProvider(CreateHost());
            explicitProvider.SetLevel(5);
            LevelConditionAdapter adapter = CreateHost().AddComponent<LevelConditionAdapter>();
            ConfigureCondition(adapter, LevelConditionEvaluationMode.LevelAtLeast, 4, false, explicitProvider);

            Assert.That(adapter.Evaluate(context.gameObject), Is.True);

            Object.DestroyImmediate(explicitProvider.gameObject);

            Assert.That(adapter.Evaluate(context.gameObject), Is.False);
            Assert.That(adapter.LastResult, Is.False);
        }

        [TestCase(LevelNoCodeActionType.AddXp, 1, 3)]
        [TestCase(LevelNoCodeActionType.AddXp, 7, 3)]
        [TestCase(LevelNoCodeActionType.SetLevel, 1, 4)]
        [TestCase(LevelNoCodeActionType.SetLevel, 7, 4)]
        public void Action_SuccessListenerMutatesProvider_LevelEventRetainsActionResult(
            LevelNoCodeActionType actionType, int listenerLevel, int expectedLevel)
        {
            LevelComponent provider = CreateProvider(CreateHost());
            LevelNoCodeAction action = CreateHost().AddComponent<LevelNoCodeAction>();
            action.LevelProvider = provider;
            action.ActionType = actionType;
            action.XpAmount = 250;
            action.TargetLevel = 4;
            List<string> events = new();
            action.OnSuccess.AddListener(() =>
            {
                events.Add("success");
                provider.SetLevel(listenerLevel);
            });
            action.OnLevelUp.AddListener(level => events.Add($"level:{level}"));

            action.Execute();

            Assert.That(provider.Level, Is.EqualTo(listenerLevel));
            Assert.That(events, Is.EqualTo(new[] { "success", $"level:{expectedLevel}" }));
        }

        [TestCase(LevelNoCodeActionType.AddXp)]
        [TestCase(LevelNoCodeActionType.SetLevel)]
        public void Action_SuccessListenerChangesLevel_DoesNotInventActionLevelEvent(LevelNoCodeActionType actionType)
        {
            GameObject host = CreateHost();
            LevelComponent provider = CreateProvider(host);
            LevelNoCodeAction action = host.AddComponent<LevelNoCodeAction>();
            action.ActionType = actionType;
            action.XpAmount = 1;
            action.TargetLevel = 1;
            int successes = 0;
            List<int> levels = new();
            action.OnSuccess.AddListener(() =>
            {
                successes++;
                provider.SetLevel(4);
            });
            action.OnLevelUp.AddListener(levels.Add);

            action.Execute();

            Assert.That(provider.Level, Is.EqualTo(4));
            Assert.That(successes, Is.EqualTo(1));
            Assert.That(levels, Is.Empty);
        }

        [Test]
        public void Action_ReentrantSuccessListener_PreservesEachInvocationResult()
        {
            GameObject host = CreateHost();
            LevelComponent provider = CreateProvider(host);
            LevelNoCodeAction action = host.AddComponent<LevelNoCodeAction>();
            action.ActionType = LevelNoCodeActionType.SetLevel;
            action.TargetLevel = 2;
            bool nestedExecuted = false;
            List<string> events = new();
            action.OnSuccess.AddListener(() =>
            {
                events.Add($"success:{provider.Level}");
                if (!nestedExecuted)
                {
                    nestedExecuted = true;
                    action.TargetLevel = 3;
                    action.Execute();
                }
            });
            action.OnLevelUp.AddListener(level => events.Add($"level:{level}"));

            action.Execute();

            Assert.That(provider.Level, Is.EqualTo(3));
            Assert.That(events, Is.EqualTo(new[] { "success:2", "success:3", "level:3", "level:2" }));
        }

        [Test]
        public void Action_MissingThenLateSiblingProvider_AppliesXpAndReportsActualLevel()
        {
            GameObject host = CreateHost();
            LevelNoCodeAction action = host.AddComponent<LevelNoCodeAction>();
            action.XpAmount = 250;
            int successes = 0;
            List<int> levels = new();
            action.OnSuccess.AddListener(() => successes++);
            action.OnLevelUp.AddListener(levels.Add);

            action.Execute();

            Assert.That(successes, Is.Zero);
            Assert.That(levels, Is.Empty);

            LevelComponent provider = CreateProvider(host);
            action.Execute();

            Assert.That(provider.TotalXp, Is.EqualTo(250));
            Assert.That(provider.Level, Is.EqualTo(3));
            Assert.That(successes, Is.EqualTo(1));
            Assert.That(levels, Is.EqualTo(new[] { 3 }));
        }

        [TestCase(5, 2, 1)]
        [TestCase(2, 2, 0)]
        public void Action_SetLevel_PreservesDownwardAndUnchangedEventBehavior(
            int initialLevel, int targetLevel, int expectedLevelEvents)
        {
            GameObject host = CreateHost();
            LevelComponent provider = CreateProvider(host);
            provider.SetLevel(initialLevel);
            LevelNoCodeAction action = host.AddComponent<LevelNoCodeAction>();
            action.ActionType = LevelNoCodeActionType.SetLevel;
            action.SetTargetLevel(targetLevel);
            int successes = 0;
            List<int> levels = new();
            action.OnSuccess.AddListener(() => successes++);
            action.OnLevelUp.AddListener(levels.Add);

            action.Execute();

            Assert.That(provider.Level, Is.EqualTo(targetLevel));
            Assert.That(successes, Is.EqualTo(1));
            Assert.That(levels.Count, Is.EqualTo(expectedLevelEvents));
            if (expectedLevelEvents > 0)
            {
                Assert.That(levels[0], Is.EqualTo(targetLevel));
            }
        }

        [Test]
        public void Action_UnknownAction_DoesNotMutateProviderOrRaiseEvents()
        {
            GameObject host = CreateHost();
            LevelComponent provider = CreateProvider(host);
            provider.SetLevel(2);
            LevelNoCodeAction action = host.AddComponent<LevelNoCodeAction>();
            action.ActionType = (LevelNoCodeActionType)999;
            int successes = 0;
            List<int> levels = new();
            action.OnSuccess.AddListener(() => successes++);
            action.OnLevelUp.AddListener(levels.Add);

            action.Execute();

            Assert.That(provider.Level, Is.EqualTo(2));
            Assert.That(provider.TotalXp, Is.EqualTo(100));
            Assert.That(successes, Is.Zero);
            Assert.That(levels, Is.Empty);
        }

        private GameObject CreateHost()
        {
            GameObject host = new("Audit20261023NeoCoreLevelBridge");
            host.SetActive(false);
            _hosts.Add(host);
            return host;
        }

        private LevelComponent CreateProvider(GameObject host)
        {
            LevelComponent provider = host.AddComponent<LevelComponent>();
            using (SerializedObject serialized = new(provider))
            {
                serialized.FindProperty("_loadOnAwake").boolValue = false;
                serialized.FindProperty("_autoSave").boolValue = false;
                serialized.FindProperty("_saveKey").stringValue = string.Empty;
                serialized.ApplyModifiedPropertiesWithoutUndo();
            }

            provider.LevelCurveDefinition = _curve;
            provider.EnsureInitialized();
            return provider;
        }

        private static void ConfigureCondition(LevelConditionAdapter adapter, LevelConditionEvaluationMode mode,
            int threshold, bool invert, LevelComponent provider = null)
        {
            using (SerializedObject serialized = new(adapter))
            {
                serialized.FindProperty("_levelProvider").objectReferenceValue = provider;
                serialized.FindProperty("_mode").enumValueIndex = (int)mode;
                serialized.FindProperty("_threshold").intValue = threshold;
                serialized.FindProperty("_invert").boolValue = invert;
                serialized.ApplyModifiedPropertiesWithoutUndo();
            }
        }
    }
}
