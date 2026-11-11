using System;
using System.Collections.Generic;
using System.Globalization;
using Neo.Animations;
using Neo.Samples;
using Neo.Tools;
using Neo.UI;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

namespace Neo.Demo.Editor.Builders
{
    /// <summary>
    ///     Authors the Neo.UI "UI Components" demo into UIComponentsDemo.unity: one card with three columns that shows
    ///     what six small components are for - a <see cref="CategoryBar" /> tab strip that switches a
    ///     <see cref="VariantView" /> page, a second VariantView badge with its own Prev/Next navigation, an
    ///     <see cref="AnchorMove" /> anchor playground, a <see cref="FakeLoad" /> progress bar, two
    ///     <see cref="ButtonShake" /> targets and a <see cref="PausePage" /> overlay (inactive until opened) next to a
    ///     scaled and an unscaled <see cref="TransformAnimator" /> square. Everything is wired with persistent
    ///     UnityEvents straight into the components' public API (NoCode); the scene has no demo controller.
    /// </summary>
    internal static class UIComponentsDemoBuilder
    {
        public const string ScenePath = "Scenes/UI/UIComponentsDemo.unity";

        private const string Scene = "UIComponentsDemo.unity";

        private static readonly Color Accent = new(0.30f, 0.85f, 0.75f);

        private static readonly Tab[] Tabs =
        {
            new("all", "All", false),
            new("weapons", "Weapons", false),
            new("armor", "Armor", false),
            new("locked", "Locked", true)
        };

        private static readonly Page[] Pages =
        {
            new("All", "All items · 6", new Color(0.20f, 0.24f, 0.33f), DemoPalette.Text,
                "• Sword   • Bow   • Helmet\n• Shield   • Potion   • Elixir"),
            new("Weapons", "Weapons · 2", new Color(0.38f, 0.19f, 0.23f), new Color(1f, 0.74f, 0.78f),
                "• Sword  ·  12 damage\n• Bow  ·  8 damage, ranged"),
            new("Armor", "Armor · 2", new Color(0.16f, 0.29f, 0.42f), new Color(0.70f, 0.88f, 1f),
                "• Helmet  ·  +3 defence\n• Shield  ·  +5 defence, blocks")
        };

        private static readonly string[] BadgeTexts = { "COMMON", "RARE", "EPIC" };

        private static readonly Color[] BadgeColors =
        {
            new(0.42f, 0.44f, 0.52f),
            new(0.22f, 0.52f, 0.95f),
            new(0.62f, 0.34f, 0.95f)
        };

        private const string AnchorSliderLabel = "AnchorMove.SetX(float)";

        private static readonly Marker[] Markers =
        {
            new("Top left", 0.12f, 0.78f, LabelSide.Right),
            new("Center", 0.5f, 0.42f, LabelSide.Above),
            new("Bottom right", 0.88f, 0.22f, LabelSide.Left)
        };

