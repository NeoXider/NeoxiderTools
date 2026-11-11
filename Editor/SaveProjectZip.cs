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

        // WHY: compressing a whole project takes long enough to look like a hang. A cancelable progress bar
        // shows how far it is, and cancelling leaves no partial archive behind.
        double nextProgressAt = 0d;
        try
        {
            CreateArchive(projRoot, zipPath, (done, total, entry) =>
            {
                double now = EditorApplication.timeSinceStartup;
                if (now < nextProgressAt && done < total)
                {
                    return false;
                }

                nextProgressAt = now + 0.1d;
                float progress = total > 0 ? (float)done / total : 0f;
                return EditorUtility.DisplayCancelableProgressBar("Save Project Zip",
                    $"{done} / {total}  {entry}", progress);
            });

            Debug.Log($"Project zipped OK: {zipPath}");
            EditorUtility.RevealInFinder(zipPath);
        }
        catch (OperationCanceledException)
        {
            Debug.Log("Project zip cancelled; no archive was written.");
        }
        catch (Exception e)
        {
            Debug.LogError("ZIP-error: " + e.Message);
        }
        finally
        {
            EditorUtility.ClearProgressBar();
        }
    }

    internal static void CreateArchive(string projectRoot, string zipPath)
    {
        CreateArchive(projectRoot, zipPath, null);
    }

    /// <summary>
    ///     Writes the archive through a temporary file. <paramref name="onEntry" /> receives the number of files
    ///     written so far, the total and the entry name, and returns true to cancel; a cancelled run throws
    ///     <see cref="OperationCanceledException" /> and leaves an existing archive untouched.
    /// </summary>
    internal static void CreateArchive(string projectRoot, string zipPath, Func<int, int, string, bool> onEntry)
    {
        string destination = Path.GetFullPath(zipPath);
        string temporary = destination + "." + Guid.NewGuid().ToString("N") + ".tmp";
        try
        {
            ArchiveProgress progress = new ArchiveProgress(onEntry, onEntry != null ? CountFiles(projectRoot) : 0);
            using (ZipArchive archive = ZipFile.Open(temporary, ZipArchiveMode.Create))
            {
                foreach (string folder in RootFolders)
                {
                    string source = Path.Combine(projectRoot, folder);
                    if (Directory.Exists(source))
                    {
                        AddDirectoryToZip(archive, source, folder, destination, temporary, progress);
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

    private static int CountFiles(string projectRoot)
    {
        int total = 0;
        foreach (string folder in RootFolders)
        {
            string source = Path.Combine(projectRoot, folder);
            if (Directory.Exists(source))
            {
                total += Directory.GetFiles(source, "*", SearchOption.AllDirectories).Length;
            }
        }

        return total;
    }

    private sealed class ArchiveProgress
    {
        private readonly Func<int, int, string, bool> _onEntry;
        private readonly int _total;
        private int _done;

        public ArchiveProgress(Func<int, int, string, bool> onEntry, int total)
        {
            _onEntry = onEntry;
            _total = total;
        }

        public void Report(string entry)
        {
            _done++;
            if (_onEntry != null && _onEntry(_done, _total, entry))
            {
                throw new OperationCanceledException();
            }
        }
    }

    private static void AddDirectoryToZip(ZipArchive zip,
        string absPath,
        string relPath, string destination, string temporary, ArchiveProgress progress)
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
            progress.Report(entry);
        }

        foreach (string dir in Directory.GetDirectories(absPath))
        {
            string subRel = Path.Combine(relPath,
                Path.GetFileName(dir));
            AddDirectoryToZip(zip, dir, subRel, destination, temporary, progress);
        }
    }
}
