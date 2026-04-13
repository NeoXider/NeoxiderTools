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
            // Setup Event System context
            var esObj = new GameObject("EventSystem");
            _eventSystem = esObj.AddComponent<EventSystem>();

            _testObj = new GameObject("TestInteractive");
            var cameraObj = new GameObject("MainCamera");
            cameraObj.AddComponent<Camera>();
            cameraObj.tag = "MainCamera";

            // InteractiveObject requires a collider
            _testObj.AddComponent<BoxCollider>();
            _testObj.AddComponent<Mirror.NetworkIdentity>();
            _interactiveObject = _testObj.AddComponent<InteractiveObject>();

            _interactiveObject.onHoverEnter = new UnityEngine.Events.UnityEvent();
            _interactiveObject.onHoverExit = new UnityEngine.Events.UnityEvent();
            _interactiveObject.onClick = new UnityEngine.Events.UnityEvent();
            _interactiveObject.onRightClick = new UnityEngine.Events.UnityEvent();

            // Bypass automatic raycasters addition which might need Canvas or PhysicsRaycaster
            FieldInfo autoCreateESField = typeof(InteractiveObject).GetField("_autoCreateEventSystemIfMissing",
                BindingFlags.NonPublic | BindingFlags.Instance);
            autoCreateESField?.SetValue(_interactiveObject, false);

            FieldInfo autoCheckESField = typeof(InteractiveObject).GetField("_autoCheckEventSystem",
                BindingFlags.NonPublic | BindingFlags.Instance);
            autoCheckESField?.SetValue(_interactiveObject, false);

            // Set interact distance to 0 for pure logic test (ignores camera distance calculation)
            FieldInfo distanceField = typeof(InteractiveObject).GetField("interactionDistance",
                BindingFlags.NonPublic | BindingFlags.Instance);
            distanceField?.SetValue(_interactiveObject, 0f);

            // Invoke Awake to cache colliders
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
            _interactiveObject.OnPointerEnter(pointerData); // Enter first
            _interactiveObject.OnPointerExit(pointerData); // Then exit

            Assert.IsTrue(exitFired, "Hover Exit event should fire.");
            Assert.IsFalse(_interactiveObject.IsHovered, "InteractiveObject should report IsHovered = false.");
        }

        [Test]
        public void OnPointerClick_WhenValid_TriggersOnClick()
        {
            bool clicked = false;
            _interactiveObject.onClick.AddListener(() => clicked = true);

            // Mock hasCurrentMouseHit to bypass camera raycast logic
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

            // Mock hasCurrentMouseHit to bypass camera raycast logic
            FieldInfo hasMouseHitField = typeof(InteractiveObject).GetField("hasCurrentMouseHit",
                BindingFlags.NonPublic | BindingFlags.Instance);
            hasMouseHitField?.SetValue(_interactiveObject, true);

            var pointerData = new PointerEventData(_eventSystem);
            pointerData.button = PointerEventData.InputButton.Right;

            _interactiveObject.OnPointerClick(pointerData);

            Assert.IsTrue(rightClicked, "Right click event should fire.");
        }
    }
}
