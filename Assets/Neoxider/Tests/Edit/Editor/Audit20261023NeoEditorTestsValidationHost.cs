using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;
using Microsoft.Win32;
using Neo.Save;
using UnityEditor;
using UnityEditor.PackageManager;
using UnityEngine;
using PackageInfo = UnityEditor.PackageManager.PackageInfo;

namespace Neo.Editor.Tests
{
    /// <summary>Explicit entrypoints for the final isolated host. No probe/import runs on ordinary package load.</summary>
    public static partial class Audit20261023NeoEditorTestsValidationHost
    {
        private const string PendingKey = "Neo.Audit20261023.EditorTests.ValidationHost.Pending";
        private const string HostSegment = "/Local/Audits/assembly-2026-10-23/unity/final/";
        private static ImportState _pending;
        private static readonly Regex GuidReference = new Regex(@"guid:\s*([0-9a-fA-F]{32})", RegexOptions.Compiled);

        [Serializable]
        private sealed class EnvironmentRecord
        {
            public string companyName;
            public string productName;
            public string projectPath;
            public string persistentDataPath;
            public string platform;
            public bool isEditor;
            public string unityVersion;
            public string playerPrefsNamespace;
            public string sentinelRegistryValueName;
            public string sentinelRegistryEncoding;
            public bool sentinelRoundTrip;
            public bool sentinelRemoved;
            public string saveProviderType;
            public string[] effectiveSaveRoots;
        }

        [Serializable]
        private sealed class AssetRecord
        {
            public string path;
            public string guid;
            public int dependencyCount;
        }

        [Serializable]
        private sealed class SampleRecord
        {
            public string displayName;
            public string sourcePath;
            public string importedPath;
            public bool importCalled;
            public bool importReturnObserved;
            public bool importAccepted;
            public int assetCount;
            public int sceneCount;
            public int prefabCount;
            public int scriptCount;
            public int checkedGuidReferences;
            public AssetRecord[] assets;
        }

        [Serializable]
        private sealed class Receipt
        {
            public string action;
            public string status;
            public string utc;
            public string error;
            public EnvironmentRecord environment;
            public string packageName;
            public string packageVersion;
            public string packageSource;
            public string packageResolvedPath;
            public SampleRecord[] samples;
            public DiscoveryRecord discovery;
        }

        [Serializable]
        private sealed class ImportState
        {
            public string output;
            public double deadlineEditorSeconds;
            public int nextSample;
            public Receipt receipt;
        }

        /// <summary>executeMethod entrypoint; exits batch mode after emitting the environment/persistence receipt.</summary>
        public static void ProbeEnvironment()
        {
            string output = null;
            Receipt receipt = new Receipt { action = "environment", status = "failed" };
            try
            {
                output = RequireOwnedHost();
                receipt.environment = ProbeOwnedPersistence();
                receipt.status = "complete";
            }
            catch (Exception exception) { receipt.error = exception.ToString(); }
            Finish(output, receipt);
        }

