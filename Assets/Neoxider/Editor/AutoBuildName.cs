using System.IO;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEngine;

public class AutoBuildName : IPostprocessBuildWithReport
{
    public int callbackOrder => 0;

    public void OnPostprocessBuild(BuildReport report)
    {
        string appName = Application.productName; // App name (from Player Settings)
        string bundleVersionCode = PlayerSettings.Android.bundleVersionCode.ToString(); // Bundle version code
        string bundleVersion = PlayerSettings.bundleVersion; // Version (e.g. 1.1)
        string extension = Path.GetExtension(report.summary.outputPath); // .apk or .aab

        // Customize the naming format here
        string newName = $"{appName} {bundleVersionCode} ({bundleVersion}){extension}";

        string dir = Path.GetDirectoryName(report.summary.outputPath);
        string newPath = Path.Combine(dir, newName);

        if (File.Exists(report.summary.outputPath))
        {
            File.Move(report.summary.outputPath, newPath);
            Debug.Log($"[AutoBuildName] Output renamed to: {newName}");
        }
    }
}
