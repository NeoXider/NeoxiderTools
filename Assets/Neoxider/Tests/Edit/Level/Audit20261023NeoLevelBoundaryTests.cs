using System.Reflection;
using NUnit.Framework;
using TMPro;
using UnityEngine;
using UnityEngine.Events;
using Object = UnityEngine.Object;

namespace Neo.Level.Tests
{
    public class Audit20261023NeoLevelBoundaryTests
    {
        private GameObject _root;

        [TearDown]
        public void TearDown()
        {
            if (_root != null)
            {
                Object.DestroyImmediate(_root);
            }
        }

        [TestCase(1, int.MaxValue, "2147483648")]
        [TestCase(2, int.MaxValue, "2147483648")]
        [TestCase(0, int.MaxValue, "2147483648")]
        [TestCase(1, int.MinValue, "-2147483647")]
        public void SetVisual_WidensLabelAdditionAndPreservesIndexStateAndEvents(int visual, int level,
            string expectedText)
        {
            _root = new GameObject("Level button boundary", typeof(RectTransform), typeof(TextMeshProUGUI));
            _root.SetActive(false);
            TextMeshProUGUI text = _root.GetComponent<TextMeshProUGUI>();
            LevelButton button = _root.AddComponent<LevelButton>();
            FieldInfo labelField = typeof(LevelButton).GetField("_textLvl",
                BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.That(labelField, Is.Not.Null);
            labelField.SetValue(button, text);
            button.OnChangeVisual = new UnityEvent<int>();
            button.OnDisableVisual = new UnityEvent();
            button.OnCurrentVisual = new UnityEvent();
            button.OnEnableVisual = new UnityEvent();
            int payload = -1;
            int locked = 0;
            int current = 0;
            int passed = 0;
            button.OnChangeVisual.AddListener(value => payload = value);
            button.OnDisableVisual.AddListener(() => locked++);
            button.OnCurrentVisual.AddListener(() => current++);
            button.OnEnableVisual.AddListener(() => passed++);

            button.SetVisual(visual, level);

            Assert.That(text.text, Is.EqualTo(expectedText));
            Assert.That(button.level, Is.EqualTo(level));
            Assert.That(button.activ, Is.EqualTo(visual != 0));
            Assert.That(payload, Is.EqualTo(visual));
            Assert.That(locked, Is.EqualTo(visual == 0 ? 1 : 0));
            Assert.That(current, Is.EqualTo(visual == 1 ? 1 : 0));
            Assert.That(passed, Is.EqualTo(visual == 2 ? 1 : 0));
        }
    }
}
