using System.Collections.Generic;
using System.Reflection;
using Neo.Core.Resources;
using Neo.Reactive;
using Neo.Rpg;
using Neo.Rpg.Components;
using Neo.Rpg.Runtime;
using NUnit.Framework;
using UnityEngine;

namespace Neo.Tests.Edit.RPG
{
    public sealed class Audit20261023NeoRpgCharacterTests
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
        public void ApplyProfile_RemovesUpgradeBonusesMissingFromRestoredProfile()
        {
            RpgProgressionDefinition progression = CreateProgression(1);
            RpgCharacter character = CreateCharacter(progression);
            RpgCharacterProfileData original = character.CaptureProfile();
            character.AddUpgradePoints(1);
            Assert.That(character.UpgradeStat("Wisdom"), Is.True);
            Assert.That(character.GetStat("Wisdom"), Is.EqualTo(12f));
            Assert.That(character.MaxHp, Is.EqualTo(115f));
            Assert.That(character.Resources["Mana"].ResolvedRegenPerSecond, Is.EqualTo(2.5f));

            character.ApplyProfile(original);

            Assert.That(character.GetUpgradeLevel("Wisdom"), Is.Zero);
            Assert.That(character.GetStat("Wisdom"), Is.EqualTo(10f));
            Assert.That(character.MaxHp, Is.EqualTo(100f));
            Assert.That(character.Resources["Mana"].ResolvedRegenPerSecond, Is.EqualTo(1f));
        }

        [TestCase(0)]
        [TestCase(-2)]
        public void UpgradeStat_InvalidCostCannotCreateFreeUpgradesOrPoints(int cost)
        {
            RpgCharacter character = CreateCharacter(CreateProgression(cost));
            character.AddUpgradePoints(2);

            Assert.That(character.CanUpgradeStat("Wisdom"), Is.False);
            Assert.That(character.UpgradeStat("Wisdom"), Is.False);
            Assert.That(character.UpgradePointsValue, Is.EqualTo(2));
            Assert.That(character.GetStat("Wisdom"), Is.EqualTo(10f));
            Assert.That(character.GetUpgradeLevel("Wisdom"), Is.Zero);
        }

        [Test]
        public void SetLevel_DownwardChangeRecalculatesAutomaticStatGrowth()
        {
            RpgCharacter character = CreateCharacter(CreateProgression(1));
            character.Template.stats[0].affectedByLevel = true;
            character.Template.stats[0].growth = new RpgStatGrowthRule { AddPerLevel = 2f };
            character.RebuildRuntime();
            character.SetLevel(3);
            Assert.That(character.GetStat("Wisdom"), Is.EqualTo(14f));

            character.SetLevel(2);

            Assert.That(character.Level, Is.EqualTo(2));
            Assert.That(character.GetStat("Wisdom"), Is.EqualTo(12f));
            Assert.That(character.GetStatState("Wisdom").CurrentValue, Is.EqualTo(12f));
        }

        [Test]
        public void UpgradeStat_RuleWithoutRuntimeStatCannotConsumePoints()
        {
            RpgProgressionDefinition progression = CreateProgression(1);
            progression.upgradeRules[0].statId = new RpgStatId("Missing");
            RpgCharacter character = CreateCharacter(progression);
            character.AddUpgradePoints(1);

            Assert.That(character.UpgradeStat("Missing"), Is.False);
            Assert.That(character.UpgradePointsValue, Is.EqualTo(1));
            Assert.That(character.GetUpgradeLevel("Missing"), Is.Zero);
        }

        [TestCase(false)]
        [TestCase(true)]
        public void GenericHpRestore_RevivesExactlyOnce(bool restoreToFull)
        {
            RpgCharacter character = CreateCharacter();
            int revivals = 0;
            character.OnRevivedEvent.AddListener(() => revivals++);
            character.Damage(100f);
            Assert.That(character.IsDead, Is.True);

            if (restoreToFull)
            {
                character.RestoreResource(RpgResourceId.Hp);
            }
            else
            {
                character.Increase(RpgResourceId.Hp, 10f);
            }

            Assert.That(character.CurrentHp, Is.EqualTo(restoreToFull ? 100f : 10f));
            Assert.That(character.IsDeadState.CurrentValue, Is.False);
            Assert.That(character.CanPerformActions, Is.True);
            Assert.That(revivals, Is.EqualTo(1));
            character.Heal(1f);
            Assert.That(revivals, Is.EqualTo(1));
        }

