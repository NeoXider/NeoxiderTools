using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using HtmlToUitk.Convert.Css;
using HtmlToUitk.Convert.Dialect;
using HtmlToUitk.Core.Css;
using HtmlToUitk.Core.Html;
using HtmlToUitk.Core.Uxml;
using HtmlToUitk.Rules;

namespace HtmlToUitk.Convert.Emulations
{
    public sealed class PseudoElementsEmulation : EmulationBase
    {
        private static readonly Regex AttrRe = new Regex(@"^attr\(\s*([A-Za-z0-9_-]+)\s*\)$", RegexOptions.CultureInvariant);

        private static readonly HashSet<string> NonContainers = new HashSet<string>(StringComparer.Ordinal)
        {
            "Label", "Button", "TextField", "IntegerField", "FloatField", "Toggle", "RadioButton", "Slider", "SliderInt", "MinMaxSlider",
            "DropdownField", "ProgressBar", "Image", "TextElement", "Scroller"
        };

        public override string Id => "pseudo-elements";

        public override Tier Tier => Tier.ConvertTime;

        public override IReadOnlyList<string> Handles => new[] { "pseudo-element:before", "pseudo-element:after" };

        public override int Order => 60;

        internal override void Run(TransformState state, Construct construct, EmulationOutcome outcome)
        {
            var names = new NameGenerator();
            RuleOps.MapRules(state.Sheets, (sheetId, rule) =>
            {
                bool any = false;
                foreach (Selector selector in rule.Selectors)
                {
                    if (PseudoKind(selector) != null) any = true;
                }

                if (!any) return null;
                var output = new List<StyleRule>();
                var plain = new List<Selector>();
                foreach (Selector selector in rule.Selectors)
                {
                    string kind = PseudoKind(selector);
                    if (kind == null)
                    {
                        plain.Add(selector);
                        continue;
                    }

                    StyleRule generated = Materialise(state, names, rule, selector, kind);
                    if (generated != null) output.Add(generated);
                }

                if (plain.Count > 0) output.Insert(0, RuleOps.WithSelectors(rule, plain));
                return output;
            });
        }

        private static string PseudoKind(Selector selector)
        {
            if (selector.Parts.Count == 0) return null;
            SimpleSelector last = selector.Parts[selector.Parts.Count - 1].Part;
            foreach (string name in last.PseudoElements)
            {
                if (name == "before" || name == "after") return name;
            }

            return null;
        }

        private static Selector WithoutPseudoElement(Selector selector)
        {
            var parts = new List<(char, SimpleSelector)>();
            for (int i = 0; i < selector.Parts.Count; i++)
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

        private static string Unquote(Declaration declaration)
        {
            CssString str = declaration.Value as CssString;
            if (str != null) return str.Value;
            string text = RuleOps.Text(declaration);
            if (text.Length >= 2 && (text[0] == '"' || text[0] == '\'') && text[text.Length - 1] == text[0]) return text.Substring(1, text.Length - 2);
            return null;
        }

        private static bool HasLetters(string text)
        {
            foreach (char c in text)
            {
                if (char.IsLetter(c)) return true;
            }

            return false;
        }

        private static StyleRule Materialise(TransformState state, NameGenerator names, StyleRule rule, Selector selector, string kind)
        {
            string key = SelectorFormat.Text(selector);
            string className = names.Generate(kind, key);
            Declaration content = RuleOps.Find(rule, "content");
            var remaining = new List<Declaration>();
            foreach (Declaration d in rule.Declarations)
            {
                if (d.Name != "content") remaining.Add(d);
            }

            if (content == null) return null;
            string contentText = RuleOps.Text(content);
            string lower = contentText.ToLowerInvariant();
            if (lower == "none" || lower == "normal") return null;
            IReadOnlyList<UxmlElement> targets = state.MatchUxml(WithoutPseudoElement(selector));
            bool created = false;
            foreach (UxmlElement target in targets)
            {
                if (NonContainers.Contains(target.UxmlType))
                {
                    state.Report("E001", Severity.Error, Tier.Reject, content.Span, "::" + kind + " on " + target.UxmlType, "the pseudo-element target is not a container", "Add a real child element to the markup.", "pseudo-elements");
                    continue;
                }

                bool exists = false;
                foreach (UxmlElement child in target.Children)
                {
                    if (TransformState.HasClass(child, className)) exists = true;
                }

                if (exists)
                {
                    created = true;
                    continue;
                }

                UxmlElement element = BuildChild(state, target, kind, className, content, contentText);
                if (element == null) continue;
                if (kind == "before") state.Editor.InsertChild(target, 0, element);
                else state.Editor.AppendChild(target, element);
                created = true;
            }

            if (!created) return null;
            return RuleOps.Rule(new[] { SelectorFormat.ForClass(className, selector.Span) }, remaining, rule.Span, rule.Order);
        }

        private static UxmlElement BuildChild(TransformState state, UxmlElement target, string kind, string className, Declaration content, string contentText)
        {
            string quoted = Unquote(content);
            if (quoted != null)
            {
                if (quoted.Length == 0)
                {
                    return ElementEditor.Create("VisualElement", "class", className, "picking-mode", "Ignore");
                }

                if (HasLetters(quoted) || !state.Config.Localization.AllowGlyphOnly)
                {
                    state.Report("L001", Severity.Error, Tier.Reject, content.Span, contentText, "visible text in content needs a localisation key", "Use content: attr(data-text-" + kind + ") with data-loc-" + kind + ".", "pseudo-elements");
                    return null;
                }

                return ElementEditor.Create("Label", "text", quoted, "class", className, "picking-mode", "Ignore");
            }

            Match attr = AttrRe.Match(contentText);
            if (attr.Success && target.Source != null)
            {
                string attributeName = attr.Groups[1].Value;
                string key = UxmlMapper.Attr(target.Source, "data-loc-" + kind);
                string mock = UxmlMapper.Attr(target.Source, attributeName) ?? "";
                if (!string.IsNullOrEmpty(key))
                {
                    state.AddKey(key.Trim(), mock, target.Name, "text", content.Span);
                    return ElementEditor.Create("Label", "text", key.Trim(), "class", className, "picking-mode", "Ignore");
                }

                state.Report("L001", Severity.Error, Tier.Reject, content.Span, contentText, "content: attr() needs a data-loc-" + kind + " key", "Add data-loc-" + kind + " to the element.", "pseudo-elements");
                return null;
            }

            return null;
        }
    }
}
