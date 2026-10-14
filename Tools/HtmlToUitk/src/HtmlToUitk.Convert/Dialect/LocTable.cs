using System;
using System.Collections.Generic;
using HtmlToUitk.Core.Json;
using HtmlToUitk.Core.Text;

namespace HtmlToUitk.Convert.Dialect
{
    public sealed class LocTable
    {
        private readonly Dictionary<string, Dictionary<string, string>> _locales = new Dictionary<string, Dictionary<string, string>>(StringComparer.Ordinal);
        private readonly List<string> _sources = new List<string>();

        public IReadOnlyList<string> Sources => _sources;

        public bool IsLoaded => _sources.Count > 0;

        public void Merge(string locale, IEnumerable<KeyValuePair<string, string>> mapping)
        {
            Dictionary<string, string> table;
            if (!_locales.TryGetValue(locale, out table))
            {
                table = new Dictionary<string, string>(StringComparer.Ordinal);
                _locales[locale] = table;
            }

            foreach (KeyValuePair<string, string> pair in mapping) table[pair.Key] = pair.Value;
        }

        public bool Has(string key)
        {
            foreach (Dictionary<string, string> table in _locales.Values)
            {
                if (table.ContainsKey(key)) return true;
            }

            return false;
        }

        public string Get(string key, string locale)
        {
            Dictionary<string, string> table;
            string value;
            if (locale != null && _locales.TryGetValue(locale, out table) && table.TryGetValue(key, out value)) return value;
            var names = new List<string>(_locales.Keys);
            names.Sort(StringComparer.Ordinal);
            foreach (string name in names)
            {
                if (_locales[name].TryGetValue(key, out value)) return value;
            }

            return null;
        }

        public IReadOnlyList<string> AllKeys()
        {
            var set = new HashSet<string>(StringComparer.Ordinal);
            foreach (Dictionary<string, string> table in _locales.Values)
            {
                foreach (string key in table.Keys) set.Add(key);
            }

            var list = new List<string>(set);
            list.Sort(StringComparer.Ordinal);
            return list;
        }

        public void LoadFile(string path, string text, string format, string localeHint)
        {
            JsonValue parsed = JsonReader.Parse(text);
            JsonObject root = parsed as JsonObject;
            if (root == null) return;
            _sources.Add(path);
            if (format == "flat")
            {
                string locale = localeHint ?? PathUtil.Stem(path);
                Merge(locale, Strings(root));
                return;
            }

            foreach (KeyValuePair<string, JsonValue> member in root.Members)
            {
                if (member.Key == "version") continue;
                JsonObject mapping = member.Value as JsonObject;
                if (mapping == null) continue;
                Merge(member.Key, Strings(mapping));
            }
        }

        private static IEnumerable<KeyValuePair<string, string>> Strings(JsonObject obj)
        {
            foreach (KeyValuePair<string, JsonValue> member in obj.Members)
            {
                string text = member.Value.AsString();
                if (text != null) yield return new KeyValuePair<string, string>(member.Key, text);
            }
        }
    }
}
