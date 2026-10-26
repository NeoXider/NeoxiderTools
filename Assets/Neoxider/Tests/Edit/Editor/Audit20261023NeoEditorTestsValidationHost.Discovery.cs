using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Security.Cryptography;
using UnityEditor;
using UnityEditor.Compilation;
using UnityEditor.PackageManager;
using UnityEditor.TestTools.TestRunner.Api;
using UnityEngine;
using CompilationAssembly = UnityEditor.Compilation.Assembly;
using PackageInfo = UnityEditor.PackageManager.PackageInfo;
using ReflectionAssembly = System.Reflection.Assembly;

namespace Neo.Editor.Tests
{
    public static partial class Audit20261023NeoEditorTestsValidationHost
    {
        private const string DiscoveryPendingKey = "Neo.Audit20261023.EditorTests.ValidationHost.Discovery";
        private static DiscoveryState _discoveryPending;
        private static TestRunnerApi _discoveryApi;
        private static bool _discoveryInFlight;

        [Serializable]
        private sealed class DiscoveryRecord
        {
            public string scope = "CompilationPipeline.GetAssemblies(AssembliesType.Editor) plus currently loaded EditMode/PlayMode test trees. No compilation, player graph or test execution is performed; this is not a compile-success receipt.";
            public string api = "TestRunnerApi.RetrieveTestList(TestMode, Action<ITestAdaptor>)";
            public string activeBuildTarget;
            public int domainReloadRestarts;
            public CompiledAssemblyRecord[] compiledEditorAssemblies;
            public LoadedAssemblyRecord[] loadedAssemblies;
            public PackageIdentityRecord[] packages;
            public DiscoveryPackageProvenance neoPackage;
            public ModeDiscoveryRecord[] modes = new ModeDiscoveryRecord[2];
        }

        [Serializable]
        private sealed class CompiledAssemblyRecord
        {
            public string name;
            public string flags;
            public string[] defines;
            public string[] assemblyReferences;
            public string[] compiledAssemblyReferences;
            public string outputPath;
            public string outputAssemblyFullName;
            public long outputBytes;
            public string outputLastWriteUtc;
            public string outputSha256;
        }

        [Serializable]
        private sealed class LoadedAssemblyRecord
        {
            public string name;
            public string fullName;
            public bool isDynamic;
            public string location;
            public string moduleVersionId;
        }

        [Serializable]
        internal sealed class PackageIdentityRecord
        {
            public string name;
            public string version;
            public string packageId;
            public string source;
            public string assetPath;
            public string resolvedPath;
            public DependencyIdentityRecord[] requestedDependencies;
        }

        [Serializable]
        internal sealed class DependencyIdentityRecord
        {
            public string name;
            public string version;
        }

        [Serializable]
        internal sealed class DiscoveryPackageProvenance
        {
            public string name;
            public string version;
            public string kind;
            public string source;
            public string resolvedPath;
            public string manifestOrigin;
        }

        [Serializable]
        private sealed class ModeDiscoveryRecord
        {
            public string mode;
            public string requiredAssemblyName;
            public string rootId;
            public string rootFullName;
            public int reportedTestCaseCount;
            public int leafCount;
            public AssemblyDiscoveryCount[] byAssembly;
            public LeafDiscoveryRecord[] leaves;
        }

        [Serializable]
        private sealed class AssemblyDiscoveryCount
        {
            public string assemblyName;
            public int leafCount;
        }

        [Serializable]
        private sealed class LeafDiscoveryRecord
        {
            public string id;
            public string fullName;
            public string uniqueName;
            public string assemblyName;
            public string assemblyNameSource;
            public string testAssemblyNodeId;
            public string testAssemblyNodeName;
            public string testAssemblyNodeFullName;
            public string runState;
            public string skipReason;
        }

        [Serializable]
        private sealed class DiscoveryState
        {
            public string output;
            public double deadlineEditorSeconds;
            public int domainReloadRestarts;
            [NonSerialized] public int nextMode;
            [NonSerialized] public Receipt receipt;
        }

