using System.Collections.Generic;
using System.Reflection;
using Neo.Core.Resources;
using Neo.Rpg;
using Neo.Rpg.Components;
using Neo.Rpg.Runtime;
using NUnit.Framework;
using UnityEngine;

namespace Neo.Tests.Edit.RPG
{
    /// <summary>
    ///     Contracts of the plain-C# services extracted from <see cref="RpgCharacter" /> (stats, progression,
    ///     modifier aggregation) plus the facade regressions they fixed.
    /// </summary>
    public sealed class RpgCharacterServicesTests
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
        public void StatService_BuildsResolvesAndTracksInvestments()
        {
            RpgCharacterStatService service = new();
            service.Build(new[]
            {
                new RpgStatDefinition { id = new RpgStatId("Wisdom"), baseValue = 10f },
                null,
                new RpgStatDefinition { id = new RpgStatId("Luck"), baseValue = 3f }
            });
            service.BuildUpgradeRules(CreateProgression(1, 2));

            Assert.That(service.Count, Is.EqualTo(2));
            Assert.That(service.Contains("Wisdom"), Is.True);
            Assert.That(service.Contains(null), Is.False);
            Assert.That(service.AddBase("Wisdom", 5f), Is.True);
            Assert.That(service.SetBase("Missing", 5f), Is.False);
            Assert.That(service.TryGet("Wisdom", out RpgStatRuntime wisdom), Is.True);
            Assert.That(wisdom.BaseValue, Is.EqualTo(15f));

            Assert.That(service.CanUpgrade("Wisdom", 0), Is.False);
            Assert.That(service.CanUpgrade("Wisdom", 1), Is.True);
            Assert.That(service.CanUpgrade("Luck", 10), Is.False, "No rule configured for Luck.");
            Assert.That(service.AddInvestment("Wisdom"), Is.EqualTo(1));
            Assert.That(service.AddInvestment("Wisdom"), Is.EqualTo(2));
            Assert.That(wisdom.UpgradeCount, Is.EqualTo(2));
            Assert.That(service.CanUpgrade("Wisdom", 10), Is.False, "Upgrade cap of 2 reached.");
            Assert.That(service.ResolveStat(wisdom, 1, null), Is.EqualTo(19f));

            service.ClearInvestments();
            Assert.That(service.GetUpgradeLevel("Wisdom"), Is.Zero);
            Assert.That(wisdom.UpgradeCount, Is.Zero);
        }

        [Test]
        public void StatService_CaptureAndRestoreRoundTripBasesAndInvestments()
        {
            RpgCharacterStatService source = new();
            source.Build(new[] { new RpgStatDefinition { id = new RpgStatId("Wisdom"), baseValue = 10f } });
            source.SetBase("Wisdom", 42f);
            source.SetInvestment("Wisdom", 3);
            List<RpgStatSaveEntry> stats = new();
            List<RpgUpgradeSaveEntry> upgrades = new();
            source.CaptureTo(stats, upgrades);

            RpgCharacterStatService target = new();
            target.Build(new[] { new RpgStatDefinition { id = new RpgStatId("Wisdom"), baseValue = 10f } });
            target.SetInvestment("Wisdom", 9);
            target.RestoreBases(stats);
            target.RestoreInvestments(upgrades);

            Assert.That(target.Stats["Wisdom"].BaseValue, Is.EqualTo(42f));
            Assert.That(target.GetUpgradeLevel("Wisdom"), Is.EqualTo(3));
            Assert.That(target.Stats["Wisdom"].UpgradeCount, Is.EqualTo(3));
        }

        [Test]
        public void ProgressionService_AutomaticGrowthFollowsLevelAndManualGrowthWaitsForApply()
        {
            RpgCharacterProgressionService automatic = new() { Definition = CreateProgression(1, -1) };
            Assert.That(automatic.UsesAutomaticGrowth, Is.True);
            Assert.That(automatic.SetLevel(4), Is.True);
            Assert.That(automatic.AppliedGrowthLevel, Is.EqualTo(4));
            Assert.That(automatic.SetLevel(4), Is.False);

            RpgProgressionDefinition manualDefinition = CreateProgression(1, -1);
            manualDefinition.growthMode = RpgLevelGrowthMode.ManualUpgradePoints;
            manualDefinition.upgradePointsPerLevel = 3;
            RpgCharacterProgressionService manual = new() { Definition = manualDefinition };
            manual.SetLevel(3);
            Assert.That(manual.AppliedGrowthLevel, Is.EqualTo(1));
            Assert.That(manual.GetUpgradePointsForLevelChange(1, 3), Is.EqualTo(6));
            Assert.That(manual.GetUpgradePointsForLevelChange(3, 1), Is.Zero);
            manual.ApplyGrowthAtCurrentLevel();
            Assert.That(manual.AppliedGrowthLevel, Is.EqualTo(3));
            manual.ResetGrowthAnchor();
            Assert.That(manual.AppliedGrowthLevel, Is.EqualTo(1));
        }

