using System;
using System.Collections.Generic;
using System.Reflection;
using Neo.Extensions;
using Neo.Settings;
using Neo.Tests.Performance;
using Neo.Tools;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using UnityEngine.Events;
using Object = UnityEngine.Object;

namespace Neo.Editor.Tests
{
    /// <summary>
    ///     Regressions for the 2026-11-02 Tools/Move quality pass: follow limits and listener safety, lost distance
    ///     targets, post-step speed clamping, symmetric subscriptions and single-write screen positioning; plus the
    ///     closure-wave fixes: 3D follow sectors, the degenerate 2D aim, full-turn rotator ranges, camera constraint
    ///     source loss and edge reporting, and the allocation/no-write claims of the idle follow paths; plus the shared
    ///     <see cref="AngleSector" /> rules, the aligned-follower rotation guard and Inspector-side bounds recovery.
    /// </summary>
    public sealed class Audit20261102NeoToolsMoveQualityTests
    {
        private readonly List<Object> _objects = new();

        [TearDown]
        public void TearDown()
        {
            for (int i = _objects.Count - 1; i >= 0; i--)
            {
                if (_objects[i] != null)
                {
                    Object.DestroyImmediate(_objects[i]);
                }
            }

            _objects.Clear();
        }

        [TestCase(0.01f)]
        [TestCase(-0.01f)]
        public void Follow2D_RotationLimit_StaysAtTheNearerEdgeAcrossTheNegativeXAxis(float targetY)
        {
            Follow follow = CreateObject("Follower").AddComponent<Follow>();
            SerializedObject settings = new(follow);
            settings.FindProperty("followMode").enumValueIndex = (int)Follow.FollowMode.TwoD;
            settings.FindProperty("followPosition").boolValue = false;
            settings.FindProperty("rotationSmoothMode").enumValueIndex = (int)Follow.SmoothMode.None;
            settings.FindProperty("rotationLimitZ.enabled").boolValue = true;
            settings.FindProperty("rotationLimitZ.min").floatValue = -45f;
            settings.FindProperty("rotationLimitZ.max").floatValue = 45f;
            settings.ApplyModifiedPropertiesWithoutUndo();
            Transform target = CreateObject("Target").transform;
            target.position = new Vector3(-10f, targetY, 0f);
            follow.SetTarget(target);

            Invoke(follow, "LateUpdate");

            Assert.That(Mathf.Abs(Mathf.DeltaAngle(follow.transform.eulerAngles.z, 45f)), Is.LessThan(0.01f),
                "A target just below the -X axis must clamp to the same (nearer) edge as one just above it.");
        }

        /// <remarks>
        ///     <paramref name="aimDegrees" /> is the Atan2 direction of the target from the follower (0 = +X, 90 = +Y);
        ///     the 2D aim angle is that minus 90. Sectors past -180 must stay reachable and the nearer edge must win
        ///     for asymmetric sectors.
        /// </remarks>
        [TestCase(-225f, -135f, -100f, -190f, TestName = "Follow2D sector [-225,-135] keeps down-left aim")]
        [TestCase(-225f, -135f, -80f, -170f, TestName = "Follow2D sector [-225,-135] keeps down-right aim")]
        [TestCase(-225f, -135f, 0f, -135f, TestName = "Follow2D sector [-225,-135] clamps right aim to -135")]
        [TestCase(-225f, -135f, 180f, -225f, TestName = "Follow2D sector [-225,-135] clamps left aim to -225")]
        [TestCase(0f, 90f, -60f, 90f, TestName = "Follow2D sector [0,90] clamps down-left aim to 90")]
        [TestCase(0f, 90f, 0f, 0f, TestName = "Follow2D sector [0,90] clamps right aim to 0")]
        [TestCase(0f, 90f, 135f, 45f, TestName = "Follow2D sector [0,90] keeps up-left aim")]
        [TestCase(-45f, 45f, 90f, 0f, TestName = "Follow2D sector [-45,45] keeps straight-up aim")]
        public void Follow2D_RotationLimit_ClampsToTheNearerEdgeOfAnyAuthoredSector(float min, float max,
            float aimDegrees, float expectedAngle)
        {
            Follow follow = CreateObject("Follower").AddComponent<Follow>();
            SerializedObject settings = new(follow);
            settings.FindProperty("followMode").enumValueIndex = (int)Follow.FollowMode.TwoD;
            settings.FindProperty("followPosition").boolValue = false;
            settings.FindProperty("rotationSmoothMode").enumValueIndex = (int)Follow.SmoothMode.None;
            settings.FindProperty("rotationLimitZ.enabled").boolValue = true;
            settings.FindProperty("rotationLimitZ.min").floatValue = min;
            settings.FindProperty("rotationLimitZ.max").floatValue = max;
            settings.ApplyModifiedPropertiesWithoutUndo();
            Transform target = CreateObject("Target").transform;
            float radians = aimDegrees * Mathf.Deg2Rad;
            target.position = new Vector3(Mathf.Cos(radians), Mathf.Sin(radians), 0f) * 10f;
            follow.SetTarget(target);

            Invoke(follow, "LateUpdate");

            Assert.That(Mathf.Abs(Mathf.DeltaAngle(follow.transform.eulerAngles.z, expectedAngle)),
                Is.LessThan(0.01f), $"Received {follow.transform.eulerAngles.z}.");
        }

