using System;
using System.Collections.Generic;
using Neo.Level;
using Neo.NPC;
using Neo.Shop;
using Neo.Tools;
using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;
using Object = UnityEngine.Object;

namespace Neo.Demo.Editor.Builders
{
    /// <summary>
    ///     Keeps the NeoxiderPages showcase scene (NeoxiderPages/Demo/Scenes/UI.unity) clean and explains it. The
    ///     scene is hand-authored (PM prefab with its UIPage pages, audio and system prefabs); this builder removes a
    ///     test leftover, gives the EventSystem the Input System UI module, moves the persisting managers of the
    ///     --System-- prefab (Money, ScoreManager, LevelManager) to demo-only save keys and authors the
    ///     "[Demo] Pages Info" card: what PM does, buttons that open a popup, go back, close the current page and ask
    ///     GM for a win, and hints.
    /// </summary>
    /// <remarks>
    ///     WHY: the card is plain uGUI + TextMeshPro on a marker-less root with built-in UI sprites, without
    ///     ModuleDemoSceneInfo / DemoLogFeed: the NeoxiderPages sample must import and run without the Demo sample.
    ///     PM compiles into Assembly-CSharp (the Pages sample has no asmdef), so its methods are bound by name.
    /// </remarks>
    internal static class PagesUIBuilder
    {
        internal const string Scene = "Demo/Scenes/UI.unity";
        internal const string InfoRootKey = "Pages Info";
        internal const string CardName = "InfoCard";
        internal const string BodyName = "Body";
        internal const string HideButtonName = "Btn_Hide";
        internal const string ShowButtonName = "Btn_Info";

        /// <summary>Demo-only save key of the --System-- prefab's Money (the library default is "Money").</summary>
        internal const string MoneySaveKey = "NeoDemo.Pages.Money";

        /// <summary>Demo-only save key of the --System-- prefab's ScoreManager (the library default is "BestScore").</summary>
        internal const string BestScoreSaveKey = "NeoDemo.Pages.BestScore";

        /// <summary>Demo-only save key of the --System-- prefab's LevelManager (the library default is "LevelManager").</summary>
        internal const string LevelSaveKey = "NeoDemo.Pages.Level";

        private const string NavigationTestLeftover = "__NpcNavTest__";
        private const string PageManagerTypeName = "Neo.Pages.PM";
        private const int CardSortingOrder = 30000;
        private const float ScreenMargin = 24f;

        /// <summary>Card top below the pages' 150-unit top bar (anchored 25 units under the screen edge).</summary>
        private const float CardTopOffset = 196f;

        // WHY: the page UI is portrait (PM canvas 1080x1920, match width); the card scales with the same rule, so it
        // keeps its place next to the page layout and stays readable on phones and small Game views.
        private const float MatchWidth = 0f;
        private static readonly Vector2 PortraitReference = new(1080f, 1920f);

        private const string Description =
            "<b>PM</b> switches <b>UIPage</b> screens identified by <b>PageId</b> assets: a normal page replaces the " +
            "others, a popup opens on top, Back returns to the previous page.";

        private static readonly Color Accent = new(0.36f, 0.64f, 1f, 1f);
        private static readonly Color CardColor = new(0.07f, 0.08f, 0.12f, 0.93f);
        private static readonly Color TitleColor = new Color32(242, 242, 247, 255);
        private static readonly Color DescriptionColor = new Color32(230, 231, 240, 255);
        private static readonly Color HintColor = new Color32(186, 189, 204, 255);
        private static readonly Color ButtonColor = new(0.36f, 0.64f, 1f, 0.9f);
        private static readonly Color QuietButtonColor = new(1f, 1f, 1f, 0.1f);
        private static readonly Color ButtonLabelColor = new Color32(12, 16, 28, 255);

