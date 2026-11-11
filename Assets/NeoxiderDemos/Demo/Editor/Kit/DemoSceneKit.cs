using System;
using System.Reflection;
using Neo.Samples;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using Object = UnityEngine.Object;

namespace Neo.Demo.Editor
{
    /// <summary>
    ///     Authors the standard NeoxiderTools demo frame as real, saved scene objects in the current NeoDemoShell
    ///     look: camera, overlay canvas (ScaleWithScreenSize 1920x1080) with an accent-tinted gradient backdrop,
    ///     header with an accent underline, a content card with vertical layout (<see cref="DemoPanel" /> rows),
    ///     the bottom log feed (<see cref="DemoLogFeed" />) and the module info card bound to
    ///     <see cref="ModuleDemoSceneInfo" />. Every helper goes through <see cref="DemoSceneBuildContext" />, so
    ///     re-running a builder reuses the same objects.
    /// </summary>
    public static class DemoSceneKit
    {
        public const float ScreenMargin = 24f;
        public const int InfoCardSortingOrder = 30000;

        private static readonly Color InfoTitleColor = new Color32(242, 242, 247, 255);
        private static readonly Color InfoDescriptionColor = new Color32(230, 231, 240, 255);
        private static readonly Color InfoHintColor = new Color32(186, 189, 204, 255);
        private static readonly Color InfoCollapseColor = new Color32(201, 203, 216, 255);

        /// <summary>Builds (or refreshes) the whole demo frame. Call once per scene build.</summary>
        public static DemoFrame Frame(DemoSceneBuildContext ctx, DemoFrameOptions options)
        {
            if (ctx == null)
            {
                throw new ArgumentNullException(nameof(ctx));
            }

            if (options == null)
            {
                throw new ArgumentNullException(nameof(options));
            }

            DemoFrame frame = new(ctx, options);
            frame.Camera = options.Camera ? EnsureCamera(ctx, options.Accent) : ctx.Find<Camera>(true);
            frame.EventSystem = options.EventSystem
                ? EnsureEventSystem(ctx, options.InputModule, options.UpgradeLegacyInputModule)
                : ctx.Find<EventSystem>(true);

            GameObject root = ctx.Root(options.RootKey, true);
            frame.Root = root;
            frame.Canvas = OverlayCanvas(ctx, root, options.SortingOrder, options.ReferenceResolution);
            frame.CanvasRect = (RectTransform)root.transform;

            if (options.Backdrop != DemoBackdrop.None)
            {
                float alpha = options.Backdrop == DemoBackdrop.Dim ? Mathf.Clamp01(options.DimAlpha) : 1f;
                frame.Backdrop = Backdrop(ctx, frame.CanvasRect, options.Accent, alpha);
            }

            if (options.Header)
            {
                Header(ctx, frame, options);
            }

            if (options.ContentCard)
            {
                frame.Content = ContentCard(ctx, frame.CanvasRect, options);
            }

            if (options.LogFeed)
            {
                frame.Log = LogFeed(ctx, frame.CanvasRect, options);
            }

            return frame;
        }

        /// <summary>
        ///     Standalone overlay canvas "[Demo] <paramref name="rootKey" />" holding the info card (scenes without a
        ///     frame, decorated hand-authored games). Add more cards to it with <see cref="CornerPanel" /> on
        ///     <c>(RectTransform)ctx.Root(rootKey, true).transform</c>.
        /// </summary>
        /// <param name="accent">When set, also becomes the info component's accent color.</param>
        /// <param name="upgradeLegacyInputModule">Replace a hand-authored StandaloneInputModule (see <see cref="UpgradeLegacyInputModule" />).</param>
        public static ModuleDemoSceneInfo InfoCardCanvas(DemoSceneBuildContext ctx, ModuleDemoSceneInfo info,
            DemoInfoContent content = null, string rootKey = "Info Card", Color? accent = null,
            bool upgradeLegacyInputModule = false)
        {
            EnsureEventSystem(ctx, DemoInputModule.Auto, upgradeLegacyInputModule);
            GameObject root = ctx.Root(rootKey, true);
            OverlayCanvas(ctx, root, InfoCardSortingOrder, new Vector2(1920f, 1080f));
            return InfoCard(ctx, (RectTransform)root.transform, info, content, accent);
        }