        /// <summary>Asynchronous executeMethod entrypoint. Retrieves real test trees without executing tests; omit -quit.</summary>
        public static void DiscoverCompiledTests()
        {
            string output = null;
            Receipt receipt = new Receipt { action = "compiled-editor-test-discovery", status = "failed" };
            try
            {
                output = RequireDiscoveryHost();
                if (_discoveryPending != null)
                {
                    if (_discoveryPending.output != output) throw new InvalidOperationException("Pending discovery output changed.");
                    HookDiscoveryUpdate();
                    return;
                }
                receipt.environment = ReadDiscoveryEnvironment();
                receipt.discovery = new DiscoveryRecord();
                receipt.status = "discovering";
                _discoveryPending = new DiscoveryState
                {
                    output = output, deadlineEditorSeconds = EditorApplication.timeSinceStartup + 600d,
                    receipt = receipt
                };
                PersistDiscovery();
                HookDiscoveryUpdate();
            }
            catch (Exception exception)
            {
                if (_discoveryPending != null) CompleteDiscovery(exception);
                else
                {
                    receipt.error = exception.ToString();
                    Finish(output, receipt);
                }
            }
        }

        private static string RequireDiscoveryHost()
        {
            string output = RequireOwnedHost();
            if (!Application.isBatchMode || EditorApplication.isPlayingOrWillChangePlaymode)
                throw new InvalidOperationException("Discovery requires an isolated batch Editor outside Play Mode.");
            if (_pending != null || !string.IsNullOrEmpty(SessionState.GetString(PendingKey, string.Empty)))
                throw new InvalidOperationException("Finish sample import in a separate invocation before discovery.");
            return output;
        }

        private static EnvironmentRecord ReadDiscoveryEnvironment()
        {
            return new EnvironmentRecord
            {
                companyName = Application.companyName, productName = Application.productName,
                projectPath = Path.GetFullPath(Path.Combine(Application.dataPath, "..")),
                persistentDataPath = Path.GetFullPath(Application.persistentDataPath),
                platform = Application.platform.ToString(), isEditor = Application.isEditor,
                unityVersion = Application.unityVersion
            };
        }

        [InitializeOnLoadMethod]
        private static void ResumePendingDiscovery()
        {
            string raw = SessionState.GetString(DiscoveryPendingKey, string.Empty);
            if (string.IsNullOrEmpty(raw)) return;
            try
            {
                _discoveryPending = JsonUtility.FromJson<DiscoveryState>(raw);
                if (_discoveryPending == null || string.IsNullOrEmpty(_discoveryPending.output) ||
                    _discoveryPending.deadlineEditorSeconds <= 0d ||
                    double.IsNaN(_discoveryPending.deadlineEditorSeconds) || double.IsInfinity(_discoveryPending.deadlineEditorSeconds))
                    throw new InvalidDataException("Invalid pending discovery state.");
                _discoveryPending.domainReloadRestarts++;
                // A reloaded domain needs new trees AND a new graph. Never combine trees from different domains.
                _discoveryPending.receipt = new Receipt
                {
                    action = "compiled-editor-test-discovery", status = "discovering", environment = ReadDiscoveryEnvironment(),
                    discovery = new DiscoveryRecord { domainReloadRestarts = _discoveryPending.domainReloadRestarts }
                };
                _discoveryPending.nextMode = 0;
                _discoveryInFlight = false;
                HookDiscoveryUpdate();
            }
            catch (Exception exception) { CompleteDiscovery(exception); }
        }

        private static void HookDiscoveryUpdate()
        {
            EditorApplication.update -= DiscoveryUpdate;
            EditorApplication.update += DiscoveryUpdate;
        }

        private static void PersistDiscovery()
        {
            DiscoveryState state = _discoveryPending;
            if (RequireDiscoveryHost() != state.output) throw new InvalidOperationException("Pending discovery output changed.");
            state.receipt.utc = DateTime.UtcNow.ToString("O");
            SessionState.SetString(DiscoveryPendingKey, JsonUtility.ToJson(state));
            WriteReceipt(state.output, JsonUtility.ToJson(state.receipt, true));
        }

        private static void RequireDiscoveryDeadline(DiscoveryState state)
        {
            if (EditorApplication.timeSinceStartup >= state.deadlineEditorSeconds)
                throw new TimeoutException("Compiled test discovery exceeded the ten-minute monotonic Editor-time deadline.");
        }