        private static readonly string[] Hints =
        {
            "<b>Start</b> (Menu page) is a <b>BtnChangePage</b>: GameState Start runs GM.StartGame, then PageGame opens.",
            "<b>Pause popup</b> = PM.ChangePageByName(\"PagePause\"), <b>Back</b> = SwitchToPreviousPage, <b>Close</b> = CloseCurrentPage.",
            "<b>GM Win</b> calls GM.Win(); PM (Integrate With GM) opens PageWin. GM wins only in the Game state: press Start first.",
            "Edit Mode: select <b>PM</b> and use Editor Preview to show any page. Code: PM.I.ChangePage(pageId), UIKit.ShowPage(\"PagePause\")."
        };

        [DemoSceneBuilder(Scene, Sample = DemoSample.NeoxiderPages)]
        public static void Build(DemoSceneBuildContext ctx)
        {
            RemoveNavigationTestLeftover(ctx);
            DemoSceneKit.UpgradeLegacyInputModules(ctx);
            DemoSceneKit.EnsureEventSystem(ctx, DemoInputModule.Auto);
            // WHY: with the library default keys the imported demo reads and overwrites the balance, best score and
            // level progress of the user's own game.
            DemoSaveKeys.UseForAll<Money>(ctx, "_moneySave", MoneySaveKey);
            DemoSaveKeys.UseForAll<ScoreManager>(ctx, "_keySave", BestScoreSaveKey);
            DemoSaveKeys.UseForAll<LevelManager>(ctx, "_saveKey", LevelSaveKey);
            BuildInfoCard(ctx);
        }

        /// <summary>
        ///     An editor test once saved a NavMeshAgent + NpcNavigation probe into this UI scene; with no NavMesh it
        ///     only produces agent warnings in Play Mode. Removed by exact name, component and shape.
        /// </summary>
        private static void RemoveNavigationTestLeftover(DemoSceneBuildContext ctx)
        {
            List<GameObject> leftovers = new();
            foreach (GameObject root in ctx.Scene.GetRootGameObjects())
            {
                if (root.name == NavigationTestLeftover && root.transform.childCount == 0 &&
                    root.GetComponent<NpcNavigation>() != null)
                {
                    leftovers.Add(root);
                }
            }

            foreach (GameObject leftover in leftovers)
            {
                ctx.Retire(leftover, "test leftover: NavMeshAgent + NpcNavigation probe");
            }
        }

