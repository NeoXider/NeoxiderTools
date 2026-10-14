using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text.RegularExpressions;
using HtmlToUitk.Convert.Css;
using HtmlToUitk.Convert.Dialect;
using HtmlToUitk.Core.Css;
using HtmlToUitk.Core.Text;
using HtmlToUitk.Rules;

namespace HtmlToUitk.Convert.Emulations
{
    public sealed class PageRouterEmulation : EmulationBase
    {
        public override string Id => "page-router";
        public override Tier Tier => Tier.Runtime;
        public override IReadOnlyList<string> Handles => new[] { "page-router" };
        public override IReadOnlyList<string> RuntimeComponents => new[] { "UitkRouter" };
        public override int Order => 100;
        internal override void Run(TransformState state, Construct construct, EmulationOutcome outcome)
        {
            if (!state.Config.Emulation.AllowRuntime || state.Config.Emulation.Disable.Contains(Id)) throw new InvalidOperationException("page-router runtime backend is disabled");
            state.SpecSink.SetNav(state.Page.Nav);
        }
    }

    public sealed class KeyframesAnimatorEmulation : EmulationBase
    {
        public override string Id => EmulationIds.KeyframesAnimator;
        public override Tier Tier => Tier.Runtime;
        public override IReadOnlyList<string> Handles => new[] { "keyframes", "animation" };
        public override IReadOnlyList<string> RuntimeComponents => new[] { "UitkAnimator" };
        public override int Order => 90;

        internal override void Run(TransformState state, Construct construct, EmulationOutcome outcome)
        {
            AnimModel model = AnimCollector.Collect(state.Page, state.Caps, state.Config);
            foreach (AnimDef animation in model.Animations) state.SpecSink.AddAnimation(animation);
            foreach (AnimBinding binding in model.Bindings) state.SpecSink.AddAnimationBinding(binding);
            RuleOps.MapRules(state.Sheets, (id, rule) =>
            {
                List<Declaration> kept = new List<Declaration>();
                foreach (Declaration declaration in rule.Declarations)
                    if (!declaration.Name.StartsWith("animation", StringComparison.Ordinal) && !declaration.Name.StartsWith("--anim-", StringComparison.Ordinal)) kept.Add(declaration);
                return new[] { RuleOps.WithDeclarations(rule, kept) };
            });
        }
    }

    public sealed class MediaBreakpointsEmulation : EmulationBase
    {
        public override string Id => EmulationIds.MediaBreakpoints;
        public override Tier Tier => Tier.Runtime;
        public override IReadOnlyList<string> Handles => new[] { "media" };
        public override IReadOnlyList<string> RuntimeComponents => new[] { "BreakpointWatcher" };
        public override int Order => 80;

        internal override void Run(TransformState state, Construct construct, EmulationOutcome outcome)
        {
            foreach (HtmlToUitk.Core.Uxml.UxmlElement element in state.Elements()) state.Editor.AddClass(element, "htu-media-base");
            RuleOps.MapRules(state.Sheets, (sheet, rule) =>
            {
                if (rule.AtContext.Count == 0)
                {
                    List<Selector> normal = new List<Selector>();
                    foreach (Selector selector in rule.Selectors)
                    {
                        string original = SelectorFormat.Text(selector);
                        normal.Add(original == ":root" ? selector : SelectorFormat.Parse(original + ".htu-media-base", rule.Span));
                    }
                    return new[] { RuleOps.WithSelectors(rule, normal) };
                }
                if (rule.AtContext[0].IndexOf("keyframes", StringComparison.OrdinalIgnoreCase) >= 0) return new[] { rule };
                string queryText = "";
                foreach (string context in rule.AtContext)
                {
                    if (!context.StartsWith("@media", StringComparison.OrdinalIgnoreCase))
                        throw new InvalidOperationException("unsupported nested CSS context: " + context);
                    string part = context.Substring(6).Trim();
                    if (part.IndexOf("prefers-reduced-motion", StringComparison.OrdinalIgnoreCase) >= 0)
                    {
                        bool onlyAnimation = true;
                        foreach (Declaration declaration in rule.Declarations)
                            if (!declaration.Name.StartsWith("animation", StringComparison.Ordinal) && !declaration.Name.StartsWith("--anim-", StringComparison.Ordinal)) onlyAnimation = false;
                        if (onlyAnimation) return Array.Empty<StyleRule>();
                        throw new InvalidOperationException("unsupported reduced-motion CSS declarations outside animation spec");
                    }
                    queryText += (queryText.Length == 0 ? "" : " and ") + part;
                }
                MediaQuery query = ParseQuery(queryText);
                List<Selector> gates = new List<Selector>();
                int index = 0;
                foreach (Selector selector in rule.Selectors)
                {
                    string text = SelectorFormat.Text(selector);
                    // Keep specificity and the cascade on the same element; dynamic descendant selectors need a separate backend.
                    if (!Regex.IsMatch(text, @"^(?:[.#][A-Za-z_][A-Za-z0-9_-]*)+$", RegexOptions.CultureInvariant))
                        throw new InvalidOperationException("unsupported responsive selector: " + text);
                    string className = "mq-" + Fnv.Hex8(sheet + "|" + rule.Order + "|" + queryText + "|" + text + "|" + index++);
                    state.SpecSink.AddMediaClass(state.Document.Roots.Count == 0 ? "" : state.Document.Roots[0].GetAttribute("name"),
                        new MediaClass { Class = className, Selector = text, Query = query });
                    gates.Add(SelectorFormat.Parse(text + "." + className, rule.Span));
                }
                return new[] { RuleOps.Rule(gates, rule.Declarations, rule.Span, rule.Order) };
            });
        }

