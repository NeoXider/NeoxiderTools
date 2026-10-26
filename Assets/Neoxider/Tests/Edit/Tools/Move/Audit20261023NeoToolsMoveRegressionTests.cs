using System.Collections.Generic;
using System.Reflection;
using CMF;
using Neo.Tools;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.Events;

namespace Neo.Editor.Tests
{
    public sealed class Audit20261023NeoToolsMoveRegressionTests
    {
        private readonly List<GameObject> _objects = new List<GameObject>();
        private Random.State _randomState;

        [SetUp]
        public void SetUp()
        {
            _randomState = Random.state;
        }

        [TearDown]
        public void TearDown()
        {
            try
            {
                for (int i = _objects.Count - 1; i >= 0; i--)
                {
                    Object.DestroyImmediate(_objects[i]);
                }
            }
            finally
            {
                _objects.Clear();
                Random.state = _randomState;
            }
        }

        [TestCase(KeyboardMover.MovementPlane.XY, 6f, 8f, 30f)]
        [TestCase(KeyboardMover.MovementPlane.XZ, 6f, 20f, 8f)]
        [TestCase(KeyboardMover.MovementPlane.YZ, 10f, 6f, 8f)]
        public void KeyboardTarget_UsesSelectedPlaneAndPreservesNormal(
            KeyboardMover.MovementPlane plane, float x, float y, float z)
        {
            KeyboardMover mover = CreateObject().AddComponent<KeyboardMover>();
            SetField(mover, "movementPlane", plane);
            mover.transform.position = new Vector3(10f, 20f, 30f);

            mover.MoveToPoint(new Vector2(6f, 8f));

            AssertPosition(mover.transform, new Vector3(x, y, z));
        }

        [TestCase(KeyboardMover.MovementPlane.XY, 1f, 2f, 0f)]
        [TestCase(KeyboardMover.MovementPlane.XZ, 1f, 0f, 2f)]
        [TestCase(KeyboardMover.MovementPlane.YZ, 0f, 1f, 2f)]
        public void KeyboardDelta_UsesSelectedPlane(
            KeyboardMover.MovementPlane plane, float x, float y, float z)
        {
            KeyboardMover mover = CreateObject().AddComponent<KeyboardMover>();
            SetField(mover, "movementPlane", plane);

            mover.MoveDelta(new Vector2(1f, 2f));

            AssertPosition(mover.transform, new Vector3(x, y, z));
        }

        [Test]
        public void KeyboardSmallStep_MovesAndEmitsOneStartAndStop()
        {
            KeyboardMover mover = CreateObject().AddComponent<KeyboardMover>();
            int starts = 0;
            int stops = 0;
            mover.OnMoveStart = new UnityEvent();
            mover.OnMoveStop = new UnityEvent();
            mover.OnMoveStart.AddListener(() => starts++);
            mover.OnMoveStop.AddListener(() => stops++);

            Invoke(mover, "ApplyDelta", new Vector3(0.001f, 0f, 0f));
            Invoke(mover, "ApplyDelta", new Vector3(0.001f, 0f, 0f));
            Assert.That(mover.IsMoving, Is.True);
            Invoke(mover, "ApplyDelta", Vector3.zero);

            AssertPosition(mover.transform, new Vector3(0.002f, 0f, 0f));
            Assert.That(mover.IsMoving, Is.False);
            Assert.That(starts, Is.EqualTo(1));
            Assert.That(stops, Is.EqualTo(1));
        }

        [TestCase(MouseMover2D.AxisMask.XY, 6f, 8f, 30f)]
        [TestCase(MouseMover2D.AxisMask.XZ, 6f, 20f, 8f)]
        [TestCase(MouseMover2D.AxisMask.YZ, 10f, 6f, 8f)]
        [TestCase(MouseMover2D.AxisMask.X, 6f, 20f, 30f)]
        [TestCase(MouseMover2D.AxisMask.Y, 10f, 8f, 30f)]
        [TestCase(MouseMover2D.AxisMask.Z, 10f, 20f, 8f)]
        public void Mouse2DTarget_UsesPlaneCoordinatesWithoutAccumulatingError(
            MouseMover2D.AxisMask axes, float x, float y, float z)
        {
            MouseMover2D mover = CreateObject().AddComponent<MouseMover2D>();
            SetField(mover, "axes", axes);
            mover.transform.position = new Vector3(10f, 20f, 30f);

            mover.MoveToPoint(new Vector2(6f, 8f));
            mover.MoveToPoint(new Vector2(6f, 8f));

            AssertPosition(mover.transform, new Vector3(x, y, z));
        }

