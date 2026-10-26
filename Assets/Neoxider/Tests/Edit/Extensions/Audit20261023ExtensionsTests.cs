using System;
using System.Collections;
using System.Collections.Generic;
using Neo.Extensions;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.Events;
using Object = UnityEngine.Object;

namespace Neo.Editor.Tests
{
    [TestFixture]
    public class Audit20261023ExtensionsTests
    {
        private readonly List<GameObject> _objects = new();
        private const string BoolArrayKey = "Neo.Audit20261023Extensions.BoolArray";
        private UnityEngine.Random.State _randomState;

        [SetUp]
        public void SetUp()
        {
            _randomState = UnityEngine.Random.state;
        }

        [TearDown]
        public void TearDown()
        {
            try
            {
                foreach (GameObject gameObject in _objects)
                {
                    if (gameObject != null)
                    {
                        Object.DestroyImmediate(gameObject);
                    }
                }

                _objects.Clear();
                PlayerPrefs.DeleteKey(BoolArrayKey);
            }
            finally
            {
                UnityEngine.Random.state = _randomState;
            }
        }

        [TestCase("NaN")]
        [TestCase("Infinity")]
        [TestCase("-Infinity")]
        [TestCase("NaN:00")]
        [TestCase("1:NaN")]
        [TestCase("1e309:00")]
        public void Duration_NonFiniteValue_ReturnsFalseAndZero(string raw)
        {
            Assert.IsFalse(TimeParsingExtensions.TryParseDuration(raw, out float seconds));
            Assert.AreEqual(0f, seconds);
        }

        [TestCase(1e100, "1e+100")]
        [TestCase(1.23e100, "1.23e+100")]
        [TestCase(-1.23e110, "-1.23e+110")]
        public void Scientific_TrimZeros_PreservesExponent(double value, string expected)
        {
            NumberFormatOptions options = new(NumberNotation.Scientific, 2);
            Assert.AreEqual(expected, value.ToPrettyString(options));
        }

        [Test]
        public void Plain_TrimZeros_RemovesWholeMultiCharacterSeparator()
        {
            NumberFormatOptions options = new(NumberNotation.Plain, 3, decimalSeparator: "::");
            Assert.AreEqual("12", 12m.ToPrettyString(options));
            Assert.AreEqual("12::5", 12.5m.ToPrettyString(options));
        }

        [Test]
        public void Wrap_FullIntegerRange_DoesNotOverflow()
        {
            Assert.AreEqual(0, 0.Wrap(int.MinValue, int.MaxValue));
            Assert.AreEqual(int.MinValue, int.MaxValue.Wrap(int.MinValue, int.MaxValue));
            Assert.AreEqual(1, int.MinValue.Wrap(-1, 2));
            Assert.AreEqual(5, 10.Wrap(5, 5));
        }

        [Test]
        public void PingPong_LargeLength_DoesNotOverflow()
        {
            Assert.AreEqual(int.MaxValue, int.MaxValue.PingPong(int.MaxValue));
            Assert.AreEqual(int.MaxValue - 1, int.MinValue.PingPong(int.MaxValue));
            Assert.AreEqual(1, (-1).PingPong(int.MaxValue));
        }

        [Test]
        public void Cooldown_Accumulation_SaturatesInsteadOfReturningNegativeCount()
        {
            DateTime start = new(2000, 1, 1, 0, 0, 0, DateTimeKind.Utc);
            Assert.AreEqual(int.MaxValue, start.GetAccumulatedClaimCount(1f, start.AddYears(100)));
            Assert.AreEqual(0, start.GetAccumulatedClaimCount(float.NaN, start.AddDays(1)));
            Assert.AreEqual(0, start.GetAccumulatedClaimCount(float.PositiveInfinity, start.AddDays(1)));
        }

        [TestCase("")]
        [TestCase("invalid")]
        [TestCase("1,invalid")]
        [TestCase("0,2")]
        public void BoolArray_InvalidStoredData_PreservesCallerDefault(string raw)
        {
            bool[] fallback = { true, false };
            PlayerPrefs.SetString(BoolArrayKey, raw);
            Assert.AreSame(fallback, PlayerPrefsUtils.GetBoolArray(BoolArrayKey, fallback));
        }

        [Test]
        public void Gradient_SingleCharacter_UsesStartColor()
        {
            Assert.AreEqual("<color=#FF0000>A</color>", "A".Gradient(Color.red, Color.blue));
            Assert.AreEqual("<color=#FF0000>A</color><color=#0000FF>B</color>",
                "AB".Gradient(Color.red, Color.blue));
        }

        [Test]
        public void DelegateCache_ReplacingSlot_DetachesPreviousListener()
        {
            UnityEventDelegateCache cache = new();
            UnityEvent evt = new();
            int oldCalls = 0;
            int newCalls = 0;
            cache.SubscribeAt(0, evt, () => oldCalls++);
            cache.SubscribeAt(0, evt, () => newCalls++);
            evt.Invoke();
            Assert.AreEqual(0, oldCalls);
            Assert.AreEqual(1, newCalls);
            Assert.AreEqual(1, cache.Count);
            cache.UnsubscribeAt(0, evt);
            evt.Invoke();
            Assert.AreEqual(1, newCalls);
        }

        [Test]
        public void DelegateCache_MovingSlot_DetachesPreviousEvent()
        {
            UnityEventDelegateCache cache = new();
            UnityEvent previous = new();
            UnityEvent current = new();
            int calls = 0;
            UnityAction action = () => calls++;
            cache.SubscribeAt(2, previous, action);
            cache.SubscribeAt(2, current, action);
            previous.Invoke();
            Assert.AreEqual(0, calls);
            cache.UnsubscribeAt(2, previous);
            Assert.AreSame(action, cache[2]);
            current.Invoke();
            Assert.AreEqual(1, calls);
            cache.UnsubscribeAt(2, current);
            current.Invoke();
            Assert.AreEqual(1, calls);
        }

