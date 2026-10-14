using System;
using System.Collections.Generic;

namespace HtmlToUitk.Core.Html
{
    public interface IElement
    {
        string TypeName { get; }
        string Name { get; }
        IReadOnlyList<string> Classes { get; }
        IElement Parent { get; }
        IReadOnlyList<IElement> ChildElements { get; }
        string GetAttribute(string name);
    }

    public static class ClassList
    {
        private static readonly char[] Separators = { ' ', '\t', '\r', '\n', '\f' };

        public static IReadOnlyList<string> Split(string value)
        {
            if (string.IsNullOrEmpty(value)) return Array.Empty<string>();
            string[] parts = value.Split(Separators, StringSplitOptions.RemoveEmptyEntries);
            return parts;
        }
    }

    public abstract class DomNode
    {
        public SourceSpan Span;
        public DomElement Parent;
    }

    public sealed class DomText : DomNode
    {
        public string Text;
    }

    public sealed class DomAttribute
    {
        public string Name;
        public string Value;
        public SourceSpan Span;
    }

    public sealed class DomElement : DomNode, IElement
    {
        public string Tag;
        public IReadOnlyList<DomAttribute> Attributes = Array.Empty<DomAttribute>();
        public IReadOnlyList<DomNode> Children = Array.Empty<DomNode>();

        private IReadOnlyList<DomNode> _childrenSeen;
        private IReadOnlyList<IElement> _childElements;
        private string _classText;
        private IReadOnlyList<string> _classes;

        public string TypeName => Tag;

        public string Name
        {
            get
            {
                string name = GetAttribute("name");
                if (!string.IsNullOrEmpty(name)) return name;
                string id = GetAttribute("id");
                return string.IsNullOrEmpty(id) ? null : id;
            }
        }

        public IReadOnlyList<string> Classes
        {
            get
            {
                string text = GetAttribute("class");
                if (_classes == null || !string.Equals(text, _classText, StringComparison.Ordinal))
                {
                    _classText = text;
                    _classes = ClassList.Split(text);
                }
                return _classes;
            }
        }

        IElement IElement.Parent => Parent;

        public IReadOnlyList<IElement> ChildElements
        {
            get
            {
                if (_childElements == null || !ReferenceEquals(_childrenSeen, Children))
                {
                    var list = new List<IElement>();
                    for (int i = 0; i < Children.Count; i++)
                    {
                        if (Children[i] is DomElement element) list.Add(element);
                    }
                    _childElements = list;
                    _childrenSeen = Children;
                }
                return _childElements;
            }
        }

        public string GetAttribute(string name)
        {
            for (int i = 0; i < Attributes.Count; i++)
            {
                if (string.Equals(Attributes[i].Name, name, StringComparison.Ordinal)) return Attributes[i].Value;
            }
            return null;
        }
    }

    public sealed class StyleBlock
    {
        public string Text;
        public SourceSpan Span;
        public IReadOnlyList<DomAttribute> Attributes = Array.Empty<DomAttribute>();
    }

    public sealed class LinkRef
    {
        public string Rel, Href;
        public IReadOnlyList<DomAttribute> Attributes = Array.Empty<DomAttribute>();
        public SourceSpan Span;
    }

    public enum StyleSourceKind { Link, Style }

    public sealed class StyleSourceRef
    {
        public StyleSourceKind Kind;
        public int Index;
        public SourceSpan Span;
    }

    public sealed class HtmlDocument
    {
        public string Path;
        public DomElement Body;
        public IReadOnlyList<DomAttribute> HtmlAttributes = Array.Empty<DomAttribute>();
        public string Title;
        public IReadOnlyList<StyleBlock> StyleBlocks = Array.Empty<StyleBlock>();
        public IReadOnlyList<LinkRef> Links = Array.Empty<LinkRef>();
        public IReadOnlyList<DomElement> Metas = Array.Empty<DomElement>();
        public IReadOnlyList<DomElement> Scripts = Array.Empty<DomElement>();
        public IReadOnlyList<DomElement> Templates = Array.Empty<DomElement>();
        public IReadOnlyList<StyleSourceRef> StyleOrder = Array.Empty<StyleSourceRef>();
    }

    public interface IHtmlParser
    {
        HtmlDocument Parse(string text, string path, IDiagnosticSink sink);
    }
}
