using System.Collections;
using System.Collections.Generic;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.Events;
using UnityEngine.TestTools;

namespace Neo.NPC.Tests
{
    // Discovered in the Edit assembly; EnterPlayMode gives these tests real Awake/OnEnable callbacks.
    public sealed class Audit20261023NeoNPCLifecycleTests
    {
        private static readonly Vector3 Origin = new(12000f, 1200f, -12000f);
        private readonly List<GameObject> _objects = new();
        private NavMeshData _navMeshData;
        private NavMeshDataInstance _navMeshInstance;
        private int _agentTypeId;

        [UnitySetUp]
        public IEnumerator SetUp()
        {
            yield return new EnterPlayMode();

            Assert.That(NavMesh.GetSettingsCount(), Is.GreaterThan(0), "A native agent type is required.");
            NavMeshBuildSettings settings = NavMesh.GetSettingsByIndex(0);
            _agentTypeId = settings.agentTypeID;
            settings.overrideVoxelSize = true;
            settings.voxelSize = 0.1f;
            settings.minRegionArea = 0f;
            List<NavMeshBuildSource> sources = new()
            {
                new NavMeshBuildSource
                {
                    shape = NavMeshBuildSourceShape.Box,
                    size = new Vector3(12f, 1f, 12f),
                    transform = Matrix4x4.TRS(Origin + Vector3.down * 0.5f, Quaternion.identity, Vector3.one)
                }
            };
            _navMeshData = NavMeshBuilder.BuildNavMeshData(settings, sources,
                new Bounds(Origin, new Vector3(16f, 8f, 16f)), Vector3.zero, Quaternion.identity);
            Assert.That(_navMeshData, Is.Not.Null, "The owned transient NavMesh must be built.");
            _navMeshInstance = NavMesh.AddNavMeshData(_navMeshData);
            Assert.That(_navMeshInstance.valid, Is.True);
        }

        [UnityTearDown]
        public IEnumerator TearDown()
        {
            for (int i = _objects.Count - 1; i >= 0; i--)
            {
                if (_objects[i] != null)
                {
                    Object.DestroyImmediate(_objects[i]);
                }
            }

            _objects.Clear();
            if (_navMeshInstance.valid)
            {
                _navMeshInstance.Remove();
            }

            if (_navMeshData != null)
            {
                Object.DestroyImmediate(_navMeshData);
            }

            if (Application.isPlaying)
            {
                yield return new ExitPlayMode();
            }
        }

        [UnityTest]
        public IEnumerator Navigation_InitialModeListener_EntersPatrolOnceWithoutEnteringFollow()
        {
            NpcNavigation navigation = CreateInactiveNavigation(NpcNavigation.NavigationMode.FollowTarget,
                out NavMeshAgent agent, out Transform followTarget, out Transform waypoint);
            List<string> events = new();
            int followEntries = 0;
            int followPaths = 0;
            navigation.onModeChanged.AddListener(initialMode =>
            {
                events.Add("mode:" + initialMode);
                navigation.SetMode(NpcNavigation.NavigationMode.Patrol);
                events.Add("callback-returned");
            });
            navigation.onPatrolStarted.AddListener(() => events.Add("patrol-started"));
            navigation.onTargetChanged.AddListener(_ => followEntries++);
            navigation.onPathUpdated.AddListener(_ => followPaths++);

            // Activation, rather than reflection, drives Awake followed by the first OnEnable.
            navigation.gameObject.SetActive(true);

            Assert.That(navigation.Mode, Is.EqualTo(NpcNavigation.NavigationMode.Patrol));
            Assert.That(events, Is.EqualTo(new[] { "mode:FollowTarget", "callback-returned", "patrol-started" }));
            Assert.That(followEntries, Is.Zero, "The superseded FollowTarget behaviour must never enter.");
            Assert.That(followPaths, Is.Zero);
            Assert.That(navigation.FollowTarget, Is.SameAs(followTarget));
            yield return AssertCompletePath(agent, waypoint.position);
            Assert.That(events.Count, Is.EqualTo(3), "Path calculation must not repeat startup events.");
            Assert.That(followEntries, Is.Zero);
            Assert.That(followPaths, Is.Zero);
        }