        /// <summary>
        ///     Authors the info card under <paramref name="parent" />, binds it to <paramref name="info" /> and wires
        ///     the collapse button persistently to <see cref="ModuleDemoSceneInfo.ToggleCollapsed" />.
        /// </summary>
        /// <param name="accent">When set, also becomes the info component's accent color.</param>
        public static ModuleDemoSceneInfo InfoCard(DemoSceneBuildContext ctx, RectTransform parent, ModuleDemoSceneInfo info,
            DemoInfoContent content, Color? accent)
        {
            if (info == null)
            {
                throw new ArgumentNullException(nameof(info), "InfoCard needs the scene's ModuleDemoSceneInfo " +
                                                              "(use ctx.FindOrCreate<ModuleDemoSceneInfo>).");
            }

            if (content != null)
            {
                SetInfoContent(ctx, info, content);
            }

            if (accent.HasValue)
            {
                ctx.Set(info, "_accentColor", accent.Value);
            }

            Color infoAccent = info.AccentColor;
            Image panel = Image(ctx, parent, "InfoCard", info.BackgroundColor, true, true);
            RectTransform panelRect = panel.rectTransform;
            Vector2 anchor = CornerAnchor(info.OverlayCorner, out Vector2 offset);
            panelRect.anchorMin = anchor;
            panelRect.anchorMax = anchor;
            panelRect.pivot = anchor;
            panelRect.anchoredPosition = offset;
            panelRect.sizeDelta = new Vector2(info.PanelWidth, 0f);
            Shadow(ctx, panel.gameObject, new Color(0f, 0f, 0f, 0.35f), new Vector2(0f, -3f));
            VerticalLayout(ctx, panel.gameObject, new RectOffset(18, 18, 14, 16), 10f);
            FitHeight(ctx, panel.gameObject);

            RectTransform header = ctx.UiChild(panelRect, "Header");
            HorizontalLayoutGroup headerRow = ctx.Ensure<HorizontalLayoutGroup>(header.gameObject);
            headerRow.padding = new RectOffset(0, 0, 0, 0);
            headerRow.spacing = 10f;
            headerRow.childAlignment = TextAnchor.MiddleLeft;
            headerRow.childControlWidth = true;
            headerRow.childControlHeight = true;
            headerRow.childForceExpandWidth = false;
            headerRow.childForceExpandHeight = false;

            RectTransform titles = ctx.UiChild(header, "Titles");
            VerticalLayout(ctx, titles.gameObject, new RectOffset(0, 0, 0, 0), 1f);
            LayoutElement titlesSize = ctx.Ensure<LayoutElement>(titles.gameObject);
            titlesSize.flexibleWidth = 1f;
            TMP_Text overline = Text(ctx, titles, "Overline", "NEOXIDER · DEMO SCENE", 10.5f, infoAccent,
                TextAlignmentOptions.Left, FontStyles.Bold);
            overline.characterSpacing = 8f;
            TMP_Text title = Text(ctx, titles, "Title", info.ModuleName, 21f, InfoTitleColor, TextAlignmentOptions.Left,
                FontStyles.Bold);

            RectTransform collapse = ctx.UiChild(header, "Collapse");
            LayoutElement collapseSize = ctx.Ensure<LayoutElement>(collapse.gameObject);
            collapseSize.preferredWidth = 30f;
            collapseSize.preferredHeight = 26f;
            collapseSize.flexibleWidth = 0f;
            Image collapseImage = ctx.Ensure<UnityEngine.UI.Image>(collapse.gameObject);
            StyleImage(collapseImage, new Color(1f, 1f, 1f, 0.07f), true, true);
            Button collapseButton = ctx.Ensure<Button>(collapse.gameObject);
            collapseButton.interactable = true;
            collapseButton.targetGraphic = collapseImage;
            TMP_Text collapseLabel = Text(ctx, collapse, "Label", "-", 17f, InfoCollapseColor, TextAlignmentOptions.Center,
                FontStyles.Bold);
            Stretch(collapseLabel.rectTransform);
            DemoWiring.AddVoid(collapseButton.onClick, info.ToggleCollapsed);

            Image divider = Image(ctx, panelRect, "Divider", DemoPalette.WithAlpha(infoAccent, 0.9f), false);
            LayoutElement dividerSize = ctx.Ensure<LayoutElement>(divider.gameObject);
            dividerSize.minHeight = 2f;
            dividerSize.preferredHeight = 2f;
            dividerSize.flexibleWidth = 1f;

            RectTransform body = ctx.UiChild(panelRect, "Body");
            VerticalLayout(ctx, body.gameObject, new RectOffset(0, 0, 0, 0), 7f);
            TMP_Text description = Text(ctx, body, "Description", info.Purpose, 15f, InfoDescriptionColor);
            TMP_Text hints = Text(ctx, body, "Hints", string.Empty, 13f, InfoHintColor);
            hints.paragraphSpacing = 50f;

            info.AssignOverlay(panel.gameObject, title, description, hints, collapseLabel, divider.gameObject,
                body.gameObject);
            // WHY: the saved card shows the state Play Mode starts in, so the edit-mode Game view matches it.
            info.SetCollapsed(info.StartCollapsed);
            info.ApplyContent();
            ctx.MarkModified(info);
            if (panel.gameObject.activeSelf != info.ShowRuntimeOverlay)
            {
                panel.gameObject.SetActive(info.ShowRuntimeOverlay);
            }

            return info;
        }

