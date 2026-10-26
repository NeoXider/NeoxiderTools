using System.Collections;
using System.Collections.Generic;
using Neo.NPC.Navigation;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.Events;
using UnityEngine.TestTools;

namespace Neo.NPC.Tests
{
    public sealed class Audit20261023NeoNPCNavigationTests
    {
        private readonly List<GameObject> _objects = new();
        private NavMeshData _navMeshData;
        private NavMeshDataInstance _navMeshInstance;
        private NavMeshAgent _preparedAgent;
        private int _agentTypeId;
        private Random.State _randomState;
        private bool _hasRandomState;

        [UnitySetUp]
        public IEnumerator SetUp()
        {
            // Native agent registration requires Play Mode, even though this is an Edit assembly.
            yield return new EnterPlayMode();
            Assert.That(Application.isPlaying, Is.True);
            yield return WaitForNativeFrame();

            _randomState = Random.state;
            _hasRandomState = true;
            Assert.That(NavMesh.GetSettingsCount(), Is.GreaterThan(0));
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
                    transform = Matrix4x4.TRS(new Vector3(0f, -0.5f, 0f), Quaternion.identity, Vector3.one)
                },
                new NavMeshBuildSource
                {
                    shape = NavMeshBuildSourceShape.Box,
                    size = new Vector3(8f, 1f, 8f),
                    transform = Matrix4x4.TRS(new Vector3(20f, -0.5f, 0f), Quaternion.identity, Vector3.one)
                }
            };
            _navMeshData = NavMeshBuilder.BuildNavMeshData(settings, sources,
                new Bounds(new Vector3(10f, 0f, 0f), new Vector3(40f, 8f, 20f)),
                Vector3.zero, Quaternion.identity);
            Assert.That(_navMeshData, Is.Not.Null, "The transient two-island NavMesh must be built.");
            _navMeshInstance = NavMesh.AddNavMeshData(_navMeshData);
            Assert.That(_navMeshInstance.valid, Is.True);

            NavMeshQueryFilter filter = new() { agentTypeID = _agentTypeId, areaMask = NavMesh.AllAreas };
            Assert.That(NavMesh.SamplePosition(Vector3.zero, out NavMeshHit origin, 0.5f, filter), Is.True,
                "The registered data must expose the starting island for this exact agent type.");
            Assert.That(Vector3.Distance(origin.position, Vector3.zero), Is.LessThan(0.2f));
            Assert.That(NavMesh.SamplePosition(new Vector3(20f, 0f, 0f), out NavMeshHit otherIsland, 0.5f, filter),
                Is.True, "The disconnected destination island must also be present.");
            Assert.That(Vector3.Distance(otherIsland.position, new Vector3(20f, 0f, 0f)), Is.LessThan(0.2f));

