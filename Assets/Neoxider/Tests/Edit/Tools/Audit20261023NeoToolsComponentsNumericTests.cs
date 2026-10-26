using System;
using System.Collections.Generic;
using System.Reflection;
using Neo.Save;
using Neo.Tools;
using NUnit.Framework;
using TMPro;
using UnityEngine;
using Object = UnityEngine.Object;
using Random = UnityEngine.Random;

namespace Neo.Editor.Tests
{
    internal static class Audit20261023NeoToolsComponentsAccess
    {
        internal static void Configure(object target, string fieldName, object value)
        {
            FieldInfo field = target.GetType().GetField(fieldName, BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.That(field, Is.Not.Null, fieldName);
            field.SetValue(target, value);
        }

        internal static void Invoke(object target, string methodName, params object[] arguments)
        {
            MethodInfo method = target.GetType().GetMethod(methodName, BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.That(method, Is.Not.Null, methodName);
            method.Invoke(target, arguments);
        }
    }

    public class Audit20261023NeoToolsComponentsNumericTests
    {
        private GameObject _root;
        private string _saveKey;
        private Random.State _randomState;

        [SetUp]
        public void SetUp()
        {
            _root = new GameObject(nameof(Audit20261023NeoToolsComponentsNumericTests));
            _root.SetActive(false);
            _saveKey = "Audit20261023NeoToolsComponents." + Guid.NewGuid().ToString("N");
            _randomState = Random.state;
        }

        [TearDown]
        public void TearDown()
        {
            Counter counter = _root.GetComponent<Counter>();
            if (counter != null)
            {
                Audit20261023NeoToolsComponentsAccess.Invoke(counter, "OnDisable");
            }

            Object.DestroyImmediate(_root);
            Counter.Registry.Remove(_saveKey);
            Counter.Registry.Remove(_saveKey + ".changed");
            SaveProvider.DeleteKey(_saveKey);
            Random.state = _randomState;
        }

        [Test]
        public void Counter_AddOneAtOneMillion_UpdatesValueAndNotifiesOnce()
        {
            Counter counter = _root.AddComponent<Counter>();
            counter.Value.SetValueWithoutNotify(1000000f);
            int notifications = 0;
            counter.Value.AddListener(value => notifications++);

            counter.Add(1);

            Assert.That(counter.ValueFloat, Is.EqualTo(1000001f));
            Assert.That(notifications, Is.EqualTo(1));
            counter.Set(1000001);
            Assert.That(notifications, Is.EqualTo(1), "Setting an identical value stays silent.");
        }

        [Test]
        public void Counter_SubtractMinimumIntegerInFloatMode_AddsItsPositiveMagnitude()
        {
            Counter counter = _root.AddComponent<Counter>();
            Audit20261023NeoToolsComponentsAccess.Configure(counter, "_valueMode", CounterValueMode.Float);

            counter.Subtract(int.MinValue);

            Assert.That(counter.ValueFloat, Is.EqualTo(2147483648f));
        }

        [TestCase(CounterValueMode.Int)]
        [TestCase(CounterValueMode.Float)]
        public void Counter_ReactiveReentry_OnlyPublishesLatestTypedValue(CounterValueMode mode)
        {
            Counter counter = CreateSavedCounter(mode);
            List<float> reactive = new List<float>();
            List<float> typed = new List<float>();
            int repeats = 0;
            counter.Value.AddListener(value =>
            {
                reactive.Add(value);
                if (value == 1f)
                {
                    counter.Set(2);
                }
            });
            counter.OnValueChangedInt.AddListener(value => typed.Add(value));
            counter.OnValueChangedFloat.AddListener(value => typed.Add(value));
            counter.OnRepeatByCounterValue.AddListener(() => repeats++);

            counter.Set(1);

            Assert.That(counter.ValueFloat, Is.EqualTo(2f));
            Assert.That(reactive, Is.EqualTo(new[] { 1f, 2f }));
            Assert.That(typed, Is.EqualTo(new[] { 2f }));
            Assert.That(repeats, Is.EqualTo(2));
            Assert.That(SaveProvider.GetFloat(_saveKey), Is.EqualTo(2f));
        }

        [TestCase(CounterValueMode.Int)]
        [TestCase(CounterValueMode.Float)]
        public void Counter_TypedReentry_DoesNotRepeatOrSaveSupersededChange(CounterValueMode mode)
        {
            Counter counter = CreateSavedCounter(mode);
            List<float> typed = new List<float>();
            int repeats = 0;
            Action<float> change = value =>
            {
                typed.Add(value);
                if (value == 1f)
                {
                    counter.Set(2);
                }
            };
            counter.OnValueChangedInt.AddListener(value => change(value));
            counter.OnValueChangedFloat.AddListener(value => change(value));
            counter.OnRepeatByCounterValue.AddListener(() => repeats++);

            counter.Set(1);

            Assert.That(counter.ValueFloat, Is.EqualTo(2f));
            Assert.That(typed, Is.EqualTo(new[] { 1f, 2f }));
            Assert.That(repeats, Is.EqualTo(2), "The superseded outer change must not repeat the latest value again.");
            Assert.That(SaveProvider.GetFloat(_saveKey), Is.EqualTo(2f));
        }

        [Test]
        public void Counter_RepeatReentry_StopsTheSupersededRepeatLoop()
        {
            Counter counter = CreateSavedCounter(CounterValueMode.Int);
            List<float> repeats = new List<float>();
            counter.OnRepeatByCounterValue.AddListener(() =>
            {
                repeats.Add(counter.ValueFloat);
                if (counter.ValueInt == 3)
                {
                    counter.Set(1);
                }
            });

            counter.Set(3);

            Assert.That(counter.ValueInt, Is.EqualTo(1));
            Assert.That(repeats, Is.EqualTo(new[] { 3f, 1f }));
            Assert.That(SaveProvider.GetFloat(_saveKey), Is.EqualTo(1f));
        }

        [Test]
        public void Counter_OrdinaryChange_PreservesReactiveTypedRepeatOrder()
        {
            Counter counter = CreateSavedCounter(CounterValueMode.Int);
            List<string> events = new List<string>();
            counter.Value.AddListener(value => events.Add("reactive"));
            counter.OnValueChangedInt.AddListener(value => events.Add("typed"));
            counter.OnRepeatByCounterValue.AddListener(() => events.Add("repeat"));

            counter.Set(2);

            Assert.That(events, Is.EqualTo(new[] { "reactive", "typed", "repeat", "repeat" }));
            Assert.That(SaveProvider.GetFloat(_saveKey), Is.EqualTo(2f));
        }

        [Test]
        public void Counter_DisableAfterSaveKeyEdit_RemovesOriginalRegistration()
        {
            Counter counter = _root.AddComponent<Counter>();
            Audit20261023NeoToolsComponentsAccess.Configure(counter, "_saveKey", _saveKey);
            Audit20261023NeoToolsComponentsAccess.Invoke(counter, "OnEnable");
            Assert.That(Counter.Registry[_saveKey], Does.Contain(counter));

            Audit20261023NeoToolsComponentsAccess.Configure(counter, "_saveKey", _saveKey + ".changed");
            Audit20261023NeoToolsComponentsAccess.Invoke(counter, "OnDisable");

            Assert.That(Counter.Registry.ContainsKey(_saveKey), Is.False);
        }

        [Test]
        public void Counter_DisableOneOfTwoRegistrations_PreservesOtherCounter()
        {
            Counter first = _root.AddComponent<Counter>();
            GameObject child = new GameObject("SecondCounter");
            child.transform.SetParent(_root.transform);
            Counter second = child.AddComponent<Counter>();
            Audit20261023NeoToolsComponentsAccess.Configure(first, "_saveKey", _saveKey);
            Audit20261023NeoToolsComponentsAccess.Configure(second, "_saveKey", _saveKey);
            Audit20261023NeoToolsComponentsAccess.Invoke(first, "OnEnable");
            Audit20261023NeoToolsComponentsAccess.Invoke(second, "OnEnable");
            try
            {
                Audit20261023NeoToolsComponentsAccess.Invoke(first, "OnDisable");
                Assert.That(Counter.Registry[_saveKey], Is.EqualTo(new[] { second }));
            }
            finally
            {
                Audit20261023NeoToolsComponentsAccess.Invoke(second, "OnDisable");
            }
        }

        [Test]
        public void RandomRange_ReversedFloatBounds_GeneratesAcrossTheSameInterval()
        {
            RandomRange range = _root.AddComponent<RandomRange>();
            range.ValueMode = RandomRangeValueMode.Float;
            range.Min = 3f;
            range.Max = -2f;
            Random.InitState(1023);
            bool sampledBelowUpperBound = false;
            int generated = 0;
            range.OnGeneratedFloat.AddListener(value => generated++);

            for (int i = 0; i < 32; i++)
            {
                range.Generate();
                Assert.That(range.ValueFloat, Is.InRange(-2f, 3f));
                sampledBelowUpperBound |= range.ValueFloat < 3f;
            }

            Assert.That(sampledBelowUpperBound, Is.True, "Swapped bounds must not produce a constant.");
            Assert.That(generated, Is.EqualTo(32));
        }

        [Test]
        public void ScoreManager_ChangingTarget_RefreshesProgressAndStars()
        {
            ScoreManager score = CreateScoreManager();
            score.TargetScoreValue = 100;
            score.Set(50, false);
            Assert.That(score.CountStarsValue, Is.EqualTo(2));
            int reportedStars = -1;
            score.CountStarsReactive.AddListener(value => reportedStars = value);

            score.TargetScoreValue = 200;

            Assert.That(score.ProgressValue, Is.EqualTo(0.25f));
            Assert.That(score.CountStarsValue, Is.EqualTo(1));
            Assert.That(reportedStars, Is.EqualTo(1));
        }

        [Test]
        public void ScoreManager_Initialize_PublishesSerializedTarget()
        {
            ScoreManager score = CreateScoreManager();
            Audit20261023NeoToolsComponentsAccess.Configure(score, "_targetScore", 240);

            Audit20261023NeoToolsComponentsAccess.Invoke(score, "Init");

            Assert.That(score.TargetScoreValue, Is.EqualTo(240));
            Assert.That(score.TargetScore.CurrentValue, Is.EqualTo(240));
        }

        [Test]
        public void ScoreManager_ResetBest_RefreshesLabelAndDeletesOwnSaveKey()
        {
            ScoreManager score = CreateScoreManager();
            GameObject labelObject = new GameObject("BestScoreLabel", typeof(RectTransform));
            labelObject.transform.SetParent(_root.transform);
            TMP_Text label = labelObject.AddComponent<TextMeshProUGUI>();
            score.textBestScores = new[] { label };
            score.SetBestScore(42);
            Assert.That(label.text, Is.EqualTo("42"));

            score.ResetBestScore();

            Assert.That(label.text, Is.EqualTo("0"));
            Assert.That(score.BestScore.CurrentValue, Is.Zero);
            Assert.That(SaveProvider.HasKey(_saveKey), Is.False);
        }

        [Test]
        public void ScoreManager_NoStarThresholds_ReportsZeroAndStillUpdatesScore()
        {
            ScoreManager score = CreateScoreManager();
            score.starScores = null;

            score.Set(10, false);

            Assert.That(score.ScoreValue, Is.EqualTo(10));
            Assert.That(score.GetCountStars(), Is.Zero);
            Assert.That(score.CountStarsValue, Is.Zero);
        }

        private Counter CreateSavedCounter(CounterValueMode mode)
        {
            Counter counter = _root.AddComponent<Counter>();
            Audit20261023NeoToolsComponentsAccess.Configure(counter, "_valueMode", mode);
            Audit20261023NeoToolsComponentsAccess.Configure(counter, "_saveEnabled", true);
            Audit20261023NeoToolsComponentsAccess.Configure(counter, "_saveKey", _saveKey);
            Audit20261023NeoToolsComponentsAccess.Configure(counter, "_invokeRepeatEventOnValueChanged", true);
            return counter;
        }

        private ScoreManager CreateScoreManager()
        {
            ScoreManager score = _root.AddComponent<ScoreManager>();
            Audit20261023NeoToolsComponentsAccess.Configure(score, "_keySave", _saveKey);
            return score;
        }
    }
}
