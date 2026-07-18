using System;
using System.IO;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEngine;

namespace Neo.Editor
{
    /// <summary>
    ///     Renames Android build outputs to "ProductName bundleVersionCode (version).apk/.aab".
    ///     Applies ONLY to Android package builds: renaming a Windows .exe breaks its Data folder
    ///     link and folder outputs (WebGL, exported projects) must keep their paths.
    /// </summary>
    public class AutoBuildName : IPostprocessBuildWithReport
    {
        public int callbackOrder => 0;

        public void OnPostprocessBuild(BuildReport report)
        {
            if (report.summary.platform != BuildTarget.Android)
            {
                return;
            }

            string outputPath = report.summary.outputPath;
            string extension = Path.GetExtension(outputPath);
            bool isPackage = string.Equals(extension, ".apk", StringComparison.OrdinalIgnoreCase) ||
                             string.Equals(extension, ".aab", StringComparison.OrdinalIgnoreCase);
            if (!isPackage || !File.Exists(outputPath))
            {
                return;
            }

            string baseName =
                $"{Application.productName} {PlayerSettings.Android.bundleVersionCode} ({PlayerSettings.bundleVersion})";
            string dir = Path.GetDirectoryName(outputPath) ?? string.Empty;
            string newPath = Path.Combine(dir, baseName + extension);
            // WHY: outputPath may use '/' while Path.Combine yields '\' — compare normalized paths,
            // or an already-versioned output would collide with itself and get a bogus suffix.
            if (string.Equals(Path.GetFullPath(newPath), Path.GetFullPath(outputPath),
                    StringComparison.OrdinalIgnoreCase))
            {
                return;
            }

            // WHY: Never destroy a previous artifact with the same version — pick a unique suffix instead.
            for (int i = 2; File.Exists(newPath); i++)
            {
                newPath = Path.Combine(dir, $"{baseName} ({i}){extension}");
            }

            File.Move(outputPath, newPath);
            Debug.Log($"[AutoBuildName] Output renamed to: {Path.GetFileName(newPath)}");
        }
    }
}
