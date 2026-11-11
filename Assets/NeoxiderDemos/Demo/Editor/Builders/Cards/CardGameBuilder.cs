using System;
using System.Collections.Generic;
using Neo.Cards;
using Neo.Level;
using Neo.Samples;
using Neo.Shop;
using Neo.Tools;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;
using Object = UnityEngine.Object;

namespace Neo.Demo.Editor.Builders
{
    /// <summary>
    ///     Authors the guide of the hand-authored Drunkard card game (Scenes/_ExampleGame/CardGame.unity). The game
    ///     itself - PM pages, CardManager (DrunkardGame), DECK, both hand piles, the board and the dealer tutorial -
    ///     stays hand-authored; this builder only adds the "[Demo] Guide" overlay (module info card + live panel) and
    ///     the persistent listeners that feed the panel from DrunkardGame events.
    /// </summary>
    /// <remarks>
    ///     Listeners this builder adds to hand-authored events always target generated objects, so every pass first
    ///     re-owns those events with ctx.OwnListeners (calls into generated roots or at deleted objects are removed,
    ///     and the runner validates the events) and then adds the current set again. Hand-authored listeners such as
    ///     GM.Win are kept.
    /// </remarks>
    internal static class CardGameBuilder
    {
        internal const string ScenePath = "Scenes/_ExampleGame/CardGame.unity";
        internal const string GuideRootKey = "Guide";
        internal const string LivePanelName = "LivePanel";
        internal const string SameDealLabel = "Same deal every restart";
        internal const string RestartLabel = "Restart deal";
        internal const string MoneySaveKey = "NeoDemo.CardGame.Money";
        internal const string BestScoreSaveKey = "NeoDemo.CardGame.BestScore";
        internal const string LevelSaveKey = "NeoDemo.CardGame.Level";

        private const string SceneName = "CardGame.unity";
        private const string TutorialName = "Tutorial";
        private const float InfoCardWidth = 380f;
        private const float LivePanelWidth = 340f;
        private const int LogLines = 4;
        private const float LogLineHeight = 19f;

        private static readonly Color Accent = DemoPalette.Good;

        [DemoSceneBuilder(ScenePath)]
        public static void Build(DemoSceneBuildContext ctx)
        {
            GameRefs refs = Step("find the hand-authored Drunkard game", () => FindGame(ctx));
            ModuleDemoSceneInfo info = Step("ensure the ModuleDemoSceneInfo service",
                () => ctx.FindOrCreate<ModuleDemoSceneInfo>("Services", "Demo Info"));
            // WHY: the hand-authored EventSystem kept StandaloneInputModule, which throws in Input-System-only projects.
            Step("author the [Demo] Guide canvas and info card", () => DemoSceneKit.InfoCardCanvas(ctx, info,
                InfoContent(refs), GuideRootKey, Accent, true));
            RectTransform guide = (RectTransform)ctx.Root(GuideRootKey, true).transform;
            LivePanel live = Step("author the live panel", () => BuildLivePanel(ctx, guide, refs.Deck));
            Step("wire DrunkardGame events to the live log", () => WireGameEvents(ctx, refs.Game, live.Log));
            Step("wire the seed toggle and the restart button", () => WireControls(refs, live));
            Step("show the live panel when the dealer tutorial ends",
                () => ShowAfterTutorial(ctx, refs.Tutorial, live.Root));
            Step("demo save keys for the --System-- prefab", () =>
            {
                DemoSaveKeys.UseForAll<Money>(ctx, "_moneySave", MoneySaveKey);
                DemoSaveKeys.UseForAll<ScoreManager>(ctx, "_keySave", BestScoreSaveKey);
                DemoSaveKeys.UseForAll<LevelManager>(ctx, "_saveKey", LevelSaveKey);
            });
        }

        private static GameRefs FindGame(DemoSceneBuildContext ctx)
        {
            List<DrunkardGame> games = ctx.FindAll<DrunkardGame>();
            if (games.Count != 1)
            {
                throw new InvalidOperationException("expected exactly one hand-authored DrunkardGame (CardManager), found " +
                                                    games.Count + ". This builder only decorates the hand-authored game; " +
                                                    "restore the scene from version control instead of rebuilding it.");
            }

            DrunkardGame game = games[0];
            DeckComponent deck = ReadReference<DeckComponent>(game, "_deckComponent");
            if (deck == null)
            {
                deck = ctx.Find<DeckComponent>();
            }

            if (deck == null)
            {
                throw new InvalidOperationException("DrunkardGame on '" + DemoSceneBuildContext.PathOf(game.transform) +
                                                    "' has no DeckComponent and the scene has none either.");
            }

            BoardComponent board = ReadReference<BoardComponent>(game, "_initialBoard");
            return new GameRefs(game, deck, game.PlayerHand, game.OpponentHand, board, FindTutorial(ctx));
        }

