using System;
using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;
using Neo.Core.Level;
using Neo.Progression;
using Neo.Quest;
using Neo.Shop;
using Neo.Tools;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;
using PackageInfo = UnityEditor.PackageManager.PackageInfo;

namespace Neo.Editor.Tests
{
    /// <summary>
    ///     Sample scenes and prefabs must not persist under a library default SaveProvider key: an imported demo would
    ///     read and overwrite the save of the user's own game (inventory, wallet, shop, best score, levels...). The scan reads
    ///     the YAML of every active sample root, so it also covers the hidden Samples~ release layout, and it applies the
    ///     overrides of prefab instances to the components of their source prefabs (the --System-- prefab's Money).
    /// </summary>
    public sealed class SampleSceneSaveKeyTests
    {
        // WHY: TimeReward is obsolete but still shipped, and its default key is the same as CooldownReward's.
#pragma warning disable CS0618
        /// <summary>
        ///     Components that persist on their own: save-key field (a key or a key prefix), library default key, the
        ///     serialized switch that turns persistence on (null = always on) and its default, and whether a blank key
        ///     falls back to the default. "Always on" includes components that read their key on start and write it on
        ///     gameplay calls (ScoreManager.Add raises and saves the best score, LevelManager loads its maps). Device
        ///     preferences (AMSettings, GameSettingsComponent) are left out on purpose: a demo shares them with the game.
        /// </summary>
        private static readonly SaveKeyRule[] Rules =
        {
            new(typeof(InventoryComponent), "_saveKey", "Inventory_Default", "_autoSave", true, false),
            new(typeof(LevelComponent), "_saveKey", "Neo.Core.Level.Profile", "_autoSave", true, false),
            new(typeof(ProgressionManager), "_saveKey", "ProgressionV2.Profile", "_autoSave", true, true),
            new(typeof(QuestManager), "_saveKey", "Settings_Quests", "_autoSave", true, false),
            new(typeof(Counter), "_saveKey", "Counter", "_saveEnabled", false, false),
            new(typeof(Selector), "_saveKey", "Selector", "_saveEnabled", false, false),
            new(typeof(TimerObject), "saveKey", "TimerObject", "saveProgress", false, false),
            new(typeof(Money), "_moneySave", "Money", "_persistMoney", true, false),
            new(typeof(Neo.Shop.Shop), "_keySave", "Shop", null, true, false),
            new(typeof(ScoreManager), "_keySave", "BestScore", null, true, false),
            new(typeof(Neo.Level.LevelManager), "_saveKey", "LevelManager", null, true, false),
            new(typeof(EquipmentManager), "_saveKeyPrefix", "Equip_", "_persist", true, false),
            new(typeof(Leaderboard), "playerSaveKey", "LeaderboardPlayer", null, true, false),
            new(typeof(Neo.Bonus.Box), "_saveName", "BoxPrize", null, true, false),
            new(typeof(Neo.Bonus.Collection), "_saveKeyPrefix", "Collection_", null, true, false),
            new(typeof(Neo.Bonus.CooldownReward), "_addKey", "Bonus1", null, true, true),
            new(typeof(Neo.Bonus.TimeReward), "_addKey", "Bonus1", null, true, true)
        };
#pragma warning restore CS0618

        /// <summary>
        ///     "&lt;sample&gt;/&lt;path inside the sample&gt; | &lt;Component&gt;.&lt;field&gt;" entries that still persist
        ///     under a library default key, one component each. The list may only shrink: an entry whose file was scanned
        ///     and is clean now fails the test until it is removed here, and a second such component in the same file fails
        ///     as a new offender.
        /// </summary>
        private static readonly string[] KnownOffenders = Array.Empty<string>();