        private static void DiscoveryUpdate()
        {
            try
            {
                DiscoveryState state = _discoveryPending;
                if (state == null) throw new InvalidOperationException("Missing pending discovery state.");
                if (RequireDiscoveryHost() != state.output) throw new InvalidOperationException("Pending discovery output changed.");
                RequireDiscoveryDeadline(state);
                if (EditorApplication.isCompiling || EditorApplication.isUpdating || _discoveryInFlight) return;
                if (state.receipt.discovery.compiledEditorAssemblies == null)
                {
                    CaptureCompiledEditorGraph(state);
                    PersistDiscovery();
                }
                if (state.nextMode == 2)
                {
                    state.receipt.status = "complete";
                    CompleteDiscovery(null);
                    return;
                }
                if (_discoveryApi == null) _discoveryApi = ScriptableObject.CreateInstance<TestRunnerApi>();
                TestMode mode = state.nextMode == 0 ? TestMode.EditMode : TestMode.PlayMode;
                int modeIndex = state.nextMode;
                _discoveryInFlight = true;
                _discoveryApi.RetrieveTestList(mode, root =>
                {
                    if (!ReferenceEquals(state, _discoveryPending)) return;
                    try
                    {
                        if (RequireDiscoveryHost() != state.output) throw new InvalidOperationException("Pending discovery output changed.");
                        RequireDiscoveryDeadline(state);
                        state.receipt.discovery.modes[modeIndex] = CaptureDiscoveredMode(state, mode, root);
                        state.nextMode++;
                        _discoveryInFlight = false;
                        PersistDiscovery();
                    }
                    catch (Exception exception) { CompleteDiscovery(exception); }
                });
            }
            catch (Exception exception) { CompleteDiscovery(exception); }
        }

        private static void CaptureCompiledEditorGraph(DiscoveryState state)
        {
            DiscoveryRecord record = state.receipt.discovery;
            record.activeBuildTarget = EditorUserBuildSettings.activeBuildTarget.ToString();
            CompilationAssembly[] assemblies = CompilationPipeline.GetAssemblies(AssembliesType.Editor);
            if (assemblies.Length == 0) throw new InvalidOperationException("CompilationPipeline returned no Editor assemblies.");
            List<CompiledAssemblyRecord> compiled = new List<CompiledAssemblyRecord>();
            foreach (CompilationAssembly assembly in assemblies)
            {
                RequireDiscoveryDeadline(state);
                List<string> references = new List<string>();
                foreach (CompilationAssembly reference in assembly.assemblyReferences ?? Array.Empty<CompilationAssembly>())
                    references.Add(reference.name);
                references.Sort(StringComparer.Ordinal);
                string output = Path.GetFullPath(Path.Combine(Application.dataPath, "..", assembly.outputPath));
                if (!File.Exists(output)) throw new FileNotFoundException("Editor assembly output is absent; source graph is not compiled-output evidence.", output);
                AssemblyName identity = AssemblyName.GetAssemblyName(output);
                if (identity.Name != assembly.name) throw new InvalidDataException("Output DLL identity differs from CompilationPipeline: " + output);
                string digest;
                using (SHA256 hash = SHA256.Create())
                using (FileStream stream = File.OpenRead(output))
                    digest = BitConverter.ToString(hash.ComputeHash(stream)).Replace("-", string.Empty).ToLowerInvariant();
                FileInfo file = new FileInfo(output);
                compiled.Add(new CompiledAssemblyRecord
                {
                    name = assembly.name, flags = assembly.flags.ToString(), defines = assembly.defines,
                    assemblyReferences = references.ToArray(), compiledAssemblyReferences = assembly.compiledAssemblyReferences,
                    outputPath = output, outputAssemblyFullName = identity.FullName,
                    outputBytes = file.Length, outputLastWriteUtc = file.LastWriteTimeUtc.ToString("O"), outputSha256 = digest
                });
            }
            compiled.Sort((left, right) => string.CompareOrdinal(left.name, right.name));
            record.compiledEditorAssemblies = compiled.ToArray();
            List<LoadedAssemblyRecord> loaded = new List<LoadedAssemblyRecord>();
            foreach (ReflectionAssembly assembly in AppDomain.CurrentDomain.GetAssemblies())
            {
                RequireDiscoveryDeadline(state);
                loaded.Add(new LoadedAssemblyRecord
                {
                    name = assembly.GetName().Name, fullName = assembly.FullName, isDynamic = assembly.IsDynamic,
                    location = assembly.IsDynamic ? null : assembly.Location,
                    moduleVersionId = assembly.IsDynamic ? null : assembly.ManifestModule.ModuleVersionId.ToString("D")
                });
            }
            loaded.Sort((left, right) => string.CompareOrdinal(left.fullName, right.fullName));
            record.loadedAssemblies = loaded.ToArray();
            List<PackageIdentityRecord> packages = new List<PackageIdentityRecord>();
            foreach (PackageInfo package in PackageInfo.GetAllRegisteredPackages())
            {
                List<DependencyIdentityRecord> dependencies = new List<DependencyIdentityRecord>();
                foreach (DependencyInfo dependency in package.dependencies ?? Array.Empty<DependencyInfo>())
                    dependencies.Add(new DependencyIdentityRecord { name = dependency.name, version = dependency.version });
                dependencies.Sort((left, right) => string.CompareOrdinal(left.name, right.name));
                packages.Add(new PackageIdentityRecord
                {
                    name = package.name, version = package.version, packageId = package.packageId,
                    source = package.source.ToString(), assetPath = package.assetPath, resolvedPath = package.resolvedPath,
                    requestedDependencies = dependencies.ToArray()
                });
            }
            // Preserve the observed registered-package list; ProjectAssets is provenance, not a fabricated UPM entry.
            record.packages = packages.ToArray();
            DiscoveryPackageProvenance provenance = SelectDiscoveryPackageProvenance(packages,
                Path.GetFullPath(Path.Combine(Application.dataPath, "..")));
            record.neoPackage = provenance;
            state.receipt.packageName = provenance.name;
            state.receipt.packageVersion = provenance.version;
            state.receipt.packageSource = provenance.source;
            state.receipt.packageResolvedPath = provenance.resolvedPath;
        }

