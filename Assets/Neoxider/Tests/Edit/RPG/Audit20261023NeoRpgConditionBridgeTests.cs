using System.Collections.Generic;
using System.Reflection;
using Neo.Reactive;
using Neo.Rpg;
using Neo.Rpg.Components;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using UnityEngine.Events;
using Object = UnityEngine.Object;

namespace Neo.Tests.Edit.RPG
{
    [TestFixture]
    public sealed class Audit20261023NeoRpgConditionBridgeTests
    {
        private GameObject _root;
        private RpgConditionAdapter _adapter;
        private RpgCharacterTemplate _template;
        private RpgAttackDefinition _attack;

        [SetUp]
        public void SetUp()
        {
            _root = new GameObject("ConditionBridgeAudit");
            _root.SetActive(false);
            _adapter = CreateChild("Adapter").AddComponent<RpgConditionAdapter>();
            _template = ScriptableObject.CreateInstance<RpgCharacterTemplate>();
            _template.resources = new[]
            {
                new RpgResourceDefinition
                {
                    id = new RpgStatId(RpgStatPreset.Hp),
                    startCurrent = 75f,
                    startMax = 100f,
                    restoreToFull = false
                },
                new RpgResourceDefinition
                {
                    id = new RpgStatId("DarkMana"),
                    startCurrent = 10f,
                    startMax = 40f,
                    restoreToFull = false
                }
            };
            _template.stats = new[]
            {
                new RpgStatDefinition { id = new RpgStatId(RpgStatPreset.Strength), baseValue = 20f }
            };
        }

        [TearDown]
        public void TearDown()
        {
            Object.DestroyImmediate(_root);
            Object.DestroyImmediate(_template);
            Object.DestroyImmediate(_attack);
        }

        [TestCase(false, false)]
        [TestCase(false, true)]
        [TestCase(true, false)]
        [TestCase(true, true)]
        public void Evaluate_MissingCharacter_PublishesInvertedResultAndMatchingEvent(bool invert, bool nullContext)
        {
            using SerializedObject settings = new(_adapter);
            settings.FindProperty("_invert").boolValue = invert;
            settings.ApplyModifiedPropertiesWithoutUndo();
            List<string> events = new();
            List<bool> values = new();
            bool? lastResultAtEvent = null;
            GetEvent<UnityEventBool>("_onEvaluated").AddListener(value =>
            {
                events.Add("evaluated");
                values.Add(value);
                lastResultAtEvent = _adapter.LastResult;
            });
            GetEvent<UnityEvent>("_onTrue").AddListener(() => events.Add("true"));
            GetEvent<UnityEvent>("_onFalse").AddListener(() => events.Add("false"));

            bool result = _adapter.Evaluate(nullContext ? null : CreateChild("EmptyContext"));

            Assert.That(result, Is.EqualTo(invert));
            Assert.That(_adapter.LastResult, Is.EqualTo(invert));
            Assert.That(lastResultAtEvent, Is.EqualTo(invert));
            Assert.That(values, Is.EqualTo(new[] { invert }));
            Assert.That(events, Is.EqualTo(new[] { "evaluated", invert ? "true" : "false" }));
        }

        [TestCase(true)]
        [TestCase(false)]
        public void Evaluate_ReentrantListener_PreservesEachCallsReturnValueAndBranch(bool outerExpected)
        {
            RpgCharacter healthy = CreateCharacter("Healthy");
            RpgCharacter injured = CreateCharacter("Injured");
            injured.Damage(65f);
            RpgCharacter outer = outerExpected ? healthy : injured;
            RpgCharacter inner = outerExpected ? injured : healthy;
            List<bool> evaluated = new();
            List<bool> branches = new();
            bool reentered = false;
            bool? nestedResult = null;
            GetEvent<UnityEventBool>("_onEvaluated").AddListener(value =>
            {
                evaluated.Add(value);
                if (!reentered)
                {
                    reentered = true;
                    nestedResult = _adapter.Evaluate(inner.gameObject);
                }
            });
            GetEvent<UnityEvent>("_onTrue").AddListener(() => branches.Add(true));
            GetEvent<UnityEvent>("_onFalse").AddListener(() => branches.Add(false));

            bool result = _adapter.Evaluate(outer.gameObject);

            Assert.That(result, Is.EqualTo(outerExpected));
            Assert.That(nestedResult, Is.EqualTo(!outerExpected));
            Assert.That(evaluated, Is.EqualTo(new[] { outerExpected, !outerExpected }));
            Assert.That(branches, Is.EqualTo(new[] { !outerExpected, outerExpected }));
            Assert.That(_adapter.LastResult, Is.EqualTo(!outerExpected));
        }

