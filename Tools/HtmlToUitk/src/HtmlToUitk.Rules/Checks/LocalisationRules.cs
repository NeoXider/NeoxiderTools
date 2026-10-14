using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using HtmlToUitk.Convert;
using HtmlToUitk.Convert.Config;
using HtmlToUitk.Core.Html;
using HtmlToUitk.Core.Json;

namespace HtmlToUitk.Rules
{
    public sealed class StringTable
    {
        private readonly Dictionary<string, Dictionary<string, string>> _locales = new Dictionary<string, Dictionary<string, string>>(StringComparer.Ordinal);

        public bool HasSources { get; private set; }

        public void MarkSource()
        {
            HasSources = true;
        }

        public void Merge(string locale, IEnumerable<KeyValuePair<string, string>> entries)
        {
            Dictionary<string, string> target;
            if (!_locales.TryGetValue(locale, out target))
            {
                target = new Dictionary<string, string>(StringComparer.Ordinal);
                _locales[locale] = target;
            }
            foreach (KeyValuePair<string, string> entry in entries) target[entry.Key] = entry.Value;
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
            Dictionary<string, string> preferred;
            string text;
            if (locale != null && _locales.TryGetValue(locale, out preferred) && preferred.TryGetValue(key, out text)) return text;
            var names = new List<string>(_locales.Keys);
            names.Sort(StringComparer.Ordinal);
            foreach (string name in names)
            {
                if (_locales[name].TryGetValue(key, out text)) return text;
            }
            return null;
        }

        public IReadOnlyList<string> Keys()
        {
            var all = new HashSet<string>(StringComparer.Ordinal);
            foreach (Dictionary<string, string> table in _locales.Values)
            {
                foreach (string key in table.Keys) all.Add(key);
            }
            var list = new List<string>(all);
            list.Sort(StringComparer.Ordinal);
            return list;
        }

        public static StringTable Load(IFileSystem fs, IEnumerable<string> paths, string format)
        {
            var table = new StringTable();
            foreach (string path in paths)
            {
                if (string.IsNullOrEmpty(path) || !fs.FileExists(path)) continue;
                JsonObject root = JsonReader.Parse(fs.ReadAllText(path)).AsObject();
                if (root == null) continue;
                table.MarkSource();
                if (format == "flat")
                {
                    string locale = System.IO.Path.GetFileNameWithoutExtension(path);
                    table.Merge(locale, Strings(root));
                    continue;
                }
                foreach (KeyValuePair<string, JsonValue> member in root.Members)
                {
                    JsonObject map = member.Value.AsObject();
                    if (member.Key == "version" || map == null) continue;
                    table.Merge(member.Key, Strings(map));
                }
            }
            return table;
        }

        private static IEnumerable<KeyValuePair<string, string>> Strings(JsonObject map)
        {
            foreach (KeyValuePair<string, JsonValue> member in map.Members)
            {
                string text = member.Value.AsString();
                if (text != null) yield return new KeyValuePair<string, string>(member.Key, text);
            }
        }
    }

    public interface IStringTables
    {
        StringTable For(AnalysisPage page, ConversionConfig config);
    }

    public sealed class ConfigStringTables : IStringTables
    {
        private readonly IFileSystem _fs;
        private readonly string _baseDirectory;
        private readonly Dictionary<string, StringTable> _cache = new Dictionary<string, StringTable>(StringComparer.Ordinal);

        public ConfigStringTables(IFileSystem fs, string baseDirectory)
        {
            _fs = fs ?? throw new ArgumentNullException(nameof(fs));
            _baseDirectory = baseDirectory ?? "";
        }

        public StringTable For(AnalysisPage page, ConversionConfig config)
        {
            string screen = page.Document != null && !string.IsNullOrEmpty(page.Document.Name) ? page.Document.Name : StemOf(page.Path);
            var paths = new List<string>();
            foreach (string template in config.Localization.Files)
            {
                string filled = template.Replace("{screen}", screen).Replace("{skin}", config.Layout.Skin).Replace("{screens}", config.Layout.Screens).Replace("{strings}", config.Layout.Strings);
                paths.Add(PathMath.Combine(_baseDirectory, filled));
            }
            if (page.Html != null)
            {
                foreach (DomElement meta in page.Html.Metas)
                {
                    if (Dom.Attr(meta, "name") == "uitk:strings" && !string.IsNullOrEmpty(Dom.Attr(meta, "content")))
                    {
                        paths.Add(PathMath.Combine(PathMath.DirectoryOf(page.Path ?? ""), Dom.Attr(meta, "content")));
                    }
                }
            }
            string cacheKey = string.Join("|", paths) + "#" + config.Localization.Format;
            StringTable table;
            if (!_cache.TryGetValue(cacheKey, out table))
            {
                table = StringTable.Load(_fs, paths, config.Localization.Format);
                _cache[cacheKey] = table;
            }
            return table;
        }

