using System;
using System.Collections.Generic;
using HtmlToUitk.Convert.Css;
using HtmlToUitk.Core.Css;
using HtmlToUitk.Core.Uxml;
using HtmlToUitk.Rules;

namespace HtmlToUitk.Convert.Emulations
{
    public sealed class StaticSelectorsEmulation : EmulationBase
    {
        private static readonly HashSet<string> StaticPseudos = new HashSet<string>(StringComparer.Ordinal)
        {
            "not", "is", "where", "matches", "any", "first-child", "last-child", "only-child", "nth-child", "nth-last-child",
            "nth-of-type", "nth-last-of-type", "first-of-type", "last-of-type", "only-of-type", "empty"
        };

        public override string Id => "static-selectors";

        public override Tier Tier => Tier.ConvertTime;

        public override IReadOnlyList<string> Handles => new[] { "pseudo:not", "pseudo:is", "pseudo:where", "pseudo:nth-child", "pseudo:first-child", "selector:sibling", "selector:attribute" };

        public override int Order => 50;

        private sealed class Feature
        {
            public string Key;
            public int PrefixLength;
            public string ClassName;
        }

        internal override void Run(TransformState state, Construct construct, EmulationOutcome outcome)
        {
            var names = new NameGenerator();
            RuleOps.MapRules(state.Sheets, (sheetId, rule) =>
            {
                bool touched = false;
                var selectors = new List<Selector>();
                foreach (Selector selector in rule.Selectors)
                {
                    bool dropped;
                    Selector rewritten = Rewrite(state, names, selector, out dropped);
                    if (dropped)
                    {
                        touched = true;
                        continue;
                    }

                    if (!ReferenceEquals(rewritten, selector)) touched = true;
                    selectors.Add(rewritten);
                }

                if (!touched) return null;
                if (selectors.Count == 0) return new StyleRule[0];
                return new[] { RuleOps.WithSelectors(rule, selectors) };
            });
        }

        private static bool NeedsRewrite(Selector selector)
        {
            for (int i = 0; i < selector.Parts.Count; i++)
            {
                char combinator = selector.Parts[i].Combinator;
                SimpleSelector part = selector.Parts[i].Part;
                if (i > 0 && (combinator == '+' || combinator == '~')) return true;
                if (part.Attributes.Count > 0) return true;
                foreach (PseudoClass pseudo in part.Pseudos)
                {
                    if (!IsNativePseudo(pseudo.Name)) return true;
                }
            }

            return false;
        }

        private static bool IsNativePseudo(string name)
        {
            return name == "hover" || name == "active" || name == "focus" || name == "disabled" || name == "enabled" || name == "checked" || name == "inactive" || name == "root";
        }

        private static Selector Rewrite(TransformState state, NameGenerator names, Selector selector, out bool dropped)
        {
            dropped = false;
            if (!NeedsRewrite(selector)) return selector;
            var newParts = new List<(char, SimpleSelector)>();
            var features = new List<Feature>();
            var originalParts = selector.Parts;
            for (int i = 0; i < originalParts.Count; i++)
            {
                char combinator = originalParts[i].Combinator;
                SimpleSelector part = originalParts[i].Part;
                var featureClasses = new List<string>();
                if (i > 0 && (combinator == '+' || combinator == '~'))
                {
                    SimpleSelector left = originalParts[i - 1].Part;
                    string key = SelectorFormat.Compound(left) + " " + combinator;
                    var feature = new Feature { Key = key, PrefixLength = i + 1, ClassName = names.Generate("s", key) };
                    features.Add(feature);
                    featureClasses.Add(feature.ClassName);
                    if (newParts.Count > 0) newParts.RemoveAt(newParts.Count - 1);
                    combinator = originalParts[i - 1].Combinator;
                    if (newParts.Count == 0) combinator = '\0';
                }

                var keptPseudos = new List<PseudoClass>();
                foreach (PseudoClass pseudo in part.Pseudos)
                {
                    if (IsNativePseudo(pseudo.Name))
                    {
                        keptPseudos.Add(pseudo);
                        continue;
                    }

                    if (!StaticPseudos.Contains(pseudo.Name))
                    {
                        dropped = true;
                        return selector;
                    }

                    string key = SelectorFormat.Compound(new SimpleSelector { Pseudos = new[] { pseudo } });
                    var feature = new Feature { Key = key, PrefixLength = i + 1, ClassName = names.Generate("s", key) };
                    features.Add(feature);
                    featureClasses.Add(feature.ClassName);
                }

                foreach (AttributeSelector attribute in part.Attributes)
                {
                    string key = SelectorFormat.Compound(new SimpleSelector { Attributes = new[] { attribute } });
                    var feature = new Feature { Key = key, PrefixLength = i + 1, ClassName = names.Generate("s", key) };
                    features.Add(feature);
                    featureClasses.Add(feature.ClassName);
                }

                var classes = new List<string>(part.Classes);
                foreach (string c in featureClasses)
                {
                    if (!classes.Contains(c)) classes.Add(c);
                }

                bool anything = !string.IsNullOrEmpty(part.Type) || !string.IsNullOrEmpty(part.Id) || classes.Count > 0 || keptPseudos.Count > 0;
                var rewrittenPart = new SimpleSelector
                {
                    Type = part.Type,
                    Id = part.Id,
                    Classes = classes,
                    Pseudos = keptPseudos,
                    Attributes = new List<AttributeSelector>(),
                    PseudoElements = part.PseudoElements,
                    Universal = part.Universal && !anything
                };
                newParts.Add((newParts.Count == 0 ? '\0' : combinator, rewrittenPart));
            }

            foreach (Feature feature in features)
            {
                Selector prefix = Prefix(selector, feature.PrefixLength);
                foreach (UxmlElement element in state.MatchDom(prefix))
                {
                    state.Editor.AddClass(element, feature.ClassName);
                }
            }

            return SelectorFormat.Make(newParts, selector.Span);
        }

        private static Selector Prefix(Selector selector, int length)
        {
            var parts = new List<(char, SimpleSelector)>();
            for (int i = 0; i < length; i++)
            {
                SimpleSelector p = selector.Parts[i].Part;
                parts.Add((selector.Parts[i].Combinator, new SimpleSelector
                {
                    Type = p.Type,
                    Id = p.Id,
                    Classes = p.Classes,
                    Pseudos = p.Pseudos,
                    Attributes = p.Attributes,
                    PseudoElements = new List<string>(),
                    Universal = p.Universal
                }));
            }

            return SelectorFormat.Make(parts, selector.Span);
        }
    }
}