        [Test]
        public void ProgressionService_SaturatesInsteadOfOverflowing()
        {
            RpgProgressionDefinition definition = CreateProgression(1, -1);
            definition.growthMode = RpgLevelGrowthMode.ManualUpgradePoints;
            definition.upgradePointsPerLevel = int.MaxValue;
            RpgCharacterProgressionService service = new() { Definition = definition };

            service.AddUpgradePoints(int.MaxValue);
            service.AddUpgradePoints(int.MaxValue);
            Assert.That(service.UpgradePoints, Is.EqualTo(int.MaxValue));
            service.AddUpgradePoints(int.MinValue);
            Assert.That(service.UpgradePoints, Is.Zero);
            Assert.That(service.GetUpgradePointsForLevelChange(1, 3), Is.EqualTo(int.MaxValue));
            Assert.That(RpgCharacterProgressionService.ClampLevel((long)int.MaxValue + 5), Is.EqualTo(int.MaxValue));
            Assert.That(RpgCharacterProgressionService.ClampLevel(-7), Is.EqualTo(1));
        }

        [Test]
        public void ProgressionService_RejectsNonFiniteExperience()
        {
            RpgCharacterProgressionService service = new();
            Assert.That(service.AddXp(5f), Is.True);
            Assert.That(service.AddXp(float.NaN), Is.False);
            Assert.That(service.AddXp(float.PositiveInfinity), Is.False);
            Assert.That(service.AddXp(-1f), Is.False);
            Assert.That(service.Xp, Is.EqualTo(5f));
            service.AddXp(float.MaxValue);
            service.AddXp(float.MaxValue);
            Assert.That(service.Xp, Is.EqualTo(float.MaxValue));
        }

        [Test]
        public void Character_NaNExperienceAndPointOverflowNoLongerCorruptProgression()
        {
            RpgCharacter character = CreateCharacter();
            int xpNotifications = 0;
            character.XpState.AddListener((float _) => xpNotifications++);

            character.AddXp(10f);
            character.AddXp(float.NaN);
            character.AddUpgradePoints(int.MaxValue);
            character.AddUpgradePoints(1);

            Assert.That(character.XpValue, Is.EqualTo(10f));
            Assert.That(xpNotifications, Is.EqualTo(1));
            Assert.That(character.UpgradePointsValue, Is.EqualTo(int.MaxValue),
                "Overflow used to wrap negative and clamp the whole balance to 0.");
            Assert.That(character.CaptureProfile().Xp, Is.EqualTo(10f));
        }

        [Test]
        public void StatService_RejectsNonFiniteBasesAndOverflowingSums()
        {
            RpgCharacterStatService service = new();
            service.Build(new[] { new RpgStatDefinition { id = new RpgStatId("Wisdom"), baseValue = 10f } });

            Assert.That(service.SetBase("Wisdom", float.NaN), Is.False);
            Assert.That(service.SetBase("Wisdom", float.PositiveInfinity), Is.False);
            Assert.That(service.AddBase("Wisdom", float.NaN), Is.False);
            Assert.That(service.AddBase("Wisdom", float.NegativeInfinity), Is.False);
            Assert.That(service.TryGet("Wisdom", out RpgStatRuntime wisdom), Is.True);
            Assert.That(wisdom.BaseValue, Is.EqualTo(10f));

            Assert.That(service.SetBase("Wisdom", float.MaxValue), Is.True);
            Assert.That(service.AddBase("Wisdom", float.MaxValue), Is.False, "the sum overflows to infinity");
            service.RestoreBases(new[]
            {
                new RpgStatSaveEntry { Id = "Wisdom", Base = float.NaN },
                new RpgStatSaveEntry { Id = "Wisdom", Base = float.NegativeInfinity }
            });

            Assert.That(wisdom.BaseValue, Is.EqualTo(float.MaxValue), "corrupted save rows keep the current base");
        }

