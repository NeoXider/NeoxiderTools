using System;
using System.Collections;
using System.Collections.Generic;
using Neo.Tools;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.Events;
using UnityEngine.TestTools;
using Object = UnityEngine.Object;

#pragma warning disable CS0618 // These regressions intentionally preserve the legacy component's public contracts.
namespace Neo.Editor.Tests.Tools
{
    public sealed class Audit20261023NeoToolsOtherNavigationTests
    {
        private readonly List<GameObject> _objects = new List<GameObject>();
        private readonly Vector3 _origin = new Vector3(1000f, 0f, 1000f);
        private NavMeshData _data;
        private NavMeshDataInstance _instance;
        private int _agentTypeId;
        private float _previousTimeScale;
        private bool _hasFrameTimingOverride;
        private float _savedFrameTimeScale;
        private float _savedCaptureDeltaTime;
        private float _savedMaximumDeltaTime;

        [SetUp]
        public void SetUp()
        {
            _previousTimeScale = Time.timeScale;
        }

        [UnityTearDown]
        public IEnumerator TearDown()
        {
            RestoreFrameTiming();
            Time.timeScale = _previousTimeScale;
            for (int index = _objects.Count - 1; index >= 0; index--)
            {
                if (_objects[index] != null)
                {
                    Object.DestroyImmediate(_objects[index]);
                }
            }

            _objects.Clear();
            if (_instance.valid)
            {
                _instance.Remove();
            }

            if (_data != null)
            {
                Object.DestroyImmediate(_data);
            }

            if (Application.isPlaying)
            {
                yield return new ExitPlayMode();
            }
        }

        [Test]
        public void AllNullLoopingRoute_DoesNotStartOrRecurse()
        {
            GameObject gameObject = CreateObject("EmptyRoute", Vector3.zero);
            AiNavigation navigation = gameObject.AddComponent<AiNavigation>();
            int started = 0;
            navigation.onPatrolStarted = new UnityEvent();
            navigation.onPatrolStarted.AddListener(() => started++);
            navigation.SetPatrolPoints(new Transform[3]);

            navigation.StartPatrol();

            Assert.That(navigation.IsPatrolling, Is.False);
            Assert.That(navigation.IsMoving, Is.False);
            Assert.That(started, Is.Zero);
        }

        [UnityTest]
        public IEnumerator FollowTarget_SubmitsOnce_AndUsesOneAutomaticInterval()
        {
            yield return new EnterPlayMode();
            BuildTestNavMesh();
            AiNavigation navigation = CreateNavigation(AiNavigation.MovementMode.FollowTarget, null);
            using (SerializedObject authoring = new SerializedObject(navigation))
            {
                authoring.FindProperty("pathUpdateInterval").floatValue = 0.2f;
                authoring.ApplyModifiedPropertiesWithoutUndo();
            }

            Transform target = CreateObject("CadenceTarget", _origin + Vector3.right * 8f).transform;
            List<float> submissions = new List<float>();
            navigation.onPathUpdated.AddListener(position => submissions.Add(Time.time));

            navigation.SetTarget(target);

            Assert.That(submissions.Count, Is.EqualTo(1), "SetTarget must submit exactly one initial path.");
            float deadline = Time.time + 0.65f;
            while (Time.time < deadline)
            {
                yield return null;
            }

            Assert.That(submissions.Count, Is.GreaterThanOrEqualTo(2));
            for (int index = 1; index < submissions.Count; index++)
            {
                Assert.That(submissions[index] - submissions[index - 1], Is.GreaterThanOrEqualTo(0.199f),
                    "Start and automatic scheduling must not submit a second path inside the interval.");
            }

            navigation.AutoUpdatePath = false;
            int beforeDisable = submissions.Count;
            target.position += Vector3.right;
            yield return new WaitForSecondsRealtime(0.35f);
            Assert.That(submissions.Count, Is.EqualTo(beforeDisable));

            navigation.AutoUpdatePath = true;
            Assert.That(submissions.Count, Is.EqualTo(beforeDisable + 1),
                "Enabling automatic updates refreshes the current target once.");
            navigation.Stop();
            int beforeStop = submissions.Count;
            yield return new WaitForSecondsRealtime(0.35f);
            Assert.That(submissions.Count, Is.EqualTo(beforeStop));
            Assert.That(navigation.IsMoving, Is.False);
        }