        [Test]
        public void BaseStatChanges_ImmediatelyUpdateDependentResourceRegen()
        {
            RpgCharacter character = CreateCharacter();
            RpgCharacterTemplate template = character.Template;
            template.resources[1].regen.mode = RpgRegenMode.FromStat;
            template.resources[1].regen.scalingStat = new RpgStatId("Wisdom");
            template.resources[1].regen.scalingMultiplier = 0.5f;
            character.RebuildRuntime();
            Assert.That(character.Resources["Mana"].ResolvedRegenPerSecond, Is.EqualTo(5f));

            character.SetStatBase("Wisdom", 20f);
            Assert.That(character.Resources["Mana"].ResolvedRegenPerSecond, Is.EqualTo(10f));
            character.AddStatBase("Wisdom", 4f);
            Assert.That(character.GetStat("Wisdom"), Is.EqualTo(24f));
            Assert.That(character.Resources["Mana"].ResolvedRegenPerSecond, Is.EqualTo(12f));
        }

        [TestCase(BuffStatType.DamagePercent, 1.6f)]
        [TestCase(BuffStatType.MovementSpeedPercent, 1.6f)]
        [TestCase(BuffStatType.DefensePercent, 0.4f)]
        [TestCase(BuffStatType.SpecificDefensePercent, 0.4f)]
        public void LegacyBuffAliases_ApplyEveryActiveStack(BuffStatType type, float expected)
        {
            RpgCharacter character = CreateCharacter();
            BuffDefinition buff = Track(ScriptableObject.CreateInstance<BuffDefinition>());
            JsonUtility.FromJsonOverwrite("{\"_id\":\"legacy\",\"_stackable\":true,\"_maxStacks\":3," +
                "\"_modifiers\":[{\"_statType\":" + (int)type +
                ",\"_value\":20,\"_specificDamageType\":\"Fire\"}]}", buff);
            for (int i = 0; i < 3; i++)
            {
                Assert.That(character.ApplyBuff(buff), Is.True);
            }

            float actual = type switch
            {
                BuffStatType.DamagePercent => character.GetOutgoingDamageMultiplier(),
                BuffStatType.MovementSpeedPercent => character.GetMovementSpeedMultiplier(),
                BuffStatType.DefensePercent => character.GetIncomingDamageMultiplier(),
                _ => character.GetIncomingDamageMultiplier("Fire")
            };
            Assert.That(actual, Is.EqualTo(expected).Within(0.001f));
        }

        [Test]
        public void StatusMovementMultipliers_ComposeAndDisappearWhenRemoved()
        {
            RpgCharacter character = CreateCharacter();
            StatusEffectDefinition first = Track(ScriptableObject.CreateInstance<StatusEffectDefinition>());
            StatusEffectDefinition second = Track(ScriptableObject.CreateInstance<StatusEffectDefinition>());
            JsonUtility.FromJsonOverwrite("{\"_id\":\"slow\",\"_movementSpeedMultiplier\":0.5}", first);
            JsonUtility.FromJsonOverwrite("{\"_id\":\"chill\",\"_movementSpeedMultiplier\":0.8}", second);
            character.ApplyStatus(first);
            character.ApplyStatus(second);

            Assert.That(character.GetMovementSpeedMultiplier(), Is.EqualTo(0.4f).Within(0.001f));
            character.RemoveStatus("slow");
            Assert.That(character.GetMovementSpeedMultiplier(), Is.EqualTo(0.8f).Within(0.001f));
            character.ClearAllStatuses();
            Assert.That(character.GetMovementSpeedMultiplier(), Is.EqualTo(1f));
        }

        [Test]
        public void Bindings_ReconnectAfterLateInitializationAndDetachReplacedStates()
        {
            GameObject actor = Track(new GameObject("AuditRpgLateActor"));
            actor.SetActive(false);
            RpgCharacter character = actor.AddComponent<RpgCharacter>();
            GameObject view = Track(new GameObject("AuditRpgView"));
            RpgResourceBinding resourceBinding = view.AddComponent<RpgResourceBinding>();
            RpgStatBinding statBinding = view.AddComponent<RpgStatBinding>();
            statBinding.StatId = new RpgStatId("Wisdom");
            resourceBinding.Character = character;
            statBinding.Character = character;
            float current = -1f;
            float statValue = -1f;
            int currentNotifications = 0;
            resourceBinding.OnCurrent.AddListener((float value) => { current = value; currentNotifications++; });
            statBinding.OnValue.AddListener((float value) => statValue = value);
            character.ApplyTemplate(CreateTemplate());
            Assert.That(current, Is.EqualTo(100f));
            Assert.That(statValue, Is.EqualTo(10f));
            ReactivePropertyFloat oldHp = character.HpState;
            ReactivePropertyFloat oldStat = character.GetStatState("Wisdom");

            character.RebuildRuntime();
            int notificationsBefore = currentNotifications;
            oldHp.Value = 5f;
            oldStat.Value = 90f;
            Assert.That(currentNotifications, Is.EqualTo(notificationsBefore));
            Assert.That(current, Is.EqualTo(100f));
            Assert.That(statValue, Is.EqualTo(10f));
            character.Damage(10f);
            character.SetStatBase("Wisdom", 20f);
            Assert.That(current, Is.EqualTo(90f));
            Assert.That(statValue, Is.EqualTo(20f));
            Assert.That(currentNotifications, Is.EqualTo(notificationsBefore + 1));
        }