        [TestCase(-45f, 45f, -200f, 45f)]
        [TestCase(-45f, 45f, -100f, -45f)]
        [TestCase(-225f, -135f, -190f, -190f)]
        [TestCase(-225f, -135f, -90f, -135f)]
        [TestCase(0f, 90f, -150f, 90f)]
        [TestCase(0f, 90f, -90f, 0f)]
        [TestCase(-180f, 180f, 77f, 77f)]
        [TestCase(-10f, -20f, 5f, 5f, TestName = "Follow inverted [-10,-20] is the 350-degree sector through 180")]
        [TestCase(-10f, -20f, -12f, -10f, TestName = "Follow inverted [-10,-20] clamps the gap to its nearer edge")]
        [TestCase(90f, -90f, 10f, 90f, TestName = "Follow inverted [90,-90] is the rear half")]
        [TestCase(90f, -90f, -10f, -90f, TestName = "Follow inverted [90,-90] clamps a front-right aim to -90")]
        [TestCase(90f, -90f, 170f, 170f, TestName = "Follow inverted [90,-90] keeps a rear aim")]
        public void Follow_ClampAngleToSector_PicksTheNearerEdge(float min, float max, float angle, float expected)
        {
            float clamped = Follow.ClampAngleToSector(angle, min, max);

            Assert.That(Mathf.Abs(Mathf.DeltaAngle(clamped, expected)), Is.LessThan(1e-3f),
                $"Received {clamped}.");
        }

        /// <remarks>
        ///     <see cref="Follow" /> and <see cref="UniversalRotator" /> must read one authored range the same way,
        ///     including inverted ranges, ranges past 180 and full turns; only the output representation may differ.
        /// </remarks>
        [TestCase(-45f, 45f)]
        [TestCase(170f, 190f)]
        [TestCase(-225f, -135f)]
        [TestCase(90f, -90f)]
        [TestCase(300f, 60f)]
        [TestCase(-10f, -20f)]
        [TestCase(-360f, 360f)]
        public void FollowAndUniversalRotator_ReadOneLimitRangeTheSameWay(float min, float max)
        {
            for (int angle = -360; angle <= 360; angle += 5)
            {
                float follow = Follow.ClampAngleToSector(angle, min, max);
                float rotator = UniversalRotator.ClampAngleToRange(angle, min, max);

                Assert.That(Mathf.Abs(Mathf.DeltaAngle(follow, rotator)), Is.LessThan(1e-3f),
                    $"[{min}, {max}] at {angle}: Follow {follow}, UniversalRotator {rotator}.");
            }
        }

        [Test]
        public void Follow_StartListenerTeleporting_IsNotOverwrittenByTheStalePosition()
        {
            Follow follow = CreateObject("Follower").AddComponent<Follow>();
            SerializedObject settings = new(follow);
            settings.FindProperty("followRotation").boolValue = false;
            settings.FindProperty("positionSmoothMode").enumValueIndex = (int)Follow.SmoothMode.None;
            settings.ApplyModifiedPropertiesWithoutUndo();
            follow.SetDeadzoneEnabled(true);
            follow.SetDeadzoneRadius(1f);
            follow.onStartFollowing = new UnityEvent();
            follow.onStartFollowing.AddListener(follow.TeleportToTarget);
            Transform target = CreateObject("Target").transform;
            target.position = new Vector3(10f, 0f, 0f);
            follow.SetTarget(target);

            Invoke(follow, "LateUpdate");

            Assert.That(Vector3.Distance(follow.transform.position, target.position), Is.LessThan(1e-5f),
                "The deadzone step must start from the teleported position, not the pre-event one.");
        }

