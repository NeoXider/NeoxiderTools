using System;
using System.Collections.Generic;
using System.Text;
using HtmlToUitk.Convert.Config;
using HtmlToUitk.Convert.Css;
using HtmlToUitk.Convert.Spec;
using HtmlToUitk.Core.Css;
using HtmlToUitk.Core.Html;
using HtmlToUitk.Core.Uxml;

namespace HtmlToUitk.Convert.Dialect
{
    internal sealed class MapContext
    {
        public ConversionConfig Config;
        public LocTable Loc;
        public TranslateEnv Env;
        public string PageDirectory = "";
    }

    internal sealed class MapResult
    {
        public List<UxmlElement> Roots = new List<UxmlElement>();
        public List<KeyUse> Uses = new List<KeyUse>();
        public List<string> KeyOrder = new List<string>();
        public Dictionary<string, string> Mocks = new Dictionary<string, string>(StringComparer.Ordinal);
        public List<string> NewKeys = new List<string>();
        public List<StyleRule> ExtraRules = new List<StyleRule>();
        public Dictionary<DomElement, UxmlElement> ByDom = new Dictionary<DomElement, UxmlElement>();
    }

    internal static class UxmlMapper
    {
        private const string PassthroughPrefix = "data-uxml-";

        public static string Attr(DomElement element, string name)
        {
            for (int i = 0; i < element.Attributes.Count; i++)
            {
                if (string.Equals(element.Attributes[i].Name, name, StringComparison.OrdinalIgnoreCase)) return element.Attributes[i].Value ?? "";
            }

            return null;
        }

        public static bool HasAttr(DomElement element, string name)
        {
            return Attr(element, name) != null;
        }

        public static string OwnText(DomElement element)
        {
            var sb = new StringBuilder();
            for (int i = 0; i < element.Children.Count; i++)
            {
                DomText text = element.Children[i] as DomText;
                if (text != null) sb.Append(text.Text);
            }

            return CssText.Collapse(sb.ToString());
        }

        public static List<DomElement> Elements(DomElement element)
        {
            var list = new List<DomElement>();
            for (int i = 0; i < element.Children.Count; i++)
            {
                DomElement child = element.Children[i] as DomElement;
                if (child != null) list.Add(child);
            }

            return list;
        }

        public static MapResult Map(HtmlDocument html, MapContext context)
        {
            var result = new MapResult();
            foreach (DomNode node in html.Body.Children)
            {
                DomElement element = node as DomElement;
                if (element == null) continue;
                result.Roots.Add(Build(element, null, context, result));
            }

            return result;
        }

        private static bool HasLetters(string text)
        {
            foreach (char c in text)
            {
                if (char.IsLetter(c)) return true;
            }

            return false;
        }

        private static string DetermineType(DomElement element, DialectSpec dialect)
        {
            string tag = element.Tag.ToLowerInvariant();
            string dataUxml = Attr(element, "data-uxml");
            if (!string.IsNullOrEmpty(dataUxml) && dialect.DataUxml.Contains(dataUxml)) return dataUxml;
            if (tag == "input")
            {
                string itype = (Attr(element, "type") ?? "").ToLowerInvariant();
                if (itype.Length == 0) itype = "text";
                string mapped;
                return dialect.InputTypes.TryGetValue(itype, out mapped) ? mapped : null;
            }

            string uxml;
            return dialect.TagToUxml.TryGetValue(tag, out uxml) ? uxml : null;
        }