        private static string StemOf(string path)
        {
            if (string.IsNullOrEmpty(path)) return "";
            string name = path.Replace('\\', '/');
            int slash = name.LastIndexOf('/');
            if (slash >= 0) name = name.Substring(slash + 1);
            int dot = name.LastIndexOf('.');
            return dot > 0 ? name.Substring(0, dot) : name;
        }
    }

    public sealed class LocalisationRules : IRule
    {
        private readonly IStringTables _tables;

        public LocalisationRules(IStringTables tables = null)
        {
            _tables = tables;
        }

        public string Id => "localisation";

        public void Evaluate(RuleContext context)
        {
            var r = new RuleReporter(context.Catalog, context.Config, context.Sink, context.Capabilities);
            DialectSpec dialect = DialectSpec.Embedded;
            var loc = context.Config.Localization;
            var keyPattern = new Regex(loc.KeyPattern, RegexOptions.CultureInvariant);
            foreach (PageScope scope in PageScope.All(context.Input))
            {
                StringTable table = _tables == null ? null : _tables.For(scope.Page, context.Config);
                var state = new PageState(r, context, keyPattern, table, loc);
                foreach (DomElement e in Elements(scope.Body))
                {
                    CheckElement(state, dialect, e, loc);
                }
            }
        }

        private static IEnumerable<DomElement> Elements(DomElement root)
        {
            foreach (DomElement child in Dom.ChildElements(root))
            {
                yield return child;
                string tag = Dom.Tag(child);
                if (tag == "select" || tag == "progress" || tag == "textarea") continue;
                foreach (DomElement inner in Elements(child)) yield return inner;
            }
        }

        private sealed class PageState
        {
            public readonly RuleReporter R;
            public readonly RuleContext Context;
            public readonly Regex KeyPattern;
            public readonly StringTable Table;
            public readonly LocalizationConfig Loc;
            public readonly Dictionary<string, string> Keys = new Dictionary<string, string>(StringComparer.Ordinal);

            public PageState(RuleReporter r, RuleContext context, Regex pattern, StringTable table, LocalizationConfig loc)
            {
                R = r;
                Context = context;
                KeyPattern = pattern;
                Table = table;
                Loc = loc;
            }
        }

        private static void AddKey(PageState s, string key, string mock, DomElement e, string construct)
        {
            if (!s.KeyPattern.IsMatch(key))
            {
                s.R.Report("L002", e.Span, construct, "key '" + key + "' does not match " + s.Loc.KeyPattern);
            }
            string previous;
            if (s.Keys.TryGetValue(key, out previous))
            {
                if (!string.IsNullOrEmpty(mock) && !string.IsNullOrEmpty(previous) && Dom.NormalizeText(mock) != Dom.NormalizeText(previous))
                {
                    s.R.Report("L005", e.Span, construct, "key '" + key + "' has two mock texts: '" + previous + "' and '" + mock + "'");
                }
                else if (!string.IsNullOrEmpty(mock) && string.IsNullOrEmpty(previous))
                {
                    s.Keys[key] = mock;
                }
                return;
            }
            s.Keys[key] = mock ?? "";
            if (s.Table != null)
            {
                if (!s.Table.HasSources) return;
                if (s.Table.Has(key))
                {
                    string tableText = s.Table.Get(key, s.Loc.MockLocale);
                    if (!string.IsNullOrEmpty(mock) && tableText != null && Dom.NormalizeText(mock) != Dom.NormalizeText(tableText))
                    {
                        s.R.Report("L004", e.Span, construct, "mock text '" + mock + "' differs from the table text '" + tableText + "' (" + s.Loc.MockLocale + ")");
                    }
                }
                else
                {
                    s.R.Report("L003", e.Span, construct, "key '" + key + "' is not in the string table");
                }
                return;
            }
            AnalysisInput input = s.Context.Input;
            if (input.KnownKeys != null && input.KnownKeys.Count > 0 && input.Keys != null && ListContains(input.Keys.NewKeys, key))
            {
                s.R.Report("L003", e.Span, construct, "key '" + key + "' is not in the string table");
            }
        }