        [Test]
        public void Follow_StartListenerReplacingTheTarget_FollowsTheNewTargetInTheSameFrame()
        {
            Follow follow = CreateObject("Follower").AddComponent<Follow>();
            SerializedObject settings = new(follow);
            settings.FindProperty("followRotation").boolValue = false;
            settings.FindProperty("positionSmoothMode").enumValueIndex = (int)Follow.SmoothMode.None;
            settings.ApplyModifiedPropertiesWithoutUndo();
            Transform first = CreateObject("First target").transform;
            first.position = new Vector3(10f, 0f, 0f);
            Transform second = CreateObject("Second target").transform;
            second.position = new Vector3(0f, 5f, 0f);
            follow.onStartFollowing = new UnityEvent();
            follow.onStartFollowing.AddListener(() => follow.SetTarget(second));
            follow.SetTarget(first);

            Invoke(follow, "LateUpdate");

            Assert.That(Vector3.Distance(follow.transform.position, second.position), Is.LessThan(1e-5f));
        }

        [Test]
        public void Follow_TeleportToTarget_RespectsPositionLimits()
        {
            Follow follow = CreateObject("Follower").AddComponent<Follow>();
            SerializedObject settings = new(follow);
            settings.FindProperty("limitX.enabled").boolValue = true;
            settings.FindProperty("limitX.min").floatValue = -1f;
            settings.FindProperty("limitX.max").floatValue = 1f;
            settings.ApplyModifiedPropertiesWithoutUndo();
            Transform target = CreateObject("Target").transform;
            target.position = new Vector3(5f, 2f, 0f);
            follow.SetTarget(target);

            follow.TeleportToTarget();

            Assert.That(Vector3.Distance(follow.transform.position, new Vector3(1f, 2f, 0f)), Is.LessThan(1e-5f));
        }

        [Test]
        public void Follow_StartListenerClearingTheTarget_SkipsRotationAndReportsLossOnce()
        {
            Follow follow = CreateObject("Follower").AddComponent<Follow>();
            follow.onStartFollowing = new UnityEvent();
            follow.onStopFollowing = new UnityEvent();
            follow.onTargetLost = new UnityEvent();
            int lost = 0;
            follow.onStartFollowing.AddListener(() => follow.SetTarget(null));
            follow.onTargetLost.AddListener(() => lost++);
            Transform target = CreateObject("Target").transform;
            target.position = new Vector3(3f, 0f, 4f);
            follow.SetTarget(target);

            Assert.DoesNotThrow(() => Invoke(follow, "LateUpdate"));
            Invoke(follow, "LateUpdate");
            Invoke(follow, "LateUpdate");

            Assert.That(follow.GetTarget(), Is.Null);
            Assert.That(lost, Is.EqualTo(1));
        }

        [TestCase(true)]
        [TestCase(false)]
        public void DistanceChecker_LostTarget_DepartsOnceAndReportsOutside(bool destroyTarget)
        {
            DistanceChecker checker = CreateObject("Checker").AddComponent<DistanceChecker>();
            checker.onApproach = new UnityEvent();
            checker.onDepart = new UnityEvent();
            int approaches = 0;
            int departures = 0;
            checker.onApproach.AddListener(() => approaches++);
            checker.onDepart.AddListener(() => departures++);
            GameObject target = CreateObject("Target");
            target.transform.position = new Vector3(1f, 0f, 0f);
            checker.SetCurrentObject(checker.transform);
            checker.SetTarget(target.transform);
            checker.SetDistanceThreshold(5f);
            checker.ForceCheck();
            Assert.That(checker.IsWithinDistance(), Is.True);

            if (destroyTarget)
            {
                Object.DestroyImmediate(target);
            }
            else
            {
                checker.SetTarget(null);
            }

            Invoke(checker, "Update");
            checker.ForceCheck();

            Assert.That(checker.IsWithinDistance(), Is.False);
            Assert.That(approaches, Is.EqualTo(1));
            Assert.That(departures, Is.EqualTo(1), "Losing the target must depart exactly once.");
        }

        [Test]
        public void ForceApplier_ClampMaxSpeed_CapsTheVelocityOfTheNextTwoSteps()
        {
            GameObject owner = CreateObject("Force owner");
            Rigidbody body = owner.AddComponent<Rigidbody>();
            body.useGravity = false;
            AdvancedForceApplier applier = owner.AddComponent<AdvancedForceApplier>();
            applier.playOnAwake = false;
            applier.clampMaxSpeed = true;
            applier.maxSpeed = 5f;
            Invoke(applier, "Awake");

            applier.ApplyForce(100f, Vector3.right);
            // WHY: stand-in for the simulation step that integrates the queued impulse.
            body.linearVelocity = Vector3.right * 20f;
            Invoke(applier, "FixedUpdate");
            Assert.That(body.linearVelocity.magnitude, Is.EqualTo(5f).Within(1e-4f));

            body.linearVelocity = Vector3.right * 20f;
            Invoke(applier, "FixedUpdate");
            Assert.That(body.linearVelocity.magnitude, Is.EqualTo(5f).Within(1e-4f));

            body.linearVelocity = Vector3.right * 20f;
            Invoke(applier, "FixedUpdate");
            Assert.That(body.linearVelocity.magnitude, Is.EqualTo(20f).Within(1e-4f),
                "Without Clamp Every FixedUpdate the cap only covers the steps after an ApplyForce call.");
        }

