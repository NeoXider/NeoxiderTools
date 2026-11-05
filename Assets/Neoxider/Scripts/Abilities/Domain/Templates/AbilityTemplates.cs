using System;
using System.Collections.Generic;
using System.Globalization;

namespace Neo.Abilities
{
    /// <summary>
    ///     A ready set of blueprints produced by an <see cref="AbilityTemplates" /> factory: the abilities and modifiers
    ///     it needs, which abilities a unit should be granted and which modifiers it should carry from the start.
    /// </summary>
    public sealed class AbilityTemplateBundle
    {
        public string TemplateId;
        public string DisplayName;
        public string Description;
        public readonly List<AbilityBlueprint> Abilities = new List<AbilityBlueprint>();
        public readonly List<ModifierBlueprint> Modifiers = new List<ModifierBlueprint>();

        /// <summary>Abilities a unit using the template is granted (the others are payloads).</summary>
        public readonly List<string> GrantAbilityIds = new List<string>();

        /// <summary>Permanent modifiers a unit using the template carries from the start (innate passives).</summary>
        public readonly List<string> InnateModifierIds = new List<string>();

        /// <summary>Registers every blueprint of the bundle into the system catalogs.</summary>
        public void RegisterInto(AbilitySystem system)
        {
            if (system == null)
            {
                return;
            }

            for (int i = 0; i < Modifiers.Count; i++)
            {
                system.RegisterModifier(Modifiers[i]);
            }

            for (int i = 0; i < Abilities.Count; i++)
            {
                system.RegisterAbility(Abilities[i]);
            }
        }

        /// <summary>Registers the bundle, grants its abilities to <paramref name="unit" /> and applies its innate modifiers.</summary>
        public void SetUp(AbilitySystem system, UnitId unit)
        {
            RegisterInto(system);
            if (system == null || !unit.IsValid)
            {
                return;
            }

            for (int i = 0; i < GrantAbilityIds.Count; i++)
            {
                system.GrantAbility(unit, GrantAbilityIds[i]);
            }

            for (int i = 0; i < InnateModifierIds.Count; i++)
            {
                system.ApplyModifier(InnateModifierIds[i], unit, unit);
            }
        }

        public AbilityBlueprint FindAbility(string id)
        {
            for (int i = 0; i < Abilities.Count; i++)
            {
                if (AbilityIdComparer.Same(Abilities[i].Id, id))
                {
                    return Abilities[i];
                }
            }

            return null;
        }

        public ModifierBlueprint FindModifier(string id)
        {
            for (int i = 0; i < Modifiers.Count; i++)
            {
                if (AbilityIdComparer.Same(Modifiers[i].Id, id))
                {
                    return Modifiers[i];
                }
            }

            return null;
        }
    }

    /// <summary>One entry of <see cref="AbilityTemplates.Catalog" /> (the Ability Designer's "+ Template" menu).</summary>
    public sealed class AbilityTemplateInfo
    {
        public AbilityTemplateInfo(string id, string displayName, string description, Func<AbilityTemplateBundle> create)
        {
            Id = id;
            DisplayName = displayName;
            Description = description;
            Create = create;
        }

        public string Id { get; }
        public string DisplayName { get; }
        public string Description { get; }

        /// <summary>Builds a fresh bundle with the template's default ids and numbers.</summary>
        public Func<AbilityTemplateBundle> Create { get; }
    }

    /// <summary>
    ///     Factories of ready-made content built only from the public data model (no custom code): an auto-cast
    ///     technique, an attack enhancer, an auto-defense shield, a mark consumed by the next N hits, a stacking
    ///     poison, thorns (reflect), an aura and a formation zone. Each returns new blueprints you can tune, register
    ///     or turn into assets (the Ability Designer does that).
    /// </summary>
    public static class AbilityTemplates
    {
        public const string AutoCastTechniqueId = "auto_cast_technique";
        public const string AttackEnhancerId = "attack_enhancer";
        public const string AutoDefenseShieldId = "auto_defense_shield";
        public const string MarkId = "mark";
        public const string StackingPoisonId = "stacking_poison";
        public const string ThornsId = "thorns";
        public const string AuraId = "aura";
        public const string FormationZoneId = "formation_zone";

