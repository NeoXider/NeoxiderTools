using System;
using System.Collections.Generic;
using System.Reflection;
using System.Text.RegularExpressions;
using Neo.NPC.Combat;
using Neo.NPC.Navigation;
using Neo.Rpg;
using Neo.Rpg.Components;
using Neo.Tests.Performance;
using NUnit.Framework;
using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.TestTools;
using Object = UnityEngine.Object;

namespace Neo.NPC.Tests
{
    /// <summary>
    ///     Guards the NPC per-frame paths (navigation tick allocation, chase continuation, animator parameter
    ///     validation) without a native NavMesh. The agent here is off any NavMesh, so the allocation case covers the
    ///     component Update and the core's off-mesh guard; ticks on a real path are measured by
    ///     Audit20261023NeoNPCNavigationTests.Follow_NativeSettledAndRepathTicks_DoNotAllocate.
    /// </summary>
    public sealed class NpcHotPathAndChaseTests
    {
        private const int Iterations = 200;

        private readonly List<Object> _objects = new();

        [TearDown]
        public void TearDown()
        {
            for (int i = _objects.Count - 1; i >= 0; i--)
            {
                if (_objects[i] is GameObject host)
                {
                    Object.DestroyImmediate(host);
                }
            }

            for (int i = _objects.Count - 1; i >= 0; i--)
            {
                if (_objects[i] != null)
                {
                    Object.DestroyImmediate(_objects[i]);
                }
            }

            _objects.Clear();
        }