        internal static MediaQuery ParseQuery(string text)
        {
            MediaQuery result = new MediaQuery();
            string remaining = Regex.Replace(text, @"\(([^():]+):\s*([^()]+)\)", match =>
            {
                string name = match.Groups[1].Value.Trim().ToLowerInvariant();
                string value = match.Groups[2].Value.Trim().ToLowerInvariant();
                if (name == "orientation")
                {
                    if (value != "landscape" && value != "portrait") throw new InvalidOperationException("unsupported media orientation: " + value);
                    result.Orientation = value;
                    return " ";
                }
                bool aspect = name.EndsWith("aspect-ratio", StringComparison.Ordinal);
                double number;
                if (aspect)
                {
                    string[] ratio = value.Split('/');
                    if (ratio.Length != 2 || !double.TryParse(ratio[0].Trim(), NumberStyles.Float, CultureInfo.InvariantCulture, out double numerator)
                        || !double.TryParse(ratio[1].Trim(), NumberStyles.Float, CultureInfo.InvariantCulture, out double denominator) || denominator <= 0)
                        throw new InvalidOperationException("unsupported media aspect ratio: " + value);
                    number = numerator / denominator;
                }
                else
                {
                    if (!value.EndsWith("px", StringComparison.Ordinal) || !double.TryParse(value.Substring(0, value.Length - 2), NumberStyles.Float, CultureInfo.InvariantCulture, out number))
                        throw new InvalidOperationException("media dimensions require finite px: " + value);
                }
                if (double.IsNaN(number) || double.IsInfinity(number) || number < 0 || (aspect && number == 0)) throw new InvalidOperationException("invalid media bound: " + value);
                switch (name)
                {
                    case "min-width": result.MinWidth = Math.Max(result.MinWidth ?? 0, number); break;
                    case "max-width": result.MaxWidth = Math.Min(result.MaxWidth ?? double.MaxValue, number); break;
                    case "min-height": result.MinHeight = Math.Max(result.MinHeight ?? 0, number); break;
                    case "max-height": result.MaxHeight = Math.Min(result.MaxHeight ?? double.MaxValue, number); break;
                    case "min-aspect-ratio": result.MinAspect = Math.Max(result.MinAspect ?? 0, number); break;
                    case "max-aspect-ratio": result.MaxAspect = Math.Min(result.MaxAspect ?? double.MaxValue, number); break;
                    default: throw new InvalidOperationException("unsupported media feature: " + name);
                }
                return " ";
            });
            remaining = Regex.Replace(remaining, @"\b(?:and|screen|all|only)\b", "", RegexOptions.IgnoreCase).Trim();
            if (remaining.Length != 0) throw new InvalidOperationException("unsupported media query: " + text);
            if ((result.MinWidth.HasValue && result.MaxWidth.HasValue && result.MinWidth > result.MaxWidth)
                || (result.MinHeight.HasValue && result.MaxHeight.HasValue && result.MinHeight > result.MaxHeight)
                || (result.MinAspect.HasValue && result.MaxAspect.HasValue && result.MinAspect > result.MaxAspect))
                throw new InvalidOperationException("contradictory media bounds: minimum exceeds maximum in " + text);
            return result;
        }
    }
}
