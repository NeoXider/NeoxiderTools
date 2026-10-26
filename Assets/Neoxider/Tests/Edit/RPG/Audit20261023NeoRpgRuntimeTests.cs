using System.Collections.Generic;
using Neo.Rpg;
using Neo.Rpg.Runtime;
using NUnit.Framework;
using UnityEngine;

namespace Neo.Tests.Edit.RPG
{
    public sealed class Audit20261023NeoRpgRuntimeTests
    {
        [Test]
        public void ProfileSerialization_PreservesBuffStacksAndClonesEntries()
        {
            RpgCharacterProfileData source = new();
            source.ActiveBuffs.Add(new ActiveBuffEntry { BuffId = "rage", Stacks = 3, ExpiresAtUtc = 1234d });
            RpgCharacterProfileService service = new();

            RpgCharacterProfileData restored = service.Deserialize(service.Serialize(source));
            RpgCharacterProfileData clone = source.Clone();

            Assert.That(restored.ActiveBuffs[0].Stacks, Is.EqualTo(3));
            Assert.That(restored.ActiveBuffs[0].ExpiresAtUtc, Is.EqualTo(1234d));
            clone.ActiveBuffs[0].Stacks = 1;
            Assert.That(source.ActiveBuffs[0].Stacks, Is.EqualTo(3));
        }

        [Test]
        public void ProfileSerialization_DropsNullRowsWithoutMutatingSource()
        {
            RpgCharacterProfileData source = new();
            source.Resources.Add(null);
            source.Resources.Add(new RpgResourceSaveEntry { Id = "Mana", Current = 4f, Max = 10f });
            source.Stats.Add(null);
            source.Stats.Add(new RpgStatSaveEntry { Id = "Wisdom", Base = 7f });
            source.Upgrades.Add(null);
            source.Upgrades.Add(new RpgUpgradeSaveEntry { StatId = "Wisdom", Count = 2 });
            source.ActiveBuffs.Add(null);
            source.ActiveBuffs.Add(new ActiveBuffEntry { BuffId = "rage", Stacks = 3 });
            source.ActiveStatuses.Add(null);
            source.ActiveStatuses.Add(new ActiveStatusEntry { StatusId = "slow", Stacks = 2 });
            RpgCharacterProfileService service = new();

            RpgCharacterProfileData restored = service.Deserialize(service.Serialize(source));

            Assert.That(restored.Resources, Has.Count.EqualTo(1));
            Assert.That(restored.Resources[0].Current, Is.EqualTo(4f));
            Assert.That(restored.Stats[0].Base, Is.EqualTo(7f));
            Assert.That(restored.Upgrades[0].Count, Is.EqualTo(2));
            Assert.That(restored.ActiveBuffs[0].Stacks, Is.EqualTo(3));
            Assert.That(restored.ActiveStatuses[0].Stacks, Is.EqualTo(2));
            Assert.That(source.Resources, Has.Count.EqualTo(2));
            Assert.That(source.ActiveBuffs[0], Is.Null);
        }

        [Test]
        public void ProfileDeserialization_NullCollectionsBecomeUsableEmptyLists()
        {
            const string json = "{\"_resources\":null,\"_stats\":null,\"_upgrades\":null," +
                                "\"_activeBuffs\":null,\"_activeStatuses\":null}";
            RpgCharacterProfileService service = new();
            RpgCharacterProfileData restored = service.Deserialize(json);
            RpgCharacterProfileData raw = JsonUtility.FromJson<RpgCharacterProfileData>(json);
            Assert.That(service.Deserialize(service.Serialize(raw)).Resources, Is.Empty);

            Assert.That(restored.Resources, Is.Empty);
            Assert.That(restored.Stats, Is.Empty);
            Assert.That(restored.Upgrades, Is.Empty);
            Assert.That(restored.ActiveBuffs, Is.Empty);
            Assert.That(restored.ActiveStatuses, Is.Empty);
            restored.Resources.Add(new RpgResourceSaveEntry { Id = "Mana", Current = 2f, Max = 3f });
            Assert.That(service.Deserialize(service.Serialize(restored)).Resources[0].Current, Is.EqualTo(2f));
        }

