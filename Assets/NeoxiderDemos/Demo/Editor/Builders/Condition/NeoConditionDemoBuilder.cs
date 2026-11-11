using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;
using Neo.Condition;
using Neo.Demo.Condition;
using Neo.NoCode;
using Neo.Samples;
using Neo.Tools;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using ConditionValueType = Neo.Condition.ValueType;
using Object = UnityEngine.Object;

#pragma warning disable CS0618 // WHY: the shipped scene's player is the legacy Neo.Tools.Health; the builder keeps that hand-authored object.

namespace Neo.Demo.Editor.Builders
{
    /// <summary>
    ///     Authors the NeoCondition demo into NeoConditionDemo.unity around its hand-authored gameplay: 'Health' with the
    ///     CheckDead / CheckLowHP rules and 'Score' with CheckWin. The pre-kit Canvas and the editor-only setup object are
    ///     retired; the frame shows HP and score through NoCodeFormattedText + SetProgress, buttons call Health and
    ///     ScoreManager directly, and every rule lights its lamp and toggles its banner through NeoCondition.OnResult
    ///     persistent listeners (no code). <see cref="ConditionDemoUI" /> adds the C# path (NeoCondition.Evaluate) and
    ///     the pause switch that shows the rules still checking at Time.timeScale 0.
    /// </summary>
    internal static class NeoConditionDemoBuilder
    {
        internal const string ScenePath = "Scenes/Condition/NeoConditionDemo.unity";
        internal const string DamageLabel = "Take damage 25";
        internal const string HealLabel = "Heal 10";
        internal const string ScoreLabel = "Add score 25";
        internal const string ResetLabel = "Reset";
        internal const string EvaluateLabel = "Evaluate in code";
        internal const int Damage = 25;
        internal const int HealAmount = 10;
        internal const int Points = 25;

        private const string SceneFile = "NeoConditionDemo.unity";
        private const string HealthName = "Health";
        private const string ScoreName = "Score";
        private const string DeadName = "CheckDead";
        private const string LowName = "CheckLowHP";
        private const string WinName = "CheckWin";
        internal const string BestScoreSaveKey = "NeoDemo.Condition.BestScore";

        private static readonly Color Accent = new(0.36f, 0.86f, 0.62f);

        [DemoSceneBuilder(ScenePath)]
        public static void Build(DemoSceneBuildContext ctx)
        {
            Gameplay game = Step("find the hand-authored Health, Score and NeoCondition rules", () => FindGameplay(ctx));
            ModuleDemoSceneInfo info = Step("ensure the info service",
                () => ctx.FindOrCreate<ModuleDemoSceneInfo>("Services", "Demo Info"));
            ConditionDemoUI ui = Step("ensure the demo controller",
                () => ctx.FindOrCreate<ConditionDemoUI>("Services", "Demo Controller"));
            Step("retire the pre-kit layout", () => RetireLegacyLayout(ctx));
            Step("upgrade the EventSystem input module", () => DemoSceneKit.UpgradeLegacyInputModules(ctx));
            Step("re-own the rule listeners and pause support", () => PrepareGameplay(ctx, game));
            // WHY: the '+25' button adds to the hand-authored ScoreManager, which saves its best score.
            Step("demo save keys", () => DemoSaveKeys.Use(ctx, game.Score, "_keySave", BestScoreSaveKey));

            DemoFrame frame = Step("author the frame", () => DemoSceneKit.Frame(ctx,
                new DemoFrameOptions("Neo.Condition · NeoCondition", Accent) { ContentWidth = 920f, LogLines = 5 }));
            DemoPanel[] columns = Step("author the columns", () => frame.Content.Columns("NoCode | Rules", 2, 20f));
            Player player = Step("author the player column", () => BuildPlayerColumn(ctx, columns[0], game, frame.Log));
            RuleUi rules = Step("author the rules column", () => BuildRulesColumn(ctx, columns[1], game, ui));
            Step("wire the NeoCondition events", () => WireRules(game, player, rules, frame.Log));
            Step("author the info card", () => frame.InfoCard(info, InfoContent(game)));
            Step("assign the controller references", () =>
            {
                ctx.Set(ui, "_health", game.Health);
                ctx.Set(ui, "_scoreManager", game.Score);
                ctx.Set(ui, "_damagePerHit", Damage);
                ctx.Set(ui, "_healPerClick", HealAmount);
                ctx.Set(ui, "_pointsPerClick", Points);
                ctx.Set(ui, "_gameOverPanel", rules.GameOver);
                ctx.Set(ui, "_winPanel", rules.Win);
                ctx.Set(ui, "_warningIcon", player.LowHpBadge);
                ctx.Set(ui, "_statusText", rules.Playing);
                ctx.Set(ui, "_conditions", new List<NeoCondition> { game.Dead, game.Low, game.Win });
                ctx.Set(ui, "_codeResultText", rules.CodeResult);
                ctx.Set(ui, "_log", frame.Log);
            });
        }