        /// <summary>Writes the given texts into the info component's serialized fields (null values are kept).</summary>
        public static void SetInfoContent(DemoSceneBuildContext ctx, ModuleDemoSceneInfo info, DemoInfoContent content)
        {
            if (info == null || content == null)
            {
                return;
            }

            SetIfNotNull(ctx, info, "_moduleName", content.Title);
            SetIfNotNull(ctx, info, "_purpose", content.Purpose);
            SetIfNotNull(ctx, info, "_runtimeApi", content.RuntimeApi);
            SetIfNotNull(ctx, info, "_sceneWorkflow", content.SceneWorkflow);
            SetIfNotNull(ctx, info, "_verification", content.Verification);
            if (content.Hints != null)
            {
                ctx.Set(info, "_hints", content.Hints);
            }

            if (content.Corner.HasValue)
            {
                ctx.Set(info, "_corner", (int)content.Corner.Value);
            }

            if (content.StartCollapsed.HasValue)
            {
                ctx.Set(info, "_startCollapsed", content.StartCollapsed.Value);
            }

            if (content.PanelWidth.HasValue)
            {
                ctx.Set(info, "_panelWidth", content.PanelWidth.Value);
            }

            if (content.ShowRuntimeOverlay.HasValue)
            {
                ctx.Set(info, "_showRuntimeOverlay", content.ShowRuntimeOverlay.Value);
            }
        }

        /// <summary>Scene camera: the hand-authored one when present, otherwise "[Demo] Camera" (orthographic).</summary>
        public static Camera EnsureCamera(DemoSceneBuildContext ctx, Color accent)
        {
            Camera existing = ctx.Find<Camera>();
            if (existing != null)
            {
                return existing;
            }

            GameObject root = ctx.Root("Camera");
            root.tag = "MainCamera";
            root.transform.SetPositionAndRotation(new Vector3(0f, 0f, -10f), Quaternion.identity);
            Camera camera = ctx.Ensure<Camera>(root);
            camera.orthographic = true;
            camera.orthographicSize = 5.4f;
            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor = DemoPalette.CameraBackground(accent);
            if (ctx.Find<AudioListener>() == null)
            {
                ctx.Ensure<AudioListener>(root);
            }

            return camera;
        }

        /// <summary>Scene EventSystem: the hand-authored one when present, otherwise "[Demo] EventSystem".</summary>
        public static EventSystem EnsureEventSystem(DemoSceneBuildContext ctx, DemoInputModule module)
        {
            return EnsureEventSystem(ctx, module, false);
        }

        /// <summary>
        ///     Scene EventSystem: the hand-authored one when present (with <paramref name="upgradeLegacyModule" /> its
        ///     StandaloneInputModule is replaced, see <see cref="UpgradeLegacyInputModule" />), otherwise
        ///     "[Demo] EventSystem" with the resolved module.
        /// </summary>
        public static EventSystem EnsureEventSystem(DemoSceneBuildContext ctx, DemoInputModule module,
            bool upgradeLegacyModule)
        {
            EventSystem existing = ctx.Find<EventSystem>();
            if (existing != null)
            {
                if (upgradeLegacyModule)
                {
                    UpgradeLegacyInputModule(ctx, existing, module);
                }

                return existing;
            }

            GameObject root = ctx.Root("EventSystem");
            EventSystem eventSystem = ctx.Ensure<EventSystem>(root);
            Type moduleType = ResolveInputModule(module);
            foreach (BaseInputModule stale in root.GetComponents<BaseInputModule>())
            {
                if (stale.GetType() != moduleType)
                {
                    Object.DestroyImmediate(stale);
                }
            }

            ctx.Ensure(root, moduleType);
            return eventSystem;
        }

        /// <summary>
        ///     Replaces a legacy StandaloneInputModule on a hand-authored EventSystem with the module
        ///     <see cref="ResolveInputModule" /> picks (InputSystemUIInputModule whenever the Input System package is
        ///     present) and notes it in the summary; an EventSystem without any module gets one. StandaloneInputModule
        ///     throws every frame in Input-System-only projects (the Unity 6 default), and samples are imported into
        ///     such projects. Nothing changes without the Input System package, on generated EventSystems (the kit
        ///     already authors them) or on a module that comes from a prefab instance (warned).
        /// </summary>
        /// <returns>True when the EventSystem was changed.</returns>
        public static bool UpgradeLegacyInputModule(DemoSceneBuildContext ctx, EventSystem eventSystem,
            DemoInputModule module = DemoInputModule.Auto)
        {
            if (eventSystem == null || DemoSceneBuildContext.IsGenerated(eventSystem.gameObject))
            {
                return false;
            }

            Type preferred = ResolveInputModule(module);
            if (preferred == typeof(StandaloneInputModule))
            {
                return false;
            }

            GameObject host = eventSystem.gameObject;
            string path = DemoSceneBuildContext.PathOf(host.transform);
            StandaloneInputModule legacy = host.GetComponent<StandaloneInputModule>();
            if (legacy != null)
            {
                if (PrefabUtility.IsPartOfPrefabInstance(legacy))
                {
                    ctx.Warn("EventSystem '" + path + "' keeps StandaloneInputModule: it comes from a prefab instance.");
                    return false;
                }

                Object.DestroyImmediate(legacy);
                ctx.Ensure(host, preferred);
                ctx.MarkModified(host);
                ctx.Note("Replaced StandaloneInputModule with " + preferred.Name + " on '" + path + "'.");
                return true;
            }

            if (host.GetComponent<BaseInputModule>() != null)
            {
                return false;
            }

            ctx.Ensure(host, preferred);
            ctx.MarkModified(host);
            ctx.Note("Added " + preferred.Name + " to '" + path + "' (it had no input module).");
            return true;
        }