        [UnityTest]
        public IEnumerator FollowTarget_RefreshDueEveryFrame_ObservesNativeArrivalOnce()
        {
            yield return new EnterPlayMode();
            BuildTestNavMesh();
            UseFramesDueEveryInterval();
            try
            {
                AiNavigation navigation = CreateNavigation(AiNavigation.MovementMode.FollowTarget, null);
                navigation.WalkSpeed = 2f;
                navigation.TriggerDistance = 0f;
                SetMinimumPathInterval(navigation);
                NavMeshAgent agent = navigation.GetComponent<NavMeshAgent>();
                Transform target = CreateObject("EveryFrameTarget", _origin + Vector3.right * 3f).transform;
                List<int> submissionFrames = new List<int>();
                List<float> submissionTimes = new List<float>();
                int arrivals = 0;
                int blocked = 0;
                navigation.onPathUpdated.AddListener(position =>
                {
                    submissionFrames.Add(Time.frameCount);
                    submissionTimes.Add(Time.time);
                });
                navigation.onPathBlocked = new UnityEvent<Vector3>();
                navigation.onPathBlocked.AddListener(position => blocked++);
                navigation.onDestinationReached.AddListener(position =>
                {
                    AssertNativeArrival(agent, target.position);
                    Assert.That(Vector3.Distance(position, target.position), Is.LessThan(0.01f));
                    Assert.That(navigation.AutoUpdatePath, Is.True);
                    Assert.That(navigation.HasReachedDestination, Is.True);
                    arrivals++;
                });

                // Editor polling can resume in the same frame; wait for a real captured frame and Start.
                yield return WaitForNextFrame(Time.frameCount, Time.realtimeSinceStartup + 5f);
                navigation.SetTarget(target);
                Assert.That(submissionFrames.Count, Is.EqualTo(1));
                Assert.That(arrivals, Is.Zero, "A fresh submission cannot report arrival synchronously.");
                Assert.That(blocked, Is.Zero);
                Assert.That(navigation.HasReachedDestination, Is.False);
                int previousFrame = Time.frameCount;
                float previousTime = Time.time;
                int observedFrames = 0;
                int framesAfterArrival = 0;
                bool moved = false;
                float deadline = Time.realtimeSinceStartup + 5f;
                while ((arrivals == 0 || framesAfterArrival < 4) && Time.realtimeSinceStartup < deadline)
                {
                    bool hadArrived = arrivals > 0;
                    yield return WaitForNextFrame(previousFrame, deadline);
                    AssertDueFrame(previousFrame, previousTime);
                    previousFrame = Time.frameCount;
                    previousTime = Time.time;
                    observedFrames++;
                    Assert.That(submissionFrames.Count, Is.EqualTo(observedFrames + 1),
                        "The interval must actually produce one accepted refresh on every observed frame.");
                    Assert.That(submissionFrames[submissionFrames.Count - 1], Is.EqualTo(Time.frameCount));
                    moved |= Vector3.Distance(agent.nextPosition, _origin) > 0.25f;
                    Assert.That(arrivals, Is.LessThanOrEqualTo(1));
                    Assert.That(navigation.AutoUpdatePath, Is.True);
                    if (arrivals > 0)
                    {
                        Assert.That(navigation.HasReachedDestination, Is.True);
                    }

                    if (hadArrived)
                    {
                        framesAfterArrival++;
                    }
                }

                Assert.That(moved, Is.True, "A native agent must actually travel to the target.");
                Assert.That(arrivals, Is.EqualTo(1), "Due refreshes cannot starve an eligible native arrival.");
                Assert.That(blocked, Is.Zero);
                Assert.That(framesAfterArrival, Is.EqualTo(4), "Stationary refreshes must preserve one arrival.");
                Assert.That(navigation.IsMoving, Is.False);
                for (int index = 1; index < submissionFrames.Count; index++)
                {
                    Assert.That(submissionFrames[index] - submissionFrames[index - 1], Is.EqualTo(1));
                    Assert.That(submissionTimes[index] - submissionTimes[index - 1],
                        Is.EqualTo(0.15f).Within(0.002f));
                }
            }
            finally
            {
                RestoreFrameTiming();
            }
        }

        [UnityTest]
        public IEnumerator ArrivalListenerStop_CancelsTheDueAutomaticRefresh()
        {
            yield return new EnterPlayMode();
            yield return VerifyArrivalCancellation(navigation =>
            {
                navigation.Stop();
                Assert.That(navigation.IsMoving, Is.False);
                Assert.That(navigation.HasReachedDestination, Is.False);
                Assert.That(navigation.GetComponent<NavMeshAgent>().isStopped, Is.True);
            });
        }

        [UnityTest]
        public IEnumerator ArrivalListenerDisable_CancelsTheDueAutomaticRefresh()
        {
            yield return new EnterPlayMode();
            yield return VerifyArrivalCancellation(navigation =>
            {
                navigation.enabled = false;
                Assert.That(navigation.isActiveAndEnabled, Is.False);
                Assert.That(navigation.HasReachedDestination, Is.True);
            });
        }

        [UnityTest]
        public IEnumerator ArrivalListenerDestroy_CancelsTheDueAutomaticRefresh()
        {
            yield return new EnterPlayMode();
            yield return VerifyArrivalCancellation(navigation =>
            {
                Object.DestroyImmediate(navigation);
                Assert.That(navigation == null, Is.True);
            });
        }

