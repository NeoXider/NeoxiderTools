using System;
using UnityEngine;
using UnityEngine.UIElements;
using Neo.HtmlToUitk.Hosting;

namespace Neo.HtmlToUitk.Responsive
{
    [DisallowMultipleComponent]
    [AddComponentMenu("Neoxider/HTML to UITK/Breakpoint Watcher")]
    public sealed class BreakpointWatcher : MonoBehaviour
    {
        [SerializeField] private UIDocument document;
        [SerializeField] private TextAsset mediaQueries;
        private BreakpointController _controller;
        private UitkRootConnection _connection;
        private bool _manualRoot;
        private VisualElement _configuredRoot;
        public BreakpointController Controller => _controller;
        public string LastError { get; private set; }
        private void OnEnable() { if (_manualRoot) Rebuild(_configuredRoot); else Connect(); }
        private void Update() { _connection?.Poll(); }
        private void OnDisable() { Dispose(); }
        private void OnDestroy() { Dispose(); _configuredRoot = null; }
        public void Configure(UIDocument newDocument, TextAsset newMediaQueries)
        {
            document = newDocument; mediaQueries = newMediaQueries; _manualRoot = false; _configuredRoot = null;
            if (isActiveAndEnabled) Connect();
        }
        public void ConfigureRoot(VisualElement root, TextAsset newMediaQueries)
        {
            Dispose(); _manualRoot = true; _configuredRoot = root; mediaQueries = newMediaQueries;
            if (isActiveAndEnabled) Rebuild(root);
        }
        private void Connect()
        {
            if (_manualRoot) return;
            Dispose(); _connection = new UitkRootConnection(gameObject, document, Rebuild);
        }
        private void Rebuild(VisualElement root)
        {
            _controller?.Dispose(); _controller = null; LastError = null;
            if (root == null || mediaQueries == null) return;
            try { _controller = new BreakpointController(root, BreakpointSet.Parse(mediaQueries.text)); }
            catch (Exception exception) { LastError = exception.Message; NeoDiagnostics.LogError("[HtmlToUitk] Responsive root could not bind: " + LastError, this); }
        }
        private void Dispose()
        {
            _connection?.Dispose(); _connection = null;
            _controller?.Dispose(); _controller = null;
        }
    }
}