        private static Selector FindTutorial(DemoSceneBuildContext ctx)
        {
            foreach (Selector selector in ctx.FindAll<Selector>())
            {
                if (selector.name == TutorialName)
                {
                    return selector;
                }
            }

            return null;
        }

        private static DemoInfoContent InfoContent(GameRefs refs)
        {
            List<string> roles = new()
            {
                refs.Game.name + " = DrunkardGame",
                refs.Deck.name + " = DeckComponent"
            };
            if (refs.PlayerHand != null && refs.OpponentHand != null)
            {
                roles.Add(refs.PlayerHand.name + " / " + refs.OpponentHand.name + " = HandComponent");
            }

            if (refs.Board != null)
            {
                roles.Add(refs.Board.name + " = BoardComponent");
            }

            return new DemoInfoContent
            {
                Title = "Neo.Cards · Drunkard",
                Purpose = "A complete card game built only from Neo.Cards components and UnityEvents; the game " +
                          "itself has no demo scripts. Play a few rounds, then select the objects below.",
                RuntimeApi = "DeckComponent.Initialize / DrawCard, HandComponent.AddCardAsync, " +
                             "DrunkardGame.Play / RestartGame.",
                SceneWorkflow = string.Join(", ", roles) + ".",
                Verification = "Tap to play, click through the dealer's tips, then click your pile at the bottom: " +
                               "the higher card takes both, equal ranks start a war.",
                Hints = new[]
                {
                    "Your pile -> DrunkardGame.Play; score labels <- HandComponent.OnCardCountChanged -> SetText.",
                    "Bottom-right panel (after the tips): game events -> DemoLogFeed.Log, fixed shuffle seed + Restart deal."
                },
                Corner = ModuleDemoSceneInfo.Corner.TopLeft,
                PanelWidth = InfoCardWidth,
                StartCollapsed = false
            };
        }

        private static LivePanel BuildLivePanel(DemoSceneBuildContext ctx, RectTransform guide, DeckComponent deck)
        {
            // WHY: the card background is not a raycast target, so clicks around the controls still reach the game.
            DemoPanel panel = DemoSceneKit.CornerPanel(ctx, guide, LivePanelName, DemoPanelPlacement.BottomRight,
                LivePanelWidth, Accent, Vector2.zero, false, new RectOffset(16, 16, 12, 16), 6f);
            panel.Section("Live · DrunkardGame events");
            RectTransform logRow = panel.Row("Log", LogLines * LogLineHeight + 4f);
            TMP_Text logText = DemoSceneKit.Text(ctx, logRow, "LogText", "› events appear here", 14f, DemoPalette.Text,
                TextAlignmentOptions.TopLeft);
            DemoSceneKit.Stretch(logText.rectTransform);
            logText.textWrappingMode = TextWrappingModes.NoWrap;
            logText.overflowMode = TextOverflowModes.Ellipsis;
            DemoLogFeed log = ctx.Ensure<DemoLogFeed>(logRow.gameObject);
            ctx.Set(log, "_text", logText);
            ctx.Set(log, "_maxLines", LogLines);
            ctx.Set(log, "_lineColor", DemoPalette.Text);
            ctx.Set(log, "_readyMessage", "events appear here");

            panel.Section("Try it · seeded shuffle");
            Toggle sameDeal = panel.Toggle(SameDealLabel, deck.UseFixedShuffleSeed);
            Button restart = panel.Button(RestartLabel);
            RectTransform apiRow = panel.Row("Api", 36f);
            TMP_Text api = DemoSceneKit.Text(ctx, apiRow, "Text",
                "Toggle -> DeckComponent.UseFixedShuffleSeed\nButton -> DrunkardGame.RestartGame()", 13f, DemoPalette.Muted,
                TextAlignmentOptions.TopLeft);
            DemoSceneKit.Stretch(api.rectTransform);

            return new LivePanel(panel.Rect.gameObject, log, sameDeal, restart);
        }