        [UnityTest]
        public IEnumerator ArrivalListenerReplacement_PreservesTheNewRequestAndItsArrival()
        {
            yield return new EnterPlayMode();
            BuildTestNavMesh();
            UseFramesDueEveryInterval();
            try
            {
                AiNavigation navigation = CreateNavigation(AiNavigation.MovementMode.FollowTarget, null);
                navigation.WalkSpeed = 2f;
                SetMinimumPathInterval(navigation);
                NavMeshAgent agent = navigation.GetComponent<NavMeshAgent>();
                Transform first = CreateObject("ArrivalTarget", _origin + Vector3.right).transform;
                Transform replacement = CreateObject("ArrivalReplacement", _origin + Vector3.right * 4f).transform;
                int arrivals = 0;
                int replacementSubmissions = 0;
                navigation.onPathUpdated.AddListener(position =>
                {
                    if (arrivals == 1 && Vector3.Distance(position, replacement.position) < 0.01f)
                    {
                        replacementSubmissions++;
                    }
                });
                navigation.onDestinationReached.AddListener(position =>
                {
                    AssertNativeArrival(agent, arrivals == 0 ? first.position : replacement.position);
                    arrivals++;
                    if (arrivals == 1)
                    {
                        Assert.That(Vector3.Distance(position, first.position), Is.LessThan(0.01f));
                        navigation.SetTarget(replacement);
                        Assert.That(replacementSubmissions, Is.EqualTo(1));
                        Assert.That(navigation.Target, Is.SameAs(replacement));
                        Assert.That(navigation.IsMoving, Is.True);
                        Assert.That(navigation.HasReachedDestination, Is.False);
                    }
                    else
                    {
                        Assert.That(Vector3.Distance(position, replacement.position), Is.LessThan(0.01f));
                    }
                });

                yield return WaitForNextFrame(Time.frameCount, Time.realtimeSinceStartup + 5f);
                navigation.SetTarget(first);
                int previousFrame = Time.frameCount;
                float previousTime = Time.time;
                int framesAfterArrival = 0;
                float deadline = Time.realtimeSinceStartup + 5f;
                while ((arrivals < 2 || framesAfterArrival < 4) && Time.realtimeSinceStartup < deadline)
                {
                    bool hadArrived = arrivals == 2;
                    yield return WaitForNextFrame(previousFrame, deadline);
                    AssertDueFrame(previousFrame, previousTime);
                    previousFrame = Time.frameCount;
                    previousTime = Time.time;
                    Assert.That(arrivals, Is.LessThanOrEqualTo(2));
                    if (hadArrived)
                    {
                        framesAfterArrival++;
                    }
                }

                Assert.That(arrivals, Is.EqualTo(2));
                Assert.That(framesAfterArrival, Is.EqualTo(4));
                Assert.That(navigation.Target, Is.SameAs(replacement));
                Assert.That(navigation.AutoUpdatePath, Is.True);
                Assert.That(navigation.HasReachedDestination, Is.True);
            }
            finally
            {
                RestoreFrameTiming();
            }
        }

        [UnityTest]
        public IEnumerator FollowTarget_DisableEnable_ResumesOnlyTheUpdateScheduler()
        {
            yield return new EnterPlayMode();
            BuildTestNavMesh();
            AiNavigation navigation = CreateNavigation(AiNavigation.MovementMode.FollowTarget, null);
            Transform target = CreateObject("EnableTarget", _origin + Vector3.right * 8f).transform;
            int submissions = 0;
            navigation.onPathUpdated.AddListener(position => submissions++);
            navigation.SetTarget(target);
            navigation.enabled = false;
            target.position += Vector3.left;

            yield return new WaitForSecondsRealtime(0.6f);
            Assert.That(submissions, Is.EqualTo(1));
            navigation.enabled = true;
            float deadline = Time.realtimeSinceStartup + 1f;
            while (submissions == 1 && Time.realtimeSinceStartup < deadline)
            {
                yield return null;
            }

            Assert.That(submissions, Is.EqualTo(2), "Re-enable must resume one scheduler without a startup resubmission.");
            Assert.That(navigation.Target, Is.SameAs(target));
        }

        [UnityTest]
        public IEnumerator SetTarget_PathUpdatedListenerStop_LeavesAgentAndPublicStateStopped()
        {
            yield return new EnterPlayMode();
            BuildTestNavMesh();
            AiNavigation navigation = CreateNavigation(AiNavigation.MovementMode.FollowTarget, null);
            NavMeshAgent agent = navigation.GetComponent<NavMeshAgent>();
            Transform target = CreateObject("CanceledTarget", _origin + Vector3.right * 4f).transform;
            int submissions = 0;
            int arrivals = 0;
            navigation.onPathUpdated.AddListener(position =>
            {
                submissions++;
                navigation.Stop();
            });
            navigation.onDestinationReached.AddListener(position => arrivals++);

            navigation.SetTarget(target);

            Assert.That(submissions, Is.EqualTo(1));
            Assert.That(navigation.IsMoving, Is.False);
            Assert.That(agent.isStopped, Is.True);
            Assert.That(navigation.Target, Is.Null);
            Assert.That(navigation.HasReachedDestination, Is.False);
            yield return new WaitForSecondsRealtime(0.6f);
            Assert.That(submissions, Is.EqualTo(1));
            Assert.That(arrivals, Is.Zero);
        }

        [UnityTest]
        public IEnumerator PathUpdatedReplacementThenStop_PreservesTheLatestCommand()
        {
            yield return new EnterPlayMode();
            BuildTestNavMesh();
            AiNavigation navigation = CreateNavigation(AiNavigation.MovementMode.FollowTarget, null);
            Transform first = CreateObject("FirstTarget", _origin + Vector3.right * 4f).transform;
            Transform replacement = CreateObject("ReplacementTarget", _origin + Vector3.right * 6f).transform;
            int submissions = 0;
            navigation.onPathUpdated.AddListener(position =>
            {
                submissions++;
                if (submissions == 1)
                {
                    navigation.SetTarget(replacement);
                }
                else
                {
                    navigation.Stop();
                }
            });

            navigation.SetTarget(first);

            Assert.That(submissions, Is.EqualTo(2));
            Assert.That(navigation.Target, Is.Null);
            Assert.That(navigation.IsMoving, Is.False);
            Assert.That(navigation.GetComponent<NavMeshAgent>().isStopped, Is.True);
            Assert.That(Vector3.Distance(navigation.GetComponent<NavMeshAgent>().destination, replacement.position),
                Is.LessThan(0.2f), "The old request cannot replace the accepted replacement path.");
            yield return new WaitForSecondsRealtime(0.6f);
            Assert.That(submissions, Is.EqualTo(2));
            Assert.That(navigation.IsMoving, Is.False);
        }

