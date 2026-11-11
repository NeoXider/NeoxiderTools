using System;
using System.Collections.Generic;
using System.Globalization;
using Neo.Abilities;
using Neo.Abilities.Fixed;
using NUnit.Framework;

namespace Neo.Editor.Tests.Abilities.Fixed
{
    /// <summary>
    ///     Converter parity of the leveled and scalable parameters: each scenario is authored once as float content,
    ///     played on the float core (with <see cref="DeterministicAbilityProfile" />) and on the fixed core after
    ///     <see cref="FixedContentConverter" />, and the observations must agree within a raw unit. Plus field-level
    ///     checks that the converter maps every new field.
    /// </summary>
    public sealed class FixedLeveledConformanceTests
    {
        private const double Raw = 1d / 256d;

        private static readonly Dictionary<string, Action<IAbilityScenarioDriver, FixedConformanceTests.Observations>> All =
            new Dictionary<string, Action<IAbilityScenarioDriver, FixedConformanceTests.Observations>>
            {
                { "cooldown_by_level", CooldownByLevel },
                { "charges_by_level", ChargesByLevel },
                { "cost_by_level_and_quarter_of_max_mana", CostByLevelAndQuarterOfMaxMana },
                { "spend_all_mana_projectile", SpendAllManaProjectile },
                { "dot_scaled_by_spent_mana", DotScaledBySpentMana },
                { "slow_and_stun_by_level", SlowAndStunByLevel },
                { "multiply_scale_and_missing_health", MultiplyScaleAndMissingHealth },
                { "chance_and_targets_by_level", ChanceAndTargetsByLevel }
            };

        public static IEnumerable<string> Scenarios => All.Keys;

        [TestCaseSource(nameof(Scenarios))]
        public void LeveledScenario_PlaysTheSameOnBothCores(string scenario)
        {
            FloatScenarioDriver floatDriver = new FloatScenarioDriver();
            FixedScenarioDriver fixedDriver = new FixedScenarioDriver();
            FixedConformanceTests.Observations expected = new FixedConformanceTests.Observations();
            FixedConformanceTests.Observations actual = new FixedConformanceTests.Observations();
            All[scenario](floatDriver, expected);
            All[scenario](fixedDriver, actual);

            Assert.That(actual.Items.Count, Is.EqualTo(expected.Items.Count));
            List<string> failures = new List<string>();
            for (int i = 0; i < expected.Items.Count; i++)
            {
                FixedConformanceTests.Observation e = expected.Items[i];
                FixedConformanceTests.Observation a = actual.Items[i];
                Assert.That(a.Name, Is.EqualTo(e.Name));
                if (Math.Abs(a.Value - e.Value) > e.Tolerance + 1e-9)
                {
                    failures.Add(e.Name + ": float " + e.Value.ToString("0.######", CultureInfo.InvariantCulture) +
                                 " vs fixed " + a.Value.ToString("0.######", CultureInfo.InvariantCulture));
                }
            }

            for (int i = 0; i < fixedDriver.Issues.Count; i++)
            {
                if (fixedDriver.Issues[i].Severity != FixedConversionSeverity.Info)
                {
                    failures.Add("conversion: " + fixedDriver.Issues[i]);
                }
            }

            Assert.That(failures, Is.Empty, string.Join("\n", failures));
        }

