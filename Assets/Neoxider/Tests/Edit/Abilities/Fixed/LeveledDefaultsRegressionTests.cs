using System.Collections.Generic;
using Neo.Abilities;
using Neo.Abilities.Fixed;
using NUnit.Framework;
using UnityEngine;

namespace Neo.Editor.Tests.Abilities.Fixed
{
    /// <summary>
    ///     Regression guard of the leveled / scalable parameters: content that leaves every new field at its default
    ///     (null, or the empty arrays and strings a Unity asset deserializes to) resolves exactly as before on both
    ///     cores: the same flat values, the same receipts and the same state hash on every tick, and the converter
    ///     emits no new data for it.
    /// </summary>
    public sealed class LeveledDefaultsRegressionTests
    {
        private const int Ticks = 150;

        [Test]
        public void Float_DefaultAndEmptyLeveledFields_PlayTheSameStateHashes()
        {
            List<ulong> legacy = PlayFloat(BuildContent(false));
            List<ulong> deserialized = PlayFloat(BuildContent(true));

            Assert.That(deserialized, Is.EqualTo(legacy), "empty arrays and strings behave like the untouched fields");
        }

        [Test]
        public void Fixed_DefaultAndEmptyLeveledFields_PlayTheSameStateHashes()
        {
            List<ulong> legacy = PlayFixed(BuildContent(false));
            List<ulong> deserialized = PlayFixed(BuildContent(true));

            Assert.That(deserialized, Is.EqualTo(legacy));
        }

        [Test]
        public void Float_DefaultContent_KeepsTheFlatOutcomes()
        {
            AbilitySystem system = new AbilitySystem();
            FakeWorldAdapter world = new FakeWorldAdapter();
            system.World = world;
            AbilityUnit caster = AbilityTestSupport.CreateUnit(system, team: 1, mana: 500f);
            AbilityUnit enemy = AbilityTestSupport.CreateUnit(system, team: 2);
            world.SetPosition(caster.Id, Vector3.zero);
            world.SetPosition(enemy.Id, new Vector3(3f, 0f, 0f));
            caster.SetBaseProperty(AbilityProperties.CooldownReductionPercent, 20f);
            Register(system, BuildContent(true));
            system.GrantAbility(caster.Id, "rg_bolt");
            AbilitySlot nova = system.GrantAbility(caster.Id, "rg_nova");

            Assert.That(system.Cast(CastRequest.AtUnit(caster.Id, "rg_bolt", enemy.Id)).Success, Is.True);
            Assert.That(system.TryGetSlot(caster.Id, "rg_bolt", out AbilitySlot bolt), Is.True);
            Assert.That(bolt.CooldownRemaining, Is.EqualTo(1.2f).Within(1e-5f), "1.5 s minus 20% cdr");
            Assert.That(caster.Resources.GetCurrent(AbilityResourceIds.Mana), Is.EqualTo(480f).Within(1e-4f));
            Assert.That(world.Spawns[0].Magnitude, Is.EqualTo(15f), "the flat projectile speed");
            Assert.That(system.NotifyProjectileHit(world.Spawns[0].CastId, enemy.Id, new Vector3(3f, 0f, 0f)), Is.True);
            Assert.That(enemy.Health, Is.EqualTo(970f).Within(1e-3f), "the level-1 entry of the special");

            Assert.That(nova.Charges, Is.EqualTo(2));
            Assert.That(system.Cast(CastRequest.NoTarget(caster.Id, "rg_nova")).Success, Is.True);
            Assert.That(nova.Charges, Is.EqualTo(1));
            Assert.That(nova.CooldownRemaining, Is.EqualTo(1.6f).Within(1e-5f), "2 s restore minus 20% cdr");
            Assert.That(enemy.GetProperty(AbilityProperties.MoveSpeed, 10f), Is.EqualTo(7f).Within(1e-4f),
                "the flat Mul 0.7 slow");
        }

