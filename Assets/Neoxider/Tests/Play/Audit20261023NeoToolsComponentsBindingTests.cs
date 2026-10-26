using System;
using System.Collections;
using System.Reflection;
using Neo.Save;
using Neo.Tools;
using NUnit.Framework;
using TMPro;
using UnityEngine;
using UnityEngine.TestTools;
using Object = UnityEngine.Object;

namespace Neo.Tests.Play
{
    public class Audit20261023NeoToolsComponentsBindingTests
    {
        private GameObject _viewObject;
        private GameObject _scoreObject;
        private string _saveKey;
        private float _previousTimeScale;
        private bool _previousCreateInstance;

        [UnitySetUp]
        public IEnumerator SetUp()
        {
            _previousTimeScale = Time.timeScale;
            _previousCreateInstance = ScoreManager.CreateInstance;
            Time.timeScale = 1f;
            ScoreManager.CreateInstance = false;
            yield return null;
        }

        [UnityTearDown]
        public IEnumerator TearDown()
        {
            if (_viewObject != null)
            {
                Object.Destroy(_viewObject);
            }

            if (_scoreObject != null)
            {
                Object.Destroy(_scoreObject);
            }

            yield return null;
            Time.timeScale = _previousTimeScale;
            ScoreManager.CreateInstance = _previousCreateInstance;
            if (_saveKey != null)
            {
                SaveProvider.DeleteKey(_saveKey);
            }
        }

        [UnityTest]
        public IEnumerator TextScore_DisabledWhileWaiting_BindsOnlyAfterReenable()
        {
            Assert.That(ScoreManager.I, Is.Null, "This fixture requires a scene without a ScoreManager.");
            _viewObject = new GameObject("AuditLateScoreView", typeof(RectTransform));
            TMP_Text label = _viewObject.AddComponent<TextMeshProUGUI>();
            TextScore view = _viewObject.AddComponent<TextScore>();
            view.text = label;
            view.currentNum = 7f;
            yield return null;
            view.enabled = false;

            _scoreObject = new GameObject("AuditLateScore");
            _scoreObject.SetActive(false);
            ScoreManager score = _scoreObject.AddComponent<ScoreManager>();
            _saveKey = "Audit20261023NeoToolsComponents.Binding." + Guid.NewGuid().ToString("N");
            FieldInfo saveKeyField = typeof(ScoreManager).GetField("_keySave",
                BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.That(saveKeyField, Is.Not.Null);
            saveKeyField.SetValue(score, _saveKey);
            _scoreObject.SetActive(true);
            score.Set(42, false);

            double deadline = Time.realtimeSinceStartupAsDouble + 0.5;
            while (Time.realtimeSinceStartupAsDouble < deadline)
            {
                yield return null;
            }

            Assert.That(view.currentNum, Is.EqualTo(7f), "A disabled view must not acquire a late score source.");
            view.enabled = true;
            deadline = Time.realtimeSinceStartupAsDouble + 3.0;
            while (Mathf.Abs(view.currentNum - 42f) > 0.001f && Time.realtimeSinceStartupAsDouble < deadline)
            {
                yield return null;
            }

            Assert.That(view.currentNum, Is.EqualTo(42f).Within(0.001f));
            score.Set(84, false);
            deadline = Time.realtimeSinceStartupAsDouble + 3.0;
            while (Mathf.Abs(view.currentNum - 84f) > 0.001f && Time.realtimeSinceStartupAsDouble < deadline)
            {
                yield return null;
            }

            Assert.That(view.currentNum, Is.EqualTo(84f).Within(0.001f));
        }

        [UnityTest]
        public IEnumerator TextScore_ComponentDisabledDuringTween_PausesAndRebindsToLatestScore()
        {
            return CheckAnimatedDisable(false);
        }

        [UnityTest]
        public IEnumerator TextScore_ObjectDisabledDuringTween_PausesAndRebindsToLatestScore()
        {
            return CheckAnimatedDisable(true);
        }

        private IEnumerator CheckAnimatedDisable(bool disableObject)
        {
            Assert.That(ScoreManager.I, Is.Null, "This fixture requires a scene without a ScoreManager.");
            _scoreObject = new GameObject("AuditAnimatedScore");
            _scoreObject.SetActive(false);
            ScoreManager score = _scoreObject.AddComponent<ScoreManager>();
            _saveKey = "Audit20261023NeoToolsComponents.Animation." + Guid.NewGuid().ToString("N");
            typeof(ScoreManager).GetField("_keySave", BindingFlags.Instance | BindingFlags.NonPublic)
                .SetValue(score, _saveKey);
            _scoreObject.SetActive(true);
            int baselineListeners = score.Score.ListenerCount;

            _viewObject = new GameObject("AuditAnimatedScoreView", typeof(RectTransform));
            _viewObject.SetActive(false);
            TMP_Text label = _viewObject.AddComponent<TextMeshProUGUI>();
            TextScore view = _viewObject.AddComponent<TextScore>();
            view.text = label;
            typeof(TextScore).GetField("_scoreSource", BindingFlags.Instance | BindingFlags.NonPublic)
                .SetValue(view, score);
            typeof(SetText).GetField("_timeAnim", BindingFlags.Instance | BindingFlags.NonPublic)
                .SetValue(view, 0.5f);
            _viewObject.SetActive(true);
            yield return null;
            Assert.That(score.Score.ListenerCount, Is.EqualTo(baselineListeners + 1));
            score.Set(100, false);

            double deadline = Time.realtimeSinceStartupAsDouble + 3.0;
            while (view.currentNum <= 0f && Time.realtimeSinceStartupAsDouble < deadline)
            {
                yield return null;
            }

            Assert.That(view.currentNum, Is.GreaterThan(0f).And.LessThan(100f),
                "The disable must happen during a native tween, before completion.");
            if (disableObject)
            {
                _viewObject.SetActive(false);
            }
            else
            {
                view.enabled = false;
            }

            float pausedValue = view.currentNum;
            string pausedText = label.text;
            Assert.That(score.Score.ListenerCount, Is.EqualTo(baselineListeners));
            score.Set(200, false);
            deadline = Time.realtimeSinceStartupAsDouble + 0.65;
            while (Time.realtimeSinceStartupAsDouble < deadline)
            {
                yield return null;
            }

            Assert.That(view.currentNum, Is.EqualTo(pausedValue), "Disabled inherited animation must stay paused.");
            Assert.That(label.text, Is.EqualTo(pausedText));
            if (disableObject)
            {
                _viewObject.SetActive(true);
            }
            else
            {
                view.enabled = true;
            }

            deadline = Time.realtimeSinceStartupAsDouble + 3.0;
            while (Mathf.Abs(view.currentNum - 200f) > 0.001f && Time.realtimeSinceStartupAsDouble < deadline)
            {
                yield return null;
            }

            Assert.That(view.currentNum, Is.EqualTo(200f).Within(0.001f));
            Assert.That(label.text, Is.EqualTo("200"));
            Assert.That(score.Score.ListenerCount, Is.EqualTo(baselineListeners + 1),
                "Re-enabling owns exactly one reactive binding.");
        }
    }
}
