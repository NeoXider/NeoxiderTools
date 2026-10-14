using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using HtmlToUitk.Convert;
using HtmlToUitk.Core.Text;

namespace HtmlToUitk.Core.Includes
{
    public sealed class MemoryFileSystem : IFileSystem
    {
        private readonly SortedDictionary<string, byte[]> _files = new SortedDictionary<string, byte[]>(StringComparer.Ordinal);
        private static readonly UTF8Encoding Utf8 = new UTF8Encoding(false);

        public IReadOnlyCollection<string> Paths => _files.Keys;

        public MemoryFileSystem AddFile(string path, string text)
        {
            _files[PathUtil.Normalize(path)] = Utf8.GetBytes(text ?? "");
            return this;
        }

        public MemoryFileSystem AddFile(string path, byte[] content)
        {
            _files[PathUtil.Normalize(path)] = content ?? new byte[0];
            return this;
        }

        public bool FileExists(string path) => _files.ContainsKey(PathUtil.Normalize(path));

        public bool DirectoryExists(string path)
        {
            string prefix = PathUtil.Normalize(path);
            if (prefix.Length == 0) return _files.Count > 0;
            prefix += "/";
            foreach (string key in _files.Keys)
            {
                if (key.StartsWith(prefix, StringComparison.Ordinal)) return true;
            }
            return false;
        }

        public string ReadAllText(string path)
        {
            byte[] bytes = ReadAllBytes(path);
            int offset = bytes.Length >= 3 && bytes[0] == 0xEF && bytes[1] == 0xBB && bytes[2] == 0xBF ? 3 : 0;
            return Utf8.GetString(bytes, offset, bytes.Length - offset);
        }

        public byte[] ReadAllBytes(string path)
        {
            byte[] bytes;
            if (!_files.TryGetValue(PathUtil.Normalize(path), out bytes)) throw new FileNotFoundException("file not found", path);
            return bytes;
        }

        public IReadOnlyList<string> EnumerateFiles(string directory, bool recursive)
        {
            string prefix = PathUtil.Normalize(directory);
            if (prefix.Length > 0) prefix += "/";
            var result = new List<string>();
            foreach (string key in _files.Keys)
            {
                if (!key.StartsWith(prefix, StringComparison.Ordinal)) continue;
                string rest = key.Substring(prefix.Length);
                if (!recursive && rest.IndexOf('/') >= 0) continue;
                result.Add(key);
            }
            return result;
        }

        public void WriteAllBytes(string path, byte[] content) => AddFile(path, content);

        public void CreateDirectory(string path)
        {
        }
    }

    public sealed class DiskFileSystem : IFileSystem
    {
        private readonly string _root;
        private static readonly UTF8Encoding Utf8 = new UTF8Encoding(false);

        public DiskFileSystem(string root)
        {
            _root = string.IsNullOrEmpty(root) ? Directory.GetCurrentDirectory() : Path.GetFullPath(root);
        }

        public string Root => _root;

        private string Resolve(string path)
        {
            string normalized = PathUtil.Normalize(path);
            return PathUtil.IsRooted(normalized) ? normalized : Path.Combine(_root, normalized.Replace('/', Path.DirectorySeparatorChar));
        }

        public bool FileExists(string path) => File.Exists(Resolve(path));

        public bool DirectoryExists(string path) => Directory.Exists(Resolve(path));

        public string ReadAllText(string path)
        {
            byte[] bytes = File.ReadAllBytes(Resolve(path));
            int offset = bytes.Length >= 3 && bytes[0] == 0xEF && bytes[1] == 0xBB && bytes[2] == 0xBF ? 3 : 0;
            return Utf8.GetString(bytes, offset, bytes.Length - offset);
        }

        public byte[] ReadAllBytes(string path) => File.ReadAllBytes(Resolve(path));

        public IReadOnlyList<string> EnumerateFiles(string directory, bool recursive)
        {
            string resolved = Resolve(directory);
            if (!Directory.Exists(resolved)) return new string[0];
            string[] files = Directory.GetFiles(resolved, "*", recursive ? SearchOption.AllDirectories : SearchOption.TopDirectoryOnly);
            var result = new List<string>(files.Length);
            string rootPrefix = PathUtil.Normalize(_root);
            foreach (string file in files)
            {
                string normalized = PathUtil.Normalize(file);
                if (normalized.StartsWith(rootPrefix + "/", StringComparison.OrdinalIgnoreCase)) normalized = normalized.Substring(rootPrefix.Length + 1);
                result.Add(normalized);
            }
            result.Sort(StringComparer.Ordinal);
            return result;
        }

        public void WriteAllBytes(string path, byte[] content)
        {
            string resolved = Resolve(path);
            string directory = Path.GetDirectoryName(resolved);
            if (!string.IsNullOrEmpty(directory)) Directory.CreateDirectory(directory);
            File.WriteAllBytes(resolved, content);
        }

        public void CreateDirectory(string path) => Directory.CreateDirectory(Resolve(path));
    }
}
