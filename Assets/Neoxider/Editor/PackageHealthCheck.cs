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

        /// <summary>
        ///     One file that repeats the package version, and the literal text it must contain.
        ///     <c>{0}</c> is replaced with the version read from package.json.
        /// </summary>
        public readonly struct VersionMention
        {
            /// <summary>Path relative to the project root — Unity's working directory.</summary>
            public readonly string RelativePath;

            /// <summary>Human-readable name used in the failure message.</summary>
            public readonly string Label;

            private readonly string _format;

            public VersionMention(string relativePath, string format, string label)
            {
                RelativePath = relativePath;
                _format = format;
                Label = label;
            }

            /// <summary>The exact text this file must contain for <paramref name="version" />.</summary>
            public string Needle(string version)
            {
                return string.Format(_format, version);
            }
        }

        // WHY: every place the version is repeated, enumerated once. The previous shape spelled three
        // files out as separate calls and simply omitted the other four — so the repo-root README, the
        // public landing page on GitHub, still advertised 10.1.0 while v10.3.0 and v10.4.0 shipped.
        // A new file that carries the version goes in this list and nowhere else.
        public static readonly VersionMention[] VersionMentions =
        {
            new VersionMention("README.md", "version-{0}-", "repo-root README.md badge"),
            new VersionMention(Root + "/README.md", "version-{0}-", "package README.md badge"),
            new VersionMention(Root + "/PROJECT_SUMMARY.md", "`{0}`", "PROJECT_SUMMARY.md"),
            new VersionMention(Root + "/CHANGELOG.md", "[{0}]", "CHANGELOG.md entry"),
            new VersionMention(Root + "/Docs/README.md", "`v{0}`", "Docs/README.md entry point"),
            new VersionMention(Root + "/Docs/PackageCompatibility.md", "version: {0}",
                "Docs/PackageCompatibility.md table"),
            new VersionMention(Root + "/Skill/neoxider-tools/SKILL.md", "version: {0}",
                "Skill/neoxider-tools/SKILL.md metadata")
        };

        private static readonly Regex NeoDocPattern = new Regex(
            "NeoDoc\\(\"([^\"]+)\"\\)", RegexOptions.Compiled);

        private static readonly Regex MarkdownLinkPattern = new Regex(
            @"\]\(([^)#\s]+\.md)\)", RegexOptions.Compiled);

        [MenuItem("Neoxider/Health Check", false, 420)]
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

        /// <summary>
        ///     Returns one line per version-parity problem; empty when every file in
        ///     <see cref="VersionMentions" /> advertises the version from package.json. Pure file I/O, so
        ///     the EditMode suite can assert on it — a menu item nobody clicks is precisely how the
        ///     repo-root README stayed three releases behind.
        /// </summary>
        public static List<string> FindVersionParityProblems()
        {
            var problems = new List<string>();

            string packageJsonPath = Path.Combine(Root, "package.json");
            if (!File.Exists(packageJsonPath))
            {
                problems.Add($"'{packageJsonPath}' not found — cannot determine the package version.");
                return problems;
            }

            Match versionMatch = Regex.Match(File.ReadAllText(packageJsonPath), "\"version\"\\s*:\\s*\"([^\"]+)\"");
            if (!versionMatch.Success)
            {
                problems.Add($"'{packageJsonPath}' has no version field.");
                return problems;
            }

            string version = versionMatch.Groups[1].Value;

            foreach (VersionMention mention in VersionMentions)
            {
                // WHY: a missing file is a problem, not a pass. The old check returned "fine" for anything
                // it could not find, so a rename would have silenced it instead of failing it.
                if (!File.Exists(mention.RelativePath))
                {
                    problems.Add(
                        $"{mention.Label}: '{mention.RelativePath}' does not exist — the version list is stale.");
                    continue;
                }

                string needle = mention.Needle(version);
                if (!File.ReadAllText(mention.RelativePath).Contains(needle))
                {
                    problems.Add(
                        $"{mention.Label} ('{mention.RelativePath}') does not advertise package version " +
                        $"{version} — expected to find \"{needle}\".");
                }
            }

            return problems;
        }

        private static int CheckVersionParity()
        {
            List<string> problems = FindVersionParityProblems();
            foreach (string problem in problems)
            {
                Debug.LogWarning($"[PackageHealthCheck] {problem}");
            }

            return problems.Count;
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