        [UnityTest]
        public IEnumerator AcceptedDestination_NativeResetPath_ReportsOneBlockedDestinationAndRecovers()
        {
            yield return new EnterPlayMode();
            BuildTestNavMesh();
            AiNavigation navigation = CreateNavigation(AiNavigation.MovementMode.FollowTarget, null);
            navigation.AutoUpdatePath = false;
            navigation.WalkSpeed = 1f;
            NavMeshAgent agent = navigation.GetComponent<NavMeshAgent>();
            Vector3 destination = _origin + Vector3.right * 8f;
            int blocked = 0;
            int arrivals = 0;
            int invalidStatuses = 0;
            Vector3 blockedDestination = Vector3.zero;
            navigation.onPathBlocked = new UnityEvent<Vector3>();
            navigation.onPathBlocked.AddListener(position =>
            {
                blocked++;
                blockedDestination = position;
            });
            navigation.onDestinationReached.AddListener(position => arrivals++);
            navigation.onPathStatusChanged = new UnityEvent<NavMeshPathStatus>();
            navigation.onPathStatusChanged.AddListener(status =>
            {
                if (status == NavMeshPathStatus.PathInvalid)
                {
                    invalidStatuses++;
                }
            });

            Assert.That(navigation.SetDestination(destination), Is.True);
            yield return WaitForNativePath(agent);
            Assert.That(blocked, Is.Zero, "Normal pending/complete path resolution must not emit blockage.");
            agent.ResetPath();
            Assert.That(agent.isOnNavMesh, Is.True);
            Assert.That(agent.pathPending, Is.False);
            Assert.That(agent.hasPath, Is.False, "The trigger must clear a real native path without reflection.");
            float deadline = Time.realtimeSinceStartup + 1f;
            while (blocked == 0 && Time.realtimeSinceStartup < deadline)
            {
                yield return null;
            }

            Assert.That(blocked, Is.EqualTo(1));
            Assert.That(invalidStatuses, Is.EqualTo(1));
            Assert.That(Vector3.Distance(blockedDestination, destination), Is.LessThan(0.2f));
            Assert.That(navigation.IsPathBlocked, Is.True);
            Assert.That(navigation.HasReachedDestination, Is.False);
            Assert.That(arrivals, Is.Zero);
            yield return new WaitForSecondsRealtime(0.25f);
            Assert.That(blocked, Is.EqualTo(1));
            Assert.That(invalidStatuses, Is.EqualTo(1));

            Assert.That(navigation.SetDestination(_origin + Vector3.right), Is.True);
            deadline = Time.realtimeSinceStartup + 3f;
            while (arrivals == 0 && Time.realtimeSinceStartup < deadline)
            {
                yield return null;
            }

            Assert.That(arrivals, Is.EqualTo(1));
            Assert.That(navigation.HasReachedDestination, Is.True);
            Assert.That(navigation.IsPathBlocked, Is.False);
            yield return null;
            Assert.That(blocked, Is.EqualTo(1));
        }

        [UnityTest]
        public IEnumerator BlockedListenerReplacement_SuppressesTheOldInvalidStatus()
        {
            yield return new EnterPlayMode();
            BuildTestNavMesh();
            AiNavigation navigation = CreateNavigation(AiNavigation.MovementMode.FollowTarget, null);
            navigation.AutoUpdatePath = false;
            navigation.WalkSpeed = 1f;
            NavMeshAgent agent = navigation.GetComponent<NavMeshAgent>();
            Vector3 replacement = _origin + Vector3.right * 6f;
            int blocked = 0;
            int staleInvalidStatuses = 0;
            navigation.onPathBlocked = new UnityEvent<Vector3>();
            navigation.onPathBlocked.AddListener(position =>
            {
                blocked++;
                Assert.That(navigation.SetDestination(replacement), Is.True);
            });
            navigation.onPathStatusChanged = new UnityEvent<NavMeshPathStatus>();
            navigation.onPathStatusChanged.AddListener(status =>
            {
                if (status == NavMeshPathStatus.PathInvalid)
                {
                    staleInvalidStatuses++;
                }
            });

            Assert.That(navigation.SetDestination(_origin + Vector3.right * 8f), Is.True);
            yield return WaitForNativePath(agent);
            agent.ResetPath();
            Assert.That(agent.hasPath, Is.False);
            float deadline = Time.realtimeSinceStartup + 1f;
            while (blocked == 0 && Time.realtimeSinceStartup < deadline)
            {
                yield return null;
            }

            Assert.That(blocked, Is.EqualTo(1));
            Assert.That(staleInvalidStatuses, Is.Zero,
                "The old request's status cannot be published after the blocked callback replaces it.");
            Assert.That(navigation.IsPathBlocked, Is.False);
            Assert.That(navigation.IsMoving, Is.True);
            Assert.That(Vector3.Distance(agent.destination, replacement), Is.LessThan(0.2f));
        }

        [UnityTest]
        public IEnumerator IdleAndStoppedNativeResetPath_DoNotReportBlocked()
        {
            yield return new EnterPlayMode();
            BuildTestNavMesh();
            AiNavigation navigation = CreateNavigation(AiNavigation.MovementMode.FollowTarget, null);
            navigation.AutoUpdatePath = false;
            NavMeshAgent agent = navigation.GetComponent<NavMeshAgent>();
            int blocked = 0;
            navigation.onPathBlocked = new UnityEvent<Vector3>();
            navigation.onPathBlocked.AddListener(position => blocked++);
            navigation.Resume();
            agent.ResetPath();
            yield return null;
            yield return null;
            Assert.That(blocked, Is.Zero, "An idle agent with no path has no failed request.");

            Assert.That(navigation.SetDestination(_origin + Vector3.right * 8f), Is.True);
            navigation.Stop();
            agent.ResetPath();
            yield return null;
            yield return null;

            Assert.That(blocked, Is.Zero, "Explicit stop cancels no-path observation for the old request.");
            Assert.That(navigation.IsMoving, Is.False);
            Assert.That(navigation.HasReachedDestination, Is.False);
        }

