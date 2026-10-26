using System.Collections.Generic;
using System.Globalization;
using Neo.Extensions;
using Neo.Tools;
using NUnit.Framework;
using TMPro;
using UnityEngine;
using UnityEngine.Events;

namespace Neo.Editor.Tests
{
    [TestFixture]
    public sealed class Audit20261023TestUtilitiesSceneContractsTests
    {
        private GameObject _root;

        [SetUp]
        public void SetUp()
        {
            _root = new GameObject("Audit20261023TestUtilities.SceneContracts", typeof(RectTransform));
            _root.hideFlags = HideFlags.HideAndDontSave;
            _root.SetActive(false);
        }

        [TearDown]
        public void TearDown()
        {
            if (_root != null)
            {
                Object.DestroyImmediate(_root);
            }
        }

        [TestCase(-5f, true, "0%")]
        [TestCase(50.5f, true, "50.5%")]
        [TestCase(105f, true, "100%")]
        [TestCase(50.5f, false, "50.5")]
        public void SetPercentage_UsesExactTextAndEventPayload(float value, bool addSign, string expected)
        {
            TextMeshProUGUI label = _root.AddComponent<TextMeshProUGUI>();
            SetText presenter = _root.AddComponent<SetText>();
            presenter.text = label;
            presenter.NumberNotationStyle = NumberNotation.Plain;
            presenter.DecimalPlaces = 1;
            presenter.OnTextUpdated = new UnityEvent<string>();
            List<string> observed = new();
            presenter.OnTextUpdated.AddListener(observed.Add);

            presenter.SetPercentage(value, addSign);

            Assert.That(label.text, Is.EqualTo(expected));
            CollectionAssert.AreEqual(new[] { expected }, observed);
        }

        [TestCase("en-US")]
        [TestCase("ru-RU")]
        public void SetCurrency_UsesConfiguredSeparatorsAndExactEventPayload(string culture)
        {
            CultureInfo previous = CultureInfo.CurrentCulture;
            try
            {
                CultureInfo.CurrentCulture = CultureInfo.GetCultureInfo(culture);
                TextMeshProUGUI label = _root.AddComponent<TextMeshProUGUI>();
                SetText presenter = _root.AddComponent<SetText>();
                presenter.text = label;
                presenter.NumberNotationStyle = NumberNotation.Grouped;
                presenter.Separator = ",";
                presenter.DecimalPlaces = 2;
                presenter.OnTextUpdated = new UnityEvent<string>();
                List<string> observed = new();
                presenter.OnTextUpdated.AddListener(observed.Add);

                presenter.SetCurrency(1250.5f, "$");

                Assert.That(label.text, Is.EqualTo("$1,250.5"));
                CollectionAssert.AreEqual(new[] { "$1,250.5" }, observed);
            }
            finally
            {
                CultureInfo.CurrentCulture = previous;
            }
        }

        [TestCase(null)]
        [TestCase("")]
        [TestCase("01:60")]
        [TestCase("NaN")]
        [TestCase("1:NaN")]
        [TestCase("1e100")]
        public void TimeToText_InvalidDuration_PreservesTextTimeAndEvents(string raw)
        {
            TextMeshProUGUI label = _root.AddComponent<TextMeshProUGUI>();
            TimeToText presenter = _root.AddComponent<TimeToText>();
            presenter.text = label;
            presenter.DisplayMode = TimeDisplayMode.Clock;
            presenter.TimeFormat = TimeFormat.MinutesSeconds;
            presenter.OnTimeChanged = new UnityEvent<float>();
            presenter.Set(90f);
            List<float> observed = new();
            presenter.OnTimeChanged.AddListener(observed.Add);

            bool accepted = presenter.TrySetFromString(raw);

            Assert.That(accepted, Is.False);
            Assert.That(presenter.CurrentTime, Is.EqualTo(90f));
            Assert.That(label.text, Is.EqualTo("01:30"));
            Assert.That(observed, Is.Empty);
        }

        [Test]
        public void TimeToText_CustomSeparator_UpdatesTextAndNotifiesOnlyWhenTheTimeChanges()
        {
            TextMeshProUGUI label = _root.AddComponent<TextMeshProUGUI>();
            TimeToText presenter = _root.AddComponent<TimeToText>();
            presenter.text = label;
            presenter.DisplayMode = TimeDisplayMode.Clock;
            presenter.TimeFormat = TimeFormat.MinutesSeconds;
            presenter.Separator = "::";
            presenter.OnTimeChanged = new UnityEvent<float>();
            List<float> observed = new();
            presenter.OnTimeChanged.AddListener(observed.Add);

            Assert.That(presenter.TrySetFromString("02::30"), Is.True);
            Assert.That(presenter.TrySetFromString("02::30"), Is.True);

            Assert.That(presenter.CurrentTime, Is.EqualTo(150f));
            Assert.That(label.text, Is.EqualTo("02::30"));
            CollectionAssert.AreEqual(new[] { 150f }, observed);
        }

        [TestCase(5f, 0f, 0f)]
        [TestCase(0f, 0f, -5f)]
        public void RotateTo_InstantTurn_ChangesAnUnalignedPose(float x, float y, float z)
        {
            UniversalRotator rotator = _root.AddComponent<UniversalRotator>();
            _root.transform.SetPositionAndRotation(new Vector3(1f, 2f, 3f), Quaternion.identity);
            Vector3 direction = new(x, y, z);
            Vector3 before = _root.transform.forward;

            rotator.RotateTo(_root.transform.position + direction, true);

            Assert.That(Vector3.Angle(before, _root.transform.forward), Is.GreaterThan(45f));
            Assert.That(Vector3.Angle(direction, _root.transform.forward), Is.LessThan(0.01f));
        }

        [Test]
        public void RotateTo_AbsoluteYawLimit_ClampsAnActualTurn()
        {
            UniversalRotator rotator = _root.AddComponent<UniversalRotator>();
            rotator.limitedAxis3D = UniversalRotator.Axis.Y;
            rotator.limitRange = new Vector2(-30f, 30f);
            rotator.limitsRelativeToInitial = false;
            _root.transform.SetPositionAndRotation(Vector3.zero, Quaternion.identity);

            rotator.RotateTo(Vector3.right * 5f, true);

            Assert.That(Quaternion.Angle(_root.transform.rotation, Quaternion.Euler(0f, 30f, 0f)),
                Is.LessThan(0.01f));
        }
    }
}