        [DemoSceneBuilder(ScenePath)]
        public static void Build(DemoSceneBuildContext ctx)
        {
            string step = "services";
            try
            {
                ModuleDemoSceneInfo info = ctx.FindOrCreate<ModuleDemoSceneInfo>("Services", "Demo Info");

                step = "frame";
                DemoFrame frame = DemoSceneKit.Frame(ctx, new DemoFrameOptions("Neo.UI · UI Components", Accent)
                {
                    Placement = DemoContentPlacement.Right,
                    ContentWidth = 1400f,
                    ContentOffsetY = -6f,
                    HeaderWidth = 1000f,
                    LogWidth = 1000f,
                    LogLines = 4
                });
                DemoLogFeed log = frame.Log;
                DemoPanel[] columns = frame.Content.Columns("Components", 3, 16f);

                step = "CategoryBar + VariantView";
                TabsAndPages(ctx, columns[0], log);

                step = "VariantView badge";
                Badge(ctx, columns[1], log);

                step = "AnchorMove";
                AnchorPlayground(ctx, columns[1]);

                step = "FakeLoad";
                FakeLoad loader = Loader(ctx, columns[2], log);

                step = "ButtonShake";
                Shakes(ctx, columns[2], log);

                step = "PausePage controls";
                columns[2].Section("PausePage · pause while active");
                RectTransform clocks = columns[2].Row("PausePage · Clocks", 44f);
                Clock(ctx, clocks, "Scaled Clock", "Time.deltaTime", 0f, 0.5f, false);
                Clock(ctx, clocks, "Unscaled Clock", "unscaledDeltaTime", 0.5f, 1f, true);
                Button open = columns[2].Button("Open pause page");

                step = "info card";
                frame.InfoCard(info, new DemoInfoContent
                {
                    Title = "Neo.UI · UI components",
                    Purpose = "Six small Neo.UI building blocks, each wired in the Inspector without code. Every " +
                              "column of the card shows one of them doing its job; the lines below say what each " +
                              "one is for.",
                    RuntimeApi = "bar.Select / Next / Prev + SelectionChanged; view.SetCurrentState / NextState + " +
                                 "StateChanged; pausePage.gameObject.SetActive(true / false); fakeLoad.Load / EndLoad; " +
                                 "shake.Shake / StopShake; anchorMove.SetX / SetY / SetAnchor / Apply.",
                    SceneWorkflow = "Every control is a persistent UnityEvent: select a button, the CategoryBar row or " +
                                    "the FakeLoad bar row to see its calls. The pause screen is the inactive " +
                                    "'[Demo] Frame/Pause Page' object.",
                    Verification = "Play: tabs switch the page (Next skips Locked), the bar loads, Pause stops the " +
                                   "scaled square while the unscaled one, the shakes and the loader keep running.",
                    Hints = new[]
                    {
                        "CategoryBar - tab strip that owns the selection, skips disabled tabs and reports index / id",
                        "VariantView - one state index switches colours, texts and objects (tab pages, a badge)",
                        "PausePage - pause screen: while active it holds Time.timeScale (and GM pause / cursor)",
                        "FakeLoad - fake loading progress for splash screens: OnChange 0..1, OnFinisLoad",
                        "ButtonShake - shakes a RectTransform on press or on Shake() (error feedback)",
                        "AnchorMove - snaps a RectTransform's anchors to one x / y point; the slider calls SetX"
                    },
                    Corner = ModuleDemoSceneInfo.Corner.TopLeft,
                    StartCollapsed = false,
                    PanelWidth = 420f
                });

                step = "pause page";
                // WHY: created last so the open pause screen covers every card, the info card included.
                GameObject pausePage = PauseOverlay(ctx, frame, loader, log);
                DemoWiring.AddBool(open.onClick, pausePage.SetActive, true);
                DemoWiring.AddString(open.onClick, log.Log, "PausePage enabled › Time.timeScale = 0");
            }
            catch (Exception exception)
            {
                throw DemoSceneKit.StepFailed(Scene, step, exception);
            }
        }