        [UnityTest]
        public IEnumerator AlreadyAtAcceptedDestination_DoesNotReportNoPathBlocked()
        {
            yield return new EnterPlayMode();
            BuildTestNavMesh();
            AiNavigation navigation = CreateNavigation(AiNavigation.MovementMode.FollowTarget, null);
            navigation.AutoUpdatePath = false;
            int blocked = 0;
            int arrivals = 0;
            navigation.onPathBlocked = new UnityEvent<Vector3>();
            navigation.onPathBlocked.AddListener(position => blocked++);
            navigation.onDestinationReached.AddListener(position => arrivals++);

            Assert.That(navigation.SetDestination(navigation.GetComponent<NavMeshAgent>().nextPosition), Is.True);
            float deadline = Time.realtimeSinceStartup + 1f;
            while (arrivals == 0 && blocked == 0 && Time.realtimeSinceStartup < deadline)
            {
                yield return null;
            }

            Assert.That(arrivals, Is.EqualTo(1));
            Assert.That(blocked, Is.Zero);
            Assert.That(navigation.HasReachedDestination, Is.True);
        }

        [UnityTest]
        public IEnumerator CombinedMode_StartFollowingListenerStop_CancelsTheTransition()
        {
            yield return new EnterPlayMode();
            BuildTestNavMesh();
            AiNavigation navigation = CreateNavigation(AiNavigation.MovementMode.Combined, null);
            Transform target = CreateObject("CombinedTarget", _origin + Vector3.right * 5f).transform;
            bool followingStarted = false;
            navigation.onStartFollowing = new UnityEvent();
            navigation.onStartFollowing.AddListener(() =>
            {
                followingStarted = true;
                navigation.Stop();
            });
            navigation.SetTarget(target);
            float deadline = Time.realtimeSinceStartup + 1f;
            while (!followingStarted && Time.realtimeSinceStartup < deadline)
            {
                yield return null;
            }

            Assert.That(followingStarted, Is.True);
            Assert.That(navigation.IsMoving, Is.False);
            Assert.That(navigation.GetComponent<NavMeshAgent>().isStopped, Is.True);
            Assert.That(navigation.HasReachedDestination, Is.False);
        }

        [UnityTest]
        public IEnumerator MovementRequests_AfterStop_ResumeAgent_AndDisabledAgentRejectsWarp()
        {
            yield return new EnterPlayMode();
            BuildTestNavMesh();
            AiNavigation navigation = CreateNavigation(AiNavigation.MovementMode.FollowTarget, null);
            NavMeshAgent agent = navigation.GetComponent<NavMeshAgent>();
            Transform target = CreateObject("FollowTarget", _origin + Vector3.right * 4f).transform;
            Assert.That(navigation.SetDestination(target.position), Is.True);
            navigation.Stop();
            Assert.That(agent.isStopped, Is.True);

            Assert.That(navigation.SetDestination(target.position), Is.True);
            Assert.That(agent.isStopped, Is.False);
            Assert.That(navigation.IsMoving, Is.True);
            navigation.Stop();
            navigation.SetTarget(target);
            Assert.That(agent.isStopped, Is.False);
            Assert.That(navigation.Target, Is.SameAs(target));

            navigation.SetMovementMode(AiNavigation.MovementMode.FollowTarget);
            Assert.That(navigation.Target, Is.SameAs(target), "Changing mode must preserve the follow target.");
            agent.enabled = false;
            navigation.Stop();
            navigation.Resume();

            Assert.That(navigation.WarpToPosition(_origin), Is.False);
            Assert.That(navigation.IsMoving, Is.False);
            Assert.That(navigation.PathStatus, Is.EqualTo(NavMeshPathStatus.PathInvalid));
            Assert.That(navigation.CurrentSpeed, Is.Zero);
            Assert.That(navigation.HasReachedDestination, Is.False);
        }

        [UnityTest]
        public IEnumerator PositionDestination_EmitsPathAndArrival_AndReportsReachedState()
        {
            yield return new EnterPlayMode();
            BuildTestNavMesh();
            AiNavigation navigation = CreateNavigation(AiNavigation.MovementMode.FollowTarget, null);
            int pathUpdates = 0;
            int arrivals = 0;
            Vector3 reached = Vector3.zero;
            navigation.onPathUpdated.AddListener(position => pathUpdates++);
            navigation.onDestinationReached.AddListener(position =>
            {
                arrivals++;
                reached = position;
            });
            Vector3 destination = _origin + Vector3.right * 3f;

            Assert.That(navigation.SetDestination(destination), Is.True);
            Assert.That(pathUpdates, Is.EqualTo(1));
            Assert.That(navigation.HasReachedDestination, Is.False);
            float deadline = Time.realtimeSinceStartup + 3f;
            while (arrivals == 0 && Time.realtimeSinceStartup < deadline)
            {
                yield return null;
            }

            Assert.That(arrivals, Is.EqualTo(1));
            Assert.That(Vector3.Distance(reached, destination), Is.LessThan(0.2f));
            Assert.That(navigation.Target, Is.Null);
            Assert.That(navigation.HasReachedDestination, Is.True);
            yield return null;
            Assert.That(arrivals, Is.EqualTo(1), "Arrival must be emitted once for the accepted destination.");
            navigation.Stop();
            Assert.That(navigation.HasReachedDestination, Is.False);
        }

