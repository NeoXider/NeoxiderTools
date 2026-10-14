using System;
using System.Collections.Generic;
using System.Text;

namespace HtmlToUitk.Core.Text
{
    public static class PathUtil
    {
        public static string Normalize(string path)
        {
            if (string.IsNullOrEmpty(path)) return "";
            string text = path.Replace('\\', '/');
            string drive = null;
            if (text.Length >= 2 && text[1] == ':' && char.IsLetter(text[0]))
            {
                drive = text.Substring(0, 2);
                text = text.Substring(2);
            }
            bool rooted = text.StartsWith("/", StringComparison.Ordinal);
            var parts = new List<string>();
            foreach (string part in text.Split('/'))
            {
                if (part.Length == 0 || part == ".") continue;
                if (part == "..")
                {
                    if (parts.Count > 0 && parts[parts.Count - 1] != "..") parts.RemoveAt(parts.Count - 1);
                    else if (!rooted && drive == null) parts.Add("..");
                    continue;
                }
                parts.Add(part);
            }
            var sb = new StringBuilder();
            if (drive != null) sb.Append(drive);
            if (rooted) sb.Append('/');
            sb.Append(string.Join("/", parts));
            return sb.ToString();
        }

        public static string Combine(string directory, string relative)
        {
            if (string.IsNullOrEmpty(relative)) return Normalize(directory);
            string rel = relative.Replace('\\', '/');
            if (IsRooted(rel) || string.IsNullOrEmpty(directory)) return Normalize(rel);
            return Normalize(directory + "/" + rel);
        }

        public static bool IsRooted(string path)
        {
            if (string.IsNullOrEmpty(path)) return false;
            if (path[0] == '/' || path[0] == '\\') return true;
            return path.Length >= 2 && path[1] == ':' && char.IsLetter(path[0]);
        }

        public static string Directory(string path)
        {
            string text = Normalize(path);
            int slash = text.LastIndexOf('/');
            if (slash < 0) return "";
            if (slash == 0) return "/";
            return text.Substring(0, slash);
        }

        public static string FileName(string path)
        {
            string text = Normalize(path);
            int slash = text.LastIndexOf('/');
            return slash < 0 ? text : text.Substring(slash + 1);
        }

        public static string Stem(string path)
        {
            string name = FileName(path);
            int dot = name.LastIndexOf('.');
            return dot <= 0 ? name : name.Substring(0, dot);
        }

        public static string Extension(string path)
        {
            string name = FileName(path);
            int dot = name.LastIndexOf('.');
            return dot <= 0 ? "" : name.Substring(dot);
        }

        public static string RelativeTo(string fromDirectory, string target)
        {
            string[] from = Normalize(fromDirectory).Split(new[] { '/' }, StringSplitOptions.RemoveEmptyEntries);
            string[] to = Normalize(target).Split(new[] { '/' }, StringSplitOptions.RemoveEmptyEntries);
            int common = 0;
            while (common < from.Length && common < to.Length - 1 && string.Equals(from[common], to[common], StringComparison.Ordinal)) common++;
            var parts = new List<string>();
            for (int i = common; i < from.Length; i++) parts.Add("..");
            for (int i = common; i < to.Length; i++) parts.Add(to[i]);
            return string.Join("/", parts);
        }
    }
}