        private static UxmlElement Build(DomElement h, UxmlElement parent, MapContext context, MapResult result)
        {
            DialectSpec dialect = DialectSpec.Instance;
            ConversionConfig config = context.Config;
            string tag = h.Tag.ToLowerInvariant();
            string utype = DetermineType(h, dialect) ?? "VisualElement";
            var attrs = new List<KeyValuePair<string, string>>();
            bool hidden = false;
            for (int i = 0; i < h.Attributes.Count; i++)
            {
                string key = h.Attributes[i].Name.ToLowerInvariant();
                string value = h.Attributes[i].Value ?? "";
                if (key == "style")
                {
                    string normalised = CssText.Collapse(value).Replace(" ", "").ToLowerInvariant().TrimEnd(';');
                    if (normalised == "display:none") hidden = true;
                }
            }

            if (HasAttr(h, "hidden")) hidden = true;
            string nameAttr = Attr(h, "name");
            string idAttr = Attr(h, "id");
            string name = string.IsNullOrEmpty(nameAttr) ? (string.IsNullOrEmpty(idAttr) ? null : idAttr) : nameAttr;
            var classes = ClassList.Split(Attr(h, "class"));
            bool inner = dialect.InnerTextTags.Contains(tag);
            string textAttr;
            dialect.TextAttribute.TryGetValue(utype, out textAttr);
            string locKey = Attr(h, "data-loc");
            string mock = inner ? OwnText(h) : (Attr(h, "data-text") ?? "");
            string recordedKey = null;
            string recordedMock = null;
            if (locKey != null)
            {
                locKey = locKey.Trim();
                if (textAttr != null)
                {
                    recordedKey = locKey;
                    recordedMock = mock;
                    attrs.Add(new KeyValuePair<string, string>(textAttr, locKey));
                    AddKey(context, result, locKey, mock, name, textAttr, h.Span);
                }
            }
            else if (mock.Length > 0 && textAttr != null)
            {
                if (!HasLetters(mock) && config.Localization.AllowGlyphOnly)
                {
                    attrs.Add(new KeyValuePair<string, string>(textAttr, mock));
                    recordedMock = mock;
                }
            }

            string tooltip = Attr(h, "data-loc-tooltip");
            if (!string.IsNullOrEmpty(tooltip))
            {
                attrs.Add(new KeyValuePair<string, string>("tooltip", tooltip.Trim()));
                AddKey(context, result, tooltip.Trim(), "", name, "tooltip", h.Span);
            }

            if (tag == "input" || tag == "textarea" || utype == "TextField" || utype == "IntegerField" || utype == "FloatField")
            {
                string ph = Attr(h, "placeholder");
                string phKey = Attr(h, "data-loc-placeholder");
                if (!string.IsNullOrEmpty(phKey))
                {
                    attrs.Add(new KeyValuePair<string, string>("placeholder-text", phKey.Trim()));
                    AddKey(context, result, phKey.Trim(), ph ?? "", name, "placeholder-text", h.Span);
                }
            }

            ApplyValueAttrs(h, tag, utype, attrs);
            string picking = Attr(h, "data-picking");
            if (picking != null && picking.ToLowerInvariant() == "ignore") Set(attrs, "picking-mode", "Ignore");
            if (HasAttr(h, "disabled")) Set(attrs, "enabled", "false");
            if (HasAttr(h, "tabindex")) Set(attrs, "tabindex", Attr(h, "tabindex"));
            for (int i = 0; i < h.Attributes.Count; i++)
            {
                string key = h.Attributes[i].Name.ToLowerInvariant();
                if (key.StartsWith(PassthroughPrefix, StringComparison.Ordinal) && key != "data-uxml")
                {
                    Set(attrs, key.Substring(PassthroughPrefix.Length), h.Attributes[i].Value ?? "");
                }
            }

            if (config.Uxml.InteractiveAttributes != null && dialect.Interactive.Contains(utype))
            {
                foreach (KeyValuePair<string, string> pair in config.Uxml.InteractiveAttributes)
                {
                    if (!Contains(attrs, pair.Key)) attrs.Add(new KeyValuePair<string, string>(pair.Key, pair.Value));
                }
            }

            var element = new UxmlElement { UxmlType = utype, Source = h, Parent = parent, LocKey = recordedKey, MockText = recordedMock };
            result.ByDom[h] = element;
            var children = new List<UxmlElement>();
            if (utype == "Image") HandleImage(h, name, context, result);
            bool skipChildren = false;
            if (tag == "select")
            {
                HandleSelect(h, name, attrs, context, result);
                skipChildren = true;
            }
            else if (dialect.VoidTags.Contains(tag) || tag == "progress" || tag == "textarea") skipChildren = true;
            if (!skipChildren)
            {
                foreach (DomElement child in Elements(h)) children.Add(Build(child, element, context, result));
            }

            element.Children = children;
            element.Attributes = OrderAttributes(name, attrs, classes, hidden);
            return element;
        }

        private static bool Contains(List<KeyValuePair<string, string>> attrs, string key)
        {
            foreach (KeyValuePair<string, string> pair in attrs)
            {
                if (pair.Key == key) return true;
            }

            return false;
        }

        private static void Set(List<KeyValuePair<string, string>> attrs, string key, string value)
        {
            for (int i = 0; i < attrs.Count; i++)
            {
                if (attrs[i].Key == key)
                {
                    attrs[i] = new KeyValuePair<string, string>(key, value);
                    return;
                }
            }

            attrs.Add(new KeyValuePair<string, string>(key, value));
        }

