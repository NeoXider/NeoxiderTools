using System.Collections.Generic;
using Neo.Tools;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Neo.Editor.Tests
{
    /// <summary>
    ///     <see cref="Follow.TeleportToTarget" /> snaps only the channels the follower follows: a rotation-only
    ///     follower (a turret) keeps its place and turns straight to the aim, a position-only follower keeps its
    ///     rotation, and a follower with both moves first and then aims from its new position.
    /// </summary>
    public sealed class FollowTeleportToTargetTests
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

        [Test]
        public void RotationOnlyFollower_KeepsItsPositionAndSnapsItsAim()
        {
            Follow follow = CreateFollower(false, true);
            follow.transform.position = new Vector3(1f, 2f, 3f);
            Transform target = CreateTarget(new Vector3(11f, 2f, 3f));
            follow.SetTarget(target);

            follow.TeleportToTarget();

            Assert.That(follow.transform.position, Is.EqualTo(new Vector3(1f, 2f, 3f)),
                "followPosition is off, so the teleport must not move the follower onto the target.");
            Assert.That(Quaternion.Angle(follow.transform.rotation, Quaternion.LookRotation(Vector3.right)),
                Is.LessThan(0.01f), "The rotation must snap to the aim without smoothing.");
        }

        [Test]
        public void RotationOnlyFollower2D_SnapsInsideItsRotationLimit()
        {
            Follow follow = CreateFollower(false, true);
            SerializedObject settings = new(follow);
            settings.FindProperty("followMode").enumValueIndex = (int)Follow.FollowMode.TwoD;
            settings.FindProperty("rotationLimitZ.enabled").boolValue = true;
            settings.FindProperty("rotationLimitZ.min").floatValue = -45f;
            settings.FindProperty("rotationLimitZ.max").floatValue = 45f;
            settings.ApplyModifiedPropertiesWithoutUndo();
            Transform target = CreateTarget(new Vector3(-10f, 0.01f, 0f));
            follow.SetTarget(target);

            follow.TeleportToTarget();

            Assert.That(follow.transform.position, Is.EqualTo(Vector3.zero));
            Assert.That(Mathf.Abs(Mathf.DeltaAngle(follow.transform.eulerAngles.z, 45f)), Is.LessThan(0.01f),
                "The snapped aim must respect the 2D rotation limit like the per-frame aim does.");
        }

        [Test]
        public void PositionOnlyFollower_MovesAndKeepsItsRotation()
        {
            Follow follow = CreateFollower(true, false);
            Quaternion authored = Quaternion.Euler(0f, 30f, 0f);
            follow.transform.rotation = authored;
            follow.SetOffset(new Vector3(0f, 1f, 0f));
            Transform target = CreateTarget(new Vector3(5f, 0f, 0f));
            follow.SetTarget(target);

            follow.TeleportToTarget();

            Assert.That(Vector3.Distance(follow.transform.position, new Vector3(5f, 1f, 0f)), Is.LessThan(1e-5f));
            Assert.That(Quaternion.Angle(follow.transform.rotation, authored), Is.LessThan(0.01f),
                "followRotation is off, so the teleport must not turn the follower.");
        }

        [Test]
        public void FollowerWithBothChannels_MovesFirstAndAimsFromTheNewPosition()
        {
            Follow follow = CreateFollower(true, true);
            follow.transform.rotation = Quaternion.Euler(0f, 90f, 0f);
            follow.SetOffset(new Vector3(0f, 0f, -5f));
            Transform target = CreateTarget(new Vector3(3f, 0f, 10f));
            follow.SetTarget(target);

            follow.TeleportToTarget();

            Assert.That(Vector3.Distance(follow.transform.position, new Vector3(3f, 0f, 5f)), Is.LessThan(1e-5f));
            Assert.That(Quaternion.Angle(follow.transform.rotation, Quaternion.LookRotation(Vector3.forward)),
                Is.LessThan(0.01f), "The aim must come from the teleported position, straight along +Z.");
        }

        [Test]
        public void FollowerOnTopOfItsTarget_KeepsItsRotation()
        {
            Follow follow = CreateFollower(true, true);
            Quaternion authored = Quaternion.Euler(10f, 20f, 0f);
            follow.transform.rotation = authored;
            Transform target = CreateTarget(new Vector3(2f, 2f, 2f));
            follow.SetTarget(target);

            follow.TeleportToTarget();

            Assert.That(Vector3.Distance(follow.transform.position, target.position), Is.LessThan(1e-5f));
            Assert.That(Quaternion.Angle(follow.transform.rotation, authored), Is.LessThan(0.01f),
                "On top of the target the aim is undefined, so the rotation stays as it was.");
        }

        private Follow CreateFollower(bool followPosition, bool followRotation)
        {
            GameObject host = new("Follower");
            _objects.Add(host);
            Follow follow = host.AddComponent<Follow>();
            follow.SetFollowPosition(followPosition);
            follow.SetFollowRotation(followRotation);
            follow.SetRotationSmoothMode(Follow.SmoothMode.Lerp);
            follow.SetPositionSmoothMode(Follow.SmoothMode.MoveTowards);
            return follow;
        }

        private Transform CreateTarget(Vector3 position)
        {
            GameObject target = new("Target");
            _objects.Add(target);
            target.transform.position = position;
            return target.transform;
        }
    }
}
