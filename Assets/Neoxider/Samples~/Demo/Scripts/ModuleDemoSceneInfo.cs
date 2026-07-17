using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace Neo.Samples
{
    /// <summary>
    ///     Runtime info card for demo scenes. Builds a compact uGUI + TextMeshPro overlay
    ///     (module title, description and hint lines with a collapse toggle) entirely from
    ///     code — no prefabs, no IMGUI. The overlay is created when the component is enabled
    ///     and destroyed when it is disabled.
    /// </summary>
    [AddComponentMenu("Neoxider/Demo/Module Demo Scene Info")]
    public sealed class ModuleDemoSceneInfo : MonoBehaviour
    {
        public enum Corner
        {
            TopLeft = 0,
            TopRight = 1,
            BottomLeft = 2,
            BottomRight = 3
        }

        private const int OverlaySortingOrder = 30000;
        private const float ScreenMargin = 24f;

        private static Sprite _roundedSprite;

        [SerializeField] private string _moduleName = "Module";
        [SerializeField] private string _purpose = "Smoke demo scene.";
        [SerializeField] private string _runtimeApi = "Use the module runtime API from C#.";
        [SerializeField] private string _sceneWorkflow = "Use scene components as optional authoring wrappers.";
        [SerializeField] private string _verification = "Open scene and enter Play Mode.";
        [SerializeField] private bool _showRuntimeOverlay = true;

        [Header("Overlay Look")]
        [SerializeField] private Corner _corner = Corner.TopLeft;
        [SerializeField] private Color _accentColor = new Color32(124, 92, 255, 255);
        [SerializeField] private Color _backgroundColor = new Color32(30, 30, 40, 217);
        [SerializeField] [Min(240f)] private float _panelWidth = 460f;
        [SerializeField] private bool _startCollapsed;

        [Tooltip("Extra hint lines (what to press / what happens) shown under the description.")]
        [SerializeField] private string[] _hints = Array.Empty<string>();

        private TMP_Text _collapseLabel;
        private bool _collapsed;
        private GameObject _divider;
        private GameObject _hintsBody;
        private GameObject _overlayRoot;

        public string ModuleName => _moduleName;
        public string Purpose => _purpose;
        public string RuntimeApi => _runtimeApi;
        public string SceneWorkflow => _sceneWorkflow;
        public string Verification => _verification;
        public IReadOnlyList<string> Hints => _hints;

        private void OnEnable()
        {
            BuildOverlay();
        }

        private void OnDisable()
        {
            DestroyOverlay();
        }

        /// <summary>Creates a standalone info card at runtime — for demos assembled from code.</summary>
        public static ModuleDemoSceneInfo Show(string title, string description, params string[] hints)
        {
            var host = new GameObject("ModuleDemoSceneInfo");
            ModuleDemoSceneInfo info = host.AddComponent<ModuleDemoSceneInfo>();
            info._moduleName = title;
            info._purpose = description;
            info._runtimeApi = string.Empty;
            info._sceneWorkflow = string.Empty;
            info._verification = string.Empty;
            info._hints = hints ?? Array.Empty<string>();
            info._showRuntimeOverlay = true;
            info.Rebuild();
            return info;
        }

        public void Configure(
            string moduleName,
            string purpose,
            string runtimeApi,
            string sceneWorkflow,
            string verification,
            bool showRuntimeOverlay = true)
        {
            _moduleName = moduleName;
            _purpose = purpose;
            _runtimeApi = runtimeApi;
            _sceneWorkflow = sceneWorkflow;
            _verification = verification;
            _showRuntimeOverlay = showRuntimeOverlay;
            Rebuild();
        }

        /// <summary>Destroys and rebuilds the overlay from the current field values.</summary>
        public void Rebuild()
        {
            DestroyOverlay();
            if (isActiveAndEnabled)
            {
                BuildOverlay();
            }
        }

        public void SetCollapsed(bool collapsed)
        {
            _collapsed = collapsed;
            ApplyCollapsedState();
        }

        private void ToggleCollapsed()
        {
            SetCollapsed(!_collapsed);
        }

        #region Overlay construction

        private void BuildOverlay()
        {
            if (_overlayRoot != null || !_showRuntimeOverlay || !Application.isPlaying)
            {
                return;
            }

            _collapsed = _startCollapsed;

            _overlayRoot = new GameObject("[ModuleDemoInfo] " + _moduleName);
            Canvas canvas = _overlayRoot.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = OverlaySortingOrder;

            CanvasScaler scaler = _overlayRoot.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920f, 1080f);
            scaler.matchWidthOrHeight = 0.5f;

            _overlayRoot.AddComponent<GraphicRaycaster>();
            EnsureEventSystem();

            RectTransform panel = CreatePanel(_overlayRoot.transform);
            CreateHeader(panel);
            _divider = CreateDivider(panel);
            _hintsBody = CreateBody(panel);

            ApplyCollapsedState();
        }

        private void DestroyOverlay()
        {
            if (_overlayRoot == null)
            {
                return;
            }

            Destroy(_overlayRoot);
            _overlayRoot = null;
            _hintsBody = null;
            _divider = null;
            _collapseLabel = null;
        }

        private RectTransform CreatePanel(Transform parent)
        {
            RectTransform rect = CreateRect("Panel", parent);
            Vector2 anchor = GetCornerAnchor(out Vector2 offset);
            rect.anchorMin = anchor;
            rect.anchorMax = anchor;
            rect.pivot = anchor;
            rect.anchoredPosition = offset;
            rect.sizeDelta = new Vector2(_panelWidth, 0f);

            Image background = rect.gameObject.AddComponent<Image>();
            background.sprite = GetRoundedSprite();
            background.type = Image.Type.Sliced;
            background.color = _backgroundColor;

            Shadow shadow = rect.gameObject.AddComponent<Shadow>();
            shadow.effectColor = new Color(0f, 0f, 0f, 0.35f);
            shadow.effectDistance = new Vector2(0f, -3f);

            VerticalLayoutGroup layout = rect.gameObject.AddComponent<VerticalLayoutGroup>();
            layout.padding = new RectOffset(18, 18, 14, 16);
            layout.spacing = 10f;
            layout.childControlWidth = true;
            layout.childControlHeight = true;
            layout.childForceExpandWidth = true;
            layout.childForceExpandHeight = false;

            ContentSizeFitter fitter = rect.gameObject.AddComponent<ContentSizeFitter>();
            fitter.horizontalFit = ContentSizeFitter.FitMode.Unconstrained;
            fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
            return rect;
        }

        private void CreateHeader(RectTransform panel)
        {
            RectTransform header = CreateRect("Header", panel);
            HorizontalLayoutGroup row = header.gameObject.AddComponent<HorizontalLayoutGroup>();
            row.spacing = 10f;
            row.childControlWidth = true;
            row.childControlHeight = true;
            row.childForceExpandWidth = false;
            row.childForceExpandHeight = false;
            row.childAlignment = TextAnchor.MiddleLeft;

            RectTransform titles = CreateRect("Titles", header);
            VerticalLayoutGroup titleColumn = titles.gameObject.AddComponent<VerticalLayoutGroup>();
            titleColumn.spacing = 1f;
            titleColumn.childControlWidth = true;
            titleColumn.childControlHeight = true;
            titleColumn.childForceExpandWidth = true;
            titleColumn.childForceExpandHeight = false;
            titles.gameObject.AddComponent<LayoutElement>().flexibleWidth = 1f;

            TextMeshProUGUI overline = CreateText(
                titles, "Overline", "NEOXIDER · DEMO SCENE", 10.5f, _accentColor, FontStyles.Bold);
            overline.characterSpacing = 8f;

            CreateText(titles, "Title", _moduleName, 21f, new Color32(242, 242, 247, 255), FontStyles.Bold);

            CreateCollapseButton(header);
        }

        private void CreateCollapseButton(RectTransform parent)
        {
            RectTransform rect = CreateRect("Collapse", parent);
            LayoutElement element = rect.gameObject.AddComponent<LayoutElement>();
            element.preferredWidth = 30f;
            element.preferredHeight = 26f;
            element.flexibleWidth = 0f;

            Image image = rect.gameObject.AddComponent<Image>();
            image.sprite = GetRoundedSprite();
            image.type = Image.Type.Sliced;
            image.color = new Color(1f, 1f, 1f, 0.07f);

            Button button = rect.gameObject.AddComponent<Button>();
            button.targetGraphic = image;
            button.onClick.AddListener(ToggleCollapsed);

            _collapseLabel = CreateText(rect, "Label", "-", 17f, new Color32(201, 203, 216, 255), FontStyles.Bold);
            _collapseLabel.alignment = TextAlignmentOptions.Center;
            RectTransform labelRect = _collapseLabel.rectTransform;
            labelRect.anchorMin = Vector2.zero;
            labelRect.anchorMax = Vector2.one;
            labelRect.offsetMin = Vector2.zero;
            labelRect.offsetMax = Vector2.zero;
        }

        private GameObject CreateDivider(RectTransform panel)
        {
            RectTransform rect = CreateRect("Divider", panel);
            Image line = rect.gameObject.AddComponent<Image>();
            line.color = new Color(_accentColor.r, _accentColor.g, _accentColor.b, 0.9f);
            line.raycastTarget = false;

            LayoutElement element = rect.gameObject.AddComponent<LayoutElement>();
            element.minHeight = 2f;
            element.preferredHeight = 2f;
            element.flexibleWidth = 1f;
            return rect.gameObject;
        }

        private GameObject CreateBody(RectTransform panel)
        {
            RectTransform body = CreateRect("Body", panel);
            VerticalLayoutGroup layout = body.gameObject.AddComponent<VerticalLayoutGroup>();
            layout.spacing = 7f;
            layout.childControlWidth = true;
            layout.childControlHeight = true;
            layout.childForceExpandWidth = true;
            layout.childForceExpandHeight = false;

            if (!string.IsNullOrWhiteSpace(_purpose))
            {
                CreateText(body, "Description", _purpose, 15f, new Color32(230, 231, 240, 255));
            }

            foreach (string line in ComposeHintLines())
            {
                CreateText(body, "Hint", line, 13f, new Color32(186, 189, 204, 255));
            }

            return body.gameObject;
        }

        private void ApplyCollapsedState()
        {
            if (_hintsBody != null)
            {
                _hintsBody.SetActive(!_collapsed);
            }

            if (_divider != null)
            {
                _divider.SetActive(!_collapsed);
            }

            if (_collapseLabel != null)
            {
                _collapseLabel.text = _collapsed ? "+" : "-";
            }
        }

        private List<string> ComposeHintLines()
        {
            var lines = new List<string>();
            string accent = ColorUtility.ToHtmlStringRGB(_accentColor);
            AppendLabeledHint(lines, accent, "Runtime API", _runtimeApi);
            AppendLabeledHint(lines, accent, "Scene workflow", _sceneWorkflow);
            AppendLabeledHint(lines, accent, "Verification", _verification);

            if (_hints == null)
            {
                return lines;
            }

            foreach (string hint in _hints)
            {
                if (!string.IsNullOrWhiteSpace(hint))
                {
                    lines.Add("<color=#" + accent + ">»</color>  " + hint);
                }
            }

            return lines;
        }

        private static void AppendLabeledHint(List<string> lines, string accent, string label, string text)
        {
            if (string.IsNullOrWhiteSpace(text))
            {
                return;
            }

            lines.Add("<color=#" + accent + ">»</color>  <b>" + label + ":</b> " + text);
        }

        private Vector2 GetCornerAnchor(out Vector2 offset)
        {
            switch (_corner)
            {
                case Corner.TopRight:
                    offset = new Vector2(-ScreenMargin, -ScreenMargin);
                    return new Vector2(1f, 1f);
                case Corner.BottomLeft:
                    offset = new Vector2(ScreenMargin, ScreenMargin);
                    return new Vector2(0f, 0f);
                case Corner.BottomRight:
                    offset = new Vector2(-ScreenMargin, ScreenMargin);
                    return new Vector2(1f, 0f);
                default:
                    offset = new Vector2(ScreenMargin, -ScreenMargin);
                    return new Vector2(0f, 1f);
            }
        }

        private static RectTransform CreateRect(string name, Transform parent)
        {
            var go = new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(parent, false);
            return (RectTransform)go.transform;
        }

        private static TextMeshProUGUI CreateText(
            Transform parent,
            string name,
            string text,
            float fontSize,
            Color color,
            FontStyles style = FontStyles.Normal)
        {
            RectTransform rect = CreateRect(name, parent);
            TextMeshProUGUI tmp = rect.gameObject.AddComponent<TextMeshProUGUI>();
            tmp.text = text;
            tmp.fontSize = fontSize;
            tmp.color = color;
            tmp.fontStyle = style;
            tmp.textWrappingMode = TextWrappingModes.Normal;
            tmp.richText = true;
            tmp.raycastTarget = false;
            return tmp;
        }

        private static void EnsureEventSystem()
        {
            if (FindFirstObjectByType<EventSystem>() != null)
            {
                return;
            }

            var eventSystem = new GameObject("EventSystem");
            eventSystem.AddComponent<EventSystem>();
#if ENABLE_INPUT_SYSTEM && !ENABLE_LEGACY_INPUT_MANAGER
            Type moduleType = Type.GetType("UnityEngine.InputSystem.UI.InputSystemUIInputModule, Unity.InputSystem");
            if (moduleType != null)
            {
                eventSystem.AddComponent(moduleType);
            }
#else
            eventSystem.AddComponent<StandaloneInputModule>();
#endif
        }

        private static Sprite GetRoundedSprite()
        {
            if (_roundedSprite != null)
            {
                return _roundedSprite;
            }

            const int size = 64;
            const float radius = 14f;
            var texture = new Texture2D(size, size, TextureFormat.RGBA32, false)
            {
                name = "ModuleDemoInfoRounded",
                hideFlags = HideFlags.HideAndDontSave,
                filterMode = FilterMode.Bilinear,
                wrapMode = TextureWrapMode.Clamp
            };

            var pixels = new Color32[size * size];
            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    float alpha = RoundedRectAlpha(x, y, size, radius);
                    pixels[y * size + x] = new Color32(255, 255, 255, (byte)Mathf.RoundToInt(alpha * 255f));
                }
            }

            texture.SetPixels32(pixels);
            texture.Apply(false, false);

            const float border = radius + 4f;
            _roundedSprite = Sprite.Create(
                texture,
                new Rect(0f, 0f, size, size),
                new Vector2(0.5f, 0.5f),
                100f,
                0,
                SpriteMeshType.FullRect,
                new Vector4(border, border, border, border));
            _roundedSprite.name = "ModuleDemoInfoRounded";
            _roundedSprite.hideFlags = HideFlags.HideAndDontSave;
            return _roundedSprite;
        }

        private static float RoundedRectAlpha(int x, int y, int size, float radius)
        {
            float half = size * 0.5f;
            float px = Mathf.Abs(x + 0.5f - half) - (half - radius);
            float py = Mathf.Abs(y + 0.5f - half) - (half - radius);
            float dx = Mathf.Max(px, 0f);
            float dy = Mathf.Max(py, 0f);
            float distance = Mathf.Sqrt(dx * dx + dy * dy) - radius;
            return Mathf.Clamp01(0.5f - distance);
        }

        #endregion
    }
}
