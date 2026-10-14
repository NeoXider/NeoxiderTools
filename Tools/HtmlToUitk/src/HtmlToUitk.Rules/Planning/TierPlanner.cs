using System;
using System.Collections.Generic;
using HtmlToUitk.Capabilities.Model;
using HtmlToUitk.Convert.Config;

namespace HtmlToUitk.Rules
{
    public sealed class TierPlanner : ITierPlanner
    {
        public static class Emulations
        {
            public const string GridFlatten = "grid-flatten";
            public const string GapMargins = "gap-margins";
            public const string StaticSelectors = "static-selectors";
            public const string PseudoElements = "pseudo-elements";
            public const string CalcFold = "calc-fold";
            public const string ViewportUnits = "viewport-units";
            public const string RelativeUnits = "relative-units";
            public const string MediaBreakpoints = "media-breakpoints";
            public const string KeyframesAnimator = "keyframes-animator";
            public const string GradientBake = "gradient-bake";
            public const string ShadowBake = "shadow-bake";
            public const string FilterBake = "filter-bake";
            public const string BackdropBlur = "backdrop-blur";
            public const string OverflowScrollview = "overflow-scrollview";
            public const string FixedToRoot = "fixed-to-root";
            public const string ZOrder = "z-order";
            public const string TransformDecompose = "transform-decompose";
            public const string ShorthandExpand = "shorthand-expand";
        }

        public static class RuntimeComponents
        {
            public const string BreakpointWatcher = "BreakpointWatcher";
            public const string UitkAnimator = "UitkAnimator";
            public const string BackdropBlur = "BackdropBlur";
        }

        private static readonly HashSet<string> ViewportUnits = new HashSet<string>(StringComparer.Ordinal)
        {
            "vw", "vh", "vmin", "vmax", "svw", "svh", "lvw", "lvh", "dvw", "dvh",
        };

        public TierDecision Decide(Construct construct, CapabilityMatrix caps, ConversionConfig config)
        {
            if (construct == null) throw new ArgumentNullException(nameof(construct));
            if (caps == null) throw new ArgumentNullException(nameof(caps));
            config = config ?? new ConversionConfig();
            TierDecision decision = Base(construct, caps, config);
            return ApplyConfig(decision, config);
        }

        public static TierDecision ApplyConfig(TierDecision decision, ConversionConfig config)
        {
            if (decision.Tier != Tier.ConvertTime && decision.Tier != Tier.Runtime) return decision;
            string reason = null;
            if (config.Emulation.StrictNative) reason = "emulation.strictNative is set";
            else if (decision.EmulationId != null && ContainsOrdinal(config.Emulation.Disable, decision.EmulationId)) reason = "emulation " + decision.EmulationId + " is disabled in the config";
            else if (decision.Tier == Tier.Runtime && !config.Emulation.AllowRuntime) reason = "emulation.allowRuntime is false and " + decision.EmulationId + " needs a runtime component";
            if (reason == null) return decision;
            return new TierDecision { Tier = Tier.Reject, EmulationId = null, RuleId = decision.RuleId, Reason = reason + "; the construct is rejected" };
        }

        private static bool ContainsOrdinal(IReadOnlyList<string> list, string value)
        {
            if (list == null) return false;
            for (int i = 0; i < list.Count; i++)
            {
                if (string.Equals(list[i], value, StringComparison.Ordinal)) return true;
            }
            return false;
        }

        private static TierDecision Native(string rule, string reason)
        {
            return new TierDecision { Tier = Tier.Native, RuleId = rule, Reason = reason };
        }

        private static TierDecision Convert(string rule, string emulation, string reason)
        {
            return new TierDecision { Tier = Tier.ConvertTime, RuleId = rule, EmulationId = emulation, Reason = reason };
        }

        private static TierDecision Runtime(string rule, string emulation, string reason, params string[] components)
        {
            return new TierDecision { Tier = Tier.Runtime, RuleId = rule, EmulationId = emulation, Reason = reason, RequiredRuntime = components };
        }