        /// <summary>Imports all three manifest entries through the public UPM Sample API; resumes after domain reload.</summary>
        public static void ImportPackageSamples()
        {
            string output = null;
            Receipt receipt = new Receipt { action = "upm-sample-import", status = "failed" };
            try
            {
                output = RequireOwnedHost();
                if (!string.IsNullOrEmpty(SessionState.GetString(PendingKey, string.Empty)))
                    throw new InvalidOperationException("An import is already pending in this host.");
                receipt.environment = ProbeOwnedPersistence();
                PackageInfo package = Audit20261023NeoEditorTestsPaths.RegisteredPackage;
                if (package == null || (package.source.ToString() != "Local" && package.source.ToString() != "Embedded"))
                    throw new InvalidOperationException("Register the frozen local com.neoxider.tools package before importing samples.");
                if (!string.Equals(Path.GetFullPath(package.resolvedPath), Path.GetFullPath(Argument("-neoValidationPackageRoot")),
                    StringComparison.OrdinalIgnoreCase))
                    throw new InvalidOperationException("Registered package root differs from the operator's explicit frozen package root.");
                Audit20261023NeoEditorTestsPaths.PackageManifest manifest = Audit20261023NeoEditorTestsPaths.Manifest;
                if (manifest.version != "11.0.0" || package.version != manifest.version || manifest.samples.Length != 3)
                    throw new InvalidOperationException("Expected the actual 11.0.0 package and all three manifest samples.");
                receipt.packageName = package.name;
                receipt.packageVersion = package.version;
                receipt.packageSource = package.source.ToString();
                receipt.packageResolvedPath = package.resolvedPath;
                receipt.samples = new SampleRecord[manifest.samples.Length];
                for (int index = 0; index < manifest.samples.Length; index++)
                {
                    Audit20261023NeoEditorTestsPaths.SampleManifest sample = manifest.samples[index];
                    string imported = Audit20261023NeoEditorTestsPaths.ImportedRoot(manifest, sample);
                    if (Directory.Exists(imported) || File.Exists(imported + ".meta"))
                        throw new InvalidOperationException("A fresh sample import destination is required: " + imported);
                    receipt.samples[index] = new SampleRecord
                    {
                        displayName = sample.displayName, sourcePath = sample.path, importedPath = imported
                    };
                }
                receipt.status = "importing";
                _pending = new ImportState
                {
                    output = output, deadlineEditorSeconds = EditorApplication.timeSinceStartup + 600d, receipt = receipt
                };
                PersistPending();
                HookImportUpdate();
            }
            catch (Exception exception)
            {
                receipt.error = exception.ToString();
                Finish(output, receipt);
            }
        }

        private static string RequireOwnedHost()
        {
            string project = Path.GetFullPath(Path.Combine(Application.dataPath, "..")).Replace('\\', '/');
            if (project.IndexOf(HostSegment, StringComparison.OrdinalIgnoreCase) < 0)
                throw new InvalidOperationException("This entrypoint requires an isolated final validation host: " + project);
            string company = Argument("-neoValidationCompany");
            string product = Argument("-neoValidationProduct");
            if (!Regex.IsMatch(company, @"^NeoQA_20261023_[A-Za-z0-9_]+$") ||
                !Regex.IsMatch(product, @"^NeoTools_[A-Za-z0-9_]+$") ||
                Application.companyName != company || Application.productName != product)
                throw new InvalidOperationException("Set and explicitly confirm the unique task-owned company/product BEFORE first import.");
            string persistence = Path.GetFullPath(Application.persistentDataPath).Replace('\\', '/').TrimEnd('/');
            if (!persistence.EndsWith("/" + company + "/" + product, StringComparison.OrdinalIgnoreCase))
                throw new InvalidOperationException("Effective persistentDataPath does not match this host's unique identity: " + persistence);
            string output = Path.GetFullPath(Argument("-neoValidationOutput"));
            if (!IsWithin(output, project)) throw new InvalidOperationException("Receipt output must remain inside this isolated project.");
            return output;
        }