        [Test]
        public void Resolver_DefaultLeveledValues_MatchTheLegacyFormula()
        {
            AbilitySystem system = new AbilitySystem();
            AbilityUnit caster = AbilityTestSupport.CreateUnit(system, team: 1);
            caster.SetBaseProperty(AbilityProperties.SpellPower, 37f);
            caster.SetLevel(4);
            EffectContext context = AbilityTestSupport.Context(system, caster.Id, targets: caster.Id);
            context.AddSpent(AbilityResourceIds.Mana, 55f);

            float[] bases = { 0f, 7f, -3.25f, 1234.5f };
            for (int b = 0; b < bases.Length; b++)
            {
                for (int level = 1; level <= 5; level++)
                {
                    context.AbilityLevel = level;
                    LeveledValue value = new LeveledValue
                    {
                        Base = bases[b],
                        PerLevel = 1.5f,
                        ScaleProperty = AbilityProperties.SpellPower,
                        ScalePerPoint = 0.3f,
                        LevelFrom = LevelSource.AbilityLevel
                    };
                    float legacy = bases[b] + 1.5f * (level - 1);
                    legacy += 0.3f * caster.GetProperty(AbilityProperties.SpellPower);
                    Assert.That(LeveledValueResolver.Resolve(in value, context, caster.Id), Is.EqualTo(legacy),
                        "bit-identical with the spent term and Add mode off");
                }
            }

            EffectNodeData flat = new EffectNodeData { Amount = 42f };
            Assert.That(LeveledValueResolver.ResolveAmount(flat, context, caster.Id), Is.EqualTo(42f));
            Assert.That(flat.ResolveChance(3), Is.EqualTo(1f));
            Assert.That(flat.ResolveMaxTargets(3), Is.EqualTo(0));
            Assert.That(new PropertyContribution("x", PropertyOp.Add, 2.5f, 0.75f).ValueFor(4, 9),
                Is.EqualTo(new PropertyContribution("x", PropertyOp.Add, 2.5f, 0.75f).ValueForStacks(4)));
        }

        [Test]
        public void Converter_DefaultContent_EmitsNoLeveledData()
        {
            List<FixedConversionIssue> issues = new List<FixedConversionIssue>();
            Content content = BuildContent(true);
            FixedContentBundle bundle = FixedContentConverter.Convert(content.Abilities, content.Modifiers,
                new FixedConversionSettings(), issues);

            foreach (FixedAbilityBlueprint ability in bundle.Abilities)
            {
                Assert.That(ability.RangeByLevelRaw, Is.Null, ability.Id);
                Assert.That(ability.CooldownByLevelTicks, Is.Null, ability.Id);
                Assert.That(ability.MaxChargesByLevel, Is.Null, ability.Id);
                Assert.That(ability.ChargeRestoreByLevelTicks, Is.Null, ability.Id);
                Assert.That(ability.ProjectileSpeedByLevelRaw, Is.Null, ability.Id);
                foreach (FixedAbilityCost cost in ability.Costs)
                {
                    Assert.That(cost.Mode, Is.EqualTo(FixedCostMode.Fixed));
                    Assert.That(cost.AmountByLevelRaw, Is.Null);
                    Assert.That(cost.MinAmountRaw, Is.EqualTo(0));
                }

                AssertDefaultNodes(ability.CastEffects);
                AssertDefaultNodes(ability.ImpactEffects);
            }

            foreach (FixedModifierBlueprint modifier in bundle.Modifiers)
            {
                foreach (FixedPropertyContribution contribution in modifier.Properties)
                {
                    Assert.That(contribution.ValueByLevelRaw, Is.Null, modifier.Id);
                }

                AssertDefaultNodes(modifier.TickEffects);
            }

            List<AbilityContentIssue> validation = AbilityContentValidator.Validate(content.Abilities, content.Modifiers);
            Assert.That(validation, Is.Empty, "default leveled fields raise no validation issue");
        }

        private static void AssertDefaultNodes(List<FixedEffectNode> nodes)
        {
            for (int i = 0; i < nodes.Count; i++)
            {
                FixedEffectNode node = nodes[i];
                Assert.That(node.ChanceByLevelRaw, Is.Null);
                Assert.That(node.MaxTargetsByLevel, Is.Null);
                Assert.That(node.DurationByLevelTicks, Is.Null);
                Assert.That(node.IntervalByLevelTicks, Is.Null);
                Assert.That(node.AmountScaleMode, Is.EqualTo(FixedScaleMode.Add));
                Assert.That(node.AmountScaleStat, Is.EqualTo(FixedScaleStat.Property));
                Assert.That(node.AmountSpentCoefficientRaw, Is.EqualTo(0));
            }
        }

