using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.CompilerServices;
using UnityEditor;
using UnityEditor.Compilation;
using CompilationAssembly = UnityEditor.Compilation.Assembly;
using PackageInfo = UnityEditor.PackageManager.PackageInfo;
using ReflectionAssembly = System.Reflection.Assembly;

namespace Neo.Editor
{
    public readonly struct NeoxiderModulePackageInfo
    {
        public readonly string Name;
        public readonly string DisplayName;
        public readonly string Version;
        public readonly string RootPath;

        public NeoxiderModulePackageInfo(string name, string displayName, string version, string rootPath)
        {
            Name = name;
            DisplayName = displayName;
            Version = version;
            RootPath = rootPath;
        }
    }

    public static class NeoxiderModulePackageInfoUtility
    {
        private static readonly Dictionary<string, NeoxiderModulePackageInfo>
            CacheByAssemblyName = new();

        private static readonly HashSet<string> NegativeCache = new();
        private static bool _cacheHooksInstalled;

        public static bool TryGetForAssembly(ReflectionAssembly assembly, out NeoxiderModulePackageInfo info)
        {
            info = default;
            if (assembly == null)
            {
                return false;
            }

            EnsureCacheHooks();

            string asmName = assembly.GetName().Name ?? string.Empty;
            if (!string.IsNullOrEmpty(asmName))
            {
                if (CacheByAssemblyName.TryGetValue(asmName, out info))
                {
                    return true;
                }

                if (NegativeCache.Contains(asmName))
                {
                    return false;
                }
            }

            // 1) Preferred: PackageManager can resolve package for the assembly (when installed via UPM)
            try
            {
                PackageInfo pkg =
                    PackageInfo.FindForAssembly(assembly);

                if (pkg != null)
                {
                    info = new NeoxiderModulePackageInfo(
                        pkg.name,
                        pkg.displayName,
                        string.IsNullOrEmpty(pkg.version) ? "Unknown" : pkg.version,
                        string.IsNullOrEmpty(pkg.assetPath) ? null : pkg.assetPath.Replace('\\', '/'));

                    if (!string.IsNullOrEmpty(asmName))
                    {
                        CacheByAssemblyName[asmName] = info;
                    }

                    return true;
                }
            }
            catch
            {
                // ignore
            }

            // 2) Fallback: walk up from any source file in the target assembly and read nearest package.json.
            //    This is important for "optional modules" located in Assets/..., where PackageInfo is unavailable.
            try
            {
                string startPath = TryGetAnySourceFilePathFromAssembly(assembly) ?? GetScriptPath();
                string dir = Path.GetDirectoryName(startPath);
                while (!string.IsNullOrEmpty(dir))
                {
                    string packageJson = Path.Combine(dir, "package.json");
                    if (File.Exists(packageJson))
                    {
                        string json = File.ReadAllText(packageJson);

                        string name = TryReadJsonString(json, "name");
                        string displayName = TryReadJsonString(json, "displayName");
                        string version = TryReadJsonString(json, "version");

                        info = new NeoxiderModulePackageInfo(
                            string.IsNullOrEmpty(name) ? "Unknown" : name,
                            string.IsNullOrEmpty(displayName) ? "Unknown" : displayName,
                            string.IsNullOrEmpty(version) ? "Unknown" : version,
                            TryConvertToUnityProjectRelativePath(dir));

                        if (!string.IsNullOrEmpty(asmName))
                        {
                            CacheByAssemblyName[asmName] = info;
                        }

                        return true;
                    }

                    dir = Directory.GetParent(dir)?.FullName;
                }
            }
            catch
            {
                // ignore
            }

            if (!string.IsNullOrEmpty(asmName))
            {
                NegativeCache.Add(asmName);
            }

            return false;
        }

        private static void EnsureCacheHooks()
        {
            if (_cacheHooksInstalled)
            {
                return;
            }

            _cacheHooksInstalled = true;
            EditorApplication.projectChanged += ClearCache;
            AssemblyReloadEvents.afterAssemblyReload += ClearCache;
        }

        private static void ClearCache()
        {
            CacheByAssemblyName.Clear();
            NegativeCache.Clear();
        }

        private static string TryGetAnySourceFilePathFromAssembly(ReflectionAssembly assembly)
        {
            if (assembly == null)
            {
                return null;
            }

            try
            {
                string asmName = assembly.GetName().Name;
                if (string.IsNullOrEmpty(asmName))
                {
                    return null;
                }

                CompilationAssembly[] assemblies = CompilationPipeline.GetAssemblies();
                if (assemblies == null)
                {
                    return null;
                }

                for (int i = 0; i < assemblies.Length; i++)
                {
                    CompilationAssembly a = assemblies[i];
                    if (a == null || !string.Equals(a.name, asmName, StringComparison.Ordinal))
                    {
                        continue;
                    }

                    string[] files = a.sourceFiles;
                    if (files == null || files.Length == 0)
                    {
                        return null;
                    }

                    // sourceFiles are typically project-relative (Assets/..., Packages/...)
                    return files[0]?.Replace('\\', '/');
                }
            }
            catch
            {
                // ignore
            }

            return null;
        }

        private static string TryReadJsonString(string json, string key)
        {
            if (string.IsNullOrEmpty(json) || string.IsNullOrEmpty(key))
            {
                return null;
            }

            // Very lightweight parse, consistent with existing CustomEditorBase approach.
            string token = $"\"{key}\":";
            int idx = json.IndexOf(token, StringComparison.Ordinal);
            if (idx < 0)
            {
                return null;
            }

            int startQuote = json.IndexOf("\"", idx + token.Length, StringComparison.Ordinal);
            if (startQuote < 0)
            {
                return null;
            }

            int endQuote = json.IndexOf("\"", startQuote + 1, StringComparison.Ordinal);
            if (endQuote < 0)
            {
                return null;
            }

            return json.Substring(startQuote + 1, endQuote - startQuote - 1).Trim();
        }

        private static string TryConvertToUnityProjectRelativePath(string path)
        {
            if (string.IsNullOrEmpty(path))
            {
                return null;
            }

            string normalized = path.Replace('\\', '/');
            if (normalized.StartsWith("Assets/", StringComparison.OrdinalIgnoreCase) ||
                normalized.StartsWith("Packages/", StringComparison.OrdinalIgnoreCase))
            {
                return normalized;
            }

            try
            {
                string projectPath = Directory.GetCurrentDirectory().Replace('\\', '/');
                if (!string.IsNullOrEmpty(projectPath) &&
                    normalized.StartsWith(projectPath, StringComparison.OrdinalIgnoreCase))
                {
                    return normalized.Substring(projectPath.Length + 1);
                }
            }
            catch
            {
                // ignore
            }

            return null;
        }

        private static string GetScriptPath([CallerFilePath] string sourceFilePath = "")
        {
            if (string.IsNullOrEmpty(sourceFilePath))
            {
                return sourceFilePath;
            }

            try
            {
                string projectPath = Directory.GetCurrentDirectory();
                if (sourceFilePath.StartsWith(projectPath, StringComparison.OrdinalIgnoreCase))
                {
                    return sourceFilePath.Substring(projectPath.Length + 1).Replace('\\', '/');
                }
            }
            catch
            {
                // ignore
            }

            return sourceFilePath.Replace('\\', '/');
        }
    }
}