        private static readonly AbilityTemplateInfo[] CatalogEntries =
        {
            new AbilityTemplateInfo(AutoCastTechniqueId, "Auto-cast technique",
                "A passive that casts a damaging technique at the nearest enemy every few seconds.",
                () => AutoCastTechnique()),
            new AbilityTemplateInfo(AttackEnhancerId, "Attack enhancer",
                "A self buff: the next N hits deal more damage, then it ends.", () => AttackEnhancer()),
            new AbilityTemplateInfo(AutoDefenseShieldId, "Auto-defense shield",
                "A passive: when health drops below a threshold, gain a shield (with an internal cooldown).",
                () => AutoDefenseShield()),
            new AbilityTemplateInfo(MarkId, "Mark (next N hits)",
                "Marks an enemy: the next N hits on it deal bonus damage, then the mark is consumed.", () => Mark()),
            new AbilityTemplateInfo(StackingPoisonId, "Stacking poison",
                "A dart and an on-hit coating that stack a poison; its tick damage grows per stack.",
                () => StackingPoison()),
            new AbilityTemplateInfo(ThornsId, "Thorns (reflect)",
                "A passive that reflects a share of the damage of every hit back to the attacker.", () => Thorns()),
            new AbilityTemplateInfo(AuraId, "Aura",
                "A zone following the caster that keeps a short buff refreshed on allies inside.", () => Aura()),
            new AbilityTemplateInfo(FormationZoneId, "Formation zone",
                "A zone placed at a point that pulses damage and a slow on enemies inside for a while.",
                () => FormationZone())
        };

        /// <summary>Every template, in menu order.</summary>
        public static IReadOnlyList<AbilityTemplateInfo> Catalog => CatalogEntries;

        /// <summary>Looks a template up by id (case-insensitive).</summary>
        public static bool TryGet(string templateId, out AbilityTemplateInfo info)
        {
            for (int i = 0; i < CatalogEntries.Length; i++)
            {
                if (AbilityIdComparer.Same(CatalogEntries[i].Id, templateId))
                {
                    info = CatalogEntries[i];
                    return true;
                }
            }

            info = null;
            return false;
        }

        /// <summary>
        ///     Passive <c>{id}_autocast</c> ticking every <paramref name="interval" /> seconds that casts technique
        ///     <paramref name="id" /> (no cooldown of its own: the passive is the clock) at the nearest enemy within
        ///     <paramref name="range" />.
        /// </summary>
        public static AbilityTemplateBundle AutoCastTechnique(string id = "spirit_bolt", float interval = 2f,
            float damage = 20f, float range = 8f)
        {
            AbilityTemplateBundle bundle = Bundle(AutoCastTechniqueId, "Auto-cast technique",
                "Every " + F(interval) + " s the passive casts '" + id + "' at the nearest enemy in range.");
            AbilityBlueprint technique = new AbilityBlueprint
            {
                Id = id,
                DisplayName = Title(id),
                Description = "Deals " + F(damage) + " magical damage. Cast automatically by its passive.",
                Targeting = TargetingMode.Unit,
                TeamFilter = AbilityTeamFilter.Enemies,
                Range = range
            };
            technique.ImpactEffects.Add(Damage(damage, AbilityDamageTypes.Magical));
            bundle.Abilities.Add(technique);

            ModifierBlueprint passive = Passive(id + "_autocast", "Auto-cast: " + Title(id));
            passive.TickInterval = interval;
            passive.TickEffects.Add(new EffectNodeData
            {
                OpId = AbilityEffectOps.CastAbility,
                AbilityId = id,
                Target = EffectTargetSelector.AreaAroundCaster,
                TeamFilter = AbilityTeamFilter.Enemies,
                Radius = range,
                MaxTargets = 1
            });
            bundle.Modifiers.Add(passive);
            bundle.GrantAbilityIds.Add(id);
            bundle.InnateModifierIds.Add(passive.Id);
            return bundle;
        }

