using System;
using System.Collections.Generic;
using System.Text;

namespace Neo.HtmlToUitk.Tooling
{
    public static class CliCommandLine
    {
        public static List<string> BuildRunArguments(string jobPath, bool validateOnly, bool force, bool strict = false)
        {
            List<string> args = new List<string> { "run", jobPath, "--format", "json" };
            if (validateOnly)
                args.Add("--validate-only");
            if (force)
                args.Add("--force");
            if (strict)
                args.Add("--strict");
            return args;
        }

        public static string Quote(string argument)
        {
            if (argument.Length > 0 && argument.IndexOfAny(new[] { ' ', '\t', '"' }) < 0)
                return argument;
            StringBuilder builder = new StringBuilder("\"");
            int backslashes = 0;
            for (int i = 0; i < argument.Length; i++)
            {
                char c = argument[i];
                if (c == '\\')
                {
                    backslashes++;
                    continue;
                }

                if (c == '"')
                {
                    builder.Append('\\', backslashes * 2 + 1).Append('"');
                    backslashes = 0;
                    continue;
                }

                builder.Append('\\', backslashes);
                backslashes = 0;
                builder.Append(c);
            }

            builder.Append('\\', backslashes * 2).Append('"');
            return builder.ToString();
        }

        public static string Join(IReadOnlyList<string> arguments)
        {
            string[] parts = new string[arguments.Count];
            for (int i = 0; i < parts.Length; i++)
                parts[i] = Quote(arguments[i]);
            return string.Join(" ", parts);
        }

        public static string ExtractJsonObject(string output)
        {
            if (string.IsNullOrEmpty(output))
                return null;
            int start = output.IndexOf('{');
            int end = output.LastIndexOf('}');
            return start >= 0 && end > start ? output.Substring(start, end - start + 1) : null;
        }
    }
}
