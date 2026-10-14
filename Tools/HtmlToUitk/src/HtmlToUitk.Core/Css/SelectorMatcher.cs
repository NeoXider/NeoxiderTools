using System;
using System.Collections.Generic;
using HtmlToUitk.Core.Html;
using HtmlToUitk.Core.Style;

namespace HtmlToUitk.Core.Css
{
    public static class SelectorMatcher
    {
        private const PseudoStateSet Propagating = PseudoStateSet.Hover | PseudoStateSet.Active;

        public static bool Matches(Selector selector, IElement element, PseudoStateSet states = PseudoStateSet.None)
        {
            if (selector == null || element == null || selector.Parts.Count == 0) return false;
            return MatchChain(selector.Parts, selector.Parts.Count - 1, element, states, null);
        }

        public static bool MatchesRelative(Selector selector, IElement anchor)
        {
            if (selector == null || anchor == null || selector.Parts.Count == 0) return false;
            var candidates = new List<IElement>();
            CollectDescendants(anchor, candidates);
            char first = selector.Parts[0].Combinator;
            if (first == '+' || first == '~')
            {
                IElement sibling = NextSibling(anchor);
                while (sibling != null)
                {
                    candidates.Add(sibling);
                    CollectDescendants(sibling, candidates);
                    sibling = NextSibling(sibling);
                }
            }
            foreach (IElement candidate in candidates)
            {
                if (MatchChain(selector.Parts, selector.Parts.Count - 1, candidate, PseudoStateSet.None, anchor)) return true;
            }
            return false;
        }

        private static void CollectDescendants(IElement element, List<IElement> output)
        {
            foreach (IElement child in element.ChildElements)
            {
                output.Add(child);
                CollectDescendants(child, output);
            }
        }

        private static bool Related(IElement anchor, IElement element, char combinator)
        {
            switch (combinator)
            {
                case '>':
                    return ReferenceEquals(element.Parent, anchor);
                case '+':
                    return ReferenceEquals(PreviousSibling(element), anchor);
                case '~':
                {
                    IElement previous = PreviousSibling(element);
                    while (previous != null)
                    {
                        if (ReferenceEquals(previous, anchor)) return true;
                        previous = PreviousSibling(previous);
                    }
                    return false;
                }
                default:
                {
                    IElement ancestor = element.Parent;
                    while (ancestor != null)
                    {
                        if (ReferenceEquals(ancestor, anchor)) return true;
                        ancestor = ancestor.Parent;
                    }
                    return false;
                }
            }
        }

        private static bool MatchChain(IReadOnlyList<(char Combinator, SimpleSelector Part)> parts, int index, IElement element, PseudoStateSet states, IElement anchor)
        {
            if (!MatchesCompound(parts[index].Part, element, states)) return false;
            if (index == 0) return anchor == null || Related(anchor, element, parts[0].Combinator);
            PseudoStateSet inherited = states & Propagating;
            switch (parts[index].Combinator)
            {
                case '>':
                {
                    IElement parent = element.Parent;
                    return parent != null && MatchChain(parts, index - 1, parent, inherited, anchor);
                }
                case '+':
                {
                    IElement previous = PreviousSibling(element);
                    return previous != null && MatchChain(parts, index - 1, previous, PseudoStateSet.None, anchor);
                }
                case '~':
                {
                    IElement previous = PreviousSibling(element);
                    while (previous != null)
                    {
                        if (MatchChain(parts, index - 1, previous, PseudoStateSet.None, anchor)) return true;
                        previous = PreviousSibling(previous);
                    }
                    return false;
                }
                default:
                {
                    IElement ancestor = element.Parent;
                    while (ancestor != null)
                    {
                        if (MatchChain(parts, index - 1, ancestor, inherited, anchor)) return true;
                        ancestor = ancestor.Parent;
                    }
                    return false;
                }
            }
        }