        private static Gameplay FindGameplay(DemoSceneBuildContext ctx)
        {
            Health health = LogicDemoBuildUtility.RequireHandAuthored<Health>(ctx, HealthName, SceneFile);
            ScoreManager score = LogicDemoBuildUtility.RequireHandAuthored<ScoreManager>(ctx, ScoreName, SceneFile);
            NeoCondition dead = LogicDemoBuildUtility.RequireHandAuthored<NeoCondition>(ctx, DeadName, SceneFile);
            NeoCondition low = LogicDemoBuildUtility.RequireHandAuthored<NeoCondition>(ctx, LowName, SceneFile);
            NeoCondition win = LogicDemoBuildUtility.RequireHandAuthored<NeoCondition>(ctx, WinName, SceneFile);
            return new Gameplay(health, score, dead, low, win);
        }

        private static void RetireLegacyLayout(DemoSceneBuildContext ctx)
        {
            GameObject setup = ctx.FindHandAuthoredRoot("[NeoCondition Demo Setup]");
            if (setup != null)
            {
                if (setup.transform.childCount == 0 && setup.GetComponent("ConditionDemoSetup") != null)
                {
                    ctx.Retire(setup,
                        "editor-only setup helper that generated the old layout; this builder authors the scene now");
                }
                else
                {
                    ctx.Warn("Kept '[NeoCondition Demo Setup]': it is not the plain ConditionDemoSetup helper.");
                }
            }

            GameObject canvas = ctx.FindHandAuthoredRoot("Canvas");
            if (canvas == null)
            {
                return;
            }

            // WHY: a canvas that hosts the gameplay or the rules is not just the old presentation.
            bool isLegacyLayout = canvas.GetComponent<Canvas>() != null &&
                                  canvas.GetComponentInChildren<NeoCondition>(true) == null &&
                                  canvas.GetComponentInChildren<Health>(true) == null &&
                                  LogicDemoBuildUtility.HasButtonCalling<ConditionDemoUI>(canvas);
            if (isLegacyLayout)
            {
                ctx.Retire(canvas,
                    "pre-kit buttons, HP / score texts and panels; '[Demo] Frame' re-authors them with NoCode wiring");
            }
            else
            {
                ctx.Warn("Kept hand-authored root 'Canvas': it is not the pre-kit Condition layout. Delete it by hand " +
                         "if it duplicates '[Demo] Frame'.");
            }
        }

        private static void PrepareGameplay(DemoSceneBuildContext ctx, Gameplay game)
        {
            Func<Object, bool> demoController = target => target is ConditionDemoUI;
            foreach (NeoCondition rule in game.Rules)
            {
                ctx.OwnListeners(rule, rule.OnTrue, demoController);
                ctx.OwnListeners(rule, rule.OnFalse, demoController);
                ctx.OwnListeners(rule, rule.OnResult, demoController);
                ctx.OwnListeners(rule, rule.OnInvertedResult, demoController);
                if (!rule.UseUnscaledTime)
                {
                    // WHY: the demo's pause switch sets Time.timeScale to 0; unscaled checks keep the rules live.
                    ctx.Set(rule, "_useUnscaledTime", true);
                    ctx.Note("'" + rule.name + "' now checks in unscaled time (Use Unscaled Time) for the pause switch.");
                }

                ctx.MarkModified(rule);
            }

            // WHY: the retired layout's score label was the only TMP target; the frame shows the score through NoCode.
            ctx.Set(game.Score, "textScores", new List<TMP_Text>());
        }

