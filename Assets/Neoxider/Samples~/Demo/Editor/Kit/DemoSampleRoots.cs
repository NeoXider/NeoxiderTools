using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEditor.Compilation;

namespace Neo.Demo.Editor
{
    /// <summary>
    ///     Resolves where the demo samples currently live. Development uses Assets/Neoxider/Samples/Demo (the
    ///     release layout hides it as Samples~, where nothing compiles); a Package Manager import copies it to
    ///     Assets/Samples/NeoxiderTools/&lt;version&gt;/Demo Scenes. The kit's own asmdef location wins, so the
    ///     runner always edits the copy that is compiling. Override with <c>-neoDemoRoot &lt;asset path&gt;</c>.
    /// </summary>
    public static class DemoSampleRoots
    {
        public const string RootArgument = "-neoDemoRoot";
        public const string EditorAssemblyName = "Neo.Demo.Editor";
        public const string DevelopmentDemoRoot = "Assets/Neoxider/Samples/Demo";
        public const string ImportedSamplesRoot = "Assets/Samples/NeoxiderTools";
        public const string ImportedDemoFolder = "Demo Scenes";
        public const string PagesFolder = "NeoxiderPages";

        /// <summary>Active Demo sample root (asset path, no trailing slash). Throws when none is visible.</summary>
        public static string Demo
        {
            get
            {
                if (TryResolveDemoRoot(out string root, out string error))
                {
                    return root;
                }

                throw new DirectoryNotFoundException(error);
            }
        }

        /// <summary>Root of <paramref name="sample" /> (asset path). Throws when the sample is not visible.</summary>
        public static string Resolve(DemoSample sample)
        {
            string demo = Demo;
            if (sample == DemoSample.Demo)
            {
                return demo;
            }

            string parent = Path.GetDirectoryName(demo)?.Replace('\\', '/');
            string pages = parent + "/" + PagesFolder;
            if (!AssetDatabase.IsValidFolder(pages))
            {
                throw new DirectoryNotFoundException(
                    "NeoxiderPages sample is not visible next to the Demo sample (expected " + pages + ").");
            }

            return pages;
        }

        /// <summary>Asset path of a scene relative to a sample root.</summary>
        public static string ToAssetPath(DemoSample sample, string relativeScenePath)
        {
            return Resolve(sample) + "/" + Normalize(relativeScenePath);
        }

        public static bool TryResolveDemoRoot(out string root, out string error)
        {
            List<string> tried = new();
            string fromArgument = ReadArgument(RootArgument);
            if (!string.IsNullOrEmpty(fromArgument))
            {
                string candidate = Normalize(fromArgument).TrimEnd('/');
                if (IsDemoRoot(candidate))
                {
                    root = candidate;
                    error = null;
                    return true;
                }

                tried.Add(candidate + " (" + RootArgument + ")");
            }

            string asmdef = CompilationPipeline.GetAssemblyDefinitionFilePathFromAssemblyName(EditorAssemblyName);
            if (!string.IsNullOrEmpty(asmdef))
            {
                string editorFolder = Path.GetDirectoryName(Normalize(asmdef));
                string candidate = Path.GetDirectoryName(editorFolder)?.Replace('\\', '/');
                if (IsDemoRoot(candidate))
                {
                    root = candidate;
                    error = null;
                    return true;
                }

                tried.Add(candidate + " (kit asmdef)");
            }

            if (IsDemoRoot(DevelopmentDemoRoot))
            {
                root = DevelopmentDemoRoot;
                error = null;
                return true;
            }

            tried.Add(DevelopmentDemoRoot);
            if (AssetDatabase.IsValidFolder(ImportedSamplesRoot))
            {
                string[] versions = AssetDatabase.GetSubFolders(ImportedSamplesRoot);
                Array.Sort(versions, StringComparer.Ordinal);
                for (int i = versions.Length - 1; i >= 0; i--)
                {
                    string candidate = versions[i] + "/" + ImportedDemoFolder;
                    if (IsDemoRoot(candidate))
                    {
                        root = candidate;
                        error = null;
                        return true;
                    }

                    tried.Add(candidate);
                }
            }

            root = null;
            error = "No visible Demo sample root with a Scenes folder. Rename Samples~ to Samples or import the " +
                    "'Demo Scenes' sample. Tried: " + string.Join(", ", tried);
            return false;
        }

        internal static string Normalize(string path)
        {
            return (path ?? string.Empty).Replace('\\', '/').Trim();
        }

        internal static string ReadArgument(string name)
        {
            string[] args = Environment.GetCommandLineArgs();
            for (int i = 0; i < args.Length - 1; i++)
            {
                if (string.Equals(args[i], name, StringComparison.OrdinalIgnoreCase))
                {
                    return args[i + 1];
                }
            }

            return null;
        }

        private static bool IsDemoRoot(string candidate)
        {
            return !string.IsNullOrEmpty(candidate) && AssetDatabase.IsValidFolder(candidate) &&
                   AssetDatabase.IsValidFolder(candidate + "/Scenes");
        }
    }
}
