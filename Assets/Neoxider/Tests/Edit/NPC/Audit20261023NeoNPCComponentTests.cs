using System.Collections.Generic;
using System.Reflection;
using Neo.NPC.Combat;
using Neo.NPC.Navigation;
using Neo.Rpg;
using Neo.Rpg.Components;
using NUnit.Framework;
using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;
using UnityEngine.AI;

namespace Neo.NPC.Tests
{
    public sealed class Audit20261023NeoNPCComponentTests
    {
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

        [Test]
        public void Navigation_PreAwakeConfiguration_PreservesTargetModeAndRunningSpeed()
        {
            GameObject host = CreateObject("Uninitialized NPC");
            host.SetActive(false);
            NpcNavigation navigation = host.AddComponent<NpcNavigation>();
            Transform target = CreateObject("Configured target").transform;

            navigation.SetFollowTarget(target);
            navigation.SetMode(NpcNavigation.NavigationMode.Patrol);
            navigation.SetRunning(true);
            InvokeLifecycle(navigation, "Awake");

            Assert.That(navigation.FollowTarget, Is.SameAs(target));
            Assert.That(navigation.Mode, Is.EqualTo(NpcNavigation.NavigationMode.Patrol));
            Assert.That(host.GetComponent<NavMeshAgent>().speed, Is.EqualTo(6f));
        }

        [Test]
        public void Navigation_OffMeshAndDisabledAgent_StopResumeChangesPublicState()
        {
            GameObject host = CreateObject("Unbound NPC");
            host.SetActive(false);
            host.transform.position = new Vector3(9000f, 500f, 9000f);
            NpcNavigation navigation = host.AddComponent<NpcNavigation>();
            InvokeLifecycle(navigation, "Awake");

            navigation.Stop();
            Assert.That(navigation.IsActive, Is.False);
            navigation.Resume();
            Assert.That(navigation.IsActive, Is.True);

            host.GetComponent<NavMeshAgent>().enabled = false;
            navigation.IsActive = false;
            Assert.That(navigation.IsActive, Is.False);
            navigation.IsActive = true;
            Assert.That(navigation.IsActive, Is.True);
        }

        [Test]
        public void TargetFinder_ChangingBoundOverride_AppliesReplacementOnNextUpdate()
        {
            GameObject host = CreateObject("Target finder");
            host.SetActive(false);
            NpcTargetFinder finder = host.AddComponent<NpcTargetFinder>();
            NpcNavigation navigation = host.GetComponent<NpcNavigation>();
            Transform first = CreateObject("First override").transform;
            Transform second = CreateObject("Second override").transform;
            finder.TargetOverride = first;
            finder.FindAndSetTarget();
            Assert.That(navigation.FollowTarget, Is.SameAs(first));

            finder.TargetOverride = second;
            InvokeLifecycle(finder, "Update");

            Assert.That(navigation.FollowTarget, Is.SameAs(second));
        }

        [Test]
        public void TargetFinder_UndefinedTag_StillUsesConfiguredNameFallback()
        {
            GameObject host = CreateObject("Target finder");
            host.SetActive(false);
            NpcTargetFinder finder = host.AddComponent<NpcTargetFinder>();
            Transform target = CreateObject("Audit20261023NeoNPC_NameFallback_5e93").transform;
            using (SerializedObject settings = new(finder))
            {
                settings.FindProperty("_targetTag").stringValue = "Audit20261023NeoNPC_UndefinedTag_5e93";
                settings.FindProperty("_findByName").boolValue = true;
                settings.FindProperty("_targetName").stringValue = target.name;
                settings.ApplyModifiedPropertiesWithoutUndo();
            }

            finder.FindAndSetTarget();

            Assert.That(host.GetComponent<NpcNavigation>().FollowTarget, Is.SameAs(target));
        }

        [Test]
        public void Combat_ForceAttackWithoutController_ReturnsFailure()
        {
            GameObject host = CreateObject("Brain without attack controller");
            NpcRpgCombatBrain brain = host.AddComponent<NpcRpgCombatBrain>();
            RpgAttackPreset attack = Own(ScriptableObject.CreateInstance<RpgAttackPreset>());
            using (SerializedObject settings = new(attack))
            {
                settings.FindProperty("_requireTarget").boolValue = false;
                settings.ApplyModifiedPropertiesWithoutUndo();
            }

            NpcCombatPreset preset = Own(ScriptableObject.CreateInstance<NpcCombatPreset>());
            using (SerializedObject settings = new(preset))
            {
                settings.FindProperty("_attackPreset").objectReferenceValue = attack;
                settings.ApplyModifiedPropertiesWithoutUndo();
            }

            AssignPreset(brain, preset);

            Assert.That(brain.ForceAttack(), Is.False);
        }

