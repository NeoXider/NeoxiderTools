using System;
using Neo.Core.Level;
using Neo.Level;
using Neo.Samples;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

namespace Neo.Demo.Editor.Builders
{
    /// <summary>
    ///     Authors the level demo into LevelFlowDemo.unity. Left card: a <see cref="LevelComponent" /> with a linear
    ///     curve asset whose LevelState / XpToNextLevelState drive the labels through persistent listeners (NoCode),
    ///     two configured <see cref="LevelNoCodeAction" />s and ResetProgress as NoCode buttons, and the C# buttons of
    ///     <see cref="LevelDemoController" />. Right card: the hand-authored <see cref="LevelManager" /> gets a finite
    ///     "World 1" and an infinite "Endless" map, a grid of <see cref="LevelButton" />s (lock / next / done visuals
    ///     switched by their own events), <see cref="TextLevel" /> labels and buttons that call SaveLevel, NextLevel,
    ///     Restart and SetMapId directly.
    /// </summary>
    internal static class LevelFlowDemoBuilder
    {
        /// <summary>Curve asset of the demo, relative to the Demo sample root.</summary>
        public const string CurveAsset = "Data/Level/LevelFlowDemoCurve.asset";

        /// <summary>SaveProvider key of the demo LevelComponent profile (kept apart from game profiles).</summary>
        public const string ProfileKey = "Neo.Demo.LevelFlow.Profile";

        /// <summary>LevelManager save key of the demo maps (keys Map_Neo.Demo.LevelFlow_&lt;id&gt;__level).</summary>
        public const string MapSaveKey = "Neo.Demo.LevelFlow";

        public const int WorldLevels = 5;
        public const int XpPerLevel = 100;

        private const string Scene = "LevelFlowDemo.unity";

        private static readonly Color Accent = new(0.49f, 0.36f, 0.94f);
        private static readonly Color CellColor = new(0.20f, 0.21f, 0.30f, 1f);
        private static readonly Color LockColor = new(0.05f, 0.05f, 0.08f, 0.78f);
        private static readonly string[] MapNames = { "World 1", "Endless" };

        [DemoSceneBuilder("Scenes/Level/LevelFlowDemo.unity")]
        public static void Build(DemoSceneBuildContext ctx)
        {
            string step = "services";
            try
            {
                // WHY: the hand-authored 'Level Demo' (LevelManager + info) and 'Demo Controller' win.
                LevelManager levelManager = ctx.FindOrCreate<LevelManager>("Services", "Level Manager");
                ModuleDemoSceneInfo info = ctx.FindOrCreate<ModuleDemoSceneInfo>("Services", "Demo Info");
                LevelDemoController controller = ctx.FindOrCreate<LevelDemoController>("Services", "Demo Controller");

                step = "XP curve asset";
                LevelCurveDefinition curve = DemoSceneAssets.EnsureAsset<LevelCurveDefinition>(CurveAsset,
                    asset => asset.SetLinear(XpPerLevel));

                step = "LevelComponent";
                LevelComponent level = ctx.FindOrCreate<LevelComponent>("Services", "Player Level");
                ConfigureLevel(ctx, level, curve);
                LevelNoCodeAction addXp = Bridge(ctx, "Level NoCode · +25 XP", level, LevelNoCodeActionType.AddXp, 25, 1);
                LevelNoCodeAction setLevel = Bridge(ctx, "Level NoCode · Set level 3", level,
                    LevelNoCodeActionType.SetLevel, 0, 3);

                step = "frame";
                DemoFrame frame = DemoSceneKit.Frame(ctx,
                    new DemoFrameOptions("Neo.Core.Level + Neo.Level", Accent) { ContentWidth = 920f });
                DemoPanel[] columns = frame.Content.Columns("XP | Level flow", 2, 20f);

                step = "XP column";
                XpColumn xp = BuildXpColumn(ctx, columns[0], frame, level, addXp, setLevel, controller);

                step = "level flow column";
                FlowColumn flow = BuildFlowColumn(ctx, columns[1], frame, levelManager, controller);

                step = "LevelManager maps and events";
                ConfigureLevelManager(ctx, levelManager, flow.Grid, flow.Buttons);
                WireLevelManager(levelManager, controller);

                step = "info card";
                frame.InfoCard(info, InfoContent());

                step = "controller references";
                ctx.Set(controller, "_level", level);
                ctx.Set(controller, "_totalXpValue", xp.TotalXp);
                ctx.Set(controller, "_xpFill", xp.Fill);
                ctx.Set(controller, "_levelManager", levelManager);
                ctx.Set(controller, "_mapSaveKey", MapSaveKey);
                ctx.Set(controller, "_mapNames", MapNames);
                ctx.Set(controller, "_mapValue", flow.MapValue);
                ctx.Set(controller, "_completeValue", flow.CompleteValue);
                ctx.Set(controller, "_infiniteMapHint", flow.InfiniteHint);
                ctx.Set(controller, "_log", frame.Log);
            }
            catch (Exception exception)
            {
                throw new InvalidOperationException(
                    Scene + " build failed at step '" + step + "': " + exception.Message, exception);
            }
        }