        /// <summary>Self buff <paramref name="id" />: the next <paramref name="hits" /> hits deal ×<paramref name="multiplier" />.</summary>
        public static AbilityTemplateBundle AttackEnhancer(string id = "empower", float multiplier = 1.5f, int hits = 3,
            float duration = 8f, float cooldown = 10f)
        {
            AbilityTemplateBundle bundle = Bundle(AttackEnhancerId, "Attack enhancer",
                "'" + id + "': the next " + hits + " hits deal x" + F(multiplier) + " damage within " + F(duration) +
                " s.");
            ModifierBlueprint buff = new ModifierBlueprint
            {
                Id = id,
                DisplayName = Title(id),
                Duration = duration,
                StackPolicy = ModifierStackPolicy.Refresh,
                Dispellable = true
            };
            ModifierEventReaction enhance = new ModifierEventReaction
            {
                EventId = AbilityEvents.BeforeDealDamage,
                HitsOnly = true,
                MaxTriggers = hits,
                RemoveWhenSpent = true
            };
            enhance.Effects.Add(new EffectNodeData
            {
                OpId = AbilityEffectOps.ModifyDamage,
                CustomParam = ModifyDamageEffectOperation.ModeMultiply,
                Amount = multiplier
            });
            buff.EventReactions.Add(enhance);
            bundle.Modifiers.Add(buff);

            AbilityBlueprint ability = new AbilityBlueprint
            {
                Id = id + "_cast",
                DisplayName = Title(id),
                Description = "Empowers your next " + hits + " hits.",
                Targeting = TargetingMode.Self,
                Cooldown = cooldown
            };
            ability.CastEffects.Add(ApplyModifier(id, EffectTargetSelector.Caster));
            bundle.Abilities.Add(ability);
            bundle.GrantAbilityIds.Add(ability.Id);
            return bundle;
        }

        /// <summary>
        ///     Passive <c>{id}_passive</c>: a hit that drops health below <paramref name="threshold" /> grants shield
        ///     modifier <paramref name="id" /> (<paramref name="shield" /> HP for <paramref name="duration" /> s), at most
        ///     once per <paramref name="internalCooldown" /> s.
        /// </summary>
        public static AbilityTemplateBundle AutoDefenseShield(string id = "guardian_barrier", float threshold = 0.4f,
            float shield = 120f, float duration = 5f, float internalCooldown = 12f)
        {
            AbilityTemplateBundle bundle = Bundle(AutoDefenseShieldId, "Auto-defense shield",
                "Below " + F(threshold * 100f) + "% health the passive grants a " + F(shield) + " HP shield (cooldown " +
                F(internalCooldown) + " s).");
            ModifierBlueprint barrier = new ModifierBlueprint
            {
                Id = id,
                DisplayName = Title(id),
                Duration = duration,
                StackPolicy = ModifierStackPolicy.Refresh
            };
            barrier.Properties.Add(new PropertyContribution(AbilityProperties.ShieldHp, PropertyOp.Add, shield));
            bundle.Modifiers.Add(barrier);

            ModifierBlueprint passive = Passive(id + "_passive", "Auto-defense: " + Title(id));
            ModifierEventReaction trigger = new ModifierEventReaction
            {
                EventId = AbilityEvents.TakeDamage,
                Trigger = ModifierTrigger.HealthBelow,
                Threshold = threshold,
                Cooldown = internalCooldown
            };
            trigger.Effects.Add(ApplyModifier(id, EffectTargetSelector.Target));
            passive.EventReactions.Add(trigger);
            bundle.Modifiers.Add(passive);
            bundle.InnateModifierIds.Add(passive.Id);
            return bundle;
        }