        private static void TabsAndPages(DemoSceneBuildContext ctx, DemoPanel column, DemoLogFeed log)
        {
            column.Section("CategoryBar · tab strip");
            RectTransform strip = column.Row("CategoryBar · Tabs", 44f);
            HorizontalLayoutGroup group = ctx.Ensure<HorizontalLayoutGroup>(strip.gameObject);
            group.padding = new RectOffset(0, 0, 0, 0);
            group.spacing = 6f;
            group.childAlignment = TextAnchor.MiddleCenter;
            group.childControlWidth = true;
            group.childControlHeight = true;
            group.childForceExpandWidth = true;
            group.childForceExpandHeight = true;

            List<CategoryBarItem> items = new();
            RectTransform marker = null;
            for (int i = 0; i < Tabs.Length; i++)
            {
                CategoryBarItem item = TabItem(ctx, strip, Tabs[i], i == 0);
                items.Add(item);
                if (i == 0)
                {
                    marker = SelectionMarker(ctx, (RectTransform)item.transform);
                }
            }

            CategoryBar bar = ctx.Ensure<CategoryBar>(strip.gameObject);
            ctx.Set(bar, "_categories.Array.size", Tabs.Length);
            for (int i = 0; i < Tabs.Length; i++)
            {
                string entry = "_categories.Array.data[" + i + "].";
                ctx.Set(bar, entry + "Id", Tabs[i].Id);
                ctx.Set(bar, entry + "DisplayName", Tabs[i].Label);
                ctx.Set(bar, entry + "Icon", null);
                ctx.Set(bar, entry + "Disabled", Tabs[i].Disabled);
            }

            ctx.Set(bar, "_startIndex", 0);
            ctx.Set(bar, "_wrapNavigation", true);
            ctx.Set(bar, "_items", items);
            ctx.Set(bar, "_itemPrefab", null);
            ctx.Set(bar, "_itemsRoot", null);
            ctx.Set(bar, "_selectionMarker", marker);
            ctx.Set(bar, "_selectionMarkerOffset", new Vector2(0f, 3f));

            Button[] navigation = column.ButtonRow("Prev tab", "Next tab");
            DemoWiring.AddVoid(navigation[0].onClick, bar.Prev);
            DemoWiring.AddString(navigation[0].onClick, log.Log, "CategoryBar.Prev()");
            DemoWiring.AddVoid(navigation[1].onClick, bar.Next);
            DemoWiring.AddString(navigation[1].onClick, log.Log, "CategoryBar.Next()");
            DemoValueReadout id = column.ValueReadout("OnCategoryIdSelected", "0", Tabs[0].Id);
            column.Note("'Locked' is a disabled entry: clicks, Next and Prev skip it", 40f);

            column.Section("VariantView · one page per tab");
            RectTransform pageRow = column.Row("VariantView · Pages", 120f);
            Image background = DemoSceneKit.Image(ctx, pageRow, "Page Background", Pages[0].Background, true, false);
            DemoSceneKit.Stretch(background.rectTransform);
            TMP_Text title = DemoSceneKit.Text(ctx, background.transform, "Title", Pages[0].Title, 20f,
                Pages[0].TitleColor, TextAlignmentOptions.TopLeft, FontStyles.Bold);
            DemoSceneKit.SetAnchors(title.rectTransform, new Vector2(0f, 1f), Vector2.one, new Vector2(14f, -38f),
                new Vector2(-14f, -8f));
            GameObject[] pages = new GameObject[Pages.Length];
            for (int i = 0; i < Pages.Length; i++)
            {
                TMP_Text body = DemoSceneKit.Text(ctx, background.transform, "Page · " + Pages[i].Name, Pages[i].Body,
                    16f, DemoPalette.Text, TextAlignmentOptions.TopLeft);
                DemoSceneKit.SetAnchors(body.rectTransform, Vector2.zero, Vector2.one, new Vector2(14f, 8f),
                    new Vector2(-14f, -42f));
                pages[i] = body.gameObject;
            }

            Color[] backgrounds = new Color[Pages.Length];
            Color[] titleColors = new Color[Pages.Length];
            string[] titles = new string[Pages.Length];
            for (int i = 0; i < Pages.Length; i++)
            {
                backgrounds[i] = Pages[i].Background;
                titleColors[i] = Pages[i].TitleColor;
                titles[i] = Pages[i].Title;
            }

            VariantView view = ctx.Ensure<VariantView>(pageRow.gameObject);
            ConfigureVariants(ctx, view, background, backgrounds, title, titleColors, titles, pages);
            DemoValueReadout state = column.ValueReadout("VariantView state", "0", "0");

            DemoWiring.AddDynamic(bar.OnCategorySelected, view.SetCurrentState);
            DemoWiring.AddDynamic(bar.OnCategoryIdSelected, id.SetText);
            DemoWiring.AddDynamic(view.OnStateChanged, state.SetInt);
        }

