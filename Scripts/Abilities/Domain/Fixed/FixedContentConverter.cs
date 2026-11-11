using System;
using System.Collections.Generic;
using System.Globalization;
using Neo.Abilities.Fixed;

namespace Neo.Abilities
{
    /// <summary>How float content is converted for the fixed core.</summary>
    public sealed class FixedConversionSettings
    {
        /// <summary>Tick rate the seconds of the content are converted to.</summary>
        public int TicksPerSecond = 30;

        /// <summary>Fraction bits of the target format (8 = Q8.8).</summary>
        public int FractionBits = 8;

        /// <summary>
        ///     Multipliers that are not on the grid but are whole percents (1.1, 0.7) become exact ratios
        ///     (ValueRaw / 100) instead of rounded raw values.
        /// </summary>
        public bool ExactPercentMultipliers = true;

        /// <summary>Also report (as Info) every value that is not exactly on the grid, even within half a step.</summary>
        public bool ReportOffGridValues;

        public FixedFormat Format => new FixedFormat(FractionBits);

        /// <summary>Rules of a fixed system matching these settings (format and tick rate).</summary>
        public FixedRules CreateRules()
        {
            return new FixedRules { Format = Format, TicksPerSecond = TicksPerSecond };
        }
    }

    public enum FixedConversionSeverity
    {
        Info = 0,
        Warning = 1,
        Error = 2
    }

    /// <summary>One finding of <see cref="FixedContentConverter" />: which value of which content changed and how.</summary>
    public readonly struct FixedConversionIssue
    {
        public readonly FixedConversionSeverity Severity;
        public readonly string ContentId;
        public readonly string Field;
        public readonly double Original;
        public readonly double Converted;
        public readonly string Message;

        public FixedConversionIssue(FixedConversionSeverity severity, string contentId, string field, double original,
            double converted, string message)
        {
            Severity = severity;
            ContentId = contentId;
            Field = field;
            Original = original;
            Converted = converted;
            Message = message;
        }

        public override string ToString()
        {
            return Severity + " " + ContentId + "." + Field + ": " +
                   Original.ToString("0.######", CultureInfo.InvariantCulture) + " -> " +
                   Converted.ToString("0.######", CultureInfo.InvariantCulture) + " (" + Message + ")";
        }
    }

    /// <summary>Converted content ready for a <see cref="FixedAbilitySystem" />.</summary>
    public sealed class FixedContentBundle
    {
        public readonly List<FixedAbilityBlueprint> Abilities = new List<FixedAbilityBlueprint>();
        public readonly List<FixedModifierBlueprint> Modifiers = new List<FixedModifierBlueprint>();

        public void RegisterInto(FixedAbilitySystem system)
        {
            for (int i = 0; i < Modifiers.Count; i++)
            {
                system.RegisterModifier(Modifiers[i]);
            }

            for (int i = 0; i < Abilities.Count; i++)
            {
                system.RegisterAbility(Abilities[i]);
            }
        }

        public FixedAbilityBlueprint Ability(string id)
        {
            for (int i = 0; i < Abilities.Count; i++)
            {
                if (string.Equals(Abilities[i].Id, id, StringComparison.OrdinalIgnoreCase))
                {
                    return Abilities[i];
                }
            }

            return null;
        }

        public FixedModifierBlueprint Modifier(string id)
        {
            for (int i = 0; i < Modifiers.Count; i++)
            {
                if (string.Equals(Modifiers[i].Id, id, StringComparison.OrdinalIgnoreCase))
                {
                    return Modifiers[i];
                }
            }

            return null;
        }
    }

    /// <summary>
    ///     Converts float content (assets, blueprints, templates) into blueprints of the engine-free fixed core:
    ///     amounts, radii, fractions and multipliers to raw values rounded half away from zero, seconds to whole ticks
    ///     (rounded up, so a duration ends on the same step as in the float core with a fixed step), angles to whole
    ///     degrees. Every value that moves by more than half a raw unit (a duration that is not a whole number of
    ///     ticks, a fractional angle, a saturated or non-finite number) is reported, as is every operation the fixed
    ///     core does not provide.
    /// </summary>
    public static class FixedContentConverter
    {
        // WHY: float seconds such as 0.1f are a hair above the decimal value; without the slack 0.1 s at 30 ticks per
        // second (3.0000000447 ticks) would round up to 4.
        private const double TickSlack = 1e-4;

