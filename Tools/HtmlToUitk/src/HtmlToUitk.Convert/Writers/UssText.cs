using System;
using System.Collections.Generic;
using System.Text;
using HtmlToUitk.Convert.Css;
using HtmlToUitk.Core.Css;
using HtmlToUitk.Core.Text;

namespace HtmlToUitk.Convert.Writers
{
    public static class UssText
    {
        public const string GeneratedMarker = "\u0001GEN:";

        public static string FormatRule(IReadOnlyList<string> selectors, IReadOnlyList<KeyValuePair<string, string>> declarations, string indent)
        {
            var sb = new StringBuilder();
            for (int i = 0; i < selectors.Count; i++)
            {
                sb.Append(selectors[i]);
                sb.Append(i < selectors.Count - 1 ? "," : " {");
                sb.Append('\n');
            }

            foreach (KeyValuePair<string, string> d in declarations)
            {
                sb.Append(indent).Append(d.Key).Append(": ").Append(d.Value).Append(";\n");
            }

            sb.Append('}');
            return sb.ToString();
        }

        public static string FormatSheet(Stylesheet sheet, int indentWidth, string ussFolder)
        {
            string indent = new string(' ', Math.Max(0, indentWidth));
            var blocks = new List<string>();
            foreach (StyleRule rule in sheet.Rules)
            {
                var selectors = new List<string>();
                foreach (Selector s in rule.Selectors) selectors.Add(SelectorFormat.Text(s));
                var decls = new List<KeyValuePair<string, string>>();
                foreach (Declaration d in rule.Declarations)
                {
                    string value = d.Value == null ? "" : d.Value.ToText();
                    decls.Add(new KeyValuePair<string, string>(d.Name, ResolveGenerated(value, ussFolder)));
                }

                blocks.Add(FormatRule(selectors, decls, indent));
            }

            if (blocks.Count == 0) return "";
            return string.Join("\n\n", blocks) + "\n";
        }

        public static string ResolveGenerated(string value, string ussFolder)
        {
            int index = value.IndexOf(GeneratedMarker, StringComparison.Ordinal);
            while (index >= 0)
            {
                int end = value.IndexOf('"', index);
                if (end < 0) break;
                string assetPath = value.Substring(index + GeneratedMarker.Length, end - index - GeneratedMarker.Length);
                string relative = PathUtil.RelativeTo(ussFolder ?? "", assetPath);
                value = value.Substring(0, index) + relative + value.Substring(end);
                index = value.IndexOf(GeneratedMarker, StringComparison.Ordinal);
            }

            return value;
        }
    }
}