        private static CategoryBarItem TabItem(DemoSceneBuildContext ctx, RectTransform strip, Tab tab, bool selected)
        {
            Image background = DemoSceneKit.Image(ctx, strip, "Tab · " + tab.Label, DemoPalette.Track, true, true);
            Button button = ctx.Ensure<Button>(background.gameObject);
            button.transition = Selectable.Transition.ColorTint;
            button.targetGraphic = background;
            button.interactable = !tab.Disabled;

            Image highlight = DemoSceneKit.Image(ctx, background.transform, "Selected",
                DemoPalette.WithAlpha(Accent, 0.32f), true, false);
            DemoSceneKit.Stretch(highlight.rectTransform);
            highlight.gameObject.SetActive(selected);

            TMP_Text label = DemoSceneKit.Text(ctx, background.transform, "Label", tab.Label, 16f,
                tab.Disabled ? DemoPalette.Muted : DemoPalette.Text, TextAlignmentOptions.Center, FontStyles.Bold);
            DemoSceneKit.Stretch(label.rectTransform, 4f);
            label.textWrappingMode = TextWrappingModes.NoWrap;

            CategoryBarItem item = ctx.Ensure<CategoryBarItem>(background.gameObject);
            ctx.Set(item, "_button", button);
            ctx.Set(item, "_label", label);
            ctx.Set(item, "_icon", null);
            ctx.Set(item, "_selectedVisual", highlight.gameObject);
            return item;
        }

        private static RectTransform SelectionMarker(DemoSceneBuildContext ctx, RectTransform firstItem)
        {
            // WHY: the bar re-parents this shared underline onto the selected item at runtime and sets its
            // anchoredPosition to the marker offset; the saved scene shows it on the start entry.
            Image underline = DemoSceneKit.Image(ctx, firstItem, "Selection Marker", Accent, false, false);
            RectTransform rect = underline.rectTransform;
            rect.anchorMin = new Vector2(0f, 0f);
            rect.anchorMax = new Vector2(1f, 0f);
            rect.pivot = new Vector2(0.5f, 0f);
            rect.sizeDelta = new Vector2(-20f, 4f);
            rect.anchoredPosition = new Vector2(0f, 3f);
            LayoutElement element = ctx.Ensure<LayoutElement>(underline.gameObject);
            element.ignoreLayout = true;
            return rect;
        }

        private static void Badge(DemoSceneBuildContext ctx, DemoPanel column, DemoLogFeed log)
        {
            column.Section("VariantView · Prev / Next state");
            RectTransform row = column.Row("VariantView · Badge", 54f);
            Image badge = DemoSceneKit.Image(ctx, row, "Badge", BadgeColors[0], true, false);
            DemoSceneKit.Anchor(badge.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
                new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(220f, 46f));
            TMP_Text label = DemoSceneKit.Text(ctx, badge.transform, "Label", BadgeTexts[0], 22f, DemoPalette.Text,
                TextAlignmentOptions.Center, FontStyles.Bold);
            DemoSceneKit.Stretch(label.rectTransform);
            label.characterSpacing = 8f;

            Color[] textColors = { DemoPalette.Text, DemoPalette.Text, DemoPalette.Text };
            VariantView view = ctx.Ensure<VariantView>(row.gameObject);
            ConfigureVariants(ctx, view, badge, BadgeColors, label, textColors, BadgeTexts, null);

            Button[] navigation = column.ButtonRow("Prev state", "Next state");
            DemoWiring.AddVoid(navigation[0].onClick, view.PreviousState);
            DemoWiring.AddString(navigation[0].onClick, log.Log, "VariantView.PreviousState()");
            DemoWiring.AddVoid(navigation[1].onClick, view.NextState);
            DemoWiring.AddString(navigation[1].onClick, log.Log, "VariantView.NextState()");
            DemoValueReadout state = column.ValueReadout("OnStateChanged", "0", "0");
            DemoWiring.AddDynamic(view.OnStateChanged, state.SetInt);
            column.Note("Navigation stops at both ends; it never wraps");
        }