        private static TierDecision Reject(string rule, string reason)
        {
            return new TierDecision { Tier = Tier.Reject, RuleId = rule, Reason = reason };
        }

        private static TierDecision Silent(string emulation, string reason)
        {
            return new TierDecision { Tier = Tier.ConvertTime, RuleId = null, EmulationId = emulation, Reason = reason };
        }

        private static TierDecision Base(Construct c, CapabilityMatrix caps, ConversionConfig config)
        {
            string key = c.Key ?? "";
            if (key == ConstructKeys.DisplayGrid)
            {
                string unresolvable = Cx.Get(c, ConstructKeys.DataUnresolvable);
                if (unresolvable != null) return Reject("E001", "the grid template uses " + unresolvable + ", which grid-flatten cannot resolve");
                int tracks;
                if (int.TryParse(Cx.Get(c, ConstructKeys.DataTracks), out tracks) && tracks > config.Emulation.GridMaxTracks)
                {
                    return Reject("E001", "the grid has " + tracks + " tracks, more than emulation.gridMaxTracks (" + config.Emulation.GridMaxTracks + ")");
                }
                return Convert("C001", Emulations.GridFlatten, "static grid templates are flattened into nested flex containers");
            }
            if (key == ConstructKeys.Gap)
            {
                string property = Cx.Get(c, ConstructKeys.DataProperty) ?? "gap";
                if (caps.HasProperty(property) || (property == "gap" && caps.HasProperty("row-gap") && caps.HasProperty("column-gap")))
                {
                    return Native("C002", property + " exists in the capability matrix");
                }
                return Convert("C002", Emulations.GapMargins, property + " is emulated with margins");
            }
            if (key == ConstructKeys.ZIndex) return Convert("C021", Emulations.ZOrder, "siblings are reordered by z-index");
            if (key == ConstructKeys.SelectorSibling)
            {
                string combinator = Cx.Get(c, ConstructKeys.DataCombinator);
                if (!string.IsNullOrEmpty(combinator) && caps.HasCombinator(combinator[0])) return Native("C015", "the combinator exists in the capability matrix");
                return Convert("C015", Emulations.StaticSelectors, "the selector is evaluated against the static DOM");
            }
            if (key == ConstructKeys.SelectorAttribute) return Convert("C015", Emulations.StaticSelectors, "the attribute selector is evaluated against the static DOM");
            if (key == ConstructKeys.SelectorType) return Reject("C016", "type selectors do not map to UI Toolkit element types");
            if (key.StartsWith(ConstructKeys.PseudoPrefix, StringComparison.Ordinal)) return PseudoClass(key.Substring(ConstructKeys.PseudoPrefix.Length), caps);
            if (key.StartsWith(ConstructKeys.PseudoElementPrefix, StringComparison.Ordinal))
            {
                string name = key.Substring(ConstructKeys.PseudoElementPrefix.Length);
                if (name == "before" || name == "after")
                {
                    if (Cx.Get(c, ConstructKeys.DataContent) == "text") return Reject("L001", "the text content of ::" + name + " needs a localisation key; use content: attr(data-text-" + name + ") with data-loc-" + name);
                    return Convert("C005", Emulations.PseudoElements, "::" + name + " becomes a generated child element");
                }
                return Reject("C005", "::" + name + " cannot be emulated");
            }
            if (key.StartsWith(ConstructKeys.AtPrefix, StringComparison.Ordinal)) return AtRule(key.Substring(ConstructKeys.AtPrefix.Length), c);
            if (key == ConstructKeys.AnimationDeclaration)
            {
                return Runtime(null, Emulations.KeyframesAnimator, "animation declarations are driven by the UitkAnimator runtime component", RuntimeComponents.UitkAnimator);
            }
            if (key.StartsWith(ConstructKeys.FunctionPrefix, StringComparison.Ordinal)) return Function(key.Substring(ConstructKeys.FunctionPrefix.Length), c, caps);
            if (key.StartsWith(ConstructKeys.UnitPrefix, StringComparison.Ordinal)) return Unit(key.Substring(ConstructKeys.UnitPrefix.Length), caps);
            if (key.StartsWith(ConstructKeys.FilterPrefix, StringComparison.Ordinal)) return Filter(key.Substring(ConstructKeys.FilterPrefix.Length), c, caps);
            if (key == ConstructKeys.ShadowOuter)
            {
                if (caps.HasProperty("filter") && caps.HasFilter("drop-shadow")) return Native("C013", "box-shadow is written as filter: drop-shadow(), which follows the rendered alpha and ignores the spread");
                return Convert("C013", Emulations.ShadowBake, "the shadow is baked into a 9-slice texture and a child element");
            }
            if (key == ConstructKeys.ShadowInset) return Reject("C013", "inset shadows cannot be emulated");
            if (key.StartsWith(ConstructKeys.OverflowPrefix, StringComparison.Ordinal))
            {
                string value = key.Substring(ConstructKeys.OverflowPrefix.Length);
                if (value == "auto" || value == "scroll" || value == "overlay" || value == "axis") return Convert("C020", Emulations.OverflowScrollview, "the element becomes a ScrollView");
                return Reject("C020", "overflow: " + value + " is not valid");
            }
            if (key.StartsWith(ConstructKeys.PositionPrefix, StringComparison.Ordinal))
            {
                string value = key.Substring(ConstructKeys.PositionPrefix.Length);
                if (value == "fixed") return Convert("C009", Emulations.FixedToRoot, "the element is re-parented under the panel root");
                return Reject("C009", "position: " + value + " cannot be emulated");
            }
            if (key == ConstructKeys.TransformSingle) return Silent(Emulations.ShorthandExpand, "a single transform function expands to rotate, scale or translate");
            if (key == ConstructKeys.TransformMulti)
            {
                string order = Cx.Get(c, ConstructKeys.DataUnresolvable);
                if (order != null) return Reject("E001", "the transform functions are not in the order translate, rotate, scale (" + order + ")");
                return Convert("C022", Emulations.TransformDecompose, "several transform functions are decomposed into rotate, scale and translate");
            }
            if (key == ConstructKeys.TransformUnsupported) return Reject("C022", "only translate, rotate and scale functions can be decomposed");
            if (key.StartsWith(ConstructKeys.KeywordPrefix, StringComparison.Ordinal))
            {
                string keyword = key.Substring(ConstructKeys.KeywordPrefix.Length);
                if (caps.HasKeyword(keyword)) return Native("C018", "the keyword exists in the capability matrix, but the Unity manual lists only initial");
                return Reject("C018", keyword + " does not exist in USS");
            }
            if (key.StartsWith(ConstructKeys.ShorthandPrefix, StringComparison.Ordinal)) return Silent(Emulations.ShorthandExpand, "the shorthand expands to USS longhands");
            return Native(null, "no planning rule for " + key);
        }