        [Test]
        public void Runtimes_IgnoreNaNWritesAndKeepTheLastFiniteValue()
        {
            RpgResourceRuntime hp = new(new RpgResourceDefinition
            {
                id = new RpgStatId(RpgStatPreset.Hp), startMax = 100f, startCurrent = 40f, restoreOnAwake = false
            });
            RpgStatRuntime wisdom = new(new RpgStatDefinition { id = new RpgStatId("Wisdom"), baseValue = 10f });

            hp.SetMax(float.NaN, true);
            hp.SetCurrent(float.NaN);
            wisdom.SetCurrent(float.NaN);

            Assert.That(hp.Max, Is.EqualTo(100f));
            Assert.That(hp.Current, Is.EqualTo(40f));
            Assert.That(hp.PercentState.Value, Is.EqualTo(0.4f).Within(1e-5f));
            Assert.That(wisdom.CurrentValue, Is.EqualTo(10f));
            Assert.That(wisdom.ValueState.Value, Is.EqualTo(10f));

            hp.SetMax(float.PositiveInfinity, true);
            Assert.That(hp.Max, Is.EqualTo(float.MaxValue), "infinity still clamps to the finite ceiling");
        }

        [Test]
        public void Character_NonFiniteStatBaseChangesCannotPoisonDerivedValues()
        {
            RpgCharacter character = CreateCharacter();
            int statChanges = 0;
            character.OnStatChangedEvent.AddListener((string id, float value) => statChanges++);

            character.SetStatBase("Wisdom", float.NaN);
            character.AddStatBase("Wisdom", float.PositiveInfinity);

            Assert.That(character.GetStat("Wisdom"), Is.EqualTo(10f));
            Assert.That(statChanges, Is.Zero, "a rejected base change must not refresh or notify");
            Assert.That(character.MaxHp, Is.EqualTo(100f));
            Assert.That(character.CaptureProfile().Stats[0].Base, Is.EqualTo(10f));

            character.SetStatBase("Wisdom", 12f);
            Assert.That(character.GetStat("Wisdom"), Is.EqualTo(12f), "finite values still apply");
        }

        [Test]
        public void NetworkCommand_IsKnownTypeAcceptsEveryNamedValueOnly()
        {
            System.Array values = System.Enum.GetValues(typeof(RpgCharacterNetworkCommandType));
            foreach (RpgCharacterNetworkCommandType type in values)
            {
                Assert.That(RpgCharacterNetworkCommand.IsKnownType(type), Is.True,
                    $"{type} must be accepted; extend IsKnownType when a command is added");
            }

            Assert.That(RpgCharacterNetworkCommand.IsKnownType((RpgCharacterNetworkCommandType)(-1)), Is.False);
            Assert.That(RpgCharacterNetworkCommand.IsKnownType((RpgCharacterNetworkCommandType)values.Length), Is.False);
            Assert.That(RpgCharacterNetworkCommand.IsKnownType((RpgCharacterNetworkCommandType)int.MaxValue), Is.False);
        }

        [Test]
        public void Character_UpgradeAndProfileStillFlowThroughExtractedServices()
        {
            RpgCharacter character = CreateCharacter(CreateProgression(1, -1));
            character.AddUpgradePoints(2);
            Assert.That(character.UpgradeStat("Wisdom"), Is.True);
            Assert.That(character.GetUpgradeLevel("Wisdom"), Is.EqualTo(1));
            Assert.That(character.GetStat("Wisdom"), Is.EqualTo(12f));
            RpgCharacterProfileData saved = character.CaptureProfile();
            Assert.That(saved.Upgrades.Count, Is.EqualTo(1));
            Assert.That(saved.Stats.Count, Is.EqualTo(1));

            character.UpgradeStat("Wisdom");
            Assert.That(character.GetStat("Wisdom"), Is.EqualTo(14f));
            character.ApplyProfile(saved);

            Assert.That(character.GetUpgradeLevel("Wisdom"), Is.EqualTo(1));
            Assert.That(character.GetStat("Wisdom"), Is.EqualTo(12f));
            Assert.That(character.UpgradePointsValue, Is.EqualTo(1));
        }