        private static List<ulong> PlayFloat(Content content)
        {
            AbilitySystem system = new AbilitySystem();
            DeterministicAbilityProfile.Apply(system.Rules, 30);
            FakeWorldAdapter world = new FakeWorldAdapter();
            system.World = world;
            AbilityUnit caster = AbilityTestSupport.CreateUnit(system, team: 1, mana: 500f);
            world.SetPosition(caster.Id, Vector3.zero);
            List<AbilityUnit> enemies = new List<AbilityUnit>();
            for (int i = 0; i < 3; i++)
            {
                AbilityUnit enemy = AbilityTestSupport.CreateUnit(system, team: 2);
                world.SetPosition(enemy.Id, new Vector3(2f + i, 0f, 0f));
                enemies.Add(enemy);
            }

            Register(system, content);
            system.GrantAbility(caster.Id, "rg_bolt");
            system.GrantAbility(caster.Id, "rg_nova");
            system.GrantAbility(caster.Id, "rg_field");
            List<ulong> hashes = new List<ulong>(Ticks);
            for (int tick = 0; tick < Ticks; tick++)
            {
                if (tick % 10 == 0)
                {
                    int before = world.Spawns.Count;
                    system.Cast(CastRequest.AtUnit(caster.Id, "rg_bolt", enemies[tick / 10 % 3].Id, (uint)tick + 1u));
                    system.Cast(CastRequest.NoTarget(caster.Id, "rg_nova", (uint)tick + 7u));
                    system.Cast(CastRequest.NoTarget(caster.Id, "rg_field", (uint)tick + 11u));
                    if (world.Spawns.Count > before)
                    {
                        SpawnRequest spawn = world.Spawns[world.Spawns.Count - 1];
                        system.NotifyProjectileHit(spawn.CastId, spawn.TargetUnit, new Vector3(3f, 0f, 0f));
                    }
                }

                system.StepTicks(1);
                hashes.Add(AbilityStateHasher.Compute(system));
            }

            return hashes;
        }

        private static List<ulong> PlayFixed(Content content)
        {
            FixedConversionSettings settings = new FixedConversionSettings();
            FixedAbilitySystem system = new FixedAbilitySystem(settings.CreateRules());
            FixedRecordingWorld world = new FixedRecordingWorld(system);
            system.World = world;
            FixedUnit caster = FixedTestSupport.Unit(system, 1, mana: 500d);
            List<FixedUnit> enemies = new List<FixedUnit>();
            for (int i = 0; i < 3; i++)
            {
                enemies.Add(FixedTestSupport.Unit(system, 2, x: 2d + i));
            }

            FixedContentConverter.Convert(content.Abilities, content.Modifiers, settings, null).RegisterInto(system);
            system.GrantAbility(caster.Id, "rg_bolt");
            system.GrantAbility(caster.Id, "rg_nova");
            system.GrantAbility(caster.Id, "rg_field");
            List<ulong> hashes = new List<ulong>(Ticks);
            for (int tick = 0; tick < Ticks; tick++)
            {
                if (tick % 10 == 0)
                {
                    int before = world.Spawns.Count;
                    FixedCastRequest bolt = FixedCastRequest.AtUnit(caster.Id, "rg_bolt", enemies[tick / 10 % 3].Id,
                        (uint)tick + 1u);
                    FixedCastRequest nova = FixedCastRequest.NoTarget(caster.Id, "rg_nova", (uint)tick + 7u);
                    FixedCastRequest field = FixedCastRequest.NoTarget(caster.Id, "rg_field", (uint)tick + 11u);
                    system.Cast(in bolt);
                    system.Cast(in nova);
                    system.Cast(in field);
                    if (world.Spawns.Count > before)
                    {
                        FixedSpawnRequest spawn = world.Spawns[world.Spawns.Count - 1];
                        system.NotifyProjectileHit(spawn.CastId, spawn.TargetUnit, FixedVec3.Zero);
                    }
                }

                system.Step();
                hashes.Add(FixedStateHasher.Compute(system));
            }

            return hashes;
        }

        private static void Register(AbilitySystem system, Content content)
        {
            for (int i = 0; i < content.Modifiers.Count; i++)
            {
                system.RegisterModifier(content.Modifiers[i]);
            }

            for (int i = 0; i < content.Abilities.Count; i++)
            {
                system.RegisterAbility(content.Abilities[i]);
            }
        }