        private static void WireGameEvents(DemoSceneBuildContext ctx, DrunkardGame game, DemoLogFeed log)
        {
            WireLog(ctx, game, game.OnGameRestarted, log, "Shuffled + dealt · OnGameRestarted");
            WireLog(ctx, game, game.OnGameStarted, log, "First round · OnGameStarted");
            WireLog(ctx, game, game.OnPlayerWonRound, log, "You win · OnPlayerWonRound");
            WireLog(ctx, game, game.OnOpponentWonRound, log, "Bot wins · OnOpponentWonRound");
            WireLog(ctx, game, game.OnWarStarted, log, "War! Same rank · OnWarStarted");
            WireLog(ctx, game, game.OnWarEnded, log, "War settled · OnWarEnded");
            WireLog(ctx, game, game.OnPlayerWin, log, "You won the game · OnPlayerWin");
            WireLog(ctx, game, game.OnOpponentWin, log, "Bot won the game · OnOpponentWin");
            ctx.MarkModified(game);
        }

        private static void WireLog(DemoSceneBuildContext ctx, DrunkardGame game, UnityEventBase gameEvent,
            DemoLogFeed log, string message)
        {
            ctx.OwnListeners(game, gameEvent);
            DemoWiring.AddString(gameEvent, log.Log, message);
        }

        private static void WireControls(GameRefs refs, LivePanel live)
        {
            DemoWiring.AddSetter(live.SameDeal.onValueChanged, refs.Deck, nameof(DeckComponent.UseFixedShuffleSeed));
            DemoWiring.AddString(live.SameDeal.onValueChanged, live.Log.Log, "Seed option set · now Restart deal");
            DemoWiring.AddVoid(live.Restart.onClick, refs.Game.RestartGame);
        }

        private static void ShowAfterTutorial(DemoSceneBuildContext ctx, Selector tutorial, GameObject panel)
        {
            if (tutorial == null)
            {
                ctx.Note("No hand-authored '" + TutorialName + "' Selector; the live panel is visible from the start.");
                panel.SetActive(true);
                return;
            }

            ctx.OwnListeners(tutorial, tutorial.OnFinished);
            bool finishes = tutorial.enabled && tutorial.gameObject.activeSelf && !ReadBool(tutorial, "_loop", true);
            if (!finishes)
            {
                ctx.Note("'" + TutorialName + "' never raises OnFinished (inactive or looping); the live panel is " +
                         "visible from the start.");
                panel.SetActive(true);
                return;
            }

            // WHY: the dealer tips cover the bottom-right corner; the panel appears once they are dismissed.
            DemoWiring.AddBool(tutorial.OnFinished, panel.SetActive, true);
            panel.SetActive(false);
        }

        private static T ReadReference<T>(Object owner, string field) where T : Object
        {
            SerializedObject serialized = new(owner);
            SerializedProperty property = serialized.FindProperty(field);
            return property != null && property.propertyType == SerializedPropertyType.ObjectReference
                ? property.objectReferenceValue as T
                : null;
        }

        private static bool ReadBool(Object owner, string field, bool fallback)
        {
            SerializedObject serialized = new(owner);
            SerializedProperty property = serialized.FindProperty(field);
            return property != null && property.propertyType == SerializedPropertyType.Boolean
                ? property.boolValue
                : fallback;
        }

        private static void Step(string step, Action action)
        {
            DemoSceneKit.Step(SceneName, step, action);
        }

        private static T Step<T>(string step, Func<T> action)
        {
            return DemoSceneKit.Step(SceneName, step, action);
        }

        private sealed class GameRefs
        {
            public GameRefs(DrunkardGame game, DeckComponent deck, HandComponent playerHand, HandComponent opponentHand,
                BoardComponent board, Selector tutorial)
            {
                Game = game;
                Deck = deck;
                PlayerHand = playerHand;
                OpponentHand = opponentHand;
                Board = board;
                Tutorial = tutorial;
            }

            public DrunkardGame Game { get; }
            public DeckComponent Deck { get; }
            public HandComponent PlayerHand { get; }
            public HandComponent OpponentHand { get; }
            public BoardComponent Board { get; }
            public Selector Tutorial { get; }
        }

        private sealed class LivePanel
        {
            public LivePanel(GameObject root, DemoLogFeed log, Toggle sameDeal, Button restart)
            {
                Root = root;
                Log = log;
                SameDeal = sameDeal;
                Restart = restart;
            }

            public GameObject Root { get; }
            public DemoLogFeed Log { get; }
            public Toggle SameDeal { get; }
            public Button Restart { get; }
        }
    }
}