        private static void AnchorPlayground(DemoSceneBuildContext ctx, DemoPanel column)
        {
            column.Section("AnchorMove · anchor helper");
            RectTransform row = column.Row("AnchorMove · Playground", 136f);
            Image field = DemoSceneKit.Image(ctx, row, "Field", DemoPalette.Track, true, false);
            DemoSceneKit.Stretch(field.rectTransform);
            AnchorMove center = null;
            foreach (Marker marker in Markers)
            {
                AnchorMove move = AnchorMarker(ctx, field.rectTransform, marker);
                if (marker.Name == "Center")
                {
                    center = move;
                }
            }

            DemoSliderRow slider = column.SliderRow(AnchorSliderLabel, 0f, 1f, center.x);
            DemoWiring.AddDynamic(slider.Slider.onValueChanged, center.SetX);
            column.Note("Edit Mode: drag x / y of a marker's AnchorMove; Play: the slider moves the centre one", 40f);
        }

        private static AnchorMove AnchorMarker(DemoSceneBuildContext ctx, RectTransform field, Marker marker)
        {
            Image dot = DemoSceneKit.Image(ctx, field, "Marker · " + marker.Name, Accent, false, false);
            dot.sprite = DemoSceneAssets.Disc;
            RectTransform rect = dot.rectTransform;
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.sizeDelta = new Vector2(22f, 22f);
            AnchorMove move = ctx.Ensure<AnchorMove>(dot.gameObject);
            move.x = marker.X;
            move.y = marker.Y;
            move.Apply();
            ctx.MarkModified(move);

            string text = (marker.Name == "Center" ? "x = slider" : "x " + Number(marker.X)) + " · y " +
                          Number(marker.Y);
            TextAlignmentOptions alignment = marker.Side == LabelSide.Right ? TextAlignmentOptions.Left
                : marker.Side == LabelSide.Left ? TextAlignmentOptions.Right
                : TextAlignmentOptions.Center;
            TMP_Text label = DemoSceneKit.Text(ctx, rect, "Label", text, 14f, DemoPalette.Text, alignment,
                FontStyles.Bold);
            label.textWrappingMode = TextWrappingModes.NoWrap;
            switch (marker.Side)
            {
                case LabelSide.Right:
                    DemoSceneKit.Anchor(label.rectTransform, new Vector2(1f, 0.5f), new Vector2(1f, 0.5f),
                        new Vector2(0f, 0.5f), new Vector2(8f, 0f), new Vector2(140f, 20f));
                    break;
                case LabelSide.Left:
                    DemoSceneKit.Anchor(label.rectTransform, new Vector2(0f, 0.5f), new Vector2(0f, 0.5f),
                        new Vector2(1f, 0.5f), new Vector2(-8f, 0f), new Vector2(140f, 20f));
                    break;
                default:
                    DemoSceneKit.Anchor(label.rectTransform, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f),
                        new Vector2(0.5f, 0f), new Vector2(0f, 4f), new Vector2(140f, 20f));
                    break;
            }

            return move;
        }

