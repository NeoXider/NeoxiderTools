using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using UnityEditor;

namespace Neo.HtmlToUitk.Tooling
{
    public sealed class HtmlToUitkWatcher : IDisposable
    {
        private readonly List<FileSystemWatcher> _watchers = new List<FileSystemWatcher>();
        private readonly ConcurrentQueue<string> _events = new ConcurrentQueue<string>();
        private readonly DebounceGate _gate;
        private readonly Action<IReadOnlyList<string>> _onFire;
        private readonly HashSet<string> _extensions;
        private readonly List<string> _fired = new List<string>();
        private volatile bool _disposed;

        public HtmlToUitkWatcher(double debounceSeconds, IEnumerable<string> extensions, Action<IReadOnlyList<string>> onFire)
        {
            _gate = new DebounceGate(debounceSeconds);
            _onFire = onFire;
            _extensions = new HashSet<string>(extensions, StringComparer.OrdinalIgnoreCase);
            EditorApplication.update += Poll;
            AssemblyReloadEvents.beforeAssemblyReload += Dispose;
        }

        public int WatchedDirectories => _watchers.Count;

        public double DebounceSeconds
        {
            get => _gate.QuietSeconds;
            set => _gate.QuietSeconds = value;
        }

        public void Watch(IEnumerable<string> directories)
        {
            if (_disposed)
                throw new ObjectDisposedException(nameof(HtmlToUitkWatcher));
            foreach (FileSystemWatcher watcher in _watchers)
                watcher.Dispose();
            _watchers.Clear();
            HashSet<string> seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            foreach (string directory in directories)
            {
                if (string.IsNullOrEmpty(directory) || !Directory.Exists(directory) || !seen.Add(GlobMatcher.Normalize(directory)))
                    continue;
                FileSystemWatcher watcher = new FileSystemWatcher(directory)
                {
                    IncludeSubdirectories = true,
                    NotifyFilter = NotifyFilters.LastWrite | NotifyFilters.FileName | NotifyFilters.Size | NotifyFilters.CreationTime
                };
                watcher.Changed += OnFileEvent;
                watcher.Created += OnFileEvent;
                watcher.Deleted += OnFileEvent;
                watcher.Renamed += OnRenamed;
                _watchers.Add(watcher);
                watcher.EnableRaisingEvents = true;
            }
        }

        public void Notify(string path)
        {
            if (!_disposed)
                _events.Enqueue(path ?? string.Empty);
        }

        public void Dispose()
        {
            if (_disposed)
                return;
            _disposed = true;
            EditorApplication.update -= Poll;
            AssemblyReloadEvents.beforeAssemblyReload -= Dispose;
            foreach (FileSystemWatcher watcher in _watchers)
                watcher.Dispose();
            _watchers.Clear();
            _gate.Clear();
        }

        private void OnFileEvent(object sender, FileSystemEventArgs e)
        {
            Accept(e.FullPath);
        }

        private void OnRenamed(object sender, RenamedEventArgs e)
        {
            Accept(e.OldFullPath);
            Accept(e.FullPath);
        }

        private void Accept(string path)
        {
            if (_disposed)
                return;
            string name = Path.GetFileName(path);
            if (name.StartsWith(".", StringComparison.Ordinal))
                return;
            string extension = Path.GetExtension(path);
            if (_extensions.Count == 0 || _extensions.Contains(extension))
                _events.Enqueue(path);
        }

        private void Poll()
        {
            if (_disposed)
                return;
            double now = EditorApplication.timeSinceStartup;
            while (_events.TryDequeue(out string path))
                _gate.Touch(now, GlobMatcher.Normalize(path));
            if (_gate.TryFire(now, _fired))
                _onFire?.Invoke(new List<string>(_fired));
        }
    }
}