        [Test]
        public void ForceApplier_WithoutClampMaxSpeed_LeavesTheIntegratedVelocity()
        {
            GameObject owner = CreateObject("Force owner");
            Rigidbody body = owner.AddComponent<Rigidbody>();
            body.useGravity = false;
            AdvancedForceApplier applier = owner.AddComponent<AdvancedForceApplier>();
            applier.playOnAwake = false;
            applier.maxSpeed = 5f;
            Invoke(applier, "Awake");

            applier.ApplyForce(100f, Vector3.right);
            body.linearVelocity = Vector3.right * 20f;
            Invoke(applier, "FixedUpdate");

            Assert.That(body.linearVelocity.magnitude, Is.EqualTo(20f).Within(1e-4f));
        }

        [Test]
        public void Controller3D_SettingsFlagChangedWhileEnabled_StillUnsubscribesOnDisable()
        {
            PlayerController3DPhysics controller =
                CreateObject("Controller").AddComponent<PlayerController3DPhysics>();
            try
            {
                Invoke(controller, "OnEnable");
                Assert.That(CountSettingsHandlers(controller), Is.EqualTo(1));

                SetField(controller, "_useGameSettingsMouseSensitivity", false);
                Invoke(controller, "OnDisable");

                Assert.That(CountSettingsHandlers(controller), Is.Zero,
                    "OnDisable must remove the handler OnEnable added, whatever the flag says now.");

                SetField(controller, "_useGameSettingsMouseSensitivity", true);
                Invoke(controller, "OnEnable");
                Invoke(controller, "OnEnable");
                Assert.That(CountSettingsHandlers(controller), Is.EqualTo(1), "Re-enabling must not double-subscribe.");
                Invoke(controller, "OnDisable");
                Assert.That(CountSettingsHandlers(controller), Is.Zero);
            }
            finally
            {
                RemoveSettingsHandlers(controller);
            }
        }

        [Test]
        public void ScreenPositioner_PreservesZAndAppliesWorldOffsetInOneUpdate()
        {
            Camera camera = CreateObject("Camera").AddComponent<Camera>();
            camera.orthographic = true;
            camera.pixelRect = new Rect(0f, 0f, 640f, 480f);
            camera.transform.position = new Vector3(0f, 0f, -10f);
            ScreenPositioner positioner = CreateObject("Positioner").AddComponent<ScreenPositioner>();
            positioner.transform.position = new Vector3(0f, 0f, 7f);
            SetField(positioner, "_targetCamera", camera);
            SetField(positioner, "_offset", new Vector2(0.5f, -0.25f));
            Vector3 edge = camera.GetWorldPositionAtScreenEdge(ScreenEdge.BottomLeft, Vector2.zero, 4f);

            positioner.Configure(ScreenEdge.BottomLeft, Vector2.zero, 4f);

            Vector3 expected = new(edge.x + 0.5f, edge.y - 0.25f, 7f);
            Assert.That(Vector3.Distance(positioner.transform.position, expected), Is.LessThan(1e-4f));
        }

        /// <remarks>
        ///     <paramref name="aimYaw" /> is the yaw of the target seen from the follower (0 = +Z, 90 = +X). The authored
        ///     sector [135, 225] faces backwards: -170 (= 190) is inside it, -100 (= 260) is nearer the 225 edge.
        /// </remarks>
        [TestCase(-170f, 190f, TestName = "Follow3D yaw sector [135,225] keeps an aim behind-left")]
        [TestCase(-100f, 225f, TestName = "Follow3D yaw sector [135,225] clamps a left aim to 225")]
        [TestCase(100f, 135f, TestName = "Follow3D yaw sector [135,225] clamps a right aim to 135")]
        [TestCase(180f, 180f, TestName = "Follow3D yaw sector [135,225] keeps a straight-back aim")]
        public void Follow3D_YawSectorPastTheBack_ClampsToTheNearerEdge(float aimYaw, float expectedYaw)
        {
            Follow follow = CreateObject("Follower").AddComponent<Follow>();
            SerializedObject settings = new(follow);
            settings.FindProperty("followPosition").boolValue = false;
            settings.FindProperty("rotationSmoothMode").enumValueIndex = (int)Follow.SmoothMode.None;
            settings.FindProperty("rotationLimitY.enabled").boolValue = true;
            settings.FindProperty("rotationLimitY.min").floatValue = 135f;
            settings.FindProperty("rotationLimitY.max").floatValue = 225f;
            settings.ApplyModifiedPropertiesWithoutUndo();
            Transform target = CreateObject("Target").transform;
            float radians = aimYaw * Mathf.Deg2Rad;
            target.position = new Vector3(Mathf.Sin(radians), 0f, Mathf.Cos(radians)) * 10f;
            follow.SetTarget(target);

            Invoke(follow, "LateUpdate");

            Assert.That(Mathf.Abs(Mathf.DeltaAngle(follow.transform.eulerAngles.y, expectedYaw)), Is.LessThan(0.01f),
                $"Received yaw {follow.transform.eulerAngles.y}.");
        }