        [TestCase(NpcNavigation.NavigationMode.FollowTarget)]
        [TestCase(NpcNavigation.NavigationMode.Combined)]
        public void Navigation_WarmTick_DoesNotAllocate(NpcNavigation.NavigationMode mode)
        {
            GameObject host = CreateObject("Allocation NPC");
            host.SetActive(false);
            NpcNavigation navigation = host.AddComponent<NpcNavigation>();
            navigation.SetMode(mode);
            InvokeLifecycle(navigation, "Awake");
            Transform target = CreateObject("Allocation target").transform;
            target.position = new Vector3(3f, 0f, 0f);
            navigation.SetFollowTarget(target);
            navigation.SetCombinedTarget(target);
            MethodInfo updateMethod = typeof(NpcNavigation).GetMethod("Update",
                BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.That(updateMethod, Is.Not.Null);
            Action update = (Action)Delegate.CreateDelegate(typeof(Action), navigation, updateMethod);

            for (int i = 0; i < 8; i++)
            {
                update();
            }

            using GcAllocCounter allocationCounter = GcAllocCounter.CreateOrIgnore();
            allocationCounter.Begin();
            for (int i = 0; i < Iterations; i++)
            {
                update();
            }

            long allocated = allocationCounter.End();
            Assert.That(allocated, Is.Zero,
                "The component Update up to the off-mesh guard must not allocate (cached constrain delegate).");
        }

        [Test]
        public void Combat_SustainedChase_StartsOnceWithoutRetargetingEveryDecision()
        {
            NpcRpgCombatBrain brain = CreateCombatNpc(5f, out NpcNavigation navigation, out GameObject target);
            UnityEvent chaseStarted = brain.OnChaseStarted;
            int chases = 0;
            chaseStarted.AddListener(() => chases++);
            navigation.onTargetChanged = new UnityEvent<Transform>();
            navigation.onSpeedChanged = new UnityEvent<float>();
            int retargets = 0;
            int speedChanges = 0;
            navigation.onTargetChanged.AddListener(_ => retargets++);
            navigation.onSpeedChanged.AddListener(_ => speedChanges++);

            brain.EvaluateNow();
            brain.EvaluateNow();
            brain.EvaluateNow();

            Assert.That(navigation.FollowTarget, Is.SameAs(target.transform));
            Assert.That(navigation.IsActive, Is.True);
            Assert.That(chases, Is.EqualTo(1), "A continuing chase is not a new chase start.");
            Assert.That(retargets, Is.EqualTo(1), "An unchanged target must not reset the follow path each decision.");
            Assert.That(speedChanges, Is.EqualTo(1));
        }

        [Test]
        public void Combat_ChaseAfterHold_StartsAgainAndResumesNavigation()
        {
            NpcRpgCombatBrain brain = CreateCombatNpc(5f, out NpcNavigation navigation, out GameObject target);
            UnityEvent chaseStarted = brain.OnChaseStarted;
            int chases = 0;
            chaseStarted.AddListener(() => chases++);
            brain.EvaluateNow();
            Assert.That(chases, Is.EqualTo(1));

            target.transform.position = brain.transform.position + Vector3.right;
            brain.EvaluateNow();
            Assert.That(navigation.IsActive, Is.False, "Inside attack range without a usable attack the NPC holds.");

            target.transform.position = brain.transform.position + Vector3.right * 5f;
            brain.EvaluateNow();

            Assert.That(chases, Is.EqualTo(2));
            Assert.That(navigation.IsActive, Is.True);
            Assert.That(navigation.FollowTarget, Is.SameAs(target.transform));
        }

        [Test]
        public void Combat_ReceiverAddedAfterAwake_StillGatesActions()
        {
            NpcRpgCombatBrain brain = CreateCombatNpc(5f, out NpcNavigation _, out GameObject _);
            brain.EvaluateNow();
            Assert.That(brain.CurrentDecision, Is.EqualTo(NpcCombatDecisionCore.Decision.ChaseTarget),
                "Without a receiver the brain may act.");

            RpgCharacter lateReceiver = brain.gameObject.AddComponent<RpgCharacter>();
            SetPrivate(lateReceiver, "_isDead", true);
            Assume.That(lateReceiver.CanPerformActions, Is.False);

            brain.EvaluateNow();

            Assert.That(brain.CurrentDecision, Is.EqualTo(NpcCombatDecisionCore.Decision.HoldPosition),
                "A receiver added after Awake must be found; a dead NPC must stop chasing.");
        }

        [Test]
        public void Navigation_AutoUpdatePathSetter_ReconfiguresTheRunningFollowCore()
        {
            GameObject host = CreateObject("Manual path NPC");
            host.SetActive(false);
            NpcNavigation navigation = host.AddComponent<NpcNavigation>();
            InvokeLifecycle(navigation, "Awake");
            Transform target = CreateObject("Manual path target").transform;
            navigation.SetFollowTarget(target);
            object followCore = GetPrivate(navigation, "followCore");

            navigation.AutoUpdatePath = false;

            Assert.That(navigation.AutoUpdatePath, Is.False);
            Assert.That(GetPrivate(followCore, "autoUpdatePath"), Is.False,
                "The follow core must stop periodic re-paths without a mode re-entry.");
            Assert.That(navigation.RefreshPath(), Is.False, "Without a NavMesh there is no path to refresh.");
            navigation.SetMode(NpcNavigation.NavigationMode.Patrol);
            Assert.That(navigation.RefreshPath(), Is.False, "Patrol has no follow path to refresh.");
        }

        [Test]
        public void Animation_MissingParameters_AreSkippedAndControllerSwapRevalidates()
        {
            GameObject host = CreateObject("Animated NPC");
            Animator animator = host.AddComponent<Animator>();
            AnimatorController movingOnly = CreateController(false);
            animator.runtimeAnimatorController = movingOnly;
            animator.Rebind();
            animator.SetBool("IsMoving", true);
            NpcAnimationCore core = new(null, animator, "Speed", "IsMoving", 0.1f);

            core.Tick(0.1f);

            Assert.That(core.HasSpeedParameter, Is.False);
            Assert.That(core.HasIsMovingParameter, Is.True);
            Assert.That(animator.GetBool("IsMoving"), Is.False, "Existing parameters are still driven.");

            AnimatorController full = CreateController(true);
            animator.runtimeAnimatorController = full;
            animator.Rebind();
            animator.SetFloat("Speed", 1f);

            core.Tick(0.1f);

            Assert.That(core.HasSpeedParameter, Is.True, "A replaced controller is validated again.");
            Assert.That(animator.GetFloat("Speed"), Is.InRange(0f, 0.5f));
            LogAssert.NoUnexpectedReceived();
        }

        [Test]
        public void Animation_MissingParameter_WarnsOncePerControllerWhenWarningsAreEnabled()
        {
            bool previousWarnings = NeoDiagnostics.RuntimeWarningsEnabled;
            NeoDiagnostics.Configure(warnings: true);
            try
            {
                GameObject host = CreateObject("Misconfigured NPC");
                Animator animator = host.AddComponent<Animator>();
                animator.runtimeAnimatorController = CreateController(false);
                animator.Rebind();
                NpcAnimationCore core = new(null, animator, "Speed", "IsMoving", 0.1f);
                LogAssert.Expect(LogType.Warning, new Regex(@"no Float parameter 'Speed'"));

                core.Tick(0.1f);
                core.Tick(0.1f);
                core.Tick(0.1f);

                Assert.That(core.HasSpeedParameter, Is.False);
                LogAssert.NoUnexpectedReceived();
            }
            finally
            {
                NeoDiagnostics.Configure(warnings: previousWarnings);
            }
        }

        [Test]
        public void Animation_WithoutController_IsIdle()
        {
            GameObject host = CreateObject("Controller-less NPC");
            Animator animator = host.AddComponent<Animator>();
            NpcAnimationCore core = new(null, animator, "Speed", "IsMoving", 0.1f);

            core.Tick(0.1f);

            Assert.That(core.HasSpeedParameter, Is.False);
            Assert.That(core.HasIsMovingParameter, Is.False);
            LogAssert.NoUnexpectedReceived();
        }

        private AnimatorController CreateController(bool withSpeed)
        {
            AnimatorController controller = Own(new AnimatorController());
            if (withSpeed)
            {
                controller.AddParameter("Speed", AnimatorControllerParameterType.Float);
            }

            controller.AddParameter("IsMoving", AnimatorControllerParameterType.Bool);
            controller.AddLayer("Base Layer");
            Own(controller.layers[0].stateMachine);
            return controller;
        }

        private NpcRpgCombatBrain CreateCombatNpc(float targetDistance, out NpcNavigation navigation,
            out GameObject target)
        {
            Vector3 origin = new(9100f, 500f, 9100f);
            GameObject host = CreateObject("Chasing NPC");
            host.SetActive(false);
            host.transform.position = origin;
            navigation = host.AddComponent<NpcNavigation>();
            InvokeLifecycle(navigation, "Awake");
            RpgTargetSelector selector = host.AddComponent<RpgTargetSelector>();
            using (SerializedObject settings = new(selector))
            {
                settings.FindProperty("_query._targetLayers").intValue = 1 << 31;
                settings.FindProperty("_query._use2D").boolValue = false;
                settings.ApplyModifiedPropertiesWithoutUndo();
            }

            host.AddComponent<RpgAttackController>();
            NpcRpgCombatBrain brain = host.AddComponent<NpcRpgCombatBrain>();
            NpcCombatPreset preset = Own(ScriptableObject.CreateInstance<NpcCombatPreset>());
            using (SerializedObject settings = new(brain))
            {
                settings.FindProperty("_preset").objectReferenceValue = preset;
                settings.ApplyModifiedPropertiesWithoutUndo();
            }

            brain.AutoResolveReferences();
            target = CreateObject("Chase target");
            target.layer = 31;
            target.transform.position = origin + Vector3.right * targetDistance;
            target.AddComponent<RpgCharacter>();
            target.AddComponent<BoxCollider>();
            Physics.SyncTransforms();
            return brain;
        }

        private GameObject CreateObject(string name)
        {
            return Own(new GameObject(name));
        }

        private T Own<T>(T value) where T : Object
        {
            _objects.Add(value);
            return value;
        }

        private static void InvokeLifecycle(MonoBehaviour component, string name)
        {
            MethodInfo callback = component.GetType().GetMethod(name, BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.That(callback, Is.Not.Null);
            callback.Invoke(component, null);
        }

        private static void SetPrivate(object target, string name, object value)
        {
            FieldInfo field = target.GetType().GetField(name, BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.That(field, Is.Not.Null, $"Field {name} is missing.");
            field.SetValue(target, value);
        }

        private static object GetPrivate(object target, string name)
        {
            FieldInfo field = target.GetType().GetField(name, BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.That(field, Is.Not.Null, $"Field {name} is missing.");
            return field.GetValue(target);
        }
    }
}