        [UnityTest]
        public IEnumerator Navigation_InitialModeListener_UsesLastDeferredModeWithoutStartingPatrol()
        {
            NpcNavigation navigation = CreateInactiveNavigation(NpcNavigation.NavigationMode.Patrol,
                out NavMeshAgent agent, out Transform followTarget, out Transform waypoint);
            List<NpcNavigation.NavigationMode> modes = new();
            List<Transform> targets = new();
            List<Vector3> paths = new();
            int patrolStarts = 0;
            navigation.onModeChanged.AddListener(initialMode =>
            {
                modes.Add(initialMode);
                navigation.SetMode(NpcNavigation.NavigationMode.Combined);
                navigation.SetMode(NpcNavigation.NavigationMode.FollowTarget);
            });
            navigation.onPatrolStarted.AddListener(() => patrolStarts++);
            navigation.onTargetChanged.AddListener(targets.Add);
            navigation.onPathUpdated.AddListener(paths.Add);

            navigation.gameObject.SetActive(true);

            Assert.That(navigation.Mode, Is.EqualTo(NpcNavigation.NavigationMode.FollowTarget));
            Assert.That(modes, Is.EqualTo(new[] { NpcNavigation.NavigationMode.Patrol }));
            Assert.That(patrolStarts, Is.Zero, "Neither superseded Patrol nor Combined may enter.");
            Assert.That(targets, Is.EqualTo(new[] { followTarget }));
            Assert.That(paths.Count, Is.EqualTo(1));
            Assert.That(Vector3.Distance(paths[0], followTarget.position), Is.LessThan(0.2f));
            yield return AssertCompletePath(agent, followTarget.position);
            Assert.That(Vector3.Distance(agent.destination, waypoint.position), Is.GreaterThan(1f));
            Assert.That(modes.Count, Is.EqualTo(1));
            Assert.That(patrolStarts, Is.Zero);
            Assert.That(targets.Count, Is.EqualTo(1));
            Assert.That(paths.Count, Is.EqualTo(1));
        }

        private NpcNavigation CreateInactiveNavigation(NpcNavigation.NavigationMode initialMode,
            out NavMeshAgent agent, out Transform followTarget, out Transform waypoint)
        {
            followTarget = CreateObject("Owned follow target", Origin + Vector3.left * 4f).transform;
            waypoint = CreateObject("Owned patrol waypoint", Origin + Vector3.right * 4f).transform;
            GameObject host = CreateObject("Owned NPC lifecycle host", Origin);
            host.SetActive(false);
            agent = host.AddComponent<NavMeshAgent>();
            agent.agentTypeID = _agentTypeId;
            NpcNavigation navigation = host.AddComponent<NpcNavigation>();
            navigation.SetMode(initialMode);
            navigation.SetFollowTarget(followTarget);
            using (SerializedObject settings = new(navigation))
            {
                SerializedProperty points = settings.FindProperty("patrolPoints");
                points.arraySize = 1;
                points.GetArrayElementAtIndex(0).objectReferenceValue = waypoint;
                settings.FindProperty("autoUpdatePath").boolValue = false;
                settings.FindProperty("walkSpeed").floatValue = 0.1f;
                settings.FindProperty("stoppingDistance").floatValue = 0.1f;
                settings.ApplyModifiedPropertiesWithoutUndo();
            }

            navigation.onModeChanged = new UnityEvent<NpcNavigation.NavigationMode>();
            navigation.onPatrolStarted = new UnityEvent();
            navigation.onTargetChanged = new UnityEvent<Transform>();
            navigation.onPathUpdated = new UnityEvent<Vector3>();
            return navigation;
        }

        private GameObject CreateObject(string name, Vector3 position)
        {
            GameObject result = new(name);
            _objects.Add(result);
            result.transform.position = position;
            return result;
        }

        private static IEnumerator AssertCompletePath(NavMeshAgent agent, Vector3 destination)
        {
            Assert.That(agent.isOnNavMesh, Is.True, "Activation must bind the real native agent.");
            double deadline = Time.realtimeSinceStartupAsDouble + 3d;
            while ((agent.pathPending || !agent.hasPath) && Time.realtimeSinceStartupAsDouble < deadline)
            {
                yield return null;
            }

            Assert.That(agent.pathPending, Is.False, "The native path must settle within the deadline.");
            Assert.That(agent.hasPath, Is.True);
            Assert.That(agent.pathStatus, Is.EqualTo(NavMeshPathStatus.PathComplete));
            Assert.That(Vector3.Distance(agent.destination, destination), Is.LessThan(0.2f));
            Vector3[] corners = agent.path.corners;
            Assert.That(corners.Length, Is.GreaterThanOrEqualTo(2));
            Assert.That(Vector3.Distance(corners[corners.Length - 1], destination), Is.LessThan(0.2f));
        }
    }
}
