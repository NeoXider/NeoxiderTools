using System.Reflection;
using Neo.Tools;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.EventSystems;

namespace Neo.Editor.Tests
{
    [TestFixture]
    public class InteractiveObjectTests
    {
        private GameObject _testObj;
        private InteractiveObject _interactiveObject;
        private EventSystem _eventSystem;

        [SetUp]
        public void SetUp()
        {
            var esObj = new GameObject("EventSystem");
            _eventSystem = esObj.AddComponent<EventSystem>();

            _testObj = new GameObject("TestInteractive");
            var cameraObj = new GameObject("MainCamera");
            cameraObj.AddComponent<Camera>();
            cameraObj.tag = "MainCamera";

            // WHY: InteractiveObject requires a collider
            _testObj.AddComponent<BoxCollider>();
            _testObj.AddComponent<Mirror.NetworkIdentity>();
            _interactiveObject = _testObj.AddComponent<InteractiveObject>();

            _interactiveObject.onHoverEnter = new UnityEngine.Events.UnityEvent();
            _interactiveObject.onHoverExit = new UnityEngine.Events.UnityEvent();
            _interactiveObject.onClick = new UnityEngine.Events.UnityEvent();
            _interactiveObject.onDoubleClick = new UnityEngine.Events.UnityEvent();
            _interactiveObject.onRightClick = new UnityEngine.Events.UnityEvent();
            _interactiveObject.onMiddleClick = new UnityEngine.Events.UnityEvent();
            _interactiveObject.onInteractDown = new UnityEngine.Events.UnityEvent();
            _interactiveObject.onInteractUp = new UnityEngine.Events.UnityEvent();

            // WHY: Bypass automatic raycasters addition which might need Canvas or PhysicsRaycaster
            FieldInfo autoCreateESField = typeof(InteractiveObject).GetField("_autoCreateEventSystemIfMissing",
                BindingFlags.NonPublic | BindingFlags.Instance);
            autoCreateESField?.SetValue(_interactiveObject, false);

            FieldInfo autoCheckESField = typeof(InteractiveObject).GetField("_autoCheckEventSystem",
                BindingFlags.NonPublic | BindingFlags.Instance);
            autoCheckESField?.SetValue(_interactiveObject, false);

            // WHY: Set interact distance to 0 for pure logic test (ignores camera distance calculation)
            FieldInfo distanceField = typeof(InteractiveObject).GetField("interactionDistance",
                BindingFlags.NonPublic | BindingFlags.Instance);
            distanceField?.SetValue(_interactiveObject, 0f);

            // WHY: Invoke Awake to cache colliders
            MethodInfo awakeMethod =
                typeof(InteractiveObject).GetMethod("Awake", BindingFlags.NonPublic | BindingFlags.Instance);
            awakeMethod?.Invoke(_interactiveObject, null);
        }

        [TearDown]
        public void TearDown()
        {
            if (_testObj != null)
            {
                Object.DestroyImmediate(_testObj);
            }

            if (_eventSystem != null)
            {
                Object.DestroyImmediate(_eventSystem.gameObject);
            }

            var cam = GameObject.Find("MainCamera");
            if (cam != null)
            {
                Object.DestroyImmediate(cam);
            }
        }

        [Test]
        public void OnPointerEnter_WhenInteractable_TriggersHoverEnter()
        {
            bool hovered = false;
            _interactiveObject.onHoverEnter.AddListener(() => hovered = true);

            _interactiveObject.interactable = true;
            var pointerData = new PointerEventData(_eventSystem);

            _interactiveObject.OnPointerEnter(pointerData);

            Assert.IsTrue(hovered, "Hover Enter event should fire when interactable.");
            Assert.IsTrue(_interactiveObject.IsHovered, "InteractiveObject should report IsHovered = true.");
        }

        [Test]
        public void OnPointerEnter_WhenNotInteractable_DoesNotTriggerHoverEnter()
        {
            bool hovered = false;
            _interactiveObject.onHoverEnter.AddListener(() => hovered = true);

            _interactiveObject.interactable = false;
            var pointerData = new PointerEventData(_eventSystem);

            _interactiveObject.OnPointerEnter(pointerData);

            Assert.IsFalse(hovered, "Hover Enter event should NOT fire when not interactable.");
            Assert.IsFalse(_interactiveObject.IsHovered, "InteractiveObject should report IsHovered = false.");
        }

        [Test]
        public void OnPointerExit_TriggersHoverExit()
        {
            bool exitFired = false;
            _interactiveObject.onHoverExit.AddListener(() => exitFired = true);

            var pointerData = new PointerEventData(_eventSystem);
            _interactiveObject.OnPointerEnter(pointerData);
            _interactiveObject.OnPointerExit(pointerData);

            Assert.IsTrue(exitFired, "Hover Exit event should fire.");
            Assert.IsFalse(_interactiveObject.IsHovered, "InteractiveObject should report IsHovered = false.");
        }