        /// <summary><see cref="UpgradeLegacyInputModule" /> for every hand-authored EventSystem of the scene.</summary>
        /// <returns>Number of changed EventSystems.</returns>
        public static int UpgradeLegacyInputModules(DemoSceneBuildContext ctx, DemoInputModule module = DemoInputModule.Auto)
        {
            int changed = 0;
            foreach (EventSystem eventSystem in ctx.FindAll<EventSystem>())
            {
                if (UpgradeLegacyInputModule(ctx, eventSystem, module))
                {
                    changed++;
                }
            }

            return changed;
        }

        /// <summary>
        ///     Input module type for an authored EventSystem. NeoDemoShell picked at runtime per project; an authored
        ///     scene is saved once and imported into other projects, so <see cref="DemoInputModule.Auto" /> prefers
        ///     InputSystemUIInputModule whenever the Input System package exists (a package dependency): it works in
        ///     "Input System" and "Both" handling, while StandaloneInputModule throws in Input-System-only projects.
        /// </summary>
        public static Type ResolveInputModule(DemoInputModule module)
        {
            Type inputSystem = Type.GetType("UnityEngine.InputSystem.UI.InputSystemUIInputModule, Unity.InputSystem");
            switch (module)
            {
                case DemoInputModule.Legacy:
                    return typeof(StandaloneInputModule);
                case DemoInputModule.InputSystem:
                    return inputSystem ?? throw new InvalidOperationException(
                        "DemoInputModule.InputSystem requested but com.unity.inputsystem is not installed.");
                default:
                    return inputSystem ?? typeof(StandaloneInputModule);
            }
        }

        /// <summary>Overlay canvas + ScaleWithScreenSize scaler + raycaster on <paramref name="host" />.</summary>
        public static Canvas OverlayCanvas(DemoSceneBuildContext ctx, GameObject host, int sortingOrder,
            Vector2 referenceResolution)
        {
            Canvas canvas = ctx.Ensure<Canvas>(host);
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = sortingOrder;
            canvas.pixelPerfect = false;
            CanvasScaler scaler = ctx.Ensure<CanvasScaler>(host);
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = referenceResolution;
            scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
            scaler.matchWidthOrHeight = 0.5f;
            ctx.Ensure<GraphicRaycaster>(host);
            return canvas;
        }

        /// <summary>Extra floating card on a frame (used by <see cref="DemoFrame.Panel" />).</summary>
        public static DemoPanel FloatingPanel(DemoFrame frame, string name, DemoPanelPlacement placement, float width,
            Vector2 offset)
        {
            return CornerPanel(frame.Context, frame.CanvasRect, name, placement, width, frame.Accent, offset);
        }

        /// <summary>
        ///     Frame-less card (rounded <see cref="DemoPalette.Card" /> background, shadow, vertical layout, height fitted
        ///     to its rows) anchored at <paramref name="placement" /> of any canvas rect, e.g. a live panel next to a
        ///     hand-authored game on the <see cref="InfoCardCanvas" /> root.
        /// </summary>
        /// <param name="raycastBackground">
        ///     False lets clicks around the card's controls reach the game below (the controls still raycast).
        /// </param>
        /// <param name="padding">Card padding; null uses 18 px on every side.</param>
        public static DemoPanel CornerPanel(DemoSceneBuildContext ctx, RectTransform parent, string name,
            DemoPanelPlacement placement, float width, Color accent, Vector2 offset = default,
            bool raycastBackground = true, RectOffset padding = null, float spacing = 8f)
        {
            Image card = Image(ctx, parent, name, DemoPalette.Card, true, raycastBackground);
            RectTransform rect = card.rectTransform;
            Vector2 anchor = PanelAnchor(placement, out Vector2 margin);
            rect.anchorMin = anchor;
            rect.anchorMax = anchor;
            rect.pivot = anchor;
            rect.anchoredPosition = margin + offset;
            rect.sizeDelta = new Vector2(width, 0f);
            Shadow(ctx, card.gameObject, new Color(0f, 0f, 0f, 0.4f), new Vector2(0f, -4f));
            VerticalLayout(ctx, card.gameObject, padding ?? new RectOffset(18, 18, 18, 18), spacing);
            FitHeight(ctx, card.gameObject);
            return new DemoPanel(ctx, rect, accent);
        }

        /// <summary>Accent button named "Btn_&lt;label&gt;" with a centered auto-sized label.</summary>
        public static Button AccentButton(DemoSceneBuildContext ctx, Transform parent, string label, Color color)
        {
            return AccentButton(ctx, parent, label, color, DemoPalette.Ink);
        }