        [Test]
        public void SampleScenes_DoNotPersistUnderLibraryDefaultSaveKeys()
        {
            List<SampleRoot> roots = ActiveSampleRoots();
            if (roots.Count == 0)
            {
                Assert.Ignore("No NeoxiderTools sample root (Samples, Samples~ or imported) is available.");
            }

            Dictionary<string, SaveKeyRule> rules = RulesByScriptGuid();
            Dictionary<string, string> sampleGuids = PrefabGuidsOnDisk(roots);
            YamlSaveKeyScanner scanner = new(rules, guid => ReadPrefabYaml(guid, sampleGuids));
            Dictionary<string, List<SaveKeyFinding>> findings = new(StringComparer.Ordinal);
            HashSet<string> scannedFiles = new(StringComparer.Ordinal);
            foreach (SampleRoot root in roots)
            {
                foreach (string file in SampleYamlFiles(root.DiskPath))
                {
                    string label = root.Name + "/" + Relative(root.DiskPath, file);
                    if (!scannedFiles.Add(label))
                    {
                        continue;
                    }

                    foreach (SaveKeyFinding finding in scanner.Scan(label, File.ReadAllText(file)))
                    {
                        if (!findings.TryGetValue(finding.Id, out List<SaveKeyFinding> same))
                        {
                            same = new List<SaveKeyFinding>();
                            findings.Add(finding.Id, same);
                        }

                        same.Add(finding);
                    }
                }
            }

            Assert.That(scannedFiles.Count, Is.GreaterThan(0), "No sample scene or prefab YAML was scanned.");
            Assert.That(scanner.InspectedComponents, Is.GreaterThan(0),
                "No persisting component was recognized; the script GUID resolution is broken.");
            TestContext.WriteLine("Scanned " + scannedFiles.Count + " sample files, " + scanner.InspectedComponents +
                                  " persisting components.");

            HashSet<string> known = new(KnownOffenders, StringComparer.Ordinal);
            List<string> failures = new();
            foreach (KeyValuePair<string, List<SaveKeyFinding>> pair in findings)
            {
                bool isKnown = known.Contains(pair.Key);
                if (isKnown && pair.Value.Count == 1)
                {
                    continue;
                }

                foreach (SaveKeyFinding finding in pair.Value)
                {
                    failures.Add(finding.Describe() + (isKnown
                        ? " - " + pair.Value.Count + " components, the KnownOffenders entry covers one"
                        : string.Empty));
                }
            }

            foreach (string offender in KnownOffenders)
            {
                string file = offender.Substring(0, offender.IndexOf(" | ", StringComparison.Ordinal));
                if (scannedFiles.Contains(file) && !findings.ContainsKey(offender))
                {
                    failures.Add("Known offender is clean now, remove it from KnownOffenders: " + offender);
                }
            }

            failures.Sort(StringComparer.Ordinal);
            if (failures.Count > 0)
            {
                Assert.Fail("Sample content persists under library default save keys (give the demo its own " +
                            "'NeoDemo.<Demo>.<Data>' key through its builder, or switch persistence off):\n" +
                            string.Join("\n", failures));
            }
        }

        [Test]
        public void SaveKeyRules_MatchComponentDefaults()
        {
            List<string> failures = new();
            foreach (SaveKeyRule rule in Rules)
            {
                GameObject host = new(nameof(SaveKeyRules_MatchComponentDefaults) + " " + rule.Component.Name);
                try
                {
                    Component component = host.AddComponent(rule.Component);
                    using SerializedObject serialized = new(component);
                    SerializedProperty key = serialized.FindProperty(rule.KeyField);
                    if (key == null || key.propertyType != SerializedPropertyType.String)
                    {
                        failures.Add(rule.Id + ": no serialized string field.");
                    }
                    else if (key.stringValue != rule.DefaultKey)
                    {
                        failures.Add(rule.Id + ": default key is '" + key.stringValue + "', the rule says '" +
                                     rule.DefaultKey + "'.");
                    }

                    if (rule.SwitchField == null)
                    {
                        continue;
                    }

                    SerializedProperty toggle = serialized.FindProperty(rule.SwitchField);
                    if (toggle == null || toggle.propertyType != SerializedPropertyType.Boolean)
                    {
                        failures.Add(rule.Component.Name + "." + rule.SwitchField + ": no serialized bool field.");
                    }
                    else if (toggle.boolValue != rule.SwitchDefault)
                    {
                        failures.Add(rule.Component.Name + "." + rule.SwitchField + ": defaults to " + toggle.boolValue +
                                     ", the rule says " + rule.SwitchDefault + ".");
                    }
                }
                finally
                {
                    Object.DestroyImmediate(host);
                }
            }

            Assert.That(failures, Is.Empty, string.Join("\n", failures));
        }