        private static Content BuildContent(bool emptyLeveledFields)
        {
            AbilityBlueprint bolt = new AbilityBlueprint
            {
                Id = "rg_bolt", Targeting = TargetingMode.Unit, Range = 8f, Cooldown = 1.5f,
                Delivery = AbilityDeliveryType.Projectile, ProjectileArchetypeId = "bolt", ProjectileSpeed = 15f
            };
            bolt.Costs.Add(new AbilityCost(AbilityResourceIds.Mana, 20f));
            bolt.Specials.Add(new AbilitySpecialValue
            {
                Name = "dmg",
                Value = new LeveledValue { Base = 30f, Levels = new[] { 30f, 40f }, LevelFrom = LevelSource.AbilityLevel }
            });
            bolt.ImpactEffects.Add(new EffectNodeData
                { OpId = AbilityEffectOps.Damage, AmountKey = "dmg", DamageType = AbilityDamageTypes.Pure });
            bolt.ImpactEffects.Add(new EffectNodeData { OpId = AbilityEffectOps.ApplyModifier, ModifierId = "rg_burn" });

            AbilityBlueprint nova = new AbilityBlueprint
            {
                Id = "rg_nova", Cooldown = 3f, MaxCharges = 2, ChargeRestoreTime = 2f
            };
            nova.Costs.Add(new AbilityCost(AbilityResourceIds.Mana, 10f));
            nova.ImpactEffects.Add(new EffectNodeData
            {
                OpId = AbilityEffectOps.Damage, Target = EffectTargetSelector.AreaAroundCaster, Radius = 6f,
                MaxTargets = 2, Amount = 12f, Chance = 0.5f, DamageType = AbilityDamageTypes.Physical
            });
            nova.ImpactEffects.Add(new EffectNodeData
            {
                OpId = AbilityEffectOps.ApplyModifier, Target = EffectTargetSelector.AreaAroundCaster, Radius = 6f,
                ModifierId = "rg_slow"
            });

            AbilityBlueprint field = new AbilityBlueprint { Id = "rg_field", Cooldown = 2f };
            field.ImpactEffects.Add(new EffectNodeData
            {
                OpId = AbilityEffectOps.Zone, Target = EffectTargetSelector.Caster, Radius = 3f, Duration = 2f,
                Interval = 0.5f, AbilityId = "rg_tick"
            });
            AbilityBlueprint tick = new AbilityBlueprint { Id = "rg_tick" };
            tick.ImpactEffects.Add(new EffectNodeData
                { OpId = AbilityEffectOps.Damage, Amount = 2f, DamageType = AbilityDamageTypes.Pure });

            ModifierBlueprint burn = new ModifierBlueprint
            {
                Id = "rg_burn", Duration = 3f, TickInterval = 1f, StackPolicy = ModifierStackPolicy.Stack, MaxStacks = 3
            };
            burn.TickEffects.Add(new EffectNodeData
                { OpId = AbilityEffectOps.Damage, Amount = 5f, AmountPerStack = 1f, DamageType = AbilityDamageTypes.Pure });
            ModifierBlueprint slow = new ModifierBlueprint { Id = "rg_slow", Duration = 2f };
            slow.Properties.Add(new PropertyContribution(AbilityProperties.MoveSpeed, PropertyOp.Mul, 0.7f));

            Content content = new Content();
            content.Abilities.AddRange(new[] { bolt, nova, field, tick });
            content.Modifiers.AddRange(new[] { burn, slow });
            if (emptyLeveledFields)
            {
                for (int i = 0; i < content.Abilities.Count; i++)
                {
                    EmptyFields(content.Abilities[i]);
                }

                for (int i = 0; i < content.Modifiers.Count; i++)
                {
                    EmptyFields(content.Modifiers[i]);
                }
            }

            return content;
        }

        private static void EmptyFields(AbilityBlueprint ability)
        {
            ability.RangeByLevel = new float[0];
            ability.CooldownByLevel = new float[0];
            ability.MaxChargesByLevel = new int[0];
            ability.ChargeRestoreTimeByLevel = new float[0];
            ability.ProjectileSpeedByLevel = new float[0];
            for (int i = 0; i < ability.Costs.Count; i++)
            {
                AbilityCost cost = ability.Costs[i];
                cost.AmountByLevel = new float[0];
                cost.AmountKey = string.Empty;
                ability.Costs[i] = cost;
            }

            for (int i = 0; i < ability.Specials.Count; i++)
            {
                AbilitySpecialValue special = ability.Specials[i];
                special.Value.SpentResource = string.Empty;
                ability.Specials[i] = special;
            }

            EmptyFields(ability.CastEffects);
            EmptyFields(ability.ImpactEffects);
        }

        private static void EmptyFields(ModifierBlueprint modifier)
        {
            for (int i = 0; i < modifier.Properties.Count; i++)
            {
                PropertyContribution contribution = modifier.Properties[i];
                contribution.ValueByLevel = new float[0];
                modifier.Properties[i] = contribution;
            }

            EmptyFields(modifier.TickEffects);
        }

        private static void EmptyFields(List<EffectNodeData> nodes)
        {
            for (int i = 0; i < nodes.Count; i++)
            {
                nodes[i].ChanceByLevel = new float[0];
                nodes[i].MaxTargetsByLevel = new int[0];
                nodes[i].DurationByLevel = new float[0];
                nodes[i].IntervalByLevel = new float[0];
                nodes[i].AmountSpentResource = string.Empty;
            }
        }

        private sealed class Content
        {
            public readonly List<AbilityBlueprint> Abilities = new List<AbilityBlueprint>();
            public readonly List<ModifierBlueprint> Modifiers = new List<ModifierBlueprint>();
        }
    }
}
