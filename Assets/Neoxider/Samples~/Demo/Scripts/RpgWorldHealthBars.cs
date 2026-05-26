using System.Collections.Generic;
using Neo.Rpg.Components;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Neo.Rpg.Demo
{
    [AddComponentMenu("Neoxider/Samples/RPG World Health Bars")]
    public sealed class RpgWorldHealthBars : MonoBehaviour
    {
        [Header("Targets")]
        [SerializeField] private string _targetTag = "Enemy";
        [SerializeField] private bool _includeInactiveTargets;
        [SerializeField] private bool _hideDeadTargets = true;
        [SerializeField] [Min(0.05f)] private float _scanInterval = 0.25f;
        [SerializeField] [Min(1)] private int _maxBars = 64;

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
        private readonly Dictionary<RpgCharacter, BarView> _activeViews = new();
        private readonly List<BarView> _viewPool = new();

        private float _nextScanTime;
        private int _visibleBarCount;

        public int TrackedTargetCount => _targets.Count;
        public int VisibleBarCount => _visibleBarCount;
        public Canvas Canvas => _canvas;

        private void Awake()
        {
            ResolveCamera();
            EnsureCanvas();
            RefreshTargets();
        }

        private void Update()
        {
            if (_worldCamera == null)
            {
                ResolveCamera();
            }

            if (_canvas == null || _barRoot == null)
            {
                EnsureCanvas();
            }

            if (Time.time >= _nextScanTime)
            {
                RefreshTargets();
                _nextScanTime = Time.time + _scanInterval;
            }
        }

        private void LateUpdate()
        {
            UpdateViews();
        }

        [Button]
        public void RefreshTargets()
        {
            _targets.RemoveAll(target => target == null || !_includeInactiveTargets && !target.gameObject.activeInHierarchy);

            FindObjectsInactive inactiveMode =
                _includeInactiveTargets ? FindObjectsInactive.Include : FindObjectsInactive.Exclude;
            RpgCharacter[] characters = FindObjectsByType<RpgCharacter>(inactiveMode, FindObjectsSortMode.None);

            for (int i = 0; i < characters.Length && _targets.Count < _maxBars; i++)
            {
                RpgCharacter character = characters[i];
                if (character == null || !MatchesTarget(character) || _targets.Contains(character))
                {
                    continue;
                }

                _targets.Add(character);
            }
        }

        private void UpdateViews()
        {
            if (_worldCamera == null || _canvas == null || _barRoot == null)
            {
                return;
            }

            _visibleBarCount = 0;
            var seen = new HashSet<RpgCharacter>();

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
                view.SetVisible(true);
                view.SetSize(_barSize, _drawNames);
                view.SetColors(_backgroundColor, _healthColor, _borderColor, _nameColor);
                view.SetLabel(_drawNames ? CleanName(target.name) : string.Empty);
                view.SetPercent(target.HpPercentValue);

                seen.Add(target);
                _visibleBarCount++;
            }

            RemoveInactiveViews(seen);
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

            BarView created = BarView.Create(_barRoot, $"EnemyHealthBar_{_viewPool.Count}");
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

        private void RemoveInactiveViews(HashSet<RpgCharacter> seen)
        {
            _removeBuffer.Clear();
            foreach (KeyValuePair<RpgCharacter, BarView> pair in _activeViews)
            {
                if (pair.Key == null || !seen.Contains(pair.Key))
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

        private readonly List<RpgCharacter> _removeBuffer = new();

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
            _worldCamera = Camera.main != null ? Camera.main : FindFirstObjectByType<Camera>();
        }

        private void EnsureCanvas()
        {
            if (_canvas == null && !_createCanvasIfMissing)
            {
                return;
            }

            if (_canvas == null)
            {
                GameObject canvasObject = new("RPG Enemy Health Bars Canvas");
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
                rootObject.transform.SetParent(_canvas.transform, false);
                _barRoot = rootObject.AddComponent<RectTransform>();
                _barRoot.anchorMin = Vector2.zero;
                _barRoot.anchorMax = Vector2.one;
                _barRoot.offsetMin = Vector2.zero;
                _barRoot.offsetMax = Vector2.zero;
            }
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
                var view = new BarView(root, border, background, fill, label);
                view.SetVisible(false);
                return view;
            }

            public void SetVisible(bool visible)
            {
                Root.gameObject.SetActive(visible);
            }

            public void SetSize(Vector2 barSize, bool drawName)
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

                _fillTransform.anchorMin = Vector2.zero;
                _fillTransform.anchorMax = Vector2.one;
                _fillTransform.offsetMin = Vector2.zero;
                _fillTransform.offsetMax = Vector2.zero;
            }

            public void SetColors(Color background, Color health, Color border, Color name)
            {
                _background.color = background;
                _fill.color = health;
                _border.color = border;
                _label.color = name;
            }

            public void SetLabel(string label)
            {
                _label.text = label;
            }

            public void SetPercent(float value01)
            {
                Vector2 anchorMax = _fillTransform.anchorMax;
                anchorMax.x = Mathf.Clamp01(value01);
                _fillTransform.anchorMax = anchorMax;
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
