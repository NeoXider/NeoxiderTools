using System.Reflection;
using Neo.Tools;
using NUnit.Framework;
using UnityEngine;

namespace Neo.Editor.Tests.Tools
{
    /// <summary>
    ///     The key and the mouse must reach the same objects under one set of serialized settings; any
    ///     difference has to come from a flag the designer can see, not from a constant baked into one
    ///     branch. The keyboard look ray used to pass a hardcoded <c>true</c> both for "foreign triggers
    ///     block" and for "require a clear path", so a pickup standing inside a door's trigger volume was
    ///     clickable with the mouse while the key silently did nothing - and turning
    ///     <c>checkObstacles</c> off changed the mouse but not the key.
    /// </summary>
    [TestFixture]
    public sealed class InteractiveObjectAimSymmetryTests
    {
        // WHY: The fixture builds its scene far from the world origin, so a collider leaked by another
        // fixture cannot land on the ray this one shoots.
        private static readonly Vector3 EyePosition = new Vector3(500f, 500f, 0f);

        private const float TargetDistance = 5f;
        private const float BlockerDistance = 2f;

        private GameObject _cameraObject;
        private GameObject _targetObject;
        private GameObject _blockerObject;
        private Camera _camera;
        private RenderTexture _cameraTexture;
        private InteractiveObject _interactiveObject;

        [SetUp]
        public void SetUp()
        {
            _cameraObject = new GameObject("AimSymmetryEye");
            _cameraObject.tag = "MainCamera";
            _cameraObject.transform.SetPositionAndRotation(EyePosition, Quaternion.identity);
            _camera = _cameraObject.AddComponent<Camera>();

            // WHY: pixelWidth / pixelHeight drive the screen-center ray. A render target pins them to a
            // known size instead of whatever the Game View happens to be while the suite runs.
            _cameraTexture = new RenderTexture(256, 256, 0);
            _camera.targetTexture = _cameraTexture;

            _targetObject = new GameObject("AimSymmetryTarget");
            _targetObject.transform.position = EyePosition + Vector3.forward * TargetDistance;
            _targetObject.AddComponent<BoxCollider>();
            _targetObject.AddComponent<Mirror.NetworkIdentity>();
            _interactiveObject = _targetObject.AddComponent<InteractiveObject>();

            SetPrivateField("_autoCheckEventSystem", false);
            SetPrivateField("_autoCreateEventSystemIfMissing", false);

            // WHY: Distance rules are not what this fixture measures - only the aiming rays are.
            SetPrivateField("interactionDistance", 0f);

            // WHY: Both rays must start from the same place and point the same way, otherwise the
            // comparison measures the ray sources rather than the settings. An explicit view check point
            // also keeps the keyboard ray off Camera.main, which a neighbouring fixture may own.
            SetPrivateField("viewCheckPoint", _cameraObject.transform);
            SetPrivateField("useScreenCenterRay", true);
        }

        [TearDown]
        public void TearDown()
        {
            DestroyIfPresent(_blockerObject);
            DestroyIfPresent(_targetObject);
            DestroyIfPresent(_cameraObject);
            _blockerObject = null;
            _targetObject = null;
            _cameraObject = null;

            if (_cameraTexture != null)
            {
                _cameraTexture.Release();
                Object.DestroyImmediate(_cameraTexture);
                _cameraTexture = null;
            }
        }

        [Test]
        public void ForeignTrigger_WithObstacleChecksOff_KeyReachesExactlyLikeMouse()
        {
            SpawnBlocker(true);

            EvaluateAim(false, out bool keyboardReaches, out bool mouseReaches);

            Assert.That(mouseReaches, Is.True,
                "Mouse aim should reach a target behind a foreign trigger volume with checkObstacles off.");
            Assert.That(keyboardReaches, Is.EqualTo(mouseReaches),
                "Key and mouse must agree: a foreign trigger volume is not a wall for either of them.");
        }

