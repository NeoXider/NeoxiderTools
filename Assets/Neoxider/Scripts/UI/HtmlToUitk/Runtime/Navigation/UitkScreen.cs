using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;
using Neo.HtmlToUitk.Animation;
using Neo.HtmlToUitk.Actions;
using Neo.HtmlToUitk.Hosting;
using Neo.HtmlToUitk.Localization;
using Neo.HtmlToUitk.Responsive;

namespace Neo.HtmlToUitk.Navigation
{
    [DisallowMultipleComponent]
    [AddComponentMenu("Neoxider/HTML to UITK/Screen")]
    public sealed class UitkScreen : MonoBehaviour
    {
        [SerializeField] private UIDocument document;
        [SerializeField] private TextAsset navigation;
        [SerializeField] private TextAsset animations;
        [SerializeField] private TextAsset mediaQueries;
        [SerializeField] private string hiddenClass = "is-hidden";
        [SerializeField] private string activeClass = "is-active";
        [SerializeField] private bool startOnEnable = true;
        [SerializeField] private bool backOnCancel = true;
        [SerializeField] private bool reducedMotion;
        [SerializeField] private bool manageBreakpoints = true;
        private readonly List<IRouterHook> _pendingHooks = new List<IRouterHook>();
        private readonly UitkActionBindings _actions = new UitkActionBindings();
        private UitkRootConnection _connection;
        private VisualElement _root;
        private UiDocumentPageBackend _backend;
        private EventCallback<NavigationCancelEvent> _cancelCallback;
        private bool _manualRoot;
        private VisualElement _configuredRoot;
        private int _connectionGeneration;

        public UitkRouter Router { get; private set; }
        public UitkAnimator Animator { get; private set; }
        public BreakpointController Breakpoints { get; private set; }
        public UitkActionBindings Actions => _actions;
        public VisualElement Root => _root;
        public string LastError { get; private set; }
        public ILocalizer Localizer { get; set; }
        public IReadOnlyList<string> MissingElements => _backend != null ? _backend.MissingElements : (IReadOnlyList<string>)Array.Empty<string>();

        private void OnEnable() { if (_manualRoot) Rebuild(_configuredRoot); else Connect(); }
        private void Update() { _connection?.Poll(); }
        private void OnDisable() { Disconnect(); }
        private void OnDestroy() { Disconnect(); _actions.Dispose(); _configuredRoot = null; _pendingHooks.Clear(); }

        public void Configure(UIDocument newDocument, TextAsset nav, TextAsset anim = null, TextAsset media = null, bool ownResponsiveLayout = true)
        {
            document = newDocument; navigation = nav; animations = anim; mediaQueries = media;
            manageBreakpoints = ownResponsiveLayout; _manualRoot = false; _configuredRoot = null;
            if (isActiveAndEnabled) Connect();
        }

        /// <summary>Explicit root path for custom hosts; component roots are handled by the compatibility adapter.</summary>
        public void ConfigureRoot(VisualElement root, TextAsset nav, TextAsset anim = null, TextAsset media = null, bool ownResponsiveLayout = true)
        {
            Disconnect(); _manualRoot = true; _configuredRoot = root;
            navigation = nav; animations = anim; mediaQueries = media; manageBreakpoints = ownResponsiveLayout;
            if (isActiveAndEnabled) Rebuild(root);
        }
        public void ReloadRoot(VisualElement root)
        {
            Disconnect(); _manualRoot = true; _configuredRoot = root;
            if (isActiveAndEnabled) Rebuild(root);
        }
        public IDisposable RegisterAction(string buttonName, Action action) => _actions.Bind(buttonName, action);
        public IDisposable RegisterAction(string buttonName, Action<Button> action) => _actions.Bind(buttonName, action);
        public void AddHook(IRouterHook hook)
        {
            if (hook == null || _pendingHooks.Contains(hook)) return;
            _pendingHooks.Add(hook); Router?.AddHook(hook);
        }
        public void RemoveHook(IRouterHook hook) { _pendingHooks.Remove(hook); Router?.RemoveHook(hook); }
        public void Relocalize() { if (_root != null && Localizer != null) UitkLocalization.Apply(_root, Localizer); }
        private bool IsReducedMotion() => reducedMotion || (UitkAnimator.SystemReducedMotion != null && UitkAnimator.SystemReducedMotion());

        private void Connect()
        {
            if (_manualRoot) return;
            Disconnect();
            int generation = _connectionGeneration;
            UitkRootConnection connection = new UitkRootConnection(gameObject, document,
                root => { if (generation == _connectionGeneration && isActiveAndEnabled && !_manualRoot) Rebuild(root); });
            if (generation == _connectionGeneration && isActiveAndEnabled && !_manualRoot) _connection = connection;
            else connection.Dispose();
        }
        private void Disconnect()
        {
            _connectionGeneration++;
            _connection?.Dispose(); _connection = null;
            TeardownProjection(); _actions.Rebind(null); _root = null;
        }
        private void Rebuild(VisualElement root)
        {
            TeardownProjection(); _root = root; _actions.Rebind(root); LastError = null;
            if (root == null) return;
            try
            {
                Relocalize();
                if (manageBreakpoints && mediaQueries != null) Breakpoints = new BreakpointController(root, BreakpointSet.Parse(mediaQueries.text));
                if (animations != null)
                {
                    Animator = new UitkAnimator(root, AnimSpec.Parse(animations.text)) { ReducedMotion = reducedMotion };
                    Animator.PlayOnLoad();
                }
                if (navigation == null) return;
                NavSpec spec = NavSpec.Parse(navigation.text);
                _backend = new UiDocumentPageBackend(root, new PageClasses { Hidden = hiddenClass, Active = activeClass });
                Router = new UitkRouter(spec, _backend, new RouterOptions { ReducedMotion = IsReducedMotion });
                foreach (IRouterHook hook in _pendingHooks) Router.AddHook(hook);
                Router.WireTriggers();
                if (Animator != null) Animator.AttachRouter(Router, _backend.PageElement);
                if (backOnCancel)
                {
                    UitkRouter owner = Router;
                    _cancelCallback = evt => { if (ReferenceEquals(Router, owner) && owner.Back()) evt.StopPropagation(); };
                    root.RegisterCallback(_cancelCallback);
                }
                if (startOnEnable) Router.Start();
            }
            catch (Exception exception)
            {
                TeardownProjection(); _actions.Rebind(null);
                LastError = exception.Message;
                Debug.LogError("[HtmlToUitk] Screen could not bind: " + LastError, this);
            }
        }
        private void TeardownProjection()
        {
            if (_root != null && _cancelCallback != null) _root.UnregisterCallback(_cancelCallback);
            _cancelCallback = null;
            UitkAnimator animator = Animator; Animator = null;
            UitkRouter router = Router; Router = null;
            BreakpointController breakpoints = Breakpoints; Breakpoints = null;
            animator?.Dispose();
            router?.Dispose();
            breakpoints?.Dispose();
            _backend = null;
        }
    }
}