        [Test]
        public void Follow2D_FollowerOnTopOfTheTarget_KeepsItsRotation()
        {
            Follow follow = CreateObject("Follower").AddComponent<Follow>();
            SerializedObject settings = new(follow);
            settings.FindProperty("followMode").enumValueIndex = (int)Follow.FollowMode.TwoD;
            settings.FindProperty("positionSmoothMode").enumValueIndex = (int)Follow.SmoothMode.None;
            settings.FindProperty("rotationSmoothMode").enumValueIndex = (int)Follow.SmoothMode.None;
            settings.ApplyModifiedPropertiesWithoutUndo();
            follow.transform.rotation = Quaternion.Euler(0f, 0f, 30f);
            Transform target = CreateObject("Target").transform;
            target.position = new Vector3(3f, -2f, 0f);
            follow.SetTarget(target);

            Invoke(follow, "LateUpdate");

            Assert.That(Vector3.Distance(follow.transform.position, target.position), Is.LessThan(1e-5f));
            Assert.That(Mathf.Abs(Mathf.DeltaAngle(follow.transform.eulerAngles.z, 30f)), Is.LessThan(0.01f),
                "With no direction to aim along, the follower keeps its rotation instead of snapping to -90.");
        }

        [Test]
        public void Follow_WarmLateUpdate_DoesNotAllocateAndAnIdleFollowerDoesNotRewriteItsTransform()
        {
            Follow follow = CreateObject("Follower").AddComponent<Follow>();
            Transform target = CreateObject("Target").transform;
            target.position = new Vector3(5f, 1f, 3f);
            follow.SetTarget(target);
            Action lateUpdate = CreateAction(follow, "LateUpdate");
            Action frames = () =>
            {
                for (int i = 0; i < 64; i++)
                {
                    lateUpdate();
                }
            };
            lateUpdate();

            long calls = GcAlloc.TryCount(frames, out string notAdmitted);

            follow.SetPositionSmoothMode(Follow.SmoothMode.None);
            follow.SetRotationSmoothMode(Follow.SmoothMode.None);
            lateUpdate();
            follow.transform.hasChanged = false;
            lateUpdate();

            Assert.That(follow.transform.hasChanged, Is.False,
                "A follower already at its target and facing it must not rewrite its transform.");
            GcAlloc.IgnoreIfNotAdmitted(notAdmitted);
            Assert.That(calls, Is.Zero, "A warm Follow.LateUpdate (moving and rotating) must not allocate.");
        }

        [TestCase(Follow.FollowMode.ThreeD)]
        [TestCase(Follow.FollowMode.TwoD)]
        public void Follow_AlignedFollowerWithAnOffset_StopsRewritingItsRotationButFixesExternalTurns(
            Follow.FollowMode mode)
        {
            Follow follow = CreateObject("Follower").AddComponent<Follow>();
            SerializedObject settings = new(follow);
            settings.FindProperty("followMode").enumValueIndex = (int)mode;
            settings.FindProperty("positionSmoothMode").enumValueIndex = (int)Follow.SmoothMode.None;
            settings.FindProperty("rotationSmoothMode").enumValueIndex = (int)Follow.SmoothMode.None;
            settings.FindProperty("offset").vector3Value = mode == Follow.FollowMode.ThreeD
                ? new Vector3(1f, 2f, -5f)
                : new Vector3(-3f, 2f, 0f);
            settings.ApplyModifiedPropertiesWithoutUndo();
            Transform target = CreateObject("Target").transform;
            target.position = new Vector3(4f, 1f, mode == Follow.FollowMode.ThreeD ? 3f : 0f);
            follow.SetTarget(target);
            Action lateUpdate = CreateAction(follow, "LateUpdate");
            for (int i = 0; i < 4; i++)
            {
                lateUpdate();
            }

            Quaternion aligned = follow.transform.rotation;
            follow.transform.hasChanged = false;
            lateUpdate();
            lateUpdate();

            Assert.That(follow.transform.hasChanged, Is.False,
                "An aligned follower with a non-zero offset must stop rewriting its rotation.");

            follow.transform.rotation = Quaternion.Euler(10f, 20f, 30f);
            lateUpdate();

            Assert.That(Quaternion.Angle(follow.transform.rotation, aligned), Is.LessThan(0.01f),
                "A rotation changed from outside must still be corrected on the next frame.");
        }

