using System;
using System.Collections.Generic;
using System.Text;

namespace HtmlToUitk.Core.Text
{
    public static class NameCase
    {
        public static IReadOnlyList<string> Words(string text)
        {
            var words = new List<string>();
            if (string.IsNullOrEmpty(text)) return words;
            var current = new StringBuilder();
            for (int i = 0; i < text.Length; i++)
            {
                char c = text[i];
                if (!char.IsLetterOrDigit(c))
                {
                    Flush(words, current);
                    continue;
                }
                if (current.Length > 0)
                {
                    char previous = text[i - 1];
                    bool lowerToUpper = (char.IsLower(previous) || char.IsDigit(previous)) && char.IsUpper(c);
                    bool acronymEnd = char.IsUpper(previous) && char.IsUpper(c) && i + 1 < text.Length && char.IsLower(text[i + 1]);
                    if (lowerToUpper || acronymEnd) Flush(words, current);
                }
                current.Append(c);
            }
            Flush(words, current);
            return words;
        }

        private static void Flush(List<string> words, StringBuilder current)
        {
            if (current.Length == 0) return;
            words.Add(current.ToString());
            current.Clear();
        }

        public static string ToPascal(string text)
        {
            var sb = new StringBuilder();
            foreach (string word in Words(text))
            {
                sb.Append(char.ToUpperInvariant(word[0]));
                if (word.Length > 1) sb.Append(word.Substring(1).ToLowerInvariant());
            }
            return sb.ToString();
        }

        public static string ToKebab(string text)
        {
            var parts = new List<string>();
            foreach (string word in Words(text)) parts.Add(word.ToLowerInvariant());
            return string.Join("-", parts);
        }

        public static bool IsKebab(string text)
        {
            if (string.IsNullOrEmpty(text)) return false;
            if (text[0] < 'a' || text[0] > 'z') return false;
            bool previousDash = false;
            for (int i = 0; i < text.Length; i++)
            {
                char c = text[i];
                bool ok = (c >= 'a' && c <= 'z') || (c >= '0' && c <= '9') || c == '-';
                if (!ok) return false;
                if (c == '-' && previousDash) return false;
                previousDash = c == '-';
            }
            return !previousDash;
        }
    }
}