        private static void BuildInfoCard(DemoSceneBuildContext ctx)
        {
            GameObject root = ctx.Root(InfoRootKey, true);
            DemoSceneKit.OverlayCanvas(ctx, root, CardSortingOrder, PortraitReference);
            ctx.Ensure<CanvasScaler>(root).matchWidthOrHeight = MatchWidth;

            Image card = PlainImage(ctx, root.transform, CardName, CardColor, true);
            RectTransform cardRect = card.rectTransform;
            cardRect.anchorMin = new Vector2(0f, 1f);
            cardRect.anchorMax = new Vector2(1f, 1f);
            cardRect.pivot = new Vector2(0.5f, 1f);
            cardRect.anchoredPosition = new Vector2(0f, -CardTopOffset);
            cardRect.sizeDelta = new Vector2(-2f * ScreenMargin, 0f);
            DemoSceneKit.Shadow(ctx, card.gameObject, new Color(0f, 0f, 0f, 0.35f), new Vector2(0f, -4f));
            DemoSceneKit.VerticalLayout(ctx, card.gameObject, new RectOffset(30, 30, 24, 28), 14f);
            DemoSceneKit.FitHeight(ctx, card.gameObject);

            RectTransform header = ctx.UiChild(cardRect, "Header");
            HorizontalLayoutGroup headerRow = Row(ctx, header.gameObject, 16f, false);
            headerRow.childAlignment = TextAnchor.MiddleLeft;

            RectTransform titles = ctx.UiChild(header, "Titles");
            DemoSceneKit.VerticalLayout(ctx, titles.gameObject, new RectOffset(0, 0, 0, 0), 2f);
            ctx.Ensure<LayoutElement>(titles.gameObject).flexibleWidth = 1f;
            TMP_Text overline = DemoSceneKit.Text(ctx, titles, "Overline", "NEOXIDER PAGES SAMPLE", 22f, Accent,
                TextAlignmentOptions.Left, FontStyles.Bold);
            overline.characterSpacing = 8f;
            DemoSceneKit.Text(ctx, titles, "Title", "Page navigation with PM", 40f, TitleColor, TextAlignmentOptions.Left,
                FontStyles.Bold);

            Button hide = CardButton(ctx, header, HideButtonName, "Hide", QuietButtonColor, TitleColor, 140f);
            Button show = CardButton(ctx, header, ShowButtonName, "Info", QuietButtonColor, TitleColor, 140f);

            Image divider = PlainImage(ctx, cardRect, "Divider", Accent, false, false);
            LayoutElement dividerSize = ctx.Ensure<LayoutElement>(divider.gameObject);
            dividerSize.minHeight = 3f;
            dividerSize.preferredHeight = 3f;

            RectTransform body = ctx.UiChild(cardRect, BodyName);
            DemoSceneKit.VerticalLayout(ctx, body.gameObject, new RectOffset(0, 0, 0, 0), 14f);
            DemoSceneKit.Text(ctx, body, "Description", Description, 30f, DescriptionColor);
            BuildTryRow(ctx, body);
            TMP_Text hints = DemoSceneKit.Text(ctx, body, "Hints", "- " + string.Join("\n- ", Hints), 26f, HintColor);
            hints.paragraphSpacing = 8f;

            WireCollapse(hide, show, body.gameObject);
        }

        private static void BuildTryRow(DemoSceneBuildContext ctx, RectTransform body)
        {
            RectTransform row = ctx.UiChild(body, "Try");
            Row(ctx, row.gameObject, 12f, true);
            Button pause = CardButton(ctx, row, "Btn_Pause popup", "Pause popup", ButtonColor, ButtonLabelColor, 0f);
            Button win = CardButton(ctx, row, "Btn_GM Win", "GM Win", ButtonColor, ButtonLabelColor, 0f);
            Button back = CardButton(ctx, row, "Btn_Back", "Back", ButtonColor, ButtonLabelColor, 0f);
            Button close = CardButton(ctx, row, "Btn_Close", "Close", ButtonColor, ButtonLabelColor, 0f);

            MonoBehaviour pageManager = FindPageManager(ctx);
            if (pageManager == null)
            {
                ctx.Warn("No hand-authored " + PageManagerTypeName + " found; the page buttons of the info card stay unwired.");
            }
            else
            {
                DemoWiring.AddString(pause.onClick, Bind<UnityAction<string>>(pageManager, "ChangePageByName"), "PagePause");
                DemoWiring.AddBool(back.onClick, Bind<UnityAction<bool>>(pageManager, "SwitchToPreviousPage"), false);
                DemoWiring.AddVoid(close.onClick, Bind<UnityAction>(pageManager, "CloseCurrentPage"));
            }

            GM gm = ctx.Find<GM>();
            if (gm == null)
            {
                ctx.Warn("No hand-authored GM found; the GM Win button of the info card stays unwired.");
            }
            else
            {
                DemoWiring.AddVoid(win.onClick, gm.Win);
            }
        }

        private static void WireCollapse(Button hide, Button show, GameObject body)
        {
            GameObject hideObject = hide.gameObject;
            GameObject showObject = show.gameObject;
            DemoWiring.AddBool(hide.onClick, body.SetActive, false);
            DemoWiring.AddBool(hide.onClick, showObject.SetActive, true);
            DemoWiring.AddBool(hide.onClick, hideObject.SetActive, false);
            DemoWiring.AddBool(show.onClick, body.SetActive, true);
            DemoWiring.AddBool(show.onClick, hideObject.SetActive, true);
            DemoWiring.AddBool(show.onClick, showObject.SetActive, false);

            // WHY: the saved scene starts expanded, as Play Mode does.
            body.SetActive(true);
            hideObject.SetActive(true);
            showObject.SetActive(false);
        }