        /// <summary>Selects observed package provenance only; it neither registers a package nor discovers tests.</summary>
        internal static DiscoveryPackageProvenance SelectDiscoveryPackageProvenance(
            IReadOnlyList<PackageIdentityRecord> registeredPackages, string projectRoot)
        {
            if (registeredPackages == null) throw new ArgumentNullException(nameof(registeredPackages));
            foreach (PackageIdentityRecord package in registeredPackages)
            {
                if (package.name != Audit20261023NeoEditorTestsPaths.PackageName) continue;
                if (package.version != "11.0.0")
                    throw new InvalidDataException("Discovery requires registered Neo version 11.0.0; Assets cannot override " + package.version + ".");
                if (string.IsNullOrEmpty(package.resolvedPath) || string.IsNullOrEmpty(package.source))
                    throw new InvalidDataException("The registered Neo source/path identity is incomplete.");
                return new DiscoveryPackageProvenance
                {
                    name = package.name, version = package.version, kind = "RegisteredPackageInfo", source = package.source,
                    resolvedPath = Path.GetFullPath(package.resolvedPath), manifestOrigin = "PackageInfo.GetAllRegisteredPackages"
                };
            }

            if (string.IsNullOrEmpty(projectRoot)) throw new ArgumentException("The actual project root is required.", nameof(projectRoot));
            string assetsRoot = Path.GetFullPath(Path.Combine(projectRoot, "Assets", "Neoxider"));
            string manifestPath = Path.Combine(assetsRoot, "package.json");
            string json = File.ReadAllText(manifestPath).Trim();
            if (!json.StartsWith("{", StringComparison.Ordinal) || !json.EndsWith("}", StringComparison.Ordinal))
                throw new InvalidDataException("ProjectAssets requires a JSON object manifest: " + manifestPath);
            Audit20261023NeoEditorTestsPaths.PackageManifest manifest;
            try { manifest = JsonUtility.FromJson<Audit20261023NeoEditorTestsPaths.PackageManifest>(json); }
            catch (ArgumentException exception)
            {
                throw new InvalidDataException("Malformed ProjectAssets manifest: " + manifestPath, exception);
            }
            if (manifest == null || manifest.name != Audit20261023NeoEditorTestsPaths.PackageName || manifest.version != "11.0.0")
                throw new InvalidDataException("ProjectAssets requires exact com.neoxider.tools version 11.0.0: " + manifestPath);
            return new DiscoveryPackageProvenance
            {
                name = manifest.name, version = manifest.version, kind = "ProjectAssets", source = "ProjectAssets",
                resolvedPath = assetsRoot, manifestOrigin = manifestPath
            };
        }

