using UnityEngine;
using UnityEngine.UIElements;

namespace Neo.UI
{
    /// <summary>Which UI Toolkit component this host found on its GameObject.</summary>
    public enum UIMeshRigPanelHostKind
    {
        /// <summary>Nothing to attach to — the element is built but has no panel.</summary>
        None = 0,

        /// <summary>Unity 6.5+ <c>PanelRenderer</c>, the supported world-space renderer.</summary>
        PanelRenderer = 1,

        /// <summary>Legacy <c>UIDocument</c>, used on Unity versions without <c>PanelRenderer</c>.</summary>
        UIDocument = 2
    }

    /// <summary>
    /// Optional scene adapter that inserts a configured <see cref="UIMeshRigElement"/> into a UI Toolkit
    /// panel. UXML / UI Builder users can instantiate the element directly and do not need this component.
    /// <para>
    /// <b>PanelRenderer first.</b> From Unity 6.5 world-space UI Toolkit renders through
    /// <c>PanelRenderer</c>, so that is what this host binds to when it is available: it subscribes to the
    /// UI-reload callback and adds the element to the root the renderer hands out. <c>UIDocument</c> is only
    /// the fallback for editors that predate <c>PanelRenderer</c>; the component is deliberately not
    /// <c>[RequireComponent(typeof(UIDocument))]</c> any more, because that forced the legacy component onto
    /// projects that have already migrated.
    /// </para>
    /// </summary>
    [ExecuteAlways]
    [DisallowMultipleComponent]
    [AddComponentMenu("Neoxider/UI/UI Mesh Rig UI Toolkit Host")]
    [NeoDoc("UI/UIMeshRig.md")]
    public sealed class UIMeshRigUIToolkitHost : MonoBehaviour
    {
        [Header("Source")]
        [Tooltip("Sprite drawn by the element. Without it the element renders nothing.")]
        [SerializeField] private Sprite _sprite;

        [Tooltip("Vertex tint applied to the generated mesh.")]
        [SerializeField] private Color _color = Color.white;

        [Header("Geometry")]
        [Tooltip("Horizontal grid resolution. Below 4 the silhouette cannot bend smoothly.")]
        [Range(2, 40)] [SerializeField] private int _columns = 16;

        [Tooltip("Vertical grid resolution. Below 4 the silhouette cannot bend smoothly.")]
        [Range(2, 40)] [SerializeField] private int _rows = 20;

        [Tooltip("Fits the sprite inside Size without stretching it.")]
        [SerializeField] private bool _preserveAspect = true;

        [Tooltip("Turns the whole rig off without deleting its layout preset.")]
        [SerializeField] private bool _deformationEnabled = true;

        [Tooltip("Element size in panel pixels.")]
        [SerializeField] private Vector2 _size = new Vector2(300f, 300f);

        [Tooltip("Absolute position of the element inside the panel root, in panel pixels.")]
        [SerializeField] private Vector2 _position = new Vector2(40f, 120f);

        [Header("Motion")]
        [Tooltip("Ready-made point layout evaluated procedurally — UI Toolkit has no point child objects.")]
        [SerializeField] private UIMeshRigLayoutPreset _layoutPreset = UIMeshRigLayoutPreset.SimpleBounce;

        [Tooltip("Plays the layout's motion presets. Off leaves the element in its rest pose.")]
        [SerializeField] private bool _motionEnabled = true;

        [Tooltip("Multiplies motion speed for every point in the layout.")]
        [SerializeField] private float _motionSpeed = 1f;

        [Tooltip("Extra cycle offset added on top of each point's own phase.")]
        [SerializeField] private float _motionPhase;

        private UIMeshRigElement _element;
        private VisualElement _attachedRoot;
        private UIMeshRigPanelHostKind _hostKind;
        private Component _panelRenderer;
        private object _panelReloadSubscription;
        private UIDocument _document;

        public UIMeshRigElement Element => _element;
        public Sprite Sprite => _sprite;
        public int Columns => _columns;
        public int Rows => _rows;
        public bool PreserveAspect => _preserveAspect;
        public bool DeformationEnabled => _deformationEnabled;
        public Vector2 Size => _size;
        public Vector2 Position => _position;
        public Color Color => _color;
        public UIMeshRigLayoutPreset LayoutPreset => _layoutPreset;

        /// <summary>Which UI Toolkit component the host is bound to right now.</summary>
        public UIMeshRigPanelHostKind HostKind => _hostKind;

        /// <summary>True once a panel handed the host a root and the element is parented to it.</summary>
        public bool IsAttached => _element != null && _attachedRoot != null && _element.parent == _attachedRoot;

