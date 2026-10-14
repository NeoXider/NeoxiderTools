using System;
using System.Collections.Generic;
using System.Text;
using System.Text.RegularExpressions;

namespace Neo.HtmlToUitk.Tooling
{
    public static class GlobMatcher
    {
        public static bool HasWildcards(string pattern)
        {
            return pattern != null && pattern.IndexOfAny(new[] { '*', '?' }) >= 0;
        }

        public static string Normalize(string path)
        {
            return path == null ? null : path.Replace('\\', '/');
        }

        public static string StaticPrefix(string pattern)
        {
            string normalized = Normalize(pattern);
            string[] parts = normalized.Split('/');
            List<string> prefix = new List<string>();
            for (int i = 0; i < parts.Length - 1; i++)
            {
                if (HasWildcards(parts[i]))
                    break;
                prefix.Add(parts[i]);
            }

            return string.Join("/", prefix);
        }

        public static bool IsMatch(string pattern, string path, bool ignoreCase = true)
        {
            RegexOptions options = RegexOptions.CultureInvariant;
            if (ignoreCase)
                options |= RegexOptions.IgnoreCase;
            return new Regex(ToRegex(Normalize(pattern)), options).IsMatch(Normalize(path));
        }

        public static string ToRegex(string pattern)
        {
            StringBuilder builder = new StringBuilder("^");
            int i = 0;
            while (i < pattern.Length)
            {
                char c = pattern[i];
                if (c == '*')
                {
                    if (i + 1 < pattern.Length && pattern[i + 1] == '*')
                    {
                        bool segmentStart = i == 0 || pattern[i - 1] == '/';
                        bool followedBySlash = i + 2 < pattern.Length && pattern[i + 2] == '/';
                        if (segmentStart && followedBySlash)
                        {
                            builder.Append("(?:.*/)?");
                            i += 3;
                            continue;
                        }

                        builder.Append(".*");
                        i += 2;
                        continue;
                    }

                    builder.Append("[^/]*");
                    i++;
                }
                else if (c == '?')
                {
                    builder.Append("[^/]");
                    i++;
                }
                else
                {
                    builder.Append(Regex.Escape(c.ToString()));
                    i++;
                }
            }

            builder.Append('$');
            return builder.ToString();
        }
    }
}
