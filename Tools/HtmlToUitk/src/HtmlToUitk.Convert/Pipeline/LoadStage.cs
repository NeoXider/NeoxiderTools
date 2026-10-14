using System;
using System.Collections.Generic;
using System.Text;
using System.Text.RegularExpressions;
using HtmlToUitk.Convert.Config;
using HtmlToUitk.Core.Css;
using HtmlToUitk.Core.Text;

namespace HtmlToUitk.Convert
{
    internal static class Glob
    {
        public static bool HasWildcard(string pattern)
        {
            return pattern.IndexOf('*') >= 0 || pattern.IndexOf('?') >= 0;
        }

        public static Regex ToRegex(string pattern)
        {
            string text = PathUtil.Normalize(pattern);
            var sb = new StringBuilder("^");
            for (int i = 0; i < text.Length; i++)
            {
                char c = text[i];
                if (c == '*')
                {
                    if (i + 1 < text.Length && text[i + 1] == '*')
                    {
                        i++;
                        if (i + 1 < text.Length && text[i + 1] == '/')
                        {
                            i++;
                            sb.Append("(?:.*/)?");
                        }
                        else sb.Append(".*");
                    }
                    else sb.Append("[^/]*");
                }
                else if (c == '?') sb.Append("[^/]");
                else sb.Append(Regex.Escape(c.ToString()));
            }

            sb.Append('$');
            return new Regex(sb.ToString(), RegexOptions.CultureInvariant);
        }

        public static string StaticPrefix(string pattern)
        {
            string text = PathUtil.Normalize(pattern);
            int star = text.IndexOfAny(new[] { '*', '?' });
            if (star < 0) return text;
            int slash = text.LastIndexOf('/', star);
            return slash < 0 ? "" : text.Substring(0, slash);
        }

        public static List<string> Expand(IFileSystem fs, string pattern, string extension)
        {
            var result = new List<string>();
            string normalized = PathUtil.Normalize(pattern);
            if (!HasWildcard(normalized))
            {
                if (fs.FileExists(normalized)) result.Add(normalized);
                else if (fs.DirectoryExists(normalized))
                {
                    foreach (string file in fs.EnumerateFiles(normalized, true))
                    {
                        string n = PathUtil.Normalize(file);
                        if (extension == null || n.EndsWith(extension, StringComparison.OrdinalIgnoreCase)) result.Add(n);
                    }
                }

                result.Sort(StringComparer.Ordinal);
                return result;
            }

            string prefix = StaticPrefix(normalized);
            Regex regex = ToRegex(normalized);
            foreach (string file in fs.EnumerateFiles(prefix, true))
            {
                string n = PathUtil.Normalize(file);
                if (regex.IsMatch(n)) result.Add(n);
            }

            result.Sort(StringComparer.Ordinal);
            return result;
        }
    }

    public sealed class LoadStage : IPipelineStage<ConversionJob, InputSet>
    {
        private readonly PipelineState _state;

        internal LoadStage(PipelineState state)
        {
            _state = state;
        }

        public string Name => "load";

        public InputSet Run(ConversionJob job, StageContext context)
        {
            IFileSystem fs = context.Fs;
            JobSpec spec = job.Spec;
            var seen = new HashSet<string>(StringComparer.Ordinal);
            var pages = new List<PageInput>();
            foreach (string pattern in spec.Inputs.Html)
            {
                List<string> files = Glob.Expand(fs, pattern, ".html");
                if (files.Count == 0) EmptyPattern(context, pattern);
                foreach (string file in files)
                {
                    if (!seen.Add(file)) continue;
                    pages.Add(new PageInput { Path = file, Text = fs.ReadAllText(file) });
                }
            }

            var styles = new List<StyleInput>();
            seen.Clear();
            foreach (string pattern in spec.Inputs.Css)
            {
                List<string> files = Glob.Expand(fs, pattern, ".css");
                if (files.Count == 0) EmptyPattern(context, pattern);
                foreach (string file in files)
                {
                    if (!seen.Add(file)) continue;
                    styles.Add(new StyleInput { Path = file, Text = fs.ReadAllText(file), Origin = StyleOrigin.ConfigListed });
                }
            }

            var uss = new List<UssInput>();
            seen.Clear();
            foreach (string pattern in spec.Inputs.Uss)
            {
                List<string> files = Glob.Expand(fs, pattern, ".uss");
                if (files.Count == 0) EmptyPattern(context, pattern);
                foreach (string file in files)
                {
                    if (!seen.Add(file)) continue;
                    uss.Add(new UssInput { Path = file, Text = fs.ReadAllText(file) });
                }
            }

            var partials = new List<PartialInput>();
            seen.Clear();
            foreach (string pattern in spec.Inputs.Partials)
            {
                List<string> files = Glob.Expand(fs, pattern, ".html");
                if (files.Count == 0) EmptyPattern(context, pattern);
                foreach (string file in files)
                {
                    if (!seen.Add(file)) continue;
                    partials.Add(new PartialInput { Path = file, Id = PathUtil.Stem(file), Text = fs.ReadAllText(file) });
                }
            }

            return new InputSet { Pages = pages, Styles = styles, UssPassThrough = uss, Partials = partials, Includes = new IncludeGraph() };
        }

        private static void EmptyPattern(StageContext context, string pattern)
        {
            context.Diagnostics.Report(new Diagnostic
            {
                RuleId = "J010",
                Severity = Severity.Warning,
                Tier = Tier.Native,
                Span = new SourceSpan(pattern, default(SourcePos), default(SourcePos)),
                Construct = pattern,
                Message = "the input pattern '" + pattern + "' matched nothing",
                Suggestion = "Check the path or the glob."
            });
        }
    }
}