        [TestCase(-360f, 360f)]
        [TestCase(-180f, 180f)]
        [TestCase(-90f, 270f)]
        [TestCase(0f, 360f)]
        public void UniversalRotator_FullTurnRange_LeavesTheAimUnclamped(float min, float max)
        {
            UniversalRotator rotator = CreateObject("Rotator").AddComponent<UniversalRotator>();
            rotator.limitRange = new Vector2(min, max);
            float radians = 120f * Mathf.Deg2Rad;

            rotator.RotateTo(new Vector3(Mathf.Sin(radians), 0f, Mathf.Cos(radians)) * 5f, true);

            Assert.That(Mathf.Abs(Mathf.DeltaAngle(rotator.transform.eulerAngles.y, 120f)), Is.LessThan(0.01f),
                $"A [{min}, {max}] range covers every angle; received {rotator.transform.eulerAngles.y}.");
            Assert.That(UniversalRotator.ClampAngleToRange(-150f, min, max), Is.EqualTo(-150f).Within(1e-4f));
        }

        [TestCase(-30f, 30f, 120f, 30f)]
        [TestCase(-30f, 30f, -120f, -30f)]
        [TestCase(-30f, 30f, 12f, 12f)]
        [TestCase(170f, 190f, -175f, -175f)]
        [TestCase(170f, 190f, 90f, 170f)]
        [TestCase(0f, 90f, -100f, 0f)]
        [TestCase(0f, 90f, 170f, 90f)]
        [TestCase(0f, 90f, -170f, 90f, TestName = "UniversalRotator sector [0,90] keeps the nearer edge past 180")]
        public void UniversalRotator_PartialRange_ClampsToTheNearerSectorEdge(float min, float max, float angle,
            float expected)
        {
            float clamped = UniversalRotator.ClampAngleToRange(angle, min, max);

            Assert.That(Mathf.Abs(Mathf.DeltaAngle(clamped, expected)), Is.LessThan(1e-3f), $"Received {clamped}.");
        }

        [Test]
        public void CameraConstraint_SourceLostAtRuntime_PausesInsteadOfDisablingAndResumesOnTheNextSource()
        {
            CameraConstraint constraint = CreateConstraint(out Transform cameraTransform);
            BoxCollider first = CreateBounds("First bounds", Vector3.zero);
            constraint.SetBoundsSource(CameraConstraint.BoundsType.BoxCollider, first);
            cameraTransform.position = new Vector3(50f, 0f, -10f);
            Invoke(constraint, "LateUpdate");
            Assert.That(cameraTransform.position.x, Is.EqualTo(4f).Within(1e-4f));
            Assert.That(constraint.HasValidBounds, Is.True);

            cameraTransform.hasChanged = false;
            Invoke(constraint, "LateUpdate");
            Assert.That(cameraTransform.hasChanged, Is.False, "An in-bounds camera must not be rewritten.");

            Object.DestroyImmediate(first.gameObject);
            cameraTransform.position = new Vector3(50f, 0f, -10f);
            Invoke(constraint, "LateUpdate");

            Assert.That(constraint.enabled, Is.True, "Losing the source at runtime must not disable the component.");
            Assert.That(constraint.HasValidBounds, Is.False);
            Assert.That(constraint.IsAtEdge(out _, out _, out _, out _), Is.False);
            Assert.That(cameraTransform.position.x, Is.EqualTo(50f), "Clamping pauses while no source exists.");

            BoxCollider second = CreateBounds("Second bounds", new Vector3(100f, 0f, 0f));
            Assert.That(constraint.SetBoundsSource(CameraConstraint.BoundsType.BoxCollider, second), Is.True);
            Invoke(constraint, "LateUpdate");

            Assert.That(constraint.HasValidBounds, Is.True);
            Assert.That(cameraTransform.position.x, Is.EqualTo(96f).Within(1e-4f),
                "The next source resumes clamping without re-enabling the component.");
        }