        private static Player BuildPlayerColumn(DemoSceneBuildContext ctx, DemoPanel column, Gameplay game, DemoLogFeed log)
        {
            string healthType = typeof(Health).FullName;
            string scoreType = typeof(ScoreManager).FullName;

            column.Section("Player · object '" + HealthName + "'");
            // WHY: Health restores to max on Awake, so the authored bar starts full.
            Image hpFill = LogicDemoBuildUtility.FilledBar(ctx, column, "HP · Health.Hp", DemoPalette.Danger, 1f,
                out TMP_Text hpValue);
            LogicDemoBuildUtility.FormattedText(ctx, hpValue, "{0:0} / {1:0}", NoCodeFloatUpdateMode.Reactive, 0.16f,
                new LogicBindingSource(game.Health.gameObject, healthType, nameof(Health.Hp)),
                new LogicBindingSource(game.Health.gameObject, healthType, nameof(Health.MaxHp)));
            SetProgress hpProgress = ctx.Ensure<SetProgress>(hpFill.transform.parent.parent.gameObject);
            LogicDemoBuildUtility.Bind(ctx, hpProgress, game.Health.gameObject, healthType, nameof(Health.Hp),
                NoCodeFloatUpdateMode.Reactive);
            ctx.Set(hpProgress, "_image", hpFill);
            ctx.Set(hpProgress, "_slider", null);
            ctx.Set(hpProgress, "_minValue", 0f);
            ctx.Set(hpProgress, "_maxValue", (float)Mathf.Max(1, game.Health.maxHp));

            RectTransform warning = column.Row("Low HP Warning", 30f);
            Image badge = DemoSceneKit.Image(ctx, warning, "Badge", DemoPalette.WithAlpha(DemoPalette.Warm, 0.92f));
            DemoSceneKit.Stretch(badge.rectTransform);
            TMP_Text badgeText = DemoSceneKit.Text(ctx, badge.transform, "Label", "LOW HP · " + LowName + ".OnResult",
                15f, DemoPalette.Ink, TextAlignmentOptions.Center, FontStyles.Bold);
            DemoSceneKit.Stretch(badgeText.rectTransform);
            // WHY: the row keeps its height; only the badge follows the rule, so the buttons below never move.
            badge.gameObject.SetActive(false);

            Button[] hp = column.ButtonRow(DamageLabel, HealLabel);
            DemoWiring.AddInt(hp[0].onClick, game.Health.TakeDamage, Damage);
            DemoWiring.AddString(hp[0].onClick, log.Log, "Health.TakeDamage(" + Damage + ")");
            DemoWiring.AddInt(hp[1].onClick, game.Health.Heal, HealAmount);
            DemoWiring.AddString(hp[1].onClick, log.Log, "Health.Heal(" + HealAmount + ")");

            column.Section("Score · object '" + ScoreName + "'");
            int target = Mathf.Max(1, game.Score.TargetScoreValue);
            Image scoreFill = LogicDemoBuildUtility.FilledBar(ctx, column, "Score · ScoreManager.Score", DemoPalette.Good,
                Mathf.Clamp01((float)game.Score.ScoreValue / target), out TMP_Text scoreValue);
            LogicDemoBuildUtility.FormattedText(ctx, scoreValue, "{0:0} / {1:0}", NoCodeFloatUpdateMode.Reactive, 0.16f,
                new LogicBindingSource(game.Score.gameObject, scoreType, nameof(ScoreManager.Score)),
                new LogicBindingSource(game.Score.gameObject, scoreType, nameof(ScoreManager.TargetScoreValue)));
            SetProgress scoreProgress = ctx.Ensure<SetProgress>(scoreFill.transform.parent.parent.gameObject);
            LogicDemoBuildUtility.Bind(ctx, scoreProgress, game.Score.gameObject, scoreType, nameof(ScoreManager.Score),
                NoCodeFloatUpdateMode.Reactive);
            ctx.Set(scoreProgress, "_image", scoreFill);
            ctx.Set(scoreProgress, "_slider", null);
            ctx.Set(scoreProgress, "_minValue", 0f);
            ctx.Set(scoreProgress, "_maxValue", (float)target);

            Button[] score = column.ButtonRow(ScoreLabel, ResetLabel);
            DemoWiring.AddInt(score[0].onClick, game.Score.Add, Points);
            DemoWiring.AddString(score[0].onClick, log.Log, "ScoreManager.Add(" + Points + ")");
            DemoWiring.AddVoid(score[1].onClick, game.Health.Restore);
            DemoWiring.AddVoid(score[1].onClick, game.Score.ResetScore);
            DemoWiring.AddString(score[1].onClick, log.Log, "Health.Restore() + ScoreManager.ResetScore()");
            column.Note("Buttons call Health / ScoreManager directly");
            return new Player(badge.gameObject);
        }