        [Test]
        public void Converter_MapsEveryLeveledField()
        {
            AbilityBlueprint ability = new AbilityBlueprint
            {
                Id = "cv_all",
                RangeByLevel = new[] { 2f, 3.5f },
                CooldownByLevel = new[] { 10f, 8f, 6f },
                MaxChargesByLevel = new[] { 1, 2 },
                ChargeRestoreTimeByLevel = new[] { 1f, 0f },
                ProjectileSpeedByLevel = new[] { 12f, 20f }
            };
            ability.Costs.Add(new AbilityCost(AbilityResourceIds.Mana, 5f, AbilityCostMode.UpTo, 2f)
                { AmountByLevel = new[] { 5f, 7.5f }, AmountKey = "price" });
            ability.Specials.Add(new AbilitySpecialValue
            {
                Name = "price",
                Value = new LeveledValue
                {
                    Base = 1f, ScaleProperty = AbilityResourceIds.Mana, ScaleStat = ScaleAmountStat.ResourceMax,
                    ScaleMode = ScaleAmountMode.Multiply, ScalePerPoint = 0.5f, SpentResource = "rage", SpentCoefficient = 1.5f
                }
            });
            ability.ImpactEffects.Add(new EffectNodeData
            {
                OpId = AbilityEffectOps.Zone, Radius = 2f, AbilityId = "cv_all", MaxTargetsByLevel = new[] { 1, 3 },
                ChanceByLevel = new[] { 0.5f, 1f }, DurationByLevel = new[] { 1f, 2f }, IntervalByLevel = new[] { 0.5f, 0.25f },
                AmountScaleStat = ScaleAmountStat.ResourceMissing, AmountScaleMode = ScaleAmountMode.Multiply,
                AmountSpentResource = AbilityResourceIds.Mana, AmountSpentCoefficient = 2f
            });
            ModifierBlueprint modifier = new ModifierBlueprint { Id = "cv_mod" };
            modifier.Properties.Add(new PropertyContribution(AbilityProperties.MoveSpeed, PropertyOp.Mul, 0.8f)
                { ValueByLevel = new[] { 0.8f, 0.7f } });
            modifier.Properties.Add(new PropertyContribution(AbilityProperties.Armor, PropertyOp.Add, 1f)
                { ValueByLevel = new[] { 1f, 2.5f } });

            List<FixedConversionIssue> issues = new List<FixedConversionIssue>();
            FixedConversionSettings settings = new FixedConversionSettings();
            FixedAbilityBlueprint a = FixedContentConverter.Convert(ability, settings, issues);
            FixedModifierBlueprint m = FixedContentConverter.Convert(modifier, settings, issues);

            Assert.That(a.RangeByLevelRaw, Is.EqualTo(new[] { 512, 896 }));
            Assert.That(a.CooldownByLevelTicks, Is.EqualTo(new[] { 300, 240, 180 }));
            Assert.That(a.MaxChargesByLevel, Is.EqualTo(new[] { 1, 2 }));
            Assert.That(a.MaxChargesByLevel, Is.Not.SameAs(ability.MaxChargesByLevel), "copied, not shared");
            Assert.That(a.ChargeRestoreByLevelTicks, Is.EqualTo(new[] { 30, 0 }));
            Assert.That(a.ProjectileSpeedByLevelRaw, Is.EqualTo(new[] { 3072, 5120 }));

            FixedAbilityCost cost = a.Costs[0];
            Assert.That(cost.Mode, Is.EqualTo(FixedCostMode.UpTo));
            Assert.That(cost.MinAmountRaw, Is.EqualTo(512));
            Assert.That(cost.AmountByLevelRaw, Is.EqualTo(new[] { 1280, 1920 }));
            Assert.That(cost.AmountKey, Is.EqualTo("price"));

            FixedLeveledValue special = a.Specials[0].Value;
            Assert.That(special.ScaleStat, Is.EqualTo(FixedScaleStat.ResourceMax));
            Assert.That(special.ScaleMode, Is.EqualTo(FixedScaleMode.Multiply));
            Assert.That(special.SpentResource, Is.EqualTo("rage"));
            Assert.That(special.SpentCoefficientRaw, Is.EqualTo(384));

            FixedEffectNode node = a.ImpactEffects[0];
            Assert.That(node.MaxTargetsByLevel, Is.EqualTo(new[] { 1, 3 }));
            Assert.That(node.ChanceByLevelRaw, Is.EqualTo(new[] { 128, FixedEffectNode.Always }));
            Assert.That(node.DurationByLevelTicks, Is.EqualTo(new[] { 30, 60 }));
            Assert.That(node.IntervalByLevelTicks, Is.EqualTo(new[] { 15, 8 }));
            Assert.That(node.AmountScaleStat, Is.EqualTo(FixedScaleStat.ResourceMissing));
            Assert.That(node.AmountScaleMode, Is.EqualTo(FixedScaleMode.Multiply));
            Assert.That(node.AmountSpentResource, Is.EqualTo(AbilityResourceIds.Mana));
            Assert.That(node.AmountSpentCoefficientRaw, Is.EqualTo(512));

            Assert.That(m.Properties[0].Denominator, Is.EqualTo(100), "whole-percent multipliers stay exact");
            Assert.That(m.Properties[0].ValueByLevelRaw, Is.EqualTo(new[] { 80, 70 }));
            Assert.That(m.Properties[1].Denominator, Is.EqualTo(0));
            Assert.That(m.Properties[1].ValueByLevelRaw, Is.EqualTo(new[] { 256, 640 }));
        }