        [TestCase(RpgConditionEvaluationMode.CanEvade, true, false, true)]
        [TestCase(RpgConditionEvaluationMode.CanEvade, true, true, false)]
        [TestCase(RpgConditionEvaluationMode.CanEvade, false, false, false)]
        [TestCase(RpgConditionEvaluationMode.CanEvade, false, true, true)]
        [TestCase(RpgConditionEvaluationMode.AttackReady, true, false, true)]
        [TestCase(RpgConditionEvaluationMode.AttackReady, true, true, false)]
        [TestCase(RpgConditionEvaluationMode.AttackReady, false, false, false)]
        [TestCase(RpgConditionEvaluationMode.AttackReady, false, true, true)]
        public void Evaluate_ControllerMode_UsesAssignedControllerReadinessWithoutCharacter(
            RpgConditionEvaluationMode mode, bool controllerEnabled, bool invert, bool expected)
        {
            using SerializedObject settings = new(_adapter);
            settings.FindProperty("_mode").intValue = (int)mode;
            settings.FindProperty("_invert").boolValue = invert;
            RpgEvadeController evade = null;
            RpgAttackController attack = null;
            if (mode == RpgConditionEvaluationMode.CanEvade)
            {
                evade = CreateChild("Evade").AddComponent<RpgEvadeController>();
                evade.EnableBuiltInInput = false;
                evade.enabled = controllerEnabled;
                settings.FindProperty("_evadeController").objectReferenceValue = evade;
            }
            else
            {
                attack = CreateChild("Attack").AddComponent<RpgAttackController>();
                attack.EnableBuiltInInput = false;
                attack.enabled = controllerEnabled;
                _attack = ScriptableObject.CreateInstance<RpgAttackDefinition>();
                _attack.name = "ConditionBridgeAuditAttack";
                using SerializedObject attackSettings = new(attack);
                SerializedProperty attacks = attackSettings.FindProperty("_attacks");
                attacks.arraySize = 1;
                attacks.GetArrayElementAtIndex(0).objectReferenceValue = _attack;
                attackSettings.ApplyModifiedPropertiesWithoutUndo();
                settings.FindProperty("_attackController").objectReferenceValue = attack;
                settings.FindProperty("_attackId").stringValue = _attack.Id;
            }
            settings.ApplyModifiedPropertiesWithoutUndo();
            _root.SetActive(true);

            if (mode == RpgConditionEvaluationMode.CanEvade)
            {
                Assert.That(evade.CanEvade, Is.EqualTo(controllerEnabled));
            }
            else
            {
                Assert.That(attack.CanUseAttack(_attack.Id, out _), Is.EqualTo(controllerEnabled));
            }
            List<bool> evaluated = new();
            GetEvent<UnityEventBool>("_onEvaluated").AddListener(evaluated.Add);

            bool result = _adapter.Evaluate(null);

            Assert.That(result, Is.EqualTo(expected));
            Assert.That(_adapter.LastResult, Is.EqualTo(expected));
            Assert.That(evaluated, Is.EqualTo(new[] { expected }));
        }

        [TestCase(RpgConditionEvaluationMode.CanEvade)]
        [TestCase(RpgConditionEvaluationMode.AttackReady)]
        public void Evaluate_ControllerMode_RequiresAssignedControllerEvenWithCharacter(RpgConditionEvaluationMode mode)
        {
            RpgCharacter character = CreateCharacter("Character");
            using SerializedObject settings = new(_adapter);
            settings.FindProperty("_mode").intValue = (int)mode;
            settings.ApplyModifiedPropertiesWithoutUndo();

            Assert.That(_adapter.Evaluate(character.gameObject), Is.False);
            Assert.That(_adapter.LastResult, Is.False);
        }

        [Test]
        public void Evaluate_ExplicitCharacterTakesPriority_DestroyedReferenceFallsBackToContext()
        {
            RpgCharacter assigned = CreateCharacter("Assigned");
            RpgCharacter context = CreateCharacter("Context");
            context.Damage(65f);
            using SerializedObject settings = new(_adapter);
            settings.FindProperty("_character").objectReferenceValue = assigned;
            settings.ApplyModifiedPropertiesWithoutUndo();
            List<bool> evaluated = new();
            GetEvent<UnityEventBool>("_onEvaluated").AddListener(evaluated.Add);

            Assert.That(_adapter.Evaluate(context.gameObject), Is.True);
            Object.DestroyImmediate(assigned.gameObject);
            Assert.That(_adapter.Evaluate(context.gameObject), Is.False);
            Assert.That(evaluated, Is.EqualTo(new[] { true, false }));
            Assert.That(_adapter.LastResult, Is.False);
        }