        public static FixedContentBundle Convert(AbilityLibrary library, FixedConversionSettings settings,
            List<FixedConversionIssue> issues)
        {
            FixedContentBundle bundle = new FixedContentBundle();
            if (library == null)
            {
                return bundle;
            }

            for (int i = 0; i < library.Modifiers.Count; i++)
            {
                if (library.Modifiers[i] != null)
                {
                    bundle.Modifiers.Add(Convert(library.Modifiers[i].Blueprint, settings, issues));
                }
            }

            for (int i = 0; i < library.Abilities.Count; i++)
            {
                if (library.Abilities[i] != null)
                {
                    bundle.Abilities.Add(Convert(library.Abilities[i].Blueprint, settings, issues));
                }
            }

            return bundle;
        }

        public static FixedContentBundle Convert(IEnumerable<AbilityBlueprint> abilities,
            IEnumerable<ModifierBlueprint> modifiers, FixedConversionSettings settings, List<FixedConversionIssue> issues)
        {
            FixedContentBundle bundle = new FixedContentBundle();
            if (modifiers != null)
            {
                foreach (ModifierBlueprint modifier in modifiers)
                {
                    if (modifier != null)
                    {
                        bundle.Modifiers.Add(Convert(modifier, settings, issues));
                    }
                }
            }

            if (abilities != null)
            {
                foreach (AbilityBlueprint ability in abilities)
                {
                    if (ability != null)
                    {
                        bundle.Abilities.Add(Convert(ability, settings, issues));
                    }
                }
            }

            return bundle;
        }

        public static FixedAbilityBlueprint Convert(AbilityDefinition definition, FixedConversionSettings settings,
            List<FixedConversionIssue> issues)
        {
            return definition != null ? Convert(definition.Blueprint, settings, issues) : null;
        }

        public static FixedModifierBlueprint Convert(ModifierDefinition definition, FixedConversionSettings settings,
            List<FixedConversionIssue> issues)
        {
            return definition != null ? Convert(definition.Blueprint, settings, issues) : null;
        }

        public static FixedAbilityBlueprint Convert(AbilityBlueprint source, FixedConversionSettings settings,
            List<FixedConversionIssue> issues)
        {
            if (source == null)
            {
                return null;
            }

            Converter c = new Converter(settings ?? new FixedConversionSettings(), issues, source.Id);
            FixedAbilityBlueprint result = new FixedAbilityBlueprint
            {
                Id = source.Id,
                Targeting = (FixedTargeting)(int)source.Targeting,
                TeamFilter = (FixedTeamFilter)(int)source.TeamFilter,
                RangeRaw = c.Raw(source.Range, "Range"),
                RangeByLevelRaw = c.RawArray(source.RangeByLevel, "RangeByLevel"),
                CooldownTicks = c.Ticks(source.Cooldown, "Cooldown"),
                CooldownByLevelTicks = c.TicksArray(source.CooldownByLevel, "CooldownByLevel"),
                MaxCharges = source.MaxCharges,
                MaxChargesByLevel = CopyArray(source.MaxChargesByLevel),
                ChargeRestoreTicks = c.Ticks(source.ChargeRestoreTime, "ChargeRestoreTime"),
                ChargeRestoreByLevelTicks = c.TicksArray(source.ChargeRestoreTimeByLevel, "ChargeRestoreTimeByLevel"),
                Delivery = (FixedDelivery)(int)source.Delivery,
                ProjectileArchetypeId = source.ProjectileArchetypeId,
                ProjectileSpeedRaw = c.Raw(source.ProjectileSpeed, "ProjectileSpeed"),
                ProjectileSpeedByLevelRaw = c.RawArray(source.ProjectileSpeedByLevel, "ProjectileSpeedByLevel")
            };

            if (source.Costs != null)
            {
                for (int i = 0; i < source.Costs.Count; i++)
                {
                    AbilityCost cost = source.Costs[i];
                    string path = "Costs[" + i + "]";
                    result.Costs.Add(new FixedAbilityCost(cost.ResourceId, c.Raw(cost.Amount, path),
                        (FixedCostMode)(int)cost.Mode, c.Raw(cost.MinAmount, path + ".MinAmount"))
                    {
                        AmountByLevelRaw = c.RawArray(cost.AmountByLevel, path + ".AmountByLevel"),
                        AmountKey = cost.AmountKey
                    });
                }
            }

            c.Nodes(source.CastEffects, result.CastEffects, "CastEffects");
            c.Nodes(source.ImpactEffects, result.ImpactEffects, "ImpactEffects");
            if (source.Specials != null)
            {
                for (int i = 0; i < source.Specials.Count; i++)
                {
                    result.Specials.Add(new FixedAbilitySpecial
                    {
                        Name = source.Specials[i].Name,
                        Value = c.Leveled(source.Specials[i].Value, "Specials[" + source.Specials[i].Name + "]")
                    });
                }
            }

            return result;
        }