        private static void ConfigureLevel(DemoSceneBuildContext ctx, LevelComponent level, LevelCurveDefinition curve)
        {
            ctx.Set(level, "_levelCurve", curve);
            ctx.Set(level, "_useXp", true);
            ctx.Set(level, "_startXp", 0);
            ctx.Set(level, "_startLevel", 1);
            ctx.Set(level, "_hasMaxLevel", false);
            ctx.Set(level, "_saveKey", ProfileKey);
            ctx.Set(level, "_loadOnAwake", true);
            ctx.Set(level, "_autoSave", true);
        }

        private static LevelNoCodeAction Bridge(DemoSceneBuildContext ctx, string name, LevelComponent level,
            LevelNoCodeActionType type, int xp, int targetLevel)
        {
            GameObject host = ctx.Child(ctx.Root("Services").transform, name);
            LevelNoCodeAction bridge = ctx.Ensure<LevelNoCodeAction>(host);
            ctx.Set(bridge, "_levelProvider", level);
            ctx.Set(bridge, "_actionType", (int)type);
            ctx.Set(bridge, "_xpAmount", xp);
            ctx.Set(bridge, "_level", targetLevel);
            return bridge;
        }

        private static XpColumn BuildXpColumn(DemoSceneBuildContext ctx, DemoPanel column, DemoFrame frame,
            LevelComponent level, LevelNoCodeAction addXp, LevelNoCodeAction setLevel, LevelDemoController controller)
        {
            XpColumn result = new();
            column.Section("XP · LevelComponent");
            TMP_Text big = column.BigLabel("Level 1");
            DemoValueReadout bigReadout = ctx.Ensure<DemoValueReadout>(big.gameObject);
            bigReadout.Configure(big, "0", "Level ");
            result.Fill = column.Bar("Progress to the next level", Accent);
            result.TotalXp = column.ValueLabel("Total XP", "0");
            TMP_Text toNext = column.ValueLabel("XP to next", XpPerLevel.ToString());
            DemoValueReadout toNextReadout = ctx.Ensure<DemoValueReadout>(toNext.gameObject);
            toNextReadout.Configure(toNext, "0");

            // WHY: NoCode binding - the reactive states push into the labels; no controller code in this path.
            DemoWiring.AddDynamic(level.LevelState.OnChanged, bigReadout.SetInt);
            DemoWiring.AddDynamic(level.XpToNextLevelState.OnChanged, toNextReadout.SetInt);
            DemoWiring.AddDynamic(level.XpState.OnChanged, controller.OnXpChanged);
            DemoWiring.AddDynamic(level.OnLevelUp, controller.OnLevelUp);

            Button[] code = column.ButtonRow("+10 XP", "+50 XP", "Level up");
            DemoWiring.AddInt(code[0].onClick, controller.AddXp, 10);
            DemoWiring.AddInt(code[1].onClick, controller.AddXp, 50);
            DemoWiring.AddVoid(code[2].onClick, controller.LevelUp);
            column.Note("above: C# API · below: NoCode bridges");

            Button[] noCode = column.ButtonRow("+25 XP · NoCode", "Level 3 · NoCode", "Reset · NoCode");
            DemoWiring.AddVoid(noCode[0].onClick, addXp.Execute);
            DemoWiring.AddVoid(noCode[1].onClick, setLevel.Execute);
            DemoWiring.AddString(noCode[2].onClick, frame.Log.Log, "LevelComponent.ResetProgress() (NoCode)");
            DemoWiring.AddVoid(noCode[2].onClick, level.ResetProgress);
            DemoWiring.AddString(addXp.OnSuccess, frame.Log.Log, "LevelNoCodeAction.Execute() -> AddXp(25)");
            DemoWiring.AddString(setLevel.OnSuccess, frame.Log.Log, "LevelNoCodeAction.Execute() -> SetLevel(3)");
            return result;
        }

