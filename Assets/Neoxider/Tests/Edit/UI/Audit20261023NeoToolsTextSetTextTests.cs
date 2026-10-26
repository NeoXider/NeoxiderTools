using System;
using System.Collections;
using System.Collections.Generic;
using System.Numerics;
using DG.Tweening;
using Neo.Extensions;
using Neo.Tools;
using NUnit.Framework;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.TestTools;

namespace Neo.Tests.Edit
{
    [Serializable]
    public class Audit20261023NeoToolsTextSetTextTests
    {
        // The runner serializes the fixture across Enter/ExitPlayMode domain reloads.
        [SerializeField] private bool _ownsPlaySession;
        private Behaviour _nativeDriver;
        private readonly List<Tween> _ownedTweens = new List<Tween>();
        private GameObject _go;
        private GameObject _parent;
        private SetText _view;
        private TextMeshProUGUI _label;

        [UnitySetUp]
        public IEnumerator SetUp()
        {
            _ownsPlaySession = false;
            _nativeDriver = null;
            _go = null;
            _parent = null;
            _view = null;
            _label = null;
            _ownedTweens.Clear();
            Assert.IsFalse(Application.isPlaying || EditorApplication.isPlayingOrWillChangePlaymode,
                "This fixture must acquire its own Play session; an existing or pending session cannot be borrowed.");

            yield return new EnterPlayMode();
            _ownsPlaySession = true;
            Assert.IsTrue(Application.isPlaying, "Numeric animation requires a genuine Play Mode session.");
            _nativeDriver = DOTween.Init() as Behaviour;
            Assert.IsNotNull(_nativeDriver, "Public DOTween.Init must admit a native driver before creating the view.");
            Assert.IsTrue(_nativeDriver.isActiveAndEnabled);
            Assert.IsTrue(_nativeDriver.gameObject.activeInHierarchy);
            CreateView();
        }

        [UnityTearDown]
        public IEnumerator TearDown()
        {
            if (!_ownsPlaySession)
            {
                yield break;
            }

            try
            {
                if (_view != null)
                {
                    DOTween.Kill(_view);
                }

                if (_parent != null)
                {
                    UnityEngine.Object.DestroyImmediate(_parent);
                }

                if (_go != null)
                {
                    UnityEngine.Object.DestroyImmediate(_go);
                }
            }
            catch (Exception exception)
            {
                // Keep the cleanup fault observable while still exiting the session this fixture acquired.
                Debug.LogException(exception);
            }

            if (Application.isPlaying)
            {
                yield return new ExitPlayMode();
            }

            _ownsPlaySession = false;
            _nativeDriver = null;
            _ownedTweens.Clear();
        }

        [TestCase("string", "Ready")]
        [TestCase("clear", "")]
        [TestCase("percentage", "25%")]
        [TestCase("currency", "$12")]
        [TestCase("bigInteger", "123.456.789.012.345.678.901.234.567.890")]
        [TestCase("formattedFloat", "7.5")]
        [TestCase("formattedBigInteger", "1234567890123")]
        public void ImmediateWrite_ReplacesAnimationAndRemainsOnScreen(string operation, string expected)
        {
            int notifications = 0;
            _view.OnTextUpdated.AddListener(value => notifications++);
            Tween retired = Animate(() => _view.Set(1000f));
            AdvanceOwnedTweens(0.05f);
            Assert.Greater(_view.currentNum, 0f);
            Assert.Less(_view.currentNum, 1000f);

            switch (operation)
            {
                case "string":
                    _view.Set("Ready");
                    break;
                case "clear":
                    _view.Clear();
                    break;
                case "percentage":
                    _view.SetPercentage(25f);
                    break;
                case "currency":
                    _view.SetCurrency(12f);
                    break;
                case "bigInteger":
                    _view.SetBigInteger(BigInteger.Parse("123456789012345678901234567890"));
                    break;
                case "formattedFloat":
                    _view.SetFormatted(7.5f, new NumberFormatOptions(NumberNotation.Plain, 1));
                    break;
                case "formattedBigInteger":
                    _view.SetFormatted(new BigInteger(1234567890123L),
                        new NumberFormatOptions(NumberNotation.Plain, 0));
                    break;
            }

            Assert.AreEqual(expected, _label.text);
            Assert.IsFalse(retired.IsActive(), "An immediate replacement must release its previous tween.");
            int notificationsAfterWrite = notifications;
            float numberAfterWrite = _view.currentNum;
            AdvanceOwnedTweens(1f);
            Assert.AreEqual(expected, _label.text);
            Assert.AreEqual(numberAfterWrite, _view.currentNum,
                "The superseded tween must not mutate retained animation state.");
            Assert.AreEqual(notificationsAfterWrite, notifications,
                "The superseded animation must not publish later text updates.");
        }