        /// <summary>
        ///     Accent button with a custom label color: use <see cref="DemoPalette.Text" /> on saturated or dark accents
        ///     (<see cref="DemoPalette.Accent" />, <see cref="DemoPalette.Danger" />, <see cref="DemoPalette.Panel" />),
        ///     where the default dark ink label has low contrast.
        /// </summary>
        public static Button AccentButton(DemoSceneBuildContext ctx, Transform parent, string label, Color color,
            Color labelColor)
        {
            RectTransform rect = ctx.UiChild(parent, "Btn_" + label);
            Image image = ctx.Ensure<UnityEngine.UI.Image>(rect.gameObject);
            StyleImage(image, color, true, true);
            Button button = ctx.Ensure<Button>(rect.gameObject);
            button.interactable = true;
            button.transition = Selectable.Transition.ColorTint;
            button.targetGraphic = image;
            ColorBlock colors = ColorBlock.defaultColorBlock;
            colors.normalColor = Color.white;
            colors.highlightedColor = Color.white;
            colors.selectedColor = Color.white;
            colors.pressedColor = new Color(0.85f, 0.85f, 0.9f, 1f);
            colors.colorMultiplier = 1f;
            button.colors = colors;
            TMP_Text text = Text(ctx, rect, "Label", label, 24f, labelColor, TextAlignmentOptions.Center,
                FontStyles.Bold);
            Stretch(text.rectTransform, 4f);
            text.enableAutoSizing = true;
            text.fontSizeMin = 14f;
            text.fontSizeMax = 24f;
            text.textWrappingMode = TextWrappingModes.NoWrap;
            return button;
        }

        /// <summary>TextMeshPro label child styled with the demo font.</summary>
        public static TMP_Text Text(DemoSceneBuildContext ctx, Transform parent, string name, string text, float size,
            Color color, TextAlignmentOptions alignment = TextAlignmentOptions.Left, FontStyles style = FontStyles.Normal)
        {
            RectTransform rect = ctx.UiChild(parent, name);
            TextMeshProUGUI label = ctx.Ensure<TextMeshProUGUI>(rect.gameObject);
            StyleText(label, text, size, color, alignment, style);
            return label;
        }

        /// <summary>Resets a label to the kit's text style (font, size, color, wrapping, no raycast).</summary>
        public static void StyleText(TMP_Text label, string text, float size, Color color, TextAlignmentOptions alignment,
            FontStyles style)
        {
            TMP_FontAsset font = DemoSceneAssets.Font;
            if (font != null && label.font != font)
            {
                label.font = font;
                label.fontSharedMaterial = font.material;
            }

            label.text = text ?? string.Empty;
            label.fontSize = size;
            label.enableAutoSizing = false;
            label.color = color;
            label.alignment = alignment;
            label.fontStyle = style;
            label.characterSpacing = 0f;
            label.paragraphSpacing = 0f;
            label.richText = true;
            label.raycastTarget = false;
            label.textWrappingMode = TextWrappingModes.Normal;
            label.overflowMode = TextOverflowModes.Overflow;
            label.margin = Vector4.zero;
        }

        /// <summary>Image child; rounded uses the 9-sliced kit sprite, otherwise a plain quad.</summary>
        public static Image Image(DemoSceneBuildContext ctx, Transform parent, string name, Color color, bool rounded = true,
            bool raycastTarget = false)
        {
            RectTransform rect = ctx.UiChild(parent, name);
            Image image = ctx.Ensure<UnityEngine.UI.Image>(rect.gameObject);
            StyleImage(image, color, rounded, raycastTarget);
            return image;
        }

        public static void StyleImage(Image image, Color color, bool rounded, bool raycastTarget)
        {
            image.sprite = rounded ? DemoSceneAssets.RoundedRect : null;
            image.type = rounded ? UnityEngine.UI.Image.Type.Sliced : UnityEngine.UI.Image.Type.Simple;
            image.fillCenter = true;
            image.pixelsPerUnitMultiplier = 1f;
            image.preserveAspect = false;
            image.color = color;
            image.raycastTarget = raycastTarget;
        }

        /// <summary>World-space sprite child (SpriteRenderer) for demo props.</summary>
        public static SpriteRenderer WorldSprite(DemoSceneBuildContext ctx, Transform parent, string name, Sprite sprite,
            Color color, Vector3 localPosition, float scale = 1f, int sortingOrder = 0)
        {
            GameObject host = ctx.Child(parent, name);
            host.transform.localPosition = localPosition;
            host.transform.localRotation = Quaternion.identity;
            host.transform.localScale = Vector3.one * scale;
            SpriteRenderer renderer = ctx.Ensure<SpriteRenderer>(host);
            renderer.sprite = sprite;
            renderer.color = color;
            renderer.sortingOrder = sortingOrder;
            return renderer;
        }