        [Test]
        public void OnPointerClick_WhenValid_TriggersOnClick()
        {
            bool clicked = false;
            _interactiveObject.onClick.AddListener(() => clicked = true);

            // WHY: Mock hasCurrentMouseHit to bypass camera raycast logic
            FieldInfo hasMouseHitField = typeof(InteractiveObject).GetField("hasCurrentMouseHit",
                BindingFlags.NonPublic | BindingFlags.Instance);
            hasMouseHitField?.SetValue(_interactiveObject, true);

            var pointerData = new PointerEventData(_eventSystem);
            pointerData.button = PointerEventData.InputButton.Left;

            _interactiveObject.OnPointerClick(pointerData);

            Assert.IsTrue(clicked, "Click event should fire on Left button click when valid.");
        }

        [Test]
        public void OnPointerClick_RightClick_TriggersOnRightClick()
        {
            bool rightClicked = false;
            _interactiveObject.onRightClick.AddListener(() => rightClicked = true);

            // WHY: Mock hasCurrentMouseHit to bypass camera raycast logic
            FieldInfo hasMouseHitField = typeof(InteractiveObject).GetField("hasCurrentMouseHit",
                BindingFlags.NonPublic | BindingFlags.Instance);
            hasMouseHitField?.SetValue(_interactiveObject, true);

            var pointerData = new PointerEventData(_eventSystem);
            pointerData.button = PointerEventData.InputButton.Right;

            _interactiveObject.OnPointerClick(pointerData);

            Assert.IsTrue(rightClicked, "Right click event should fire.");
        }

        [Test]
        public void ManualInteractionApi_RoutesEveryEvent()
        {
            int downCount = 0;
            int upCount = 0;
            int clickCount = 0;
            int doubleClickCount = 0;
            int rightClickCount = 0;
            int middleClickCount = 0;
            _interactiveObject.onInteractDown.AddListener(() => downCount++);
            _interactiveObject.onInteractUp.AddListener(() => upCount++);
            _interactiveObject.onClick.AddListener(() => clickCount++);
            _interactiveObject.onDoubleClick.AddListener(() => doubleClickCount++);
            _interactiveObject.onRightClick.AddListener(() => rightClickCount++);
            _interactiveObject.onMiddleClick.AddListener(() => middleClickCount++);

            _interactiveObject.InteractDown();
            _interactiveObject.InteractUp();
            _interactiveObject.Click();
            _interactiveObject.Click(InteractiveObject.MouseButton.Left, true);
            _interactiveObject.Click(InteractiveObject.MouseButton.Right);
            _interactiveObject.Click(InteractiveObject.MouseButton.Middle);

            Assert.That(downCount, Is.EqualTo(1));
            Assert.That(upCount, Is.EqualTo(1));
            Assert.That(clickCount, Is.EqualTo(1));
            Assert.That(doubleClickCount, Is.EqualTo(1));
            Assert.That(rightClickCount, Is.EqualTo(1));
            Assert.That(middleClickCount, Is.EqualTo(1));
        }

        [Test]
        public void ManualInteractionApi_WhenNotInteractable_DoesNotInvokeEvents()
        {
            int invocationCount = 0;
            _interactiveObject.onInteractDown.AddListener(() => invocationCount++);
            _interactiveObject.onInteractUp.AddListener(() => invocationCount++);
            _interactiveObject.onClick.AddListener(() => invocationCount++);
            _interactiveObject.onDoubleClick.AddListener(() => invocationCount++);
            _interactiveObject.onRightClick.AddListener(() => invocationCount++);
            _interactiveObject.onMiddleClick.AddListener(() => invocationCount++);
            _interactiveObject.interactable = false;

            _interactiveObject.InteractDown();
            _interactiveObject.InteractUp();
            _interactiveObject.Click();
            _interactiveObject.Click(InteractiveObject.MouseButton.Left, true);
            _interactiveObject.Click(InteractiveObject.MouseButton.Right);
            _interactiveObject.Click(InteractiveObject.MouseButton.Middle);

            Assert.That(invocationCount, Is.Zero);
        }

        [TestCase(nameof(InteractiveObject.InteractDown), "Test Interact Down")]
        [TestCase(nameof(InteractiveObject.InteractUp), "Test Interact Up")]
        [TestCase(nameof(InteractiveObject.Click), "Test Click")]
        [TestCase(nameof(InteractiveObject.InvalidateCachedColliders), "Invalidate Colliders")]
        public void InspectorTestMethods_ArePlayModeOnlyButtons(string methodName, string buttonName)
        {
            MethodInfo method = typeof(InteractiveObject).GetMethod(methodName, BindingFlags.Public | BindingFlags.Instance);
            Neo.ButtonAttribute attribute = method?.GetCustomAttribute<Neo.ButtonAttribute>();

            Assert.That(method, Is.Not.Null);
            Assert.That(attribute, Is.Not.Null);
            Assert.That(attribute.ButtonName, Is.EqualTo(buttonName));
            Assert.That(attribute.PlayModeOnly, Is.True);
        }

        [Test]
        public void Click_DefaultParameters_TestLeftSingleClick()
        {
            MethodInfo method = typeof(InteractiveObject).GetMethod(nameof(InteractiveObject.Click),
                BindingFlags.Public | BindingFlags.Instance);
            ParameterInfo[] parameters = method?.GetParameters();

            Assert.That(parameters, Is.Not.Null);
            Assert.That(parameters.Length, Is.EqualTo(2));
            Assert.That(parameters[0].DefaultValue, Is.EqualTo(InteractiveObject.MouseButton.Left));
            Assert.That(parameters[1].DefaultValue, Is.EqualTo(false));
        }
    }
}