        private static ModeDiscoveryRecord CaptureDiscoveredMode(DiscoveryState state, TestMode mode, ITestAdaptor root)
        {
            if (root == null) throw new InvalidDataException("TestRunnerApi returned a null " + mode + " tree.");
            List<LeafDiscoveryRecord> leaves = new List<LeafDiscoveryRecord>();
            Dictionary<string, int> byAssembly = new Dictionary<string, int>(StringComparer.Ordinal);
            HashSet<string> ids = new HashSet<string>(StringComparer.Ordinal);
            Stack<ITestAdaptor> pending = new Stack<ITestAdaptor>();
            pending.Push(root);
            while (pending.Count > 0)
            {
                RequireDiscoveryDeadline(state);
                ITestAdaptor node = pending.Pop();
                if (node.IsSuite)
                {
                    if (node.Children != null)
                        foreach (ITestAdaptor child in node.Children) pending.Push(child);
                    continue;
                }
                if (node.HasChildren || string.IsNullOrEmpty(node.Id) || string.IsNullOrEmpty(node.FullName) || !ids.Add(node.Id))
                    throw new InvalidDataException("Invalid/duplicate discovered leaf identity in " + mode + ": " + node.FullName);
                ITestAdaptor assemblyNode = null;
                string assemblyName = null;
                for (ITestAdaptor ancestor = node; ancestor != null; ancestor = ancestor.Parent)
                {
                    if (assemblyName == null && ancestor.TypeInfo != null)
                        assemblyName = ancestor.TypeInfo.Type.Assembly.GetName().Name;
                    if (!ancestor.IsTestAssembly) continue;
                    assemblyNode = ancestor;
                    break;
                }
                if (assemblyNode == null) throw new InvalidDataException("Discovered leaf has no test assembly ancestor: " + node.FullName);
                string assemblySource = "TypeInfo.Type.Assembly";
                if (string.IsNullOrEmpty(assemblyName))
                {
                    assemblyName = Path.GetFileNameWithoutExtension(assemblyNode.Name);
                    assemblySource = "testAssemblyNode.Name";
                }
                if (string.IsNullOrEmpty(assemblyName)) throw new InvalidDataException("Missing discovered assembly identity: " + node.FullName);
                byAssembly.TryGetValue(assemblyName, out int count);
                byAssembly[assemblyName] = count + 1;
                leaves.Add(new LeafDiscoveryRecord
                {
                    id = node.Id, fullName = node.FullName, uniqueName = node.UniqueName,
                    assemblyName = assemblyName, assemblyNameSource = assemblySource,
                    testAssemblyNodeId = assemblyNode.Id, testAssemblyNodeName = assemblyNode.Name,
                    testAssemblyNodeFullName = assemblyNode.FullName, runState = node.RunState.ToString(), skipReason = node.SkipReason
                });
            }
            if (leaves.Count == 0) throw new InvalidDataException("TestRunnerApi discovered zero actual " + mode + " leaves.");
            string requiredAssembly = mode == TestMode.EditMode ? "Neo.Editor.Tests" : "Neo.Tests.Play";
            if (!byAssembly.TryGetValue(requiredAssembly, out int requiredCount) || requiredCount == 0)
                throw new InvalidDataException("No actual " + mode + " leaves were discovered for required assembly " + requiredAssembly + ".");
            leaves.Sort((left, right) => string.CompareOrdinal(left.uniqueName, right.uniqueName));
            List<AssemblyDiscoveryCount> counts = new List<AssemblyDiscoveryCount>();
            foreach (KeyValuePair<string, int> pair in byAssembly)
                counts.Add(new AssemblyDiscoveryCount { assemblyName = pair.Key, leafCount = pair.Value });
            counts.Sort((left, right) => string.CompareOrdinal(left.assemblyName, right.assemblyName));
            return new ModeDiscoveryRecord
            {
                mode = mode.ToString(), requiredAssemblyName = requiredAssembly, rootId = root.Id, rootFullName = root.FullName,
                reportedTestCaseCount = root.TestCaseCount, leafCount = leaves.Count,
                byAssembly = counts.ToArray(), leaves = leaves.ToArray()
            };
        }

        private static void CompleteDiscovery(Exception exception)
        {
            EditorApplication.update -= DiscoveryUpdate;
            DiscoveryState state = _discoveryPending;
            _discoveryPending = null;
            _discoveryInFlight = false;
            SessionState.EraseString(DiscoveryPendingKey);
            try
            {
                if (_discoveryApi != null) UnityEngine.Object.DestroyImmediate(_discoveryApi);
            }
            catch (Exception cleanupFailure) { exception = exception ?? cleanupFailure; }
            _discoveryApi = null;
            Receipt receipt = state?.receipt ?? new Receipt { action = "compiled-editor-test-discovery", status = "failed" };
            string output = null;
            try
            {
                output = RequireDiscoveryHost();
                if (state != null && state.output != output)
                {
                    output = null;
                    throw new InvalidOperationException("Pending discovery output changed.");
                }
            }
            catch (Exception guardFailure)
            {
                output = null;
                exception = exception ?? guardFailure;
            }
            if (exception != null)
            {
                receipt.status = "failed";
                receipt.error = exception.ToString();
            }
            Finish(output, receipt);
        }
    }
}