        [Test]
        public void TargetSelector_IgnoreSelfExcludesChildColliders()
        {
            RpgCharacter source = CreateCharacter();
            source.transform.position = new Vector3(20000f, 0f, 0f);
            GameObject ownCollider = Track(new GameObject("AuditRpgOwnCollider"));
            ownCollider.transform.SetParent(source.transform, false);
            ownCollider.AddComponent<SphereCollider>();
            RpgCharacter enemy = CreateCharacter();
            enemy.transform.position = source.transform.position + Vector3.right * 2f;
            enemy.gameObject.AddComponent<SphereCollider>();
            RpgTargetSelector selector = source.gameObject.AddComponent<RpgTargetSelector>();
            Physics.SyncTransforms();

            Assert.That(selector.SelectTarget(), Is.EqualTo(enemy.gameObject));
            Assert.That(selector.HasTargetState.CurrentValue, Is.True);
        }

        [Test]
        public void NullStatId_UsesUnknownStatResultsWithoutChangingOtherStats()
        {
            RpgCharacter character = CreateCharacter();
            character.SetStatBase(null, 100f);
            character.AddStatBase(null, 100f);
            Assert.That(character.GetStat(null), Is.Zero);
            Assert.That(character.GetStatState(null), Is.Null);
            Assert.That(character.GetUpgradeLevel(null), Is.Zero);
            Assert.That(character.CanUpgradeStat(null), Is.False);
            Assert.That(character.GetStat("Wisdom"), Is.EqualTo(10f));
        }

        [Test]
        public void ProjectileWithoutPrefab_FailsBeforeCostOrCooldown()
        {
            RpgCharacter character = CreateCharacter();
            RpgAttackController controller = character.gameObject.AddComponent<RpgAttackController>();
            RpgAttackDefinition attack = Track(ScriptableObject.CreateInstance<RpgAttackDefinition>());
            JsonUtility.FromJsonOverwrite("{\"_id\":\"missing-projectile\",\"_deliveryType\":2," +
                                          "\"_costResourceId\":\"Mana\",\"_costAmount\":5}", attack);
            RpgAttackPreset preset = Track(ScriptableObject.CreateInstance<RpgAttackPreset>());
            Configure(preset, "_attackDefinition", attack);
            Configure(preset, "_requireTarget", false);

            Assert.That(controller.TryUsePreset(preset, out string reason), Is.False);
            Assert.That(reason, Does.Contain("prefab"));
            Assert.That(character.ManaValue, Is.EqualTo(100f));
            Assert.That(controller.GetRemainingCooldown(attack.Id), Is.Zero);
            Assert.That(controller.GetRemainingCooldown(null), Is.Zero);
        }

        private RpgCharacter CreateCharacter(RpgProgressionDefinition progression = null)
        {
            GameObject actor = Track(new GameObject("AuditRpgActor"));
            RpgCharacter character = actor.AddComponent<RpgCharacter>();
            RpgCharacterTemplate template = CreateTemplate();
            template.progression = progression;
            character.ApplyTemplate(template);
            return character;
        }

        private RpgCharacterTemplate CreateTemplate()
        {
            RpgCharacterTemplate template = Track(ScriptableObject.CreateInstance<RpgCharacterTemplate>());
            template.resources = new[]
            {
                new RpgResourceDefinition { id = new RpgStatId(RpgStatPreset.Hp) },
                new RpgResourceDefinition
                {
                    id = new RpgStatId("Mana"), regen = new RpgRegenDefinition { enabled = true, value = 1f }
                }
            };
            template.stats = new[] { new RpgStatDefinition { id = new RpgStatId("Wisdom"), baseValue = 10f } };
            return template;
        }

        private RpgProgressionDefinition CreateProgression(int cost)
        {
            RpgProgressionDefinition progression = Track(ScriptableObject.CreateInstance<RpgProgressionDefinition>());
            progression.upgradeRules = new[]
            {
                new RpgStatUpgradeRule
                {
                    statId = new RpgStatId("Wisdom"), costPerUpgrade = cost, increasePerPoint = 2f,
                    derivedResourceModifiers = new[]
                    {
                        new RpgResourceModifier
                        {
                            resourceId = new RpgStatId(RpgStatPreset.Hp), kind = RpgResourceModifierKind.AddMaxFlat, value = 15f
                        },
                        new RpgResourceModifier
                        {
                            resourceId = new RpgStatId("Mana"), kind = RpgResourceModifierKind.AddRegenFlat, value = 1.5f
                        }
                    }
                }
            };
            return progression;
        }

        private T Track<T>(T value) where T : Object
        {
            _objects.Add(value);
            return value;
        }

        private static void Configure(object target, string fieldName, object value)
        {
            FieldInfo field = target.GetType().GetField(fieldName, BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.That(field, Is.Not.Null, fieldName);
            field.SetValue(target, value);
        }
    }
}
