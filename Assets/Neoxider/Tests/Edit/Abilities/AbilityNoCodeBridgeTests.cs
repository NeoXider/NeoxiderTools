using Neo.Abilities;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace Neo.Editor.Tests.Abilities
{
    /// <summary>
    ///     AbilityNoCodeAction dispatch: success and failure paths per action type, graceful failures
    ///     (events with messages, no exceptions) on missing targets and unknown ids.
    /// </summary>
    public sealed class AbilityNoCodeBridgeTests
    {
        private AbilitySceneRig _rig;
        private AbilityUnitBehaviour _player;
        private AbilityCasterBehaviour _caster;
        private AbilityUnitBehaviour _enemy;
        private AbilityNoCodeAction _action;
        private ScriptableObject _definitionToDestroy;

        private int _successCount;
        private string _failedMessage;

        [SetUp]
        public void SetUp()
        {
            _rig = new AbilitySceneRig();
            _player = _rig.AddUnit("player", team: 1, Vector3.zero);
            _caster = _player.gameObject.AddComponent<AbilityCasterBehaviour>();
            _enemy = _rig.AddUnit("enemy", team: 2, new Vector3(3f, 0f, 0f));

            var blast = new AbilityBlueprint { Id = "blast", Targeting = TargetingMode.NoTarget, Cooldown = 1f };
            blast.ImpactEffects.Add(AbilityTestSupport.DamageNode(10f));
            _rig.System.RegisterAbility(blast);
            _rig.System.GrantAbility(_player.UnitId, "blast");

            var bolt = new AbilityBlueprint
            {
                Id = "bolt",
                Targeting = TargetingMode.Unit,
                TeamFilter = AbilityTeamFilter.Enemies,
                Cooldown = 1f
            };
            bolt.ImpactEffects.Add(AbilityTestSupport.DamageNode(10f));
            _rig.System.RegisterAbility(bolt);
            _rig.System.GrantAbility(_player.UnitId, "bolt");

            // WHY: the bridge lives on its own GameObject so fallback searches stay empty unless
            // a test wires the serialized references explicitly.
            _action = _rig.Own(new GameObject("bridge")).AddComponent<AbilityNoCodeAction>();
            _successCount = 0;
            _failedMessage = null;
            _action.OnSuccess.AddListener(() => _successCount++);
            _action.OnFailed.AddListener(message => _failedMessage = message);
        }

        [TearDown]
        public void TearDown()
        {
            if (_definitionToDestroy != null)
            {
                Object.DestroyImmediate(_definitionToDestroy);
                _definitionToDestroy = null;
            }

            _rig.Dispose();
        }

        [Test]
        public void CastById_Success_FiresOnSuccess_AndStartsCooldown()
        {
            Configure(AbilityNoCodeActionType.CastById, abilityId: "blast", caster: _caster);

            _action.Execute();

            Assert.That(_successCount, Is.EqualTo(1));
            Assert.That(_failedMessage, Is.Null);
            Assert.That(_rig.System.TryGetSlot(_player.UnitId, "blast", out AbilitySlot slot), Is.True);
            Assert.That(slot.CooldownRemaining, Is.GreaterThan(0f));
        }

        [Test]
        public void CastById_UnknownId_FiresOnFailedWithReason()
        {
            Configure(AbilityNoCodeActionType.CastById, abilityId: "nope", caster: _caster);

            _action.Execute();

            Assert.That(_successCount, Is.Zero);
            Assert.That(_failedMessage, Does.Contain("nope").And.Contain(nameof(CastFailureReason.UnknownAbility)));
        }

        [Test]
        public void Execute_NoCasterAnywhere_FiresOnFailed()
        {
            Configure(AbilityNoCodeActionType.CastById, abilityId: "blast");

            _action.Execute();

            Assert.That(_successCount, Is.Zero);
            Assert.That(_failedMessage, Does.Contain("AbilityCasterBehaviour"));
        }

        [Test]
        public void CastAtUnit_MissingTarget_FiresOnFailed()
        {
            Configure(AbilityNoCodeActionType.CastAtUnit, abilityId: "bolt", caster: _caster);

            _action.Execute();

            Assert.That(_failedMessage, Does.Contain("Target unit"));
        }

        [Test]
        public void CastAtUnit_Success_DamagesTarget()
        {
            Configure(AbilityNoCodeActionType.CastAtUnit, abilityId: "bolt", caster: _caster, targetUnit: _enemy);

            _action.Execute();

            Assert.That(_successCount, Is.EqualTo(1));
            Assert.That(_enemy.CurrentHealth, Is.EqualTo(90f).Within(0.001f));
        }

        [Test]
        public void CastFirstAbility_EmptyCasterList_FiresOnFailed()
        {
            Configure(AbilityNoCodeActionType.CastFirstAbility, caster: _caster);

            _action.Execute();

            Assert.That(_failedMessage, Does.Contain("no abilities"));
        }

        [Test]
        public void GrantAbility_ByDefinition_RegistersAndGrants()
        {
            var definition = ScriptableObject.CreateInstance<AbilityDefinition>();
            _definitionToDestroy = definition;
            definition.Blueprint.Id = "gift";
            definition.Blueprint.Targeting = TargetingMode.NoTarget;
            Configure(AbilityNoCodeActionType.GrantAbility, unit: _player, ability: definition);

            _action.Execute();

            Assert.That(_successCount, Is.EqualTo(1));
            Assert.That(_rig.System.TryGetSlot(_player.UnitId, "gift", out _), Is.True);
        }

        [Test]
        public void GrantAbility_UnknownId_FiresOnFailed()
        {
            Configure(AbilityNoCodeActionType.GrantAbility, abilityId: "ghost_ability", unit: _player);

            _action.Execute();

            Assert.That(_failedMessage, Does.Contain("ghost_ability"));
        }

        [Test]
        public void RevokeAbility_RemovesSlot()
        {
            Configure(AbilityNoCodeActionType.RevokeAbility, abilityId: "blast", unit: _player);

            _action.Execute();

            Assert.That(_successCount, Is.EqualTo(1));
            Assert.That(_rig.System.TryGetSlot(_player.UnitId, "blast", out _), Is.False);
        }

        [Test]
        public void SetAbilityLevel_Granted_SetsSlotLevel()
        {
            Configure(AbilityNoCodeActionType.SetAbilityLevel, abilityId: "blast", unit: _player, level: 3);

            _action.Execute();

            Assert.That(_successCount, Is.EqualTo(1));
            _rig.System.TryGetSlot(_player.UnitId, "blast", out AbilitySlot slot);
            Assert.That(slot.Level, Is.EqualTo(3));
        }

        [Test]
        public void SetAbilityLevel_NotGranted_FiresOnFailed()
        {
            Configure(AbilityNoCodeActionType.SetAbilityLevel, abilityId: "ghost_ability", unit: _player, level: 3);

            _action.Execute();

            Assert.That(_failedMessage, Does.Contain("ghost_ability"));
        }

        [Test]
        public void SetUnitLevel_SetsDomainLevel()
        {
            Configure(AbilityNoCodeActionType.SetUnitLevel, unit: _player, level: 4);

            _action.Execute();

            Assert.That(_successCount, Is.EqualTo(1));
            Assert.That(_player.Unit.Level, Is.EqualTo(4));
        }

        [Test]
        public void ApplyDamage_FallsBackToActingUnit()
        {
            Configure(AbilityNoCodeActionType.ApplyDamage, unit: _player, amount: 25f);

            _action.Execute();

            Assert.That(_successCount, Is.EqualTo(1));
            Assert.That(_player.CurrentHealth, Is.EqualTo(75f).Within(0.001f));
        }

        [Test]
        public void Heal_RestoresDamagedTarget()
        {
            _player.ApplyDamage(50f);
            Configure(AbilityNoCodeActionType.Heal, targetUnit: _player, amount: 30f);

            _action.Execute();

            Assert.That(_successCount, Is.EqualTo(1));
            Assert.That(_player.CurrentHealth, Is.EqualTo(80f).Within(0.001f));
        }

        [Test]
        public void ApplyModifier_ByDefinition_AppliesToTarget()
        {
            var definition = ScriptableObject.CreateInstance<ModifierDefinition>();
            _definitionToDestroy = definition;
            definition.Blueprint.Id = "haste";
            definition.Blueprint.Duration = 5f;
            Configure(AbilityNoCodeActionType.ApplyModifier, unit: _player, targetUnit: _enemy,
                modifier: definition);

            _action.Execute();

            Assert.That(_successCount, Is.EqualTo(1));
            Assert.That(AbilityTestSupport.ActiveModifiers(_rig.System, _enemy.UnitId), Has.Count.EqualTo(1));
        }

        [Test]
        public void RemoveModifier_NotActive_FiresOnFailed()
        {
            Configure(AbilityNoCodeActionType.RemoveModifier, targetUnit: _enemy, modifierId: "ghost_modifier");

            _action.Execute();

            Assert.That(_failedMessage, Does.Contain("ghost_modifier"));
        }

        private void Configure(AbilityNoCodeActionType actionType, string abilityId = null, string modifierId = null,
            AbilityCasterBehaviour caster = null, AbilityUnitBehaviour unit = null,
            AbilityUnitBehaviour targetUnit = null, AbilityDefinition ability = null,
            ModifierDefinition modifier = null, int level = 1, float amount = 25f)
        {
            var serialized = new SerializedObject(_action);
            serialized.FindProperty("_actionType").enumValueIndex = (int)actionType;
            serialized.FindProperty("_abilityId").stringValue = abilityId ?? string.Empty;
            serialized.FindProperty("_modifierId").stringValue = modifierId ?? string.Empty;
            serialized.FindProperty("_caster").objectReferenceValue = caster;
            serialized.FindProperty("_unit").objectReferenceValue = unit;
            serialized.FindProperty("_targetUnit").objectReferenceValue = targetUnit;
            serialized.FindProperty("_ability").objectReferenceValue = ability;
            serialized.FindProperty("_modifier").objectReferenceValue = modifier;
            serialized.FindProperty("_level").intValue = level;
            serialized.FindProperty("_amount").floatValue = amount;
            serialized.ApplyModifiedPropertiesWithoutUndo();
        }
    }
}
