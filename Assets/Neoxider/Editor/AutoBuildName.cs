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
        var appName = Application.productName; // Имя приложения (из Player Settings)
        var bundleVersionCode = PlayerSettings.Android.bundleVersionCode.ToString(); // Номер бандла
        var bundleVersion = PlayerSettings.bundleVersion; // Версия (например, 1.1)
        var extension = Path.GetExtension(report.summary.outputPath); // .apk или .aab

        // 👉 Здесь меняешь формат под себя
        var newName = $"{appName} {bundleVersionCode} ({bundleVersion}){extension}";

        var dir = Path.GetDirectoryName(report.summary.outputPath);
        var newPath = Path.Combine(dir, newName);

        if (File.Exists(report.summary.outputPath))
        {
            File.Move(report.summary.outputPath, newPath);
            Debug.Log($"[AutoBuildName] Файл переименован в: {newName}");
        }
    }
}