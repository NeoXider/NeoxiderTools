using System;
using System.Collections.Generic;
using System.Text;

namespace HtmlToUitk.Core.Uxml
{
    public sealed class UxmlWriter : IUxmlWriter
    {
        public static readonly IReadOnlyDictionary<string, string> NamespaceUris = new Dictionary<string, string>(StringComparer.Ordinal)
        {
            { "ui", "UnityEngine.UIElements" },
            { "uie", "UnityEditor.UIElements" }
        };

        public string Write(UiDocument document, UxmlWriterOptions options)
        {
            if (document == null) throw new ArgumentNullException(nameof(document));
            options = options ?? new UxmlWriterOptions();
            string indent = new string(' ', Math.Max(0, options.Indent));
            string prefix = string.IsNullOrEmpty(options.DefaultPrefix) ? "ui" : options.DefaultPrefix;
            var sb = new StringBuilder();
            if (options.XmlDeclaration) sb.Append("<?xml version=\"1.0\" encoding=\"utf-8\"?>\n");
            sb.Append('<').Append(prefix).Append(":UXML");
            foreach (string ns in options.Namespaces)
            {
                string uri;
                if (NamespaceUris.TryGetValue(ns, out uri)) sb.Append(" xmlns:").Append(ns).Append("=\"").Append(uri).Append('"');
            }
            sb.Append(" editor-extension-mode=\"").Append(options.EditorExtensionMode ? "True" : "False").Append("\">\n");
            foreach (TemplateRef template in document.Templates)
            {
                sb.Append(indent).Append('<').Append(prefix).Append(":Template name=\"").Append(EscapeAttribute(template.Name)).Append("\" src=\"").Append(EscapeAttribute(template.Src)).Append("\" />\n");
            }
            foreach (StyleRef style in document.Styles)
            {
                sb.Append(indent).Append("<Style src=\"").Append(EscapeAttribute(style.Src)).Append("\" />\n");
            }
            foreach (UxmlElement root in document.Roots) WriteElement(sb, root, indent, 1, prefix);
            sb.Append("</").Append(prefix).Append(":UXML>\n");
            return sb.ToString();
        }

        private static void WriteElement(StringBuilder sb, UxmlElement element, string indent, int depth, string prefix)
        {
            string pad = Repeat(indent, depth);
            string tag = element.UxmlType.IndexOf(':') >= 0 ? element.UxmlType : prefix + ":" + element.UxmlType;
            sb.Append(pad).Append('<').Append(tag);
            foreach (UxmlAttribute attribute in element.Attributes)
            {
                sb.Append(' ').Append(attribute.Name).Append("=\"").Append(EscapeAttribute(attribute.Value)).Append('"');
            }
            if (element.Children.Count == 0)
            {
                sb.Append(" />\n");
                return;
            }
            sb.Append(">\n");
            foreach (UxmlElement child in element.Children) WriteElement(sb, child, indent, depth + 1, prefix);
            sb.Append(pad).Append("</").Append(tag).Append(">\n");
        }

        private static string Repeat(string text, int count)
        {
            if (count <= 0 || text.Length == 0) return "";
            if (count == 1) return text;
            var sb = new StringBuilder(text.Length * count);
            for (int i = 0; i < count; i++) sb.Append(text);
            return sb.ToString();
        }

        public static string EscapeAttribute(string value)
        {
            if (value == null) return "";
            StringBuilder sb = null;
            for (int i = 0; i < value.Length; i++)
            {
                char c = value[i];
                string replacement = null;
                switch (c)
                {
                    case '&': replacement = "&amp;"; break;
                    case '<': replacement = "&lt;"; break;
                    case '>': replacement = "&gt;"; break;
                    case '"': replacement = "&quot;"; break;
                    case '\n': replacement = "&#10;"; break;
                    case '\r': replacement = "&#13;"; break;
                    case '\t': replacement = "&#9;"; break;
                }
                if (replacement != null)
                {
                    if (sb == null) sb = new StringBuilder(value, 0, i, value.Length + 16);
                    sb.Append(replacement);
                }
                else if (sb != null) sb.Append(c);
            }
            return sb == null ? value : sb.ToString();
        }
    }
}