        private static bool ListContains(IReadOnlyList<string> list, string value)
        {
            foreach (string item in list)
            {
                if (string.Equals(item, value, StringComparison.Ordinal)) return true;
            }
            return false;
        }

        private static void CheckElement(PageState s, DialectSpec dialect, DomElement e, LocalizationConfig loc)
        {
            RuleReporter r = s.R;
            string tag = Dom.Tag(e);
            string utype = Dom.UxmlTypeOrDefault(e, dialect);
            bool inner = dialect.InnerTextTags.Contains(tag);
            string textAttr;
            dialect.TextAttribute.TryGetValue(utype, out textAttr);
            string locKey = Dom.Attr(e, "data-loc");
            string dataText = Dom.Attr(e, "data-text");
            string mock = inner ? Dom.OwnText(e) : (dataText ?? "");

            if (tag == "select")
            {
                CheckSelect(s, e, loc);
            }
            else if (locKey != null)
            {
                locKey = locKey.Trim();
                if (textAttr == null)
                {
                    r.Report("L006", e.Span, "data-loc=\"" + locKey + "\"", "<" + tag + "> (" + utype + ") has no text property");
                }
                else
                {
                    AddKey(s, locKey, mock, e, "data-loc=\"" + locKey + "\"");
                    if (!inner && string.IsNullOrEmpty(dataText))
                    {
                        r.Report("L007", e.Span, "<" + tag + " data-loc=\"" + locKey + "\">", "<" + tag + "> has data-loc but no data-text for the mock");
                    }
                }
            }
            else if (!string.IsNullOrEmpty(mock) && textAttr != null)
            {
                if (Dom.HasLetters(mock) || !loc.AllowGlyphOnly)
                {
                    r.Report("L001", e.Span, "\"" + Truncate(mock, 50) + "\"", "visible text \"" + Truncate(mock, 50) + "\" has no data-loc key");
                }
            }
            else if (!string.IsNullOrEmpty(mock) && textAttr == null && inner)
            {
                r.Report("L001", e.Span, "\"" + Truncate(mock, 50) + "\"", "visible text \"" + Truncate(mock, 50) + "\" has no data-loc key");
            }

            string tooltip = Dom.Attr(e, "data-loc-tooltip");
            if (!string.IsNullOrEmpty(tooltip)) AddKey(s, tooltip.Trim(), "", e, "data-loc-tooltip=\"" + tooltip.Trim() + "\"");
            string title = Dom.Attr(e, "title");
            if (title != null && title.Trim().Length > 0)
            {
                r.Report("L001", e.Span, "title=\"" + Truncate(title, 40) + "\"", "title attribute is raw visible text; use data-loc-tooltip");
            }
            if (tag == "input" || tag == "textarea" || utype == "TextField" || utype == "IntegerField" || utype == "FloatField")
            {
                string placeholder = Dom.Attr(e, "placeholder");
                string key = Dom.Attr(e, "data-loc-placeholder");
                if (!string.IsNullOrEmpty(key))
                {
                    AddKey(s, key.Trim(), placeholder ?? "", e, "data-loc-placeholder=\"" + key.Trim() + "\"");
                }
                else if (placeholder != null && placeholder.Trim().Length > 0)
                {
                    r.Report("L001", e.Span, "placeholder=\"" + Truncate(placeholder, 40) + "\"", "placeholder \"" + Truncate(placeholder, 40) + "\" has no data-loc-placeholder key");
                }
            }
        }

        private static void CheckSelect(PageState s, DomElement select, LocalizationConfig loc)
        {
            foreach (DomElement option in Dom.ChildElements(select))
            {
                if (Dom.Tag(option) != "option") continue;
                string text = Dom.OwnText(option);
                string key = Dom.Attr(option, "data-loc");
                if (!string.IsNullOrEmpty(key))
                {
                    AddKey(s, key.Trim(), text, option, "data-loc=\"" + key.Trim() + "\"");
                }
                else if (!string.IsNullOrEmpty(text) && (Dom.HasLetters(text) || !loc.AllowGlyphOnly))
                {
                    s.R.Report("L001", option.Span, "\"" + Truncate(text, 50) + "\"", "option text \"" + Truncate(text, 50) + "\" has no data-loc key");
                }
            }
        }

        private static string Truncate(string text, int length)
        {
            return text.Length <= length ? text : text.Substring(0, length);
        }
    }
}
