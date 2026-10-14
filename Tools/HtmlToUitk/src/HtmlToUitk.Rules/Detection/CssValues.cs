using System;
using System.Collections.Generic;
using HtmlToUitk.Core.Css;

namespace HtmlToUitk.Rules
{
    public static class CssValues
    {
        public static IReadOnlyList<CssValue> SpaceTokens(CssValue value)
        {
            if (value == null) return Array.Empty<CssValue>();
            var list = value as CssList;
            if (list != null && list.Separator != ',') return list.Items;
            return new[] { value };
        }

        public static IReadOnlyList<CssValue> CommaItems(CssValue value)
        {
            if (value == null) return Array.Empty<CssValue>();
            var list = value as CssList;
            if (list != null && list.Separator == ',') return list.Items;
            return new[] { value };
        }

        public static IEnumerable<CssValue> Descend(CssValue value)
        {
            if (value == null) yield break;
            yield return value;
            var function = value as CssFunction;
            if (function != null)
            {
                foreach (CssValue arg in function.Args)
                {
                    foreach (CssValue inner in Descend(arg)) yield return inner;
                }
                yield break;
            }
            var list = value as CssList;
            if (list != null)
            {
                foreach (CssValue item in list.Items)
                {
                    foreach (CssValue inner in Descend(item)) yield return inner;
                }
                yield break;
            }
            var reference = value as CssVarRef;
            if (reference != null && reference.Fallback != null)
            {
                foreach (CssValue inner in Descend(reference.Fallback)) yield return inner;
            }
        }

        public static bool ContainsVar(CssValue value)
        {
            foreach (CssValue inner in Descend(value))
            {
                if (inner is CssVarRef) return true;
                var raw = inner as CssRaw;
                if (raw != null && raw.Text != null && raw.Text.IndexOf("var(", StringComparison.OrdinalIgnoreCase) >= 0) return true;
            }
            return false;
        }

        public static string Keyword(CssValue value)
        {
            var ident = value as CssIdent;
            if (ident != null) return ident.Name.ToLowerInvariant();
            var raw = value as CssRaw;
            if (raw != null && raw.Text != null)
            {
                string t = raw.Text.Trim();
                if (t.Length > 0 && IsIdentText(t)) return t.ToLowerInvariant();
            }
            return null;
        }

        public static bool TryDimension(CssValue value, out double number, out string unit)
        {
            var n = value as CssNumber;
            if (n != null) { number = n.Value; unit = ""; return true; }
            var d = value as CssDimension;
            if (d != null) { number = d.Value; unit = d.Unit.ToLowerInvariant(); return true; }
            var p = value as CssPercent;
            if (p != null) { number = p.Value; unit = "%"; return true; }
            number = 0;
            unit = null;
            return false;
        }

        public static string FunctionName(CssValue value)
        {
            var function = value as CssFunction;
            return function == null ? null : function.Name.ToLowerInvariant();
        }

        public static string Text(CssValue value)
        {
            return value == null ? "" : value.ToText();
        }

        public static bool IsIdentText(string text)
        {
            if (string.IsNullOrEmpty(text)) return false;
            for (int i = 0; i < text.Length; i++)
            {
                char c = text[i];
                bool ok = (c >= 'a' && c <= 'z') || (c >= 'A' && c <= 'Z') || c == '-' || c == '_' || (i > 0 && c >= '0' && c <= '9');
                if (!ok) return false;
            }
            return true;
        }
    }
}
