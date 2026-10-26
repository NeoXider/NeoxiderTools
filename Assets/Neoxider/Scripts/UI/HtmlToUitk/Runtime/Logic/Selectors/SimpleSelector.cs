using System;
using System.Collections.Generic;

namespace Neo.HtmlToUitk.Selectors
{
    public interface ISelectNode
    {
        string TypeName { get; }

        string Name { get; }

        ISelectNode Parent { get; }

        bool HasClass(string className);
    }

    public sealed class SelectorList
    {
        private sealed class Compound
        {
            public string Type;
            public string Id;
            public List<string> Classes = new List<string>();
        }

        private sealed class Complex
        {
            public List<Compound> Parts = new List<Compound>();
            public List<char> Combinators = new List<char>();
        }

        private readonly List<Complex> _alternatives = new List<Complex>();

        private SelectorList(string text, string error)
        {
            Text = text;
            Error = error;
        }

        public string Text { get; }

        public string Error { get; }

        public bool IsValid => Error == null && _alternatives.Count > 0;

        public static SelectorList Parse(string text)
        {
            if (string.IsNullOrWhiteSpace(text))
                return new SelectorList(text, "Empty selector");
            SelectorList result = new SelectorList(text, null);
            string[] alternatives = text.Split(',');
            for (int a = 0; a < alternatives.Length; a++)
            {
                string error = ParseComplex(alternatives[a], out Complex complex);
                if (error != null)
                    return new SelectorList(text, error);
                result._alternatives.Add(complex);
            }

            return result;
        }

        public bool Matches(ISelectNode node)
        {
            if (!IsValid || node == null)
                return false;
            for (int i = 0; i < _alternatives.Count; i++)
            {
                if (MatchesComplex(_alternatives[i], _alternatives[i].Parts.Count - 1, node))
                    return true;
            }

            return false;
        }

        private static bool MatchesComplex(Complex complex, int index, ISelectNode node)
        {
            if (!MatchesCompound(complex.Parts[index], node))
                return false;
            if (index == 0)
                return true;
            char combinator = complex.Combinators[index - 1];
            ISelectNode parent = node.Parent;
            if (combinator == '>')
                return parent != null && MatchesComplex(complex, index - 1, parent);
            while (parent != null)
            {
                if (MatchesComplex(complex, index - 1, parent))
                    return true;
                parent = parent.Parent;
            }

            return false;
        }

        private static bool MatchesCompound(Compound compound, ISelectNode node)
        {
            if (compound.Type != null && !string.Equals(compound.Type, node.TypeName, StringComparison.OrdinalIgnoreCase))
                return false;
            if (compound.Id != null && !string.Equals(compound.Id, node.Name, StringComparison.Ordinal))
                return false;
            for (int i = 0; i < compound.Classes.Count; i++)
            {
                if (!node.HasClass(compound.Classes[i]))
                    return false;
            }

            return true;
        }

        private static string ParseComplex(string text, out Complex complex)
        {
            complex = new Complex();
            string trimmed = text.Trim();
            if (trimmed.Length == 0)
                return "Empty selector";
            int i = 0;
            char pendingCombinator = ' ';
            while (i < trimmed.Length)
            {
                while (i < trimmed.Length && char.IsWhiteSpace(trimmed[i]))
                {
                    i++;
                    if (pendingCombinator != '>')
                        pendingCombinator = ' ';
                }

                if (i >= trimmed.Length)
                    break;
                if (trimmed[i] == '>')
                {
                    if (complex.Parts.Count == 0)
                        return "Selector cannot start with '>'";
                    if (pendingCombinator == '>')
                        return "Missing selector between '>' combinators";
                    pendingCombinator = '>';
                    i++;
                    continue;
                }

                int start = i;
                while (i < trimmed.Length && !char.IsWhiteSpace(trimmed[i]) && trimmed[i] != '>')
                    i++;
                string error = ParseCompound(trimmed.Substring(start, i - start), out Compound compound);
                if (error != null)
                    return error;
                if (complex.Parts.Count > 0)
                    complex.Combinators.Add(pendingCombinator);
                complex.Parts.Add(compound);
                pendingCombinator = ' ';
            }

            if (complex.Parts.Count == 0)
                return "Empty selector";
            if (pendingCombinator == '>')
                return "Selector cannot end with '>'";
            return null;
        }

        private static string ParseCompound(string text, out Compound compound)
        {
            compound = new Compound();
            int i = 0;
            while (i < text.Length)
            {
                char c = text[i];
                if (c == '*')
                {
                    if (i != 0)
                        return "Universal selector must start a compound";
                    i++;
                    continue;
                }

                if (c == '.' || c == '#')
                {
                    int start = ++i;
                    while (i < text.Length && IsNameChar(text[i]))
                        i++;
                    if (i == start)
                        return "Missing name after '" + c + "' in '" + text + "'";
                    string name = text.Substring(start, i - start);
                    if (c == '.')
                        compound.Classes.Add(name);
                    else
                    {
                        if (compound.Id != null && !string.Equals(compound.Id, name, StringComparison.Ordinal))
                            return "Conflicting ids in '" + text + "'";
                        compound.Id = name;
                    }
                    continue;
                }

                if (IsNameChar(c) && compound.Type == null && i == 0)
                {
                    int start = i;
                    while (i < text.Length && IsNameChar(text[i]))
                        i++;
                    compound.Type = text.Substring(start, i - start);
                    continue;
                }

                return "Unsupported selector syntax '" + text + "'";
            }

            return null;
        }

        private static bool IsNameChar(char c)
        {
            return char.IsLetterOrDigit(c) || c == '-' || c == '_';
        }
    }
}