        public void SetSource(Sprite sprite, Color tint)
        {
            _sprite = sprite;
            _color = tint;
            ApplySettings();
        }

        public void SetGridResolution(int columns, int rows)
        {
            _columns = Mathf.Clamp(columns, 2, 40);
            _rows = Mathf.Clamp(rows, 2, 40);
            ApplySettings();
        }

        public void SetPreserveAspect(bool preserveAspect)
        {
            _preserveAspect = preserveAspect;
            ApplySettings();
        }

        public void SetDeformationEnabled(bool enabled)
        {
            _deformationEnabled = enabled;
            ApplySettings();
        }

        public void SetSize(Vector2 size)
        {
            _size = new Vector2(Mathf.Max(1f, size.x), Mathf.Max(1f, size.y));
            ApplySettings();
        }

        public void SetPosition(Vector2 position)
        {
            _position = position;
            ApplySettings();
        }

        public void SetLayoutPreset(UIMeshRigLayoutPreset preset)
        {
            _layoutPreset = preset;
            ApplySettings();
        }

        public UIMeshRigGeometry BuildGeometry(float timeSeconds)
        {
            EnsureElement();
            ApplySettings();
            return _element.BuildGeometry(_size, timeSeconds);
        }

        public void Refresh()
        {
            Unbind();
            Bind();
            ApplySettings();
        }

        private void OnEnable()
        {
            Bind();
        }

        private void OnDisable()
        {
            Unbind();
        }

        private void OnValidate()
        {
            _columns = Mathf.Clamp(_columns, 2, 40);
            _rows = Mathf.Clamp(_rows, 2, 40);
            _size = new Vector2(Mathf.Max(1f, _size.x), Mathf.Max(1f, _size.y));
            ApplySettings();
        }

        private void Bind()
        {
            EnsureElement();
            _panelRenderer = UIMeshRigPanelCompat.FindPanelRenderer(gameObject);
            if (_panelRenderer != null)
            {
                _hostKind = UIMeshRigPanelHostKind.PanelRenderer;
                // WHY: PanelRenderer rebuilds its tree and re-raises this callback on the same root, so the
                // element is re-parented (not duplicated) every time and AttachTo stays idempotent.
                _panelReloadSubscription = UIMeshRigPanelCompat.RegisterReload(_panelRenderer, AttachTo);
                return;
            }

            _document = GetComponent<UIDocument>();
            if (_document == null)
            {
                _hostKind = UIMeshRigPanelHostKind.None;
                return;
            }

            _hostKind = UIMeshRigPanelHostKind.UIDocument;
            AttachTo(_document.rootVisualElement);
        }

        private void Unbind()
        {
            if (_panelRenderer != null)
            {
                UIMeshRigPanelCompat.UnregisterReload(_panelRenderer, _panelReloadSubscription);
            }

            _panelRenderer = null;
            _panelReloadSubscription = null;
            _document = null;
            _attachedRoot = null;
            if (_element != null)
            {
                _element.RemoveFromHierarchy();
            }
        }

        private void AttachTo(VisualElement root)
        {
            EnsureElement();
            _attachedRoot = root;
            if (root == null)
            {
                _element.RemoveFromHierarchy();
                return;
            }

            if (_element.parent != root)
            {
                _element.RemoveFromHierarchy();
                root.Add(_element);
            }

            ApplySettings();
        }

        private void EnsureElement()
        {
            if (_element == null)
            {
                _element = new UIMeshRigElement
                {
                    name = "ui-mesh-rig"
                };
            }
        }

        private void ApplySettings()
        {
            if (_element == null)
            {
                return;
            }

            _element.Sprite = _sprite;
            _element.Columns = _columns;
            _element.Rows = _rows;
            _element.PreserveAspect = _preserveAspect;
            _element.DeformationEnabled = _deformationEnabled;
            _element.Tint = _color;
            _element.LayoutPreset = _layoutPreset;
            _element.MotionEnabled = _motionEnabled;
            _element.MotionSpeed = _motionSpeed;
            _element.MotionPhase = _motionPhase;
            _element.style.width = _size.x;
            _element.style.height = _size.y;
            _element.style.position = UnityEngine.UIElements.Position.Absolute;
            _element.style.left = _position.x;
            _element.style.top = _position.y;
        }
    }