        private static FlowColumn BuildFlowColumn(DemoSceneBuildContext ctx, DemoPanel column, DemoFrame frame,
            LevelManager levelManager, LevelDemoController controller)
        {
            FlowColumn result = new();
            column.Section("Level flow · LevelManager");
            result.MapValue = column.ValueLabel("Map", MapNames[0] + " · " + WorldLevels + " levels");
            TMP_Text selected = column.ValueLabel("Selected", "Level 1");
            TextLevelLabel(ctx, selected, levelManager, 0, 1, "Level ");
            TMP_Text cleared = column.ValueLabel("Cleared", "0");
            TextLevelLabel(ctx, cleared, levelManager, 1, 0, string.Empty);
            result.CompleteValue = column.ValueLabel("Map.IsComplete", "no · 0 of " + WorldLevels);
            foreach (TMP_Text label in new[] { result.MapValue, selected, cleared, result.CompleteValue })
            {
                label.textWrappingMode = TextWrappingModes.NoWrap;
            }

            BuildLevelGrid(ctx, column, levelManager, result);

            Button[] flow = column.ButtonRow("Complete level", "Next level", "Replay");
            DemoWiring.AddString(flow[0].onClick, frame.Log.Log, "LevelManager.SaveLevel() (NoCode)");
            DemoWiring.AddVoid(flow[0].onClick, levelManager.SaveLevel);
            DemoWiring.AddString(flow[1].onClick, frame.Log.Log, "LevelManager.NextLevel() (NoCode)");
            DemoWiring.AddVoid(flow[1].onClick, levelManager.NextLevel);
            DemoWiring.AddString(flow[2].onClick, frame.Log.Log, "LevelManager.Restart() (NoCode)");
            DemoWiring.AddVoid(flow[2].onClick, levelManager.Restart);

            Button[] maps = column.ButtonRow(MapNames[0], MapNames[1], "Reset map");
            DemoWiring.AddInt(maps[0].onClick, levelManager.SetMapId, 0);
            DemoWiring.AddInt(maps[1].onClick, levelManager.SetMapId, 1);
            DemoWiring.AddVoid(maps[2].onClick, controller.ResetMapProgress);
            column.Note("Tip: Complete level -> Next level -> repeat");
            return result;
        }

        private static void TextLevelLabel(DemoSceneBuildContext ctx, TMP_Text label, LevelManager levelManager, int mode,
            int offset, string prefix)
        {
            TextLevel textLevel = ctx.Ensure<TextLevel>(label.gameObject);
            ctx.Set(textLevel, "text", label);
            ctx.Set(textLevel, "_displayMode", mode);
            ctx.Set(textLevel, "_displayOffset", offset);
            ctx.Set(textLevel, "_levelSource", levelManager);
            ctx.Set(textLevel, "startAdd", prefix);
            ctx.Set(textLevel, "endAdd", string.Empty);
        }

        private static void BuildLevelGrid(DemoSceneBuildContext ctx, DemoPanel column, LevelManager levelManager,
            FlowColumn result)
        {
            RectTransform row = column.Row("Level select", 64f);
            HorizontalLayoutGroup layout = ctx.Ensure<HorizontalLayoutGroup>(row.gameObject);
            layout.padding = new RectOffset(0, 0, 0, 0);
            layout.spacing = 8f;
            layout.childAlignment = TextAnchor.MiddleCenter;
            layout.childControlWidth = true;
            layout.childControlHeight = true;
            layout.childForceExpandWidth = true;
            layout.childForceExpandHeight = true;

            TMP_Text hint = DemoSceneKit.Text(ctx, row, "Infinite map hint", "Infinite map · no level grid, use Next level",
                16f, DemoPalette.Muted, TextAlignmentOptions.Center, FontStyles.Italic);
            LayoutElement hintLayout = ctx.Ensure<LayoutElement>(hint.gameObject);
            hintLayout.ignoreLayout = true;
            DemoSceneKit.Stretch(hint.rectTransform);
            hint.gameObject.SetActive(false);
            result.InfiniteHint = hint.gameObject;

            result.Buttons = new LevelButton[WorldLevels];
            for (int i = 0; i < WorldLevels; i++)
            {
                result.Buttons[i] = LevelCell(ctx, row, i, levelManager);
            }

            result.Grid = row;
        }