        [Test]
        public void Mouse2DPointMode_ApproachesTargetInXZCoordinates()
        {
            MouseMover2D mover = CreateObject().AddComponent<MouseMover2D>();
            SetField(mover, "axes", MouseMover2D.AxisMask.XZ);
            SetField(mover, "mode", MouseMover2D.MoveMode.ClickToPoint);
            SetField(mover, "targetPoint", new Vector2(6f, 8f));
            SetField(mover, "hasTarget", true);
            SetField(mover, "speed", 100f / Time.fixedDeltaTime);
            mover.transform.position = new Vector3(10f, 20f, 30f);

            Invoke(mover, "FixedUpdate");
            Invoke(mover, "FixedUpdate");

            AssertPosition(mover.transform, new Vector3(6f, 20f, 8f));
            Assert.That(mover.IsMoving, Is.False);
        }

        [TestCase(MouseMover2D.AxisMask.XY, 6f, 8f, 3f, 0f, 0f, -10f, 0f, 0f)]
        [TestCase(MouseMover2D.AxisMask.XZ, 6f, 2f, 8f, 0f, 20f, 0f, 90f, 0f)]
        [TestCase(MouseMover2D.AxisMask.YZ, 1f, 6f, 8f, -10f, 0f, 0f, 0f, 90f)]
        public void Mouse2DProjection_IntersectsPerspectiveCameraRayWithMovementPlane(
            MouseMover2D.AxisMask axes, float x, float y, float z,
            float cameraX, float cameraY, float cameraZ, float pitch, float yaw)
        {
            Camera camera = CreateObject().AddComponent<Camera>();
            camera.pixelRect = new Rect(0f, 0f, 640f, 480f);
            camera.transform.SetPositionAndRotation(new Vector3(cameraX, cameraY, cameraZ),
                Quaternion.Euler(pitch, yaw, 0f));
            MouseMover2D mover = CreateObject().AddComponent<MouseMover2D>();
            mover.transform.position = new Vector3(1f, 2f, 3f);
            SetField(mover, "axes", axes);
            SetField(mover, "cam", camera);
            Vector3 expected = new Vector3(x, y, z);
            MethodInfo projection = typeof(MouseMover2D).GetMethod("ScreenToWorld",
                BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.That(projection, Is.Not.Null);
            Vector2 target = (Vector2)projection.Invoke(mover,
                new object[] { camera.WorldToScreenPoint(expected) });

            mover.MoveToPoint(target);

            Assert.That(Vector3.Distance(mover.transform.position, expected), Is.LessThan(1e-4f));
        }

        [TestCase(false)]
        [TestCase(true)]
        public void MouseDeltaMode_AppliesSmallPhysicsSteps(bool threeD)
        {
            GameObject owner = CreateObject();
            if (threeD)
            {
                MouseMover3D mover = owner.AddComponent<MouseMover3D>();
                SetField(mover, "desiredVel", new Vector3(0.1f, 0f, 0.2f));
                Invoke(mover, "FixedUpdate");
                Assert.That(mover.IsMoving, Is.True);
                AssertPosition(owner.transform, new Vector3(0.1f, 0f, 0.2f) * Time.fixedDeltaTime);
            }
            else
            {
                MouseMover2D mover = owner.AddComponent<MouseMover2D>();
                SetField(mover, "desiredVelocity", new Vector2(0.1f, 0.2f));
                Invoke(mover, "FixedUpdate");
                Assert.That(mover.IsMoving, Is.True);
                AssertPosition(owner.transform, new Vector3(0.1f, 0.2f, 0f) * Time.fixedDeltaTime);
            }
        }

        [Test]
        public void FreeFly_MinimumSpeedStillMovesAt240Hz()
        {
            FreeFlyCameraController controller = CreateObject().AddComponent<FreeFlyCameraController>();
            SetField(controller, "_lockCursorWhileLooking", false);
            SetField(controller, "_allowMouseWheelSpeed", false);
            SetField(controller, "_fastKey", KeyCode.None);
            SetField(controller, "_slowKey", KeyCode.None);
            controller.SetRequireLookButton(false);
            controller.SetMoveOnlyWhileLooking(false);
            controller.SetBaseSpeed(0.05f);
            controller.SetExternalMoveInput(Vector3.forward);
            controller.SetExternalLookInput(Vector2.zero);

            controller.Tick(1f / 240f);

            Assert.That(controller.transform.position.z, Is.EqualTo(0.05f / 240f).Within(1e-7f));
            Assert.That(controller.IsFlying, Is.True);
        }

        [Test]
        public void SprintDisable_RestoresWalkSpeedAndResetsEventsForNextCycle()
        {
            GameObject owner = CreateObject();
            NeoCharacterInput input = owner.AddComponent<NeoCharacterInput>();
            AdvancedWalkerController walker = owner.AddComponent<AdvancedWalkerController>();
            walker.movementSpeed = 4f;
            NeoCharacterSprint sprint = owner.AddComponent<NeoCharacterSprint>();
            SetField(sprint, "_requireGrounded", false);
            Invoke(sprint, "Awake");
            int starts = 0;
            int stops = 0;
            UnityEvent started = new UnityEvent();
            UnityEvent stopped = new UnityEvent();
            started.AddListener(() => starts++);
            stopped.AddListener(() => stops++);
            SetField(sprint, "_onSprintStart", started);
            SetField(sprint, "_onSprintStop", stopped);
            input.SetMoveInput(Vector2.up);
            input.SetRunInput(true);

            Invoke(sprint, "Update");
            Assert.That(walker.movementSpeed, Is.GreaterThan(4f));
            Invoke(sprint, "OnDisable");
            Invoke(sprint, "OnDisable");

            Assert.That(walker.movementSpeed, Is.EqualTo(4f));
            Assert.That(sprint.IsSprinting, Is.False);
            Assert.That(stops, Is.EqualTo(1));
            Invoke(sprint, "Update");
            Assert.That(starts, Is.EqualTo(2));
            Assert.That(sprint.IsSprinting, Is.True);
        }

        [Test]
        public void SetWalkSpeed_AppliesImmediatelyWhenNotSprinting()
        {
            GameObject owner = CreateObject();
            owner.AddComponent<NeoCharacterInput>();
            AdvancedWalkerController walker = owner.AddComponent<AdvancedWalkerController>();
            NeoCharacterSprint sprint = owner.AddComponent<NeoCharacterSprint>();
            Invoke(sprint, "Awake");

            sprint.SetWalkSpeed(2f);

            Assert.That(walker.movementSpeed, Is.EqualTo(2f));
            Assert.That(sprint.WalkSpeed, Is.EqualTo(2f));
        }

        [TestCase(false)]
        [TestCase(true)]
        public void Rotator2D_AimsInWorldSpaceUnderRotatedParent(bool instant)
        {
            GameObject parent = CreateObject();
            parent.transform.rotation = Quaternion.Euler(0f, 0f, 90f);
            GameObject child = CreateObject();
            child.transform.SetParent(parent.transform, false);
            UniversalRotator rotator = child.AddComponent<UniversalRotator>();
            SetField(rotator, "rotationMode", UniversalRotator.RotationMode.Mode2D);
            Invoke(rotator, "Awake");

            rotator.RotateTo(child.transform.position + Vector3.right, instant);
            if (!instant)
            {
                Invoke(rotator, "Tick", 1f);
            }

            Assert.That(Vector3.Dot(child.transform.right, Vector3.right), Is.GreaterThan(0.9999f));
        }

        [Test]
        public void RotatorLimits_UseCurrentParentAfterReparenting()
        {
            GameObject originalParent = CreateObject();
            originalParent.transform.rotation = Quaternion.Euler(0f, 30f, 0f);
            GameObject child = CreateObject();
            child.transform.SetParent(originalParent.transform, false);
            UniversalRotator rotator = child.AddComponent<UniversalRotator>();
            Invoke(rotator, "Awake");
            GameObject newParent = CreateObject();
            newParent.transform.rotation = Quaternion.Euler(0f, 90f, 0f);
            child.transform.SetParent(newParent.transform, false);
            rotator.limitRange = new Vector2(-10f, 10f);

            rotator.RotateTo(child.transform.position + Vector3.forward, true);

            Assert.That(Mathf.DeltaAngle(0f, child.transform.localEulerAngles.y), Is.EqualTo(-10f).Within(0.01f));
            Assert.That(Mathf.DeltaAngle(0f, child.transform.eulerAngles.y), Is.EqualTo(80f).Within(0.01f));
        }

        [Test]
        public void AxisConstraintChange_RecalculatesDepthWithoutAutomaticBoundsUpdates()
        {
            GameObject cameraObject = CreateObject();
            Camera camera = cameraObject.AddComponent<Camera>();
            camera.orthographic = true;
            camera.orthographicSize = 1f;
            camera.aspect = 1f;
            CameraConstraint constraint = cameraObject.AddComponent<CameraConstraint>();
            SetField(constraint, "autoUpdateBounds", false);
            BoxCollider bounds = CreateObject().AddComponent<BoxCollider>();
            bounds.center = new Vector3(0f, 0f, 10f);
            bounds.size = new Vector3(20f, 20f, 10f);
            Physics.SyncTransforms();
            constraint.SetBoundsSource(CameraConstraint.BoundsType.BoxCollider, bounds);

            constraint.SetAxisConstraint(true, true, true);
            constraint.GetConstraintBounds(out Vector3 min, out Vector3 max);
            cameraObject.transform.position = new Vector3(0f, 0f, 20f);
            Invoke(constraint, "LateUpdate");

            Assert.That(min.z, Is.EqualTo(5f));
            Assert.That(max.z, Is.EqualTo(15f));
            Assert.That(cameraObject.transform.position.z, Is.EqualTo(15f));
        }

        [Test]
        public void Controller2D_RejectsOwnCollidersAndStillFindsGroundBehindThem()
        {
            GameObject owner = CreateObject();
            owner.transform.position = new Vector3(2345f, 3456f, 0f);
            owner.AddComponent<BoxCollider2D>();
            GameObject child = CreateObject();
            child.transform.SetParent(owner.transform, false);
            child.AddComponent<CircleCollider2D>();
            PlayerController2DPhysics controller = owner.AddComponent<PlayerController2DPhysics>();
            SetField(controller, "_groundCheck", owner.transform);
            Invoke(controller, "Awake");
            Physics2D.SyncTransforms();

            Invoke(controller, "UpdateGroundState");
            Assert.That(controller.IsGrounded, Is.False, "Self and child colliders are not ground.");

            GameObject ground = CreateObject();
            ground.transform.position = owner.transform.position;
            ground.AddComponent<BoxCollider2D>();
            Physics2D.SyncTransforms();
            Invoke(controller, "UpdateGroundState");
            Assert.That(controller.IsGrounded, Is.True, "A foreign collider must survive self filtering.");
        }

        [Test]
        public void Controller3D_QuarterStickProducesQuarterSpeedAndPreservesVerticalVelocity()
        {
            GameObject owner = CreateObject();
            PlayerController3DPhysics controller = owner.AddComponent<PlayerController3DPhysics>();
            SetField(controller, "_groundCheck", owner.transform);
            Invoke(controller, "Awake");
            controller.CursorControlEnabled = false;
            controller.SetLookEnabled(false);
            controller.SetJumpEnabled(false);
            controller.SetMoveInput(new Vector2(0f, 0.25f));
            SetField(controller, "_walkSpeed", 8f);
            SetField(controller, "_airAcceleration", 1000f);
            Rigidbody body = owner.GetComponent<Rigidbody>();
            body.linearVelocity = new Vector3(0f, 3f, 0f);

            Invoke(controller, "CaptureInput");
            Invoke(controller, "HandleMovement", 1f);

            Assert.That(body.linearVelocity.z, Is.EqualTo(2f).Within(1e-5f));
            Assert.That(body.linearVelocity.y, Is.EqualTo(3f).Within(1e-5f));
        }

        [Test]
        public void ForceApplier_WorldForwardDoesNotRotateWithOwner()
        {
            GameObject owner = CreateObject();
            Rigidbody body = owner.AddComponent<Rigidbody>();
            owner.transform.rotation = Quaternion.Euler(0f, 90f, 0f);
            AdvancedForceApplier applier = owner.AddComponent<AdvancedForceApplier>();
            applier.forceMode3D = ForceMode.Force;
            applier.directionMode = AdvancedForceApplier.DirectionMode.TransformForward;
            applier.useLocalForward = false;
            Invoke(applier, "Awake");

            applier.ApplyForce(4f);

            Assert.That(Vector3.Distance(body.GetAccumulatedForce(), Vector3.forward * 4f), Is.LessThan(1e-5f));
        }

        [Test]
        public void ForceApplier_StartUsesConfiguredRandomRange()
        {
            GameObject owner = CreateObject();
            Rigidbody body = owner.AddComponent<Rigidbody>();
            AdvancedForceApplier applier = owner.AddComponent<AdvancedForceApplier>();
            applier.forceMode3D = ForceMode.Force;
            applier.randomizeForce = true;
            applier.forceRange = new Vector2(7f, 7f);
            SetField(applier, "defaultForce", 100f);
            Invoke(applier, "Awake");

            Invoke(applier, "Start");

            Assert.That(body.GetAccumulatedForce().magnitude, Is.EqualTo(7f).Within(1e-5f));
        }

        private GameObject CreateObject()
        {
            GameObject owner = new GameObject("Audit20261023NeoToolsMove");
            _objects.Add(owner);
            return owner;
        }

        private static void SetField(object target, string name, object value)
        {
            FieldInfo field = target.GetType().GetField(name, BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.That(field, Is.Not.Null, name);
            field.SetValue(target, value);
        }

        private static void Invoke(object target, string name, params object[] arguments)
        {
            MethodInfo method = target.GetType().GetMethod(name, BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.That(method, Is.Not.Null, name);
            method.Invoke(target, arguments);
        }

        private static void AssertPosition(Transform target, Vector3 expected)
        {
            Assert.That(Vector3.Distance(target.position, expected), Is.LessThan(1e-5f));
        }
    }
}
