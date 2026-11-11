using System;
using Neo.Demo.GridSystem;
using Neo.GridSystem;
using Neo.GridSystem.Dice;
using Neo.Samples;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Neo.Demo.Editor.Builders
{
    /// <summary>
    ///     Authors GridSystemDiceMergeDemo.unity: the hand-authored GridSystem_DiceMerge board (FieldGenerator +
    ///     DiceBoardService + DiceMergeDemoController + DiceMergeDemoView) is configured in place with the package dice
    ///     art; the frame, the control card with its own log, the tray caption, the info card, the edit-mode board
    ///     preview and every persistent listener are saved in the scene. The board and dice are drawn by the view at
    ///     runtime from the live field.
    /// </summary>
    internal static class GridSystemDiceMergeDemoBuilder
    {
        internal const string ScenePath = "Scenes/GridSystem/GridSystemDiceMergeDemo.unity";
        internal const string BoardName = "GridSystem_DiceMerge";

        /// <summary>Sprites/Dice/cell.png.</summary>
        internal const string CellSpriteGuid = "a2506bb6638763342acd31e0616c9e80";

        /// <summary>Samples Demo/Prefabs/Dice/DiceCell.prefab (GridCellMarker + collider).</summary>
        internal const string CellPrefabGuid = "7ddfb0ce755bf1b44a3cec9666dad698";

        /// <summary>Samples Demo/Prefabs/Dice/Dice.prefab (DiceDieView).</summary>
        internal const string DiePrefabGuid = "e0c80a1a3d1543b1a5782517608dd02f";

        /// <summary>Sprites/Dice/1.png ... 9.png in value order.</summary>
        internal static readonly string[] DiceSpriteGuids =
        {
            "edcd94355bb05544091383a00736b604",
            "5090186394f351248855cb68082aa88e",
            "45eaea899203e124db650fafd0305d03",
            "983c76dc58dfc55499ea1581ef7a5c0f",
            "f08b254e77e53c14f95d7b9eadcffdcc",
            "82f448f8d39e30d409562a48b86fc53b",
            "909125c9d90960d4394b6082feefbc01",
            "bc87da1f616a5b04faad089969f9749e",
            "2f82d1d5b6db1794fbd84f01ff7791fc"
        };

        private const string Scene = "GridSystemDiceMergeDemo.unity";
        private const float CameraSize = 5f;
        private const float CellPitch = 0.82f;
        private const int LogLines = 5;
        private static readonly Vector3 BoardPosition = new(0f, 1f, 0f);
        private static readonly Vector3 TrayOffset = new(0f, -3.55f, 0f);
        private static readonly Color Accent = DemoPalette.Warm;
        private static readonly Color CellColor = new(0.72f, 0.72f, 0.68f, 1f);
        private static readonly Color BackdropColor = new(0.05f, 0.05f, 0.07f, 0.95f);

        [DemoSceneBuilder(ScenePath)]
        public static void Build(DemoSceneBuildContext ctx)
        {
            DemoSceneKit.Step(Scene, "retire the pre-kit bootstrap", () =>
            {
                GridSystemDemoStage.RetireBootstrap(ctx, IsLegacyUi);
                DemoSceneKit.UpgradeLegacyInputModules(ctx);
            });

            Art art = DemoSceneKit.Step(Scene, "load the dice art", LoadArt);
            Board board = DemoSceneKit.Step(Scene, "configure the GridSystem_DiceMerge board", () => ConfigureBoard(ctx, art));
            ModuleDemoSceneInfo info = DemoSceneKit.Step(Scene, "ensure the info component",
                () => ctx.FindOrCreate<ModuleDemoSceneInfo>("Services", "Demo Info"));

            DemoFrame frame = DemoSceneKit.Step(Scene, "author the frame", () =>
            {
                // WHY: the tray sits under the board, so the log moves into the card instead of the bottom edge.
                DemoFrame authored = DemoSceneKit.Frame(ctx,
                    GridSystemDemoStage.FrameOptions("Neo.GridSystem · Dice Merge", Accent, false));
                GridSystemDemoStage.FrameCamera(ctx, authored.Camera, new Vector3(0f, 0f, -10f), CameraSize, Accent);
                return authored;
            });

            Card card = DemoSceneKit.Step(Scene, "author the control card", () => BuildCard(ctx, frame.Content));
            DemoSceneKit.Step(Scene, "wire buttons, slider and merge events", () => Wire(ctx, board, card));
            DemoSceneKit.Step(Scene, "assign serialized references", () =>
            {
                DiceMergeDemoController controller = board.Controller;
                ctx.Set(controller, "_generator", board.Generator);
                ctx.Set(controller, "_diceBoard", board.Dice);
                ctx.Set(controller, "_scoreText", card.Score);
                ctx.Set(controller, "_poolText", card.Pool);
                ctx.Set(controller, "_statusText", card.Status);
                ctx.Set(controller, "_log", card.Log);

                DiceMergeDemoView view = board.View;
                ctx.Set(view, "_generator", board.Generator);
                ctx.Set(view, "_diceBoard", board.Dice);
                ctx.Set(view, "_controller", controller);
                ctx.Set(view, "_camera", frame.Camera);
            });

            DemoSceneKit.Step(Scene, "author the tray caption", () =>
                GridSystemDemoStage.WorldLabel(ctx, ctx.Root(GridSystemDemoStage.WorldRootKey).transform, "Tray Caption",
                    "NEXT PIECE · drag it onto the board · tap to rotate", 2.6f, DemoPalette.Muted,
                    BoardPosition + TrayOffset + new Vector3(CellPitch * 0.5f, -1.8f, 0f), new Vector2(9f, 0.6f), 10));
            DemoSceneKit.Step(Scene, "author the edit-mode board preview", () => BuildPreview(ctx, board, art));
            DemoSceneKit.Step(Scene, "author the info card",
                () => GridSystemDemoStage.InfoCard(ctx, frame, info, InfoContent(board.Generator.name)));
        }

        private static bool IsLegacyUi(string childName)
        {
            return childName == "DiceStatusText" || childName == "DiceScoreText" || childName == "DicePoolText";
        }

        private static bool IsRuntimeViewChild(string childName)
        {
            return childName == "DiceBoardView" || childName == "DiceTrayView" || childName == "DicePlacedPiecesView" ||
                   childName == "DiceDragPreview";
        }

        private static Art LoadArt()
        {
            Sprite cell = GridSystemDemoStage.RequireAsset<Sprite>(CellSpriteGuid, "the dice cell sprite (cell.png)", Scene);
            Sprite[] dice = new Sprite[DiceSpriteGuids.Length];
            for (int i = 0; i < dice.Length; i++)
            {
                dice[i] = GridSystemDemoStage.RequireAsset<Sprite>(DiceSpriteGuids[i], "the dice sprite " + (i + 1) + ".png",
                    Scene);
            }

            GridCellMarker cellPrefab =
                GridSystemDemoStage.RequirePrefabComponent<GridCellMarker>(CellPrefabGuid, "the DiceCell prefab", Scene);
            DiceDieView diePrefab =
                GridSystemDemoStage.RequirePrefabComponent<DiceDieView>(DiePrefabGuid, "the Dice prefab", Scene);
            return new Art(cell, dice, cellPrefab, diePrefab);
        }

        private static Board ConfigureBoard(DemoSceneBuildContext ctx, Art art)
        {
            DiceBoardService dice = GridSystemDemoStage.FindOrCreateBoard<DiceBoardService>(ctx, BoardName);
            GameObject host = dice.gameObject;
            GridSystemDemoStage.PlaceHost(ctx, host, BoardPosition);
            GridSystemDemoStage.RetireViewLeftovers(ctx, host.transform, IsRuntimeViewChild);

            Grid grid = ctx.Ensure<Grid>(host);
            grid.cellSize = new Vector3(CellPitch, CellPitch, 1f);
            grid.cellGap = Vector3.zero;
            grid.cellLayout = GridLayout.CellLayout.Rectangle;
            ctx.MarkModified(grid);

            FieldGenerator generator = ctx.Ensure<FieldGenerator>(host);
            FieldGeneratorConfig config = generator.Config ?? new FieldGeneratorConfig();
            config.Size = new Vector3Int(5, 5, 1);
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

            ctx.Set(dice, "_emptyContentId", -1);
            ctx.Set(dice, "_minMergeGroupSize", 3);
            ctx.Set(dice, "_mergeStep", 1);
            ctx.Set(dice, "_maxContentId", 0);
            ctx.Set(dice, "_requireWalkable", true);

            DiceMergeDemoController controller = ctx.Ensure<DiceMergeDemoController>(host);
            DiceMergeDemoView view = ctx.Ensure<DiceMergeDemoView>(host);
            ctx.Set(view, "_cellSprite", art.Cell);
            ctx.Set(view, "_cellPrefab", art.CellPrefab);
            ctx.Set(view, "_diePrefab", art.DiePrefab);
            ctx.Set(view, "_diceSprites", art.Dice);
            ctx.Set(view, "_cellSize", new Vector2(0.68f, 0.68f));
            ctx.Set(view, "_backdropSprite", DemoSceneAssets.RoundedRect);
            ctx.Set(view, "_backdropColor", BackdropColor);
            ctx.Set(view, "_trayPosition", TrayOffset);
            ctx.Set(view, "_dragWorldOffset", new Vector3(0f, 0.5f, 0f));
            ctx.Set(view, "_snapDragPreviewToGrid", true);
            ctx.Set(view, "_emptyContentId", -1);

            GridSystemDemoStage.GenerateForAuthoring(ctx, generator);
            return new Board(generator, dice, controller, view);
        }

        private static Card BuildCard(DemoSceneBuildContext ctx, DemoPanel card)
        {
            card.Section("Run");
            TMP_Text status = GridSystemDemoStage.StatusLine(card, "Drag the next piece onto the board");
            TMP_Text score = card.ValueLabel("Score", "0");
            TMP_Text pool = GridSystemDemoStage.WideValue(card, "Dice pool", "1, 2, 3, 4, 5");

            card.Section("Try it");
            Button[] actions = card.ButtonRow("Restart", "Rotate", "Hint");
            card.Note("Merge three equal neighbours into a higher die");

            card.Section("Rules · DiceBoardService");
            DemoSliderRow mergeSize = card.SliderRow("Merge group size", 2f, 5f, 3f, true, "0");

            card.Section("Log");
            DemoLogFeed log = GridSystemDemoStage.CardLog(ctx, card, LogLines);
            return new Card(status, score, pool, actions, mergeSize.Slider, log);
        }

        private static void Wire(DemoSceneBuildContext ctx, Board board, Card card)
        {
            DiceMergeDemoController controller = board.Controller;
            DemoWiring.AddVoid(card.Actions[0].onClick, controller.RestartDemo);
            DemoWiring.AddVoid(card.Actions[1].onClick, controller.RotateCurrentPiece);
            DemoWiring.AddVoid(card.Actions[2].onClick, controller.ShowHint);
            DemoWiring.AddDynamic(card.MergeSize.onValueChanged, controller.SetMergeGroupSize);

            // WHY: the NoCode path - the merge event of the module calls the controller through a saved listener.
            ctx.OwnListeners(board.Dice, board.Dice.OnMergesResolved, controller);
            DemoWiring.AddDynamic(board.Dice.OnMergesResolved, controller.HandleMergesResolved);
            ctx.MarkModified(board.Dice);
        }

        private static void BuildPreview(DemoSceneBuildContext ctx, Board board, Art art)
        {
            Transform preview = GridSystemDemoStage.PreviewRoot(ctx);
            FieldGenerator generator = board.Generator;
            Vector3Int size = generator.Config.Size;
            Vector3 first = generator.GetCellWorldCenter(Vector3Int.zero);
            Vector3 last = generator.GetCellWorldCenter(new Vector3Int(size.x - 1, size.y - 1, 0));
            Vector2 backdropSize = new(size.x * CellPitch + 0.28f, size.y * CellPitch + 0.28f);
            GridSystemDemoStage.WorldSprite(ctx, preview, "Backdrop", DemoSceneAssets.RoundedRect, BackdropColor,
                (first + last) * 0.5f + new Vector3(0f, 0f, 0.08f), backdropSize, -1);

            Vector2 cellSize = Scaled(art.Cell, art.CellPrefab.transform.localScale);
            foreach (FieldCell cell in generator.GetAllCells(true))
            {
                Vector3Int position = cell.Position;
                GridSystemDemoStage.WorldSprite(ctx, preview, "Cell " + position.x + "," + position.y, art.Cell, CellColor,
                    generator.GetCellWorldCenter(position), cellSize, 0);
            }

            Vector3 tray = BoardPosition + TrayOffset;
            Vector2 dieSize = Scaled(art.Dice[1], art.DiePrefab.transform.localScale);
            GridSystemDemoStage.WorldSprite(ctx, preview, "Tray die 2", art.Dice[1], Color.white, tray, dieSize, 300);
            GridSystemDemoStage.WorldSprite(ctx, preview, "Tray die 4", art.Dice[3], Color.white,
                tray + new Vector3(CellPitch, 0f, 0f), dieSize, 300);
        }

        private static Vector2 Scaled(Sprite sprite, Vector3 scale)
        {
            Vector2 bounds = sprite.bounds.size;
            return new Vector2(bounds.x * scale.x, bounds.y * scale.y);
        }

        private static DemoInfoContent InfoContent(string boardObject)
        {
            return new DemoInfoContent
            {
                Title = "Neo.GridSystem · Dice Merge",
                Purpose = "A 5×5 dice merge puzzle from GridSystem blocks: DiceBoardService places single dice and " +
                          "pairs on a FieldGenerator field and merges three or more equal neighbours into a higher die. " +
                          "Drag the next piece from the tray onto the board; a quick tap rotates a pair.",
                RuntimeApi = "DiceBoardService.Place / CanPlace / CanPlaceAnywhere / TryFindPlacement / MinMergeGroupSize, " +
                             "DicePieceGenerator.Generate, DicePiece.RotateClockwise.",
                SceneWorkflow = "Select " + boardObject + ": FieldGenerator (5×5), DiceBoardService (merge group, step, " +
                                "cap), DiceMergeDemoController (game loop) and DiceMergeDemoView (art and prefabs).",
                Verification = "Play: place dice, merge three equal neighbours to score; when the next piece fits " +
                               "nowhere (CanPlaceAnywhere) the run is over - press Restart.",
                Hints = new[]
                {
                    "Hint -> DiceBoardService.TryFindPlacement; the slider sets MinMergeGroupSize live.",
                    "DiceBoardService.OnMergesResolved -> DiceMergeDemoController.HandleMergesResolved (Inspector).",
                    "[Demo] Board Preview shows the layout in Edit Mode; the view draws the real board on Play."
                }
            };
        }

        private sealed class Art
        {
            public Art(Sprite cell, Sprite[] dice, GridCellMarker cellPrefab, DiceDieView diePrefab)
            {
                Cell = cell;
                Dice = dice;
                CellPrefab = cellPrefab;
                DiePrefab = diePrefab;
            }

            public Sprite Cell { get; }
            public Sprite[] Dice { get; }
            public GridCellMarker CellPrefab { get; }
            public DiceDieView DiePrefab { get; }
        }

        private sealed class Board
        {
            public Board(FieldGenerator generator, DiceBoardService dice, DiceMergeDemoController controller,
                DiceMergeDemoView view)
            {
                Generator = generator;
                Dice = dice;
                Controller = controller;
                View = view;
            }

            public FieldGenerator Generator { get; }
            public DiceBoardService Dice { get; }
            public DiceMergeDemoController Controller { get; }
            public DiceMergeDemoView View { get; }
        }

        private sealed class Card
        {
            public Card(TMP_Text status, TMP_Text score, TMP_Text pool, Button[] actions, Slider mergeSize, DemoLogFeed log)
            {
                Status = status;
                Score = score;
                Pool = pool;
                Actions = actions;
                MergeSize = mergeSize;
                Log = log;
            }

            public TMP_Text Status { get; }
            public TMP_Text Score { get; }
            public TMP_Text Pool { get; }
            public Button[] Actions { get; }
            public Slider MergeSize { get; }
            public DemoLogFeed Log { get; }
        }
    }
}