        private static LevelButton LevelCell(DemoSceneBuildContext ctx, RectTransform row, int index,
            LevelManager levelManager)
        {
            RectTransform cell = ctx.UiChild(row, "Level " + (index + 1));
            Image body = ctx.Ensure<Image>(cell.gameObject);
            DemoSceneKit.StyleImage(body, CellColor, true, true);
            Button button = ctx.Ensure<Button>(cell.gameObject);
            button.interactable = true;
            button.transition = Selectable.Transition.ColorTint;
            button.targetGraphic = body;

            Image next = DemoSceneKit.Image(ctx, cell, "Next", DemoPalette.WithAlpha(Accent, 0.55f), true);
            DemoSceneKit.Stretch(next.rectTransform);
            TMP_Text number = DemoSceneKit.Text(ctx, cell, "Number", (index + 1).ToString(), 26f, DemoPalette.Text,
                TextAlignmentOptions.Center, FontStyles.Bold);
            DemoSceneKit.Stretch(number.rectTransform);
            Image done = DemoSceneKit.Image(ctx, cell, "Done", DemoPalette.Good, false);
            done.sprite = DemoSceneAssets.Disc;
            done.preserveAspect = true;
            DemoSceneKit.Anchor(done.rectTransform, Vector2.one, Vector2.one, Vector2.one, new Vector2(-6f, -6f),
                new Vector2(14f, 14f));
            Image lockOverlay = DemoSceneKit.Image(ctx, cell, "Lock", LockColor, true);
            DemoSceneKit.Stretch(lockOverlay.rectTransform);
            TMP_Text lockLabel = DemoSceneKit.Text(ctx, lockOverlay.transform, "Label", "LOCKED", 11f, DemoPalette.Muted,
                TextAlignmentOptions.Bottom, FontStyles.Bold);
            lockLabel.characterSpacing = 4f;
            DemoSceneKit.Stretch(lockLabel.rectTransform, 4f);

            LevelButton levelButton = ctx.Ensure<LevelButton>(cell.gameObject);
            ctx.Set(levelButton, "_button", button);
            ctx.Set(levelButton, "_closes", new[] { lockOverlay.gameObject });
            ctx.Set(levelButton, "_opens", Array.Empty<GameObject>());
            ctx.Set(levelButton, "_textLvl", number);
            ctx.Set(levelButton, "_levelManager", levelManager);
            ctx.Set(levelButton, "level", index);
            ctx.Set(levelButton, "activ", index == 0);

            // WHY: NoCode visuals - LevelButton raises one event per state (locked / next to play / cleared).
            DemoWiring.AddBool(levelButton.OnCurrentVisual, next.gameObject.SetActive, true);
            DemoWiring.AddBool(levelButton.OnCurrentVisual, done.gameObject.SetActive, false);
            DemoWiring.AddBool(levelButton.OnEnableVisual, next.gameObject.SetActive, false);
            DemoWiring.AddBool(levelButton.OnEnableVisual, done.gameObject.SetActive, true);
            DemoWiring.AddBool(levelButton.OnDisableVisual, next.gameObject.SetActive, false);
            DemoWiring.AddBool(levelButton.OnDisableVisual, done.gameObject.SetActive, false);

            // WHY: saved preview of a fresh profile (level 1 next, the rest locked); LevelManager.Init repaints it.
            next.gameObject.SetActive(index == 0);
            done.gameObject.SetActive(false);
            lockOverlay.gameObject.SetActive(index != 0);
            return levelButton;
        }