        private static FakeLoad Loader(DemoSceneBuildContext ctx, DemoPanel column, DemoLogFeed log)
        {
            column.Section("FakeLoad · fake loading progress");
            DemoBarRow bar = column.BarRow("OnChange › Image.fillAmount", Accent, 0f, true);
            // WHY: the loader sits on the bar row it drives, so selecting the row shows its events.
            FakeLoad loader = ctx.Ensure<FakeLoad>(bar.Row.gameObject);
            ctx.Set(loader, "_loadOnAwake", true);
            ctx.Set(loader, "timeLoad", new Vector2(2f, 3f));
            ctx.Set(loader, "isLoadOne", false);
            ctx.Set(loader, "_useUnscaledTime", true);

            TMP_Text percent = column.ValueLabel("OnChangePercent", "0%");
            DemoValueReadout percentReadout = DemoSceneKit.Readout(ctx, percent, "0", string.Empty, "%");
            TMP_Text status = column.ValueLabel("OnStart / OnFinisLoad", "starts on Play");
            DemoValueReadout statusReadout = DemoSceneKit.Readout(ctx, status, "0");
            Button[] buttons = column.ButtonRow("Load()", "EndLoad()");

            DemoWiring.AddSetter(loader.OnChange, bar.Fill, nameof(Image.fillAmount));
            DemoWiring.AddDynamic(loader.OnChangePercent, percentReadout.SetInt);
            DemoWiring.AddString(loader.OnStart, statusReadout.SetText, "loading...");
            DemoWiring.AddString(loader.OnStart, log.Log, "FakeLoad.OnStart");
            DemoWiring.AddString(loader.OnFinisLoad, statusReadout.SetText, "finished");
            DemoWiring.AddString(loader.OnFinisLoad, log.Log, "FakeLoad.OnFinisLoad");
            DemoWiring.AddVoid(buttons[0].onClick, loader.Load);
            DemoWiring.AddVoid(buttons[1].onClick, loader.EndLoad);
            DemoWiring.AddString(buttons[1].onClick, log.Log, "FakeLoad.EndLoad() - skip to 100%");
            return loader;
        }

        private static void Shakes(DemoSceneBuildContext ctx, DemoPanel column, DemoLogFeed log)
        {
            column.Section("ButtonShake · shake a RectTransform");
            RectTransform row = column.Row("ButtonShake · Targets", 52f);
            Button hold = DemoSceneKit.AccentButton(ctx, row, "Hold to shake", Accent);
            DemoSceneKit.SetAnchors((RectTransform)hold.transform, Vector2.zero, new Vector2(0.48f, 1f),
                new Vector2(0f, 2f), new Vector2(0f, -2f));
            ConfigureShake(ctx, hold.gameObject, 0f, 4f);
            DemoWiring.AddString(hold.onClick, log.Log, "ButtonShake: pointer down shakes, release stops");

            Image pin = DemoSceneKit.Image(ctx, row, "PIN Box", DemoPalette.WithAlpha(DemoPalette.Danger, 0.22f), true,
                false);
            DemoSceneKit.SetAnchors(pin.rectTransform, new Vector2(0.52f, 0f), Vector2.one, new Vector2(0f, 2f),
                new Vector2(0f, -2f));
            TMP_Text pinLabel = DemoSceneKit.Text(ctx, pin.transform, "Label", "Wrong PIN", 18f, DemoPalette.Danger,
                TextAlignmentOptions.Center, FontStyles.Bold);
            DemoSceneKit.Stretch(pinLabel.rectTransform);
            ButtonShake pinShake = ConfigureShake(ctx, pin.gameObject, 0.45f, 10f);

            Button[] buttons = column.ButtonRow("Shake()", "StopShake()");
            DemoWiring.AddVoid(buttons[0].onClick, pinShake.Shake);
            DemoWiring.AddString(buttons[0].onClick, log.Log, "ButtonShake.Shake() - 0.45 s on unscaled time");
            DemoWiring.AddVoid(buttons[1].onClick, pinShake.StopShake);
        }

        private static ButtonShake ConfigureShake(DemoSceneBuildContext ctx, GameObject host, float duration,
            float magnitude)
        {
            ButtonShake shake = ctx.Ensure<ButtonShake>(host);
            ctx.Set(shake, "_rectTransform", host.transform);
            ctx.Set(shake, "_shakeDuration", duration);
            ctx.Set(shake, "_shakeMagnitude", magnitude);
            ctx.Set(shake, "_enableShake", true);
            ctx.Set(shake, "_shakeOnStart", false);
            ctx.Set(shake, "_shakeOnEnd", false);
            ctx.Set(shake, "_useUnscaledTime", true);
            return shake;
        }