        public static FixedModifierBlueprint Convert(ModifierBlueprint source, FixedConversionSettings settings,
            List<FixedConversionIssue> issues)
        {
            if (source == null)
            {
                return null;
            }

            Converter c = new Converter(settings ?? new FixedConversionSettings(), issues, source.Id);
            FixedModifierBlueprint result = new FixedModifierBlueprint
            {
                Id = source.Id,
                DurationTicks = c.Ticks(source.Duration, "Duration"),
                DurationByLevelTicks = c.TicksArray(source.DurationByLevel, "DurationByLevel"),
                StackPolicy = (FixedStackPolicy)(int)source.StackPolicy,
                MaxStacks = source.MaxStacks,
                TickIntervalTicks = c.Ticks(source.TickInterval, "TickInterval"),
                TickOnApply = source.TickOnApply,
                RestartTicksOnRefresh = source.RestartTicksOnRefresh,
                IsDebuff = source.IsDebuff,
                Dispellable = source.Dispellable
            };

            if (source.Properties != null)
            {
                for (int i = 0; i < source.Properties.Count; i++)
                {
                    result.Properties.Add(c.Property(source.Properties[i], "Properties[" + i + "]"));
                }
            }

            if (source.States != null)
            {
                result.States.AddRange(source.States);
            }

            c.Nodes(source.TickEffects, result.TickEffects, "TickEffects");
            if (source.EventReactions != null)
            {
                for (int i = 0; i < source.EventReactions.Count; i++)
                {
                    ModifierEventReaction reaction = source.EventReactions[i];
                    if (reaction == null)
                    {
                        continue;
                    }

                    string path = "EventReactions[" + i + "]";
                    FixedModifierReaction converted = new FixedModifierReaction
                    {
                        EventId = reaction.EventId,
                        TargetEventSource = reaction.TargetEventSource,
                        Trigger = (FixedModifierTrigger)(int)reaction.Trigger,
                        EveryN = reaction.EveryN,
                        ThresholdRaw = c.Raw(reaction.Threshold, path + ".Threshold"),
                        StateId = reaction.StateId,
                        TagFilter = reaction.TagFilter,
                        AbilityFilter = reaction.AbilityFilter,
                        HitsOnly = reaction.HitsOnly,
                        MaxTriggers = reaction.MaxTriggers,
                        RemoveWhenSpent = reaction.RemoveWhenSpent,
                        CooldownTicks = c.Ticks(reaction.Cooldown, path + ".Cooldown"),
                        Priority = reaction.Priority
                    };
                    c.Nodes(reaction.Effects, converted.Effects, path + ".Effects");
                    result.EventReactions.Add(converted);
                }
            }

            return result;
        }