        private static void ConfigureLevelManager(DemoSceneBuildContext ctx, LevelManager levelManager, RectTransform grid,
            LevelButton[] buttons)
        {
            SerializedObject serialized = new(levelManager);
            Require(serialized, "_saveKey").stringValue = MapSaveKey;
            Require(serialized, "_currentLevel").intValue = 0;
            Require(serialized, "_mapId").intValue = 0;
            Require(serialized, "_onAwakeNextLevel").boolValue = true;
            Require(serialized, "_onAwakeNextMap").boolValue = false;
            Require(serialized, "_parentLevel").objectReferenceValue = grid;

            SerializedProperty maps = Require(serialized, "_maps");
            maps.arraySize = MapNames.Length;
            SetMap(maps.GetArrayElementAtIndex(0), false, WorldLevels, 0);
            SetMap(maps.GetArrayElementAtIndex(1), true, 0, 1);

            SerializedProperty levelButtons = Require(serialized, "_lvlBtns");
            levelButtons.arraySize = buttons.Length;
            for (int i = 0; i < buttons.Length; i++)
            {
                levelButtons.GetArrayElementAtIndex(i).objectReferenceValue = buttons[i];
            }

            serialized.ApplyModifiedPropertiesWithoutUndo();
            ctx.MarkModified(levelManager);
        }

        private static void SetMap(SerializedProperty map, bool infinite, int count, int id)
        {
            RequireRelative(map, "isInfinity").boolValue = infinite;
            RequireRelative(map, "countLevels").intValue = count;
            RequireRelative(map, "isLoopLevel").boolValue = false;
            RequireRelative(map, "_level").intValue = 0;
            RequireRelative(map, "idMap").intValue = id;
        }

        private static void WireLevelManager(LevelManager levelManager, LevelDemoController controller)
        {
            levelManager.OnChangeLevel ??= new UnityEngine.Events.UnityEvent<int>();
            levelManager.OnChangeMap ??= new UnityEngine.Events.UnityEvent<int>();
            levelManager.OnChangeMaxLevel ??= new UnityEngine.Events.UnityEvent<int>();
            DemoWiring.AddDynamic(levelManager.OnChangeLevel, controller.OnMapLevelChanged);
            DemoWiring.AddDynamic(levelManager.OnChangeMap, controller.OnMapChanged);
            DemoWiring.AddDynamic(levelManager.OnChangeMaxLevel, controller.OnMapProgressChanged);
        }

        private static SerializedProperty Require(SerializedObject serialized, string field)
        {
            SerializedProperty property = serialized.FindProperty(field);
            if (property == null)
            {
                throw new InvalidOperationException(serialized.targetObject.GetType().Name + " has no serialized field '" +
                                                    field + "'.");
            }

            return property;
        }

        private static SerializedProperty RequireRelative(SerializedProperty parent, string field)
        {
            SerializedProperty property = parent.FindPropertyRelative(field);
            if (property == null)
            {
                throw new InvalidOperationException("Map has no serialized field '" + field + "'.");
            }

            return property;
        }

        private static DemoInfoContent InfoContent()
        {
            return new DemoInfoContent
            {
                Title = "Neo.Core.Level + Neo.Level",
                Purpose = "LevelComponent turns XP into levels through a LevelCurveDefinition asset and saves its " +
                          "profile. LevelManager tracks maps, the selected level and the cleared frontier per map; " +
                          "LevelButtons and TextLevel show it without code.",
                RuntimeApi = "LevelComponent.AddXp / SetLevel / ResetProgress, LevelState / XpState / " +
                             "XpToNextLevelState, OnLevelUp; LevelManager.SaveLevel / NextLevel / SetMapId, Map.IsComplete.",
                SceneWorkflow = "[Demo] Services/Player Level holds the LevelComponent and the two LevelNoCodeActions; " +
                                "'Level Demo' holds LevelManager (World 1: 5 levels, Endless: infinite). Labels, grid " +
                                "and buttons are wired with persistent UnityEvents.",
                Verification = "+XP fills the bar and levels up; Complete level unlocks the next grid cell; after 5 " +
                               "clears Map.IsComplete is YES. Stop and re-enter Play Mode: XP and progress return.",
                Hints = new[]
                {
                    "Player Level -> LevelState.OnChanged -> DemoValueReadout (NoCode label)",
                    "Level 1..5 cells: LevelButton events toggle Lock / Next / Done",
                    "Complete level -> LevelManager.SaveLevel; Endless map has no grid"
                }
            };
        }

        private sealed class XpColumn
        {
            public Image Fill;
            public TMP_Text TotalXp;
        }

        private sealed class FlowColumn
        {
            public TMP_Text MapValue;
            public TMP_Text CompleteValue;
            public GameObject InfiniteHint;
            public RectTransform Grid;
            public LevelButton[] Buttons;
        }
    }
}