        private static RuleUi BuildRulesColumn(DemoSceneBuildContext ctx, DemoPanel column, Gameplay game, ConditionDemoUI ui)
        {
            column.Section("NeoCondition · live rules");
            GameObject deadLamp = RuleRow(ctx, column, game.Dead, "Game Over banner", DemoPalette.Danger);
            GameObject lowLamp = RuleRow(ctx, column, game.Low, "LOW HP badge", DemoPalette.Warm);
            GameObject winLamp = RuleRow(ctx, column, game.Win, "You Win banner", DemoPalette.Good);

            RectTransform result = column.Row("Result", 60f);
            Image backdrop = DemoSceneKit.Image(ctx, result, "Backdrop", DemoPalette.Track);
            DemoSceneKit.Stretch(backdrop.rectTransform);
            TMP_Text playing = DemoSceneKit.Text(ctx, result, "Playing", "Playing · every rule is false", 18f,
                DemoPalette.Muted, TextAlignmentOptions.Center);
            DemoSceneKit.Stretch(playing.rectTransform, 6f);
            GameObject gameOver = Banner(ctx, result, "Game Over", "GAME OVER · " + DeadName + ".OnResult",
                DemoPalette.Danger);
            GameObject win = Banner(ctx, result, "You Win", "YOU WIN · " + WinName + ".OnResult", DemoPalette.Good);

            column.Section("C# · same rules from code");
            Button evaluate = column.Button(EvaluateLabel);
            DemoWiring.AddVoid(evaluate.onClick, ui.EvaluateInCode);
            RectTransform codeRow = column.Row("Code Result", 40f);
            TMP_Text code = DemoSceneKit.Text(ctx, codeRow, "Result", "NeoCondition.Evaluate() -> press the button", 15f,
                DemoPalette.Text, TextAlignmentOptions.Left);
            DemoSceneKit.Stretch(code.rectTransform);
            Toggle pause = column.Toggle("Pause · Time.timeScale = 0", false);
            DemoWiring.AddDynamic(pause.onValueChanged, ui.SetPaused);
            return new RuleUi(deadLamp, lowLamp, winLamp, playing, gameOver, win, code);
        }

        private static GameObject RuleRow(DemoSceneBuildContext ctx, DemoPanel column, NeoCondition rule, string effect,
            Color color)
        {
            RectTransform row = column.Row("Rule · " + rule.name, 54f);
            Image lamp = DemoSceneKit.Image(ctx, row, "Lamp", DemoPalette.Track, false);
            lamp.sprite = DemoSceneAssets.Disc;
            DemoSceneKit.Anchor(lamp.rectTransform, new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), new Vector2(0f, 0.5f),
                new Vector2(2f, 0f), new Vector2(22f, 22f));
            Image lit = DemoSceneKit.Image(ctx, lamp.transform, "Lit", color, false);
            lit.sprite = DemoSceneAssets.Disc;
            DemoSceneKit.Stretch(lit.rectTransform);
            lit.gameObject.SetActive(false);

            TMP_Text title = DemoSceneKit.Text(ctx, row, "Title", rule.name + " -> " + effect, 18f, color,
                TextAlignmentOptions.Left, FontStyles.Bold);
            DemoSceneKit.SetAnchors(title.rectTransform, new Vector2(0f, 0.5f), new Vector2(1f, 1f), new Vector2(34f, 0f),
                Vector2.zero);
            title.textWrappingMode = TextWrappingModes.NoWrap;
            title.overflowMode = TextOverflowModes.Ellipsis;

