using System;
using System.Collections.Generic;
using HtmlToUitk.Capabilities.Model;
using HtmlToUitk.Core.Css;

namespace HtmlToUitk.Rules
{
    public sealed class TransitionRules : IRule
    {
        public string Id => "transitions";

        public void Evaluate(RuleContext context)
        {
            var reporter = new RuleReporter(context.Catalog, context.Config, context.Sink);
            var checker = new Checker(context.Capabilities, reporter);
            foreach (Stylesheet sheet in SheetScope.Convertible(context.Input))
            {
                foreach (StyleRule rule in sheet.Rules)
                {
                    foreach (Declaration d in rule.Declarations)
                    {
                        string name = SheetScope.NormalName(d.Name);
                        if (d.Value == null || !name.StartsWith("transition", StringComparison.Ordinal)) continue;
                        checker.Check(d, name);
                    }
                }
            }
        }

        public static bool IsTime(CssValue value)
        {
            var d = value as CssDimension;
            if (d == null) return false;
            string unit = d.Unit.ToLowerInvariant();
            return unit == "s" || unit == "ms";
        }

        private sealed class Checker
        {
            private readonly CapabilityMatrix _caps;
            private readonly RuleReporter _r;
            private readonly UssSpec _uss = UssSpec.Embedded;

            public Checker(CapabilityMatrix caps, RuleReporter reporter)
            {
                _caps = caps;
                _r = reporter;
            }

            public void Check(Declaration d, string name)
            {
                CssValue value = d.Value;
                string text = name + ": " + CssValues.Text(value);
                if (CssValues.ContainsVar(value)) return;
                string keyword = CssValues.Keyword(value);
                IReadOnlyList<CssValue> items = CssValues.CommaItems(value);
                switch (name)
                {
                    case "transition":
                        if (keyword == "none" || items.Count == 0) return;
                        foreach (CssValue item in items)
                        {
                            if (!CheckItem(d, text, item, false)) return;
                        }
                        return;
                    case "transition-property":
                        foreach (CssValue item in items)
                        {
                            if (!CheckProperty(d, text, CssValues.Keyword(item) ?? CssValues.Text(item))) return;
                        }
                        return;
                    case "transition-duration":
                    case "transition-delay":
                        foreach (CssValue item in items)
                        {
                            if (CssValues.Keyword(item) == "initial") continue;
                            if (!IsTime(item))
                            {
                                _r.Report("R009", d.Span, text, "'" + CssValues.Text(item) + "' is not a time (use s or ms)");
                                return;
                            }
                        }
                        return;
                    case "transition-timing-function":
                        foreach (CssValue item in items)
                        {
                            string k = CssValues.Keyword(item);
                            if (k == null || !_caps.HasEasing(k))
                            {
                                _r.Report("R007", d.Span, text, "easing '" + CssValues.Text(item) + "' is not supported by USS", EasingAdvice(CssValues.Text(item)));
                                return;
                            }
                        }
                        return;
                }
            }

            private bool CheckItem(Declaration d, string text, CssValue item, bool propertyOnly)
            {
                string property = null;
                var times = new List<CssValue>();
                CssValue timing = null;
                foreach (CssValue token in CssValues.SpaceTokens(item))
                {
                    string k = CssValues.Keyword(token);
                    string fn = CssValues.FunctionName(token);
                    if (IsTime(token)) times.Add(token);
                    else if ((k != null && _caps.HasEasing(k)) || fn == "cubic-bezier" || fn == "steps" || k == "step-start" || k == "step-end") timing = token;
                    else if (property == null) property = k ?? CssValues.Text(token).ToLowerInvariant();
                    else
                    {
                        _r.Report("R009", d.Span, text, "unexpected token '" + CssValues.Text(token) + "' in the transition");
                        return false;
                    }
                }
                if (property == null) property = "all";
                if (times.Count > 2)
                {
                    _r.Report("R009", d.Span, text, "more than two time values");
                    return false;
                }
                if (timing != null)
                {
                    string k = CssValues.Keyword(timing);
                    if (k == null || !_caps.HasEasing(k))
                    {
                        _r.Report("R007", d.Span, text, "easing '" + CssValues.Text(timing) + "' is not supported by USS", EasingAdvice(CssValues.Text(timing)));
                        return false;
                    }
                }
                if (property == "transform") return CheckProperty(d, text, "translate") & CheckProperty(d, text, "rotate") & CheckProperty(d, text, "scale");
                return CheckProperty(d, text, property);
            }

            private bool CheckProperty(Declaration d, string text, string property)
            {
                if (property == "all" || property == "none" || property == "initial" || property == "ignored" || property.StartsWith("--", StringComparison.Ordinal)) return true;
                PropertyCapability capability = _caps.Property(property);
                if (capability == null)
                {
                    _r.Report("C023", d.Span, text, "transition property '" + property + "' is not a USS property");
                    return false;
                }
                if (capability.Animatable != Animatable.Full) _r.Report("R006", d.Span, text, "'" + property + "' does not animate in UI Toolkit");
                if (_uss.LayoutProperties.Contains(property)) _r.Report("R008", d.Span, text, "transition on layout property '" + property + "'");
                return true;
            }

            private static string EasingAdvice(string easing)
            {
                string low = (easing ?? "").ToLowerInvariant();
                if (low.StartsWith("cubic-bezier(", StringComparison.Ordinal))
                {
                    return "Use a keyword; ease-out-cubic is close to cubic-bezier(0.33, 1, 0.68, 1), ease-in-out-cubic to cubic-bezier(0.65, 0, 0.35, 1).";
                }
                if (low.StartsWith("steps(", StringComparison.Ordinal) || low == "step-start" || low == "step-end")
                {
                    return "USS has no stepped easing; use linear or split the animation into class toggles.";
                }
                return null;
            }
        }
    }
}