        [Test]
        public void DelegateCache_AddAndClear_KeepManualSubscriptionContract()
        {
            UnityEventDelegateCache cache = new();
            UnityEvent evt = new();
            int calls = 0;
            UnityAction action = () => calls++;
            evt.AddListener(action);
            cache.Add(action);
            cache.UnsubscribeAt(0, evt);
            evt.Invoke();
            Assert.AreEqual(0, calls);
            cache.SubscribeAt(0, evt, action);
            cache.Clear();
            evt.Invoke();
            Assert.AreEqual(1, calls);
            Assert.AreEqual(0, cache.Count);
        }

        [TestCase("line")]
        [TestCase("grid")]
        [TestCase("circle")]
        [TestCase("grid3d")]
        [TestCase("circle3d")]
        [TestCase("sphere")]
        [TestCase("spiral")]
        [TestCase("sine")]
        public void Layout_OneShotSource_IsEnumeratedOnce(string layout)
        {
            Transform first = CreateObject("First").transform;
            Transform second = CreateObject("Second").transform;
            Vector3 initial = new(999f, 999f, 999f);
            second.position = initial;
            OneShotEnumerable<Transform> elements = new(new[] { first, second });
            switch (layout)
            {
                case "line": elements.ArrangeInLine(Vector3.zero, Vector3.right, 2f); break;
                case "grid": elements.ArrangeInGrid(Vector3.zero, 2, 2f, 3f); break;
                case "circle": elements.ArrangeInCircle(Vector3.zero, 2f); break;
                case "grid3d": elements.ArrangeInGrid3D(Vector3.zero, 2, Vector3.right, Vector3.up, 2f, 3f); break;
                case "circle3d": elements.ArrangeInCircle3D(Vector3.zero, Vector3.up, 2f); break;
                case "sphere": elements.ArrangeOnSphereSurface(Vector3.zero, 2f); break;
                case "spiral": elements.ArrangeInSpiral(Vector3.zero, 2f, 90f); break;
                case "sine": elements.ArrangeOnSineWave(Vector3.zero, Vector3.right, 1f, 1f, 2f); break;
                default: Assert.Fail("Unknown layout."); break;
            }

            Assert.AreEqual(1, elements.EnumerationCount);
            Assert.AreNotEqual(initial, second.position);
        }

        [Test]
        public void CombinedBounds_OneShotSource_IncludesEveryRenderer()
        {
            GameObject first = GameObject.CreatePrimitive(PrimitiveType.Cube);
            GameObject second = GameObject.CreatePrimitive(PrimitiveType.Cube);
            _objects.Add(first);
            _objects.Add(second);
            first.transform.position = new Vector3(-3f, 0f, 0f);
            second.transform.position = new Vector3(4f, 0f, 0f);
            OneShotEnumerable<GameObject> elements = new(new[] { null, first, second });
            Bounds bounds = elements.GetCombinedBounds();
            Assert.AreEqual(1, elements.EnumerationCount);
            Assert.AreEqual(new Vector3(-3.5f, -0.5f, -0.5f), bounds.min);
            Assert.AreEqual(new Vector3(4.5f, 0.5f, 0.5f), bounds.max);
        }

        [Test]
        public void ScreenBounds_RotatedCamera_ContainsAllViewportCorners()
        {
            Camera camera = CreateObject("Camera").AddComponent<Camera>();
            camera.orthographic = true;
            camera.aspect = 1f;
            camera.transform.rotation = Quaternion.Euler(0f, 0f, 45f);
            Bounds bounds = camera.GetWorldScreenBounds(10f);
            for (int x = 0; x <= 1; x++)
            {
                for (int y = 0; y <= 1; y++)
                {
                    Assert.IsTrue(bounds.Contains(camera.ViewportToWorldPoint(new Vector3(x, y, 10f))));
                }
            }
        }

        [Test]
        public void WorldToCanvas_Overlay_ReturnsLocalCoordinates()
        {
            Camera camera = CreateObject("Camera").AddComponent<Camera>();
            camera.pixelRect = new Rect(0f, 0f, 800f, 600f);
            camera.transform.position = new Vector3(0f, 0f, -10f);
            GameObject canvasObject = new("Canvas", typeof(RectTransform), typeof(Canvas));
            _objects.Add(canvasObject);
            Canvas canvas = canvasObject.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            RectTransform rectangle = canvasObject.GetComponent<RectTransform>();
            rectangle.position = camera.WorldToScreenPoint(Vector3.zero);
            Vector2 local = UIUtils.WorldToCanvasPoint(canvas, Vector3.zero, camera);
            Assert.AreEqual(0f, local.x, 0.001f);
            Assert.AreEqual(0f, local.y, 0.001f);
        }

        private GameObject CreateObject(string name)
        {
            GameObject gameObject = new(name);
            _objects.Add(gameObject);
            return gameObject;
        }

        private sealed class OneShotEnumerable<T> : IEnumerable<T>
        {
            private readonly IEnumerable<T> _source;
            public int EnumerationCount { get; private set; }

            public OneShotEnumerable(IEnumerable<T> source)
            {
                _source = source;
            }

            public IEnumerator<T> GetEnumerator()
            {
                EnumerationCount++;
                if (EnumerationCount > 1)
                {
                    throw new InvalidOperationException("The source must only be enumerated once.");
                }

                return _source.GetEnumerator();
            }

            IEnumerator IEnumerable.GetEnumerator()
            {
                return GetEnumerator();
            }
        }
    }
}
