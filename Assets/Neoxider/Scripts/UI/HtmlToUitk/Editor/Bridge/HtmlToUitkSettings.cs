using System.IO;
using UnityEditor;
using UnityEngine;

namespace Neo.HtmlToUitk.Tooling
{
    public static class HtmlToUitkSettings
    {
        private const string Prefix = "Neoxider.HtmlToUitk.";

        public static string CliPath
        {
            get => EditorPrefs.GetString(Prefix + "cli", string.Empty);
            set => EditorPrefs.SetString(Prefix + "cli", value ?? string.Empty);
        }

        public static bool PreferInProcess
        {
            get => EditorPrefs.GetBool(Prefix + "inProcess", false);
            set => EditorPrefs.SetBool(Prefix + "inProcess", value);
        }

        /// <summary>An explicit external runner wins; otherwise discover the project-owned CLI.</summary>
        public static string EffectiveCliPath => ResolveCliPath(CliPath, HtmlToUitkPaths.ProjectRoot);

        public static string ResolveCliPath(string configured, string projectRoot)
        {
            if (!string.IsNullOrWhiteSpace(configured))
            {
                string path = configured.Trim();
                if (Path.IsPathRooted(path)) return Path.GetFullPath(path);
                if (path.IndexOf('/') < 0 && path.IndexOf('\\') < 0 && !path.EndsWith(".dll", System.StringComparison.OrdinalIgnoreCase)) return path;
                return Path.GetFullPath(Path.Combine(projectRoot, path));
            }
            string[] candidates =
            {
                "Tools/HtmlToUitk/bin/htmltouitk.dll",
                "Tools/HtmlToUitk/src/HtmlToUitk.Cli/bin/Release/net9.0/htmltouitk.dll",
                "Tools/HtmlToUitk/bin/HtmlToUitk.Cli.dll",
                "Tools/HtmlToUitk/src/HtmlToUitk.Cli/bin/Release/net9.0/HtmlToUitk.Cli.dll"
            };
            foreach (string candidate in candidates)
            {
                string absolute = Path.GetFullPath(Path.Combine(projectRoot, candidate));
                if (File.Exists(absolute)) return absolute;
            }
            return "htmltouitk";
        }

        public static bool ManageTextureImport
        {
            get => EditorPrefs.GetBool(Prefix + "textures", true);
            set => EditorPrefs.SetBool(Prefix + "textures", value);
        }

        public static string LastJobPath
        {
            get => EditorPrefs.GetString(Prefix + "lastJob", string.Empty);
            set => EditorPrefs.SetString(Prefix + "lastJob", value ?? string.Empty);
        }

        public static int CliTimeoutSeconds
        {
            get => EditorPrefs.GetInt(Prefix + "cliTimeout", 120);
            set => EditorPrefs.SetInt(Prefix + "cliTimeout", Mathf.Max(5, value));
        }
    }

    public static class HtmlToUitkPaths
    {
        public static string ProjectRoot => GlobMatcher.Normalize(Directory.GetParent(Application.dataPath).FullName);

        public static string ToAbsolute(string path)
        {
            if (string.IsNullOrEmpty(path))
                return string.Empty;
            return InputExpander.IsRooted(path) ? GlobMatcher.Normalize(path) : InputExpander.Combine(ProjectRoot, path);
        }

        public static string ToProjectRelative(string absolutePath)
        {
            if (string.IsNullOrEmpty(absolutePath))
                return null;
            string normalized = GlobMatcher.Normalize(absolutePath);
            string root = ProjectRoot + "/";
            return normalized.StartsWith(root, System.StringComparison.OrdinalIgnoreCase) ? normalized.Substring(root.Length) : null;
        }

        public static bool IsInsideAssets(string absolutePath)
        {
            string relative = ToProjectRelative(absolutePath);
            return relative != null && (relative.StartsWith("Assets/", System.StringComparison.Ordinal) || relative.StartsWith("Packages/", System.StringComparison.Ordinal));
        }

        public static string DirectoryOf(string absoluteFilePath)
        {
            return GlobMatcher.Normalize(Path.GetDirectoryName(absoluteFilePath) ?? string.Empty);
        }
    }

    public sealed class DiskFileView : IFileView
    {
        public static readonly DiskFileView Instance = new DiskFileView();

        public bool FileExists(string path) => File.Exists(path);

        public bool DirectoryExists(string path) => Directory.Exists(path);

        public System.Collections.Generic.IEnumerable<string> EnumerateFiles(string directory, bool recursive)
        {
            return Directory.EnumerateFiles(directory, "*", recursive ? SearchOption.AllDirectories : SearchOption.TopDirectoryOnly);
        }
    }
}