    /// <summary>
    /// The single place that knows whether this editor has UI Toolkit's <c>PanelRenderer</c>. The host, the
    /// UI Mesh Rig menu and the tests go through it, so the Unity version threshold is written exactly once.
    /// </summary>
    // WHY: PanelRenderer first ships in Unity 6.5 (6000.5). The 6000.5 Scripting API documents
    // UIElements.PanelRenderer (RegisterUIReloadCallback, panelSettings) and "What's new in Unity 6.5" lists
    // "Added the Panel Renderer component that replaces UI Document"; the 6000.4 Scripting API has no
    // PanelRenderer page (404) while its UIDocument page exists. Local editors agree: the type is absent in
    // 6000.3.14f1 and present in 6000.6.0f1. A per-call-site #if went stale once, hence one switch here.
    internal static class UIMeshRigPanelCompat
    {
        /// <summary>True when the running editor/player has <c>PanelRenderer</c>.</summary>
        public static bool HasPanelRenderer => Backend.HasPanelRenderer;

        /// <summary>The <c>PanelRenderer</c> on <paramref name="gameObject"/>, or null (always null without it).</summary>
        public static Component FindPanelRenderer(GameObject gameObject)
        {
            return gameObject != null ? Backend.FindPanelRenderer(gameObject) : null;
        }

        /// <summary>
        /// Adds the panel component a fresh host should bind to — <c>PanelRenderer</c> when it exists, otherwise
        /// the legacy <c>UIDocument</c> — and assigns <paramref name="panelSettings"/> to it.
        /// </summary>
        public static Component AddPanelComponent(GameObject gameObject, PanelSettings panelSettings)
        {
            return Backend.AddPanelComponent(gameObject, panelSettings);
        }

        /// <summary>
        /// Subscribes <paramref name="onReload"/> to the renderer's UI-reload callback. Returns the token
        /// <see cref="UnregisterReload"/> needs, or null when nothing was subscribed.
        /// </summary>
        public static object RegisterReload(Component panelRenderer, System.Action<VisualElement> onReload)
        {
            return panelRenderer != null && onReload != null ? Backend.RegisterReload(panelRenderer, onReload) : null;
        }

        /// <summary>Removes a subscription made by <see cref="RegisterReload"/>. Null tokens are ignored.</summary>
        public static void UnregisterReload(Component panelRenderer, object token)
        {
            if (panelRenderer != null && token != null)
            {
                Backend.UnregisterReload(panelRenderer, token);
            }
        }

#if UNITY_6000_5_OR_NEWER
        private static class Backend
        {
            public static bool HasPanelRenderer => true;

            public static Component FindPanelRenderer(GameObject gameObject)
            {
                return gameObject.GetComponent<PanelRenderer>();
            }

            public static Component AddPanelComponent(GameObject gameObject, PanelSettings panelSettings)
            {
                PanelRenderer panelRenderer = gameObject.AddComponent<PanelRenderer>();
                panelRenderer.panelSettings = panelSettings;
                return panelRenderer;
            }

            public static object RegisterReload(Component panelRenderer, System.Action<VisualElement> onReload)
            {
                PanelRenderer renderer = panelRenderer as PanelRenderer;
                if (renderer == null)
                {
                    return null;
                }

                ReloadSubscription subscription = new ReloadSubscription(onReload);
                renderer.RegisterUIReloadCallback(subscription.Handle);
                return subscription;
            }

            public static void UnregisterReload(Component panelRenderer, object token)
            {
                PanelRenderer renderer = panelRenderer as PanelRenderer;
                ReloadSubscription subscription = token as ReloadSubscription;
                if (renderer != null && subscription != null)
                {
                    renderer.UnregisterUIReloadCallback(subscription.Handle);
                }
            }

            private sealed class ReloadSubscription
            {
                private readonly System.Action<VisualElement> _onReload;

                public ReloadSubscription(System.Action<VisualElement> onReload)
                {
                    _onReload = onReload;
                }

                // WHY: Unregister needs a delegate equal to the registered one; a method group on the same
                // instance compares equal, a fresh lambda would not.
                public void Handle(PanelRenderer renderer, VisualElement root)
                {
                    _onReload(root);
                }
            }
        }
#else
        private static class Backend
        {
            public static bool HasPanelRenderer => false;

            public static Component FindPanelRenderer(GameObject gameObject)
            {
                return null;
            }

            public static Component AddPanelComponent(GameObject gameObject, PanelSettings panelSettings)
            {
                UIDocument document = gameObject.AddComponent<UIDocument>();
                document.panelSettings = panelSettings;
                return document;
            }

            public static object RegisterReload(Component panelRenderer, System.Action<VisualElement> onReload)
            {
                return null;
            }

            public static void UnregisterReload(Component panelRenderer, object token)
            {
            }
        }
#endif
    }
}
