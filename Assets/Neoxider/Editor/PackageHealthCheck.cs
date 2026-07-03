using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;
using UnityEditor;
using UnityEngine;

namespace Neo.Editor
{
    /// <summary>
    ///     Release-hygiene checks: the package version must match everywhere it is written, and every
    ///     [NeoDoc] path must resolve to a real page under Docs/. Run before tagging a release —
    ///     both drifts have shipped in the past (version desync twice, missing doc pages).
    /// </summary>
    public static class PackageHealthCheck
    {
        private const string Root = "Assets/Neoxider";

        private static readonly Regex NeoDocPattern = new Regex(
            "NeoDoc\\(\"([^\"]+)\"\\)", RegexOptions.Compiled);

        [MenuItem("Tools/Neoxider/Package Health Check")]
        public static void Run()
        {
            int issues = 0;
            issues += CheckVersionParity();
            issues += CheckNeoDocPathsResolve();

            if (issues == 0)
            {
                Debug.Log("[PackageHealthCheck] OK: versions in sync, all [NeoDoc] paths resolve.");
            }
            else
            {
                Debug.LogWarning($"[PackageHealthCheck] {issues} issue group(s) found — see messages above.");
            }
        }

        private static int CheckVersionParity()
        {
            string packageJsonPath = Path.Combine(Root, "package.json");
            if (!File.Exists(packageJsonPath))
            {
                Debug.LogError("[PackageHealthCheck] package.json not found.");
                return 1;
            }

            Match versionMatch = Regex.Match(File.ReadAllText(packageJsonPath), "\"version\"\\s*:\\s*\"([^\"]+)\"");
            if (!versionMatch.Success)
            {
                Debug.LogError("[PackageHealthCheck] package.json has no version field.");
                return 1;
            }

            string version = versionMatch.Groups[1].Value;
            int issues = 0;

            issues += CheckFileMentionsVersion(Path.Combine(Root, "README.md"),
                $"version-{version}-", version, "README.md badge");
            issues += CheckFileMentionsVersion(Path.Combine(Root, "PROJECT_SUMMARY.md"),
                $"`{version}`", version, "PROJECT_SUMMARY.md");
            issues += CheckFileMentionsVersion(Path.Combine(Root, "CHANGELOG.md"),
                $"[{version}]", version, "CHANGELOG.md entry");
            return issues;
        }

        private static int CheckFileMentionsVersion(string path, string needle, string version, string label)
        {
            if (!File.Exists(path))
            {
                return 0;
            }

            if (File.ReadAllText(path).Contains(needle))
            {
                return 0;
            }

            Debug.LogWarning($"[PackageHealthCheck] {label} does not mention package version {version}.");
            return 1;
        }

        // Every NeoDoc attribute's relative path must point at an existing page under Docs/.
        private static int CheckNeoDocPathsResolve()
        {
            string docsRoot = Path.Combine(Root, "Docs");
            if (!Directory.Exists(docsRoot))
            {
                Debug.LogError("[PackageHealthCheck] Docs folder not found.");
                return 1;
            }

            var missing = new SortedDictionary<string, string>();
            foreach (string file in Directory.GetFiles(Root, "*.cs", SearchOption.AllDirectories))
            {
                foreach (Match m in NeoDocPattern.Matches(File.ReadAllText(file)))
                {
                    string relative = m.Groups[1].Value.Replace('\\', '/').TrimStart('/');
                    if (!File.Exists(Path.Combine(docsRoot, relative)) && !missing.ContainsKey(relative))
                    {
                        missing.Add(relative, file.Replace('\\', '/'));
                    }
                }
            }

            if (missing.Count == 0)
            {
                return 0;
            }

            foreach (KeyValuePair<string, string> entry in missing)
            {
                Debug.LogWarning(
                    $"[PackageHealthCheck] [NeoDoc] path '{entry.Key}' has no page under Docs/ (declared in {entry.Value}).");
            }

            return 1;
        }
    }
}
