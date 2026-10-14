using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using UnityEngine;
using UnityEngine.UIElements;
using Neo.HtmlToUitk.Selectors;

namespace Neo.HtmlToUitk.Responsive
{
    public sealed class BreakpointController : IDisposable
    {
        private sealed class Node : ISelectNode
        {
            private readonly VisualElement _element;
            public Node(VisualElement element) { _element = element; }
            public string TypeName => _element.GetType().Name;
            public string Name => _element.name;
            public ISelectNode Parent => _element.parent != null ? new Node(_element.parent) : null;
            public bool HasClass(string name) => _element.ClassListContains(name);
        }
        private static readonly ConditionalWeakTable<VisualElement, BreakpointController> Owners = new ConditionalWeakTable<VisualElement, BreakpointController>();
        private readonly BreakpointSet _set;
        private readonly VisualElement _classTarget;
        private readonly Dictionary<BreakpointRule, SelectorList> _selectors = new Dictionary<BreakpointRule, SelectorList>();
        private readonly Dictionary<VisualElement, HashSet<string>> _owned = new Dictionary<VisualElement, HashSet<string>>();
        private readonly List<string> _active = new List<string>();
        private readonly List<string> _scratch = new List<string>();
        private VisualElement _panelRoot;
        private IVisualElementScheduledItem _initialRefresh;
        private bool _disposed;

        public BreakpointController(VisualElement root, BreakpointSet set)
        {
            if (root == null) throw new ArgumentNullException(nameof(root));
            _set = set ?? throw new ArgumentNullException(nameof(set));
            VisualElement named = string.IsNullOrEmpty(set.Root) ? null : root.Q<VisualElement>(set.Root);
            _classTarget = root.name == set.Root ? root : named ?? root;
            foreach (BreakpointRule rule in set.Rules)
            {
                if (string.IsNullOrWhiteSpace(rule.ClassName) || rule.ClassName.IndexOfAny(new[] { ' ', '.', '#', '\t' }) >= 0)
                    throw new FormatException("Invalid media class: " + rule.ClassName);
                if (!string.IsNullOrEmpty(rule.Selector))
                {
                    SelectorList selector = SelectorList.Parse(rule.Selector);
                    if (!selector.IsValid) throw new FormatException("Unsupported media selector: " + selector.Error);
                    _selectors.Add(rule, selector);
                }
            }
            if (Owners.TryGetValue(_classTarget, out BreakpointController existing))
                throw new InvalidOperationException("This root already has a responsive owner. Dispose it or disable automatic breakpoints on the screen.");
            Owners.Add(_classTarget, this);
            _classTarget.RegisterCallback<GeometryChangedEvent>(OnGeometry);
            _classTarget.RegisterCallback<AttachToPanelEvent>(OnAttach, TrickleDown.TrickleDown);
            _classTarget.RegisterCallback<DetachFromPanelEvent>(OnDetach, TrickleDown.TrickleDown);
            if (_classTarget.panel != null) Attach();
            Refresh();
        }

        public IReadOnlyList<string> Active => _active.AsReadOnly();
        public event Action<IReadOnlyList<string>> Changed;

        public void Refresh()
        {
            if (_disposed) return;
            VisualElement viewport = _panelRoot ?? _classTarget;
            Rect size = viewport.layout;
            ApplyViewport(size.width, size.height);
        }

