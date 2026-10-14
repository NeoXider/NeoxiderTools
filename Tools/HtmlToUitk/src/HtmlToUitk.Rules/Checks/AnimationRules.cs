using System;
using System.Collections.Generic;
using HtmlToUitk.Capabilities.Model;
using HtmlToUitk.Core.Css;

namespace HtmlToUitk.Rules
{
    public sealed class AnimationRules : IRule
    {
        private static readonly HashSet<string> Directions = new HashSet<string>(StringComparer.Ordinal) { "normal", "reverse", "alternate", "alternate-reverse" };
        private static readonly HashSet<string> FillModes = new HashSet<string>(StringComparer.Ordinal) { "none", "forwards", "backwards", "both" };
        private static readonly HashSet<string> PlayStates = new HashSet<string>(StringComparer.Ordinal) { "running", "paused" };

        public string Id => "animation";

        public void Evaluate(RuleContext context)
        {
            var r = new RuleReporter(context.Catalog, context.Config, context.Sink, context.Capabilities);
            CapabilityMatrix caps = context.Capabilities;
            var keyframes = new Dictionary<string, AtRule>(StringComparer.Ordinal);
            foreach (Stylesheet sheet in SheetScope.Convertible(context.Input))
            {
                foreach (AtRule at in sheet.AtRules)
                {
                    if (AtRuleConstructDetector.NormalName(at.Name) != "keyframes") continue;
                    string name = (at.Prelude ?? "").Trim();
                    if (name.Length > 0 && !keyframes.ContainsKey(name)) keyframes[name] = at;
                    CheckKeyframes(context, r, caps, at, name);
                }
            }
            foreach (Stylesheet sheet in SheetScope.Convertible(context.Input))
            {
                foreach (StyleRule rule in sheet.Rules)
                {
                    bool handled = HasReducedMotion(rule);
                    foreach (Declaration d in rule.Declarations)
                    {
                        string name = SheetScope.NormalName(d.Name);
                        if (!name.StartsWith("animation", StringComparison.Ordinal) || d.Value == null) continue;
                        CheckDeclaration(context, r, caps, keyframes, d, name, handled);
                    }
                }
            }
        }

        private static bool HasReducedMotion(StyleRule rule)
        {
            foreach (Declaration d in rule.Declarations)
            {
                if (SheetScope.NormalName(d.Name) != "--anim-reduced-motion" || d.Value == null) continue;
                string k = CssValues.Keyword(d.Value);
                if (k == "skip" || k == "simplify") return true;
            }
            return false;
        }

        private static void CheckKeyframes(RuleContext context, RuleReporter r, CapabilityMatrix caps, AtRule at, string name)
        {
            UssSpec uss = UssSpec.Embedded;
            bool allowLayout = context.Config.Rules.AllowLayoutAnimation;
            if (at.Rules.Count > context.Config.Rules.MaxKeyframeSteps)
            {
                r.Report("A004", at.Span, "@keyframes " + name, "@keyframes " + name + " has " + at.Rules.Count + " steps; the limit is " + context.Config.Rules.MaxKeyframeSteps);
            }
            var seen = new HashSet<string>(StringComparer.Ordinal);
            foreach (StyleRule block in at.Rules)
            {
                foreach (Declaration d in block.Declarations)
                {
                    string property = SheetScope.NormalName(d.Name);
                    if (property.StartsWith("--", StringComparison.Ordinal) || property == "animation-timing-function") continue;
                    if (!seen.Add(property)) continue;
                    PropertyCapability capability = caps.Property(property);
                    string text = property + ": " + CssValues.Text(d.Value);
                    if (capability == null || capability.Animatable == Animatable.None)
                    {
                        r.Report("A002", d.Span, text, "'" + property + "' cannot be animated in USS (keyframes " + name + ")");
                    }
                    else if (!allowLayout && uss.LayoutProperties.Contains(property))
                    {
                        r.Report("A003", d.Span, text, "'" + property + "' is a layout property (keyframes " + name + ")");
                    }
                }
            }
        }

