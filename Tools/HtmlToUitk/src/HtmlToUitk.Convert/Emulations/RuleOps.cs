using System;
using System.Collections.Generic;
using HtmlToUitk.Convert.Css;
using HtmlToUitk.Core.Css;

namespace HtmlToUitk.Convert.Emulations
{
    internal static class RuleOps
    {
        public static string Text(Declaration declaration)
        {
            return declaration.Value == null ? "" : declaration.Value.ToText().Trim();
        }

        public static Declaration Decl(string name, string text, SourceSpan span)
        {
            return new Declaration { Name = name, Value = new CssRaw(text), Span = span };
        }

        public static StyleRule Rule(IEnumerable<Selector> selectors, IEnumerable<Declaration> declarations, SourceSpan span, int order = 0, IReadOnlyList<string> atContext = null)
        {
            return new StyleRule
            {
                Selectors = new List<Selector>(selectors),
                Declarations = new List<Declaration>(declarations),
                Span = span,
                Order = order,
                AtContext = atContext ?? Array.Empty<string>()
            };
        }

        public static StyleRule WithDeclarations(StyleRule rule, IEnumerable<Declaration> declarations)
        {
            return new StyleRule
            {
                Selectors = rule.Selectors,
                Declarations = new List<Declaration>(declarations),
                AtContext = rule.AtContext,
                Span = rule.Span,
                Order = rule.Order
            };
        }

        public static StyleRule WithSelectors(StyleRule rule, IEnumerable<Selector> selectors)
        {
            return new StyleRule
            {
                Selectors = new List<Selector>(selectors),
                Declarations = rule.Declarations,
                AtContext = rule.AtContext,
                Span = rule.Span,
                Order = rule.Order
            };
        }

        public static bool Has(StyleRule rule, string name)
        {
            foreach (Declaration d in rule.Declarations)
            {
                if (d.Name == name) return true;
            }

            return false;
        }

        public static Declaration Find(StyleRule rule, string name)
        {
            Declaration found = null;
            foreach (Declaration d in rule.Declarations)
            {
                if (d.Name == name) found = d;
            }

            return found;
        }

        public static string SelectorsText(StyleRule rule)
        {
            var parts = new List<string>();
            foreach (Selector s in rule.Selectors) parts.Add(SelectorFormat.Text(s));
            return string.Join(", ", parts);
        }

        public static void MapRules(SheetBuilder sheets, Func<string, StyleRule, IEnumerable<StyleRule>> map)
        {
            var ids = new List<string>(sheets.SheetIds);
            foreach (string id in ids)
            {
                var output = new List<StyleRule>();
                foreach (StyleRule rule in sheets.Rules(id))
                {
                    IEnumerable<StyleRule> replacement = map(id, rule);
                    if (replacement == null) output.Add(rule);
                    else output.AddRange(replacement);
                }

                sheets.ReplaceRules(id, output);
            }
        }

        public static IEnumerable<KeyValuePair<string, StyleRule>> All(SheetBuilder sheets)
        {
            var ids = new List<string>(sheets.SheetIds);
            foreach (string id in ids)
            {
                foreach (StyleRule rule in new List<StyleRule>(sheets.Rules(id))) yield return new KeyValuePair<string, StyleRule>(id, rule);
            }
        }

        public static bool IsBlockingAtContext(StyleRule rule)
        {
            return rule.AtContext != null && rule.AtContext.Count > 0;
        }

        public static bool Same(StyleRule a, StyleRule b)
        {
            if (ReferenceEquals(a, b)) return true;
            if (a.Span.IsNone || b.Span.IsNone) return false;
            return a.Span.Equals(b.Span) && SelectorsText(a) == SelectorsText(b);
        }

        public static bool InsertAfter(SheetBuilder sheets, StyleRule anchor, IReadOnlyList<StyleRule> generated, string fallbackSheet)
        {
            var ids = new List<string>(sheets.SheetIds);
            foreach (string id in ids)
            {
                var list = new List<StyleRule>(sheets.Rules(id));
                for (int i = 0; i < list.Count; i++)
                {
                    if (Same(list[i], anchor))
                    {
                        list.InsertRange(i + 1, generated);
                        sheets.ReplaceRules(id, list);
                        return true;
                    }
                }
            }

            if (fallbackSheet != null)
            {
                var list = new List<StyleRule>(sheets.Rules(fallbackSheet));
                list.AddRange(generated);
                sheets.ReplaceRules(fallbackSheet, list);
            }

            return false;
        }

        public static void StripDeclarations(SheetBuilder sheets, StyleRule anchor, Func<Declaration, bool> predicate)
        {
            var ids = new List<string>(sheets.SheetIds);
            foreach (string id in ids)
            {
                var list = new List<StyleRule>(sheets.Rules(id));
                bool changed = false;
                for (int i = 0; i < list.Count; i++)
                {
                    if (!Same(list[i], anchor)) continue;
                    var kept = new List<Declaration>();
                    foreach (Declaration d in list[i].Declarations)
                    {
                        if (!predicate(d)) kept.Add(d);
                    }

                    if (kept.Count != list[i].Declarations.Count)
                    {
                        list[i] = WithDeclarations(list[i], kept);
                        changed = true;
                    }
                }

                if (changed) sheets.ReplaceRules(id, list);
            }
        }
    }
}