        [UnityTest]
        public IEnumerator PartialPositionPath_ReportsBlockedWithoutSuccessfulArrival()
        {
            yield return new EnterPlayMode();
            BuildTestNavMesh(true);
            AiNavigation navigation = CreateNavigation(AiNavigation.MovementMode.FollowTarget, null);
            NavMeshAgent agent = navigation.GetComponent<NavMeshAgent>();
            int arrivals = 0;
            int blocked = 0;
            navigation.onPathBlocked = new UnityEvent<Vector3>();
            navigation.onPathBlocked.AddListener(position => blocked++);
            navigation.onDestinationReached.AddListener(position => arrivals++);

            Assert.That(navigation.SetDestination(_origin + Vector3.right * 10f), Is.True);
            float deadline = Time.realtimeSinceStartup + 3f;
            while ((agent.pathPending || !agent.hasPath || agent.remainingDistance > agent.stoppingDistance) &&
                   Time.realtimeSinceStartup < deadline)
            {
                yield return null;
            }

            yield return null;
            Assert.That(navigation.PathStatus, Is.EqualTo(NavMeshPathStatus.PathPartial));
            Assert.That(agent.remainingDistance, Is.LessThanOrEqualTo(agent.stoppingDistance));
            Assert.That(navigation.IsPathBlocked, Is.True);
            Assert.That(blocked, Is.EqualTo(1));
            Assert.That(arrivals, Is.Zero);
            Assert.That(navigation.HasReachedDestination, Is.False);
        }

        [UnityTest]
        public IEnumerator MovingFollowTarget_ReopensArrivalState_AndEmitsAnotherArrival()
        {
            yield return new EnterPlayMode();
            BuildTestNavMesh();
            AiNavigation navigation = CreateNavigation(AiNavigation.MovementMode.FollowTarget, null);
            Transform target = CreateObject("MovingTarget", _origin + Vector3.right * 2f).transform;
            int arrivals = 0;
            navigation.onDestinationReached.AddListener(position => arrivals++);
            navigation.SetTarget(target);
            float deadline = Time.realtimeSinceStartup + 3f;
            while (arrivals == 0 && Time.realtimeSinceStartup < deadline)
            {
                yield return null;
            }

            Assert.That(arrivals, Is.EqualTo(1));
            Assert.That(navigation.HasReachedDestination, Is.True);
            target.position += Vector3.right * 3f;
            deadline = Time.realtimeSinceStartup + 3f;
            while (arrivals < 2 && Time.realtimeSinceStartup < deadline)
            {
                yield return null;
            }

            Assert.That(arrivals, Is.EqualTo(2), "Following a moved target must re-arm arrival detection.");
            Assert.That(navigation.HasReachedDestination, Is.True);
            Assert.That(Vector3.Distance(navigation.transform.position, target.position), Is.LessThan(0.3f));
        }

        [UnityTest]
        public IEnumerator StopPatrol_DuringWait_CancelsDelayedMovement()
        {
            yield return new EnterPlayMode();
            yield return VerifyPatrolCancellation(navigation => navigation.StopPatrol(), false);
        }

        [UnityTest]
        public IEnumerator Stop_DuringWait_RemainsPausedUntilANewRequest()
        {
            yield return new EnterPlayMode();
            yield return VerifyPatrolCancellation(navigation => navigation.Stop(), true);
        }

        [UnityTest]
        public IEnumerator Disable_DuringWait_CancelsDelayedMovement()
        {
            yield return new EnterPlayMode();
            yield return VerifyPatrolCancellation(navigation => navigation.enabled = false, true);
        }

        [UnityTest]
        public IEnumerator ClearingWaypoints_DuringWait_StopsTheRoute()
        {
            yield return new EnterPlayMode();
            yield return VerifyPatrolCancellation(navigation => navigation.SetPatrolPoints(null), false);
        }

        [UnityTest]
        public IEnumerator RouteWithNullWaypoints_SkipsToAValidWaypoint()
        {
            yield return new EnterPlayMode();
            BuildTestNavMesh();
            Transform waypoint = CreateObject("ValidWaypoint", _origin + Vector3.right * 3f).transform;
            AiNavigation navigation = CreateNavigation(AiNavigation.MovementMode.Patrol,
                new Transform[] { null, waypoint, null });

            navigation.StartPatrol();

            Assert.That(navigation.IsPatrolling, Is.True);
            Assert.That(navigation.CurrentPatrolIndex, Is.EqualTo(1));
            Assert.That(navigation.IsMoving, Is.True);
            Assert.That(Vector3.Distance(navigation.GetComponent<NavMeshAgent>().destination, waypoint.position),
                Is.LessThan(0.2f));
        }