        private static void CooldownByLevel(IAbilityScenarioDriver d, FixedConformanceTests.Observations o)
        {
            int hero = d.Unit(1, 500d);
            int dummy = d.Unit(2, 500d, x: 1d);
            AbilityBlueprint strike = Strike("lc_cd", 5f);
            strike.CooldownByLevel = new[] { 10f, 8f, 6f };
            d.Register(new[] { strike }, Array.Empty<ModifierBlueprint>());
            d.Grant(hero, "lc_cd");
            for (int level = 1; level <= 3; level++)
            {
                d.SetLevel(hero, "lc_cd", level);
                o.Exact("cast at level " + level, d.Cast(hero, "lc_cd", dummy) ? 1 : 0);
                o.Value("cooldown at level " + level, d.CooldownSeconds(hero, "lc_cd"), 1e-3);
                d.Step(30);
                d.SetLevel(hero, "lc_cd", 3);
                o.Value("kept after a level change " + level, d.CooldownSeconds(hero, "lc_cd"), 1e-3);
                d.Step(300);
            }
        }

        private static void ChargesByLevel(IAbilityScenarioDriver d, FixedConformanceTests.Observations o)
        {
            int hero = d.Unit(1, 500d);
            int dummy = d.Unit(2, 500d, x: 1d);
            AbilityBlueprint strike = Strike("lc_ch", 5f);
            strike.Cooldown = 3f;
            strike.ChargeRestoreTimeByLevel = new[] { 2f, 1f };
            strike.MaxChargesByLevel = new[] { 1, 3 };
            d.Register(new[] { strike }, Array.Empty<ModifierBlueprint>());
            d.Grant(hero, "lc_ch");
            o.Exact("charges at level 1", d.SlotCharges(hero, "lc_ch"));
            d.SetLevel(hero, "lc_ch", 2);
            o.Exact("charges at level 2", d.SlotCharges(hero, "lc_ch"));
            for (int i = 0; i < 4; i++)
            {
                o.Exact("cast " + i, d.Cast(hero, "lc_ch", dummy) ? 1 : 0);
            }

            o.Value("restore", d.CooldownSeconds(hero, "lc_ch"), 1e-3);
            d.Step(45);
            o.Exact("charges after 1.5 s", d.SlotCharges(hero, "lc_ch"));
            d.SetLevel(hero, "lc_ch", 1);
            o.Exact("charges after the cap drops", d.SlotCharges(hero, "lc_ch"));
            o.Value("timer after the cap drops", d.CooldownSeconds(hero, "lc_ch"), 1e-3);
            o.Value("dummy", d.Health(dummy), Raw);
        }

        private static void CostByLevelAndQuarterOfMaxMana(IAbilityScenarioDriver d, FixedConformanceTests.Observations o)
        {
            int hero = d.Unit(1, 500d, mana: 200d);
            int dummy = d.Unit(2, 500d, x: 1d);
            AbilityBlueprint leveled = Strike("lc_cost_lv", 1f);
            leveled.Costs.Add(new AbilityCost(AbilityResourceIds.Mana, 10f) { AmountByLevel = new[] { 10f, 30f } });
            AbilityBlueprint keyed = Strike("lc_cost_key", 1f);
            keyed.Costs.Add(new AbilityCost(AbilityResourceIds.Mana, 0f) { AmountKey = "cost" });
            keyed.Specials.Add(new AbilitySpecialValue
            {
                Name = "cost",
                Value = new LeveledValue
                    { ScaleProperty = AbilityResourceIds.Mana, ScaleStat = ScaleAmountStat.ResourceMax, ScalePerPoint = 0.25f }
            });
            d.Register(new[] { leveled, keyed }, Array.Empty<ModifierBlueprint>());
            d.Grant(hero, "lc_cost_lv");
            d.Grant(hero, "lc_cost_key");
            d.SetLevel(hero, "lc_cost_lv", 2);
            o.Exact("leveled cost cast", d.Cast(hero, "lc_cost_lv", dummy) ? 1 : 0);
            o.Value("mana after the level-2 cost", d.Mana(hero), Raw);
            o.Exact("keyed cost cast", d.Cast(hero, "lc_cost_key", dummy) ? 1 : 0);
            o.Value("mana after a quarter of max mana", d.Mana(hero), Raw);
        }