        private static int[] CopyArray(int[] values)
        {
            if (values == null || values.Length == 0)
            {
                return null;
            }

            int[] copy = new int[values.Length];
            Array.Copy(values, copy, values.Length);
            return copy;
        }

        /// <summary>A float value as raw in <paramref name="format" />, rounded half away from zero and saturated.</summary>
        public static int ToRaw(double value, FixedFormat format)
        {
            if (double.IsNaN(value))
            {
                return 0;
            }

            double scaled = value * format.One;
            if (scaled >= int.MaxValue)
            {
                return int.MaxValue;
            }

            if (scaled <= int.MinValue)
            {
                return int.MinValue;
            }

            return (int)(scaled < 0d ? -Math.Floor(-scaled + 0.5d) : Math.Floor(scaled + 0.5d));
        }

        /// <summary>
        ///     Seconds as whole ticks, rounded up (a positive duration is at least one tick): the step on which a float
        ///     timer stepped at the same rate reaches it. Non-positive seconds stay 0.
        /// </summary>
        public static int ToTicks(double seconds, int ticksPerSecond)
        {
            if (!(seconds > 0d))
            {
                return 0;
            }

            double ticks = seconds * ticksPerSecond;
            if (ticks >= int.MaxValue)
            {
                return int.MaxValue;
            }

            int result = (int)Math.Ceiling(ticks - TickSlack);
            return result < 1 ? 1 : result;
        }

        private sealed class Converter
        {
            private readonly FixedConversionSettings _settings;
            private readonly FixedFormat _format;
            private readonly List<FixedConversionIssue> _issues;
            private readonly string _id;

            public Converter(FixedConversionSettings settings, List<FixedConversionIssue> issues, string id)
            {
                _settings = settings;
                _format = settings.Format;
                _issues = issues;
                _id = id;
            }

            public int Raw(double value, string field)
            {
                int raw = ToRaw(value, _format);
                Check(value, (double)raw / _format.One, field, "raw");
                return raw;
            }

            public int[] RawArray(float[] values, string field)
            {
                if (values == null || values.Length == 0)
                {
                    return null;
                }

                int[] result = new int[values.Length];
                for (int i = 0; i < values.Length; i++)
                {
                    result[i] = Raw(values[i], field + "[" + i + "]");
                }

                return result;
            }

            public int Ticks(double seconds, string field)
            {
                int ticks = ToTicks(seconds, _settings.TicksPerSecond);
                double back = (double)ticks / _settings.TicksPerSecond;
                if (seconds > 0d && Math.Abs(back - seconds) > 0.5d / _format.One)
                {
                    Report(FixedConversionSeverity.Warning, field, seconds, back,
                        "not a whole number of ticks at " + _settings.TicksPerSecond +
                        " ticks per second; the timer ends on the next tick, as the float core does");
                }

                return ticks;
            }

            public int[] TicksArray(float[] values, string field)
            {
                if (values == null || values.Length == 0)
                {
                    return null;
                }

                int[] result = new int[values.Length];
                for (int i = 0; i < values.Length; i++)
                {
                    result[i] = Ticks(values[i], field + "[" + i + "]");
                }

                return result;
            }

            public FixedPropertyContribution Property(PropertyContribution source, string field)
            {
                float[] byLevel = source.ValueByLevel != null && source.ValueByLevel.Length > 0 ? source.ValueByLevel : null;
                if (source.Op == PropertyOp.Mul && _settings.ExactPercentMultipliers &&
                    (!OnGrid(source.Value) || !OnGrid(source.PerStackValue) || !AllOnGrid(byLevel)) &&
                    IsWholePercent(source.Value) && IsWholePercent(source.PerStackValue) && AllWholePercent(byLevel))
                {
                    // WHY: one denominator covers the flat value, the per-stack step and every level entry.
                    FixedPropertyContribution exact = new FixedPropertyContribution(source.PropertyId,
                        FixedPropertyOp.Mul, (int)Math.Round(source.Value * 100d),
                        (int)Math.Round(source.PerStackValue * 100d), 100);
                    if (byLevel != null)
                    {
                        exact.ValueByLevelRaw = new int[byLevel.Length];
                        for (int i = 0; i < byLevel.Length; i++)
                        {
                            exact.ValueByLevelRaw[i] = (int)Math.Round(byLevel[i] * 100d);
                        }
                    }

                    return exact;
                }

                return new FixedPropertyContribution(source.PropertyId, (FixedPropertyOp)(int)source.Op,
                    Raw(source.Value, field + ".Value"), Raw(source.PerStackValue, field + ".PerStackValue"))
                {
                    ValueByLevelRaw = RawArray(byLevel, field + ".ValueByLevel")
                };
            }