        [Test]
        public void Scanner_FlagsDefaultKeysAndPrefabInstanceOverrides()
        {
            const string scriptGuid = "0123456789abcdef0123456789abcdef";
            const string alwaysOnGuid = "11111111111111111111111111111111";
            const string blankDefaultGuid = "22222222222222222222222222222222";
            const string prefabGuid = "fedcba9876543210fedcba9876543210";
            SaveKeyRule rule = Rules[0];
            SaveKeyRule alwaysOn = Array.Find(Rules, candidate => candidate.Component == typeof(ScoreManager));
            SaveKeyRule blankDefault = Array.Find(Rules, candidate => candidate.Component == typeof(Neo.Bonus.CooldownReward));
            Dictionary<string, SaveKeyRule> rules = new(StringComparer.Ordinal)
            {
                { scriptGuid, rule }, { alwaysOnGuid, alwaysOn }, { blankDefaultGuid, blankDefault }
            };
            string prefab = Document(114, "5", Script(scriptGuid), "  _autoSave: 1", "  _saveKey: Inventory_Default");
            YamlSaveKeyScanner scanner = new(rules, guid => guid == prefabGuid ? prefab : null);
            string scene =
                Document(114, "10", Script(scriptGuid), "  _autoSave: 1", "  _saveKey: Inventory_Default") +
                Document(114, "11", Script(scriptGuid), "  _autoSave: 1", "  _saveKey: NeoDemo.Test.Inventory") +
                Document(114, "12", Script(scriptGuid), "  _autoSave: 0", "  _saveKey: Inventory_Default") +
                Document(114, "13", Script(scriptGuid), "  _otherField: 2", "    _saveKey: nested") +
                Document(114, "14", Script(scriptGuid), "  _autoSave: 1", "  _saveKey: 'Inventory_Default'") +
                Document(114, "15", Script("00000000000000000000000000000000"), "  _saveKey: Inventory_Default") +
                Document(114, "16", Script(alwaysOnGuid), "  _keySave: BestScore") +
                Document(114, "17", Script(alwaysOnGuid), "  _keySave: NeoDemo.Test.BestScore") +
                Document(114, "18", Script(blankDefaultGuid), "  _addKey: ") +
                Document(114, "19", Script(blankDefaultGuid), "  _addKey: NeoDemo.Test.Gift") +
                PrefabInstance("20", prefabGuid, "5", "m_Name", "Inherits the default key") +
                PrefabInstance("21", prefabGuid, "5", "_saveKey", "NeoDemo.Test.Instance") +
                PrefabInstance("22", prefabGuid, "5", "_autoSave", "0");

            List<SaveKeyFinding> findings = scanner.Scan("Test/Scene.unity", scene);
            List<string> flagged = findings.ConvertAll(finding => finding.Where + " " + finding.Rule.Id);

            Assert.That(findings.TrueForAll(finding => finding.File == "Test/Scene.unity"), Is.True);
            Assert.That(flagged, Is.EquivalentTo(new[]
                {
                    "&10 " + rule.Id, "&13 " + rule.Id, "&14 " + rule.Id, "prefab instance &20 (5) " + rule.Id,
                    "&16 " + alwaysOn.Id, "&18 " + blankDefault.Id
                }),
                "A default key, a missing key and switch (the C# defaults apply), a quoted key, an inherited prefab " +
                "key, an always-on component's default key and a blank key that falls back to the default are " +
                "flagged; a demo key, a switched-off component, another script and instance overrides that change " +
                "the key or switch saving off are not.");
            Assert.That(scanner.InspectedComponents, Is.EqualTo(12));
        }