        /// <summary>Anchors a rect like SurvivorUI.Anchor: horizontally stretched rects keep only the height.</summary>
        public static RectTransform Anchor(RectTransform rect, Vector2 anchorMin, Vector2 anchorMax, Vector2 pivot,
            Vector2 anchoredPosition, Vector2 size)
        {
            rect.anchorMin = anchorMin;
            rect.anchorMax = anchorMax;
            rect.pivot = pivot;
            rect.anchoredPosition = anchoredPosition;
            rect.sizeDelta = Mathf.Approximately(anchorMin.x, anchorMax.x) ? size : new Vector2(0f, size.y);
            return rect;
        }

        /// <summary>Sets anchors and edge offsets directly.</summary>
        public static RectTransform SetAnchors(RectTransform rect, Vector2 anchorMin, Vector2 anchorMax, Vector2 offsetMin,
            Vector2 offsetMax)
        {
            rect.anchorMin = anchorMin;
            rect.anchorMax = anchorMax;
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.offsetMin = offsetMin;
            rect.offsetMax = offsetMax;
            return rect;
        }

        /// <summary>Stretches a rect over its parent with an even padding.</summary>
        public static void Stretch(RectTransform rect, float padding = 0f)
        {
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.offsetMin = new Vector2(padding, padding);
            rect.offsetMax = new Vector2(-padding, -padding);
        }

        /// <summary>Vertical layout that controls child sizes and expands width (cards, columns).</summary>
        public static VerticalLayoutGroup VerticalLayout(DemoSceneBuildContext ctx, GameObject host, RectOffset padding,
            float spacing)
        {
            VerticalLayoutGroup layout = ctx.Ensure<VerticalLayoutGroup>(host);
            layout.padding = padding;
            layout.spacing = spacing;
            layout.childAlignment = TextAnchor.UpperCenter;
            layout.childControlWidth = true;
            layout.childControlHeight = true;
            layout.childForceExpandWidth = true;
            layout.childForceExpandHeight = false;
            layout.childScaleWidth = false;
            layout.childScaleHeight = false;
            layout.reverseArrangement = false;
            return layout;
        }

        /// <summary>Content size fitter that sizes <paramref name="host" />'s height to its layout (cards).</summary>
        public static ContentSizeFitter FitHeight(DemoSceneBuildContext ctx, GameObject host)
        {
            ContentSizeFitter fitter = ctx.Ensure<ContentSizeFitter>(host);
            fitter.horizontalFit = ContentSizeFitter.FitMode.Unconstrained;
            fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
            return fitter;
        }

        /// <summary>Drop shadow of kit cards (graphic alpha respected).</summary>
        public static Shadow Shadow(DemoSceneBuildContext ctx, GameObject host, Color color, Vector2 distance)
        {
            Shadow shadow = ctx.Ensure<Shadow>(host);
            shadow.effectColor = color;
            shadow.effectDistance = distance;
            shadow.useGraphicAlpha = true;
            return shadow;
        }

        /// <summary>
        ///     The demo backdrop as a "Backdrop" child of <paramref name="parent" />: a full-rect image in the accent's
        ///     bottom color with the kit gradient on top (<see cref="StyleBackdrop" />). Place it first so it stays
        ///     behind the content (for example under a hand-authored game canvas).
        /// </summary>
        public static Image Backdrop(DemoSceneBuildContext ctx, RectTransform parent, Color accent, float alpha = 1f)
        {
            return StyleBackdrop(ctx, ctx.UiChild(parent, "Backdrop"), accent, alpha);
        }

        /// <summary>Turns <paramref name="host" /> itself into the stretched demo backdrop (image + "Gradient" child).</summary>
        public static Image StyleBackdrop(DemoSceneBuildContext ctx, RectTransform host, Color accent, float alpha = 1f)
        {
            Image backdrop = ctx.Ensure<UnityEngine.UI.Image>(host.gameObject);
            StyleImage(backdrop, DemoPalette.WithAlpha(DemoPalette.BackdropBottom(accent), alpha), false, false);
            Stretch(host);
            Image gradient = Image(ctx, host, "Gradient", DemoPalette.WithAlpha(DemoPalette.BackdropTop(accent), alpha),
                false);
            gradient.sprite = DemoSceneAssets.Gradient;
            Stretch(gradient.rectTransform);
            if (gradient.sprite == null)
            {
                ctx.Warn("Backdrop gradient sprite missing; the backdrop is a flat color.");
                gradient.enabled = false;
            }
            else
            {
                gradient.enabled = true;
            }

            return backdrop;
        }

        /// <summary>
        ///     World-space TextMeshPro label (annotations next to scene objects) on a RectTransform child, centered,
        ///     bold, single line, drawn with <paramref name="sortingOrder" /> above sprites.
        /// </summary>
        public static TextMeshPro WorldLabel(DemoSceneBuildContext ctx, Transform parent, string name, string text,
            Vector3 localPosition, float fontSize, Color color, Vector2 size, int sortingOrder = 5)
        {
            RectTransform rect = ctx.UiChild(parent, name);
            TextMeshPro label = ctx.Ensure<TextMeshPro>(rect.gameObject);
            StyleText(label, text, fontSize, color, TextAlignmentOptions.Center, FontStyles.Bold);
            label.textWrappingMode = TextWrappingModes.NoWrap;
            rect.sizeDelta = size;
            rect.localPosition = localPosition;
            SyncAnchoredPosition(rect);
            rect.localRotation = Quaternion.identity;
            rect.localScale = Vector3.one;
            ctx.MarkModified(rect);
            MeshRenderer renderer = label.GetComponent<MeshRenderer>();
            if (renderer != null)
            {
                renderer.sortingOrder = sortingOrder;
            }

            return label;
        }

