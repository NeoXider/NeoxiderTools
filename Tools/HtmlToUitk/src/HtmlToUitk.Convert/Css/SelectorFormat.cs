using System;
using System.Collections.Generic;
using System.Text;
using HtmlToUitk.Core.Css;

namespace HtmlToUitk.Convert.Css
{
    internal static class SelectorFormat
    {
        public static string Compound(SimpleSelector part)
        {
            return SelectorParser.FormatCompound(part);
        }

        public static string Text(Selector selector)
        {
            if (selector.Parts == null || selector.Parts.Count == 0) return selector.Text ?? "";
            return SelectorParser.Format(selector);
        }

        public static Selector Make(IReadOnlyList<(char Combinator, SimpleSelector Part)> parts, SourceSpan span)
        {
            int ids = 0, classes = 0, types = 0;
            for (int i = 0; i < parts.Count; i++)
            {
                SimpleSelector p = parts[i].Part;
                if (!string.IsNullOrEmpty(p.Id)) ids++;
                classes += p.Classes.Count + p.Pseudos.Count + p.Attributes.Count;
                if (!string.IsNullOrEmpty(p.Type)) types++;
            }

            var selector = new Selector
            {
                Parts = parts,
                Specificity = new Specificity(ids, classes, types),
                Span = span
            };
            selector.Text = Text(selector);
            return selector;
        }

        public static Selector ForClass(string className, SourceSpan span)
        {
            var part = new SimpleSelector { Classes = new[] { className } };
            return Make(new[] { ('\0', part) }, span);
        }

        public static Selector ForName(string name, SourceSpan span)
        {
            var part = new SimpleSelector { Id = name };
            return Make(new[] { ('\0', part) }, span);
        }

        public static Selector Parse(string text, SourceSpan span)
        {
            Selector parsed = SelectorParser.Parse(text);
            if (parsed == null) return Make(new List<(char, SimpleSelector)>(), span);
            parsed.Span = span;
            return parsed;
        }
    }
}