            TMP_Text text = DemoSceneKit.Text(ctx, row, "Rule", Describe(rule), 14f, DemoPalette.Muted);
            DemoSceneKit.SetAnchors(text.rectTransform, new Vector2(0f, 0f), new Vector2(1f, 0.5f), new Vector2(34f, 0f),
                Vector2.zero);
            // WHY: comparison operators such as "<=" must not be parsed as rich-text tags.
            text.richText = false;
            text.textWrappingMode = TextWrappingModes.NoWrap;
            text.overflowMode = TextOverflowModes.Ellipsis;
            return lit.gameObject;
        }

        private static GameObject Banner(DemoSceneBuildContext ctx, RectTransform parent, string name, string text,
            Color color)
        {
            Image banner = DemoSceneKit.Image(ctx, parent, name, color);
            DemoSceneKit.Stretch(banner.rectTransform);
            TMP_Text label = DemoSceneKit.Text(ctx, banner.transform, "Label", text, 20f, DemoPalette.Ink,
                TextAlignmentOptions.Center, FontStyles.Bold);
            DemoSceneKit.Stretch(label.rectTransform, 4f);
            label.enableAutoSizing = true;
            label.fontSizeMin = 12f;
            label.fontSizeMax = 20f;
            label.textWrappingMode = TextWrappingModes.NoWrap;
            banner.gameObject.SetActive(false);
            return banner.gameObject;
        }

        private static void WireRules(Gameplay game, Player player, RuleUi rules, DemoLogFeed log)
        {
            DemoWiring.AddDynamic(game.Dead.OnResult, rules.DeadLamp.SetActive);
            DemoWiring.AddDynamic(game.Dead.OnResult, rules.GameOver.SetActive);
            DemoWiring.AddString(game.Dead.OnTrue, log.Log, DeadName + ".OnTrue -> Game Over banner");

            DemoWiring.AddDynamic(game.Low.OnResult, rules.LowLamp.SetActive);
            DemoWiring.AddDynamic(game.Low.OnResult, player.LowHpBadge.SetActive);
            DemoWiring.AddString(game.Low.OnTrue, log.Log, LowName + ".OnTrue -> LOW HP badge");

            DemoWiring.AddDynamic(game.Win.OnResult, rules.WinLamp.SetActive);
            DemoWiring.AddDynamic(game.Win.OnResult, rules.Win.SetActive);
            DemoWiring.AddString(game.Win.OnTrue, log.Log, WinName + ".OnTrue -> You Win banner");
        }

        private static DemoInfoContent InfoContent(Gameplay game)
        {
            return new DemoInfoContent
            {
                Title = "Neo.Condition · NeoCondition",
                Purpose = "Rules without code: each NeoCondition reads a component value, compares it and fires " +
                          "OnTrue / OnFalse / OnResult. Here they read Health and ScoreManager.",
                RuntimeApi = "condition.Evaluate() (no events), Check(), AddCondition(entry), " +
                             "entry.TryEvaluate(owner, out bool); events OnTrue, OnFalse, OnResult(bool).",
                SceneWorkflow = "Select '" + DeadName + "', '" + LowName + "' (under '" + HealthName + "') or '" +
                                WinName + "' (under '" + ScoreName + "'): the Conditions list and OnResult listeners " +
                                "drive the lamps, banners and badge.",
                Verification = "Play: damage the player to 30 HP or less (LOW HP badge), then to 0 (Game Over); add " +
                               "score up to " + game.Score.TargetScoreValue + " (You Win). Reset restores both.",
                Hints = new[]
                {
                    "Buttons -> Health.TakeDamage / Heal, ScoreManager.Add (NoCode)",
                    "NeoCondition.OnResult -> GameObject.SetActive (lamp, banner)",
                    "Evaluate in code -> ConditionDemoUI -> NeoCondition.Evaluate() (C#)"
                },
                Corner = ModuleDemoSceneInfo.Corner.TopLeft,
                PanelWidth = 440f,
                StartCollapsed = false
            };
        }