        [Test]
        public void ModifierMath_FoldsLegacyAliasesLikeTheFacade()
        {
            List<BuffStatModifierApplication> modifiers = new()
            {
                new BuffStatModifierApplication(BuffStatType.OutgoingDamagePercent, null, null, 10f, 2),
                new BuffStatModifierApplication(BuffStatType.DamagePercent, null, null, 5f, 1),
                new BuffStatModifierApplication(BuffStatType.IncomingDamagePercent, null, null, 20f, 1),
                new BuffStatModifierApplication(BuffStatType.DefensePercent, null, null, 10f, 1),
                new BuffStatModifierApplication(BuffStatType.DamageTypeResistPercent, null, "Fire", 30f, 1),
                new BuffStatModifierApplication(BuffStatType.MoveSpeedPercent, null, null, 25f, 1),
                new BuffStatModifierApplication(BuffStatType.MovementSpeedPercent, null, null, 25f, 1),
                new BuffStatModifierApplication(BuffStatType.AttackSpeedPercent, null, null, 50f, 2)
            };

            Assert.That(RpgModifierMath.GetOutgoingDamageMultiplier(modifiers), Is.EqualTo(1.25f).Within(1e-5f));
            Assert.That(RpgModifierMath.GetIncomingDamageMultiplier(modifiers, null), Is.EqualTo(1.1f).Within(1e-5f));
            Assert.That(RpgModifierMath.GetIncomingDamageMultiplier(modifiers, "Fire"), Is.EqualTo(0.8f).Within(1e-5f));
            Assert.That(RpgModifierMath.GetMovementSpeedBuffMultiplier(modifiers), Is.EqualTo(1.5f).Within(1e-5f));
            Assert.That(RpgModifierMath.GetAttackSpeedMultiplier(modifiers), Is.EqualTo(2f).Within(1e-5f));
            Assert.That(RpgModifierMath.GetOutgoingDamageMultiplier(null), Is.EqualTo(1f));
        }

        [Test]
        public void EffectShelf_StatusMovementAndBlockingQueriesIgnoreUnknownStatuses()
        {
            RpgEffectShelf shelf = new();
            StatusEffectDefinition slow = CreateStatus("slow", 0.5f, false);
            StatusEffectDefinition stun = CreateStatus("stun", 1f, true);
            shelf.RegisterStatusLibrary(new[] { slow, stun });
            Assert.That(shelf.GetStatusMovementMultiplier(), Is.EqualTo(1f));
            Assert.That(shelf.HasBlockingStatus(), Is.False);

            shelf.ApplyStatus(slow);
            Assert.That(shelf.GetStatusMovementMultiplier(), Is.EqualTo(0.5f));
            Assert.That(shelf.HasBlockingStatus(), Is.False);
            shelf.ApplyStatus(stun);
            Assert.That(shelf.HasBlockingStatus(), Is.True);
            shelf.RestoreActiveEffects(null, new[] { new ActiveStatusEntry { StatusId = "unknown" } });
            Assert.That(shelf.GetStatusMovementMultiplier(), Is.EqualTo(1f));
            Assert.That(shelf.HasBlockingStatus(), Is.False);
        }

        private RpgCharacter CreateCharacter(RpgProgressionDefinition progression = null)
        {
            GameObject actor = Track(new GameObject("RpgServicesActor"));
            RpgCharacter character = actor.AddComponent<RpgCharacter>();
            RpgCharacterTemplate template = Track(ScriptableObject.CreateInstance<RpgCharacterTemplate>());
            template.resources = new[] { new RpgResourceDefinition { id = new RpgStatId(RpgStatPreset.Hp) } };
            template.stats = new[] { new RpgStatDefinition { id = new RpgStatId("Wisdom"), baseValue = 10f } };
            template.progression = progression;
            character.ApplyTemplate(template);
            Assert.That(character.MaxHp, Is.EqualTo(100f));
            Assert.That(character.HpValue, Is.EqualTo(100f));
            Assert.That(character.Resources.ContainsKey(RpgResourceId.Hp), Is.True);
            return character;
        }

        private RpgProgressionDefinition CreateProgression(int cost, int maxUpgrades)
        {
            RpgProgressionDefinition progression = Track(ScriptableObject.CreateInstance<RpgProgressionDefinition>());
            progression.growthMode = RpgLevelGrowthMode.Hybrid;
            progression.upgradeRules = new[]
            {
                new RpgStatUpgradeRule
                {
                    statId = new RpgStatId("Wisdom"),
                    increasePerPoint = 2f,
                    costPerUpgrade = cost,
                    maxUpgradeCount = maxUpgrades
                }
            };
            return progression;
        }

        private StatusEffectDefinition CreateStatus(string id, float movement, bool blocks)
        {
            StatusEffectDefinition status = Track(ScriptableObject.CreateInstance<StatusEffectDefinition>());
            Set(status, "_id", id);
            Set(status, "_movementSpeedMultiplier", movement);
            Set(status, "_blocksActions", blocks);
            Set(status, "_duration", 1000000f);
            return status;
        }

        private T Track<T>(T value) where T : Object
        {
            _objects.Add(value);
            return value;
        }

        private static void Set(object target, string name, object value)
        {
            FieldInfo field = target.GetType().GetField(name, BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.That(field, Is.Not.Null, name);
            field.SetValue(target, value);
        }
    }
}
