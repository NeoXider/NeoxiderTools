using System;
using System.Collections.Generic;
using System.Text;
using System.Text.RegularExpressions;
using HtmlToUitk.Core.Html;

namespace HtmlToUitk.Rules
{
    internal static class Dom
    {
        public static IEnumerable<DomElement> Descendants(DomElement root)
        {
            if (root == null) yield break;
            foreach (DomNode child in root.Children)
            {
                var element = child as DomElement;
                if (element == null) continue;
                yield return element;
                foreach (DomElement inner in Descendants(element)) yield return inner;
            }
        }

        public static IEnumerable<DomElement> ChildElements(DomElement element)
        {
            foreach (DomNode child in element.Children)
            {
                var e = child as DomElement;
                if (e != null) yield return e;
            }
        }

        public static string Tag(DomElement e)
        {
            return (e.Tag ?? "").ToLowerInvariant();
        }

        public static DomAttribute Attribute(DomElement e, string name)
        {
            foreach (DomAttribute a in e.Attributes)
            {
                if (string.Equals(a.Name, name, StringComparison.OrdinalIgnoreCase)) return a;
            }
            return null;
        }

        public static bool Has(DomElement e, string name)
        {
            return Attribute(e, name) != null;
        }

        public static string Attr(DomElement e, string name)
        {
            DomAttribute a = Attribute(e, name);
            return a == null ? null : a.Value;
        }

        public static string OwnText(DomElement e)
        {
            var sb = new StringBuilder();
            foreach (DomNode child in e.Children)
            {
                var text = child as DomText;
                if (text != null) sb.Append(text.Text);
            }
            return NormalizeText(sb.ToString());
        }

        public static string NormalizeText(string text)
        {
            return Regex.Replace(text ?? "", @"\s+", " ").Trim();
        }

        public static bool HasLetters(string text)
        {
            if (text == null) return false;
            foreach (char c in text)
            {
                if (char.IsLetter(c)) return true;
            }
            return false;
        }

        public static string UxmlType(DomElement e, DialectSpec dialect, out string problem)
        {
            problem = null;
            string dataUxml = Attr(e, "data-uxml");
            if (!string.IsNullOrEmpty(dataUxml))
            {
                if (dialect.DataUxml.Contains(dataUxml)) return dataUxml;
                problem = "data-uxml";
            }
            string tag = Tag(e);
            if (tag == "input")
            {
                string itype = (Attr(e, "type") ?? "text").ToLowerInvariant();
                string mapped;
                if (dialect.InputTypes.TryGetValue(itype, out mapped)) return mapped;
                problem = problem ?? "input";
                return null;
            }
            string uxml;
            if (dialect.TagToUxml.TryGetValue(tag, out uxml)) return uxml;
            problem = problem ?? "tag";
            return null;
        }

        public static string UxmlTypeOrDefault(DomElement e, DialectSpec dialect)
        {
            string problem;
            return UxmlType(e, dialect, out problem) ?? "VisualElement";
        }

        public static string Directory(string path)
        {
            return PathMath.DirectoryOf(path);
        }

        public static bool IsMocked(DomElement e)
        {
            return e != null;
        }
    }

    internal sealed class PageScope
    {
        public AnalysisPage Page;
        public DomElement Body;
        public string Path;

        public static IEnumerable<PageScope> All(AnalysisInput input)
        {
            if (input.Pages == null) yield break;
            foreach (AnalysisPage page in input.Pages)
            {
                if (page == null || page.Html == null || page.Html.Body == null) continue;
                yield return new PageScope { Page = page, Body = page.Html.Body, Path = page.Path ?? page.Html.Path };
            }
        }

        public IEnumerable<DomElement> Elements()
        {
            return Dom.Descendants(Body);
        }
    }
}
