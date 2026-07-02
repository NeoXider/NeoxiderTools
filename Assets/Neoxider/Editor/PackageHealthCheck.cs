using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;
using UnityEditor;
using UnityEngine;

namespace Neo.Editor
{
    /// <summary>
    ///     Release-hygiene checks: the package version must match everywhere it is written, and the
    ///     RU/EN documentation trees should stay in parity. Run before tagging a release —
    ///     both drifts have shipped in the past (version desync twice, ~40 RU docs without EN).
    /// </summary>
    public static class PackageHealthCheck
    {
        private const string Root = "Assets/Neoxider";

        [MenuItem("Tools/Neoxider/Package Health Check")]
        public static void Run()
        {
            int issues = 0;
            issues += CheckVersionParity();
            issues += CheckDocsParity();

            if (issues == 0)
            {
                Debug.Log("[PackageHealthCheck] OK: versions in sync, Docs/DocsEn in parity.");
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

        private static int CheckDocsParity()
        {
            string ruRoot = Path.Combine(Root, "Docs");
            string enRoot = Path.Combine(Root, "DocsEn");
            if (!Directory.Exists(ruRoot) || !Directory.Exists(enRoot))
            {
                return 0;
            }

            HashSet<string> ru = CollectRelativeDocs(ruRoot);
            HashSet<string> en = CollectRelativeDocs(enRoot);

            var ruOnly = new List<string>();
            foreach (string doc in ru)
            {
                if (!en.Contains(doc))
                {
                    ruOnly.Add(doc);
                }
            }

            var enOnly = new List<string>();
            foreach (string doc in en)
            {
                if (!ru.Contains(doc))
                {
                    enOnly.Add(doc);
                }
            }

            ruOnly.Sort();
            enOnly.Sort();

            int issues = 0;
            if (ruOnly.Count > 0)
            {
                issues++;
                Debug.LogWarning(
                    $"[PackageHealthCheck] {ruOnly.Count} doc(s) exist only in Docs (RU): {string.Join(", ", ruOnly)}");
            }

            if (enOnly.Count > 0)
            {
                issues++;
                Debug.LogWarning(
                    $"[PackageHealthCheck] {enOnly.Count} doc(s) exist only in DocsEn (EN): {string.Join(", ", enOnly)}");
            }

            return issues;
        }

        private static HashSet<string> CollectRelativeDocs(string root)
        {
            var result = new HashSet<string>();
            foreach (string file in Directory.GetFiles(root, "*.md", SearchOption.AllDirectories))
            {
                string relative = file.Substring(root.Length + 1).Replace('\\', '/');
                result.Add(relative);
            }

            return result;
        }
    }
}