        /// <summary>
        ///     Ability <paramref name="id" /> marks an enemy with modifier <c>{id}_mark</c>: the next
        ///     <paramref name="hits" /> hits on it deal +<paramref name="bonus" /> damage, then the mark is consumed.
        /// </summary>
        public static AbilityTemplateBundle Mark(string id = "hunter_mark", float bonus = 25f, int hits = 3,
            float duration = 10f, float range = 10f, float cooldown = 6f)
        {
            AbilityTemplateBundle bundle = Bundle(MarkId, "Mark (next N hits)",
                "'" + id + "' marks an enemy: the next " + hits + " hits deal +" + F(bonus) + " damage.");
            ModifierBlueprint mark = new ModifierBlueprint
            {
                Id = id + "_mark",
                DisplayName = Title(id),
                Duration = duration,
                StackPolicy = ModifierStackPolicy.Refresh,
                IsDebuff = true
            };
            ModifierEventReaction bonusHit = new ModifierEventReaction
            {
                EventId = AbilityEvents.BeforeTakeDamage,
                HitsOnly = true,
                MaxTriggers = hits,
                RemoveWhenSpent = true
            };
            bonusHit.Effects.Add(new EffectNodeData
            {
                OpId = AbilityEffectOps.ModifyDamage,
                CustomParam = ModifyDamageEffectOperation.ModeAdd,
                Amount = bonus
            });
            mark.EventReactions.Add(bonusHit);
            bundle.Modifiers.Add(mark);

            AbilityBlueprint ability = new AbilityBlueprint
            {
                Id = id,
                DisplayName = Title(id),
                Description = "Marks an enemy for " + hits + " bonus hits.",
                Targeting = TargetingMode.Unit,
                TeamFilter = AbilityTeamFilter.Enemies,
                Range = range,
                Cooldown = cooldown
            };
            ability.ImpactEffects.Add(ApplyModifier(mark.Id, EffectTargetSelector.Target));
            bundle.Abilities.Add(ability);
            bundle.GrantAbilityIds.Add(id);
            return bundle;
        }

        /// <summary>
        ///     Poison <paramref name="id" /> (Stack, up to <paramref name="maxStacks" />, <paramref name="tickDamage" />
        ///     per second per stack), the dart <c>{id}_dart</c> and the innate on-hit coating <c>{id}_coating</c>.
        /// </summary>
        public static AbilityTemplateBundle StackingPoison(string id = "venom", float tickDamage = 4f, int maxStacks = 5,
            float duration = 5f, float dartDamage = 5f, float range = 8f)
        {
            AbilityTemplateBundle bundle = Bundle(StackingPoisonId, "Stacking poison",
                "Every hit adds a stack of '" + id + "' (max " + maxStacks + "); each stack deals " + F(tickDamage) +
                " per second.");
            ModifierBlueprint poison = new ModifierBlueprint
            {
                Id = id,
                DisplayName = Title(id),
                Duration = duration,
                StackPolicy = ModifierStackPolicy.Stack,
                MaxStacks = maxStacks,
                TickInterval = 1f,
                IsDebuff = true
            };
            poison.TickEffects.Add(new EffectNodeData
            {
                OpId = AbilityEffectOps.Damage,
                Target = EffectTargetSelector.Target,
                Amount = tickDamage,
                AmountPerStack = tickDamage,
                DamageType = AbilityDamageTypes.Magical
            });
            bundle.Modifiers.Add(poison);

            AbilityBlueprint dart = new AbilityBlueprint
            {
                Id = id + "_dart",
                DisplayName = Title(id) + " Dart",
                Description = "A quick dart that adds a stack of " + Title(id) + ".",
                Targeting = TargetingMode.Unit,
                TeamFilter = AbilityTeamFilter.Enemies,
                Range = range,
                Cooldown = 1f
            };
            dart.ImpactEffects.Add(Damage(dartDamage, AbilityDamageTypes.Physical));
            dart.ImpactEffects.Add(ApplyModifier(id, EffectTargetSelector.Target));
            bundle.Abilities.Add(dart);

            ModifierBlueprint coating = Passive(id + "_coating", Title(id) + " Coating");
            ModifierEventReaction onHit = new ModifierEventReaction
            {
                EventId = AbilityEvents.AttackLanded,
                TargetEventSource = true
            };
            onHit.Effects.Add(ApplyModifier(id, EffectTargetSelector.Target));
            coating.EventReactions.Add(onHit);
            bundle.Modifiers.Add(coating);
            bundle.GrantAbilityIds.Add(dart.Id);
            bundle.InnateModifierIds.Add(coating.Id);
            return bundle;
        }