        [Test]
        public void CameraConstraint_SourceReassignedInTheInspector_ResumesClampingWithAutoUpdateOff()
        {
            CameraConstraint constraint = CreateConstraint(out Transform cameraTransform);
            SerializedObject settings = new(constraint);
            settings.FindProperty("autoUpdateBounds").boolValue = false;
            settings.FindProperty("boundsType").enumValueIndex = (int)CameraConstraint.BoundsType.BoxCollider;
            settings.FindProperty("boxCollider").objectReferenceValue = CreateBounds("First bounds", Vector3.zero);
            settings.ApplyModifiedPropertiesWithoutUndo();
            cameraTransform.position = new Vector3(50f, 0f, -10f);
            Invoke(constraint, "LateUpdate");
            Assert.That(cameraTransform.position.x, Is.EqualTo(4f).Within(1e-4f));

            Object.DestroyImmediate(((BoxCollider)settings.FindProperty("boxCollider").objectReferenceValue).gameObject);
            cameraTransform.position = new Vector3(50f, 0f, -10f);
            Invoke(constraint, "LateUpdate");
            Assert.That(constraint.HasValidBounds, Is.False);
            Assert.That(cameraTransform.position.x, Is.EqualTo(50f), "Clamping pauses while no source exists.");

            settings.Update();
            settings.FindProperty("boxCollider").objectReferenceValue =
                CreateBounds("Second bounds", new Vector3(100f, 0f, 0f));
            settings.ApplyModifiedPropertiesWithoutUndo();
            Invoke(constraint, "LateUpdate");

            Assert.That(constraint.HasValidBounds, Is.True,
                "A source assigned outside SetBoundsSource must be picked up even with autoUpdateBounds off.");
            Assert.That(cameraTransform.position.x, Is.EqualTo(96f).Within(1e-4f));
        }

        [Test]
        public void CameraConstraint_IsAtEdge_ReportsOnlyConstrainedAxesWithValidBounds()
        {
            CameraConstraint constraint = CreateConstraint(out Transform cameraTransform);
            cameraTransform.position = new Vector3(0f, 0f, -10f);

            Assert.That(constraint.IsAtEdge(out bool minX, out bool maxX, out bool minY, out bool maxY), Is.False,
                "Without calculated bounds no edge can be reported.");
            Assert.That(minX || maxX || minY || maxY, Is.False);

            constraint.SetBoundsSource(CameraConstraint.BoundsType.BoxCollider, CreateBounds("Bounds", Vector3.zero));
            constraint.SetAxisConstraint(false, true, false);
            cameraTransform.position = new Vector3(4f, 9f, -10f);
            Invoke(constraint, "LateUpdate");

            Assert.That(constraint.IsAtEdge(out minX, out maxX, out minY, out maxY), Is.True);
            Assert.That(cameraTransform.position.x, Is.EqualTo(4f), "X is not constrained.");
            Assert.That(maxX, Is.False, "An unconstrained axis has no edge, even at the bound value.");
            Assert.That(minX, Is.False);
            Assert.That(maxY, Is.True);
            Assert.That(minY, Is.False);
        }

        [Test]
        public void CameraConstraint_WarmLateUpdateInsideTheBounds_DoesNotAllocate()
        {
            CameraConstraint constraint = CreateConstraint(out Transform cameraTransform);
            constraint.SetBoundsSource(CameraConstraint.BoundsType.BoxCollider, CreateBounds("Bounds", Vector3.zero));
            cameraTransform.position = new Vector3(2f, -1f, -10f);
            Action lateUpdate = CreateAction(constraint, "LateUpdate");
            Action frames = () =>
            {
                for (int i = 0; i < 64; i++)
                {
                    lateUpdate();
                }
            };
            lateUpdate();
            cameraTransform.hasChanged = false;

            long calls = GcAlloc.TryCount(frames, out string notAdmitted);

            Assert.That(constraint.HasValidBounds, Is.True);
            Assert.That(cameraTransform.hasChanged, Is.False, "An in-bounds camera must not be rewritten.");
            Assert.That(cameraTransform.position, Is.EqualTo(new Vector3(2f, -1f, -10f)));
            GcAlloc.IgnoreIfNotAdmitted(notAdmitted);
            Assert.That(calls, Is.Zero,
                "A warm LateUpdate with Auto Update Bounds recalculates and clamps without allocating.");
        }