        /// <summary>One Unity YAML document; <paramref name="lines" /> are its body lines below the type line.</summary>
        private static string Document(int classId, string fileId, params string[] lines)
        {
            string type = classId == 114 ? "MonoBehaviour" : "PrefabInstance";
            return "--- !u!" + classId + " &" + fileId + "\n" + type + ":\n  m_ObjectHideFlags: 0\n" +
                   string.Join("\n", lines) + "\n";
        }

        private static string Script(string guid)
        {
            return "  m_Script: {fileID: 11500000, guid: " + guid + ", type: 3}";
        }

        private static string PrefabInstance(string fileId, string prefabGuid, string target, string property,
            string value)
        {
            return Document(1001, fileId,
                "  m_Modification:",
                "    serializedVersion: 3",
                "    m_TransformParent: {fileID: 0}",
                "    m_Modifications:",
                "    - target: {fileID: " + target + ", guid: " + prefabGuid + ", type: 3}",
                "      propertyPath: " + property,
                "      value: " + value,
                "      objectReference: {fileID: 0}",
                "    m_RemovedComponents: []",
                "  m_SourcePrefab: {fileID: 100100000, guid: " + prefabGuid + ", type: 3}");
        }

        private static Dictionary<string, SaveKeyRule> RulesByScriptGuid()
        {
            Dictionary<Type, SaveKeyRule> byType = new();
            foreach (SaveKeyRule rule in Rules)
            {
                byType[rule.Component] = rule;
            }

            Dictionary<string, SaveKeyRule> result = new(StringComparer.Ordinal);
            foreach (MonoScript script in MonoImporter.GetAllRuntimeMonoScripts())
            {
                Type type = script != null ? script.GetClass() : null;
                if (type != null && byType.TryGetValue(type, out SaveKeyRule rule) &&
                    AssetDatabase.TryGetGUIDAndLocalFileIdentifier(script, out string guid, out long _))
                {
                    result[guid] = rule;
                }
            }

            foreach (SaveKeyRule rule in Rules)
            {
                Assert.That(result.ContainsValue(rule), Is.True, "No MonoScript asset found for " + rule.Component.FullName);
            }

            return result;
        }

        private static List<SampleRoot> ActiveSampleRoots()
        {
            Audit20261023NeoEditorTestsPaths.PackageManifest manifest = Audit20261023NeoEditorTestsPaths.Manifest;
            List<SampleRoot> roots = new();
            foreach (Audit20261023NeoEditorTestsPaths.SampleManifest sample in manifest.samples)
            {
                string imported = Audit20261023NeoEditorTestsPaths.ImportedRoot(manifest, sample);
                string development = Audit20261023NeoEditorTestsPaths.DevelopmentRoot(sample);
                string packaged = Path.Combine(Audit20261023NeoEditorTestsPaths.PackageDiskRoot, sample.path);
                foreach (string candidate in new[] { imported, development, packaged })
                {
                    string disk = Path.GetFullPath(Audit20261023NeoEditorTestsPaths.ToDiskPath(candidate));
                    if (Directory.Exists(disk) && !roots.Exists(root => root.DiskPath == disk))
                    {
                        roots.Add(new SampleRoot(sample.displayName, disk));
                    }
                }
            }

            return roots;
        }

        private static IEnumerable<string> SampleYamlFiles(string diskRoot)
        {
            foreach (string pattern in new[] { "*.unity", "*.prefab" })
            {
                foreach (string file in Directory.EnumerateFiles(diskRoot, pattern, SearchOption.AllDirectories))
                {
                    yield return file;
                }
            }
        }

        private static string Relative(string root, string file)
        {
            return file.Substring(root.Length).TrimStart(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar)
                .Replace('\\', '/');
        }