        private static TierDecision PseudoClass(string name, CapabilityMatrix caps)
        {
            if (name == "has") return Reject("C003", ":has() cannot be evaluated statically");
            if (PseudoNames.Functional.Contains(name))
            {
                if (name != "not" && name != "is" && name != "where" && name != "matches" && name != "any" && name != "-webkit-any" && name != "-moz-any")
                {
                    return Reject("C035", ":" + name + " is not supported by USS");
                }
                if (caps.HasPseudoClass(name)) return Native("C003", ":" + name + " exists in the capability matrix");
                return Convert("C003", Emulations.StaticSelectors, ":" + name + "() is evaluated against the static DOM");
            }
            if (PseudoNames.Structural.Contains(name))
            {
                if (caps.HasPseudoClass(name)) return Native("C004", ":" + name + " exists in the capability matrix");
                return Convert("C004", Emulations.StaticSelectors, ":" + name + " is evaluated against the static DOM");
            }
            return Reject("C035", ":" + name + " is not supported by USS");
        }

        private static TierDecision AtRule(string name, Construct c)
        {
            if (name == "media")
            {
                string unsupported = Cx.Get(c, ConstructKeys.DataFeature);
                if (unsupported != null) return Reject("C007", "the media query uses " + unsupported + ", which cannot be mapped to breakpoint classes");
                return Runtime("C007", Emulations.MediaBreakpoints, "media queries become root classes toggled by the BreakpointWatcher", RuntimeComponents.BreakpointWatcher);
            }
            if (name == "keyframes")
            {
                if (Cx.Is(c, ConstructKeys.DataInvalid)) return new TierDecision { Tier = Tier.Reject, RuleId = null, Reason = "the keyframes animate properties that cannot be animated" };
                return Runtime("C007", Emulations.KeyframesAnimator, "keyframes are written to an animation spec played by the UitkAnimator", RuntimeComponents.UitkAnimator);
            }
            return Reject("C007", "@" + name + " is not supported");
        }