        private static MonoBehaviour FindPageManager(DemoSceneBuildContext ctx)
        {
            foreach (MonoBehaviour behaviour in ctx.FindAll<MonoBehaviour>())
            {
                if (behaviour != null && behaviour.GetType().FullName == PageManagerTypeName)
                {
                    return behaviour;
                }
            }

            return null;
        }

        private static T Bind<T>(Object target, string method) where T : Delegate
        {
            return (T)Delegate.CreateDelegate(typeof(T), target, method);
        }

        private static HorizontalLayoutGroup Row(DemoSceneBuildContext ctx, GameObject host, float spacing,
            bool expandWidth)
        {
            HorizontalLayoutGroup row = ctx.Ensure<HorizontalLayoutGroup>(host);
            row.padding = new RectOffset(0, 0, 0, 0);
            row.spacing = spacing;
            row.childAlignment = TextAnchor.MiddleCenter;
            row.childControlWidth = true;
            row.childControlHeight = true;
            row.childForceExpandWidth = expandWidth;
            row.childForceExpandHeight = false;
            row.childScaleWidth = false;
            row.childScaleHeight = false;
            row.reverseArrangement = false;
            return row;
        }

        /// <summary>Image child with the built-in UISprite (sliced when <paramref name="rounded" />), never a Demo sample sprite.</summary>
        private static Image PlainImage(DemoSceneBuildContext ctx, Transform parent, string name, Color color, bool rounded,
            bool raycastTarget = true)
        {
            RectTransform rect = ctx.UiChild(parent, name);
            Image image = ctx.Ensure<Image>(rect.gameObject);
            image.sprite = rounded ? DemoSceneAssets.BuiltinStandard : null;
            image.type = rounded ? Image.Type.Sliced : Image.Type.Simple;
            image.fillCenter = true;
            image.pixelsPerUnitMultiplier = 1f;
            image.preserveAspect = false;
            image.color = color;
            image.raycastTarget = raycastTarget;
            return image;
        }

        /// <param name="width">Preferred width; 0 lets the row share its width between the buttons.</param>
        private static Button CardButton(DemoSceneBuildContext ctx, Transform parent, string objectName, string label,
            Color color, Color labelColor, float width)
        {
            Image image = PlainImage(ctx, parent, objectName, color, true);
            Button button = ctx.Ensure<Button>(image.gameObject);
            button.interactable = true;
            button.transition = Selectable.Transition.ColorTint;
            button.targetGraphic = image;
            ColorBlock colors = ColorBlock.defaultColorBlock;
            colors.normalColor = Color.white;
            colors.highlightedColor = new Color(0.92f, 0.95f, 1f, 1f);
            colors.selectedColor = Color.white;
            colors.pressedColor = new Color(0.8f, 0.84f, 0.92f, 1f);
            colors.colorMultiplier = 1f;
            button.colors = colors;

            LayoutElement size = ctx.Ensure<LayoutElement>(image.gameObject);
            size.minHeight = 64f;
            size.preferredHeight = 64f;
            size.preferredWidth = width > 0f ? width : -1f;
            size.flexibleWidth = width > 0f ? 0f : 1f;

            TMP_Text text = DemoSceneKit.Text(ctx, image.rectTransform, "Label", label, 30f, labelColor,
                TextAlignmentOptions.Center, FontStyles.Bold);
            DemoSceneKit.Stretch(text.rectTransform, 6f);
            text.textWrappingMode = TextWrappingModes.NoWrap;
            text.enableAutoSizing = true;
            text.fontSizeMin = 20f;
            text.fontSizeMax = 30f;
            return button;
        }
    }
}
