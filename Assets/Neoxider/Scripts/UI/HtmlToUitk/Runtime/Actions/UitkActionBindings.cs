using System;
using System.Collections.Generic;
using UnityEngine.UIElements;

namespace Neo.HtmlToUitk.Actions
{
    /// <summary>Named native Button actions with ownership-safe replacement and root reload.</summary>
    public sealed class UitkActionBindings : IDisposable
    {
        private sealed class Entry : IDisposable
        {
            public string Name;
            public Action<Button> Action;
            public Button Button;
            public Action Click;
            public UitkActionBindings Owner;
            public void Dispose() { Owner?.Remove(this); }
        }

        private readonly Dictionary<string, Entry> _entries = new Dictionary<string, Entry>(StringComparer.Ordinal);
        private VisualElement _root;
        private bool _disposed;
        private bool _suspended;

        public UitkActionBindings(VisualElement root = null) { Rebind(root); }
        public int Count => _entries.Count;

        public IDisposable Bind(string name, Action action)
        {
            if (action == null) throw new ArgumentNullException(nameof(action));
            return Bind(name, button => action());
        }

        public IDisposable Bind(string name, Action<Button> action)
        {
            if (_disposed) throw new ObjectDisposedException(nameof(UitkActionBindings));
            if (string.IsNullOrWhiteSpace(name)) throw new ArgumentException("Button name/id is required.", nameof(name));
            if (action == null) throw new ArgumentNullException(nameof(action));
            string key = name[0] == '#' ? name.Substring(1) : name;
            if (key.Length == 0) throw new ArgumentException("Button id cannot be empty.", nameof(name));
            if (_entries.TryGetValue(key, out Entry old)) Remove(old);
            Entry entry = new Entry { Name = key, Action = action, Owner = this };
            _entries.Add(key, entry);
            Wire(entry);
            return entry;
        }

        public IDisposable Bind<T>(string name, Func<T> payload, Action<T> action)
        {
            if (payload == null) throw new ArgumentNullException(nameof(payload));
            if (action == null) throw new ArgumentNullException(nameof(action));
            return Bind(name, button => action(payload()));
        }

        public void Rebind(VisualElement root)
        {
            if (_disposed) throw new ObjectDisposedException(nameof(UitkActionBindings));
            foreach (Entry entry in _entries.Values) Unwire(entry);
            if (_root != null)
            {
                _root.UnregisterCallback<AttachToPanelEvent>(OnAttach, TrickleDown.TrickleDown);
                _root.UnregisterCallback<DetachFromPanelEvent>(OnDetach, TrickleDown.TrickleDown);
            }
            _root = root;
            _suspended = false;
            if (_root != null)
            {
                _root.RegisterCallback<AttachToPanelEvent>(OnAttach, TrickleDown.TrickleDown);
                _root.RegisterCallback<DetachFromPanelEvent>(OnDetach, TrickleDown.TrickleDown);
            }
            Refresh();
        }

        public void Refresh()
        {
            if (_disposed) return;
            foreach (Entry entry in _entries.Values) { Unwire(entry); Wire(entry); }
        }

        private void Wire(Entry entry)
        {
            if (_root == null || _suspended || _disposed) return;
            entry.Button = _root as Button;
            if (entry.Button == null || entry.Button.name != entry.Name) entry.Button = _root.Q<Button>(entry.Name);
            if (entry.Button == null) return;
            Entry captured = entry;
            Button bound = entry.Button;
            Action click = null;
            click = () =>
            {
                if (!_disposed && !_suspended && captured.Owner == this && ReferenceEquals(captured.Click, click)
                    && ReferenceEquals(captured.Button, bound) && bound.enabledInHierarchy)
                    captured.Action(bound);
            };
            entry.Click = click;
            entry.Button.clicked += entry.Click;
        }

        private static void Unwire(Entry entry)
        {
            if (entry.Button != null && entry.Click != null) entry.Button.clicked -= entry.Click;
            entry.Button = null;
            entry.Click = null;
        }
        private void Remove(Entry entry)
        {
            if (_entries.TryGetValue(entry.Name, out Entry current) && ReferenceEquals(current, entry)) _entries.Remove(entry.Name);
            Unwire(entry);
            entry.Owner = null;
            entry.Action = null;
        }
        private void OnAttach(AttachToPanelEvent evt)
        {
            if (ReferenceEquals(evt.target, _root)) _suspended = false;
            if (!_suspended) Refresh();
        }
        private void OnDetach(DetachFromPanelEvent evt)
        {
            if (!ReferenceEquals(evt.target, _root))
            {
                VisualElement detached = evt.target as VisualElement;
                foreach (Entry entry in _entries.Values)
                    if (entry.Button != null && (ReferenceEquals(entry.Button, detached) || (detached != null && detached.Contains(entry.Button)))) Unwire(entry);
                return;
            }
            _suspended = true;
            foreach (Entry entry in _entries.Values) Unwire(entry);
        }
        public void Dispose()
        {
            if (_disposed) return;
            Rebind(null);
            _disposed = true;
            foreach (Entry entry in _entries.Values) { entry.Owner = null; entry.Action = null; }
            _entries.Clear();
        }
    }
}