        /// <summary>Explicit viewport path for render-texture hosts and an external layout owner.</summary>
        public void ApplyViewport(float width, float height)
        {
            if (_disposed || float.IsNaN(width) || float.IsInfinity(width) || float.IsNaN(height) || float.IsInfinity(height) || width <= 0f || height <= 0f) return;
            _set.Evaluate(width, height, _scratch);
            Dictionary<VisualElement, HashSet<string>> desired = new Dictionary<VisualElement, HashSet<string>>();
            foreach (BreakpointRule rule in _set.Rules)
            {
                if (!rule.Query.Matches(width, height)) continue;
                if (!_selectors.TryGetValue(rule, out SelectorList selector)) AddDesired(desired, _classTarget, rule.ClassName);
                else
                {
                    Stack<VisualElement> pending = new Stack<VisualElement>();
                    pending.Push(_classTarget);
                    while (pending.Count > 0)
                    {
                        VisualElement element = pending.Pop();
                        if (selector.Matches(new Node(element))) AddDesired(desired, element, rule.ClassName);
                        for (int i = element.childCount - 1; i >= 0; i--) pending.Push(element[i]);
                    }
                }
            }
            foreach (KeyValuePair<VisualElement, HashSet<string>> old in _owned)
                foreach (string className in old.Value)
                    if (!desired.TryGetValue(old.Key, out HashSet<string> needed) || !needed.Contains(className)) old.Key.RemoveFromClassList(className);
            Dictionary<VisualElement, HashSet<string>> next = new Dictionary<VisualElement, HashSet<string>>();
            foreach (KeyValuePair<VisualElement, HashSet<string>> target in desired)
                foreach (string className in target.Value)
                {
                    bool wasOwned = _owned.TryGetValue(target.Key, out HashSet<string> previous) && previous.Contains(className);
                    if (!target.Key.ClassListContains(className) || wasOwned)
                    {
                        target.Key.AddToClassList(className);
                        AddDesired(next, target.Key, className);
                    }
                }
            _owned.Clear();
            foreach (KeyValuePair<VisualElement, HashSet<string>> item in next) _owned.Add(item.Key, item.Value);
            if (!BreakpointSet.SameSet(_scratch, _active))
            {
                _active.Clear();
                _active.AddRange(_scratch);
                Changed?.Invoke(_active.AsReadOnly());
            }
        }

        private static void AddDesired(Dictionary<VisualElement, HashSet<string>> targets, VisualElement element, string className)
        {
            if (!targets.TryGetValue(element, out HashSet<string> classes)) { classes = new HashSet<string>(StringComparer.Ordinal); targets.Add(element, classes); }
            classes.Add(className);
        }
        private void OnGeometry(GeometryChangedEvent evt) { Refresh(); }
        private void OnAttach(AttachToPanelEvent evt) { if (ReferenceEquals(evt.target, _classTarget)) Attach(); Refresh(); }
        private void OnDetach(DetachFromPanelEvent evt)
        {
            if (!ReferenceEquals(evt.target, _classTarget))
            {
                if (!(evt.target is VisualElement detached)) return;
                List<VisualElement> removed = new List<VisualElement>();
                foreach (KeyValuePair<VisualElement, HashSet<string>> entry in _owned)
                    if (ReferenceEquals(entry.Key, detached) || detached.Contains(entry.Key))
                    {
                        foreach (string className in entry.Value) entry.Key.RemoveFromClassList(className);
                        removed.Add(entry.Key);
                    }
                foreach (VisualElement element in removed) _owned.Remove(element);
                return;
            }
            _initialRefresh?.Pause();
            if (_panelRoot != null) _panelRoot.UnregisterCallback<GeometryChangedEvent>(OnGeometry);
            _panelRoot = null;
            ClearOwnedClasses();
        }
        private void Attach()
        {
            VisualElement root = _classTarget.panel?.visualTree;
            if (ReferenceEquals(root, _panelRoot)) return;
            if (_panelRoot != null) _panelRoot.UnregisterCallback<GeometryChangedEvent>(OnGeometry);
            _panelRoot = root;
            if (_panelRoot != null && !ReferenceEquals(_panelRoot, _classTarget)) _panelRoot.RegisterCallback<GeometryChangedEvent>(OnGeometry);
            _initialRefresh?.Pause();
            _initialRefresh = _classTarget.schedule.Execute(Refresh);
        }
        private void ClearOwnedClasses()
        {
            foreach (KeyValuePair<VisualElement, HashSet<string>> item in _owned)
                foreach (string className in item.Value) item.Key.RemoveFromClassList(className);
            _owned.Clear();
            _active.Clear();
        }
        public void Dispose()
        {
            if (_disposed) return;
            _disposed = true;
            _initialRefresh?.Pause();
            _classTarget.UnregisterCallback<GeometryChangedEvent>(OnGeometry);
            _classTarget.UnregisterCallback<AttachToPanelEvent>(OnAttach, TrickleDown.TrickleDown);
            _classTarget.UnregisterCallback<DetachFromPanelEvent>(OnDetach, TrickleDown.TrickleDown);
            if (_panelRoot != null) _panelRoot.UnregisterCallback<GeometryChangedEvent>(OnGeometry);
            _panelRoot = null;
            ClearOwnedClasses();
            Owners.Remove(_classTarget);
            Changed = null;
        }
    }
}
