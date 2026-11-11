using System;
using System.Collections.Generic;
using Neo.Samples;
using Neo.Tools;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Object = UnityEngine.Object;

namespace Neo.Demo.Editor.Builders
{
    /// <summary>
    ///     Authors the Selector demo into Selector.unity: three <see cref="Selector" /> components with their item
    ///     objects - a skin picker (index mode, random), a level bar (fill mode) and hearts (fill mode, index offset
    ///     -1, loop off) - plus buttons wired straight to the Selector API, NoCode readouts on
    ///     <see cref="Selector.OnSelectionChanged" /> and the info card. The pre-kit hand-authored canvas showed
    ///     the same three configurations in a fixed 800x600 layout; it is retired once its known shape is confirmed.
    /// </summary>
    internal static class SelectorDemoBuilder
    {
        private const string Scene = "Selector.unity";
        private const string LegacyCanvasName = "Canvas";

        private static readonly Color Accent = new(0.49f, 0.36f, 0.94f);

        private static readonly string[] LegacySelectorNames = { "id example (no fill mode)", "bar example (0-5)", "HP" };

        private static readonly string[] SkinNames = { "Ruby", "Amber", "Jade", "Azure", "Violet" };

        private static readonly Color[] SkinColors =
        {
            new(0.93f, 0.30f, 0.40f),
            new(0.98f, 0.72f, 0.30f),
            new(0.26f, 0.85f, 0.64f),
            new(0.25f, 0.62f, 0.98f),
            new(0.66f, 0.45f, 0.98f)
        };

        private static readonly Color HeartColor = new(1f, 0.36f, 0.48f);

        [DemoSceneBuilder("Scenes/Tools/View/Selector.unity")]
        public static void Build(DemoSceneBuildContext ctx)
        {
            string step = "legacy layout";
            try
            {
                RetireLegacyCanvas(ctx);

                step = "services";
                ModuleDemoSceneInfo info = ctx.FindOrCreate<ModuleDemoSceneInfo>("Services", "Demo Info");
                SelectorDemoController controller = ctx.FindOrCreate<SelectorDemoController>("Services", "Demo Controller");

                step = "frame";
                DemoFrame frame = DemoSceneKit.Frame(ctx, new DemoFrameOptions("Neo.Tools · Selector", Accent)
                {
                    ContentWidth = 920f
                });
                DemoPanel card = frame.Content;
                card.Note("Each column is one Selector: it switches its item GameObjects by index.");
                card.Note("Buttons call Selector.Next / Previous / SetRandom / SetFirst / SetLast directly (NoCode).");
                DemoPanel[] columns = card.Columns("Selectors", 3, 16f);

                step = "skin picker";
                Selector skins = SkinPicker(ctx, columns[0], controller, out TMP_Text skinName);

                step = "level bar";
                Selector levelBar = LevelBar(ctx, columns[1], controller);

                step = "hearts";
                Selector hearts = Hearts(ctx, columns[2], controller);

                card.Note("C#: selector.Next(); selector.Set(2); selector.OnSelectionChanged.AddListener(OnPicked);");

                step = "info card";
                frame.InfoCard(info, new DemoInfoContent
                {
                    Title = "Neo.Tools · Selector",
                    Purpose = "Selector activates items by index: one at a time (skins), or 0..index in fill mode " +
                              "(bars, hearts). Index offset -1 lets index 0 mean 'nothing'.",
                    RuntimeApi = "selector.Next / Previous / Set(i) / SetRandom / SetFirst / SetLast; Value, Count, " +
                                 "IndexWithOffset; OnSelectionChanged(int), OnFinished. Pure rules: SelectorModel.",
                    SceneWorkflow = "Select 'Selector · Skins', 'Selector · Level bar' or 'Selector · Hearts' to see " +
                                    "Items, Loop, Fill Mode, Index Offset and the OnSelectionChanged listeners.",
                    Verification = "Enter Play Mode: skins switch (Random too), the bar wraps, hearts stop at 0 and " +
                                   "full; every change updates the Index labels and the log.",
                    Hints = new[]
                    {
                        "Buttons -> Selector methods (On Click lists)",
                        "OnSelectionChanged -> DemoValueReadout.SetInt (no code)",
                        "Hearts: Loop off -> OnFinished at full health"
                    }
                });

                step = "controller references";
                ctx.Set(controller, "_skins", skins);
                ctx.Set(controller, "_levelBar", levelBar);
                ctx.Set(controller, "_hearts", hearts);
                ctx.Set(controller, "_skinNames", SkinNames);
                ctx.Set(controller, "_skinName", skinName);
                ctx.Set(controller, "_log", frame.Log);
            }
            catch (Exception exception)
            {
                throw DemoSceneKit.StepFailed(Scene, step, exception);
            }
        }

