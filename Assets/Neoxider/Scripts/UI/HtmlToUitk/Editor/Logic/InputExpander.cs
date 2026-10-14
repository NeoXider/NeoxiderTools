using System;
using System.Collections.Generic;

namespace Neo.HtmlToUitk.Tooling
{
    public interface IFileView
    {
        bool FileExists(string path);

        bool DirectoryExists(string path);

        IEnumerable<string> EnumerateFiles(string directory, bool recursive);
    }

    public enum EntryKind
    {
        File,
        Folder,
        Glob
    }

    public sealed class ExpandedEntry
    {
        public ExpandedEntry(string entry, EntryKind kind, IReadOnlyList<string> files)
        {
            Entry = entry;
            Kind = kind;
            Files = files;
        }

        public string Entry { get; }

        public EntryKind Kind { get; }

        public IReadOnlyList<string> Files { get; }

        public bool IsEmpty => Files.Count == 0;
    }

    public static class InputExpander
    {
        public static IReadOnlyList<ExpandedEntry> Expand(string baseDirectory, IReadOnlyList<string> entries, IFileView fs, IReadOnlyList<string> extensions)
        {
            List<ExpandedEntry> result = new List<ExpandedEntry>();
            string baseDir = GlobMatcher.Normalize(baseDirectory).TrimEnd('/');
            for (int i = 0; i < entries.Count; i++)
            {
                string entry = GlobMatcher.Normalize(entries[i]?.Trim());
                if (string.IsNullOrEmpty(entry))
                    continue;
                result.Add(ExpandOne(baseDir, entry, fs, extensions));
            }

            return result;
        }

        public static IReadOnlyList<string> Flatten(IReadOnlyList<ExpandedEntry> entries)
        {
            HashSet<string> seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            List<string> list = new List<string>();
            for (int i = 0; i < entries.Count; i++)
            {
                for (int f = 0; f < entries[i].Files.Count; f++)
                {
                    if (seen.Add(entries[i].Files[f]))
                        list.Add(entries[i].Files[f]);
                }
            }

            list.Sort(StringComparer.Ordinal);
            return list;
        }

        public static string MakeRelative(string baseDirectory, string path)
        {
            string baseDir = GlobMatcher.Normalize(baseDirectory).TrimEnd('/') + "/";
            string normalized = GlobMatcher.Normalize(path);
            if (normalized.StartsWith(baseDir, StringComparison.OrdinalIgnoreCase))
                return normalized.Substring(baseDir.Length);
            return normalized;
        }

        public static bool IsRooted(string path)
        {
            string normalized = GlobMatcher.Normalize(path);
            return normalized.StartsWith("/", StringComparison.Ordinal) || (normalized.Length > 1 && normalized[1] == ':');
        }

        public static string Combine(string baseDirectory, string relative)
        {
            if (IsRooted(relative))
                return GlobMatcher.Normalize(relative);
            string baseDir = GlobMatcher.Normalize(baseDirectory).TrimEnd('/');
            return string.IsNullOrEmpty(relative) ? baseDir : baseDir + "/" + GlobMatcher.Normalize(relative).TrimStart('/');
        }

        private static ExpandedEntry ExpandOne(string baseDir, string entry, IFileView fs, IReadOnlyList<string> extensions)
        {
            if (GlobMatcher.HasWildcards(entry))
                return new ExpandedEntry(entry, EntryKind.Glob, ExpandGlob(baseDir, entry, fs));
            string full = Combine(baseDir, entry);
            if (fs.DirectoryExists(full))
                return new ExpandedEntry(entry, EntryKind.Folder, ExpandFolder(baseDir, full, fs, extensions));
            List<string> files = new List<string>();
            if (fs.FileExists(full))
                files.Add(MakeRelative(baseDir, full));
            return new ExpandedEntry(entry, EntryKind.File, files);
        }

        private static IReadOnlyList<string> ExpandGlob(string baseDir, string pattern, IFileView fs)
        {
            string fullPattern = Combine(baseDir, pattern);
            string prefix = GlobMatcher.StaticPrefix(fullPattern);
            List<string> files = new List<string>();
            if (string.IsNullOrEmpty(prefix) || !fs.DirectoryExists(prefix))
                return files;
            foreach (string file in fs.EnumerateFiles(prefix, true))
            {
                string candidate = GlobMatcher.Normalize(file);
                if (GlobMatcher.IsMatch(fullPattern, candidate))
                    files.Add(MakeRelative(baseDir, candidate));
            }

            files.Sort(StringComparer.Ordinal);
            return files;
        }

        private static IReadOnlyList<string> ExpandFolder(string baseDir, string folder, IFileView fs, IReadOnlyList<string> extensions)
        {
            List<string> files = new List<string>();
            foreach (string file in fs.EnumerateFiles(folder, true))
            {
                if (HasExtension(file, extensions))
                    files.Add(MakeRelative(baseDir, GlobMatcher.Normalize(file)));
            }

            files.Sort(StringComparer.Ordinal);
            return files;
        }

        private static bool HasExtension(string file, IReadOnlyList<string> extensions)
        {
            if (extensions == null || extensions.Count == 0)
                return true;
            for (int i = 0; i < extensions.Count; i++)
            {
                if (file.EndsWith(extensions[i], StringComparison.OrdinalIgnoreCase))
                    return true;
            }

            return false;
        }
    }
}