        private static IReadOnlyList<UxmlAttribute> OrderAttributes(string name, List<KeyValuePair<string, string>> attrs, IReadOnlyList<string> classes, bool hidden)
        {
            var all = new List<KeyValuePair<string, string>>();
            if (!string.IsNullOrEmpty(name)) all.Add(new KeyValuePair<string, string>("name", name));
            foreach (KeyValuePair<string, string> pair in attrs)
            {
                if (pair.Key == "name" || pair.Key == "class") continue;
                all.Add(pair);
            }

            if (classes.Count > 0) all.Add(new KeyValuePair<string, string>("class", string.Join(" ", classes)));
            if (hidden) all.Add(new KeyValuePair<string, string>("style", "display: none;"));
            return AttrOrder.Sorted(all);
        }

        private static void ApplyValueAttrs(DomElement h, string tag, string utype, List<KeyValuePair<string, string>> attrs)
        {
            if (utype == "Slider" || utype == "SliderInt" || utype == "MinMaxSlider")
            {
                if (HasAttr(h, "min")) Set(attrs, "low-value", Attr(h, "min"));
                if (HasAttr(h, "max")) Set(attrs, "high-value", Attr(h, "max"));
                if (HasAttr(h, "value")) Set(attrs, "value", Attr(h, "value"));
            }
            else if (utype == "ProgressBar")
            {
                if (HasAttr(h, "value")) Set(attrs, "value", Attr(h, "value"));
                if (HasAttr(h, "max")) Set(attrs, "high-value", Attr(h, "max"));
            }
            else if (utype == "TextField" || utype == "IntegerField" || utype == "FloatField")
            {
                string value = Attr(h, "value");
                if (!string.IsNullOrEmpty(value)) Set(attrs, "value", value);
                if (HasAttr(h, "maxlength")) Set(attrs, "max-length", Attr(h, "maxlength"));
                if (HasAttr(h, "readonly")) Set(attrs, "readonly", "true");
                if (tag == "textarea") Set(attrs, "multiline", "true");
                if ((Attr(h, "type") ?? "").ToLowerInvariant() == "password") Set(attrs, "password", "true");
            }
            else if (utype == "Toggle" || utype == "RadioButton")
            {
                if (HasAttr(h, "checked")) Set(attrs, "value", "true");
            }
        }

        private static void HandleImage(DomElement h, string name, MapContext context, MapResult result)
        {
            string src = Attr(h, "src");
            if (string.IsNullOrEmpty(name) || string.IsNullOrEmpty(src)) return;
            var decl = new Declaration { Name = "background-image", Span = h.Span };
            string url = DeclTranslator.ResolveUrl(context.Env, src, decl);
            if (url == null) return;
            var rule = new StyleRule
            {
                Selectors = new[] { SelectorFormat.ForName(name, h.Span) },
                Declarations = new[]
                {
                    new Declaration { Name = "background-image", Value = new CssRaw(url), Span = h.Span },
                    new Declaration { Name = "-unity-background-scale-mode", Value = new CssRaw("scale-to-fit"), Span = h.Span }
                },
                Span = h.Span,
                Order = 1000000 + result.ExtraRules.Count
            };
            result.ExtraRules.Add(rule);
        }

        private static void HandleSelect(DomElement h, string name, List<KeyValuePair<string, string>> attrs, MapContext context, MapResult result)
        {
            var keys = new List<string>();
            int? selected = null;
            int index = 0;
            foreach (DomElement option in Elements(h))
            {
                if (option.Tag.ToLowerInvariant() != "option") continue;
                string text = OwnText(option);
                string key = Attr(option, "data-loc");
                if (!string.IsNullOrEmpty(key))
                {
                    key = key.Trim();
                    AddKey(context, result, key, text, name, "choices", option.Span);
                    keys.Add(key);
                }
                else if (text.Length > 0) keys.Add(text);
                if (HasAttr(option, "selected")) selected = index;
                index++;
            }

            if (keys.Count > 0) Set(attrs, "choices", string.Join(",", keys));
            if (selected.HasValue) Set(attrs, "index", selected.Value.ToString(System.Globalization.CultureInfo.InvariantCulture));
        }

        private static void AddKey(MapContext context, MapResult result, string key, string mock, string elementName, string attribute, SourceSpan span)
        {
            result.Uses.Add(new KeyUse { Key = key, ElementName = elementName, Attribute = attribute, Span = span });
            string previous;
            if (result.Mocks.TryGetValue(key, out previous))
            {
                if (!string.IsNullOrEmpty(mock) && string.IsNullOrEmpty(previous)) result.Mocks[key] = mock;
                return;
            }

            result.Mocks[key] = mock ?? "";
            result.KeyOrder.Add(key);
            LocTable loc = context.Loc;
            if (loc != null && loc.IsLoaded)
            {
                if (!loc.Has(key)) result.NewKeys.Add(key);
            }
            else result.NewKeys.Add(key);
        }
    }
}
