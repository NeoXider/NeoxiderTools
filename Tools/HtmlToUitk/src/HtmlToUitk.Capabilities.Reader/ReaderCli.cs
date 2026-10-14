using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;

namespace HtmlToUitk.Capabilities.Reader;

public static class UnityLocator
{
    public static string FindManagedDirectory()
    {
        var fromEnv = Environment.GetEnvironmentVariable("UNITY_MANAGED_DIR");
        if (!string.IsNullOrEmpty(fromEnv) && Directory.Exists(fromEnv))
        {
            return fromEnv;
        }

        var editor = Environment.GetEnvironmentVariable("UNITY_EDITOR_DIR");
        if (!string.IsNullOrEmpty(editor))
        {
            var candidate = Path.Combine(editor, "Data", "Managed", "UnityEngine");
            if (Directory.Exists(candidate))
            {
                return candidate;
            }
        }

        var roots = new List<string>
        {
            @"C:\Program Files\Unity\Hub\Editor",
            "/Applications/Unity/Hub/Editor",
            Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), "Unity", "Hub", "Editor"),
        };
        foreach (var root in roots.Where(Directory.Exists))
        {
            foreach (var version in Directory.GetDirectories(root).OrderByDescending(d => d, StringComparer.Ordinal))
            {
                foreach (var tail in new[] { Path.Combine("Editor", "Data", "Managed", "UnityEngine"), Path.Combine("Unity.app", "Contents", "Managed", "UnityEngine") })
                {
                    var candidate = Path.Combine(version, tail);
                    if (Directory.Exists(candidate))
                    {
                        return candidate;
                    }
                }
            }
        }

        return null;
    }

    public static string VersionFromPath(string path)
    {
        var m = Regex.Match(path.Replace('\\', '/'), @"(\d{4}\.\d+\.\d+[abfp]\d+)");
        return m.Success ? m.Groups[1].Value : null;
    }

    public static string FileNameForVersion(string unityVersion)
    {
        var parts = unityVersion.Split('.');
        return parts.Length >= 2 ? "capabilities." + parts[0] + "." + parts[1] + ".json" : "capabilities." + unityVersion + ".json";
    }
}

public static class ReaderCli
{
    public const int ExitOk = 0;
    public const int ExitMismatch = 1;
    public const int ExitUsage = 2;

    public static int Run(string[] args, TextWriter output, TextWriter error)
    {
        string managed = null;
        string version = null;
        string configPath = null;
        string overridesPath = null;
        string outPath = null;
        string checkPath = null;
        var quiet = false;
        for (var i = 0; i < args.Length; i++)
        {
            var a = args[i];
            string Next()
            {
                if (i + 1 >= args.Length)
                {
                    throw new ArgumentException("missing value for " + a);
                }

                return args[++i];
            }

            try
            {
                switch (a)
                {
                    case "--unity-managed": managed = Next(); break;
                    case "--unity-version": version = Next(); break;
                    case "--config": configPath = Next(); break;
                    case "--overrides": overridesPath = Next(); break;
                    case "--out": outPath = Next(); break;
                    case "--check": checkPath = Next(); break;
                    case "--quiet": quiet = true; break;
                    case "--help":
                    case "-h":
                        output.WriteLine(Usage);
                        return ExitOk;
                    default:
                        error.WriteLine("unknown argument: " + a);
                        error.WriteLine(Usage);
                        return ExitUsage;
                }
            }
            catch (ArgumentException ex)
            {
                error.WriteLine(ex.Message);
                return ExitUsage;
            }
        }

        managed ??= UnityLocator.FindManagedDirectory();
        if (managed == null || !Directory.Exists(managed))
        {
            error.WriteLine("Unity managed directory not found; pass --unity-managed or set UNITY_MANAGED_DIR");
            return ExitUsage;
        }

        version ??= UnityLocator.VersionFromPath(managed);
        if (version == null)
        {
            error.WriteLine("Unity version could not be derived from the path; pass --unity-version");
            return ExitUsage;
        }

        ReadResult result;
        try
        {
            var config = configPath != null ? ReaderConfig.Load(configPath) : ReaderConfig.Default();
            var overrides = overridesPath != null ? OverridesFile.Load(overridesPath) : OverridesFile.Empty();
            result = CapabilityReader.Read(managed, version, config, overrides);
        }
        catch (Exception ex) when (ex is IOException || ex is InvalidDataException || ex is System.Text.Json.JsonException || ex is KeyNotFoundException || ex is InvalidOperationException)
        {
            error.WriteLine("read failed: " + ex.Message);
            return ExitUsage;
        }

        if (!quiet)
        {
            foreach (var w in result.Warnings)
            {
                error.WriteLine("warning: " + w);
            }
        }

        if (checkPath != null)
        {
            if (!File.Exists(checkPath))
            {
                error.WriteLine("check file not found: " + checkPath);
                return ExitUsage;
            }

            var existing = File.ReadAllText(checkPath, Encoding.UTF8).Replace("\r\n", "\n");
            if (!string.Equals(existing, result.Json, StringComparison.Ordinal))
            {
                error.WriteLine("capabilities file differs from a fresh read: " + checkPath);
                return ExitMismatch;
            }

            output.WriteLine("capabilities file matches a fresh read: " + checkPath);
            return ExitOk;
        }

        var target = ResolveOut(outPath, version);
        var dir = Path.GetDirectoryName(Path.GetFullPath(target));
        if (!string.IsNullOrEmpty(dir))
        {
            Directory.CreateDirectory(dir);
        }

        File.WriteAllBytes(target, new UTF8Encoding(false).GetBytes(result.Json));
        output.WriteLine("wrote " + target);
        return ExitOk;
    }

    private static string ResolveOut(string outPath, string version)
    {
        var name = UnityLocator.FileNameForVersion(version);
        if (string.IsNullOrEmpty(outPath))
        {
            return name;
        }

        if (Directory.Exists(outPath) || outPath.EndsWith("/", StringComparison.Ordinal) || outPath.EndsWith("\\", StringComparison.Ordinal))
        {
            return Path.Combine(outPath, name);
        }

        return outPath;
    }

    public const string Usage =
        "usage: htmltouitk-capreader [--unity-managed <dir>] [--unity-version <v>] [--config <file>] [--overrides <file>] [--out <file|dir>] [--check <file>] [--quiet]";
}