        /// <summary>Readable form of a rule, built from its entries ("Health.HpValue &lt;= 30 · every 0.2 s").</summary>
        internal static string Describe(NeoCondition condition)
        {
            StringBuilder builder = new();
            IReadOnlyList<ConditionEntry> entries = condition.Conditions;
            for (int i = 0; i < entries.Count; i++)
            {
                ConditionEntry entry = entries[i];
                if (entry == null)
                {
                    continue;
                }

                if (builder.Length > 0)
                {
                    builder.Append(condition.Logic == LogicMode.AND ? " AND " : " OR ");
                }

                if (entry.Invert)
                {
                    builder.Append("NOT ");
                }

                builder.Append(ShortType(entry.ComponentTypeName)).Append('.').Append(entry.PropertyName).Append(' ')
                    .Append(Operator(entry.Compare)).Append(' ').Append(Threshold(entry));
            }

            if (builder.Length == 0)
            {
                builder.Append("no conditions (always true)");
            }

            builder.Append(" · ").Append(Schedule(condition));
            return builder.ToString();
        }

        private static string Threshold(ConditionEntry entry)
        {
            if (entry.ThresholdSource == ThresholdSource.OtherObject)
            {
                return ShortType(entry.OtherComponentTypeName) + "." + entry.OtherPropertyName;
            }

            return entry.CurrentValueType switch
            {
                ConditionValueType.Int => entry.ThresholdInt.ToString(CultureInfo.InvariantCulture),
                ConditionValueType.Float => entry.ThresholdFloat.ToString("0.##", CultureInfo.InvariantCulture),
                ConditionValueType.Bool => entry.ThresholdBool ? "true" : "false",
                _ => "\"" + entry.ThresholdString + "\""
            };
        }

        private static string Operator(CompareOp op)
        {
            return op switch
            {
                CompareOp.Equal => "==",
                CompareOp.NotEqual => "!=",
                CompareOp.Greater => ">",
                CompareOp.Less => "<",
                CompareOp.GreaterOrEqual => ">=",
                _ => "<="
            };
        }

        private static string Schedule(NeoCondition condition)
        {
            return condition.Mode switch
            {
                CheckMode.EveryFrame => "every frame",
                CheckMode.Interval => "every " + condition.CheckInterval.ToString("0.##", CultureInfo.InvariantCulture) + " s",
                _ => "on Check()"
            };
        }

        private static string ShortType(string typeName)
        {
            if (string.IsNullOrEmpty(typeName))
            {
                return "?";
            }

            int dot = typeName.LastIndexOf('.');
            return dot >= 0 && dot < typeName.Length - 1 ? typeName.Substring(dot + 1) : typeName;
        }

        private static void Step(string step, Action action)
        {
            DemoSceneKit.Step(SceneFile, step, action);
        }

        private static T Step<T>(string step, Func<T> action)
        {
            return DemoSceneKit.Step(SceneFile, step, action);
        }

        private sealed class Gameplay
        {
            public Gameplay(Health health, ScoreManager score, NeoCondition dead, NeoCondition low, NeoCondition win)
            {
                Health = health;
                Score = score;
                Dead = dead;
                Low = low;
                Win = win;
                Rules = new[] { dead, low, win };
            }

            public Health Health { get; }
            public ScoreManager Score { get; }
            public NeoCondition Dead { get; }
            public NeoCondition Low { get; }
            public NeoCondition Win { get; }
            public NeoCondition[] Rules { get; }
        }

        private sealed class Player
        {
            public Player(GameObject lowHpBadge)
            {
                LowHpBadge = lowHpBadge;
            }

            public GameObject LowHpBadge { get; }
        }

        private sealed class RuleUi
        {
            public RuleUi(GameObject deadLamp, GameObject lowLamp, GameObject winLamp, TMP_Text playing, GameObject gameOver,
                GameObject win, TMP_Text codeResult)
            {
                DeadLamp = deadLamp;
                LowLamp = lowLamp;
                WinLamp = winLamp;
                Playing = playing;
                GameOver = gameOver;
                Win = win;
                CodeResult = codeResult;
            }

            public GameObject DeadLamp { get; }
            public GameObject LowLamp { get; }
            public GameObject WinLamp { get; }
            public TMP_Text Playing { get; }
            public GameObject GameOver { get; }
            public GameObject Win { get; }
            public TMP_Text CodeResult { get; }
        }
    }
}
