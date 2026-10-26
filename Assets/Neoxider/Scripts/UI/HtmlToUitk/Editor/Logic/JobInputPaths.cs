using System;
using System.Collections.Generic;
using System.IO;

namespace Neo.HtmlToUitk.Tooling
{
    /// <summary>Shared dependency boundaries for editor import and filesystem notifications.</summary>
    public static class JobInputPaths
    {
        private static readonly string[] HtmlExtensions = { ".html", ".htm" };
        private static readonly string[] CssExtensions = { ".css" };
        private static readonly string[] UssExtensions = { ".uss" };

        public static bool IsInput(JobDocument document, string baseDirectory, string path)
        {
            if (string.IsNullOrWhiteSpace(path))
                return false;
            string candidate = AbsoluteEntry(baseDirectory, path);
            string output = AbsoluteEntry(baseDirectory, string.IsNullOrEmpty(document.OutputRoot) ? "." : document.OutputRoot);
            if (string.Equals(candidate, output, StringComparison.OrdinalIgnoreCase) || candidate.StartsWith(Prefix(output), StringComparison.OrdinalIgnoreCase))
                return false;
            return Matches(document.Html, HtmlExtensions, baseDirectory, candidate)
                || Matches(document.Css, CssExtensions, baseDirectory, candidate)
                || Matches(document.Uss, UssExtensions, baseDirectory, candidate)
                || Matches(document.Partials, HtmlExtensions, baseDirectory, candidate);
        }

        public static IReadOnlyList<string> GetWatchDirectories(JobDocument document, string baseDirectory, IFileView files)
        {
            List<string> directories = new List<string>();
            HashSet<string> seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            AddDirectories(document.Html, baseDirectory, files, directories, seen);
            AddDirectories(document.Css, baseDirectory, files, directories, seen);
            AddDirectories(document.Uss, baseDirectory, files, directories, seen);
            AddDirectories(document.Partials, baseDirectory, files, directories, seen);
            return directories;
        }

        private static bool Matches(IReadOnlyList<string> entries, IReadOnlyList<string> extensions, string baseDirectory, string candidate)
        {
            for (int index = 0; index < entries.Count; index++)
            {
                if (string.IsNullOrWhiteSpace(entries[index]))
                    continue;
                string entry = AbsoluteEntry(baseDirectory, entries[index].Trim());
                if (GlobMatcher.HasWildcards(entry))
                {
                    if (GlobMatcher.IsMatch(entry, candidate))
                        return true;
                }
                else if (string.Equals(entry, candidate, StringComparison.OrdinalIgnoreCase))
                {
                    return true;
                }
                else if (candidate.StartsWith(Prefix(entry), StringComparison.OrdinalIgnoreCase))
                {
                    for (int extension = 0; extension < extensions.Count; extension++)
                        if (candidate.EndsWith(extensions[extension], StringComparison.OrdinalIgnoreCase))
                            return true;
                }
            }
            return false;
        }

        private static void AddDirectories(IReadOnlyList<string> entries, string baseDirectory, IFileView files, List<string> directories, HashSet<string> seen)
        {
            for (int index = 0; index < entries.Count; index++)
            {
                if (string.IsNullOrWhiteSpace(entries[index]))
                    continue;
                string entry = AbsoluteEntry(baseDirectory, entries[index].Trim());
                string directory = GlobMatcher.HasWildcards(entry)
                    ? NormalizeAbsolute(GlobMatcher.StaticPrefix(entry) + "/")
                    : files.DirectoryExists(entry) ? entry : GlobMatcher.Normalize(Path.GetDirectoryName(entry));
                // Watch an existing ancestor as well when an authored input directory has not been created yet.
                while (!string.IsNullOrEmpty(directory) && !files.DirectoryExists(directory))
                    directory = GlobMatcher.Normalize(Path.GetDirectoryName(directory));
                if (!string.IsNullOrEmpty(directory) && seen.Add(directory))
                    directories.Add(directory);
            }
        }

        private static string AbsoluteEntry(string baseDirectory, string entry)
        {
            string full = InputExpander.Combine(NormalizeAbsolute(baseDirectory), GlobMatcher.Normalize(entry));
            if (!GlobMatcher.HasWildcards(full))
                return NormalizeAbsolute(full);
            string prefix = GlobMatcher.StaticPrefix(full);
            string tail = full.Substring(prefix.Length).TrimStart('/');
            return Prefix(NormalizeAbsolute(prefix + "/")) + tail;
        }

        private static string NormalizeAbsolute(string path)
        {
            string normalized = GlobMatcher.Normalize(Path.GetFullPath(path));
            string root = GlobMatcher.Normalize(Path.GetPathRoot(normalized));
            return string.Equals(normalized, root, StringComparison.OrdinalIgnoreCase) ? normalized : normalized.TrimEnd('/');
        }

        private static string Prefix(string path)
        {
            return path.EndsWith("/", StringComparison.Ordinal) ? path : path + "/";
        }
    }
}