        [Test]
        public void ForeignTrigger_WithObstacleChecksOn_KeyReachesExactlyLikeMouse()
        {
            SpawnBlocker(true);

            EvaluateAim(true, out bool keyboardReaches, out bool mouseReaches);

            Assert.That(mouseReaches, Is.True,
                "Line-of-sight blocks on solid geometry, not on trigger volumes.");
            Assert.That(keyboardReaches, Is.EqualTo(mouseReaches),
                "Key and mouse must agree on which colliders block the aim.");
        }

        [Test]
        public void SolidWall_WithObstacleChecksOff_KeyReachesExactlyLikeMouse()
        {
            SpawnBlocker(false);

            EvaluateAim(false, out bool keyboardReaches, out bool mouseReaches);

            Assert.That(mouseReaches, Is.True,
                "With checkObstacles off the mouse accepts a hit on this object even behind solid geometry.");
            Assert.That(keyboardReaches, Is.EqualTo(mouseReaches),
                "Obstacle checking on the keyboard look ray must follow checkObstacles, not a constant.");
        }

        [Test]
        public void SolidWall_WithObstacleChecksOn_BlocksKeyAndMouseAlike()
        {
            SpawnBlocker(false);

            EvaluateAim(true, out bool keyboardReaches, out bool mouseReaches);

            Assert.That(mouseReaches, Is.False,
                "A solid wall in front of the object must block the mouse when checkObstacles is on.");
            Assert.That(keyboardReaches, Is.EqualTo(mouseReaches),
                "The symmetry fix must not turn obstacle checking off - a wall still blocks the key.");
        }

        [Test]
        public void ClearLineOfSight_ReachesWithEitherInput_UnderBothObstacleSettings()
        {
            EvaluateAim(true, out bool keyboardWithChecks, out bool mouseWithChecks);
            EvaluateAim(false, out bool keyboardWithoutChecks, out bool mouseWithoutChecks);

            Assert.That(mouseWithChecks, Is.True);
            Assert.That(keyboardWithChecks, Is.EqualTo(mouseWithChecks));
            Assert.That(mouseWithoutChecks, Is.True);
            Assert.That(keyboardWithoutChecks, Is.EqualTo(mouseWithoutChecks));
        }

        private void SpawnBlocker(bool isTrigger)
        {
            _blockerObject = new GameObject("AimSymmetryBlocker");
            _blockerObject.transform.position = EyePosition + Vector3.forward * BlockerDistance;
            _blockerObject.transform.localScale = new Vector3(4f, 4f, 1f);
            BoxCollider blocker = _blockerObject.AddComponent<BoxCollider>();
            blocker.isTrigger = isTrigger;
        }

        private void EvaluateAim(bool checkObstacles, out bool keyboardReaches, out bool mouseReaches)
        {
            SetPrivateField("checkObstacles", checkObstacles);

            // WHY: Physics.autoSyncTransforms is off by default, so freshly positioned colliders are not
            // visible to queries until the transforms are pushed into the physics scene.
            Physics.SyncTransforms();

            // WHY: The mouse path reads the cached collider without resolving it; the component normally
            // caches in Awake, which an EditMode fixture never gets.
            InvokeInstanceMethod("RefreshCachedReferences", null);

            keyboardReaches = (bool)InvokeInstanceMethod("IsInViewForKeyboardInteraction", null);

            object[] mouseArguments = { _camera, null };
            mouseReaches = (bool)InvokeInstanceMethod("TryGetCurrentMouseTargetHit", mouseArguments);
        }

        private object InvokeInstanceMethod(string methodName, object[] arguments)
        {
            MethodInfo method = typeof(InteractiveObject).GetMethod(methodName,
                BindingFlags.NonPublic | BindingFlags.Instance);

            Assert.That(method, Is.Not.Null,
                $"InteractiveObject.{methodName} was renamed - this symmetry guard no longer measures anything.");

            return method.Invoke(_interactiveObject, arguments);
        }

        private void SetPrivateField(string fieldName, object value)
        {
            FieldInfo field = typeof(InteractiveObject).GetField(fieldName,
                BindingFlags.NonPublic | BindingFlags.Instance);

            Assert.That(field, Is.Not.Null,
                $"InteractiveObject.{fieldName} was renamed - this symmetry guard no longer measures anything.");

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