        private static void Clock(DemoSceneBuildContext ctx, RectTransform row, string name, string caption,
            float from, float to, bool unscaled)
        {
            RectTransform half = ctx.UiChild(row, name);
            DemoSceneKit.SetAnchors(half, new Vector2(from, 0f), new Vector2(to, 1f), Vector2.zero, Vector2.zero);
            Image square = DemoSceneKit.Image(ctx, half, "Square", unscaled ? DemoPalette.Good : DemoPalette.Warm, false,
                false);
            DemoSceneKit.Anchor(square.rectTransform, new Vector2(0f, 0.5f), new Vector2(0f, 0.5f),
                new Vector2(0.5f, 0.5f), new Vector2(22f, 0f), new Vector2(26f, 26f));
            square.rectTransform.localRotation = Quaternion.identity;
            square.rectTransform.localScale = Vector3.one;
            TMP_Text label = DemoSceneKit.Text(ctx, half, "Caption", caption, 15f, DemoPalette.Muted,
                TextAlignmentOptions.Left, FontStyles.Bold);
            DemoSceneKit.SetAnchors(label.rectTransform, Vector2.zero, Vector2.one, new Vector2(48f, 0f), Vector2.zero);
            label.textWrappingMode = TextWrappingModes.NoWrap;

            TransformAnimator spin = ctx.Ensure<TransformAnimator>(square.gameObject);
            ctx.Set(spin, "_target", null);
            ctx.Set(spin, "_playOnEnable", true);
            ctx.Set(spin, "_randomizeStartTime", false);
            ctx.Set(spin, "_useUnscaledTime", unscaled);
            ctx.Set(spin, "_settings.RotateEnabled", true);
            ctx.Set(spin, "_settings.RotationSpeed", new Vector3(0f, 0f, -180f));
            ctx.Set(spin, "_settings.FloatEnabled", false);
            ctx.Set(spin, "_settings.ScaleEnabled", false);
            ctx.Set(spin, "_settings.ShakeEnabled", false);
        }

        private static GameObject PauseOverlay(DemoSceneBuildContext ctx, DemoFrame frame, FakeLoad loader,
            DemoLogFeed log)
        {
            RectTransform layer = frame.Layer("Pause Page");
            GameObject overlay = layer.gameObject;
            Image dim = ctx.Ensure<Image>(overlay);
            DemoSceneKit.StyleImage(dim, DemoPalette.WithAlpha(DemoPalette.Ink, 0.62f), false, true);

            PausePage page = ctx.Ensure<PausePage>(overlay);
            ctx.Set(page, "_useTimeScale", true);
            ctx.Set(page, "_timeScaleOnPause", 0f);
            // WHY: this scene has no GM; a game would leave Send Pause on so GM pauses with the screen.
            ctx.Set(page, "_sendPause", false);
            ctx.Set(page, "_controlCursor", false);

            DemoPanel card = DemoSceneKit.CornerPanel(ctx, layer, "Pause Card", DemoPanelPlacement.Center, 620f, Accent);
            card.Section("PausePage · active");
            card.BigLabel("Paused");
            card.Paragraph("Time.timeScale is 0 while this page is active. The scaled square stopped; the " +
                           "unscaled one, ButtonShake and FakeLoad still run.", 96f);
            ButtonShake cardShake = ConfigureShake(ctx, card.Rect.gameObject, 0.4f, 8f);
            Button[] tools = card.ButtonRow("Shake this card", "FakeLoad.Load()");
            DemoWiring.AddVoid(tools[0].onClick, cardShake.Shake);
            DemoWiring.AddString(tools[0].onClick, log.Log, "ButtonShake.Shake() while paused - ends on unscaled time");
            DemoWiring.AddVoid(tools[1].onClick, loader.Load);
            DemoWiring.AddString(tools[1].onClick, log.Log, "FakeLoad.Load() while paused - unscaled progress");
            Button resume = card.Button("Resume · close the page");
            DemoWiring.AddBool(resume.onClick, overlay.SetActive, false);
            DemoWiring.AddString(resume.onClick, log.Log, "PausePage disabled › Time.timeScale restored");

            // WHY: PausePage pauses while its object is active, so the saved screen starts closed.
            overlay.SetActive(false);
            return overlay;
        }