        [Test]
        public void DecimalPlaces_ReformatsTheValueWithoutParsingTheDecimalSeparator()
        {
            _view.DecimalPlaces = 2;
            Animate(() => _view.Set(1.25f));
            AdvanceOwnedTweens(1f);
            Assert.AreEqual("1.25", _label.text);

            _view.DecimalPlaces = 1;
            AdvanceOwnedTweens(1f);

            Assert.AreEqual("1.2", _label.text);
            Assert.AreEqual(1.25f, _view.currentNum);
        }

        [Test]
        public void Separator_ReformatsBigIntegerWithoutPrecisionLoss()
        {
            _view.SetBigInteger(BigInteger.Parse("9007199254740993"));
            Assert.AreEqual("9.007.199.254.740.993", _label.text);

            _view.Separator = ",";

            Assert.AreEqual("9,007,199,254,740,993", _label.text);
        }

        [Test]
        public void NumericProperties_PreserveCurrencyAndUseTheOriginalValue()
        {
            _view.DecimalPlaces = 2;
            _view.SetCurrency(1250.5f, "USD ");
            _view.Separator = ",";
            Assert.AreEqual("USD 1,250.5", _label.text);

            _view.NumberNotationStyle = NumberNotation.IdleShort;
            Assert.AreEqual("USD 1.25K", _label.text);
            _view.NumberNotationStyle = NumberNotation.Plain;
            _view.DecimalPlaces = 0;
            _view.RoundingMode = NumberRoundingMode.AwayFromZero;
            Assert.AreEqual("USD 1251", _label.text);
        }

        [Test]
        public void NumericProperties_DoNotReinterpretLiteralTextOrRestartClearedNumbers()
        {
            _view.Set("1.25");
            _view.DecimalPlaces = 1;
            AdvanceOwnedTweens(1f);
            Assert.AreEqual("1.25", _label.text);

            _view.SetPercentage(25f);
            _view.Clear();
            _view.DecimalPlaces = 2;
            Assert.AreEqual("", _label.text);
        }

        [TestCase(int.MaxValue, 1, 2147483648f)]
        [TestCase(int.MinValue, -1, -2147483648f)]
        public void IntegerOffset_DoesNotWrapBeforeAnimation(int value, int offset, float expected)
        {
            _view.IndexOffset = offset;
            Animate(() => _view.Set(value));
            AdvanceOwnedTweens(1f);

            Assert.AreEqual(expected, _view.currentNum);
            Assert.AreEqual(expected < 0f, _label.text.StartsWith("-", StringComparison.Ordinal));
        }

        [TestCase(16777217, 0, NumberNotation.Plain, "16777217")]
        [TestCase(16777216, 1, NumberNotation.Grouped, "16,777,217")]
        [TestCase(-16777217, 0, NumberNotation.Plain, "-16777217")]
        [TestCase(int.MaxValue, int.MaxValue, NumberNotation.Plain, "4294967294")]
        [TestCase(int.MinValue, int.MinValue, NumberNotation.Plain, "-4294967296")]
        [TestCase(int.MaxValue, 1, NumberNotation.Grouped, "2,147,483,648")]
        [TestCase(int.MinValue, -1, NumberNotation.Grouped, "-2,147,483,649")]
        public void IntegerCompletion_DisplaysExactValueAndOffset(
            int value, int offset, NumberNotation notation, string expected)
        {
            _view.IndexOffset = offset;
            _view.Separator = ",";
            _view.NumberNotationStyle = notation;
            string lastNotification = null;
            _view.OnTextUpdated.AddListener(updated => lastNotification = updated);

            Animate(() => _view.Set(value));
            AdvanceOwnedTweens(1f);

            Assert.AreEqual(expected, _label.text);
            Assert.AreEqual(expected, lastNotification);
        }

