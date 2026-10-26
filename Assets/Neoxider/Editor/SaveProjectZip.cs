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

    [MenuItem("Neoxider/Tools/Save Project Zip", false, 102)]
    private static void MakeZip()
    {
        string projRoot = Path.GetDirectoryName(Application.dataPath);
        string projName = Path.GetFileName(projRoot);
        string defaultFile =
            $"{projName}.zip";

        string zipPath = EditorUtility.SaveFilePanel(
            "Save Project as ZIP",
            projRoot,
            defaultFile,
            "zip");

        if (string.IsNullOrEmpty(zipPath)) // WHY: cancel
        {
            return;
        }

        try
        {
            CreateArchive(projRoot, zipPath);

            Debug.Log($"Project zipped OK: {zipPath}");
            EditorUtility.RevealInFinder(zipPath);
        }
        catch (Exception e)
        {
            Debug.LogError("ZIP-error: " + e.Message);
        }
    }

    internal static void CreateArchive(string projectRoot, string zipPath)
    {
        string destination = Path.GetFullPath(zipPath);
        string temporary = destination + "." + Guid.NewGuid().ToString("N") + ".tmp";
        try
        {
            using (ZipArchive archive = ZipFile.Open(temporary, ZipArchiveMode.Create))
            {
                foreach (string folder in RootFolders)
                {
                    string source = Path.Combine(projectRoot, folder);
                    if (Directory.Exists(source))
                    {
                        AddDirectoryToZip(archive, source, folder, destination, temporary);
                    }
                }
            }

            if (File.Exists(destination))
            {
                File.Replace(temporary, destination, null);
            }
            else
            {
                File.Move(temporary, destination);
            }
        }
        finally
        {
            if (File.Exists(temporary))
            {
                File.Delete(temporary);
            }
        }
    }

    private static void AddDirectoryToZip(ZipArchive zip,
        string absPath,
        string relPath, string destination, string temporary)
    {
        foreach (string file in Directory.GetFiles(absPath))
        {
            StringComparison comparison = Path.DirectorySeparatorChar == '\\'
                ? StringComparison.OrdinalIgnoreCase
                : StringComparison.Ordinal;
            string fullPath = Path.GetFullPath(file);
            if (string.Equals(fullPath, destination, comparison) ||
                string.Equals(fullPath, temporary, comparison))
            {
                continue;
            }

            string entry = Path.Combine(relPath, Path.GetFileName(file))
                .Replace("\\", "/");
            zip.CreateEntryFromFile(file, entry,
                CompressionLevel.Optimal);
        }

        foreach (string dir in Directory.GetDirectories(absPath))
        {
            string subRel = Path.Combine(relPath,
                Path.GetFileName(dir));
            AddDirectoryToZip(zip, dir, subRel, destination, temporary);
        }
    }
}