        private static void ConfigureVariants(DemoSceneBuildContext ctx, VariantView view, Image image, Color[] colors,
            TMP_Text text, Color[] textColors, string[] texts, GameObject[] objects)
        {
            // WHY: state 0 is applied before the serialized arrays change, so VariantView.OnValidate (which applies the
            // current state, toggling objects) has nothing left to switch.
            image.color = colors[0];
            text.color = textColors[0];
            text.text = texts[0];
            if (objects != null)
            {
                for (int i = 0; i < objects.Length; i++)
                {
                    objects[i].SetActive(i == 0);
                }
            }

            // WHY: one SerializedObject write configures every variant group at once, so validation never sees
            // half-sized arrays.
            SerializedObject serialized = new(view);
            serialized.FindProperty("_imageVariants").arraySize = 0;

            SerializedProperty imageColors = serialized.FindProperty("_imageColors");
            imageColors.arraySize = 1;
            SerializedProperty imageColor = imageColors.GetArrayElementAtIndex(0);
            imageColor.FindPropertyRelative("image").objectReferenceValue = image;
            WriteColors(imageColor.FindPropertyRelative("colors"), colors);

            SerializedProperty textVariants = serialized.FindProperty("_textColorVariants");
            textVariants.arraySize = 1;
            SerializedProperty textVariant = textVariants.GetArrayElementAtIndex(0);
            textVariant.FindPropertyRelative("tmp").objectReferenceValue = text;
            textVariant.FindPropertyRelative("use_text").boolValue = true;
            WriteColors(textVariant.FindPropertyRelative("colors"), textColors);
            SerializedProperty textValues = textVariant.FindPropertyRelative("texts");
            textValues.arraySize = texts.Length;
            for (int i = 0; i < texts.Length; i++)
            {
                textValues.GetArrayElementAtIndex(i).stringValue = texts[i];
            }

            SerializedProperty objectVariants = serialized.FindProperty("_objectVariants");
            objectVariants.arraySize = objects != null ? 1 : 0;
            if (objects != null)
            {
                SerializedProperty objectList = objectVariants.GetArrayElementAtIndex(0).FindPropertyRelative("objects");
                objectList.arraySize = objects.Length;
                for (int i = 0; i < objects.Length; i++)
                {
                    objectList.GetArrayElementAtIndex(i).objectReferenceValue = objects[i];
                }
            }

            serialized.FindProperty("_currentStateIndex").intValue = 0;
            serialized.FindProperty("_isBuildPhase").boolValue = false;
            serialized.FindProperty("_maxStates").intValue = colors.Length;
            serialized.ApplyModifiedPropertiesWithoutUndo();
            ctx.MarkModified(view);
        }

        private static void WriteColors(SerializedProperty property, Color[] colors)
        {
            property.arraySize = colors.Length;
            for (int i = 0; i < colors.Length; i++)
            {
                property.GetArrayElementAtIndex(i).colorValue = colors[i];
            }
        }

        private static string Number(float value)
        {
            return value.ToString("0.00", CultureInfo.InvariantCulture);
        }

        private enum LabelSide
        {
            Right,
            Left,
            Above
        }

        private sealed class Tab
        {
            public Tab(string id, string label, bool disabled)
            {
                Id = id;
                Label = label;
                Disabled = disabled;
            }

            public string Id { get; }
            public string Label { get; }
            public bool Disabled { get; }
        }

        private sealed class Page
        {
            public Page(string name, string title, Color background, Color titleColor, string body)
            {
                Name = name;
                Title = title;
                Background = background;
                TitleColor = titleColor;
                Body = body;
            }

            public string Name { get; }
            public string Title { get; }
            public Color Background { get; }
            public Color TitleColor { get; }
            public string Body { get; }
        }

        private sealed class Marker
        {
            public Marker(string name, float x, float y, LabelSide side)
            {
                Name = name;
                X = x;
                Y = y;
                Side = side;
            }

            public string Name { get; }
            public float X { get; }
            public float Y { get; }
            public LabelSide Side { get; }
        }
    }
}
