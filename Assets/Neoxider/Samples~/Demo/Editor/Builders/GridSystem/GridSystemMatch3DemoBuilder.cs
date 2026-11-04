using System;
using Neo.Demo.GridSystem;
using Neo.GridSystem;
using Neo.GridSystem.Match3;
using Neo.Samples;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Neo.Demo.Editor.Builders
{
    /// <summary>
    ///     Authors GridSystemMatch3Demo.unity: the hand-authored GridSystem_Match3 board (FieldGenerator +
    ///     FieldDebugDrawer + Match3BoardService + GridSystemMatch3BoardView) is configured in place, and the frame,
    ///     the control card, the info card, the edit-mode board preview and every persistent listener (buttons to
    ///     GridSystemMatch3DemoUI, Match3BoardService / FieldGenerator events to the controller) are saved in the
    ///     scene. Nothing is assembled at Play start.
    /// </summary>
    internal static class GridSystemMatch3DemoBuilder
    {
        internal const string ScenePath = "Scenes/GridSystem/GridSystemMatch3Demo.unity";
        internal const string BoardName = "GridSystem_Match3";

        /// <summary>Resources/GridShapeMask.asset: DisabledCells mask that cuts the four corners of the 8×8 board.</summary>
        internal const string CutCornersMaskGuid = "032086abed0cf29429f9344647d6ed99";

        private const string Scene = "GridSystemMatch3Demo.unity";
        private const float CameraSize = 6.4f;
        private static readonly Vector2 CellSize = new(0.9f, 0.9f);
        private static readonly Color Accent = new(1f, 0.45f, 0.62f, 1f);

        // WHY: same tile colors as GridSystemMatch3BoardView, in a pattern without three in a row.
        private static readonly Color[] PreviewTiles =
        {
            new(0.95f, 0.3f, 0.3f, 1f),
            new(0.3f, 0.9f, 0.4f, 1f),
            new(0.35f, 0.55f, 1f, 1f),
            new(1f, 0.86f, 0.3f, 1f),
            new(0.7f, 0.4f, 0.95f, 1f)
        };

        private static readonly Color DisabledColor = new(1f, 1f, 1f, 0.05f);

        [DemoSceneBuilder(ScenePath)]
        public static void Build(DemoSceneBuildContext ctx)
        {
            DemoSceneKit.Step(Scene, "retire the pre-kit bootstrap", () =>
            {
                GridSystemDemoStage.RetireBootstrap(ctx, IsLegacyUi);
                DemoSceneKit.UpgradeLegacyInputModules(ctx);
            });

            Board board = DemoSceneKit.Step(Scene, "configure the GridSystem_Match3 board", () => ConfigureBoard(ctx));
            ModuleDemoSceneInfo info = DemoSceneKit.Step(Scene, "ensure the info component",
                () => ctx.FindOrCreate<ModuleDemoSceneInfo>("Services", "Demo Info"));
            GridSystemMatch3DemoUI controller = DemoSceneKit.Step(Scene, "ensure the demo controller",
                () => ctx.FindOrCreate<GridSystemMatch3DemoUI>("Services", "Demo Controller"));

            DemoFrame frame = DemoSceneKit.Step(Scene, "author the frame", () =>
            {
                DemoFrame authored = DemoSceneKit.Frame(ctx,
                    GridSystemDemoStage.FrameOptions("Neo.GridSystem · Match3", Accent, true));
                GridSystemDemoStage.FrameCamera(ctx, authored.Camera, new Vector3(0f, 0f, -10f), CameraSize, Accent);
                return authored;
            });

            Card card = DemoSceneKit.Step(Scene, "author the control card", () => BuildCard(frame.Content));
            DemoSceneKit.Step(Scene, "wire buttons and module events", () => Wire(ctx, board, controller, card));
            DemoSceneKit.Step(Scene, "assign serialized references", () =>
            {
                ctx.Set(controller, "_generator", board.Generator);
                ctx.Set(controller, "_match3", board.Match3);
                ctx.Set(controller, "_debugDrawer", board.Drawer);
                ctx.Set(controller, "_statusText", card.Status);
                ctx.Set(controller, "_boardView", board.View);
                ctx.Set(controller, "_cutCornersMask", board.CutCorners);
                ctx.Set(controller, "_boardValue", card.Field);
                ctx.Set(controller, "_clearedValue", card.Cleared);
                ctx.Set(controller, "_pathValue", card.Highlight);
                ctx.Set(controller, "_log", frame.Log);

                ctx.Set(board.View, "_generator", board.Generator);
                ctx.Set(board.View, "_match3", board.Match3);
                ctx.Set(board.View, "_camera", frame.Camera);
                ctx.Set(board.View, "_statusText", card.Status);
            });

            DemoSceneKit.Step(Scene, "author the edit-mode board preview", () => BuildPreview(ctx, board.Generator));
            DemoSceneKit.Step(Scene, "author the info card",
                () => GridSystemDemoStage.InfoCard(ctx, frame, info, InfoContent(controller.name, board.Generator.name)));
        }

        private static bool IsLegacyUi(string childName)
        {
            return childName.StartsWith("Btn_", StringComparison.Ordinal) ||
                   childName.StartsWith("Text_", StringComparison.Ordinal);
        }

        private static Board ConfigureBoard(DemoSceneBuildContext ctx)
        {
            Match3BoardService match3 = GridSystemDemoStage.FindOrCreateBoard<Match3BoardService>(ctx, BoardName);
            GameObject host = match3.gameObject;
            GridSystemDemoStage.PlaceHost(ctx, host, Vector3.zero);
            GridSystemDemoStage.RetireViewLeftovers(ctx, host.transform,
                name => name.StartsWith("Match3Cell_", StringComparison.Ordinal));

            Grid grid = ctx.Ensure<Grid>(host);
            grid.cellSize = Vector3.one;
            grid.cellGap = Vector3.zero;
            grid.cellLayout = GridLayout.CellLayout.Rectangle;
            ctx.MarkModified(grid);

            GridShapeMask cutCorners = GridSystemDemoStage.LoadAsset<GridShapeMask>(CutCornersMaskGuid);
            if (cutCorners == null)
            {
                ctx.Warn(Scene + ": the cut-corners GridShapeMask (guid " + CutCornersMaskGuid +
                         ") is missing; the board starts as a full rectangle.");
            }

            FieldGenerator generator = ctx.Ensure<FieldGenerator>(host);
            FieldGeneratorConfig config = generator.Config ?? new FieldGeneratorConfig();
            config.Size = new Vector3Int(8, 8, 1);
            config.GridType = GridType.Rectangular;
            config.MovementPreset = GridMovementPreset.FourDirections2D;
            config.ShapeMask = cutCorners;
            config.DisabledCells.Clear();
            config.ForcedEnabledCells.Clear();
            config.BlockedCells.Clear();
            config.ForcedWalkableCells.Clear();
            config.PassabilityMode = CellPassabilityMode.WalkableEnabledAndUnoccupied;
            config.Origin2D = GridOrigin2D.Center;
            config.OriginDepth = GridOriginDepth.Center;
            config.OriginOffset = Vector3Int.zero;
            generator.Config = config;
            generator.DebugEnabled = true;
            ctx.MarkModified(generator);

            FieldDebugDrawer drawer = ctx.Ensure<FieldDebugDrawer>(host);
            drawer.DrawCoordinates = true;
            drawer.DrawPath = true;
            drawer.DebugPath.Clear();
            drawer.TextFontSize = 10;
            ctx.MarkModified(drawer);

            ctx.Set(match3, "_minMatchLength", 3);
            ctx.Set(match3, "_autoGenerateOnStart", true);
            ctx.Set(match3, "_avoidStartMatches", true);
            ctx.Set(match3, "_useResolveDelay", true);
            ctx.Set(match3, "_resolveDelaySeconds", 0.2f);

            GridSystemMatch3BoardView view = ctx.Ensure<GridSystemMatch3BoardView>(host);
            ctx.Set(view, "_tileSprite", DemoSceneAssets.RoundedRect);
            ctx.Set(view, "_cellSize", CellSize);
            ctx.Set(view, "_blockedColor", new Color(0.42f, 0.16f, 0.2f, 1f));
            ctx.Set(view, "_disabledColor", DisabledColor);
            ctx.Set(view, "_occupiedColor", new Color(0.42f, 0.36f, 0.22f, 1f));
            ctx.Set(view, "_selectedOutlineColor", Color.white);
            ctx.Set(view, "_highlightColor", DemoPalette.Cyan);
            ctx.Set(view, "_outlineThickness", 0.12f);

            GridSystemDemoStage.GenerateForAuthoring(ctx, generator);
            return new Board(generator, drawer, match3, view, cutCorners);
        }

        private static Card BuildCard(DemoPanel card)
        {
            card.Section("Live state");
            TMP_Text status = GridSystemDemoStage.StatusLine(card, "Click two neighbouring tiles");
            TMP_Text field = GridSystemDemoStage.WideValue(card, "Field", "8×8");
            TMP_Text cleared = card.ValueLabel("Tiles cleared", "0");
            TMP_Text highlight = GridSystemDemoStage.WideValue(card, "Highlight", "-");

            card.Section("Shape · GridShapeMask");
            Button[] shape = card.ButtonRow("Full 8×8", "Cut corners", "Diamond");
            card.Section("Cell state");
            Button[] cells = card.ButtonRow("Block", "Disable", "Occupy");
            card.Section("Pathfinding");
            Button[] paths = card.ButtonRow("Shortest path", "Reach 3 moves");
            card.Section("Match3 rules");
            Button[] rules = card.ButtonRow("Swap valid", "Restart");
            card.Note("Click a tile, then a neighbour to swap");
            return new Card(status, field, cleared, highlight, shape, cells, paths, rules);
        }

        private static void Wire(DemoSceneBuildContext ctx, Board board, GridSystemMatch3DemoUI controller, Card card)
        {
            DemoWiring.AddVoid(card.Shape[0].onClick, controller.GenerateRectBoard);
            DemoWiring.AddVoid(card.Shape[1].onClick, controller.GenerateCutCornersBoard);
            DemoWiring.AddVoid(card.Shape[2].onClick, controller.GenerateDiamondBoard);
            DemoWiring.AddVoid(card.Cells[0].onClick, controller.ToggleRandomBlocked);
            DemoWiring.AddVoid(card.Cells[1].onClick, controller.ToggleRandomDisabled);
            DemoWiring.AddVoid(card.Cells[2].onClick, controller.ToggleRandomOccupied);
            DemoWiring.AddVoid(card.Paths[0].onClick, controller.RunPathDemo);
            DemoWiring.AddVoid(card.Paths[1].onClick, controller.ShowReachableArea);
            DemoWiring.AddVoid(card.Rules[0].onClick, controller.SwapRandom);
            DemoWiring.AddVoid(card.Rules[1].onClick, controller.RestartBoard);

            // WHY: the NoCode path - module events call the controller through listeners saved on the module.
            ctx.OwnListeners(board.Match3, board.Match3.OnMatchesResolved, controller);
            DemoWiring.AddDynamic(board.Match3.OnMatchesResolved, controller.HandleMatchesResolved);
            ctx.OwnListeners(board.Match3, board.Match3.OnBoardShuffled, controller);
            DemoWiring.AddVoid(board.Match3.OnBoardShuffled, controller.HandleBoardShuffled);
            ctx.OwnListeners(board.Generator, board.Generator.OnFieldGenerated, controller);
            DemoWiring.AddVoid(board.Generator.OnFieldGenerated, controller.HandleFieldGenerated);
            ctx.MarkModified(board.Match3);
            ctx.MarkModified(board.Generator);
        }

        private static void BuildPreview(DemoSceneBuildContext ctx, FieldGenerator generator)
        {
            Transform preview = GridSystemDemoStage.PreviewRoot(ctx);
            Sprite tile = DemoSceneAssets.RoundedRect;
            foreach (FieldCell cell in generator.GetAllCells(true))
            {
                Vector3Int position = cell.Position;
                Color color = cell.IsEnabled ? PreviewTiles[(position.x + 2 * position.y) % PreviewTiles.Length] : DisabledColor;
                GridSystemDemoStage.WorldSprite(ctx, preview, "Tile " + position.x + "," + position.y, tile, color,
                    generator.GetCellWorldCenter(position), CellSize, 0);
            }
        }

        private static DemoInfoContent InfoContent(string controllerObject, string boardObject)
        {
            return new DemoInfoContent
            {
                Title = "Neo.GridSystem · Match3",
                Purpose = "A Match3 game assembled from GridSystem blocks: FieldGenerator owns the 8×8 field and its " +
                          "cell states, Match3BoardService adds swap, match and cascade rules, and a replaceable view " +
                          "draws it. Click a tile, then a neighbour to swap.",
                RuntimeApi = "FieldGenerator.GenerateField / SetWalkable / SetEnabled / SetOccupied, " +
                             "GridShapeMask.Create, GridPathfinder.TryFindPath / CollectReachableCells, " +
                             "Match3BoardService.TrySwapAndResolve / TryFindValidSwap / InitializeBoard.",
                SceneWorkflow = "Select " + boardObject + ": FieldGenerator (size, shape mask, movement preset), " +
                                "Match3BoardService (min match, tiles, delay) and GridSystemMatch3BoardView. " +
                                "Their events are wired in the Inspector.",
                Verification = "Play: swap to clear lines; Cut corners / Diamond regenerate the field and the view " +
                               "follows; Block / Disable / Occupy change single cells; the path avoids them.",
                Hints = new[]
                {
                    "Buttons -> GridSystemMatch3DemoUI on '" + controllerObject + "'; each calls one GridSystem API.",
                    "Match3BoardService.OnMatchesResolved -> GridSystemMatch3DemoUI.HandleMatchesResolved (Inspector).",
                    "Cyan outline = cells from GridPathfinder; [Demo] Board Preview shows the layout in Edit Mode only."
                }
            };
        }

        private sealed class Board
        {
            public Board(FieldGenerator generator, FieldDebugDrawer drawer, Match3BoardService match3,
                GridSystemMatch3BoardView view, GridShapeMask cutCorners)
            {
                Generator = generator;
                Drawer = drawer;
                Match3 = match3;
                View = view;
                CutCorners = cutCorners;
            }

            public FieldGenerator Generator { get; }
            public FieldDebugDrawer Drawer { get; }
            public Match3BoardService Match3 { get; }
            public GridSystemMatch3BoardView View { get; }
            public GridShapeMask CutCorners { get; }
        }

        private sealed class Card
        {
            public Card(TMP_Text status, TMP_Text field, TMP_Text cleared, TMP_Text highlight, Button[] shape,
                Button[] cells, Button[] paths, Button[] rules)
            {
                Status = status;
                Field = field;
                Cleared = cleared;
                Highlight = highlight;
                Shape = shape;
                Cells = cells;
                Paths = paths;
                Rules = rules;
            }

            public TMP_Text Status { get; }
            public TMP_Text Field { get; }
            public TMP_Text Cleared { get; }
            public TMP_Text Highlight { get; }
            public Button[] Shape { get; }
            public Button[] Cells { get; }
            public Button[] Paths { get; }
            public Button[] Rules { get; }
        }
    }
}
