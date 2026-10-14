using System;
using System.Collections.Generic;
using HtmlToUitk.Core.Html;

namespace HtmlToUitk.Core.Uxml
{
    public sealed class UxmlAttribute
    {
        public string Name;
        public string Value;
    }

    public sealed class UxmlElement : IElement
    {
        public string UxmlType;
        public IReadOnlyList<UxmlAttribute> Attributes = Array.Empty<UxmlAttribute>();
        public IReadOnlyList<UxmlElement> Children = Array.Empty<UxmlElement>();
        public string LocKey, MockText;
        public DomElement Source;
        public string GeneratedBy;
        public UxmlElement Parent;

        public string TypeName => UxmlType;

        public string Name => GetAttribute("name");

        public IReadOnlyList<string> Classes => ClassList.Split(GetAttribute("class"));

        IElement IElement.Parent => Parent;

        public IReadOnlyList<IElement> ChildElements => Children;

        public string GetAttribute(string name)
        {
            for (int i = 0; i < Attributes.Count; i++)
            {
                if (string.Equals(Attributes[i].Name, name, StringComparison.Ordinal)) return Attributes[i].Value;
            }
            return null;
        }
    }

    public sealed class StyleRef
    {
        public string Src;
        public SourceSpan Span;
    }

    public sealed class TemplateRef
    {
        public string Name;
        public string Src;
        public SourceSpan Span;
    }

    public sealed class PageDef
    {
        public string Id;
        public string ElementName;
        public bool IsStart;
        public IReadOnlyDictionary<string, string> Data = Empty.Strings;
        public SourceSpan Span;
    }

    public sealed class UiDocument
    {
        public string Name;
        public string SourcePath;
        public IReadOnlyList<UxmlElement> Roots = Array.Empty<UxmlElement>();
        public IReadOnlyList<StyleRef> Styles = Array.Empty<StyleRef>();
        public IReadOnlyList<TemplateRef> Templates = Array.Empty<TemplateRef>();
        public IReadOnlyList<PageDef> Pages = Array.Empty<PageDef>();
    }

    public sealed class UxmlWriterOptions
    {
        public int Indent = 4;
        public IReadOnlyList<string> Namespaces = new[] { "ui", "uie" };
        public string DefaultPrefix = "ui";
        public bool EditorExtensionMode;
        public bool XmlDeclaration = true;
    }

    public interface IUxmlWriter
    {
        string Write(UiDocument document, UxmlWriterOptions options);
    }

    public interface IUxmlReader
    {
        UiDocument Read(string text, string path, IDiagnosticSink sink);
    }
}
