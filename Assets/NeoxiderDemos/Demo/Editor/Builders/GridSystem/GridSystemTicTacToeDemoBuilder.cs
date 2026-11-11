using System;
using Neo.Demo.GridSystem;
using Neo.GridSystem;
using Neo.GridSystem.TicTacToe;
using Neo.Samples;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Neo.Demo.Editor.Builders
{
    /// <summary>
    ///     Authors GridSystemTicTacToeDemo.unity: the hand-authored GridSystem_TicTacToe board (FieldGenerator +
    ///     FieldDebugDrawer + TicTacToeBoardService + GridSystemTicTacToeBoardView) is configured in place; the frame,
    ///     control card, info card, edit-mode preview and the persistent listeners (Reset button straight to
    ///     TicTacToeBoardService.ResetBoard, service events to GridSystemTicTacToeDemoUI and the log) are saved.
    /// </summary>
    internal static class GridSystemTicTacToeDemoBuilder
    {
        internal const string ScenePath = "Scenes/GridSystem/GridSystemTicTacToeDemo.unity";
        internal const string BoardName = "GridSystem_TicTacToe";

        private const string Scene = "GridSystemTicTacToeDemo.unity";
        private const float CameraSize = 4f;
        private static readonly Vector2 CellSize = new(0.9f, 0.9f);
        private static readonly Color Accent = DemoPalette.Cyan;
        private static readonly Color TileColor = new(0.17f, 0.19f, 0.27f, 1f);

        [DemoSceneBuilder(ScenePath)]
        public static void Build(DemoSceneBuildContext ctx)
        {
            DemoSceneKit.Step(Scene, "retire the pre-kit bootstrap", () =>
            {
                GridSystemDemoStage.RetireBootstrap(ctx, IsLegacyUi);
                DemoSceneKit.UpgradeLegacyInputModules(ctx);
            });

            Board board = DemoSceneKit.Step(Scene, "configure the GridSystem_TicTacToe board", () => ConfigureBoard(ctx));
            ModuleDemoSceneInfo info = DemoSceneKit.Step(Scene, "ensure the info component",
                () => ctx.FindOrCreate<ModuleDemoSceneInfo>("Services", "Demo Info"));
            GridSystemTicTacToeDemoUI controller = DemoSceneKit.Step(Scene, "ensure the demo controller",
                () => ctx.FindOrCreate<GridSystemTicTacToeDemoUI>("Services", "Demo Controller"));

            DemoFrame frame = DemoSceneKit.Step(Scene, "author the frame", () =>
            {
                DemoFrame authored = DemoSceneKit.Frame(ctx,
                    GridSystemDemoStage.FrameOptions("Neo.GridSystem · TicTacToe", Accent, true));
                GridSystemDemoStage.FrameCamera(ctx, authored.Camera, new Vector3(0f, 0f, -10f), CameraSize, Accent);
                return authored;
            });

            Card card = DemoSceneKit.Step(Scene, "author the control card", () => BuildCard(frame.Content));
            DemoSceneKit.Step(Scene, "wire buttons and service events",
                () => Wire(ctx, board, controller, card, frame.Log));
            DemoSceneKit.Step(Scene, "assign serialized references", () =>
            {
                ctx.Set(controller, "_generator", board.Generator);
                ctx.Set(controller, "_board", board.Service);
                ctx.Set(controller, "_debugDrawer", board.Drawer);
                ctx.Set(controller, "_statusText", card.Status);
                ctx.Set(controller, "_rulesValue", card.Rules);
                ctx.Set(controller, "_log", frame.Log);
                ctx.Set(controller, "_botPlaysO", true);
                ctx.Set(controller, "_botDelay", 0.35f);

                ctx.Set(board.View, "_generator", board.Generator);
                ctx.Set(board.View, "_board", board.Service);
                ctx.Set(board.View, "_camera", frame.Camera);
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
            TicTacToeBoardService service = GridSystemDemoStage.FindOrCreateBoard<TicTacToeBoardService>(ctx, BoardName);
            GameObject host = service.gameObject;
            GridSystemDemoStage.PlaceHost(ctx, host, Vector3.zero);
            GridSystemDemoStage.RetireViewLeftovers(ctx, host.transform,
                name => name.StartsWith("Cell_", StringComparison.Ordinal));

            Grid grid = ctx.Ensure<Grid>(host);
            grid.cellSize = Vector3.one;
            grid.cellGap = Vector3.zero;
            grid.cellLayout = GridLayout.CellLayout.Rectangle;
            ctx.MarkModified(grid);

            FieldGenerator generator = ctx.Ensure<FieldGenerator>(host);
            FieldGeneratorConfig config = generator.Config ?? new FieldGeneratorConfig();
            config.Size = new Vector3Int(3, 3, 1);
            config.GridType = GridType.Rectangular;
            config.MovementPreset = GridMovementPreset.FourDirections2D;
            config.ShapeMask = null;
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
            drawer.TextFontSize = 14;
            ctx.MarkModified(drawer);

            ctx.Set(service, "_resetOnStart", true);
            ctx.Set(service, "_winLineLength", 3);

            GridSystemTicTacToeBoardView view = ctx.Ensure<GridSystemTicTacToeBoardView>(host);
            ctx.Set(view, "_tileSprite", DemoSceneAssets.RoundedRect);
            ctx.Set(view, "_cellSize", CellSize);
            ctx.Set(view, "_textScale", 6f);
            ctx.Set(view, "_enabledColor", TileColor);
            ctx.Set(view, "_blockedColor", new Color(0.45f, 0.2f, 0.24f, 1f));
            ctx.Set(view, "_disabledColor", new Color(1f, 1f, 1f, 0.05f));
            ctx.Set(view, "_winColor", new Color(0.18f, 0.55f, 0.43f, 1f));
            ctx.Set(view, "_xColor", DemoPalette.Cyan);
            ctx.Set(view, "_oColor", DemoPalette.Warm);

            GridSystemDemoStage.GenerateForAuthoring(ctx, generator);
            return new Board(generator, drawer, service, view);
        }

        private static Card BuildCard(DemoPanel card)
        {
            card.Section("Round");
            TMP_Text status = card.BigLabel("Player X turn");
            status.enableAutoSizing = true;
            status.fontSizeMin = 20f;
            status.fontSizeMax = 40f;
            status.textWrappingMode = TextWrappingModes.NoWrap;
            TMP_Text rules = GridSystemDemoStage.WideValue(card, "Rules", "3×3 · 3 in a row");
            Button[] round = card.ButtonRow("Reset board", "Random move");
            Toggle bot = card.Toggle("Bot plays O", true);

            card.Section("Board · WinLineLength");
            Button[] sizes = card.ButtonRow("3×3 · 3 in a row", "5×5 · 4 in a row");
            card.Section("Cell rules · FieldGenerator");
            Button[] cells = card.ButtonRow("Block center", "Disable corner");
            card.Note("Blocked or disabled cells cannot be played");
            return new Card(status, rules, round, bot, sizes, cells);
        }

        private static void Wire(DemoSceneBuildContext ctx, Board board, GridSystemTicTacToeDemoUI controller, Card card,
            DemoLogFeed log)
        {
            if (log == null)
            {
                throw new InvalidOperationException("the frame has no log feed.");
            }

            // WHY: the NoCode path - Reset calls the module directly; the round state comes back through its events.
            DemoWiring.AddVoid(card.Round[0].onClick, board.Service.ResetBoard);
            DemoWiring.AddString(card.Round[0].onClick, log.Log, "Reset · TicTacToeBoardService.ResetBoard()");
            DemoWiring.AddVoid(card.Round[1].onClick, controller.MakeRandomMove);
            DemoWiring.AddDynamic(card.Bot.onValueChanged, controller.SetBotPlaysO);
            DemoWiring.AddVoid(card.Sizes[0].onClick, controller.UseClassicBoard);
            DemoWiring.AddVoid(card.Sizes[1].onClick, controller.UseFourInARowBoard);
            DemoWiring.AddVoid(card.Cells[0].onClick, controller.ToggleCenterBlocked);
            DemoWiring.AddVoid(card.Cells[1].onClick, controller.ToggleCornerDisabled);

            TicTacToeBoardService service = board.Service;
            ctx.OwnListeners(service, service.OnPlayerChanged, controller);
            DemoWiring.AddDynamic(service.OnPlayerChanged, controller.HandlePlayerChanged);
            ctx.OwnListeners(service, service.OnWinnerDetected, controller);
            DemoWiring.AddDynamic(service.OnWinnerDetected, controller.HandleWinnerDetected);
            ctx.OwnListeners(service, service.OnDrawDetected, controller);
            DemoWiring.AddVoid(service.OnDrawDetected, controller.HandleDraw);
            ctx.OwnListeners(service, service.OnBoardReset, controller);
            DemoWiring.AddString(service.OnBoardReset, log.Log, "New round · OnBoardReset");
            ctx.MarkModified(service);
        }

        private static void BuildPreview(DemoSceneBuildContext ctx, FieldGenerator generator)
        {
            Transform preview = GridSystemDemoStage.PreviewRoot(ctx);
            Sprite tile = DemoSceneAssets.RoundedRect;
            foreach (FieldCell cell in generator.GetAllCells(true))
            {
                Vector3Int position = cell.Position;
                GridSystemDemoStage.WorldSprite(ctx, preview, "Tile " + position.x + "," + position.y, tile, TileColor,
                    generator.GetCellWorldCenter(position), CellSize, 0);
            }
        }

        private static DemoInfoContent InfoContent(string controllerObject, string boardObject)
        {
            return new DemoInfoContent
            {
                Title = "Neo.GridSystem · TicTacToe",
                Purpose = "TicTacToeBoardService adds turns and n-in-a-row wins on top of any FieldGenerator field. " +
                          "Click a cell to place X; the bot answers as O. Switch to 5×5 with four in a row " +
                          "(WinLineLength) and block cells to change the rules.",
                RuntimeApi = "TicTacToeBoardService.TryMakeMove / ResetBoard / WinLineLength / WinningLine, " +
                             "FieldGenerator.SetWalkable / SetEnabled / GenerateField, OnCellStateChanged for views.",
                SceneWorkflow = "Select " + boardObject + ": FieldGenerator (size, movement preset) and " +
                                "TicTacToeBoardService (Reset On Start, Win Line Length); its events are wired to " +
                                "GridSystemTicTacToeDemoUI in the Inspector.",
                Verification = "Play: three in a row lights the winning line (WinningLine); a full board is a draw; " +
                               "a blocked center cannot be played.",
                Hints = new[]
                {
                    "Reset board -> TicTacToeBoardService.ResetBoard directly (no script): see the button's OnClick.",
                    "OnPlayerChanged / OnWinnerDetected / OnDrawDetected -> GridSystemTicTacToeDemoUI on '" +
                    controllerObject + "'.",
                    "The view redraws single cells from FieldGenerator.OnCellStateChanged, raised by the service."
                }
            };
        }

        private sealed class Board
        {
            public Board(FieldGenerator generator, FieldDebugDrawer drawer, TicTacToeBoardService service,
                GridSystemTicTacToeBoardView view)
            {
                Generator = generator;
                Drawer = drawer;
                Service = service;
                View = view;
            }

            public FieldGenerator Generator { get; }
            public FieldDebugDrawer Drawer { get; }
            public TicTacToeBoardService Service { get; }
            public GridSystemTicTacToeBoardView View { get; }
        }

        private sealed class Card
        {
            public Card(TMP_Text status, TMP_Text rules, Button[] round, Toggle bot, Button[] sizes, Button[] cells)
            {
                Status = status;
                Rules = rules;
                Round = round;
                Bot = bot;
                Sizes = sizes;
                Cells = cells;
            }

            public TMP_Text Status { get; }
            public TMP_Text Rules { get; }
            public Button[] Round { get; }
            public Toggle Bot { get; }
            public Button[] Sizes { get; }
            public Button[] Cells { get; }
        }
    }
}
