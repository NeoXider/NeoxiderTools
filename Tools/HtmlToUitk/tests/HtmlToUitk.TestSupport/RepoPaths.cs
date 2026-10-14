using System;
using System.Collections.Generic;
using System.IO;

namespace HtmlToUitk.TestSupport;

public static class RepoPaths
{
    private static string _root;

    public static string Root
    {
        get
        {
            if (_root != null)
            {
                return _root;
            }

            var dir = new DirectoryInfo(AppContext.BaseDirectory);
            while (dir != null)
            {
                if (File.Exists(Path.Combine(dir.FullName, "spec", "rules.json")) && Directory.Exists(Path.Combine(dir.FullName, "src")))
                {
                    _root = dir.FullName;
                    return _root;
                }

                dir = dir.Parent;
            }

            throw new DirectoryNotFoundException("repository root not found above " + AppContext.BaseDirectory);
        }
    }

    public static string Spec(string name)
    {
        return Path.Combine(Root, "spec", name);
    }

    public static string Fixtures => Path.Combine(Root, "fixtures");

    public static string Examples => Path.Combine(Root, "examples");

    public static string UnityManagedOrNull()
    {
        var env = Environment.GetEnvironmentVariable("UNITY_MANAGED_DIR");
        if (!string.IsNullOrEmpty(env) && File.Exists(Path.Combine(env, "UnityEngine.UIElementsModule.dll")))
        {
            return env;
        }

        return null;
    }
}

public sealed class TempDir : IDisposable
{
    public string Path { get; }

    public TempDir()
    {
        Path = System.IO.Path.Combine(System.IO.Path.GetTempPath(), "htu-" + Guid.NewGuid().ToString("N").Substring(0, 12));
        Directory.CreateDirectory(Path);
    }

    public string File(string relative, string content)
    {
        var full = System.IO.Path.Combine(Path, relative.Replace('/', System.IO.Path.DirectorySeparatorChar));
        Directory.CreateDirectory(System.IO.Path.GetDirectoryName(full));
        System.IO.File.WriteAllText(full, content);
        return full;
    }

    public void Dispose()
    {
        try
        {
            Directory.Delete(Path, true);
        }
        catch (IOException)
        {
        }
        catch (UnauthorizedAccessException)
        {
        }
    }
}