        private static Selector SkinPicker(DemoSceneBuildContext ctx, DemoPanel column, SelectorDemoController controller,
            out TMP_Text skinName)
        {
            column.Section("Skin picker · index");
            RectTransform stage = column.Row("Stage · Skins", 130f);
            Image pedestal = DemoSceneKit.Image(ctx, stage, "Pedestal", DemoPalette.Track);
            Center(pedestal.rectTransform, new Vector2(124f, 124f));
            RectTransform holder = ctx.UiChild(stage, "Selector · Skins");
            Center(holder, new Vector2(108f, 108f));

            GameObject[] items = new GameObject[SkinNames.Length];
            for (int i = 0; i < SkinNames.Length; i++)
            {
                Image skin = DemoSceneKit.Image(ctx, holder, "Skin · " + SkinNames[i], SkinColors[i]);
                DemoSceneKit.Stretch(skin.rectTransform);
                TMP_Text label = DemoSceneKit.Text(ctx, skin.transform, "Name", SkinNames[i], 22f, DemoPalette.Ink,
                    TextAlignmentOptions.Center, FontStyles.Bold);
                DemoSceneKit.Stretch(label.rectTransform);
                items[i] = skin.gameObject;
            }

            Selector selector = ConfigureSelector(ctx, holder.gameObject, items, 0, false, 0, false, true, true);
            skinName = column.ValueLabel("Selected", SkinNames[0]);
            DemoValueReadout index = DemoSceneKit.Readout(ctx, column.ValueLabel("Index", "0"), "0");
            DemoWiring.AddDynamic(selector.OnSelectionChanged, index.SetInt);
            DemoWiring.AddDynamic(selector.OnSelectionChanged, controller.OnSkinChanged);

            Button[] step = column.ButtonRow("Prev skin", "Next skin");
            DemoWiring.AddVoid(step[0].onClick, selector.Previous);
            DemoWiring.AddVoid(step[1].onClick, selector.Next);
            DemoWiring.AddVoid(column.Button("Random skin").onClick, selector.SetRandom);
            return selector;
        }

        private static Selector LevelBar(DemoSceneBuildContext ctx, DemoPanel column, SelectorDemoController controller)
        {
            const int segments = 5;
            column.Section("Level bar · fill mode");
            RectTransform stage = column.Row("Stage · Level bar", 130f);
            Vector2 size = new(250f, 40f);
            RectTransform slots = Strip(ctx, stage, "Slots", size, 8f);
            RectTransform holder = Strip(ctx, stage, "Selector · Level bar", size, 8f);

            GameObject[] items = new GameObject[segments];
            for (int i = 0; i < segments; i++)
            {
                Image slot = DemoSceneKit.Image(ctx, slots, "Slot " + (i + 1), DemoPalette.Track);
                Fixed(ctx, slot.gameObject, 43f, 40f);
                float t = i / (float)(segments - 1);
                Color color = t < 0.5f
                    ? Color.Lerp(DemoPalette.Good, DemoPalette.Warm, t * 2f)
                    : Color.Lerp(DemoPalette.Warm, DemoPalette.Danger, (t - 0.5f) * 2f);
                Image segment = DemoSceneKit.Image(ctx, holder, "Segment " + (i + 1), color);
                Fixed(ctx, segment.gameObject, 43f, 40f);
                items[i] = segment.gameObject;
            }

            Selector selector = ConfigureSelector(ctx, holder.gameObject, items, 2, true, 0, false, true, false);
            DemoValueReadout index = DemoSceneKit.Readout(ctx, column.ValueLabel("Index", "2"), "0");
            DemoWiring.AddDynamic(selector.OnSelectionChanged, index.SetInt);
            DemoWiring.AddDynamic(selector.OnSelectionChanged, controller.OnLevelChanged);

            Button[] step = column.ButtonRow("Bar -1", "Bar +1");
            DemoWiring.AddVoid(step[0].onClick, selector.Previous);
            DemoWiring.AddVoid(step[1].onClick, selector.Next);
            Button[] ends = column.ButtonRow("Bar first", "Bar last");
            DemoWiring.AddVoid(ends[0].onClick, selector.SetFirst);
            DemoWiring.AddVoid(ends[1].onClick, selector.SetLast);
            return selector;
        }