        [TestCase(long.MaxValue, "9223372036854775807")]
        [TestCase(long.MinValue, "-9223372036854775808")]
        public void SetInteger_PreservesLongValueWithoutApplyingIndexOffset(long value, string expected)
        {
            _view.IndexOffset = 31;
            _view.NumberNotationStyle = NumberNotation.Plain;

            Animate(() => _view.SetInteger(value));
            AdvanceOwnedTweens(1f);

            Assert.AreEqual(expected, _label.text);
        }

        [Test]
        public void IntegerCompletion_FormattingOnlyUpdatesRetainExactValueAndAffixes()
        {
            SerializedObject serialized = new SerializedObject(_view);
            serialized.FindProperty("startAdd").stringValue = "Level ";
            serialized.FindProperty("endAdd").stringValue = " XP";
            serialized.ApplyModifiedPropertiesWithoutUndo();
            _view.NumberNotationStyle = NumberNotation.Plain;

            Animate(() => _view.Set(16777217));
            AdvanceOwnedTweens(1f);
            Assert.AreEqual(16777216f, _view.currentNum,
                "The public animation state deliberately retains float interpolation semantics.");
            Assert.AreEqual("Level 16777217 XP", _label.text);

            _view.NumberNotationStyle = NumberNotation.Grouped;
            _view.Separator = ",";
            _view.DecimalPlaces = 2;
            _view.RoundingMode = NumberRoundingMode.AwayFromZero;
            Assert.AreEqual("Level 16,777,217 XP", _label.text);

            _view.NumberNotationStyle = NumberNotation.Plain;
            Assert.AreEqual("Level 16777217 XP", _label.text);
        }

        [Test]
        public void IntegerCompletion_DoesNotPublishARedundantIdenticalFinalString()
        {
            _view.NumberNotationStyle = NumberNotation.Plain;
            int notifications = 0;
            _view.OnTextUpdated.AddListener(value => notifications++);

            Animate(() => _view.Set(12));
            AdvanceOwnedTweens(1f);

            Assert.AreEqual("12", _label.text);
            Assert.AreEqual(1, notifications);
        }

        [TestCase("string", "Ready")]
        [TestCase("float", "9.5")]
        [TestCase("integer", "16777219")]
        [TestCase("clear", "")]
        public void IntegerCompletion_DoesNotOverwriteASupersedingWrite(string replacement, string expected)
        {
            _view.NumberNotationStyle = NumberNotation.Plain;
            _view.DecimalPlaces = 1;
            List<string> notifications = new List<string>();
            _view.OnTextUpdated.AddListener(value => notifications.Add(value));
            Tween retired = Animate(() => _view.Set(16777217));
            int retiredKills = 0;
            retired.OnKill(() => retiredKills++);
            AdvanceOwnedTweens(0.05f);
            Assert.IsTrue(retired.IsActive(), "Replacement requires a still-active original tween.");
            Assert.IsTrue(retired.IsPlaying());
            Assert.Greater(retired.Elapsed(false), 0f);
            Assert.Less(retired.Elapsed(false), retired.Duration(false));
            Assert.Greater(_view.currentNum, 0f);
            Assert.Less(_view.currentNum, 16777216f);

            switch (replacement)
            {
                case "string":
                    _view.Set("Ready");
                    break;
                case "float":
                    Animate(() => _view.Set(9.5f), retired);
                    break;
                case "integer":
                    Animate(() => _view.SetInteger(16777219L), retired);
                    break;
                case "clear":
                    _view.Clear();
                    break;
            }

            Assert.IsFalse(retired.IsActive(), "The replaced integer tween must be inactive before any later update.");
            Assert.AreEqual(1, retiredKills, "Replacement outside an update must kill the old owned tween exactly once.");
            AdvanceOwnedTweens(1f);
            Assert.AreEqual(expected, _label.text);
            Assert.AreEqual(expected, notifications[notifications.Count - 1]);
            int notificationsAfterCompletion = notifications.Count;
            float numberAfterCompletion = _view.currentNum;
            AdvanceOwnedTweens(1f);
            Assert.AreEqual(expected, _label.text);
            Assert.AreEqual(numberAfterCompletion, _view.currentNum);
            Assert.AreEqual(notificationsAfterCompletion, notifications.Count,
                "Neither the retired tween nor the completed replacement may publish a later write.");
            Assert.AreEqual(1, retiredKills);
        }