        private static TierDecision Function(string name, Construct c, CapabilityMatrix caps)
        {
            switch (name)
            {
                case "calc":
                case "min":
                case "max":
                case "clamp":
                    if (Cx.Is(c, ConstructKeys.DataFoldable)) return Convert("C006", Emulations.CalcFold, name + "() of constants is folded at conversion time");
                    return Reject("C006", name + "() depends on mixed units or non-constant values and cannot be folded");
                case "attr":
                    return Reject("C006", "attr() reads DOM attributes at render time");
                case "env":
                    if (caps.HasFunction("env")) return Native("C006", "env() exists in the capability matrix");
                    return Reject("C006", "env() does not exist in USS");
                case "linear-gradient":
                    if (caps.HasFunction("linear-gradient") && Cx.Is(c, ConstructKeys.DataSupported)) return Native("C011", "linear-gradient() exists in the capability matrix");
                    return Convert("C011", Emulations.GradientBake, "the gradient is baked into a texture");
                default:
                    return Convert("C011", Emulations.GradientBake, name + "() is baked into a texture");
            }
        }

        private static TierDecision Unit(string unit, CapabilityMatrix caps)
        {
            if (caps.HasUnit(unit)) return Native("C008", "the unit exists in the capability matrix");
            if (ViewportUnits.Contains(unit)) return Convert("C008", Emulations.ViewportUnits, unit + " is resolved against the reference resolution");
            switch (unit)
            {
                case "em":
                case "rem":
                case "ch":
                case "ex":
                case "pt":
                case "pc":
                case "cm":
                case "mm":
                case "in":
                case "q":
                    return Convert("C008", Emulations.RelativeUnits, unit + " is resolved to px with the static font size");
                default:
                    return Reject("C008", "the unit " + unit + " does not exist in USS");
            }
        }

        private static TierDecision Filter(string function, Construct c, CapabilityMatrix caps)
        {
            string property = Cx.Get(c, ConstructKeys.DataProperty) ?? "filter";
            if (function != "url" && caps.HasProperty(property) && caps.HasFilter(function))
            {
                string condition = caps.Condition(property);
                return Native("C010", condition == null ? function + "() exists in the capability matrix" : function + "() exists in the capability matrix; " + property + " requires " + condition);
            }
            if (property == "backdrop-filter")
            {
                if (function == "blur") return Runtime("C010", Emulations.BackdropBlur, "backdrop blur is rendered by the BackdropBlur runtime component", RuntimeComponents.BackdropBlur);
                return Reject("C010", "backdrop-filter: " + function + "() cannot be emulated");
            }
            if (function == "url") return Reject("C010", "filter: url() cannot be emulated");
            return Convert("C010", Emulations.FilterBake, "the filter is baked into the image at conversion time");
        }
    }
}