            private bool AllOnGrid(float[] values)
            {
                if (values == null)
                {
                    return true;
                }

                for (int i = 0; i < values.Length; i++)
                {
                    if (!OnGrid(values[i]))
                    {
                        return false;
                    }
                }

                return true;
            }

            private static bool AllWholePercent(float[] values)
            {
                if (values == null)
                {
                    return true;
                }

                for (int i = 0; i < values.Length; i++)
                {
                    if (!IsWholePercent(values[i]))
                    {
                        return false;
                    }
                }

                return true;
            }

            public FixedLeveledValue Leveled(LeveledValue source, string field)
            {
                return new FixedLeveledValue
                {
                    BaseRaw = Raw(source.Base, field + ".Base"),
                    LevelsRaw = RawArray(source.Levels, field + ".Levels"),
                    PerLevelRaw = Raw(source.PerLevel, field + ".PerLevel"),
                    ScaleProperty = source.ScaleProperty,
                    ScalePerPointRaw = Raw(source.ScalePerPoint, field + ".ScalePerPoint"),
                    ScaleFrom = (FixedScaleSource)(int)source.ScaleFrom,
                    LevelFrom = (FixedLevelSource)(int)source.LevelFrom,
                    ScaleStat = (FixedScaleStat)(int)source.ScaleStat,
                    ScaleMode = (FixedScaleMode)(int)source.ScaleMode,
                    SpentResource = source.SpentResource,
                    SpentCoefficientRaw = Raw(source.SpentCoefficient, field + ".SpentCoefficient")
                };
            }

            private int[] ChanceArray(float[] values, string field)
            {
                if (values == null || values.Length == 0)
                {
                    return null;
                }

                int[] result = new int[values.Length];
                for (int i = 0; i < values.Length; i++)
                {
                    result[i] = values[i] >= 1f ? FixedEffectNode.Always : Raw(values[i], field + "[" + i + "]");
                }

                return result;
            }

            public void Nodes(List<EffectNodeData> source, List<FixedEffectNode> target, string field)
            {
                if (source == null)
                {
                    return;
                }

                for (int i = 0; i < source.Count; i++)
                {
                    if (source[i] != null)
                    {
                        target.Add(Node(source[i], field + "[" + i + "]"));
                    }
                }
            }