        private static void SpendAllManaProjectile(IAbilityScenarioDriver d, FixedConformanceTests.Observations o)
        {
            int hero = d.Unit(1, 500d, mana: 90d);
            int dummy = d.Unit(2, 800d, x: 2d);
            AbilityBlueprint nuke = new AbilityBlueprint
            {
                Id = "lc_nuke", Targeting = TargetingMode.Unit, Delivery = AbilityDeliveryType.Projectile,
                ProjectileArchetypeId = "orb"
            };
            nuke.Costs.Add(new AbilityCost(AbilityResourceIds.Mana, 0f, AbilityCostMode.All, 10f));
            nuke.ImpactEffects.Add(new EffectNodeData
            {
                OpId = AbilityEffectOps.Damage, DamageType = AbilityDamageTypes.Pure,
                AmountSpentResource = AbilityResourceIds.Mana, AmountSpentCoefficient = 2f
            });
            d.Register(new[] { nuke }, Array.Empty<ModifierBlueprint>());
            d.Grant(hero, "lc_nuke");
            o.Exact("cast", d.Cast(hero, "lc_nuke", dummy) ? 1 : 0);
            o.Value("mana", d.Mana(hero), Raw);
            d.Step(10);
            o.Value("before the hit", d.Health(dummy), Raw);
            o.Exact("hit", d.HitLastProjectile(dummy) ? 1 : 0);
            o.Value("after the hit", d.Health(dummy), Raw);
            o.Exact("second cast needs 10 mana", d.Cast(hero, "lc_nuke", dummy) ? 1 : 0);
        }

        private static void DotScaledBySpentMana(IAbilityScenarioDriver d, FixedConformanceTests.Observations o)
        {
            int hero = d.Unit(1, 500d, mana: 100d);
            int dummy = d.Unit(2, 800d, x: 1d);
            ModifierBlueprint burn = new ModifierBlueprint { Id = "lc_burn", Duration = 3.5f, TickInterval = 1f };
            burn.TickEffects.Add(new EffectNodeData
            {
                OpId = AbilityEffectOps.Damage, DamageType = AbilityDamageTypes.Pure, Amount = 2f,
                AmountSpentResource = AbilityResourceIds.Mana, AmountSpentCoefficient = 0.25f
            });
            AbilityBlueprint ignite = Strike("lc_ignite", 0f);
            ignite.ImpactEffects.Clear();
            ignite.ImpactEffects.Add(new EffectNodeData { OpId = AbilityEffectOps.ApplyModifier, ModifierId = "lc_burn" });
            ignite.Costs.Add(new AbilityCost(AbilityResourceIds.Mana, 40f));
            d.Register(new[] { ignite }, new[] { burn });
            d.Grant(hero, "lc_ignite");
            o.Exact("cast", d.Cast(hero, "lc_ignite", dummy) ? 1 : 0);
            // WHY: observed one step after each tick, away from the float core's known one-step timer boundary.
            d.Step(1);
            for (int second = 1; second <= 3; second++)
            {
                d.Step(30);
                o.Value("after " + second + " s", d.Health(dummy), Raw);
            }

            d.Step(30);
            o.Exact("burn ended", d.Count(dummy, "lc_burn"));
        }

        private static void SlowAndStunByLevel(IAbilityScenarioDriver d, FixedConformanceTests.Observations o)
        {
            int hero = d.Unit(1, 500d);
            int dummy = d.Unit(2, 500d, x: 1d);
            d.Base(dummy, AbilityProperties.MoveSpeed, 10d);
            ModifierBlueprint slow = new ModifierBlueprint { Id = "lc_slow", Duration = 5f };
            slow.Properties.Add(new PropertyContribution(AbilityProperties.MoveSpeed, PropertyOp.Mul, 0.8f)
                { ValueByLevel = new[] { 0.8f, 0.7f, 0.6f } });
            ModifierBlueprint stun = new ModifierBlueprint { Id = "lc_stun", Duration = 1f, DurationByLevel = new[] { 1f, 1.5f, 2f } };
            stun.States.Add(AbilityStates.Stunned);
            AbilityBlueprint frost = Strike("lc_frost", 0f);
            frost.ImpactEffects.Clear();
            frost.ImpactEffects.Add(new EffectNodeData { OpId = AbilityEffectOps.ApplyModifier, ModifierId = "lc_slow" });
            frost.ImpactEffects.Add(new EffectNodeData { OpId = AbilityEffectOps.ApplyModifier, ModifierId = "lc_stun" });
            d.Register(new[] { frost }, new[] { slow, stun });
            d.Grant(hero, "lc_frost");
            for (int level = 1; level <= 3; level++)
            {
                d.SetLevel(hero, "lc_frost", level);
                o.Exact("cast " + level, d.Cast(hero, "lc_frost", dummy) ? 1 : 0);
                o.Value("move speed " + level, d.Property(dummy, AbilityProperties.MoveSpeed), Raw);
                o.Exact("stun ticks " + level, d.RemainingTicks(dummy, "lc_stun"));
                d.Step(90);
            }
        }