        private static void CheckDeclaration(RuleContext context, RuleReporter r, CapabilityMatrix caps, Dictionary<string, AtRule> keyframes, Declaration d, string name, bool reducedMotionHandled)
        {
            string text = name + ": " + CssValues.Text(d.Value);
            CssValue value = d.Value;
            if (CssValues.ContainsVar(value)) return;
            switch (name)
            {
                case "animation":
                    foreach (CssValue item in CssValues.CommaItems(value)) CheckShorthandItem(context, r, caps, keyframes, d, text, item, reducedMotionHandled);
                    return;
                case "animation-name":
                    foreach (CssValue item in CssValues.CommaItems(value))
                    {
                        string k = CssValues.Keyword(item) ?? CssValues.Text(item);
                        if (k != "none" && !keyframes.ContainsKey(k)) r.Report("A001", d.Span, text, "@keyframes " + k + " is not defined");
                    }
                    return;
                case "animation-duration":
                case "animation-delay":
                    foreach (CssValue item in CssValues.CommaItems(value))
                    {
                        var dim = item as CssDimension;
                        if (dim == null || !TransitionRules.IsTime(item) || (name == "animation-duration" && dim.Value < 0))
                        {
                            r.Report("A006", d.Span, text, "'" + CssValues.Text(item) + "' is not a valid " + (name == "animation-duration" ? "duration" : "delay") + " (use s or ms)");
                            return;
                        }
                    }
                    return;
                case "animation-iteration-count":
                    foreach (CssValue item in CssValues.CommaItems(value))
                    {
                        if (CssValues.Keyword(item) == "infinite")
                        {
                            if (!reducedMotionHandled) r.Report("A005", d.Span, text, "an infinite animation has no --anim-reduced-motion handling");
                            continue;
                        }
                        double n;
                        string unit;
                        if (!CssValues.TryDimension(item, out n, out unit) || unit != "" || n < 0 || n != Math.Floor(n))
                        {
                            r.Report("A006", d.Span, text, "'" + CssValues.Text(item) + "' is not a valid iteration count");
                            return;
                        }
                    }
                    return;
                case "animation-timing-function":
                    foreach (CssValue item in CssValues.CommaItems(value))
                    {
                        string k = CssValues.Keyword(item);
                        if (k == null || !caps.HasEasing(k)) r.Report("R007", d.Span, text, "easing '" + CssValues.Text(item) + "' is not supported by USS");
                    }
                    return;
            }
        }

        private static void CheckShorthandItem(RuleContext context, RuleReporter r, CapabilityMatrix caps, Dictionary<string, AtRule> keyframes, Declaration d, string text, CssValue item, bool reducedMotionHandled)
        {
            string animationName = null;
            var times = new List<CssDimension>();
            bool infinite = false;
            foreach (CssValue token in CssValues.SpaceTokens(item))
            {
                string k = CssValues.Keyword(token);
                string fn = CssValues.FunctionName(token);
                var dim = token as CssDimension;
                double n;
                string unit;
                if (dim != null && TransitionRules.IsTime(token))
                {
                    times.Add(dim);
                }
                else if (k == "infinite")
                {
                    infinite = true;
                }
                else if (k != null && caps.HasEasing(k))
                {
                    continue;
                }
                else if (fn == "cubic-bezier" || fn == "steps" || k == "step-start" || k == "step-end")
                {
                    r.Report("R007", d.Span, text, "easing '" + CssValues.Text(token) + "' is not supported by USS");
                    return;
                }
                else if (k != null && (Directions.Contains(k) || FillModes.Contains(k) || PlayStates.Contains(k)))
                {
                    continue;
                }
                else if (CssValues.TryDimension(token, out n, out unit) && unit == "")
                {
                    if (n < 0 || n != Math.Floor(n))
                    {
                        r.Report("A006", d.Span, text, "'" + CssValues.Text(token) + "' is not a valid iteration count");
                        return;
                    }
                }
                else if (k != null && animationName == null)
                {
                    animationName = CssValues.Keyword(token) == null ? CssValues.Text(token) : TokenName(token);
                }
                else if (token is CssString && animationName == null)
                {
                    animationName = ((CssString)token).Value;
                }
                else
                {
                    r.Report("A006", d.Span, text, "unexpected token '" + CssValues.Text(token) + "' in the animation");
                    return;
                }
            }
            if (times.Count > 2)
            {
                r.Report("A006", d.Span, text, "more than two time values");
                return;
            }
            if (times.Count > 0 && times[0].Value < 0)
            {
                r.Report("A006", d.Span, text, "the duration must not be negative");
                return;
            }
            if (animationName != null && animationName != "none" && !keyframes.ContainsKey(animationName))
            {
                r.Report("A001", d.Span, text, "@keyframes " + animationName + " is not defined");
            }
            if (infinite && !reducedMotionHandled)
            {
                r.Report("A005", d.Span, text, "an infinite animation has no --anim-reduced-motion handling");
            }
        }

        private static string TokenName(CssValue token)
        {
            var ident = token as CssIdent;
            return ident != null ? ident.Name : CssValues.Keyword(token);
        }
    }
}