        [TestCase(16777216f, 9.5f, "9.5")]
        [TestCase(-16777216f, -9.5f, "-9.5")]
        public void FloatCompletion_SettlesTheRequestedTargetAfterLargeStart(
            float start, float target, string expected)
        {
            _view.NumberNotationStyle = NumberNotation.Plain;
            _view.DecimalPlaces = 1;
            _view.currentNum = start;
            Assert.AreEqual(1, _view.DecimalPlaces);
            Assert.Greater(Mathf.Abs(_view.currentNum), Mathf.Abs(target));
            string lastNotification = null;
            _view.OnTextUpdated.AddListener(value => lastNotification = value);

            Animate(() => _view.Set(target));
            AdvanceOwnedTweens(1f);

            Assert.AreEqual(target, _view.currentNum);
            Assert.AreEqual(expected, _label.text);
            Assert.AreEqual(expected, lastNotification);
            _view.Separator = ",";
            Assert.AreEqual(expected, _label.text);
        }

        [Test]
        public void IntegerCompletion_ReentrantReplacementKeepsTheNewestSource()
        {
            bool replaced = false;
            _view.OnTextUpdated.AddListener(value =>
            {
                if (!replaced)
                {
                    replaced = true;
                    _view.Set("Newest");
                }
            });

            Animate(() => _view.Set(16777217));
            AdvanceOwnedTweens(1f);
            _view.Separator = ",";

            Assert.IsTrue(replaced);
            Assert.AreEqual("Newest", _label.text);
        }

        [UnityTest]
        public IEnumerator Animation_HiddenParentPauses_AndDestroyedComponentStopsWriting()
        {
            Assert.IsTrue(Application.isPlaying, "Lifecycle evidence requires an actual Play Mode transition.");
            Assert.AreEqual(typeof(SetText), _view.GetType());
            Assert.IsTrue(_view.isActiveAndEnabled);
            Assert.AreSame(_label, _view.text);
            _parent = new GameObject("AuditTextParent");
            _go.transform.SetParent(_parent.transform);

            Tween tween = Animate(() => _view.Set(1000f));
            AdvanceOwnedTweens(0.05f);
            float pausedValue = _view.currentNum;
            string pausedText = _label.text;
            Assert.Greater(pausedValue, 0f,
                $"Owned Manual tween must advance before pause assertions; playing={tween.IsPlaying()}, " +
                $"elapsed={tween.Elapsed()}, Unity timeScale={Time.timeScale}, DOTween timeScale={DOTween.timeScale}.");
            Assert.Less(pausedValue, 1000f);

            _parent.SetActive(false);
            Assert.IsFalse(_view.isActiveAndEnabled);
            Assert.IsFalse(tween.IsPlaying(), "Native OnDisable must pause the owned tween.");
            AdvanceOwnedTweens(0.05f);
            Assert.AreEqual(pausedValue, _view.currentNum);
            Assert.AreEqual(pausedText, _label.text);

            _parent.SetActive(true);
            Assert.IsTrue(_view.isActiveAndEnabled);
            Assert.IsTrue(tween.IsPlaying(), "Native OnEnable must resume the paused tween.");
            AdvanceOwnedTweens(0.05f);
            Assert.Greater(_view.currentNum, pausedValue);
            Assert.Less(_view.currentNum, 1000f);

            int notifications = 0;
            _view.OnTextUpdated.AddListener(value => notifications++);
            UnityEngine.Object.DestroyImmediate(_view);
            Assert.IsFalse(tween.IsActive(), "Native destruction must release the unfinished owned tween.");
            string textAfterDestroy = _label.text;
            AdvanceOwnedTweens(1f);

            Assert.AreEqual(textAfterDestroy, _label.text);
            Assert.AreEqual(0, notifications);
            yield break;
        }

