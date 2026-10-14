using System;
using System.Collections.Generic;
using HtmlToUitk.Core.Css;
using HtmlToUitk.Core.Html;

namespace HtmlToUitk.Core.Style
{
    public sealed class ResolvedValue
    {
        public CssValue Value;
        public Declaration Source;
        public StyleRule Rule;
        public Stylesheet Sheet;
        public bool Inherited;
    }

    public sealed class ResolvedStyle
    {
        public IReadOnlyDictionary<string, ResolvedValue> Properties = new Dictionary<string, ResolvedValue>(0);
    }

    [Flags]
    public enum PseudoStateSet
    {
        None = 0,
        Hover = 1,
        Active = 2,
        Focus = 4,
        Disabled = 8,
        Checked = 16,
        Root = 32
    }

    public sealed class StyleConflict
    {
        public string SelectorText;
        public string Property;
        public SourceSpan First;
        public SourceSpan Second;
        public string FirstValue;
        public string SecondValue;
        public Stylesheet FirstSheet;
        public Stylesheet SecondSheet;
    }

    public sealed class StyleOptions
    {
        public IReadOnlyCollection<string> InheritedProperties;
        public bool DetectConflicts = true;
        public int MaxVarDepth = 32;
    }

    public interface IStyleModel
    {
        IReadOnlyList<Stylesheet> Cascade { get; }
        ResolvedStyle Resolve(IElement element, PseudoStateSet states);
        IReadOnlyList<IElement> Match(Selector selector, IElement root);
        IReadOnlyDictionary<string, CssValue> Variables(IElement element);
        IReadOnlyList<StyleConflict> Conflicts { get; }
    }

    public interface IStyleEngine
    {
        IStyleModel Build(IReadOnlyList<Stylesheet> cascade, StyleOptions options, IDiagnosticSink sink);
    }
}
