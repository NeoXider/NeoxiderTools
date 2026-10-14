using System;
using UnityEngine;
using UnityEngine.UIElements;

namespace Neo.HtmlToUitk.Hosting
{
    /// <summary>One version-aware root/reload subscription; no Neo.UIKit dependency.</summary>
    public sealed class UitkRootConnection : IDisposable
    {
        private readonly Action<VisualElement> _changed;
        private UIDocument _document;
        private Component _renderer;
        private object _reload;
        private bool _disposed;

        public UitkRootConnection(GameObject host, UIDocument document, Action<VisualElement> changed)
        {
            _changed = changed ?? throw new ArgumentNullException(nameof(changed));
            _renderer = PanelCompatibility.Find(host);
            if (_renderer != null) _reload = PanelCompatibility.Register(_renderer, SetRoot);
            else { _document = document != null ? document : host != null ? host.GetComponent<UIDocument>() : null; Poll(); }
        }

        public VisualElement Root { get; private set; }
        public bool UsesPanelRenderer => _renderer != null;
        public void Poll()
        {
            if (!_disposed && _document != null && _document.rootVisualElement != Root) SetRoot(_document.rootVisualElement);
        }

        private void SetRoot(VisualElement root)
        {
            if (_disposed) return;
            Root = root;
            _changed(root); // Same-root UI reload still invalidates all old tree bindings.
        }

        public void Dispose()
        {
            if (_disposed) return;
            _disposed = true;
            PanelCompatibility.Unregister(_renderer, _reload);
            _renderer = null;
            _reload = null;
            _document = null;
            Root = null;
        }
    }

    /// <summary>The sole Unity version switch for PanelRenderer, introduced in Unity 6000.5.</summary>
    internal static class PanelCompatibility
    {
#if UNITY_6000_5_OR_NEWER
        private sealed class Subscription
        {
            private readonly Action<VisualElement> _changed;
            public Subscription(Action<VisualElement> changed)
            {
                _changed = changed;
#if UNITY_6000_6_OR_NEWER
                Handler = (renderer, root, version) => _changed(root);
#endif
            }
#if UNITY_6000_6_OR_NEWER
            public PanelRenderer.VersionedUIReloadCallback Handler { get; }
#endif
            public void Handle(PanelRenderer renderer, VisualElement root) { _changed(root); }
        }
        public static Component Find(GameObject host) => host != null ? host.GetComponent<PanelRenderer>() : null;
        public static object Register(Component renderer, Action<VisualElement> changed)
        {
            Subscription subscription = new Subscription(changed);
#if UNITY_6000_6_OR_NEWER
            ((PanelRenderer)renderer).RegisterUIReloadCallback(subscription.Handler);
#else
            ((PanelRenderer)renderer).RegisterUIReloadCallback(subscription.Handle);
#endif
            return subscription;
        }
        public static void Unregister(Component renderer, object token)
        {
            if (renderer != null && token is Subscription subscription)
#if UNITY_6000_6_OR_NEWER
                ((PanelRenderer)renderer).UnregisterUIReloadCallback(subscription.Handler);
#else
                ((PanelRenderer)renderer).UnregisterUIReloadCallback(subscription.Handle);
#endif
        }
#else
        public static Component Find(GameObject host) => null;
        public static object Register(Component renderer, Action<VisualElement> changed) => null;
        public static void Unregister(Component renderer, object token) { }
#endif
    }
}