        [UnityTest]
        public IEnumerator SetOnDisabledComponent_WaitsForEnable()
        {
            Assert.IsTrue(Application.isPlaying, "Lifecycle evidence requires an actual Play Mode transition.");
            Assert.AreEqual(typeof(SetText), _view.GetType());
            Assert.IsTrue(_view.isActiveAndEnabled);
            Assert.AreSame(_label, _view.text);
            _view.enabled = false;
            Assert.IsFalse(_view.isActiveAndEnabled);
            Tween tween = Animate(() => _view.Set(100f));
            Assert.IsFalse(tween.IsPlaying(), "Setting a disabled component must queue a paused tween.");
            AdvanceOwnedTweens(1f);
            Assert.AreEqual("idle", _label.text);
            Assert.AreEqual(0f, _view.currentNum);

            _view.enabled = true;
            Assert.IsTrue(_view.isActiveAndEnabled);
            Assert.IsTrue(tween.IsPlaying(), "Native OnEnable must start the queued tween.");
            AdvanceOwnedTweens(1f);
            Assert.AreEqual("100", _label.text);
            Assert.AreEqual(100f, _view.currentNum);
            yield break;
        }

        private void CreateView()
        {
            _go = new GameObject("AuditSetText", typeof(RectTransform), typeof(TextMeshProUGUI));
            _label = _go.GetComponent<TextMeshProUGUI>();
            _label.text = "idle";
            _view = _go.AddComponent<SetText>();
            _view.text = _label;
            _view.OnTextUpdated = new UnityEvent<string>();
        }

        private Tween Animate(Action setValue, Tween retired = null)
        {
            Assert.IsTrue(_ownsPlaySession && Application.isPlaying,
                "Animation must run in the Play session acquired by UnitySetUp.");
            Assert.IsTrue(_nativeDriver != null && _nativeDriver.isActiveAndEnabled,
                "The admitted native DOTween driver must remain active before animation.");
            setValue();
            List<Tween> tweens = DOTween.TweensByTarget(_view);
            Assert.IsNotNull(tweens, "Set must create a real tween targeted to this component.");
            Tween owned = null;
            int activeCount = 0;
            int retiredMatches = 0;
            List<Tween> distinctActive = new List<Tween>();
            foreach (Tween tween in tweens)
            {
                if (ReferenceEquals(tween, retired))
                {
                    retiredMatches++;
                }

                if (tween.IsActive())
                {
                    owned = tween;
                    activeCount++;
                    if (!distinctActive.Exists(candidate => ReferenceEquals(candidate, tween)))
                    {
                        distinctActive.Add(tween);
                    }
                }
            }

            string ownership = $"Query entries={tweens.Count}, active entries={activeCount}, " +
                $"distinct active references={distinctActive.Count}, retired matches={retiredMatches}, " +
                $"retired active={retired != null && retired.IsActive()}.";
            Assert.IsFalse(retired != null && retired.IsActive(),
                "Set must kill the previous owned tween before configuring its replacement. " + ownership);
            Assert.AreEqual(1, activeCount, "Exactly one active tween must belong to this view. " + ownership);
            if (retired != null)
            {
                Assert.AreNotSame(retired, owned, "A non-recyclable retired tween must not become the replacement.");
            }

            // Configure the created tween after DOTween's first-use settings load, not its global default.
            owned.SetUpdate(UpdateType.Manual, true);
            Assert.Greater(owned.Duration(false), 0f, "The authored transition must have positive duration.");
            Assert.Greater(owned.timeScale, 0f, "The owned tween clock must advance.");
            Assert.Greater(DOTween.timeScale, 0f, "The isolated runner must provide an advancing DOTween clock.");
            Assert.AreEqual(_view.isActiveAndEnabled, owned.IsPlaying(),
                "Initial play/pause state must match the component's native enabled state.");
            _ownedTweens.Add(owned);
            return owned;
        }

        private void AdvanceOwnedTweens(float deltaTime)
        {
            Assert.IsTrue(_ownsPlaySession && Application.isPlaying,
                "Manual animation requires the acquired Play session.");
            Assert.IsTrue(_nativeDriver != null && _nativeDriver.isActiveAndEnabled,
                "Manual animation requires the admitted native DOTween driver.");
            int count = _ownedTweens.Count;
            for (int index = 0; index < count; index++)
            {
                Tween tween = _ownedTweens[index];
                if (tween.IsActive())
                {
                    tween.ManualUpdate(deltaTime, deltaTime);
                }
            }
        }
    }
}