        private static EnvironmentRecord ProbeOwnedPersistence()
        {
            EnvironmentRecord record = new EnvironmentRecord
            {
                companyName = Application.companyName, productName = Application.productName,
                projectPath = Path.GetFullPath(Path.Combine(Application.dataPath, "..")),
                persistentDataPath = Path.GetFullPath(Application.persistentDataPath),
                platform = Application.platform.ToString(), isEditor = Application.isEditor,
                unityVersion = Application.unityVersion
            };
            if (Application.platform != RuntimePlatform.WindowsEditor)
                throw new PlatformNotSupportedException("This entrypoint verifies the actual Windows Editor registry. Other platforms require their own native probe.");
            ISaveProvider provider = SaveProvider.CurrentProvider;
            record.saveProviderType = provider.GetType().FullName;
            List<string> roots = new List<string> { record.persistentDataPath };
            if (provider is FileSaveProvider fileProvider)
            {
                FieldInfo pathField = typeof(FileSaveProvider).GetField("_filePath", BindingFlags.Instance | BindingFlags.NonPublic);
                if (pathField == null) throw new MissingFieldException("FileSaveProvider._filePath");
                string file = Path.GetFullPath((string)pathField.GetValue(fileProvider));
                if (!IsWithin(file, record.persistentDataPath))
                    throw new InvalidOperationException("The active file provider redirects outside the isolated persistence root: " + file);
                roots.Add(file);
            }
            else if (!(provider is PlayerPrefsSaveProvider))
                throw new InvalidOperationException("Unrecognized active provider; prove its effective roots before mutating fixtures: " + record.saveProviderType);
            record.effectiveSaveRoots = roots.ToArray();
            string key = "NeoQAProbe_" + Guid.NewGuid().ToString("N");
            string value = Guid.NewGuid().ToString("N");
            string editorBranch = "Software\\Unity\\UnityEditor\\" + record.companyName + "\\" + record.productName;
            string playerBranch = "Software\\" + record.companyName + "\\" + record.productName;
            try
            {
                PlayerPrefs.SetString(key, value);
                PlayerPrefs.Save();
                record.sentinelRoundTrip = PlayerPrefs.GetString(key, string.Empty) == value;
                if (!record.sentinelRoundTrip) throw new InvalidOperationException("Owned preference sentinel did not roundtrip.");
                int matches = 0;
                foreach (string branch in new[] { editorBranch, playerBranch })
                {
                    using (RegistryKey registry = Registry.CurrentUser.OpenSubKey(branch))
                    {
                        if (registry == null) continue;
                        foreach (string name in registry.GetValueNames())
                        {
                            if (name != key && !name.StartsWith(key + "_h", StringComparison.Ordinal)) continue;
                            object raw = registry.GetValue(name);
                            string stored = raw is byte[] bytes ? Encoding.UTF8.GetString(bytes).TrimEnd('\0') : raw as string;
                            string encoding = raw is byte[] ? "UTF-8 binary" : "registry string";
                            if (stored != value && raw is byte[] utf16)
                            {
                                stored = Encoding.Unicode.GetString(utf16).TrimEnd('\0');
                                encoding = "UTF-16 binary";
                            }
                            if (stored != value) continue;
                            matches++;
                            record.playerPrefsNamespace = "HKEY_CURRENT_USER\\" + branch;
                            record.sentinelRegistryValueName = name;
                            record.sentinelRegistryEncoding = encoding;
                        }
                    }
                }
                if (matches != 1) throw new InvalidOperationException("Could not uniquely observe the sentinel in the actual Windows PlayerPrefs branch.");
            }
            finally
            {
                PlayerPrefs.DeleteKey(key);
                PlayerPrefs.Save();
                record.sentinelRemoved = !PlayerPrefs.HasKey(key);
                if (record.playerPrefsNamespace != null)
                {
                    string branch = record.playerPrefsNamespace.Substring("HKEY_CURRENT_USER\\".Length);
                    using (RegistryKey registry = Registry.CurrentUser.OpenSubKey(branch))
                        record.sentinelRemoved &= registry == null || registry.GetValue(record.sentinelRegistryValueName) == null;
                }
                if (!record.sentinelRemoved) throw new InvalidOperationException("Owned preference sentinel cleanup failed.");
            }
            return record;
        }

        [InitializeOnLoadMethod]
        private static void ResumePendingImport()
        {
            string raw = SessionState.GetString(PendingKey, string.Empty);
            if (string.IsNullOrEmpty(raw)) return;
            _pending = JsonUtility.FromJson<ImportState>(raw);
            HookImportUpdate();
        }

        private static void HookImportUpdate()
        {
            EditorApplication.update -= ImportUpdate;
            EditorApplication.update += ImportUpdate;
        }

        private static void PersistPending()
        {
            SessionState.SetString(PendingKey, JsonUtility.ToJson(_pending));
            _pending.receipt.utc = DateTime.UtcNow.ToString("O");
            WriteReceipt(_pending.output, JsonUtility.ToJson(_pending.receipt, true));
        }

