using System;
using System.Collections.Generic;

namespace HtmlToUitk.Rules
{
    public static class ConstructKeys
    {
        public const string DisplayGrid = "display:grid";
        public const string Gap = "prop:gap";
        public const string ZIndex = "prop:z-index";
        public const string AnimationDeclaration = "anim:declaration";
        public const string SelectorSibling = "selector:sibling";
        public const string SelectorAttribute = "selector:attribute";
        public const string SelectorType = "selector:type";
        public const string PseudoPrefix = "pseudo:";
        public const string PseudoElementPrefix = "pseudo-element:";
        public const string AtPrefix = "at:";
        public const string FunctionPrefix = "fn:";
        public const string UnitPrefix = "unit:";
        public const string FilterPrefix = "filter:";
        public const string ShadowOuter = "shadow:outer";
        public const string ShadowInset = "shadow:inset";
        public const string OverflowPrefix = "overflow:";
        public const string PositionPrefix = "position:";
        public const string TransformSingle = "transform:single";
        public const string TransformMulti = "transform:multi";
        public const string TransformUnsupported = "transform:unsupported";
        public const string KeywordPrefix = "keyword:";
        public const string ShorthandPrefix = "shorthand:";

        public const string DataText = "text";
        public const string DataProperty = "property";
        public const string DataValue = "value";
        public const string DataMessage = "message";
        public const string DataSuggestion = "suggestion";
        public const string DataApproximate = "approximate";
        public const string DataSelector = "selector";
        public const string DataName = "name";
        public const string DataUnit = "unit";
        public const string DataFeature = "feature";
        public const string DataFoldable = "foldable";
        public const string DataSupported = "supported";
        public const string DataUnresolvable = "unresolvable";
        public const string DataTracks = "tracks";
        public const string DataContext = "context";
        public const string DataCombinator = "combinator";
        public const string DataPage = "page";
        public const string DataContent = "content";
        public const string DataInvalid = "invalid";
    }

    internal static class Cx
    {
        public static Construct Make(ConstructKind kind, string key, SourceSpan span, object node, params string[] pairs)
        {
            var data = new Dictionary<string, string>(StringComparer.Ordinal);
            for (int i = 0; i + 1 < pairs.Length; i += 2)
            {
                if (pairs[i + 1] != null) data[pairs[i]] = pairs[i + 1];
            }
            return new Construct { Kind = kind, Key = key, Span = span, Node = node, Data = data };
        }

        public static string Get(Construct construct, string key)
        {
            string value;
            return construct != null && construct.Data != null && construct.Data.TryGetValue(key, out value) ? value : null;
        }

        public static bool Is(Construct construct, string key)
        {
            return string.Equals(Get(construct, key), "true", StringComparison.Ordinal);
        }
    }
}