        /// <summary>GUID -> disk path of the prefabs under the sample roots (hidden Samples~ is not in the AssetDatabase).</summary>
        private static Dictionary<string, string> PrefabGuidsOnDisk(List<SampleRoot> roots)
        {
            Dictionary<string, string> result = new(StringComparer.Ordinal);
            foreach (SampleRoot root in roots)
            {
                foreach (string meta in Directory.EnumerateFiles(root.DiskPath, "*.prefab.meta", SearchOption.AllDirectories))
                {
                    foreach (string line in File.ReadLines(meta))
                    {
                        if (line.StartsWith("guid: ", StringComparison.Ordinal))
                        {
                            result.TryAdd(line.Substring("guid: ".Length).Trim(), meta.Substring(0, meta.Length - 5));
                            break;
                        }
                    }
                }
            }

            return result;
        }

        private static string ReadPrefabYaml(string guid, Dictionary<string, string> sampleGuids)
        {
            string disk = null;
            string assetPath = AssetDatabase.GUIDToAssetPath(guid);
            if (!string.IsNullOrEmpty(assetPath))
            {
                disk = AssetDiskPath(assetPath);
            }

            if ((disk == null || !File.Exists(disk)) && !sampleGuids.TryGetValue(guid, out disk))
            {
                return null;
            }

            return File.Exists(disk) ? File.ReadAllText(disk) : null;
        }

        private static string AssetDiskPath(string assetPath)
        {
            if (assetPath.StartsWith("Packages/", StringComparison.Ordinal))
            {
                PackageInfo package = PackageInfo.FindForAssetPath(assetPath);
                if (package != null && !string.IsNullOrEmpty(package.resolvedPath))
                {
                    return Path.Combine(package.resolvedPath, assetPath.Substring(package.assetPath.Length).TrimStart('/'));
                }
            }

            return Path.GetFullPath(Audit20261023NeoEditorTestsPaths.ToDiskPath(assetPath));
        }

        private sealed class SampleRoot
        {
            public SampleRoot(string name, string diskPath)
            {
                Name = name;
                DiskPath = diskPath;
            }

            public string Name { get; }
            public string DiskPath { get; }
        }

        private sealed class SaveKeyRule
        {
            public SaveKeyRule(Type component, string keyField, string defaultKey, string switchField, bool switchDefault,
                bool blankIsDefault)
            {
                Component = component;
                KeyField = keyField;
                DefaultKey = defaultKey;
                SwitchField = switchField;
                SwitchDefault = switchDefault;
                BlankIsDefault = blankIsDefault;
            }

            public Type Component { get; }
            public string KeyField { get; }
            public string DefaultKey { get; }
            public string SwitchField { get; }
            public bool SwitchDefault { get; }
            public bool BlankIsDefault { get; }
            public string Id => Component.Name + "." + KeyField;

            /// <summary>
            ///     True when the serialized fields persist under the library default key. A field missing from the YAML
            ///     was serialized before it existed, so its C# default applies.
            /// </summary>
            public bool UsesDefaultKey(IReadOnlyDictionary<string, string> fields, out string key)
            {
                key = null;
                if (SwitchField != null)
                {
                    bool persists = fields.TryGetValue(SwitchField, out string raw) ? raw.Trim() == "1" : SwitchDefault;
                    if (!persists)
                    {
                        return false;
                    }
                }

                key = fields.TryGetValue(KeyField, out string value) ? YamlSaveKeyScanner.Unquote(value) : DefaultKey;
                return key == DefaultKey || (BlankIsDefault && string.IsNullOrWhiteSpace(key));
            }
        }

        private sealed class SaveKeyFinding
        {
            public SaveKeyFinding(string file, SaveKeyRule rule, string where, string key)
            {
                File = file;
                Rule = rule;
                Where = where;
                Key = key;
            }

            public string File { get; }
            public SaveKeyRule Rule { get; }
            public string Where { get; }
            public string Key { get; }
            public string Id => File + " | " + Rule.Id;

            public string Describe()
            {
                return Id + " = '" + Key + "' (" + Where + ")";
            }
        }