        private IEnumerator VerifyArrivalCancellation(Action<AiNavigation> cancel)
        {
            Assert.That(Application.isPlaying, Is.True, "The top-level UnityTest must admit native callbacks first.");
            BuildTestNavMesh();
            UseFramesDueEveryInterval();
            try
            {
                AiNavigation navigation = CreateNavigation(AiNavigation.MovementMode.FollowTarget, null);
                navigation.WalkSpeed = 2f;
                SetMinimumPathInterval(navigation);
                NavMeshAgent agent = navigation.GetComponent<NavMeshAgent>();
                Transform target = CreateObject("CanceledArrivalTarget", _origin + Vector3.right * 3f).transform;
                int submissions = 0;
                int arrivals = 0;
                int submissionsAtArrival = 0;
                navigation.onPathUpdated.AddListener(position => submissions++);
                navigation.onDestinationReached.AddListener(position =>
                {
                    AssertNativeArrival(agent, target.position);
                    Assert.That(navigation.AutoUpdatePath, Is.True);
                    arrivals++;
                    submissionsAtArrival = submissions;
                    cancel(navigation);
                });

                yield return WaitForNextFrame(Time.frameCount, Time.realtimeSinceStartup + 5f);
                navigation.SetTarget(target);
                int previousFrame = Time.frameCount;
                float previousTime = Time.time;
                int framesAfterArrival = 0;
                float deadline = Time.realtimeSinceStartup + 5f;
                while ((arrivals == 0 || framesAfterArrival < 4) && Time.realtimeSinceStartup < deadline)
                {
                    bool hadArrived = arrivals > 0;
                    yield return WaitForNextFrame(previousFrame, deadline);
                    AssertDueFrame(previousFrame, previousTime);
                    previousFrame = Time.frameCount;
                    previousTime = Time.time;
                    if (arrivals > 0)
                    {
                        Assert.That(submissions, Is.EqualTo(submissionsAtArrival),
                            "An arrival observer must cancel this frame's due refresh and later automatic work.");
                    }

                    if (hadArrived)
                    {
                        framesAfterArrival++;
                    }
                }

                Assert.That(arrivals, Is.EqualTo(1));
                Assert.That(submissionsAtArrival, Is.GreaterThan(1), "Automatic refresh must precede cancellation.");
                Assert.That(framesAfterArrival, Is.EqualTo(4));
                Assert.That(agent.isOnNavMesh, Is.True, "Destroying the adapter must leave its independent agent intact.");
            }
            finally
            {
                RestoreFrameTiming();
            }
        }

        private void UseFramesDueEveryInterval()
        {
            _savedFrameTimeScale = Time.timeScale;
            _savedCaptureDeltaTime = Time.captureDeltaTime;
            _savedMaximumDeltaTime = Time.maximumDeltaTime;
            _hasFrameTimingOverride = true;
            Time.timeScale = 1f;
            Time.maximumDeltaTime = Mathf.Max(_savedMaximumDeltaTime, 0.15f);
            Time.captureDeltaTime = 0.15f;
        }

        private void RestoreFrameTiming()
        {
            if (!_hasFrameTimingOverride)
            {
                return;
            }

            Time.captureDeltaTime = _savedCaptureDeltaTime;
            Time.maximumDeltaTime = _savedMaximumDeltaTime;
            Time.timeScale = _savedFrameTimeScale;
            _hasFrameTimingOverride = false;
        }

        private static void SetMinimumPathInterval(AiNavigation navigation)
        {
            using (SerializedObject authoring = new SerializedObject(navigation))
            {
                authoring.FindProperty("pathUpdateInterval").floatValue = 0.1f;
                authoring.ApplyModifiedPropertiesWithoutUndo();
            }
        }

        private static void AssertDueFrame(int previousFrame, float previousTime)
        {
            Assert.That(Time.frameCount - previousFrame, Is.EqualTo(1), "Observe consecutive real PlayerLoop frames.");
            Assert.That(Time.time - previousTime, Is.EqualTo(0.15f).Within(0.002f));
            Assert.That(Time.time - previousTime, Is.GreaterThanOrEqualTo(0.1f),
                "The configured refresh interval must be due on every observed frame.");
            Assert.That(Time.deltaTime, Is.EqualTo(0.15f).Within(0.002f));
        }

        private static IEnumerator WaitForNextFrame(int previousFrame, float deadline)
        {
            while (Time.frameCount == previousFrame && Time.realtimeSinceStartup < deadline)
            {
                yield return null;
            }

            Assert.That(Time.frameCount, Is.GreaterThan(previousFrame), "Timed out waiting for a native PlayerLoop frame.");
        }

        private static void AssertNativeArrival(NavMeshAgent agent, Vector3 destination)
        {
            Assert.That(agent.isOnNavMesh, Is.True);
            Assert.That(agent.pathPending, Is.False, "Arrival must observe a settled native request.");
            if (agent.hasPath)
            {
                Assert.That(agent.pathStatus, Is.EqualTo(NavMeshPathStatus.PathComplete));
                Assert.That(agent.remainingDistance, Is.LessThanOrEqualTo(agent.stoppingDistance));
            }

            Assert.That(Vector3.Distance(agent.nextPosition, destination), Is.LessThan(0.2f));
        }