        [Test]
        public void Combat_ImmediateHoldThenClear_RestoresPreviouslyActiveNavigation()
        {
            NpcRpgCombatBrain brain = CreateCombatNpc(1f, out NpcNavigation navigation);
            navigation.SetMode(NpcNavigation.NavigationMode.Patrol);
            brain.EvaluateNow();
            Assert.That(brain.HasTarget, Is.True);
            Assert.That(navigation.IsActive, Is.False, "The in-range NPC should hold its position.");

            brain.ClearCombatTarget();

            Assert.That(navigation.IsActive, Is.True);
            Assert.That(navigation.Mode, Is.EqualTo(NpcNavigation.NavigationMode.Patrol));
            Assert.That(brain.HasTarget, Is.False);
        }

        [Test]
        public void Combat_AutoResolveAfterSelectorReplacement_AcquiresThroughNewComponent()
        {
            NpcRpgCombatBrain brain = CreateCombatNpc(1f, out NpcNavigation navigation);
            GameObject expected = brain.AcquireTarget();
            Assert.That(expected, Is.Not.Null);
            Object.DestroyImmediate(brain.GetComponent<RpgTargetSelector>());
            RpgTargetSelector replacement = brain.gameObject.AddComponent<RpgTargetSelector>();
            using (SerializedObject settings = new(replacement))
            {
                settings.FindProperty("_query._targetLayers").intValue = 1 << 31;
                settings.FindProperty("_query._use2D").boolValue = false;
                settings.ApplyModifiedPropertiesWithoutUndo();
            }

            brain.AutoResolveReferences();

            Assert.That(brain.AcquireTarget(), Is.SameAs(expected));
            Assert.That(brain.CurrentTarget, Is.SameAs(expected));
        }

        [TestCase(true)]
        [TestCase(false)]
        public void Combat_ChaseThenClear_RestoresOriginalFollowTargetAndActivity(bool initiallyActive)
        {
            NpcRpgCombatBrain brain = CreateCombatNpc(5f, out NpcNavigation navigation);
            Transform originalTarget = CreateObject("Original follow target").transform;
            navigation.SetFollowTarget(originalTarget);
            navigation.IsActive = initiallyActive;
            brain.EvaluateNow();
            Assert.That(navigation.FollowTarget, Is.SameAs(brain.CurrentTarget.transform));

            brain.ClearCombatTarget();

            Assert.That(navigation.FollowTarget, Is.SameAs(originalTarget));
            Assert.That(navigation.IsActive, Is.EqualTo(initiallyActive));
            Assert.That(brain.HasTarget, Is.False);
        }

        [TestCase(0f, 1f)]
        [TestCase(0.1f, 0.5f)]
        public void Animation_UsesSuppliedDeltaTime_WithNoBoundAgent(float deltaTime, float expectedMaximum)
        {
            GameObject host = CreateObject("Animated NPC");
            Animator animator = host.AddComponent<Animator>();
            AnimatorController controller = Own(new AnimatorController());
            controller.AddParameter("Speed", AnimatorControllerParameterType.Float);
            controller.AddParameter("IsMoving", AnimatorControllerParameterType.Bool);
            controller.AddLayer("Base Layer");
            Own(controller.layers[0].stateMachine);
            animator.runtimeAnimatorController = controller;
            animator.Rebind();
            animator.SetFloat("Speed", 1f);
            animator.SetBool("IsMoving", true);
            NpcAnimationCore core = new(null, animator, "Speed", "IsMoving", 0.1f);

            core.Tick(deltaTime);

            float speed = animator.GetFloat("Speed");
            if (deltaTime == 0f)
            {
                Assert.That(speed, Is.EqualTo(1f).Within(0.0001f));
            }
            else
            {
                Assert.That(speed, Is.InRange(0f, expectedMaximum));
            }

            Assert.That(animator.GetBool("IsMoving"), Is.False);
        }

        private NpcRpgCombatBrain CreateCombatNpc(float targetDistance, out NpcNavigation navigation)
        {
            Vector3 origin = new(9000f, 500f, 9000f);
            GameObject host = CreateObject("Combat NPC");
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
            AssignPreset(brain, Own(ScriptableObject.CreateInstance<NpcCombatPreset>()));
            brain.AutoResolveReferences();
            GameObject target = CreateObject("Combat target");
            target.layer = 31;
            target.transform.position = origin + Vector3.right * targetDistance;
            target.AddComponent<RpgCharacter>();
            target.AddComponent<BoxCollider>();
            Physics.SyncTransforms();
            return brain;
        }

        private static void AssignPreset(NpcRpgCombatBrain brain, NpcCombatPreset preset)
        {
            using (SerializedObject settings = new(brain))
            {
                settings.FindProperty("_preset").objectReferenceValue = preset;
                settings.ApplyModifiedPropertiesWithoutUndo();
            }
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
    }
}