        /// <summary>
        ///     Reads Unity YAML (scenes, prefabs) without the AssetDatabase. MonoBehaviours of a rule's script are
        ///     checked with their top-level fields; a prefab instance checks the components of its source prefab with the
        ///     instance's property overrides applied, and nested prefab instances inside it with their own overrides.
        /// </summary>
        private sealed class YamlSaveKeyScanner
        {
            private const int MaxPrefabDepth = 8;

            private static readonly Regex DocumentHeader = new(@"^--- !u!(\d+) &(-?\d+)( stripped)?[ \t]*\r?$",
                RegexOptions.Multiline | RegexOptions.Compiled);

            private static readonly Regex ScriptGuid = new(@"^  m_Script: \{fileID: 11500000, guid: ([0-9a-f]{32}), type: 3\}",
                RegexOptions.Multiline | RegexOptions.Compiled);

            private static readonly Regex TopLevelField = new(@"^  (\w+):[ ]?(.*?)\r?$",
                RegexOptions.Multiline | RegexOptions.Compiled);

            private static readonly Regex SourcePrefab = new(@"^  m_SourcePrefab: \{fileID: -?\d+, guid: ([0-9a-f]{32}), type: \d\}",
                RegexOptions.Multiline | RegexOptions.Compiled);

            private static readonly Regex Modification = new(
                @"- target: \{fileID: (-?\d+), guid: ([0-9a-f]{32}), type: \d\}\r?\n\s+propertyPath: (.*?)\r?\n\s+value: ?(.*?)\r?\n",
                RegexOptions.Compiled);

            private static readonly Regex RemovedComponent = new(@"^    - \{fileID: (-?\d+), guid: ([0-9a-f]{32}), type: \d\}",
                RegexOptions.Multiline | RegexOptions.Compiled);

            private readonly Func<string, string> _loadPrefab;
            private readonly Dictionary<string, List<YamlDocument>> _prefabs = new(StringComparer.Ordinal);
            private readonly IReadOnlyDictionary<string, SaveKeyRule> _rules;

            public YamlSaveKeyScanner(IReadOnlyDictionary<string, SaveKeyRule> rulesByScriptGuid,
                Func<string, string> loadPrefabYaml)
            {
                _rules = rulesByScriptGuid;
                _loadPrefab = loadPrefabYaml;
            }

            /// <summary>Persisting rule components seen so far (findings or not).</summary>
            public int InspectedComponents { get; private set; }

            public List<SaveKeyFinding> Scan(string file, string yaml)
            {
                List<SaveKeyFinding> findings = new();
                foreach (YamlDocument document in Parse(yaml))
                {
                    if (document.ClassId == 114 && !document.Stripped)
                    {
                        Check(file, document, null, "&" + document.FileId, findings);
                    }
                    else if (document.ClassId == 1001)
                    {
                        CheckInstance(file, document, "prefab instance &" + document.FileId, 0, findings);
                    }
                }

                return findings;
            }

            internal static string Unquote(string raw)
            {
                string value = raw.Trim();
                if (value.Length >= 2 && value[0] == '\'' && value[value.Length - 1] == '\'')
                {
                    return value.Substring(1, value.Length - 2).Replace("''", "'");
                }

                if (value.Length >= 2 && value[0] == '"' && value[value.Length - 1] == '"')
                {
                    return value.Substring(1, value.Length - 2).Replace("\\\"", "\"").Replace("\\\\", "\\");
                }

                return value;
            }