        private static void MultiplyScaleAndMissingHealth(IAbilityScenarioDriver d, FixedConformanceTests.Observations o)
        {
            int hero = d.Unit(1, 500d, mana: 100d);
            int dummy = d.Unit(2, 800d, x: 1d);
            d.Base(hero, AbilityProperties.SpellPower, 2d);
            AbilityBlueprint surge = Strike("lc_surge", 0f);
            surge.ImpactEffects.Clear();
            surge.ImpactEffects.Add(new EffectNodeData
            {
                OpId = AbilityEffectOps.Damage, DamageType = AbilityDamageTypes.Pure, Amount = 10f,
                AmountScaleProperty = AbilityProperties.SpellPower, AmountScalePerPoint = 0.25f,
                AmountScaleMode = ScaleAmountMode.Multiply, AmountSpentResource = AbilityResourceIds.Mana,
                AmountSpentCoefficient = 1f
            });
            surge.Costs.Add(new AbilityCost(AbilityResourceIds.Mana, 30f));
            AbilityBlueprint reap = Strike("lc_reap", 0f);
            reap.ImpactEffects.Clear();
            reap.ImpactEffects.Add(new EffectNodeData
            {
                OpId = AbilityEffectOps.Damage, DamageType = AbilityDamageTypes.Pure,
                AmountScaleProperty = AbilityResourceIds.Health, AmountScaleStat = ScaleAmountStat.ResourceMissing,
                AmountScaleSource = ScaleAmountSource.Target, AmountScalePerPoint = 0.5f
            });
            d.Register(new[] { surge, reap }, Array.Empty<ModifierBlueprint>());
            d.Grant(hero, "lc_surge");
            d.Grant(hero, "lc_reap");
            o.Exact("surge", d.Cast(hero, "lc_surge", dummy) ? 1 : 0);
            o.Value("after the multiplied surge", d.Health(dummy), Raw);
            o.Exact("reap", d.Cast(hero, "lc_reap", dummy) ? 1 : 0);
            o.Value("after half the missing health", d.Health(dummy), Raw);
        }

        private static void ChanceAndTargetsByLevel(IAbilityScenarioDriver d, FixedConformanceTests.Observations o)
        {
            int hero = d.Unit(1, 500d);
            int[] pack = { d.Unit(2, 300d, x: 1d), d.Unit(2, 300d, x: 2d), d.Unit(2, 300d, x: 3d) };
            AbilityBlueprint nova = new AbilityBlueprint { Id = "lc_nova" };
            nova.ImpactEffects.Add(new EffectNodeData
            {
                OpId = AbilityEffectOps.Damage, DamageType = AbilityDamageTypes.Pure, Amount = 10f,
                Target = EffectTargetSelector.AreaAroundCaster, Radius = 5f, MaxTargets = 1,
                MaxTargetsByLevel = new[] { 1, 2 }, ChanceByLevel = new[] { 0f, 1f }
            });
            d.Register(new[] { nova }, Array.Empty<ModifierBlueprint>());
            d.Grant(hero, "lc_nova");
            for (int level = 1; level <= 2; level++)
            {
                d.SetLevel(hero, "lc_nova", level);
                o.Exact("cast " + level, d.Cast(hero, "lc_nova") ? 1 : 0);
                for (int i = 0; i < pack.Length; i++)
                {
                    o.Value("level " + level + " unit " + i, d.Health(pack[i]), Raw);
                }
            }
        }

        private static AbilityBlueprint Strike(string id, float amount)
        {
            AbilityBlueprint strike = new AbilityBlueprint { Id = id, Targeting = TargetingMode.Unit };
            strike.ImpactEffects.Add(new EffectNodeData
                { OpId = AbilityEffectOps.Damage, Amount = amount, DamageType = AbilityDamageTypes.Pure });
            return strike;
        }
    }
}