        private IEnumerator VerifyPatrolCancellation(Action<AiNavigation> cancel, bool expectPatrolling)
        {
            Assert.That(Application.isPlaying, Is.True, "The top-level UnityTest must admit native patrol first.");
            BuildTestNavMesh();
            Transform first = CreateObject("FirstWaypoint", _origin + Vector3.right).transform;
            Transform second = CreateObject("SecondWaypoint", _origin + Vector3.right * 4f).transform;
            AiNavigation navigation = CreateNavigation(AiNavigation.MovementMode.Patrol,
                new Transform[] { first, second });
            using (SerializedObject authoring = new SerializedObject(navigation))
            {
                authoring.FindProperty("patrolWaitTime").floatValue = 0.2f;
                authoring.ApplyModifiedPropertiesWithoutUndo();
            }
            int reachedPoints = 0;
            navigation.onPatrolPointReached.AddListener(index => reachedPoints++);
            float deadline = Time.realtimeSinceStartup + 3f;
            while (reachedPoints == 0 && Time.realtimeSinceStartup < deadline)
            {
                yield return null;
            }

            Assert.That(reachedPoints, Is.EqualTo(1), "The test must interrupt a real waypoint wait.");
            cancel(navigation);
            double canceledScaledTime = Time.timeAsDouble;
            int canceledFrame = Time.frameCount;
            int observedFrame = canceledFrame;
            float postCancelDeadline = Time.realtimeSinceStartup + 3f;
            while (Time.timeAsDouble - canceledScaledTime < 0.35d &&
                   Time.realtimeSinceStartup < postCancelDeadline)
            {
                yield return WaitForNextFrame(observedFrame, postCancelDeadline);
                observedFrame = Time.frameCount;
            }

            Assert.That(Time.frameCount, Is.GreaterThan(canceledFrame),
                "Post-cancel observation requires actual native frame progression.");
            Assert.That(Time.timeAsDouble - canceledScaledTime, Is.GreaterThanOrEqualTo(0.35d),
                "Native scaled time must exceed the authored 0.2-second patrol wait before observing cancellation.");
            // Give an eligible delayed continuation another native frame before asserting its absence.
            yield return WaitForNextFrame(Time.frameCount, postCancelDeadline);
            Assert.That(Time.realtimeSinceStartup, Is.LessThan(postCancelDeadline),
                "Post-cancel native/scaled observation must complete within the realtime deadline.");

            Assert.That(navigation.CurrentPatrolIndex, Is.Zero, "A canceled wait must not advance the old route.");
            Assert.That(navigation.IsMoving, Is.False);
            Assert.That(navigation.IsPatrolling, Is.EqualTo(expectPatrolling));
            Assert.That(reachedPoints, Is.EqualTo(1));

            if (expectPatrolling && navigation.enabled)
            {
                navigation.Resume();
                deadline = Time.realtimeSinceStartup + 2f;
                while (navigation.CurrentPatrolIndex == 0 && Time.realtimeSinceStartup < deadline)
                {
                    yield return null;
                }

                Assert.That(navigation.CurrentPatrolIndex, Is.EqualTo(1),
                    "Resuming a canceled waypoint wait must continue the retained route.");
            }
        }

        private AiNavigation CreateNavigation(AiNavigation.MovementMode mode, Transform[] waypoints)
        {
            Assert.That(Application.isPlaying, Is.True, "Build and attach the native agent in the admitted Play session.");
            Time.timeScale = 1f;
            GameObject gameObject = CreateObject("Navigation", _origin);
            gameObject.SetActive(false);
            NavMeshAgent agent = gameObject.AddComponent<NavMeshAgent>();
            agent.agentTypeID = _agentTypeId;
            AiNavigation navigation = gameObject.AddComponent<AiNavigation>();
            navigation.WalkSpeed = 10f;
            navigation.Acceleration = 100f;
            navigation.StoppingDistance = 0.1f;
            navigation.onPathUpdated = new UnityEvent<Vector3>();
            navigation.onDestinationReached = new UnityEvent<Vector3>();
            navigation.onPatrolPointReached = new UnityEvent<int>();
            navigation.SetPatrolPoints(waypoints);
            navigation.SetMovementMode(mode);
            gameObject.SetActive(true);
            Assert.That(navigation.IsOnNavMesh, Is.True);
            return navigation;
        }

        private static IEnumerator WaitForNativePath(NavMeshAgent agent)
        {
            float deadline = Time.realtimeSinceStartup + 3f;
            while ((agent.pathPending || !agent.hasPath) && Time.realtimeSinceStartup < deadline)
            {
                yield return null;
            }

            Assert.That(agent.isOnNavMesh, Is.True);
            Assert.That(agent.pathPending, Is.False);
            Assert.That(agent.hasPath, Is.True, "Native no-path acceptance must begin with an actual accepted path.");
            Assert.That(agent.remainingDistance, Is.GreaterThan(agent.stoppingDistance));
        }

        private GameObject CreateObject(string name, Vector3 position)
        {
            GameObject gameObject = new GameObject(name);
            gameObject.transform.position = position;
            _objects.Add(gameObject);
            return gameObject;
        }

        private void BuildTestNavMesh(bool disconnected = false)
        {
            Assert.That(NavMesh.GetSettingsCount(), Is.GreaterThan(0));
            NavMeshBuildSettings settings = NavMesh.GetSettingsByIndex(0);
            _agentTypeId = settings.agentTypeID;
            NavMeshBuildSource floor = new NavMeshBuildSource
            {
                shape = NavMeshBuildSourceShape.Box,
                transform = Matrix4x4.Translate(_origin + Vector3.down * 0.1f),
                size = disconnected ? new Vector3(6f, 0.2f, 6f) : new Vector3(30f, 0.2f, 30f),
                area = 0
            };
            List<NavMeshBuildSource> sources = new List<NavMeshBuildSource> { floor };
            if (disconnected)
            {
                NavMeshBuildSource secondFloor = floor;
                secondFloor.transform = Matrix4x4.Translate(_origin + Vector3.right * 10f + Vector3.down * 0.1f);
                sources.Add(secondFloor);
            }

            _data = NavMeshBuilder.BuildNavMeshData(settings, sources,
                new Bounds(_origin, new Vector3(32f, 8f, 32f)), Vector3.zero, Quaternion.identity);
            Assert.That(_data, Is.Not.Null);
            _instance = NavMesh.AddNavMeshData(_data);
            Assert.That(_instance.valid, Is.True);
        }
    }
}
#pragma warning restore CS0618