        /// <summary>
        ///     Writes the anchored position of a RectTransform under a plain Transform from its local position. Such a
        ///     RectTransform saves m_AnchoredPosition, which setting localPosition in an editor build without a frame does
        ///     not refresh, so a created or rebuilt world label would be saved at its old (or zero) place.
        /// </summary>
        public static void SyncAnchoredPosition(RectTransform rect)
        {
            if (rect != null && !(rect.parent is RectTransform))
            {
                Vector3 local = rect.localPosition;
                rect.anchoredPosition = new Vector2(local.x, local.y);
            }
        }

        /// <summary>
        ///     <see cref="DemoValueReadout" /> on an authored label (format, prefix, suffix and the optional bool texts
        ///     are saved with the scene); wire it with <c>DemoWiring.AddDynamic(evt, readout.SetFloat)</c>.
        /// </summary>
        public static DemoValueReadout Readout(DemoSceneBuildContext ctx, TMP_Text label, string format,
            string prefix = "", string suffix = "", string onText = null, string offText = null)
        {
            DemoValueReadout readout = ctx.Ensure<DemoValueReadout>(label.gameObject);
            readout.Configure(label, format, prefix, suffix);
            if (onText != null)
            {
                ctx.Set(readout, "_onText", onText);
            }

            if (offText != null)
            {
                ctx.Set(readout, "_offText", offText);
            }

            ctx.MarkModified(readout);
            return readout;
        }

        /// <summary>Runs one build step; a failure is rethrown naming the scene and the step (see <see cref="StepFailed" />).</summary>
        public static void Step(string scene, string step, Action action)
        {
            try
            {
                action();
            }
            catch (Exception exception)
            {
                throw StepFailed(scene, step, exception);
            }
        }

        /// <summary>Runs one build step that returns a value; a failure names the scene and the step.</summary>
        public static T Step<T>(string scene, string step, Func<T> action)
        {
            try
            {
                return action();
            }
            catch (Exception exception)
            {
                throw StepFailed(scene, step, exception);
            }
        }

        /// <summary>
        ///     "[scene] step 'x' failed (ExceptionType): message" wrapper with the original exception as the inner
        ///     one, so the runner summary says where a builder broke.
        /// </summary>
        public static Exception StepFailed(string scene, string step, Exception exception)
        {
            Exception cause = exception is TargetInvocationException invocation && invocation.InnerException != null
                ? invocation.InnerException
                : exception;
            return new InvalidOperationException(
                "[" + scene + "] step '" + step + "' failed (" + cause.GetType().Name + "): " + cause.Message, cause);
        }

        private static void Header(DemoSceneBuildContext ctx, DemoFrame frame, DemoFrameOptions options)
        {
            RectTransform header = ctx.UiChild(frame.CanvasRect, "Header");
            Anchor(header, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -54f),
                new Vector2(options.HeaderWidth ?? options.ContentWidth + 40f, 96f));

            TMP_Text overline = Text(ctx, header, "Overline", options.Overline, 15f,
                DemoPalette.WithAlpha(options.Accent, 0.95f), TextAlignmentOptions.Center, FontStyles.Bold);
            overline.characterSpacing = 10f;
            Anchor(overline.rectTransform, new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(0.5f, 1f),
                new Vector2(0f, -2f), new Vector2(0f, 22f));
            frame.Overline = overline;

            TMP_Text title = Text(ctx, header, "Title", options.Title, 40f, DemoPalette.Text, TextAlignmentOptions.Center,
                FontStyles.Bold);
            Anchor(title.rectTransform, new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(0.5f, 1f),
                new Vector2(0f, -26f), new Vector2(0f, 52f));
            // WHY: long module titles must shrink instead of wrapping up onto the overline.
            title.enableAutoSizing = true;
            title.fontSizeMax = 40f;
            title.fontSizeMin = 22f;
            title.textWrappingMode = TextWrappingModes.NoWrap;
            frame.Title = title;

