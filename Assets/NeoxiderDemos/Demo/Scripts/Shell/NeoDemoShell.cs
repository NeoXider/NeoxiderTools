using System;
using System.Collections.Generic;
using Neo.Samples.Survivor;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace Neo.Samples
{
    /// <summary>
    ///     Deprecated runtime builder for the shared "module demo" look. Demo scenes are now authored in the
    ///     editor (camera, canvas, header, content rows, log feed and
    ///     info card saved in the scene with persistent listeners). This class
    ///     stays only as a fallback for controllers that have not been converted yet: <see cref="Build(string, Color, Transform)" />
    ///     still assembles the same frame at runtime and returns a <see cref="Context" /> for rows and logging.
    /// </summary>
    public static class NeoDemoShell
    {
        internal const string DeprecationMessage =
            "Runtime-built demo frames are deprecated. Author the frame in the scene as uGUI objects.";

        // WHY: aliases DemoPalette so authored scenes and the runtime fallback read as one system.
        public static readonly Color Ink = DemoPalette.Ink;
        public static readonly Color Panel = DemoPalette.Panel;
        public static readonly Color Card = DemoPalette.Card;
        public static readonly Color Track = DemoPalette.Track;
        public static readonly Color Text = DemoPalette.Text;
        public static readonly Color Muted = DemoPalette.Muted;

        private const int SortingOrder = 25000;
        private const int InfoSortingOrder = 30000;
        private const float ContentWidth = 1000f;
        private const float InfoMargin = 24f;
        private const int LogLines = 5;

        /// <summary>
        ///     Shows the tutorial info card unless the scene already contains a
        ///     <see cref="ModuleDemoSceneInfo" /> (authored demo scenes ship one; empty scenes get one built here).
        /// </summary>
        [Obsolete(DeprecationMessage)]
        public static void ShowInfoCardOnce(string title, string description, params string[] hints)
        {
            if (UnityEngine.Object.FindFirstObjectByType<ModuleDemoSceneInfo>() == null)
            {
                ModuleDemoSceneInfo.Show(title, description, hints);
            }
        }

        /// <summary>
        ///     Builds the full demo frame (camera, canvas, header, content card, log feed) and returns a
        ///     <see cref="Context" /> for adding interactive rows and logging actions.
        /// </summary>
        /// <param name="title">Big header title (module name).</param>
        /// <param name="accent">Accent color for underline, buttons, fills and highlights.</param>
        [Obsolete(DeprecationMessage)]
        public static Context Build(string title, Color accent)
        {
            return Build(title, accent, null);
        }

        /// <summary>Builds an owned frame. Dispose the returned context when its controller is removed.</summary>
        [Obsolete(DeprecationMessage)]
        public static Context Build(string title, Color accent, Transform owner)
        {
            Context ctx = new() { Accent = accent };

            EnsureCamera(accent);
            EnsureEventSystem();

            GameObject canvasGo = new("[NeoDemoShell] " + title, typeof(RectTransform));
            ctx.OwnCanvas(canvasGo);
            try
            {
                if (owner != null) canvasGo.transform.SetParent(owner, false);
                Canvas canvas = canvasGo.AddComponent<Canvas>();
                canvas.renderMode = RenderMode.ScreenSpaceOverlay;
                canvas.sortingOrder = SortingOrder;
                CanvasScaler scaler = canvasGo.AddComponent<CanvasScaler>();
                scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
                scaler.referenceResolution = new Vector2(1920f, 1080f);
                scaler.matchWidthOrHeight = 0.5f;
                canvasGo.AddComponent<GraphicRaycaster>();
                ctx.Canvas = canvas;

                Image bg = SurvivorUI.Image("Backdrop", canvas.transform, Tint(accent), false);
                bg.sprite = ctx.CreateGradient(accent);
                bg.type = Image.Type.Simple;
                bg.raycastTarget = false;
                SurvivorUI.Stretch(bg.rectTransform);

                BuildHeader(canvas.transform, title, accent);
                ctx.Content = BuildContent(canvas.transform);
                ctx.BuildLog(canvas.transform);

                return ctx;
            }
            catch
            {
                ctx.Dispose();
                throw;
            }
        }

        private static void BuildHeader(Transform parent, string title, Color accent)
        {
            RectTransform header = SurvivorUI.Rect("Header", parent);
            SurvivorUI.Anchor(header, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f),
                new Vector2(0.5f, 1f), new Vector2(0f, -54f), new Vector2(ContentWidth + 40f, 96f));

            TMP_Text overline = SurvivorUI.Label("Overline", header, "NEOXIDER · MODULE DEMO", 15f,
                new Color(accent.r, accent.g, accent.b, 0.95f), TextAlignmentOptions.Center, FontStyles.Bold);
            overline.characterSpacing = 10f;
            SurvivorUI.Anchor(overline.rectTransform, new Vector2(0f, 1f), new Vector2(1f, 1f),
                new Vector2(0.5f, 1f), new Vector2(0f, -2f), new Vector2(0f, 22f));

            TMP_Text titleText = SurvivorUI.Label("Title", header, title, 40f, Text,
                TextAlignmentOptions.Center, FontStyles.Bold);
            SurvivorUI.Anchor(titleText.rectTransform, new Vector2(0f, 1f), new Vector2(1f, 1f),
                new Vector2(0.5f, 1f), new Vector2(0f, -26f), new Vector2(0f, 52f));
            // WHY: long module titles must shrink instead of wrapping up onto the overline.
            titleText.enableAutoSizing = true;
            titleText.fontSizeMax = 40f;
            titleText.fontSizeMin = 22f;
            titleText.textWrappingMode = TextWrappingModes.NoWrap;

            Image line = SurvivorUI.Image("Underline", header, accent);
            SurvivorUI.Anchor(line.rectTransform, new Vector2(0.5f, 0f), new Vector2(0.5f, 0f),
                new Vector2(0.5f, 0f), new Vector2(0f, 4f), new Vector2(200f, 3f));
            line.raycastTarget = false;
        }

        private static RectTransform BuildContent(Transform parent)
        {
            Image card = SurvivorUI.Image("Content", parent, Card);
            RectTransform rt = card.rectTransform;
            rt.anchorMin = new Vector2(0.5f, 0.5f);
            rt.anchorMax = new Vector2(0.5f, 0.5f);
            rt.pivot = new Vector2(0.5f, 0.5f);
            rt.anchoredPosition = new Vector2(0f, 8f);
            rt.sizeDelta = new Vector2(ContentWidth, 0f);

            Shadow shadow = card.gameObject.AddComponent<Shadow>();
            shadow.effectColor = new Color(0f, 0f, 0f, 0.4f);
            shadow.effectDistance = new Vector2(0f, -4f);

            VerticalLayoutGroup layout = card.gameObject.AddComponent<VerticalLayoutGroup>();
            layout.padding = new RectOffset(24, 24, 24, 24);
            layout.spacing = 8f;
            layout.childControlWidth = true;
            layout.childControlHeight = true;
            layout.childForceExpandWidth = true;
            layout.childForceExpandHeight = false;
            layout.childAlignment = TextAnchor.UpperCenter;

            ContentSizeFitter fitter = card.gameObject.AddComponent<ContentSizeFitter>();
            fitter.horizontalFit = ContentSizeFitter.FitMode.Unconstrained;
            fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
            return rt;
        }

        private static void EnsureCamera(Color accent)
        {
            if (Camera.main != null)
            {
                return;
            }

            GameObject go = new("Demo Camera");
            Camera cam = go.AddComponent<Camera>();
            cam.orthographic = true;
            cam.orthographicSize = 5.4f;
            cam.clearFlags = CameraClearFlags.SolidColor;
            // WHY: dark base tinted slightly toward the accent so the backdrop feels alive.
            cam.backgroundColor = new Color(0.05f + accent.r * 0.03f, 0.05f + accent.g * 0.03f,
                0.07f + accent.b * 0.04f, 1f);
            cam.transform.position = new Vector3(0f, 0f, -10f);
            go.tag = "MainCamera";
        }

        private static void EnsureEventSystem()
        {
            if (UnityEngine.Object.FindFirstObjectByType<EventSystem>() != null)
            {
                return;
            }

            GameObject go = new("EventSystem");
            go.AddComponent<EventSystem>();
#if ENABLE_INPUT_SYSTEM && !ENABLE_LEGACY_INPUT_MANAGER
            Type moduleType = Type.GetType(
                "UnityEngine.InputSystem.UI.InputSystemUIInputModule, Unity.InputSystem");
            if (moduleType != null)
            {
                go.AddComponent(moduleType);
            }
            else
            {
                go.AddComponent<StandaloneInputModule>();
            }
#else
            go.AddComponent<StandaloneInputModule>();
#endif
        }

        /// <summary>
        ///     Deprecated runtime fallback behind <see cref="ModuleDemoSceneInfo.Show" />: builds the info card
        ///     under the info host (so it is destroyed with it) and binds it to the component.
        /// </summary>
        internal static void BuildRuntimeInfoOverlay(ModuleDemoSceneInfo info)
        {
            if (info == null)
            {
                return;
            }

            EnsureEventSystem();
            GameObject canvasGo = new("[ModuleDemoInfo] " + info.ModuleName, typeof(RectTransform));
            canvasGo.transform.SetParent(info.transform, false);
            Canvas canvas = canvasGo.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = InfoSortingOrder;
            CanvasScaler scaler = canvasGo.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920f, 1080f);
            scaler.matchWidthOrHeight = 0.5f;
            canvasGo.AddComponent<GraphicRaycaster>();

            Image panel = SurvivorUI.Image("Panel", canvasGo.transform, info.BackgroundColor);
            RectTransform panelRect = panel.rectTransform;
            Vector2 anchor = CornerAnchor(info.OverlayCorner, out Vector2 offset);
            panelRect.anchorMin = anchor;
            panelRect.anchorMax = anchor;
            panelRect.pivot = anchor;
            panelRect.anchoredPosition = offset;
            panelRect.sizeDelta = new Vector2(info.PanelWidth, 0f);
            VerticalLayoutGroup layout = panel.gameObject.AddComponent<VerticalLayoutGroup>();
            layout.padding = new RectOffset(18, 18, 14, 16);
            layout.spacing = 10f;
            layout.childControlWidth = true;
            layout.childControlHeight = true;
            layout.childForceExpandWidth = true;
            layout.childForceExpandHeight = false;
            ContentSizeFitter fitter = panel.gameObject.AddComponent<ContentSizeFitter>();
            fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

            RectTransform header = SurvivorUI.Rect("Header", panelRect);
            HorizontalLayoutGroup row = header.gameObject.AddComponent<HorizontalLayoutGroup>();
            row.spacing = 10f;
            row.childControlWidth = true;
            row.childControlHeight = true;
            row.childForceExpandWidth = false;
            row.childForceExpandHeight = false;
            row.childAlignment = TextAnchor.MiddleLeft;
            TMP_Text title = SurvivorUI.Label("Title", header, info.ModuleName, 21f, Text,
                TextAlignmentOptions.Left, FontStyles.Bold);
            title.gameObject.AddComponent<LayoutElement>().flexibleWidth = 1f;
            Button collapse = SurvivorUI.Button("Collapse", header, new Color(1f, 1f, 1f, 0.07f));
            LayoutElement collapseSize = collapse.gameObject.AddComponent<LayoutElement>();
            collapseSize.preferredWidth = 30f;
            collapseSize.preferredHeight = 26f;
            TMP_Text collapseLabel = SurvivorUI.Label("Label", collapse.transform, "-", 17f, Muted,
                TextAlignmentOptions.Center, FontStyles.Bold);
            SurvivorUI.Stretch(collapseLabel.rectTransform);
            collapse.onClick.AddListener(info.ToggleCollapsed);

            Image divider = SurvivorUI.Image("Divider", panelRect, DemoPalette.WithAlpha(info.AccentColor, 0.9f), false);
            LayoutElement dividerSize = divider.gameObject.AddComponent<LayoutElement>();
            dividerSize.minHeight = 2f;
            dividerSize.preferredHeight = 2f;

            RectTransform body = SurvivorUI.Rect("Body", panelRect);
            VerticalLayoutGroup bodyLayout = body.gameObject.AddComponent<VerticalLayoutGroup>();
            bodyLayout.spacing = 7f;
            bodyLayout.childControlWidth = true;
            bodyLayout.childControlHeight = true;
            bodyLayout.childForceExpandWidth = true;
            bodyLayout.childForceExpandHeight = false;
            TMP_Text description = SurvivorUI.Label("Description", body, info.Purpose, 15f, Text);
            TMP_Text hints = SurvivorUI.Label("Hints", body, string.Empty, 13f, Muted);
            hints.paragraphSpacing = 50f;

            info.AssignOverlay(canvasGo, title, description, hints, collapseLabel, divider.gameObject, body.gameObject);
            info.ApplyContent();
        }

        private static Vector2 CornerAnchor(ModuleDemoSceneInfo.Corner corner, out Vector2 offset)
        {
            switch (corner)
            {
                case ModuleDemoSceneInfo.Corner.TopRight:
                    offset = new Vector2(-InfoMargin, -InfoMargin);
                    return new Vector2(1f, 1f);
                case ModuleDemoSceneInfo.Corner.BottomLeft:
                    offset = new Vector2(InfoMargin, InfoMargin);
                    return new Vector2(0f, 0f);
                case ModuleDemoSceneInfo.Corner.BottomRight:
                    offset = new Vector2(-InfoMargin, InfoMargin);
                    return new Vector2(1f, 0f);
                default:
                    offset = new Vector2(InfoMargin, -InfoMargin);
                    return new Vector2(0f, 1f);
            }
        }

        private static Color Tint(Color accent)
        {
            return new Color(accent.r, accent.g, accent.b, 1f);
        }

        internal static void DestroyOwnedObject(UnityEngine.Object value)
        {
            if (value == null) return;
            if (Application.isPlaying) UnityEngine.Object.Destroy(value);
            else UnityEngine.Object.DestroyImmediate(value);
        }

        /// <summary>
        ///     Live handle to a built demo frame: exposes the content card to add rows into and a bottom
        ///     log feed. All row helpers append to the centered card in an 8px vertical rhythm.
        /// </summary>
        public sealed class Context : IDisposable
        {
            private readonly List<string> _log = new();
            private TMP_Text _logText;
            private GameObject _ownedCanvas;
            private Sprite _gradient;
            private Texture2D _gradientTexture;
            public Canvas Canvas { get; internal set; }
            public RectTransform Content { get; internal set; }
            public Color Accent { get; internal set; }
            public bool IsDisposed { get; private set; }

            private bool CanInvoke => !IsDisposed && _ownedCanvas != null && _ownedCanvas.activeInHierarchy;

            internal void OwnCanvas(GameObject canvasObject)
            {
                _ownedCanvas = canvasObject;
            }

            internal Sprite CreateGradient(Color accent)
            {
                const int height = 256;
                _gradientTexture = new Texture2D(4, height, TextureFormat.RGBA32, false)
                {
                    name = "NeoDemoShell Gradient",
                    wrapMode = TextureWrapMode.Clamp,
                    filterMode = FilterMode.Bilinear,
                    hideFlags = HideFlags.HideAndDontSave
                };
                Color top = new(0.10f + accent.r * 0.16f, 0.10f + accent.g * 0.16f,
                    0.14f + accent.b * 0.18f, 1f);
                Color bottom = new(0.03f, 0.03f, 0.05f, 1f);
                for (int y = 0; y < height; y++)
                {
                    float t = y / (float)(height - 1);
                    Color color = Color.Lerp(bottom, top, t * t);
                    for (int x = 0; x < 4; x++) _gradientTexture.SetPixel(x, y, color);
                }

                _gradientTexture.Apply(false, false);
                _gradient = Sprite.Create(_gradientTexture, new Rect(0, 0, 4, height),
                    new Vector2(0.5f, 0.5f), 100f);
                _gradient.name = "NeoDemoShell Gradient";
                _gradient.hideFlags = HideFlags.HideAndDontSave;
                return _gradient;
            }

            /// <summary>Hides or restores the frame without rebuilding it or duplicating callbacks.</summary>
            public void SetVisible(bool visible)
            {
                if (!IsDisposed && _ownedCanvas != null) _ownedCanvas.SetActive(visible);
            }

            /// <summary>Releases only this frame and its generated gradient. Shared scene services survive.</summary>
            public void Dispose()
            {
                if (IsDisposed) return;
                IsDisposed = true;
                GameObject canvasObject = _ownedCanvas;
                Sprite gradient = _gradient;
                Texture2D texture = _gradientTexture;
                _ownedCanvas = null;
                _gradient = null;
                _gradientTexture = null;
                Canvas = null;
                Content = null;
                _logText = null;
                _log.Clear();
                try
                {
                    if (canvasObject != null)
                    {
                        canvasObject.SetActive(false);
                        foreach (Button button in canvasObject.GetComponentsInChildren<Button>(true))
                            button.onClick.RemoveAllListeners();
                        foreach (Slider slider in canvasObject.GetComponentsInChildren<Slider>(true))
                            slider.onValueChanged.RemoveAllListeners();
                        foreach (Toggle toggle in canvasObject.GetComponentsInChildren<Toggle>(true))
                            toggle.onValueChanged.RemoveAllListeners();
                    }
                }
                finally
                {
                    DestroyOwnedObject(canvasObject);
                    DestroyOwnedObject(gradient);
                    DestroyOwnedObject(texture);
                }
            }

            /// <summary>Pushes a line onto the bottom feed (newest first, keeps the last ~5).</summary>
            public void Log(string message)
            {
                if (IsDisposed || string.IsNullOrEmpty(message))
                {
                    return;
                }

                _log.Insert(0, message);
                if (_log.Count > LogLines)
                {
                    _log.RemoveRange(LogLines, _log.Count - LogLines);
                }

                if (_logText != null)
                {
                    System.Text.StringBuilder sb = new();
                    for (int i = 0; i < _log.Count; i++)
                    {
                        float a = Mathf.Lerp(1f, 0.35f, i / (float)LogLines);
                        string hex = ColorUtility.ToHtmlStringRGBA(new Color(Muted.r, Muted.g, Muted.b, a));
                        sb.Append("<color=#").Append(hex).Append('>');
                        sb.Append(i == 0 ? "› " : "  ").Append(_log[i]).Append("</color>");
                        if (i < _log.Count - 1)
                        {
                            sb.Append('\n');
                        }
                    }

                    _logText.text = sb.ToString();
                }
            }

            /// <summary>Adds a row of accent buttons that share the row width evenly.</summary>
            public Button[] AddButtonRow(params (string label, Action onClick)[] buttons)
            {
                RectTransform row = AddRow(52f);
                HorizontalLayoutGroup group = row.gameObject.AddComponent<HorizontalLayoutGroup>();
                group.spacing = 8f;
                group.childControlWidth = true;
                group.childControlHeight = true;
                group.childForceExpandWidth = true;
                group.childForceExpandHeight = true;

                Button[] made = new Button[buttons.Length];
                for (int i = 0; i < buttons.Length; i++)
                {
                    (string label, Action onClick) = buttons[i];
                    Button btn = SurvivorUI.Button("Btn_" + label, row, Accent);
                    TMP_Text txt = SurvivorUI.Label("Label", btn.transform, label, 24f, Ink,
                        TextAlignmentOptions.Center, FontStyles.Bold);
                    SurvivorUI.Stretch(txt.rectTransform, 4f);
                    Action click = onClick;
                    btn.onClick.AddListener(() =>
                    {
                        if (CanInvoke) click?.Invoke();
                    });
                    made[i] = btn;
                }

                return made;
            }

            /// <summary>Adds a labeled slider with a live value readout; fires <paramref name="onChanged" />.</summary>
            public Slider AddSlider(string label, float min, float max, float initial, Action<float> onChanged)
            {
                RectTransform row = AddRow(62f);

                TMP_Text caption = SurvivorUI.Label("Caption", row, label, 18f, Text,
                    TextAlignmentOptions.Left, FontStyles.Bold);
                SurvivorUI.Anchor(caption.rectTransform, new Vector2(0f, 1f), new Vector2(0.7f, 1f),
                    new Vector2(0f, 1f), new Vector2(0f, 0f), new Vector2(0f, 24f));

                TMP_Text value = SurvivorUI.Label("Value", row, initial.ToString("0.##"), 18f, Accent,
                    TextAlignmentOptions.Right, FontStyles.Bold);
                SurvivorUI.Anchor(value.rectTransform, new Vector2(0.7f, 1f), new Vector2(1f, 1f),
                    new Vector2(1f, 1f), new Vector2(0f, 0f), new Vector2(0f, 24f));

                Slider slider = BuildSlider(row, min, max, initial, Accent);
                SurvivorUI.Anchor((RectTransform)slider.transform, new Vector2(0f, 0f), new Vector2(1f, 0f),
                    new Vector2(0.5f, 0f), new Vector2(0f, 8f), new Vector2(0f, 20f));

                slider.onValueChanged.AddListener(v =>
                {
                    if (!CanInvoke) return;
                    value.text = v.ToString("0.##");
                    onChanged?.Invoke(v);
                });
                return slider;
            }

            /// <summary>Adds a labeled on/off toggle; fires <paramref name="onChanged" />.</summary>
            public Toggle AddToggle(string label, bool initial, Action<bool> onChanged)
            {
                RectTransform row = AddRow(40f);

                TMP_Text caption = SurvivorUI.Label("Caption", row, label, 18f, Text,
                    TextAlignmentOptions.Left, FontStyles.Bold);
                SurvivorUI.Anchor(caption.rectTransform, new Vector2(0f, 0f), new Vector2(0.75f, 1f),
                    new Vector2(0f, 0.5f), new Vector2(0f, 0f), new Vector2(0f, 0f));

                Image box = SurvivorUI.Image("Box", row, Track);
                SurvivorUI.Anchor(box.rectTransform, new Vector2(1f, 0.5f), new Vector2(1f, 0.5f),
                    new Vector2(1f, 0.5f), new Vector2(0f, 0f), new Vector2(34f, 34f));

                Image check = SurvivorUI.Image("Check", box.transform, Accent);
                SurvivorUI.Stretch(check.rectTransform, 6f);

                Toggle toggle = box.gameObject.AddComponent<Toggle>();
                toggle.targetGraphic = box;
                toggle.graphic = check;
                toggle.isOn = initial;
                toggle.onValueChanged.AddListener(v =>
                {
                    if (CanInvoke) onChanged?.Invoke(v);
                });
                return toggle;
            }

            /// <summary>Adds a big, centered hero label (e.g. a level number) and returns it to update.</summary>
            public TMP_Text AddBigLabel(string initial)
            {
                RectTransform row = AddRow(58f);
                TMP_Text big = SurvivorUI.Label("Big", row, initial, 44f, Text,
                    TextAlignmentOptions.Center, FontStyles.Bold);
                SurvivorUI.Stretch(big.rectTransform);
                return big;
            }

            /// <summary>Adds readable, wrapping instructions or status text inside the content card.</summary>
            public TMP_Text AddParagraph(string text, float height = 64f)
            {
                RectTransform row = AddRow(height);
                TMP_Text paragraph = SurvivorUI.Label("Instructions", row, text, 24f, Text,
                    TextAlignmentOptions.Left, FontStyles.Normal);
                SurvivorUI.Stretch(paragraph.rectTransform);
                paragraph.textWrappingMode = TextWrappingModes.Normal;
                return paragraph;
            }

            /// <summary>Adds a "label ....... value" row and returns the value <see cref="TMP_Text" /> to update.</summary>
            public TMP_Text AddValueLabel(string label)
            {
                RectTransform row = AddRow(30f);

                TMP_Text caption = SurvivorUI.Label("Caption", row, label, 22f, Muted,
                    TextAlignmentOptions.Left, FontStyles.Normal);
                SurvivorUI.Anchor(caption.rectTransform, new Vector2(0f, 0f), new Vector2(0.55f, 1f),
                    new Vector2(0f, 0.5f), Vector2.zero, Vector2.zero);

                TMP_Text value = SurvivorUI.Label("Value", row, "-", 24f, Text,
                    TextAlignmentOptions.Right, FontStyles.Bold);
                SurvivorUI.Anchor(value.rectTransform, new Vector2(0.45f, 0f), new Vector2(1f, 1f),
                    new Vector2(1f, 0.5f), Vector2.zero, Vector2.zero);
                return value;
            }

            /// <summary>Adds a labeled progress bar and returns the fill <see cref="Image" /> (use SurvivorUI.SetFill).</summary>
            public Image AddBar(string label, Color color)
            {
                RectTransform row = AddRow(44f);

                TMP_Text caption = SurvivorUI.Label("Caption", row, label, 15f, Muted,
                    TextAlignmentOptions.Left, FontStyles.Normal);
                SurvivorUI.Anchor(caption.rectTransform, new Vector2(0f, 1f), new Vector2(1f, 1f),
                    new Vector2(0f, 1f), new Vector2(0f, 0f), new Vector2(0f, 18f));

                Image fill = SurvivorUI.Bar("Bar", row, color, out RectTransform track);
                SurvivorUI.Anchor(track, new Vector2(0f, 0f), new Vector2(1f, 0f),
                    new Vector2(0.5f, 0f), new Vector2(0f, 4f), new Vector2(0f, 16f));
                SurvivorUI.SetFill(fill, 0f);
                return fill;
            }

            internal void BuildLog(Transform parent)
            {
                Image panel = SurvivorUI.Image("LogFeed", parent, new Color(Ink.r, Ink.g, Ink.b, 0.8f));
                SurvivorUI.Anchor(panel.rectTransform, new Vector2(0.5f, 0f), new Vector2(0.5f, 0f),
                    new Vector2(0.5f, 0f), new Vector2(0f, 26f), new Vector2(ContentWidth + 40f, 148f));
                panel.raycastTarget = false;

                TMP_Text head = SurvivorUI.Label("LogTitle", panel.transform, "LOG FEED", 12f, Accent,
                    TextAlignmentOptions.TopLeft, FontStyles.Bold);
                head.characterSpacing = 6f;
                SurvivorUI.Anchor(head.rectTransform, new Vector2(0f, 1f), new Vector2(1f, 1f),
                    new Vector2(0.5f, 1f), new Vector2(16f, -8f), new Vector2(-24f, 18f));

                _logText = SurvivorUI.Label("LogText", panel.transform, "› ready", 16f, Muted,
                    TextAlignmentOptions.TopLeft);
                _logText.textWrappingMode = TextWrappingModes.NoWrap;
                _logText.overflowMode = TextOverflowModes.Truncate;
                SurvivorUI.Stretch(_logText.rectTransform, 16f);
                _logText.rectTransform.offsetMax = new Vector2(-16f, -30f);
            }

            private RectTransform AddRow(float height)
            {
                if (IsDisposed) throw new ObjectDisposedException(nameof(Context));
                RectTransform row = SurvivorUI.Rect("Row", Content);
                LayoutElement element = row.gameObject.AddComponent<LayoutElement>();
                element.minHeight = height;
                element.preferredHeight = height;
                element.flexibleWidth = 1f;
                return row;
            }

            private static Slider BuildSlider(Transform parent, float min, float max, float initial, Color accent)
            {
                GameObject root = new("Slider", typeof(RectTransform));
                RectTransform rt = (RectTransform)root.transform;
                rt.SetParent(parent, false);
                Slider slider = root.AddComponent<Slider>();

                Image background = SurvivorUI.Image("Background", root.transform, Track);
                RectTransform bgRt = background.rectTransform;
                bgRt.anchorMin = new Vector2(0f, 0.25f);
                bgRt.anchorMax = new Vector2(1f, 0.75f);
                bgRt.offsetMin = Vector2.zero;
                bgRt.offsetMax = Vector2.zero;

                RectTransform fillArea = SurvivorUI.Rect("Fill Area", root.transform);
                fillArea.anchorMin = new Vector2(0f, 0.25f);
                fillArea.anchorMax = new Vector2(1f, 0.75f);
                fillArea.offsetMin = new Vector2(6f, 0f);
                fillArea.offsetMax = new Vector2(-6f, 0f);
                Image fill = SurvivorUI.Image("Fill", fillArea, accent);
                fill.rectTransform.sizeDelta = new Vector2(0f, 0f);

                RectTransform handleArea = SurvivorUI.Rect("Handle Slide Area", root.transform);
                handleArea.anchorMin = new Vector2(0f, 0f);
                handleArea.anchorMax = new Vector2(1f, 1f);
                handleArea.offsetMin = new Vector2(6f, 0f);
                handleArea.offsetMax = new Vector2(-6f, 0f);
                Image handle = SurvivorUI.Image("Handle", handleArea, Text);
                handle.rectTransform.sizeDelta = new Vector2(16f, 0f);

                slider.fillRect = fill.rectTransform;
                slider.handleRect = handle.rectTransform;
                slider.targetGraphic = handle;
                slider.direction = Slider.Direction.LeftToRight;
                slider.minValue = min;
                slider.maxValue = max;
                slider.SetValueWithoutNotify(initial);

                ColorBlock colors = slider.colors;
                colors.highlightedColor = Color.white;
                slider.colors = colors;
                return slider;
            }
        }
    }
}
