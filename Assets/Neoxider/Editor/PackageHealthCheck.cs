using System;
using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;
using UnityEditor;
using UnityEngine;

namespace Neo.Editor
{
    /// <summary>
    ///     Release-hygiene checks: the package version must match everywhere it is written, every
    ///     [NeoDoc] path must resolve to a real page under Docs/, every public component type must
    ///     carry a [NeoDoc] link at all (9.8.2 found 32+7 such gaps that path checking alone could not
    ///     see), and relative .md links inside Docs/ must not be dead. Run before tagging a release —
    ///     each of these drifts has shipped in the past.
    /// </summary>
    public static class PackageHealthCheck
    {
        private const string Root = "Assets/Neoxider";

        private static readonly Regex NeoDocPattern = new Regex(
            "NeoDoc\\(\"([^\"]+)\"\\)", RegexOptions.Compiled);

        private static readonly Regex MarkdownLinkPattern = new Regex(
            @"\]\(([^)#\s]+\.md)\)", RegexOptions.Compiled);

        [MenuItem("Tools/Neoxider/Package Health Check")]
        public static void Run()
        {
            int issues = 0;
            issues += CheckVersionParity();
            issues += CheckNeoDocPathsResolve();
            issues += CheckComponentsCarryNeoDoc();
            issues += CheckDocsRelativeLinksResolve();

            if (issues == 0)
            {
                Debug.Log(
                    "[PackageHealthCheck] OK: versions in sync, all [NeoDoc] paths resolve, every public " +
                    "component links a doc page, no dead links inside Docs/.");
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

        // WHY: Every NeoDoc attribute's relative path must point at an existing page under Docs/.
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

        // WHY: 9.8.2 lesson: a component can have a doc page but no [NeoDoc] attribute pointing at it —
        // path checking alone never sees that. Flag every public, non-abstract MonoBehaviour /
        // ScriptableObject compiled into a Neo.* runtime assembly that carries no [NeoDoc].
        private static int CheckComponentsCarryNeoDoc()
        {
            var offenders = new SortedSet<string>();
            CollectUndocumented(TypeCache.GetTypesDerivedFrom<MonoBehaviour>(), offenders);
            CollectUndocumented(TypeCache.GetTypesDerivedFrom<ScriptableObject>(), offenders);

            if (offenders.Count == 0)
            {
                return 0;
            }

            foreach (string offender in offenders)
            {
                Debug.LogWarning($"[PackageHealthCheck] {offender} has no [NeoDoc] attribute (no doc link in the Inspector).");
            }

            return 1;
        }

        private static void CollectUndocumented(IEnumerable<System.Type> types, SortedSet<string> offenders)
        {
            foreach (System.Type type in types)
            {
                if (!type.IsPublic || type.IsAbstract || type.IsGenericTypeDefinition)
                {
                    continue;
                }

                string assembly = type.Assembly.GetName().Name;
                if (!assembly.StartsWith("Neo.") ||
                    assembly.Contains("Editor") || assembly.Contains("Tests") || assembly.Contains("Demo"))
                {
                    continue;
                }

                if (typeof(UnityEditor.Editor).IsAssignableFrom(type) ||
                    typeof(EditorWindow).IsAssignableFrom(type))
                {
                    continue;
                }

                if (System.Attribute.IsDefined(type, typeof(NeoDocAttribute), false) ||
                    System.Attribute.IsDefined(type, typeof(System.ObsoleteAttribute), false))
                {
                    continue;
                }

                offenders.Add($"{type.FullName} ({assembly})");
            }
        }

        // WHY: Relative .md links inside Docs/ must point at existing pages (rot has shipped before — six
        // dead links fixed in 9.8.1 alone). URL-encoded spaces (%20) are decoded before checking.
        private static int CheckDocsRelativeLinksResolve()
        {
            string docsRoot = Path.Combine(Root, "Docs");
            if (!Directory.Exists(docsRoot))
            {
                return 0; // WHY: already reported by CheckNeoDocPathsResolve
            }

            int dead = 0;
            foreach (string file in Directory.GetFiles(docsRoot, "*.md", SearchOption.AllDirectories))
            {
                string dir = Path.GetDirectoryName(file) ?? docsRoot;
                foreach (Match m in MarkdownLinkPattern.Matches(File.ReadAllText(file)))
                {
                    string link = m.Groups[1].Value;
                    if (link.StartsWith("http://") || link.StartsWith("https://"))
                    {
                        continue;
                    }

                    string decoded = Uri.UnescapeDataString(link);
                    string target = Path.GetFullPath(Path.Combine(dir, decoded));
                    if (!File.Exists(target))
                    {
                        Debug.LogWarning(
                            $"[PackageHealthCheck] Dead doc link '{link}' in {file.Replace('\\', '/')}.");
                        dead++;
                    }
                }
            }

            return dead > 0 ? 1 : 0;
        }
    }
}