        private static void ImportUpdate()
        {
            try
            {
                if (_pending == null) throw new InvalidOperationException("Missing pending sample import state.");
                string output = RequireOwnedHost();
                if (output != _pending.output) throw new InvalidOperationException("Pending receipt path changed.");
                if (EditorApplication.timeSinceStartup >= _pending.deadlineEditorSeconds)
                    throw new TimeoutException("UPM sample import exceeded the ten-minute real-time deadline.");
                if (EditorApplication.isCompiling || EditorApplication.isUpdating) return;
                if (_pending.nextSample == _pending.receipt.samples.Length)
                {
                    // Integration scenes may reference sibling samples (for example the Pages PM prefab).
                    // Validate the complete imported graph, preserving every native/GUID assertion.
                    foreach (SampleRecord imported in _pending.receipt.samples)
                    {
                        InspectImportedAssets(imported);
                    }

                    _pending.receipt.status = "complete";
                    CompleteImport();
                    return;
                }
                SampleRecord record = _pending.receipt.samples[_pending.nextSample];
                object sample = FindUpmSample(_pending.receipt.packageVersion, record.displayName);
                Type type = sample.GetType();
                string actualPath = (string)Property(type, "importPath").GetValue(sample);
                if (Path.GetFullPath(actualPath) != Path.GetFullPath(record.importedPath))
                    throw new InvalidOperationException("UPM import path differs from the current-version manifest destination: " + actualPath);
                if (!record.importCalled)
                {
                    Type optionsType = type.GetNestedType("ImportOptions", BindingFlags.Public);
                    if (optionsType == null) throw new MissingMemberException(type.FullName, "ImportOptions");
                    MethodInfo import = type.GetMethod("Import", new[] { optionsType });
                    if (import == null) throw new MissingMethodException(type.FullName, "Import");
                    record.importCalled = true;
                    PersistPending(); // A script import can reload this domain before the call returns.
                    record.importAccepted = (bool)import.Invoke(sample, new[] { Enum.Parse(optionsType, "None") });
                    record.importReturnObserved = true;
                    if (!record.importAccepted) throw new InvalidOperationException("Native UPM Sample.Import rejected " + record.displayName);
                    PersistPending();
                    return;
                }
                if (!(bool)Property(type, "isImported").GetValue(sample) || !AssetDatabase.IsValidFolder(record.importedPath)) return;
                _pending.nextSample++;
                PersistPending();
            }
            catch (Exception exception)
            {
                if (_pending == null)
                {
                    EditorApplication.update -= ImportUpdate;
                    Debug.LogError(exception);
                    if (Application.isBatchMode) EditorApplication.Exit(1);
                    return;
                }
                _pending.receipt.status = "failed";
                _pending.receipt.error = exception.ToString();
                CompleteImport();
            }
        }

        private static object FindUpmSample(string version, string displayName)
        {
            const string sampleTypeName = "UnityEditor.PackageManager.UI.Sample";
            Type type = typeof(UnityEditor.Editor).Assembly.GetType(sampleTypeName, false)
                ?? Type.GetType(sampleTypeName + ", UnityEditor", false)
                ?? Type.GetType(sampleTypeName + ", UnityEditor.PackageManagerUIModule", false);
            if (type == null) throw new TypeLoadException("Public UnityEditor.PackageManager.UI.Sample API is unavailable.");
            MethodInfo find = type.GetMethod("FindByPackage", BindingFlags.Public | BindingFlags.Static,
                null, new[] { typeof(string), typeof(string) }, null);
            if (find == null) throw new MissingMethodException(type.FullName, "FindByPackage");
            IEnumerable samples = (IEnumerable)find.Invoke(null, new object[] { Audit20261023NeoEditorTestsPaths.PackageName, version });
            object result = null;
            int count = 0;
            foreach (object sample in samples)
            {
                count++;
                if ((string)Property(type, "displayName").GetValue(sample) != displayName) continue;
                if (result != null) throw new InvalidDataException("Duplicate UPM sample identity: " + displayName);
                result = sample;
            }
            if (count != 3 || result == null) throw new InvalidDataException("UPM did not discover all three declared sample identities.");
            return result;
        }

        private static PropertyInfo Property(Type type, string name)
        {
            return type.GetProperty(name, BindingFlags.Public | BindingFlags.Instance)
                ?? throw new MissingMemberException(type.FullName, name);
        }