        [TestCase(-5f, true, false, -5f, 0f)]
        [TestCase(150f, false, true, 150f, 1f)]
        public void ResourceConstruction_HonorsCurrentLimitsAndNormalizedState(float current, bool belowZero,
            bool overfill, float expectedCurrent, float expectedPercent)
        {
            RpgResourceDefinition definition = CreateResource(current);
            definition.canGoBelowZero = belowZero;
            definition.canOverfill = overfill;
            definition.minValue = -10f;

            RpgResourceRuntime resource = new(definition);

            Assert.That(resource.Current, Is.EqualTo(expectedCurrent));
            Assert.That(resource.CurrentState.CurrentValue, Is.EqualTo(expectedCurrent));
            Assert.That(resource.PercentState.CurrentValue, Is.EqualTo(expectedPercent));
        }

        [Test]
        public void ResourceConstruction_EnforcesMaxCapBeforePublishingInitialState()
        {
            RpgResourceDefinition definition = CreateResource(90f);
            definition.maxCap = 50f;
            RpgResourceRuntime resource = new(definition);

            Assert.That(resource.Max, Is.EqualTo(50f));
            Assert.That(resource.Current, Is.EqualTo(50f));
            Assert.That(resource.MaxState.CurrentValue, Is.EqualTo(50f));
            Assert.That(resource.PercentState.CurrentValue, Is.EqualTo(1f));
        }

        [TestCase(null)]
        [TestCase("")]
        [TestCase("  ")]
        public void ResourceService_InvalidIdsReturnUnknownResourceResults(string id)
        {
            RpgCharacterResourceService service = new();
            service.Build(new[] { CreateResource(20f) });

            Assert.That(service.TryGet(id, out RpgResourceRuntime resource), Is.False);
            Assert.That(resource, Is.Null);
            Assert.That(service.GetCurrent(id), Is.Zero);
            Assert.That(service.GetMaxState(id), Is.Null);
            Assert.That(service.Spend(id, 1f), Is.False);
            Assert.That(service.GetCurrent("Mana"), Is.EqualTo(20f));
        }

        [Test]
        public void StatConstruction_EnforcesDefinitionLimitsBeforePublishingInitialState()
        {
            RpgStatRuntime stat = new(new RpgStatDefinition { baseValue = 100f, maxValue = 10f });
            Assert.That(stat.BaseValue, Is.EqualTo(100f));
            Assert.That(stat.CurrentValue, Is.EqualTo(10f));
            Assert.That(stat.ValueState.CurrentValue, Is.EqualTo(10f));
        }

        [TestCase(RpgRegenMode.FlatPerTick)]
        [TestCase(RpgRegenMode.PercentMaxPerTick)]
        public void TickRegen_UsesFlatAndPercentBuffsWithoutChangingTickCadence(RpgRegenMode mode)
        {
            RpgCharacterResourceService service = new();
            RpgResourceDefinition definition = CreateResource(0f);
            definition.regen = new RpgRegenDefinition { enabled = true, mode = mode, value = 10f, tickInterval = 0.5f };
            service.Build(new[] { definition });
            List<BuffStatModifierApplication> buffs = new()
            {
                new BuffStatModifierApplication(BuffStatType.RegenFlat, "Mana", null, 4f, 1),
                new BuffStatModifierApplication(BuffStatType.RegenPercent, "Mana", null, 50f, 1)
            };
            service.RefreshDerived((RpgResourceRuntime resource) => resource.BaseMax,
                (RpgResourceRuntime resource) => RpgStatResolver.ResolveRegen(resource, null, buffs), false);

            service.TickRegen(0.25f, false);
            Assert.That(service.GetCurrent("Mana"), Is.Zero);
            service.TickRegen(0.75f, false);
            Assert.That(service.GetCurrent("Mana"), Is.EqualTo(36f).Within(0.001f));
        }