        [Test]
        public void EvaluateCurrent_QueriesParentCharacterAndObservesChangedHp()
        {
            RpgCharacter character = CreateCharacter("ParentCharacter");
            _adapter.transform.SetParent(character.transform);
            _root.SetActive(true);

            Assert.That(_adapter.EvaluateCurrent(), Is.True);
            character.Damage(50f);
            Assert.That(_adapter.EvaluateCurrent(), Is.False);
            Assert.That(_adapter.LastResult, Is.False);
        }

        [TestCase(RpgConditionEvaluationMode.HpAtLeast, 75f, true)]
        [TestCase(RpgConditionEvaluationMode.HpAtLeast, 75.1f, false)]
        [TestCase(RpgConditionEvaluationMode.HpPercentAtLeast, 75f, true)]
        [TestCase(RpgConditionEvaluationMode.HpPercentAtLeast, 75.1f, false)]
        [TestCase(RpgConditionEvaluationMode.ResourceAtLeast, 75f, true)]
        [TestCase(RpgConditionEvaluationMode.ResourceAtLeast, 75.1f, false)]
        [TestCase(RpgConditionEvaluationMode.ResourceBelow, 75f, false)]
        [TestCase(RpgConditionEvaluationMode.ResourceBelow, 75.1f, true)]
        [TestCase(RpgConditionEvaluationMode.ResourcePercentAtLeast, 75f, true)]
        [TestCase(RpgConditionEvaluationMode.ResourcePercentAtLeast, 75.1f, false)]
        [TestCase(RpgConditionEvaluationMode.ResourcePercentBelow, 75f, false)]
        [TestCase(RpgConditionEvaluationMode.ResourcePercentBelow, 75.1f, true)]
        [TestCase(RpgConditionEvaluationMode.StatAtLeast, 20f, true)]
        [TestCase(RpgConditionEvaluationMode.StatAtLeast, 20.1f, false)]
        [TestCase(RpgConditionEvaluationMode.StatBelow, 20f, false)]
        [TestCase(RpgConditionEvaluationMode.StatBelow, 20.1f, true)]
        public void Evaluate_NumericModes_PreserveInclusiveAndStrictThresholds(
            RpgConditionEvaluationMode mode, float threshold, bool expected)
        {
            RpgCharacter character = CreateCharacter("Character");
            using SerializedObject settings = new(_adapter);
            settings.FindProperty("_mode").intValue = (int)mode;
            settings.FindProperty("_threshold").floatValue = threshold;
            settings.ApplyModifiedPropertiesWithoutUndo();

            Assert.That(_adapter.Evaluate(character.gameObject), Is.EqualTo(expected));
            Assert.That(_adapter.LastResult, Is.EqualTo(expected));
        }

        [Test]
        public void Evaluate_LegacyResourceIdIsTrimmedAndOverridesTypedResource_WhitespaceUsesTypedResource()
        {
            RpgCharacter character = CreateCharacter("Character");
            using SerializedObject settings = new(_adapter);
            settings.FindProperty("_mode").intValue = (int)RpgConditionEvaluationMode.ResourceAtLeast;
            settings.FindProperty("_resourceId").stringValue = "  DarkMana  ";
            settings.ApplyModifiedPropertiesWithoutUndo();

            Assert.That(_adapter.Evaluate(character.gameObject), Is.False);
            settings.FindProperty("_resourceId").stringValue = "  ";
            settings.ApplyModifiedPropertiesWithoutUndo();
            Assert.That(_adapter.Evaluate(character.gameObject), Is.True);
        }

        private GameObject CreateChild(string name)
        {
            GameObject child = new(name);
            child.transform.SetParent(_root.transform);
            return child;
        }

        private RpgCharacter CreateCharacter(string name)
        {
            RpgCharacter character = CreateChild(name).AddComponent<RpgCharacter>();
            character.ApplyTemplate(_template);
            return character;
        }

        // Reflection only wires Inspector-owned events; assertions observe public results and notifications.
        private TEvent GetEvent<TEvent>(string fieldName) where TEvent : UnityEventBase
        {
            FieldInfo field = typeof(RpgConditionAdapter).GetField(fieldName,
                BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.That(field, Is.Not.Null, $"Missing serialized event: {fieldName}");
            return (TEvent)field.GetValue(_adapter);
        }
    }
}
