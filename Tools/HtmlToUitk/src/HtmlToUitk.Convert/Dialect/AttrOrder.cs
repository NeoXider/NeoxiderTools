using System;
using System.Collections.Generic;
using HtmlToUitk.Core.Uxml;

namespace HtmlToUitk.Convert.Dialect
{
    internal static class AttrOrder
    {
        private static readonly string[] Known =
        {
            "name", "text", "label", "title", "class", "value", "low-value", "high-value", "choices", "index",
            "placeholder-text", "max-length", "multiline", "password", "readonly", "picking-mode", "tooltip",
            "enabled", "focusable", "tabindex"
        };

        public static int Compare(string a, string b)
        {
            if (a == "style" && b == "style") return 0;
            if (a == "style") return 1;
            if (b == "style") return -1;
            int ia = Array.IndexOf(Known, a);
            int ib = Array.IndexOf(Known, b);
            if (ia >= 0 && ib >= 0) return ia.CompareTo(ib);
            if (ia >= 0) return -1;
            if (ib >= 0) return 1;
            return string.CompareOrdinal(a, b);
        }

        public static IReadOnlyList<UxmlAttribute> Sorted(IEnumerable<KeyValuePair<string, string>> attrs)
        {
            var list = new List<KeyValuePair<string, string>>(attrs);
            var indexed = new List<KeyValuePair<int, KeyValuePair<string, string>>>(list.Count);
            for (int i = 0; i < list.Count; i++) indexed.Add(new KeyValuePair<int, KeyValuePair<string, string>>(i, list[i]));
            indexed.Sort((x, y) =>
            {
                int c = Compare(x.Value.Key, y.Value.Key);
                return c != 0 ? c : x.Key.CompareTo(y.Key);
            });
            var result = new List<UxmlAttribute>(indexed.Count);
            foreach (KeyValuePair<int, KeyValuePair<string, string>> item in indexed)
            {
                result.Add(new UxmlAttribute { Name = item.Value.Key, Value = item.Value.Value });
            }

            return result;
        }

        public static IReadOnlyList<UxmlAttribute> With(IReadOnlyList<UxmlAttribute> attributes, string name, string value)
        {
            var list = new List<KeyValuePair<string, string>>(attributes.Count + 1);
            bool replaced = false;
            foreach (UxmlAttribute attr in attributes)
            {
                if (attr.Name == name)
                {
                    list.Add(new KeyValuePair<string, string>(name, value));
                    replaced = true;
                }
                else list.Add(new KeyValuePair<string, string>(attr.Name, attr.Value));
            }

            if (!replaced) list.Add(new KeyValuePair<string, string>(name, value));
            return Sorted(list);
        }

        public static IReadOnlyList<UxmlAttribute> Without(IReadOnlyList<UxmlAttribute> attributes, string name)
        {
            var list = new List<UxmlAttribute>(attributes.Count);
            foreach (UxmlAttribute attr in attributes)
            {
                if (attr.Name != name) list.Add(attr);
            }

            return list;
        }
    }
}