        public static bool MatchesCompound(SimpleSelector part, IElement element, PseudoStateSet states)
        {
            if (part.Type != null)
            {
                StringComparison comparison = element is DomElement ? StringComparison.OrdinalIgnoreCase : StringComparison.Ordinal;
                if (!string.Equals(part.Type, element.TypeName, comparison)) return false;
            }
            if (part.Id != null)
            {
                string name = element.Name;
                if (!string.Equals(part.Id, name, StringComparison.Ordinal)) return false;
            }
            if (part.Classes.Count > 0)
            {
                IReadOnlyList<string> classes = element.Classes;
                foreach (string cls in part.Classes)
                {
                    bool found = false;
                    for (int i = 0; i < classes.Count; i++)
                    {
                        if (string.Equals(classes[i], cls, StringComparison.Ordinal))
                        {
                            found = true;
                            break;
                        }
                    }
                    if (!found) return false;
                }
            }
            foreach (AttributeSelector attribute in part.Attributes)
            {
                if (!MatchesAttribute(attribute, element)) return false;
            }
            foreach (PseudoClass pseudo in part.Pseudos)
            {
                if (!MatchesPseudo(pseudo, element, states)) return false;
            }
            return true;
        }

        private static bool MatchesAttribute(AttributeSelector attribute, IElement element)
        {
            string actual = element.GetAttribute(attribute.Name);
            if (attribute.Name.Length > 0 && actual == null && element is DomElement) actual = element.GetAttribute(attribute.Name.ToLowerInvariant());
            if (actual == null) return false;
            if (attribute.Operator == null) return true;
            string expected = attribute.Value ?? "";
            StringComparison comparison = attribute.CaseInsensitive ? StringComparison.OrdinalIgnoreCase : StringComparison.Ordinal;
            switch (attribute.Operator)
            {
                case "=":
                    return string.Equals(actual, expected, comparison);
                case "~=":
                    if (expected.Length == 0 || expected.IndexOfAny(new[] { ' ', '\t', '\n', '\r', '\f' }) >= 0) return false;
                    foreach (string word in actual.Split(new[] { ' ', '\t', '\n', '\r', '\f' }, StringSplitOptions.RemoveEmptyEntries))
                    {
                        if (string.Equals(word, expected, comparison)) return true;
                    }
                    return false;
                case "|=":
                    return string.Equals(actual, expected, comparison) || actual.StartsWith(expected + "-", comparison);
                case "^=":
                    return expected.Length > 0 && actual.StartsWith(expected, comparison);
                case "$=":
                    return expected.Length > 0 && actual.EndsWith(expected, comparison);
                case "*=":
                    return expected.Length > 0 && actual.IndexOf(expected, comparison) >= 0;
                default:
                    return false;
            }
        }