            private FixedEffectNode Node(EffectNodeData source, string field)
            {
                if (!IsFixedOp(source.OpId))
                {
                    Report(FixedConversionSeverity.Warning, field + ".OpId", 0d, 0d,
                        "operation '" + source.OpId + "' is not built into the fixed core; register an " +
                        "IFixedEffectOperation with this id or the node does nothing");
                }

                int angle = (int)Math.Round(source.Angle, MidpointRounding.AwayFromZero);
                if (Math.Abs(angle - source.Angle) > 0.5d / _format.One)
                {
                    Report(FixedConversionSeverity.Warning, field + ".Angle", source.Angle, angle,
                        "cones use whole degrees");
                }

                return new FixedEffectNode
                {
                    OpId = source.OpId,
                    Target = (FixedTargetSelector)(int)source.Target,
                    TeamFilter = (FixedTeamFilter)(int)source.TeamFilter,
                    RadiusRaw = Raw(source.Radius, field + ".Radius"),
                    RadiusByLevelRaw = RawArray(source.RadiusByLevel, field + ".RadiusByLevel"),
                    MaxTargets = source.MaxTargets,
                    MaxTargetsByLevel = CopyArray(source.MaxTargetsByLevel),
                    AmountRaw = Raw(source.Amount, field + ".Amount"),
                    AmountByLevelRaw = RawArray(source.AmountByLevel, field + ".AmountByLevel"),
                    AmountKey = source.AmountKey,
                    AmountScaleProperty = source.AmountScaleProperty,
                    AmountScalePerPointRaw = Raw(source.AmountScalePerPoint, field + ".AmountScalePerPoint"),
                    AmountScaleSource = (FixedScaleSource)(int)source.AmountScaleSource,
                    AmountLevelSource = (FixedLevelSource)(int)source.AmountLevelSource,
                    AmountScaleStat = (FixedScaleStat)(int)source.AmountScaleStat,
                    AmountScaleMode = (FixedScaleMode)(int)source.AmountScaleMode,
                    AmountSpentResource = source.AmountSpentResource,
                    AmountSpentCoefficientRaw = Raw(source.AmountSpentCoefficient, field + ".AmountSpentCoefficient"),
                    DamageType = source.DamageType,
                    ModifierId = source.ModifierId,
                    ResourceId = source.ResourceId,
                    ArchetypeId = source.ArchetypeId,
                    CustomParam = source.CustomParam,
                    ChanceRaw = source.Chance >= 1f ? FixedEffectNode.Always : Raw(source.Chance, field + ".Chance"),
                    ChanceByLevelRaw = ChanceArray(source.ChanceByLevel, field + ".ChanceByLevel"),
                    AbilityId = source.AbilityId,
                    AngleDegrees = angle,
                    DurationTicks = Ticks(source.Duration, field + ".Duration"),
                    IntervalTicks = Ticks(source.Interval, field + ".Interval"),
                    DurationByLevelTicks = TicksArray(source.DurationByLevel, field + ".DurationByLevel"),
                    IntervalByLevelTicks = TicksArray(source.IntervalByLevel, field + ".IntervalByLevel"),
                    AmountPerStackRaw = Raw(source.AmountPerStack, field + ".AmountPerStack")
                };
            }

            private void Check(double original, double converted, string field, string unit)
            {
                if (double.IsNaN(original) || double.IsInfinity(original))
                {
                    Report(FixedConversionSeverity.Error, field, original, converted, "not a finite number");
                    return;
                }

                double error = Math.Abs(converted - original);
                if (error > 0.5d / _format.One + 1e-12)
                {
                    Report(FixedConversionSeverity.Warning, field, original, converted,
                        "outside the " + unit + " range of the format; saturated");
                }
                else if (error > 0d && _settings.ReportOffGridValues)
                {
                    Report(FixedConversionSeverity.Info, field, original, converted, "rounded to the grid");
                }
            }

            private bool OnGrid(float value)
            {
                double scaled = (double)value * _format.One;
                return Math.Abs(scaled - Math.Round(scaled)) < 1e-6;
            }

            private static bool IsWholePercent(float value)
            {
                double percent = value * 100d;
                return Math.Abs(percent - Math.Round(percent)) < 1e-4 && Math.Abs(percent) < int.MaxValue;
            }

            private static bool IsFixedOp(string opId)
            {
                switch (opId?.ToLowerInvariant())
                {
                    case FixedOps.Damage:
                    case FixedOps.Heal:
                    case FixedOps.ApplyModifier:
                    case FixedOps.RemoveModifier:
                    case FixedOps.Dispel:
                    case FixedOps.ResourceChange:
                    case FixedOps.Spawn:
                    case FixedOps.Execute:
                    case FixedOps.Chain:
                    case FixedOps.ModifyDamage:
                    case FixedOps.Reflect:
                    case FixedOps.Projectile:
                    case FixedOps.AreaApply:
                    case FixedOps.Zone:
                    case FixedOps.CastAbility:
                        return true;
                    default:
                        return false;
                }
            }

            private void Report(FixedConversionSeverity severity, string field, double original, double converted,
                string message)
            {
                _issues?.Add(new FixedConversionIssue(severity, _id, field, original, converted, message));
            }
        }
    }
}
