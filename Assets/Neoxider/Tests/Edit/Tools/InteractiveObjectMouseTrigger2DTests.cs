using System.Reflection;
using Neo.Tools;
using NUnit.Framework;
using UnityEngine;

namespace Neo.Editor.Tests.Tools
{
    /// <summary>
    ///     <c>includeTriggerCollidersInMouseRaycast</c> must mean the same thing in 2D as in 3D. The 3D
    ///     branch turned the flag into a <c>QueryTriggerInteraction</c>, while the 2D branch passed a
    ///     hardcoded <c>true</c> into the hit filter, so for 2D objects the checkbox did nothing at all —
    ///     trigger colliders were always kept. A switch that silently does nothing is worse than no
    ///     switch: the Inspector shows it as if it worked.
    /// </summary>
    [TestFixture]
    public sealed class InteractiveObjectMouseTrigger2DTests
    {
        // WHY: The fixture builds its scene far from the world origin, so a collider leaked by another
        // fixture cannot land on the ray this one shoots.
        private static readonly Vector3 EyePosition = new Vector3(700f, 700f, -10f);

        private GameObject _cameraObject;
        private GameObject _targetObject;
        private Camera _camera;
        private RenderTexture _cameraTexture;
        private InteractiveObject _interactiveObject;
        private bool _queriesHitTriggers;

        [SetUp]
        public void SetUp()
        {
            // WHY: In 2D the global switch is consulted before the component's own flag. Pinning it here
            // keeps the fixture measuring the component instead of whatever the project happens to set.
            _queriesHitTriggers = Physics2D.queriesHitTriggers;
            Physics2D.queriesHitTriggers = true;

            _cameraObject = new GameObject("MouseTrigger2DEye");
            _cameraObject.tag = "MainCamera";
            _cameraObject.transform.SetPositionAndRotation(EyePosition, Quaternion.identity);
            _camera = _cameraObject.AddComponent<Camera>();

            // WHY: pixelWidth / pixelHeight drive the screen-center ray. A render target pins them to a
            // known size instead of whatever the Game View happens to be while the suite runs.
            _cameraTexture = new RenderTexture(256, 256, 0);
            _camera.targetTexture = _cameraTexture;
        }

        [TearDown]
        public void TearDown()
        {
            DestroyIfPresent(_targetObject);
            DestroyIfPresent(_cameraObject);
            _targetObject = null;
            _cameraObject = null;

            if (_cameraTexture != null)
            {
                _cameraTexture.Release();
                Object.DestroyImmediate(_cameraTexture);
                _cameraTexture = null;
            }

            Physics2D.queriesHitTriggers = _queriesHitTriggers;
        }

        [Test]
        public void Trigger2D_WithFlagOn_IsReachableByMouse()
        {
            BuildTarget2D(true);

            Assert.That(EvaluateMouseAim(true), Is.True,
                "A 2D trigger collider must be hoverable while includeTriggerCollidersInMouseRaycast is on.");
        }

        [Test]
        public void Trigger2D_WithFlagOff_IsNotReachableByMouse()
        {
            BuildTarget2D(true);

            Assert.That(EvaluateMouseAim(false), Is.False,
                "The 2D mouse ray must honour includeTriggerCollidersInMouseRaycast. A hardcoded true here " +
                "made the checkbox a silent no-op for every 2D object.");
        }

        [Test]
        public void SolidCollider2D_WithFlagOff_StaysReachable()
        {
            BuildTarget2D(false);

            Assert.That(EvaluateMouseAim(false), Is.True,
                "The flag governs trigger colliders only — a solid 2D collider must stay reachable.");
        }

        [Test]
        public void Trigger3D_WithFlagOff_IsNotReachableByMouse()
        {
            BuildTarget3D(true);

            Assert.That(EvaluateMouseAim(false), Is.False,
                "3D behaviour is the reference the 2D fix aligns with and must not change.");
        }

        private void BuildTarget2D(bool isTrigger)
        {
            _targetObject = new GameObject("MouseTrigger2DTarget");
            _targetObject.transform.position = new Vector3(EyePosition.x, EyePosition.y, 0f);
            BoxCollider2D collider = _targetObject.AddComponent<BoxCollider2D>();
            collider.isTrigger = isTrigger;
            AttachInteractiveObject();
        }

        private void BuildTarget3D(bool isTrigger)
        {
            _targetObject = new GameObject("MouseTrigger3DTarget");
            _targetObject.transform.position = new Vector3(EyePosition.x, EyePosition.y, 0f);
            BoxCollider collider = _targetObject.AddComponent<BoxCollider>();
            collider.isTrigger = isTrigger;
            AttachInteractiveObject();
        }

        private void AttachInteractiveObject()
        {
            _targetObject.AddComponent<Mirror.NetworkIdentity>();
            _interactiveObject = _targetObject.AddComponent<InteractiveObject>();

            SetPrivateField("_autoCheckEventSystem", false);
            SetPrivateField("_autoCreateEventSystemIfMissing", false);

            // WHY: Distance rules are not what this fixture measures — only the mouse ray is.
            SetPrivateField("interactionDistance", 0f);
            SetPrivateField("checkObstacles", false);
            SetPrivateField("viewCheckPoint", _cameraObject.transform);
            SetPrivateField("useScreenCenterRay", true);
        }

        private bool EvaluateMouseAim(bool includeTriggers)
        {
            SetPrivateField("includeTriggerCollidersInMouseRaycast", includeTriggers);

            // WHY: autoSyncTransforms is off by default, so freshly positioned colliders are not visible
            // to queries until the transforms are pushed into the physics scenes.
            Physics.SyncTransforms();
            Physics2D.SyncTransforms();

            // WHY: The mouse path reads the cached collider without resolving it; the component normally
            // caches in Awake, which an EditMode fixture never gets.
            InvokeInstanceMethod("RefreshCachedReferences", null);

            object[] mouseArguments = { _camera, null };
            return (bool)InvokeInstanceMethod("TryGetCurrentMouseTargetHit", mouseArguments);
        }

        private object InvokeInstanceMethod(string methodName, object[] arguments)
        {
            MethodInfo method = typeof(InteractiveObject).GetMethod(methodName,
                BindingFlags.NonPublic | BindingFlags.Instance);

            Assert.That(method, Is.Not.Null,
                $"InteractiveObject.{methodName} was renamed - this guard no longer measures anything.");

            return method.Invoke(_interactiveObject, arguments);
        }

        private void SetPrivateField(string fieldName, object value)
        {
            FieldInfo field = typeof(InteractiveObject).GetField(fieldName,
                BindingFlags.NonPublic | BindingFlags.Instance);

            Assert.That(field, Is.Not.Null,
                $"InteractiveObject.{fieldName} was renamed - this guard no longer measures anything.");

            field.SetValue(_interactiveObject, value);
        }

        private static void DestroyIfPresent(GameObject target)
        {
            if (target != null)
            {
                Object.DestroyImmediate(target);
            }
        }
    }
}
