using System.Reflection;
using Neo.Tools;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;
#if MIRROR
using Mirror;
#endif

namespace Neo.Editor.Tests
{
    /// <summary>
    ///     Covers the audit fix for InteractiveObject: the mouse target raycast feeds click/press
    ///     handling, so clicks must keep working with hover detection disabled while hover events
    ///     stay gated behind useHoverDetection.
    /// </summary>
    [TestFixture]
    public class AuditFixesInteractiveObjectTests
    {
        private GameObject _cameraGo;
        private GameObject _targetGo;
        private InteractiveObject _interactive;

        [SetUp]
        public void SetUp()
        {
            _cameraGo = new GameObject("AuditFixesCamera");
            _cameraGo.tag = "MainCamera";
            _cameraGo.AddComponent<Camera>();
            _cameraGo.transform.position = new Vector3(0f, 0f, -10f);
            _cameraGo.transform.rotation = Quaternion.identity;

#if MIRROR
            _targetGo = new GameObject("AuditFixesInteractive", typeof(NetworkIdentity));
#else
            _targetGo = new GameObject("AuditFixesInteractive");
#endif
            _targetGo.transform.position = new Vector3(0f, 0f, 10f);

            // WHY: an oversized collider keeps the screen-center ray on target for any game view size.
            BoxCollider box = _targetGo.AddComponent<BoxCollider>();
            box.size = new Vector3(1000f, 1000f, 1f);
            Physics.SyncTransforms();

            _interactive = _targetGo.AddComponent<InteractiveObject>();
            _interactive.onClick = new UnityEvent();
            _interactive.onHoverEnter = new UnityEvent();
            _interactive.onHoverExit = new UnityEvent();

            // WHY: EditMode has no EventSystem/raycaster setup and no lifecycle, so bypass the
            // scene bootstrap and drive Update manually with a screen-center ray.
            SetPrivateField(_interactive, "_autoCheckEventSystem", false);
            SetPrivateField(_interactive, "_autoCreateEventSystemIfMissing", false);
            SetPrivateField(_interactive, "interactionDistance", 0f);
            _interactive.UseMouseInteraction = true;
            _interactive.UseKeyboardInteraction = false;
            _interactive.UseScreenCenterRay = true;
        }

        [TearDown]
        public void TearDown()
        {
            if (_targetGo != null)
            {
                Object.DestroyImmediate(_targetGo);
            }

            if (_cameraGo != null)
            {
                Object.DestroyImmediate(_cameraGo);
            }
        }

        [Test]
        public void MouseTargetRaycast_RunsWithHoverDetectionOff()
        {
            _interactive.UseHoverDetection = false;

            InvokePrivate(_interactive, "Update");

            Assert.IsTrue(GetPrivateField<bool>(_interactive, "hasCurrentMouseHit"),
                "the mouse target raycast must run for click handling even when hover detection is off");
        }

        [Test]
        public void PointerClick_FiresOnClick_WithHoverDetectionOff()
        {
            _interactive.UseHoverDetection = false;
            bool clicked = false;
            _interactive.onClick.AddListener(() => clicked = true);

            InvokePrivate(_interactive, "Update");
            _interactive.OnPointerClick(new PointerEventData(EventSystem.current)
            {
                button = PointerEventData.InputButton.Left
            });

            Assert.IsTrue(clicked,
                "mouse interaction must be independent of hover detection, as the inspector tooltip promises");
        }

        [Test]
        public void HoverEvents_StayGated_WhenHoverDetectionIsOff()
        {
            _interactive.UseHoverDetection = false;
            bool hovered = false;
            _interactive.onHoverEnter.AddListener(() => hovered = true);

            InvokePrivate(_interactive, "Update");

            Assert.IsFalse(hovered, "hover enter must not fire while hover detection is disabled");
            Assert.IsFalse(_interactive.IsHovered, "IsHovered must stay false while hover detection is disabled");
        }

        [Test]
        public void HoverDetectionOn_StillEntersHoverExactlyOnce()
        {
            _interactive.UseHoverDetection = true;
            int hoverEnters = 0;
            _interactive.onHoverEnter.AddListener(() => hoverEnters++);

            InvokePrivate(_interactive, "Update");
            InvokePrivate(_interactive, "Update");

            Assert.IsTrue(_interactive.IsHovered, "hover detection must still report hover over the collider");
            Assert.AreEqual(1, hoverEnters, "hover enter must fire once while the pointer stays on the object");
        }

        private static void SetPrivateField(object target, string fieldName, object value)
        {
            FieldInfo field = target.GetType().GetField(fieldName,
                BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.That(field, Is.Not.Null, fieldName);
            field.SetValue(target, value);
        }

        private static T GetPrivateField<T>(object target, string fieldName)
        {
            FieldInfo field = target.GetType().GetField(fieldName,
                BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.That(field, Is.Not.Null, fieldName);
            return (T)field.GetValue(target);
        }

        private static void InvokePrivate(object target, string methodName)
        {
            MethodInfo method = target.GetType().GetMethod(methodName,
                BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.That(method, Is.Not.Null, methodName);
            method.Invoke(target, null);
        }
    }
}