            GameObject host = CreateObject("NPC agent", origin.position);
            host.SetActive(false);
            _preparedAgent = host.AddComponent<NavMeshAgent>();
            _preparedAgent.agentTypeID = _agentTypeId;
            _preparedAgent.stoppingDistance = 0.1f;
            host.SetActive(true);
            yield return WaitUntilNative(() => _preparedAgent.isOnNavMesh,
                "Activation must register the native agent on the owned NavMesh.");
            Assert.That(_preparedAgent.Warp(Vector3.zero), Is.True, "Native Warp must succeed before any test body.");
            Assert.That(_preparedAgent.isOnNavMesh, Is.True);
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
            _preparedAgent = null;
            if (_hasRandomState)
            {
                Random.state = _randomState;
                _hasRandomState = false;
            }

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
                // Runner-supported top-level transition, including after an assertion failure.
                yield return new ExitPlayMode();
            }
        }

        [Test]
        public void Follow_FirstDestinationAtWorldOrigin_SubmitsAPathOnFirstTick()
        {
            NavMeshAgent agent = CreateAgent(new Vector3(3f, 0f, 0f));
            NpcFollowTargetCore core = CreateFollow(agent);
            int updates = 0;
            core.PathUpdated += _ => updates++;
            core.SetDestination(Vector3.zero);

            core.Tick(0f, null);

            Assert.That(updates, Is.EqualTo(1));
            Assert.That(Vector3.Distance(agent.destination, Vector3.zero), Is.LessThan(0.2f));
        }

        [Test]
        public void Follow_NewTargetAtSamePosition_SubmitsANewPathWithoutWaiting()
        {
            NavMeshAgent agent = CreateAgent(Vector3.zero);
            NpcFollowTargetCore core = CreateFollow(agent);
            Transform target = CreateObject("Replacement target", new Vector3(3f, 0f, 0f)).transform;
            core.SetDestination(target.position);
            Assert.That(core.TryForceUpdatePath(null), Is.True);
            int updates = 0;
            core.PathUpdated += _ => updates++;

            core.SetTarget(target);
            core.Tick(0f, null);

            Assert.That(core.Target, Is.SameAs(target));
            Assert.That(updates, Is.EqualTo(1));
        }

        [Test]
        public void Follow_ClearedPathToStationaryDestination_IsRebuilt()
        {
            NavMeshAgent agent = CreateAgent(Vector3.zero);
            NpcFollowTargetCore core = CreateFollow(agent);
            core.SetDestination(new Vector3(3f, 0f, 0f));
            Assert.That(core.TryForceUpdatePath(null), Is.True);
            agent.ResetPath();
            int updates = 0;
            core.PathUpdated += _ => updates++;

            core.Tick(10f, null);

            Assert.That(updates, Is.EqualTo(1));
        }

        [Test]
        public void Follow_StopSurvivesTicks_UntilResume()
        {
            NavMeshAgent agent = CreateAgent(Vector3.zero);
            NpcFollowTargetCore core = CreateFollow(agent);
            core.SetDestination(new Vector3(3f, 0f, 0f));
            Assert.That(core.TryForceUpdatePath(null), Is.True);

            core.Stop();
            core.Tick(10f, null);
            Assert.That(agent.isStopped, Is.True);

            core.Resume();
            core.Tick(11f, null);
            Assert.That(agent.isStopped, Is.False);
        }

        [UnityTest]
        public IEnumerator Follow_PartialPathWithinStoppingDistance_IsBlockedRatherThanReached()
        {
            NavMeshAgent agent = CreateAgent(Vector3.zero);
            Vector3 destination = new(20f, 0f, 0f);
            NavMeshPath path = new();
            Assert.That(agent.CalculatePath(destination, path), Is.True);
            Assert.That(path.status, Is.EqualTo(NavMeshPathStatus.PathPartial));
            Assert.That(agent.SetPath(path), Is.True);
            agent.stoppingDistance = 100f;
            agent.isStopped = true;
            yield return WaitForSettledPath(agent);
            Assert.That(agent.pathStatus, Is.EqualTo(NavMeshPathStatus.PathPartial));
            Assert.That(agent.remainingDistance, Is.LessThanOrEqualTo(agent.stoppingDistance));
            NpcFollowTargetCore core = new(agent.transform, agent);
            core.Configure(0f, false, 0.1f, 1f);
            core.SetDestination(destination);
            int reached = 0;
            int blocked = 0;
            core.DestinationReached += _ => reached++;
            core.PathBlocked += _ => blocked++;

            core.Tick(10f, null);

            Assert.That(blocked, Is.EqualTo(1));
            Assert.That(reached, Is.Zero);
        }

        [TestCase(true)]
        [TestCase(false)]
        public void Patrol_AllWaypointsMissing_StopsWithoutReportingStarted(bool loop)
        {
            NavMeshAgent agent = CreateAgent(Vector3.zero);
            NpcPatrolCore core = new(agent.transform, agent);
            core.Configure(new Transform[2], null, 0f, loop, 1f);
            int started = 0;
            core.PatrolStarted += () => started++;

            core.StartPatrol(true);

            Assert.That(core.IsPatrolling, Is.False);
            Assert.That(agent.isStopped, Is.True);
            Assert.That(started, Is.Zero);
        }

        [Test]
        public void Patrol_UnreachableZone_StopsAfterBoundedAttempts()
        {
            NavMeshAgent agent = CreateAgent(Vector3.zero);
            BoxCollider zone = CreateObject("Unreachable zone", new Vector3(100f, 0f, 0f)).AddComponent<BoxCollider>();
            NpcPatrolCore core = new(agent.transform, agent);
            core.Configure(null, zone, 0f, true, 1f);
            int unreachable = 0;
            core.DestinationUnreachable += _ => unreachable++;

            core.StartPatrol(true);

            Assert.That(core.IsPatrolling, Is.False);
            Assert.That(agent.isStopped, Is.True);
            Assert.That(unreachable, Is.EqualTo(5));
        }

        [UnityTest]
        public IEnumerator Patrol_PartialPathWithinStoppingDistance_DoesNotReportWaypointReached()
        {
            NavMeshAgent agent = CreateAgent(Vector3.zero);
            Transform waypoint = CreateObject("Disconnected waypoint", new Vector3(20f, 0f, 0f)).transform;
            NpcPatrolCore core = new(agent.transform, agent);
            core.Configure(new[] { waypoint }, null, 0f, false, 1f);
            int started = 0;
            int reached = 0;
            core.PatrolStarted += () => started++;
            core.PatrolPointReached += _ => reached++;
            bool previousAutoRepath = agent.autoRepath;

            try
            {
                // Hold one explicit native path; automatic replanning is outside this Tick contract.
                agent.autoRepath = false;
                Assert.That(agent.autoRepath, Is.False);
                core.StartPatrol(true);
                agent.isStopped = true;
                Assert.That(core.IsPatrolling, Is.True);
                Assert.That(started, Is.EqualTo(1));

                // StartPatrol queues SetDestination. Drain it before installing the calculated path.
                yield return WaitForSettledPath(agent);
                Assert.That(agent.pathPending, Is.False);
                agent.ResetPath();
                Assert.That(agent.pathPending, Is.False);
                Assert.That(agent.hasPath, Is.False);

                NavMeshPath path = new();
                Assert.That(agent.CalculatePath(waypoint.position, path), Is.True);
                Assert.That(path.status, Is.EqualTo(NavMeshPathStatus.PathPartial));
                Assert.That(agent.SetPath(path), Is.True);
                agent.stoppingDistance = 100f;
                yield return WaitForNativeFrame();
                yield return WaitForSettledPath(agent);
                Assert.That(agent.pathStatus, Is.EqualTo(NavMeshPathStatus.PathPartial));
                Assert.That(agent.remainingDistance, Is.LessThanOrEqualTo(agent.stoppingDistance));
                Assert.That(reached, Is.Zero);

                core.Tick(10f);

                Assert.That(core.IsPatrolling, Is.True);
                Assert.That(reached, Is.Zero);
                yield return WaitForNativeFrame();
                Assert.That(agent.pathStatus, Is.EqualTo(NavMeshPathStatus.PathPartial));
                core.Tick(11f);
                Assert.That(core.IsPatrolling, Is.True);
                Assert.That(started, Is.EqualTo(1));
                Assert.That(reached, Is.Zero);
            }
            finally
            {
                agent.autoRepath = previousAutoRepath;
            }
        }

        [UnityTest]
        public IEnumerator Navigation_DisabledComponent_StopsAgentAndResumesOnEnable()
        {
            NavMeshAgent agent = CreateAgent(Vector3.zero);
            NpcNavigation navigation = agent.gameObject.AddComponent<NpcNavigation>();
            Assert.That(navigation.SetDestination(new Vector3(3f, 0f, 0f)), Is.True);
            navigation.Resume();

            navigation.enabled = false;
            Assert.That(agent.isStopped, Is.True);
            yield return WaitForNativeFrame();
            Assert.That(agent.isStopped, Is.True);

            navigation.enabled = true;
            Assert.That(agent.isStopped, Is.False);
        }

        [UnityTest]
        public IEnumerator Navigation_Stop_ReportsMovementStoppedOnce()
        {
            NavMeshAgent agent = CreateAgent(Vector3.zero);
            NpcNavigation navigation = agent.gameObject.AddComponent<NpcNavigation>();
            navigation.onMovementStarted = new UnityEvent();
            navigation.onMovementStopped = new UnityEvent();
            int started = 0;
            int stopped = 0;
            float observedVelocitySquared = 0f;
            navigation.onMovementStarted.AddListener(() =>
            {
                started++;
                observedVelocitySquared = agent.velocity.sqrMagnitude;
            });
            navigation.onMovementStopped.AddListener(() => stopped++);
            Assert.That(navigation.SetDestination(new Vector3(3f, 0f, 0f)), Is.True);
            yield return WaitUntilNative(() => started > 0, "A native path must actually produce movement.");
            Assert.That(started, Is.EqualTo(1));
            Assert.That(observedVelocitySquared, Is.GreaterThan(0f));

            navigation.Stop();
            navigation.Stop();

            Assert.That(stopped, Is.EqualTo(1));
            Assert.That(agent.isStopped, Is.True);
            yield return WaitForNativeFrame();
            Assert.That(stopped, Is.EqualTo(1));
            Assert.That(agent.isStopped, Is.True);
        }

        [Test]
        public void Navigation_SwitchingToPatrolWhilePaused_DoesNotResumeAgent()
        {
            NavMeshAgent agent = CreateAgent(Vector3.zero);
            NpcNavigation navigation = agent.gameObject.AddComponent<NpcNavigation>();
            Transform waypoint = CreateObject("Patrol waypoint", new Vector3(3f, 0f, 0f)).transform;
            using (SerializedObject settings = new(navigation))
            {
                SerializedProperty points = settings.FindProperty("patrolPoints");
                points.arraySize = 1;
                points.GetArrayElementAtIndex(0).objectReferenceValue = waypoint;
                settings.ApplyModifiedPropertiesWithoutUndo();
            }

            navigation.Stop();

            navigation.SetMode(NpcNavigation.NavigationMode.Patrol);

            Assert.That(navigation.IsActive, Is.False);
            Assert.That(agent.isStopped, Is.True);
        }

        [UnityTest]
        public IEnumerator Combined_ManualPathUpdates_StillSubmitsInitialFollowPath()
        {
            NavMeshAgent agent = CreateAgent(Vector3.zero);
            NpcNavigation navigation = agent.gameObject.AddComponent<NpcNavigation>();
            using (SerializedObject settings = new(navigation))
            {
                settings.FindProperty("autoUpdatePath").boolValue = false;
                settings.ApplyModifiedPropertiesWithoutUndo();
            }

            Transform target = CreateObject("Manual follow target", new Vector3(3f, 0f, 0f)).transform;
            navigation.SetCombinedTarget(target);
            navigation.SetMode(NpcNavigation.NavigationMode.Combined);
            navigation.onPathUpdated = new UnityEvent<Vector3>();
            int updates = 0;
            navigation.onPathUpdated.AddListener(_ => updates++);

            yield return WaitUntilNative(() => navigation.IsChasing,
                "The native Combined Update must enter follow even with automatic path updates disabled.");

            Assert.That(navigation.IsChasing, Is.True);
            Assert.That(updates, Is.EqualTo(1));
            Assert.That(Vector3.Distance(agent.destination, target.position), Is.LessThan(0.2f));
        }

        [UnityTest]
        public IEnumerator Combined_UnreachableTarget_CanStartFollowingAgain()
        {
            NavMeshAgent agent = CreateAgent(Vector3.zero);
            NpcNavigation navigation = agent.gameObject.AddComponent<NpcNavigation>();
            using (SerializedObject settings = new(navigation))
            {
                settings.FindProperty("maxSampleDistance").floatValue = 1f;
                settings.ApplyModifiedPropertiesWithoutUndo();
            }

            Transform target = CreateObject("Target above NavMesh", new Vector3(0f, 4f, 0f)).transform;
            navigation.SetCombinedTarget(target);
            navigation.SetMode(NpcNavigation.NavigationMode.Combined);
            navigation.onStartFollowing = new UnityEvent();
            int starts = 0;
            navigation.onStartFollowing.AddListener(() => starts++);
            navigation.onStopFollowing = new UnityEvent();
            // Pause only after the failed native attempt completes, so exact attempt counts are observable.
            navigation.onStopFollowing.AddListener(navigation.Stop);

            yield return WaitUntilNative(() => starts > 0 && !navigation.IsActive,
                "The first real follow attempt must fail and reach the public stop listener.");
            Assert.That(starts, Is.EqualTo(1));
            Assert.That(navigation.IsChasing, Is.False, "Unreachable follow should have exited.");

            target.position = new Vector3(3f, 0f, 0f);
            navigation.Resume();
            yield return WaitUntilNative(() => starts == 2 && navigation.IsChasing,
                "After resuming, the next native attempt must follow the now-reachable target.");

            Assert.That(starts, Is.EqualTo(2));
            Assert.That(navigation.IsChasing, Is.True);
            Assert.That(Vector3.Distance(agent.destination, target.position), Is.LessThan(0.2f));
        }

        [UnityTest]
        public IEnumerator Combined_DestroyedTarget_ExitsFollowingAndClearsChaseState()
        {
            NavMeshAgent agent = CreateAgent(Vector3.zero);
            NpcNavigation navigation = agent.gameObject.AddComponent<NpcNavigation>();
            GameObject target = CreateObject("Transient target", new Vector3(3f, 0f, 0f));
            navigation.SetCombinedTarget(target.transform);
            navigation.SetMode(NpcNavigation.NavigationMode.Combined);
            navigation.onStopFollowing = new UnityEvent();
            int stops = 0;
            navigation.onStopFollowing.AddListener(() => stops++);
            yield return WaitUntilNative(() => navigation.IsChasing, "A live target must first be followed.");
            Assert.That(navigation.IsChasing, Is.True);

            Object.Destroy(target);
            yield return WaitUntilNative(() => target == null && !navigation.IsChasing,
                "Native target destruction must return the NPC to patrol.");

            Assert.That(target == null, Is.True);
            Assert.That(navigation.IsChasing, Is.False);
            Assert.That(stops, Is.EqualTo(1));
            Assert.That(agent.isStopped, Is.True, "No patrol route is configured, so the old chase must stop.");
        }

        [UnityTest]
        public IEnumerator Combined_ReplacingTargetDuringChase_UpdatesTheDestination()
        {
            NavMeshAgent agent = CreateAgent(Vector3.zero);
            NpcNavigation navigation = agent.gameObject.AddComponent<NpcNavigation>();
            Transform first = CreateObject("Initial combined target", new Vector3(3f, 0f, 0f)).transform;
            Transform second = CreateObject("Replacement combined target", new Vector3(-3f, 0f, 0f)).transform;
            navigation.SetCombinedTarget(first);
            navigation.SetMode(NpcNavigation.NavigationMode.Combined);
            yield return WaitUntilNative(() => navigation.IsChasing, "The original target must first be followed.");
            Assert.That(navigation.IsChasing, Is.True);

            navigation.SetCombinedTarget(second);
            yield return WaitUntilNative(() => Vector3.Distance(agent.destination, second.position) < 0.2f,
                "A native follow tick must submit the replacement target's destination.");

            Assert.That(navigation.CombinedTarget, Is.SameAs(second));
            Assert.That(Vector3.Distance(agent.destination, second.position), Is.LessThan(0.2f));
        }

        [UnityTest]
        public IEnumerator Follow_CompletePath_ReportsOrdinaryArrivalOnceWithRequestedPayload()
        {
            NavMeshAgent agent = CreateAgent(Vector3.zero);
            NpcFollowTargetCore core = new(agent.transform, agent);
            core.Configure(0f, false, 0.1f, 1f);
            Vector3 destination = new(3f, 0f, 0f);
            List<Vector3> arrivals = new();
            int updates = 0;
            core.DestinationReached += arrivals.Add;
            core.PathUpdated += _ => updates++;
            core.SetDestination(destination);
            Assert.That(core.TryForceUpdatePath(null), Is.True);
            yield return WaitForSettledPath(agent);
            Assert.That(agent.pathStatus, Is.EqualTo(NavMeshPathStatus.PathComplete));
            Assert.That(Vector3.Distance(agent.destination, destination), Is.LessThan(0.2f));

            double deadline = Time.realtimeSinceStartupAsDouble + 3d;
            while (arrivals.Count == 0 && Time.realtimeSinceStartupAsDouble < deadline)
            {
                core.Tick(Time.time, null);
                yield return WaitForNativeFrame();
            }

            Assert.That(Vector3.Distance(agent.transform.position, Vector3.zero), Is.GreaterThan(1f),
                "Arrival must follow actual native movement, not an injected remaining distance.");
            Assert.That(arrivals, Is.EqualTo(new[] { destination }));
            Assert.That(updates, Is.EqualTo(1));
            core.Tick(Time.time, null);
            core.Tick(Time.time, null);
            Assert.That(arrivals, Is.EqualTo(new[] { destination }));
        }

        [UnityTest]
        public IEnumerator Follow_OwnedNavMeshRemovedAndRebound_PreservesFailureAndRecoveryOracles()
        {
            NavMeshAgent agent = CreateAgent(Vector3.zero);
            NpcFollowTargetCore core = new(agent.transform, agent);
            core.Configure(0f, false, 0.1f, 1f);
            core.SetDestination(new Vector3(3f, 0f, 0f));
            int updates = 0;
            int reached = 0;
            core.PathUpdated += _ => updates++;
            core.DestinationReached += _ => reached++;
            Assert.That(core.TryForceUpdatePath(null), Is.True);
            yield return WaitForSettledPath(agent);
            Assert.That(agent.pathStatus, Is.EqualTo(NavMeshPathStatus.PathComplete));
            Assert.That(updates, Is.EqualTo(1));

            agent.enabled = false;
            _navMeshInstance.Remove();
            Assert.That(_navMeshInstance.valid, Is.False);
            NavMeshQueryFilter filter = new() { agentTypeID = _agentTypeId, areaMask = NavMesh.AllAreas };
            Assert.That(NavMesh.SamplePosition(Vector3.zero, out NavMeshHit removedHit, 0.5f, filter), Is.False,
                "Only the owned NavMesh is removed; an unexpected foreign surface must fail this precondition.");
            Assert.That(core.TryForceUpdatePath(null), Is.False);
            core.Stop();
            core.Tick(Time.time, null);
            core.Resume();
            Assert.That(updates, Is.EqualTo(1));
            Assert.That(reached, Is.Zero);

            _navMeshInstance = NavMesh.AddNavMeshData(_navMeshData);
            Assert.That(_navMeshInstance.valid, Is.True);
            agent.enabled = true;
            yield return WaitUntilNative(() => agent.isOnNavMesh, "The same owned agent must rebind to restored data.");
            Assert.That(CreateAgent(Vector3.zero), Is.SameAs(agent));
            Assert.That(core.TryForceUpdatePath(null), Is.True);
            yield return WaitForSettledPath(agent);
            Assert.That(agent.pathStatus, Is.EqualTo(NavMeshPathStatus.PathComplete));
            Assert.That(Vector3.Distance(agent.destination, new Vector3(3f, 0f, 0f)), Is.LessThan(0.2f));
            Assert.That(updates, Is.EqualTo(2));
            Assert.That(reached, Is.Zero);
        }

        private NpcFollowTargetCore CreateFollow(NavMeshAgent agent)
        {
            NpcFollowTargetCore core = new(agent.transform, agent);
            core.Configure(0f, true, 0.1f, 1f);
            return core;
        }

        private NavMeshAgent CreateAgent(Vector3 position)
        {
            // Every case receives its own agent from UnitySetUp after real activation/registration.
            NavMeshAgent agent = _preparedAgent;
            Assert.That(Application.isPlaying, Is.True);
            Assert.That(agent, Is.Not.Null);
            Assert.That(_navMeshInstance.valid, Is.True);
            Assert.That(agent.agentTypeID, Is.EqualTo(_agentTypeId));
            NavMeshQueryFilter filter = new() { agentTypeID = _agentTypeId, areaMask = agent.areaMask };
            Assert.That(NavMesh.SamplePosition(position, out NavMeshHit placement, 0.5f, filter), Is.True);
            Assert.That(Vector3.Distance(placement.position, position), Is.LessThan(0.2f));
            Assert.That(agent.isOnNavMesh, Is.True, "Registration must precede Warp, not be inferred from it.");
            Assert.That(agent.Warp(position), Is.True);
            Assert.That(agent.isOnNavMesh, Is.True);
            return agent;
        }

        private GameObject CreateObject(string name, Vector3 position)
        {
            GameObject result = new(name);
            result.transform.position = position;
            _objects.Add(result);
            return result;
        }

        private static IEnumerator WaitForSettledPath(NavMeshAgent agent)
        {
            yield return WaitUntilNative(() => !agent.pathPending && agent.hasPath &&
                !float.IsNaN(agent.remainingDistance) && !float.IsInfinity(agent.remainingDistance),
                "The real native path and remaining distance must settle before behaviour assertions.");
        }

        private static IEnumerator WaitUntilNative(System.Func<bool> condition, string message)
        {
            double deadline = Time.realtimeSinceStartupAsDouble + 3d;
            while (!condition() && Time.realtimeSinceStartupAsDouble < deadline)
            {
                yield return WaitForNativeFrame();
            }

            Assert.That(condition(), Is.True, message);
        }

        private static IEnumerator WaitForNativeFrame()
        {
            Assert.That(Application.isPlaying, Is.True);
            int frame = Time.frameCount;
            double deadline = Time.realtimeSinceStartupAsDouble + 3d;
            while (Time.frameCount == frame && Time.realtimeSinceStartupAsDouble < deadline)
            {
                yield return null;
            }

            Assert.That(Time.frameCount, Is.GreaterThan(frame), "An Editor update alone is not a native frame.");
        }
    }
}