        [Test]
        public void ScreenPositioner_WarmEveryFrameUpdate_DoesNotAllocateOrRewriteAnUnmovedTransform()
        {
            Camera camera = CreateObject("Camera").AddComponent<Camera>();
            camera.orthographic = true;
            camera.pixelRect = new Rect(0f, 0f, 640f, 480f);
            camera.transform.position = new Vector3(0f, 0f, -10f);
            ScreenPositioner positioner = CreateObject("Positioner").AddComponent<ScreenPositioner>();
            SetField(positioner, "_targetCamera", camera);
            SetField(positioner, "_updateEveryFrame", true);
            SetField(positioner, "_offset", new Vector2(0.5f, -0.25f));
            Action lateUpdate = CreateAction(positioner, "LateUpdate");
            Action frames = () =>
            {
                for (int i = 0; i < 64; i++)
                {
                    lateUpdate();
                }
            };
            lateUpdate();
            Vector3 placed = positioner.transform.position;
            positioner.transform.hasChanged = false;

            long calls = GcAlloc.TryCount(frames, out string notAdmitted);

            Assert.That(positioner.transform.hasChanged, Is.False,
                "With the camera at rest the positioner must not rewrite its transform every frame.");
            Assert.That(positioner.transform.position, Is.EqualTo(placed));

            camera.transform.position = new Vector3(3f, 0f, -10f);
            lateUpdate();

            Assert.That(positioner.transform.hasChanged, Is.True, "A moved camera must still move the positioner.");
            Assert.That(positioner.transform.position.x - placed.x, Is.EqualTo(3f).Within(1e-3f));
            GcAlloc.IgnoreIfNotAdmitted(notAdmitted);
            Assert.That(calls, Is.Zero, "A warm every-frame ScreenPositioner update must not allocate.");
        }

        [Test]
        public void CharacterSprint_ControllerLost_DisablesInsteadOfThrowingEveryFrame()
        {
            GameObject owner = CreateObject("Sprinter");
            owner.AddComponent<CMF.AdvancedWalkerController>();
            NeoCharacterSprint sprint = owner.AddComponent<NeoCharacterSprint>();
            Invoke(sprint, "Awake");
            Assert.That(sprint.enabled, Is.True, "A required walker controller is found in Awake.");
            SetField(sprint, "_controller", null);

            Assert.DoesNotThrow(() => Invoke(sprint, "Update"));
            Assert.That(sprint.enabled, Is.False);
            Assert.DoesNotThrow(() => sprint.RefreshWalkSpeed());
        }

        private CameraConstraint CreateConstraint(out Transform cameraTransform)
        {
            GameObject cameraObject = CreateObject("Constraint camera");
            Camera camera = cameraObject.AddComponent<Camera>();
            camera.orthographic = true;
            camera.orthographicSize = 1f;
            camera.aspect = 1f;
            cameraTransform = cameraObject.transform;
            return cameraObject.AddComponent<CameraConstraint>();
        }

        private BoxCollider CreateBounds(string name, Vector3 center)
        {
            BoxCollider bounds = CreateObject(name).AddComponent<BoxCollider>();
            bounds.center = center;
            bounds.size = new Vector3(10f, 10f, 1f);
            Physics.SyncTransforms();
            return bounds;
        }

        private static Action CreateAction(object target, string name)
        {
            MethodInfo method = target.GetType().GetMethod(name, BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.That(method, Is.Not.Null, name);
            return (Action)method.CreateDelegate(typeof(Action), target);
        }

        private static int CountSettingsHandlers(object target)
        {
            Delegate handlers = SettingsBackingField().GetValue(null) as Delegate;
            if (handlers == null)
            {
                return 0;
            }

            int count = 0;
            foreach (Delegate handler in handlers.GetInvocationList())
            {
                if (ReferenceEquals(handler.Target, target))
                {
                    count++;
                }
            }

            return count;
        }

        private static void RemoveSettingsHandlers(object target)
        {
            FieldInfo field = SettingsBackingField();
            Delegate handlers = field.GetValue(null) as Delegate;
            if (handlers == null)
            {
                return;
            }

            foreach (Delegate handler in handlers.GetInvocationList())
            {
                if (ReferenceEquals(handler.Target, target))
                {
                    handlers = Delegate.Remove(handlers, handler);
                }
            }

            field.SetValue(null, handlers);
        }

        private static FieldInfo SettingsBackingField()
        {
            FieldInfo field = typeof(GameSettings).GetField(nameof(GameSettings.OnSettingsChanged),
                BindingFlags.Static | BindingFlags.NonPublic);
            Assert.That(field, Is.Not.Null);
            return field;
        }

        private GameObject CreateObject(string name)
        {
            GameObject created = new("Audit20261102NeoToolsMove " + name);
            _objects.Add(created);
            return created;
        }

        private static void SetField(object target, string name, object value)
        {
            FieldInfo field = target.GetType().GetField(name, BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.That(field, Is.Not.Null, name);
            field.SetValue(target, value);
        }

        private static void Invoke(object target, string name)
        {
            MethodInfo method = target.GetType().GetMethod(name, BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.That(method, Is.Not.Null, name);
            method.Invoke(target, null);
        }
    }
}