            Image underline = Image(ctx, header, "Underline", options.Accent, false);
            Anchor(underline.rectTransform, new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0.5f, 0f),
                new Vector2(0f, 4f), new Vector2(200f, 3f));
        }

        private static DemoPanel ContentCard(DemoSceneBuildContext ctx, RectTransform canvas, DemoFrameOptions options)
        {
            Image card = Image(ctx, canvas, "Content", DemoPalette.Card, true, true);
            RectTransform rect = card.rectTransform;
            switch (options.Placement)
            {
                case DemoContentPlacement.Left:
                    rect.anchorMin = rect.anchorMax = rect.pivot = new Vector2(0f, 0.5f);
                    rect.anchoredPosition = new Vector2(40f, options.ContentOffsetY);
                    break;
                case DemoContentPlacement.Right:
                    rect.anchorMin = rect.anchorMax = rect.pivot = new Vector2(1f, 0.5f);
                    rect.anchoredPosition = new Vector2(-40f, options.ContentOffsetY);
                    break;
                default:
                    rect.anchorMin = rect.anchorMax = rect.pivot = new Vector2(0.5f, 0.5f);
                    rect.anchoredPosition = new Vector2(0f, options.ContentOffsetY);
                    break;
            }

            rect.sizeDelta = new Vector2(options.ContentWidth, 0f);
            Shadow(ctx, card.gameObject, new Color(0f, 0f, 0f, 0.4f), new Vector2(0f, -4f));
            VerticalLayout(ctx, card.gameObject, new RectOffset(24, 24, 24, 24), 8f);
            FitHeight(ctx, card.gameObject);
            return new DemoPanel(ctx, rect, options.Accent);
        }

        private static DemoLogFeed LogFeed(DemoSceneBuildContext ctx, RectTransform canvas, DemoFrameOptions options)
        {
            Image panel = Image(ctx, canvas, "LogFeed", DemoPalette.WithAlpha(DemoPalette.Ink, 0.8f));
            Anchor(panel.rectTransform, new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0.5f, 0f),
                new Vector2(0f, 26f), new Vector2(options.LogWidth ?? options.ContentWidth + 40f, 148f));

            TMP_Text head = Text(ctx, panel.transform, "LogTitle", "LOG FEED", 12f, options.Accent,
                TextAlignmentOptions.TopLeft, FontStyles.Bold);
            head.characterSpacing = 6f;
            SetAnchors(head.rectTransform, new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(16f, -26f),
                new Vector2(-16f, -8f));

            int lines = Mathf.Clamp(options.LogLines, 1, DemoLogFeed.MaxCapacity);
            TMP_Text text = Text(ctx, panel.transform, "LogText", "› ready", 16f, DemoPalette.Muted,
                TextAlignmentOptions.TopLeft);
            text.textWrappingMode = TextWrappingModes.NoWrap;
            text.overflowMode = TextOverflowModes.Truncate;
            Stretch(text.rectTransform, 16f);
            text.rectTransform.offsetMax = new Vector2(-16f, -30f);

            DemoLogFeed feed = ctx.Ensure<DemoLogFeed>(panel.gameObject);
            ctx.Set(feed, "_text", text);
            ctx.Set(feed, "_maxLines", lines);
            ctx.Set(feed, "_lineColor", DemoPalette.Muted);
            return feed;
        }

        private static Vector2 CornerAnchor(ModuleDemoSceneInfo.Corner corner, out Vector2 offset)
        {
            switch (corner)
            {
                case ModuleDemoSceneInfo.Corner.TopRight:
                    offset = new Vector2(-ScreenMargin, -ScreenMargin);
                    return new Vector2(1f, 1f);
                case ModuleDemoSceneInfo.Corner.BottomLeft:
                    offset = new Vector2(ScreenMargin, ScreenMargin);
                    return new Vector2(0f, 0f);
                case ModuleDemoSceneInfo.Corner.BottomRight:
                    offset = new Vector2(-ScreenMargin, ScreenMargin);
                    return new Vector2(1f, 0f);
                default:
                    offset = new Vector2(ScreenMargin, -ScreenMargin);
                    return new Vector2(0f, 1f);
            }
        }

        private static Vector2 PanelAnchor(DemoPanelPlacement placement, out Vector2 margin)
        {
            switch (placement)
            {
                case DemoPanelPlacement.TopRight:
                    margin = new Vector2(-ScreenMargin, -ScreenMargin);
                    return new Vector2(1f, 1f);
                case DemoPanelPlacement.BottomLeft:
                    margin = new Vector2(ScreenMargin, ScreenMargin);
                    return new Vector2(0f, 0f);
                case DemoPanelPlacement.BottomRight:
                    margin = new Vector2(-ScreenMargin, ScreenMargin);
                    return new Vector2(1f, 0f);
                case DemoPanelPlacement.Left:
                    margin = new Vector2(ScreenMargin, 0f);
                    return new Vector2(0f, 0.5f);
                case DemoPanelPlacement.Right:
                    margin = new Vector2(-ScreenMargin, 0f);
                    return new Vector2(1f, 0.5f);
                case DemoPanelPlacement.Center:
                    margin = Vector2.zero;
                    return new Vector2(0.5f, 0.5f);
                default:
                    margin = new Vector2(ScreenMargin, -ScreenMargin);
                    return new Vector2(0f, 1f);
            }
        }

        private static void SetIfNotNull(DemoSceneBuildContext ctx, ModuleDemoSceneInfo info, string field, string value)
        {
            if (value != null)
            {
                ctx.Set(info, field, value);
            }
        }
    }
}