        private static Selector Hearts(DemoSceneBuildContext ctx, DemoPanel column, SelectorDemoController controller)
        {
            const int hearts = 5;
            column.Section("Hearts · offset -1");
            RectTransform stage = column.Row("Stage · Hearts", 130f);
            Vector2 size = new(250f, 42f);
            RectTransform slots = Strip(ctx, stage, "Slots", size, 10f);
            RectTransform holder = Strip(ctx, stage, "Selector · Hearts", size, 10f);
            Sprite disc = DemoSceneAssets.Disc;

            GameObject[] items = new GameObject[hearts];
            for (int i = 0; i < hearts; i++)
            {
                Image slot = DemoSceneKit.Image(ctx, slots, "Slot " + (i + 1), DemoPalette.Track, false);
                slot.sprite = disc;
                Fixed(ctx, slot.gameObject, 42f, 42f);
                Image heart = DemoSceneKit.Image(ctx, holder, "Heart " + (i + 1), HeartColor, false);
                heart.sprite = disc;
                Fixed(ctx, heart.gameObject, 42f, 42f);
                items[i] = heart.gameObject;
            }

            // WHY: index 0..5 with offset -1 maps to effective -1..4, so index 0 shows no heart and 5 shows all.
            Selector selector = ConfigureSelector(ctx, holder.gameObject, items, hearts, true, -1, true, false, false);
            DemoValueReadout index = DemoSceneKit.Readout(ctx, column.ValueLabel("Index", hearts.ToString()), "0");
            DemoWiring.AddDynamic(selector.OnSelectionChanged, index.SetInt);
            DemoWiring.AddDynamic(selector.OnSelectionChanged, controller.OnHeartsChanged);
            DemoWiring.AddVoid(selector.OnFinished, controller.OnHeartsFull);

            Button[] step = column.ButtonRow("Damage", "Heal");
            DemoWiring.AddVoid(step[0].onClick, selector.Previous);
            DemoWiring.AddVoid(step[1].onClick, selector.Next);
            DemoWiring.AddVoid(column.Button("Full HP").onClick, selector.SetLast);
            return selector;
        }

        private static Selector ConfigureSelector(DemoSceneBuildContext ctx, GameObject host, GameObject[] items,
            int startIndex, bool fill, int offset, bool allowEmpty, bool loop, bool random)
        {
            Selector selector = ctx.Ensure<Selector>(host);
            ctx.Set(selector, "_setChild", false);
            ctx.Set(selector, "_autoUpdateFromChildren", false);
            ctx.Set(selector, "_items", items);
            ctx.Set(selector, "startOnAwake", true);
            ctx.Set(selector, "_loop", loop);
            ctx.Set(selector, "_fillMode", fill);
            ctx.Set(selector, "_indexOffset", offset);
            ctx.Set(selector, "_allowEmptyEffectiveIndex", allowEmpty);
            ctx.Set(selector, "_useRandomSelection", random);
            ctx.Set(selector, "_controlGameObjectActive", true);
            ctx.Set(selector, "_notifySelectorItemsOnly", false);
            ctx.Set(selector, "_saveEnabled", false);
            ctx.Set(selector, "_currentIndex", startIndex);

            // WHY: author the start state, so the scene shows what Play Mode starts with (the kit re-activates
            // reused children every pass, which would otherwise leave every item visible).
            int effective = startIndex + offset;
            for (int i = 0; i < items.Length; i++)
            {
                bool active = fill ? i <= effective : i == effective;
                if (items[i].activeSelf != active)
                {
                    items[i].SetActive(active);
                }
            }

            return selector;
        }

        private static void RetireLegacyCanvas(DemoSceneBuildContext ctx)
        {
            GameObject legacy = ctx.FindHandAuthoredRoot(LegacyCanvasName);
            if (legacy == null)
            {
                return;
            }

            Selector[] selectors = legacy.GetComponentsInChildren<Selector>(true);
            HashSet<string> expected = new(LegacySelectorNames);
            bool knownShape = legacy.GetComponent<Canvas>() != null && selectors.Length == LegacySelectorNames.Length;
            foreach (Selector selector in selectors)
            {
                knownShape &= expected.Remove(selector.name);
            }

            if (!knownShape)
            {
                ctx.Warn("Hand-authored root '" + LegacyCanvasName + "' no longer matches the pre-kit Selector layout; " +
                         "it was kept. Delete it by hand if the authored frame supersedes it.");
                return;
            }

            ctx.Retire(legacy, "pre-kit fixed 800x600 layout of the same three Selector configurations, now authored " +
                               "in [Demo] Frame");
        }

        private static RectTransform Strip(DemoSceneBuildContext ctx, RectTransform stage, string name, Vector2 size,
            float spacing)
        {
            RectTransform strip = ctx.UiChild(stage, name);
            Center(strip, size);
            HorizontalLayoutGroup layout = ctx.Ensure<HorizontalLayoutGroup>(strip.gameObject);
            layout.padding = new RectOffset(0, 0, 0, 0);
            layout.spacing = spacing;
            layout.childAlignment = TextAnchor.MiddleLeft;
            layout.childControlWidth = false;
            layout.childControlHeight = false;
            layout.childForceExpandWidth = false;
            layout.childForceExpandHeight = false;
            return strip;
        }

        private static void Fixed(DemoSceneBuildContext ctx, GameObject item, float width, float height)
        {
            RectTransform rect = (RectTransform)item.transform;
            rect.sizeDelta = new Vector2(width, height);
            LayoutElement element = ctx.Ensure<LayoutElement>(item);
            element.preferredWidth = width;
            element.preferredHeight = height;
        }

        private static void Center(RectTransform rect, Vector2 size)
        {
            rect.anchorMin = new Vector2(0.5f, 0.5f);
            rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = Vector2.zero;
            rect.sizeDelta = size;
        }
    }
}