        /// <summary>Passive <paramref name="id" />: every hit taken reflects <paramref name="fraction" /> of it as pure damage.</summary>
        public static AbilityTemplateBundle Thorns(string id = "thorns", float fraction = 0.3f)
        {
            AbilityTemplateBundle bundle = Bundle(ThornsId, "Thorns (reflect)",
                "Reflects " + F(fraction * 100f) + "% of every hit taken back to the attacker.");
            ModifierBlueprint thorns = Passive(id, Title(id));
            ModifierEventReaction reflect = new ModifierEventReaction
            {
                EventId = AbilityEvents.BeforeTakeDamage,
                HitsOnly = true
            };
            reflect.Effects.Add(new EffectNodeData
            {
                OpId = AbilityEffectOps.Reflect,
                Amount = fraction,
                DamageType = AbilityDamageTypes.Pure
            });
            thorns.EventReactions.Add(reflect);
            bundle.Modifiers.Add(thorns);
            bundle.InnateModifierIds.Add(thorns.Id);
            return bundle;
        }

        /// <summary>
        ///     Ability <paramref name="id" /> spawns a zone following the caster for <paramref name="duration" /> s
        ///     (0 = while the caster lives) that refreshes buff <c>{id}_buff</c> (+armor, ×outgoing damage) on allies inside.
        /// </summary>
        public static AbilityTemplateBundle Aura(string id = "war_banner", float radius = 6f, float armor = 5f,
            float damageMultiplier = 1.15f, float duration = 12f, float cooldown = 20f)
        {
            AbilityTemplateBundle bundle = Bundle(AuraId, "Aura",
                "Allies within " + F(radius) + " m of the caster get +" + F(armor) + " armor and x" + F(damageMultiplier) +
                " damage.");
            ModifierBlueprint buff = new ModifierBlueprint
            {
                Id = id + "_buff",
                DisplayName = Title(id),
                Duration = 1f,
                StackPolicy = ModifierStackPolicy.Refresh
            };
            buff.Properties.Add(new PropertyContribution(AbilityProperties.Armor, PropertyOp.Add, armor));
            buff.Properties.Add(new PropertyContribution(AbilityProperties.OutgoingDamageMul, PropertyOp.Mul,
                damageMultiplier));
            bundle.Modifiers.Add(buff);

            AbilityBlueprint ability = new AbilityBlueprint
            {
                Id = id,
                DisplayName = Title(id),
                Description = "Raises an aura around you.",
                Targeting = TargetingMode.Self,
                Cooldown = cooldown
            };
            ability.CastEffects.Add(new EffectNodeData
            {
                OpId = AbilityEffectOps.Zone,
                Target = EffectTargetSelector.Caster,
                TeamFilter = AbilityTeamFilter.Allies,
                Radius = radius,
                Duration = duration,
                Interval = 0.5f,
                ModifierId = buff.Id,
                ArchetypeId = id + "_zone"
            });
            bundle.Abilities.Add(ability);
            bundle.GrantAbilityIds.Add(id);
            return bundle;
        }