        [Test]
        public void ResourceMaxChange_RefreshesPercentTickRegen()
        {
            RpgCharacterResourceService service = new();
            RpgResourceDefinition definition = CreateResource(0f);
            definition.regen = new RpgRegenDefinition
            {
                enabled = true, mode = RpgRegenMode.PercentMaxPerTick, value = 10f, tickInterval = 1f
            };
            service.Build(new[] { definition });
            service.SetMax("Mana", 200f);
            service.TickRegen(1f, false);
            Assert.That(service.GetCurrent("Mana"), Is.EqualTo(20f));
            service.AddMax("Mana", 100f);
            service.TickRegen(1f, false);
            Assert.That(service.GetCurrent("Mana"), Is.EqualTo(50f));
        }

        [Test]
        public void RegenResolver_AppliesInvestedUpgradeBonusBeforeBuffPercent()
        {
            RpgResourceDefinition definition = CreateResource(0f);
            definition.regen = new RpgRegenDefinition { enabled = true, value = 1f };
            RpgResourceRuntime resource = new(definition);
            Dictionary<string, RpgStatRuntime> stats = new()
            {
                ["Wisdom"] = new RpgStatRuntime(new RpgStatDefinition { id = new RpgStatId("Wisdom") })
                    { UpgradeCount = 2 }
            };
            Dictionary<string, RpgStatUpgradeRule> rules = new()
            {
                ["Wisdom"] = new RpgStatUpgradeRule
                {
                    derivedResourceModifiers = new[]
                    {
                        new RpgResourceModifier
                        {
                            resourceId = new RpgStatId("Mana"), kind = RpgResourceModifierKind.AddRegenFlat, value = 1.5f
                        }
                    }
                }
            };
            BuffStatModifierApplication[] buffs =
            {
                new(BuffStatType.RegenPercent, "Mana", null, 100f, 1)
            };

            Assert.That(RpgStatResolver.ResolveRegen(resource, stats, buffs, rules), Is.EqualTo(8f));
            Assert.That(RpgStatResolver.ResolveRegen(resource, stats, buffs), Is.EqualTo(2f));
        }

        [TestCase(true)]
        [TestCase(false)]
        public void EffectExpiry_CallbackMayClearRemainingEntries(bool buffs)
        {
            RpgEffectShelf shelf = new();
            shelf.RestoreActiveEffects(new[]
            {
                new ActiveBuffEntry { BuffId = "one", ExpiresAtUtc = 0d },
                new ActiveBuffEntry { BuffId = "two", ExpiresAtUtc = 0d }
            }, new[]
            {
                new ActiveStatusEntry { StatusId = "one", ExpiresAtUtc = 0d },
                new ActiveStatusEntry { StatusId = "two", ExpiresAtUtc = 0d }
            });
            int callbacks = 0;
            if (!buffs)
            {
                shelf.ClearAllBuffs();
            }

            shelf.Tick(0f, (string id) =>
            {
                callbacks++;
                shelf.ClearAllBuffs();
            }, (string id) =>
            {
                callbacks++;
                shelf.ClearAllStatuses();
            }, null);

            Assert.That(callbacks, Is.EqualTo(1));
            Assert.That(buffs ? shelf.ActiveBuffs.Count : shelf.ActiveStatuses.Count, Is.Zero);
        }

        [Test]
        public void StatusDamage_CallbackMayRemoveStatusAndStopsFurtherDamage()
        {
            StatusEffectDefinition status = ScriptableObject.CreateInstance<StatusEffectDefinition>();
            try
            {
                JsonUtility.FromJsonOverwrite("{\"_id\":\"poison\",\"_duration\":100," +
                                              "\"_tickDamagePerSecond\":2,\"_tickInterval\":0.25}", status);
                RpgEffectShelf shelf = new();
                shelf.ApplyStatus(status);
                float totalDamage = 0f;
                shelf.Tick(1f, null, null, (StatusEffectDefinition definition, float amount) =>
                {
                    totalDamage += amount;
                    shelf.ClearAllStatuses();
                });

                Assert.That(totalDamage, Is.EqualTo(0.5f));
                Assert.That(shelf.ActiveStatuses, Is.Empty);
            }
            finally
            {
                Object.DestroyImmediate(status);
            }
        }

        private static RpgResourceDefinition CreateResource(float current)
        {
            return new RpgResourceDefinition
            {
                id = new RpgStatId("Mana"), startCurrent = current, startMax = 100f, restoreToFull = false
            };
        }
    }
}