        private static void InspectImportedAssets(SampleRecord record)
        {
            string[] guids = AssetDatabase.FindAssets(string.Empty, new[] { record.importedPath });
            List<AssetRecord> assets = new List<AssetRecord>();
            foreach (string guid in guids)
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                if (AssetDatabase.IsValidFolder(path)) continue;
                if (string.IsNullOrEmpty(path) || AssetDatabase.AssetPathToGUID(path) != guid)
                    throw new InvalidDataException("Imported asset GUID did not resolve: " + guid);
                string[] dependencies = AssetDatabase.GetDependencies(path, true);
                foreach (string dependency in dependencies)
                    if (string.IsNullOrEmpty(AssetDatabase.AssetPathToGUID(dependency)))
                        throw new InvalidDataException("Unresolved imported dependency: " + path + " -> " + dependency);
                record.assetCount++;
                string extension = Path.GetExtension(path).ToLowerInvariant();
                if (extension == ".unity") record.sceneCount++;
                if (extension == ".cs") record.scriptCount++;
                if (extension == ".prefab")
                {
                    record.prefabCount++;
                    GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
                    if (prefab == null || SampleAssetValidationTests.CountMissingMonoBehavioursRecursive(prefab) != 0)
                        throw new InvalidDataException("Imported prefab failed native script resolution: " + path);
                }
                if (extension == ".prefab" || extension == ".unity" || extension == ".asset")
                {
                    string text = File.ReadAllText(path);
                    if (text.StartsWith("%YAML", StringComparison.Ordinal))
                    {
                        foreach (Match reference in GuidReference.Matches(text))
                        {
                            string referencedGuid = reference.Groups[1].Value;
                            if (referencedGuid.StartsWith("0000000000000000", StringComparison.Ordinal)) continue;
                            record.checkedGuidReferences++;
                            if (string.IsNullOrEmpty(AssetDatabase.GUIDToAssetPath(referencedGuid)))
                                throw new InvalidDataException("Unresolved imported YAML reference: " + path + " -> " + referencedGuid);
                        }
                    }
                }
                assets.Add(new AssetRecord { path = path, guid = guid, dependencyCount = dependencies.Length });
            }
            if (record.assetCount == 0 || record.scriptCount == 0 || record.checkedGuidReferences == 0)
                throw new InvalidDataException("Import produced no meaningful asset/script/reference evidence: " + record.importedPath);
            record.assets = assets.ToArray();
        }

        private static void CompleteImport()
        {
            EditorApplication.update -= ImportUpdate;
            ImportState state = _pending;
            _pending = null;
            SessionState.EraseString(PendingKey);
            Finish(state.output, state.receipt);
        }

        private static void Finish(string output, Receipt receipt)
        {
            receipt.utc = DateTime.UtcNow.ToString("O");
            try
            {
                string json = JsonUtility.ToJson(receipt, true);
                if (output != null) WriteReceipt(output, json);
                if (receipt.status == "complete") Debug.Log("NEO_VALIDATION_RECEIPT " + json);
                else Debug.LogError("NEO_VALIDATION_RECEIPT " + json);
            }
            catch (Exception exception)
            {
                receipt.status = "failed";
                receipt.error = "Receipt output failed: " + exception;
                Debug.LogError("NEO_VALIDATION_RECEIPT " + JsonUtility.ToJson(receipt, true));
            }
            finally
            {
                if (Application.isBatchMode) EditorApplication.Exit(receipt.status == "complete" ? 0 : 1);
            }
        }

        private static void WriteReceipt(string output, string json)
        {
            Directory.CreateDirectory(Path.GetDirectoryName(output));
            File.WriteAllText(output, json + "\n", new UTF8Encoding(false));
        }

        private static string Argument(string name)
        {
            string[] arguments = Environment.GetCommandLineArgs();
            for (int index = 0; index < arguments.Length - 1; index++)
                if (arguments[index] == name) return arguments[index + 1];
            throw new ArgumentException("Required validation argument: " + name);
        }

        private static bool IsWithin(string path, string root)
        {
            string normalizedRoot = Path.GetFullPath(root).TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar) + Path.DirectorySeparatorChar;
            return Path.GetFullPath(path).StartsWith(normalizedRoot, StringComparison.OrdinalIgnoreCase);
        }
    }
}