        private static bool MatchesPseudo(PseudoClass pseudo, IElement element, PseudoStateSet states)
        {
            switch (pseudo.Name)
            {
                case "hover": return (states & PseudoStateSet.Hover) != 0;
                case "active": return (states & PseudoStateSet.Active) != 0;
                case "inactive": return (states & PseudoStateSet.Active) == 0;
                case "focus": return (states & PseudoStateSet.Focus) != 0;
                case "disabled": return IsDisabled(element, states);
                case "enabled": return !IsDisabled(element, states);
                case "checked": return (states & PseudoStateSet.Checked) != 0 || (element is DomElement && element.GetAttribute("checked") != null);
                case "root": return element.Parent == null || (states & PseudoStateSet.Root) != 0;
                case "empty": return IsEmpty(element);
                case "first-child": return SiblingIndex(element, false, false) == 0;
                case "last-child": return SiblingIndex(element, true, false) == 0;
                case "only-child": return SiblingIndex(element, false, false) == 0 && SiblingIndex(element, true, false) == 0;
                case "first-of-type": return SiblingIndex(element, false, true) == 0;
                case "last-of-type": return SiblingIndex(element, true, true) == 0;
                case "only-of-type": return SiblingIndex(element, false, true) == 0 && SiblingIndex(element, true, true) == 0;
                case "nth-child": return pseudo.HasNth && NthMatches(pseudo.NthA, pseudo.NthB, SiblingIndex(element, false, false) + 1);
                case "nth-last-child": return pseudo.HasNth && NthMatches(pseudo.NthA, pseudo.NthB, SiblingIndex(element, true, false) + 1);
                case "nth-of-type": return pseudo.HasNth && NthMatches(pseudo.NthA, pseudo.NthB, SiblingIndex(element, false, true) + 1);
                case "nth-last-of-type": return pseudo.HasNth && NthMatches(pseudo.NthA, pseudo.NthB, SiblingIndex(element, true, true) + 1);
                case "not":
                    foreach (Selector inner in pseudo.Selectors)
                    {
                        if (Matches(inner, element, states)) return false;
                    }
                    return pseudo.Selectors.Count > 0;
                case "is":
                case "where":
                case "matches":
                case "any":
                    foreach (Selector inner in pseudo.Selectors)
                    {
                        if (Matches(inner, element, states)) return true;
                    }
                    return false;
                case "has":
                    foreach (Selector inner in pseudo.Selectors)
                    {
                        if (MatchesRelative(inner, element)) return true;
                    }
                    return false;
                default:
                    return false;
            }
        }

        private static bool IsDisabled(IElement element, PseudoStateSet states)
        {
            if ((states & PseudoStateSet.Disabled) != 0) return true;
            return element is DomElement && element.GetAttribute("disabled") != null;
        }

        private static bool IsEmpty(IElement element)
        {
            var dom = element as DomElement;
            if (dom != null) return dom.Children.Count == 0;
            return element.ChildElements.Count == 0;
        }

        public static bool NthMatches(int a, int b, int position)
        {
            if (a == 0) return position == b;
            long diff = (long)position - b;
            if (diff % a != 0) return false;
            return diff / a >= 0;
        }

        private static IReadOnlyList<IElement> SiblingsOf(IElement element)
        {
            IElement parent = element.Parent;
            if (parent == null) return new[] { element };
            return parent.ChildElements;
        }

        private static int SiblingIndex(IElement element, bool fromEnd, bool sameType)
        {
            IReadOnlyList<IElement> siblings = SiblingsOf(element);
            int count = 0;
            if (!fromEnd)
            {
                for (int i = 0; i < siblings.Count; i++)
                {
                    if (ReferenceEquals(siblings[i], element)) return count;
                    if (!sameType || SameType(siblings[i], element)) count++;
                }
            }
            else
            {
                for (int i = siblings.Count - 1; i >= 0; i--)
                {
                    if (ReferenceEquals(siblings[i], element)) return count;
                    if (!sameType || SameType(siblings[i], element)) count++;
                }
            }
            return int.MaxValue / 2;
        }

        private static bool SameType(IElement a, IElement b) => string.Equals(a.TypeName, b.TypeName, StringComparison.OrdinalIgnoreCase);

        public static IElement PreviousSibling(IElement element)
        {
            IElement parent = element.Parent;
            if (parent == null) return null;
            IReadOnlyList<IElement> siblings = parent.ChildElements;
            for (int i = 0; i < siblings.Count; i++)
            {
                if (ReferenceEquals(siblings[i], element)) return i > 0 ? siblings[i - 1] : null;
            }
            return null;
        }

        public static IElement NextSibling(IElement element)
        {
            IElement parent = element.Parent;
            if (parent == null) return null;
            IReadOnlyList<IElement> siblings = parent.ChildElements;
            for (int i = 0; i < siblings.Count; i++)
            {
                if (ReferenceEquals(siblings[i], element)) return i + 1 < siblings.Count ? siblings[i + 1] : null;
            }
            return null;
        }
    }
}
