using System.Collections.Generic;
using Neo.Rpg.Components;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Neo.Rpg.Demo
{
    /// <summary>
    ///     Screen-space health bars over world <see cref="RpgCharacter" />s. Targets come from the scene-assigned list,
    ///     from explicit <see cref="Track" /> calls (a controller feeding spawned enemies) and, as a fallback for
    ///     hand-made scenes, from a periodic scene search by tag. Both demo builders author the canvas, so the runtime
    ///     canvas is a fallback only. RpgCombatNpcDemo assigns its NPCs as scene targets with the search off; in
    ///     VampireSurvivorMCP the VampireSurvivorDemoController tracks every enemy it configures and turns the search
    ///     off. The per-target bar views are pooled at runtime because their targets are spawned at runtime; they are
    ///     restyled only when their target or the inspector style changes.
    /// </summary>
    [AddComponentMenu("Neoxider/Samples/RPG World Health Bars")]
    public sealed class RpgWorldHealthBars : MonoBehaviour
    {
        [Header("Targets")]
        [SerializeField] private string _targetTag = "Enemy";
        [SerializeField] private bool _includeInactiveTargets;
        [SerializeField] private bool _hideDeadTargets = true;
        [SerializeField] [Min(0.05f)] private float _scanInterval = 0.25f;
        [SerializeField] [Min(1)] private int _maxBars = 64;

        [Tooltip("Characters assigned in the scene. Each scan tracks them again while they are active, so a hidden " +
                 "or restored character gets its bar back (unless Untrack() excluded it). The tag filter does not " +
                 "apply to them.")]
        [SerializeField] private List<RpgCharacter> _sceneTargets = new();

        [Tooltip("Fallback for hand-made scenes: search the scene for RpgCharacters with the target tag on every " +
                 "scan. Turn it off when the targets come from the list above or from Track().")]
        [SerializeField] private bool _scanSceneForTargets = true;

        [Header("Canvas")]
        [SerializeField] private Canvas _canvas;
        [SerializeField] private RectTransform _barRoot;
        [SerializeField] private bool _createCanvasIfMissing = true;
        [SerializeField] private int _sortingOrder = 40;

        [Header("Camera")]
        [SerializeField] private Camera _worldCamera;
        [SerializeField] private Vector3 _worldOffset = new(0f, 2.25f, 0f);
        [SerializeField] private bool _hideOffscreen = true;

        [Header("View")]
        [SerializeField] private bool _drawNames = true;
        [SerializeField] private Vector2 _barSize = new(72f, 8f);
        [SerializeField] private Color _backgroundColor = new(0.05f, 0.05f, 0.05f, 0.9f);
        [SerializeField] private Color _healthColor = new(0.85f, 0.16f, 0.12f, 1f);
        [SerializeField] private Color _borderColor = new(0f, 0f, 0f, 0.95f);
        [SerializeField] private Color _nameColor = Color.white;

        private readonly List<RpgCharacter> _targets = new();
        private readonly HashSet<RpgCharacter> _targetSet = new();
        private readonly HashSet<RpgCharacter> _untracked = new();
        private readonly Dictionary<RpgCharacter, BarView> _activeViews = new();
        private readonly List<BarView> _viewPool = new();
        private readonly HashSet<RpgCharacter> _seen = new();
        private readonly List<RpgCharacter> _removeBuffer = new();

        private float _nextScanTime;
        private int _visibleBarCount;
        private int _styleVersion;
        private bool _warnedMissingCanvas;
        private GameObject _createdCanvas;
        private GameObject _createdBarRoot;

        public int TrackedTargetCount => _targets.Count;
        public int VisibleBarCount => _visibleBarCount;
        public Canvas Canvas => _canvas;

        /// <summary>Tag the scene search matches; empty matches every character.</summary>
        public string TargetTag => _targetTag;

        /// <summary>Whether each scan also searches the scene by tag (the fallback for hand-made scenes).</summary>
        public bool ScanSceneForTargets
        {
            get => _scanSceneForTargets;
            set => _scanSceneForTargets = value;
        }

        private void Awake()
        {
            ResolveCamera();
            EnsureCanvas();
            RefreshTargets();
        }

        private void Update()
        {
            if (_canvas == null || _barRoot == null)
            {
                EnsureCanvas();
            }

            if (Time.time >= _nextScanTime)
            {
                if (_worldCamera == null)
                {
                    ResolveCamera();
                }

                RefreshTargets();
                _nextScanTime = Time.time + _scanInterval;
            }
        }

        private void LateUpdate()
        {
            UpdateViews();
        }

        private void OnValidate()
        {
            _styleVersion++;
        }

        private void OnDisable()
        {
            for (int i = 0; i < _viewPool.Count; i++)
            {
                BarView view = _viewPool[i];
                if (view.Root != null) view.SetVisible(false);
            }

            _activeViews.Clear();
            _visibleBarCount = 0;
        }

        private void OnDestroy()
        {
            if (_createdCanvas != null)
            {
                Destroy(_createdCanvas);
            }
            else if (_createdBarRoot != null)
            {
                Destroy(_createdBarRoot);
            }
            else
            {
                for (int i = 0; i < _viewPool.Count; i++)
                {
                    BarView view = _viewPool[i];
                    if (view.Root != null) Destroy(view.Root.gameObject);
                }
            }

            _viewPool.Clear();
        }

        /// <summary>
        ///     Starts drawing a bar over <paramref name="target" /> and lifts an earlier <see cref="Untrack" />. Ignored
        ///     for null and already tracked targets and at the bar limit; the tag filter does not apply. A target that
        ///     becomes inactive is dropped on the next scan (unless inactive targets are included), so track a pooled
        ///     character again when it respawns.
        /// </summary>
        /// <returns>True when the target was added.</returns>
        public bool Track(RpgCharacter target)
        {
            if (target == null)
            {
                return false;
            }

            _untracked.Remove(target);
            return TryAddTarget(target);
        }

        /// <summary>
        ///     Stops drawing the bar of <paramref name="target" /> and keeps the scene-assigned list and the scene
        ///     search from tracking it again; only a later <see cref="Track" /> call brings the bar back.
        /// </summary>
        /// <returns>True when the target was tracked.</returns>
        public bool Untrack(RpgCharacter target)
        {
            if (target == null)
            {
                return false;
            }

            _untracked.Add(target);
            bool removed = _targetSet.Remove(target);
            if (removed)
            {
                _targets.Remove(target);
            }

            DeactivateView(target);
            return removed;
        }

        /// <summary>True while <paramref name="target" /> is in the tracked list (drawn or not).</summary>
        public bool IsTracked(RpgCharacter target)
        {
            return target != null && _targetSet.Contains(target);
        }

        /// <summary>
        ///     Drops destroyed (and, unless included, inactive) targets, then tracks the active scene-assigned targets
        ///     and, when <see cref="ScanSceneForTargets" /> is on, the tagged characters found in the scene.
        /// </summary>
        [Button]
        public void RefreshTargets()
        {
            for (int i = _targets.Count - 1; i >= 0; i--)
            {
                RpgCharacter target = _targets[i];
                if (target == null || !_includeInactiveTargets && !target.gameObject.activeInHierarchy)
                {
                    _targets.RemoveAt(i);
                    _targetSet.Remove(target);
                }
            }

            if (_untracked.Count > 0)
            {
                _untracked.RemoveWhere(entry => entry == null);
            }

            if (_sceneTargets != null)
            {
                for (int i = 0; i < _sceneTargets.Count && _targets.Count < _maxBars; i++)
                {
                    RpgCharacter target = _sceneTargets[i];
                    if (target != null && (_includeInactiveTargets || target.gameObject.activeInHierarchy))
                    {
                        TryAddTarget(target);
                    }
                }
            }

            if (_scanSceneForTargets && _targets.Count < _maxBars)
            {
                ScanScene();
            }
        }

        /// <summary>Adds a target unless it is untracked, already tracked or over the bar limit.</summary>
        private bool TryAddTarget(RpgCharacter target)
        {
            if (_targets.Count >= _maxBars || _untracked.Contains(target) || !_targetSet.Add(target))
            {
                return false;
            }

            _targets.Add(target);
            return true;
        }

        private void ScanScene()
        {
            FindObjectsInactive inactiveMode =
                _includeInactiveTargets ? FindObjectsInactive.Include : FindObjectsInactive.Exclude;
            RpgCharacter[] characters = FindObjectsByType<RpgCharacter>(inactiveMode, FindObjectsSortMode.None);

            for (int i = 0; i < characters.Length && _targets.Count < _maxBars; i++)
            {
                RpgCharacter character = characters[i];
                if (character != null && MatchesTarget(character))
                {
                    TryAddTarget(character);
                }
            }
        }

        private void UpdateViews()
        {
            if (_worldCamera == null || _canvas == null || _barRoot == null)
            {
                return;
            }

            _visibleBarCount = 0;
            _seen.Clear();

            for (int i = 0; i < _targets.Count; i++)
            {
                RpgCharacter target = _targets[i];
                if (!ShouldDraw(target) || !TryGetCanvasPosition(target, out Vector2 anchoredPosition))
                {
                    DeactivateView(target);
                    continue;
                }

                BarView view = GetOrCreateView(target);
                view.Root.anchoredPosition = anchoredPosition;
                if (view.Target != target || view.StyleVersion != _styleVersion)
                {
                    // WHY: target.name allocates, so the label and style are rebuilt only when the view changes hands.
                    view.Apply(target, _drawNames ? CleanName(target.name) : string.Empty, _barSize, _drawNames,
                        _backgroundColor, _healthColor, _borderColor, _nameColor, _styleVersion);
                }

                view.SetVisible(true);
                view.SetPercent(target.HpPercentValue);

                _seen.Add(target);
                _visibleBarCount++;
            }

            RemoveInactiveViews();
        }

        private bool TryGetCanvasPosition(RpgCharacter target, out Vector2 anchoredPosition)
        {
            anchoredPosition = default;
            Vector3 screen = _worldCamera.WorldToScreenPoint(target.transform.position + _worldOffset);
            if (screen.z <= 0f)
            {
                return false;
            }

            if (_hideOffscreen &&
                (screen.x < 0f || screen.x > Screen.width || screen.y < 0f || screen.y > Screen.height))
            {
                return false;
            }

            Camera canvasCamera = _canvas.renderMode == RenderMode.ScreenSpaceOverlay ? null : _canvas.worldCamera;
            return RectTransformUtility.ScreenPointToLocalPointInRectangle(
                _barRoot,
                new Vector2(screen.x, screen.y),
                canvasCamera,
                out anchoredPosition);
        }

        private BarView GetOrCreateView(RpgCharacter target)
        {
            if (_activeViews.TryGetValue(target, out BarView view) && view != null)
            {
                return view;
            }

            view = GetPooledView();
            _activeViews[target] = view;
            return view;
        }

        private BarView GetPooledView()
        {
            for (int i = 0; i < _viewPool.Count; i++)
            {
                if (!_viewPool[i].Root.gameObject.activeSelf)
                {
                    return _viewPool[i];
                }
            }

            BarView created = BarView.Create(_barRoot, "EnemyHealthBar_" + _viewPool.Count);
            _viewPool.Add(created);
            return created;
        }

        private void DeactivateView(RpgCharacter target)
        {
            if (target == null)
            {
                return;
            }

            if (_activeViews.TryGetValue(target, out BarView view) && view != null)
            {
                view.SetVisible(false);
            }

            _activeViews.Remove(target);
        }

        private void RemoveInactiveViews()
        {
            _removeBuffer.Clear();
            foreach (KeyValuePair<RpgCharacter, BarView> pair in _activeViews)
            {
                if (pair.Key == null || !_seen.Contains(pair.Key))
                {
                    pair.Value?.SetVisible(false);
                    _removeBuffer.Add(pair.Key);
                }
            }

            for (int i = 0; i < _removeBuffer.Count; i++)
            {
                _activeViews.Remove(_removeBuffer[i]);
            }
        }

        private bool ShouldDraw(RpgCharacter target)
        {
            if (target == null || !target.gameObject.activeInHierarchy)
            {
                return false;
            }

            return !_hideDeadTargets || !target.IsDead;
        }

        private bool MatchesTarget(RpgCharacter character)
        {
            return string.IsNullOrWhiteSpace(_targetTag) || character.CompareTag(_targetTag);
        }

        private void ResolveCamera()
        {
            if (_worldCamera != null)
            {
                return;
            }

            _worldCamera = Camera.main != null ? Camera.main : FindFirstObjectByType<Camera>();
        }

        /// <summary>Runtime fallback for hand-made scenes; the demo builders author the canvas and the bar root.</summary>
        private void EnsureCanvas()
        {
            if (_canvas == null)
            {
                if (!_createCanvasIfMissing)
                {
                    WarnMissingCanvas();
                    return;
                }

                GameObject canvasObject = new("RPG Enemy Health Bars Canvas");
                _createdCanvas = canvasObject;
                canvasObject.transform.SetParent(transform, false);
                _canvas = canvasObject.AddComponent<Canvas>();
                _canvas.renderMode = RenderMode.ScreenSpaceOverlay;
                _canvas.sortingOrder = _sortingOrder;
                CanvasScaler scaler = canvasObject.AddComponent<CanvasScaler>();
                scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
                scaler.referenceResolution = new Vector2(1920f, 1080f);
                scaler.matchWidthOrHeight = 0.5f;
            }

            _canvas.sortingOrder = _sortingOrder;

            if (_barRoot == null)
            {
                GameObject rootObject = new("Bars");
                _createdBarRoot = rootObject;
                rootObject.transform.SetParent(_canvas.transform, false);
                _barRoot = rootObject.AddComponent<RectTransform>();
                _barRoot.anchorMin = Vector2.zero;
                _barRoot.anchorMax = Vector2.one;
                _barRoot.offsetMin = Vector2.zero;
                _barRoot.offsetMax = Vector2.zero;
            }
        }

        private void WarnMissingCanvas()
        {
            if (_warnedMissingCanvas)
            {
                return;
            }

            _warnedMissingCanvas = true;
            global::Neo.Demo.SampleDiagnostics.Warn(
                "[RpgWorldHealthBars] No canvas is assigned and Create Canvas If Missing is off, so no bars are " +
                "drawn. Re-import the scene from the Demo download or assign a canvas.", this);
        }

        private static string CleanName(string objectName)
        {
            return string.IsNullOrEmpty(objectName) ? "Enemy" : objectName.Replace("(Clone)", string.Empty).Trim();
        }

        private sealed class BarView
        {
            private readonly Image _background;
            private readonly Image _border;
            private readonly Image _fill;
            private readonly TMP_Text _label;
            private readonly RectTransform _fillTransform;
            private float _percent = -1f;

            private BarView(RectTransform root, Image border, Image background, Image fill, TMP_Text label)
            {
                Root = root;
                _border = border;
                _background = background;
                _fill = fill;
                _label = label;
                _fillTransform = fill.rectTransform;
            }

            public RectTransform Root { get; }

            /// <summary>Target the label and the style were last applied for.</summary>
            public RpgCharacter Target { get; private set; }

            /// <summary>Owner style version of the last apply; -1 until the first one.</summary>
            public int StyleVersion { get; private set; } = -1;

            public static BarView Create(RectTransform parent, string name)
            {
                GameObject rootObject = new(name);
                rootObject.transform.SetParent(parent, false);
                RectTransform root = rootObject.AddComponent<RectTransform>();
                root.anchorMin = new Vector2(0.5f, 0.5f);
                root.anchorMax = new Vector2(0.5f, 0.5f);
                root.pivot = new Vector2(0.5f, 0f);

                Image border = CreateImage("Border", root, new Color(0f, 0f, 0f, 0.95f));
                Image background = CreateImage("Background", border.rectTransform, new Color(0.05f, 0.05f, 0.05f, 0.9f));
                Image fill = CreateImage("Fill", background.rectTransform, Color.red);

                TMP_Text label = CreateLabel(root);
                BarView view = new(root, border, background, fill, label);
                view.SetVisible(false);
                return view;
            }

            public void Apply(RpgCharacter target, string label, Vector2 barSize, bool drawName, Color background,
                Color health, Color border, Color name, int styleVersion)
            {
                Target = target;
                StyleVersion = styleVersion;
                SetSize(barSize, drawName);
                SetColors(background, health, border, name);
                _label.text = label;
            }

            public void SetVisible(bool visible)
            {
                GameObject rootObject = Root.gameObject;
                if (rootObject.activeSelf != visible)
                {
                    rootObject.SetActive(visible);
                }
            }

            public void SetPercent(float value01)
            {
                float percent = Mathf.Clamp01(value01);
                if (percent == _percent)
                {
                    return;
                }

                _percent = percent;
                Vector2 anchorMax = _fillTransform.anchorMax;
                anchorMax.x = percent;
                _fillTransform.anchorMax = anchorMax;
            }

            private void SetSize(Vector2 barSize, bool drawName)
            {
                float width = Mathf.Max(1f, barSize.x);
                float height = Mathf.Max(1f, barSize.y);
                float labelHeight = drawName ? 14f : 0f;
                Root.sizeDelta = new Vector2(width + 2f, height + labelHeight + 2f);

                _label.gameObject.SetActive(drawName);
                RectTransform labelRect = _label.rectTransform;
                labelRect.anchorMin = new Vector2(0.5f, 1f);
                labelRect.anchorMax = new Vector2(0.5f, 1f);
                labelRect.pivot = new Vector2(0.5f, 1f);
                labelRect.anchoredPosition = Vector2.zero;
                labelRect.sizeDelta = new Vector2(width + 72f, labelHeight);

                RectTransform borderRect = _border.rectTransform;
                borderRect.anchorMin = new Vector2(0.5f, 0f);
                borderRect.anchorMax = new Vector2(0.5f, 0f);
                borderRect.pivot = new Vector2(0.5f, 0f);
                borderRect.anchoredPosition = Vector2.zero;
                borderRect.sizeDelta = new Vector2(width + 2f, height + 2f);

                RectTransform backgroundRect = _background.rectTransform;
                backgroundRect.anchorMin = new Vector2(0.5f, 0.5f);
                backgroundRect.anchorMax = new Vector2(0.5f, 0.5f);
                backgroundRect.pivot = new Vector2(0.5f, 0.5f);
                backgroundRect.anchoredPosition = Vector2.zero;
                backgroundRect.sizeDelta = new Vector2(width, height);

                // WHY: keep the cached fill so SetPercent may skip an unchanged value after a restyle.
                _fillTransform.anchorMin = Vector2.zero;
                _fillTransform.anchorMax = new Vector2(_percent >= 0f ? _percent : 1f, 1f);
                _fillTransform.offsetMin = Vector2.zero;
                _fillTransform.offsetMax = Vector2.zero;
            }

            private void SetColors(Color background, Color health, Color border, Color name)
            {
                _background.color = background;
                _fill.color = health;
                _border.color = border;
                _label.color = name;
            }

            private static Image CreateImage(string name, Transform parent, Color color)
            {
                GameObject obj = new(name);
                obj.transform.SetParent(parent, false);
                Image image = obj.AddComponent<Image>();
                image.color = color;
                image.raycastTarget = false;
                return image;
            }

            private static TMP_Text CreateLabel(Transform parent)
            {
                GameObject obj = new("Name");
                obj.transform.SetParent(parent, false);
                TextMeshProUGUI label = obj.AddComponent<TextMeshProUGUI>();
                label.alignment = TextAlignmentOptions.Center;
                label.fontSize = 11f;
                label.fontStyle = FontStyles.Bold;
                label.raycastTarget = false;
                label.textWrappingMode = TextWrappingModes.NoWrap;
                label.overflowMode = TextOverflowModes.Ellipsis;
                return label;
            }
        }
    }
}
