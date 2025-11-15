/*  ──────────────────────────────────────────────────────────────
    SaveProjectZip.cs
    Menu:  Tools ▸ Neoxider ▸ Save Project Zip
    Сохраняет три папки проекта (Assets, ProjectSettings, Packages)
    в ZIP-архив рядом с проектом.
   ────────────────────────────────────────────────────────────── */

using System;
using System.IO;
using System.IO.Compression;
using UnityEditor;
using UnityEngine;
using CompressionLevel = System.IO.Compression.CompressionLevel;

public static class SaveProjectZip
{
    private static readonly string[] RootFolders =
    {
        "Assets",
        "ProjectSettings",
        "Packages"
    };

    [MenuItem("Tools/Neoxider/Save Project Zip")]
    private static void MakeZip()
    {
        string projRoot = Path.GetDirectoryName(Application.dataPath);
        string projName = Path.GetFileName(projRoot);
        string defaultFile =
            $"{projName}.zip";

        string zipPath = EditorUtility.SaveFilePanel(
            "Save Project as ZIP",
            projRoot, // стартовая папка
            defaultFile,
            "zip");

        if (string.IsNullOrEmpty(zipPath)) // cancel
        {
            return;
        }

        try
        {
            if (File.Exists(zipPath))
            {
                File.Delete(zipPath);
            }

            using (ZipArchive archive =
                   ZipFile.Open(zipPath, ZipArchiveMode.Create))
            {
                foreach (string folder in RootFolders)
                {
                    string srcPath = Path.Combine(projRoot, folder);
                    if (!Directory.Exists(srcPath))
                    {
                        continue;
                    }

                    AddDirectoryToZip(archive, srcPath, folder);
                }
            }

            Debug.Log($"Project zipped OK: {zipPath}");
            EditorUtility.RevealInFinder(zipPath);
        }
        catch (Exception e)
        {
            Debug.LogError("ZIP-error: " + e.Message);
        }
    }

    // рекурсивно добавляем файлы/папки в архив
    private static void AddDirectoryToZip(ZipArchive zip,
        string absPath,
        string relPath)
    {
        foreach (string file in Directory.GetFiles(absPath))
        {
            string entry = Path.Combine(relPath, Path.GetFileName(file))
                .Replace("\\", "/");
            zip.CreateEntryFromFile(file, entry,
                CompressionLevel.Optimal);
        }

        foreach (string dir in Directory.GetDirectories(absPath))
        {
            string subRel = Path.Combine(relPath,
                Path.GetFileName(dir));
            AddDirectoryToZip(zip, dir, subRel);
        }
    }
}