        /// <summary>
        ///     Ability <paramref name="id" /> places a formation at a point: for <paramref name="duration" /> s it pulses
        ///     every <paramref name="interval" /> s, dealing <paramref name="damage" /> and refreshing slow
        ///     <c>{id}_slow</c> on enemies inside (payload <c>{id}_pulse</c>).
        /// </summary>
        public static AbilityTemplateBundle FormationZone(string id = "thunder_formation", float radius = 4f,
            float duration = 6f, float interval = 1f, float damage = 15f, float slow = 0.6f, float range = 12f,
            float cooldown = 12f)
        {
            AbilityTemplateBundle bundle = Bundle(FormationZoneId, "Formation zone",
                "A " + F(radius) + " m formation that hits enemies inside for " + F(damage) + " every " + F(interval) +
                " s and slows them.");
            ModifierBlueprint slowModifier = new ModifierBlueprint
            {
                Id = id + "_slow",
                DisplayName = Title(id) + " Slow",
                Duration = interval * 1.2f,
                StackPolicy = ModifierStackPolicy.Refresh,
                IsDebuff = true
            };
            slowModifier.Properties.Add(new PropertyContribution(AbilityProperties.MoveSpeed, PropertyOp.Mul, slow));
            bundle.Modifiers.Add(slowModifier);

            AbilityBlueprint pulse = new AbilityBlueprint
            {
                Id = id + "_pulse",
                DisplayName = Title(id) + " Pulse",
                Description = "Payload of the formation: runs on every pulse for the enemies inside.",
                Targeting = TargetingMode.NoTarget
            };
            pulse.ImpactEffects.Add(Damage(damage, AbilityDamageTypes.Magical));
            pulse.ImpactEffects.Add(ApplyModifier(slowModifier.Id, EffectTargetSelector.Target));
            bundle.Abilities.Add(pulse);

            AbilityBlueprint ability = new AbilityBlueprint
            {
                Id = id,
                DisplayName = Title(id),
                Description = "Places a damaging formation.",
                Targeting = TargetingMode.Point,
                Range = range,
                Cooldown = cooldown
            };
            ability.CastEffects.Add(new EffectNodeData
            {
                OpId = AbilityEffectOps.Zone,
                Target = EffectTargetSelector.Target,
                TeamFilter = AbilityTeamFilter.Enemies,
                Radius = radius,
                Duration = duration,
                Interval = interval,
                AbilityId = pulse.Id,
                ArchetypeId = id + "_zone"
            });
            bundle.Abilities.Add(ability);
            bundle.GrantAbilityIds.Add(id);
            return bundle;
        }

        private static AbilityTemplateBundle Bundle(string templateId, string displayName, string description)
        {
            return new AbilityTemplateBundle
            {
                TemplateId = templateId,
                DisplayName = displayName,
                Description = description
            };
        }

        private static ModifierBlueprint Passive(string id, string displayName)
        {
            return new ModifierBlueprint
            {
                Id = id,
                DisplayName = displayName,
                Duration = 0f,
                StackPolicy = ModifierStackPolicy.Unique,
                Dispellable = false
            };
        }

        private static EffectNodeData Damage(float amount, string damageType)
        {
            return new EffectNodeData
            {
                OpId = AbilityEffectOps.Damage,
                Target = EffectTargetSelector.Target,
                Amount = amount,
                DamageType = damageType
            };
        }

        private static EffectNodeData ApplyModifier(string modifierId, EffectTargetSelector target)
        {
            return new EffectNodeData
            {
                OpId = AbilityEffectOps.ApplyModifier,
                Target = target,
                ModifierId = modifierId
            };
        }

        private static string F(float value)
        {
            return value.ToString("0.##", CultureInfo.InvariantCulture);
        }

        private static string Title(string id)
        {
            if (string.IsNullOrEmpty(id))
            {
                return id;
            }

            char[] chars = id.Replace('_', ' ').ToCharArray();
            bool upper = true;
            for (int i = 0; i < chars.Length; i++)
            {
                if (upper && char.IsLetter(chars[i]))
                {
                    chars[i] = char.ToUpperInvariant(chars[i]);
                    upper = false;
                }
                else if (chars[i] == ' ')
                {
                    upper = true;
                }
            }

            return new string(chars);
        }
    }
}