            private void CheckInstance(string file, YamlDocument instance, string where, int depth,
                List<SaveKeyFinding> findings)
            {
                Match source = SourcePrefab.Match(instance.Body);
                if (!source.Success || depth >= MaxPrefabDepth)
                {
                    return;
                }

                string prefabGuid = source.Groups[1].Value;
                List<YamlDocument> prefab = LoadPrefab(prefabGuid);
                if (prefab == null)
                {
                    return;
                }

                Dictionary<string, Dictionary<string, string>> overrides = new(StringComparer.Ordinal);
                foreach (Match modification in Modification.Matches(instance.Body))
                {
                    if (modification.Groups[2].Value != prefabGuid)
                    {
                        continue;
                    }

                    string target = modification.Groups[1].Value;
                    if (!overrides.TryGetValue(target, out Dictionary<string, string> fields))
                    {
                        fields = new Dictionary<string, string>(StringComparer.Ordinal);
                        overrides.Add(target, fields);
                    }

                    fields[modification.Groups[3].Value.Trim()] = modification.Groups[4].Value;
                }

                HashSet<string> removed = new(StringComparer.Ordinal);
                foreach (Match component in RemovedComponent.Matches(instance.Body))
                {
                    if (component.Groups[2].Value == prefabGuid)
                    {
                        removed.Add(component.Groups[1].Value);
                    }
                }

                foreach (YamlDocument document in prefab)
                {
                    if (document.ClassId == 114 && !document.Stripped && !removed.Contains(document.FileId))
                    {
                        overrides.TryGetValue(document.FileId, out Dictionary<string, string> instanceFields);
                        Check(file, document, instanceFields, where + " (" + document.FileId + ")", findings);
                    }
                    else if (document.ClassId == 1001)
                    {
                        // WHY: an outer override of an object inside a nested prefab targets an id Unity derives for it;
                        // only the nested instance's own overrides are applied, which covers the sample prefabs.
                        CheckInstance(file, document, where + " > &" + document.FileId, depth + 1, findings);
                    }
                }
            }

            private void Check(string file, YamlDocument document, IReadOnlyDictionary<string, string> overrides,
                string where, List<SaveKeyFinding> findings)
            {
                Match script = ScriptGuid.Match(document.Body);
                if (!script.Success || !_rules.TryGetValue(script.Groups[1].Value, out SaveKeyRule rule))
                {
                    return;
                }

                InspectedComponents++;
                Dictionary<string, string> fields = Fields(document.Body);
                if (overrides != null)
                {
                    foreach (KeyValuePair<string, string> pair in overrides)
                    {
                        fields[pair.Key] = pair.Value;
                    }
                }

                if (rule.UsesDefaultKey(fields, out string key))
                {
                    findings.Add(new SaveKeyFinding(file, rule, where, key));
                }
            }

            private List<YamlDocument> LoadPrefab(string guid)
            {
                if (_prefabs.TryGetValue(guid, out List<YamlDocument> cached))
                {
                    return cached;
                }

                string yaml = _loadPrefab(guid);
                List<YamlDocument> documents = yaml != null ? Parse(yaml) : null;
                _prefabs[guid] = documents;
                return documents;
            }

            private static Dictionary<string, string> Fields(string body)
            {
                Dictionary<string, string> fields = new(StringComparer.Ordinal);
                foreach (Match match in TopLevelField.Matches(body))
                {
                    fields[match.Groups[1].Value] = match.Groups[2].Value;
                }

                return fields;
            }

            private static List<YamlDocument> Parse(string yaml)
            {
                List<YamlDocument> documents = new();
                MatchCollection headers = DocumentHeader.Matches(yaml);
                for (int i = 0; i < headers.Count; i++)
                {
                    Match header = headers[i];
                    int start = header.Index + header.Length;
                    int end = i + 1 < headers.Count ? headers[i + 1].Index : yaml.Length;
                    documents.Add(new YamlDocument(int.Parse(header.Groups[1].Value), header.Groups[2].Value,
                        header.Groups[3].Success, yaml.Substring(start, end - start)));
                }

                return documents;
            }
        }

        private sealed class YamlDocument
        {
            public YamlDocument(int classId, string fileId, bool stripped, string body)
            {
                ClassId = classId;
                FileId = fileId;
                Stripped = stripped;
                Body = body;
            }

            public int ClassId { get; }
            public string FileId { get; }
            public bool Stripped { get; }
            public string Body { get; }
        }
    }
}
