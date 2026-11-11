using System;
using System.Globalization;
using System.Text.RegularExpressions;
using Neo.Samples;
using Neo.Tools;
using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

namespace Neo.Demo.Editor.Builders
{
    /// <summary>
    ///     Authors the Neo.Tools.Random demo into ChanceSystemExample.unity. The left column is pure NoCode: the
    ///     Roll button calls <see cref="ChanceSystemBehaviour.GenerateVoid" /> and a <see cref="Counter" />, and the
    ///     behaviour's own events (Events By Index, OnIdGenerated) light the rarity tile and write the labels through
    ///     <see cref="SetText" />. The right column samples the same table from C# and rolls an
    ///     <see cref="IntegerChanceTable" /> through <see cref="ChanceDemoController" />.
    ///     The hand-authored 'Chance' object keeps its table; the pre-kit Canvas and its UnityLifecycleEvents
    ///     helper are retired on the first build because the frame replaces them one to one.
    /// </summary>
    internal static class ChanceSystemExampleBuilder
    {
        private const string SceneName = "ChanceSystemExample";
        private const int MaxTiles = 6;

        private static readonly Color Accent = new(1f, 0.76f, 0.27f);
        private static readonly Color TileBody = new(0.10f, 0.11f, 0.16f, 1f);
        private static readonly Color MarkerColor = new(1f, 1f, 1f, 0.9f);
        private static readonly string[] DefaultLabels = { "Common", "Rare", "Epic", "Legendary", "Mythic", "Unique" };
        private static readonly float[] DefaultWeights = { 0.5f, 0.3f, 0.17f, 0.03f };

        private static readonly Color[] RarityColors =
        {
            new(0.72f, 0.76f, 0.84f),
            new(0.33f, 0.64f, 1f),
            new(0.72f, 0.42f, 1f),
            new(1f, 0.74f, 0.22f),
            DemoPalette.Good,
            DemoPalette.Danger
        };

        private static readonly Regex PlaceholderLabel = new(@"^Chance \d+$");

        [DemoSceneBuilder("Scenes/Tools/ChanceSystemExample.unity")]
        public static void Build(DemoSceneBuildContext ctx)
        {
            string step = "services";
            try
            {
                // WHY: the hand-authored 'Chance' wins, so the shipped table and its Inspector history survive.
                ChanceSystemBehaviour chance = ctx.FindOrCreate<ChanceSystemBehaviour>("Services", "Chance");
                ModuleDemoSceneInfo info = ctx.FindOrCreate<ModuleDemoSceneInfo>("Services", "Demo Info");
                ChanceDemoController controller = ctx.FindOrCreate<ChanceDemoController>("Services", "Demo Controller");

                step = "chance tables";
                int tiles = ConfigureTable(ctx, chance);
                ConfigureExactOdds(ctx, controller);

                step = "retire pre-kit layout";
                RetireLegacyLayout(ctx);

                step = "frame";
                DemoFrame frame = DemoSceneKit.Frame(ctx,
                    new DemoFrameOptions("Neo.Tools.Random", Accent) { ContentWidth = 920f });
                DemoPanel[] columns = frame.Content.Columns("NoCode | C#", 2, 20f);

                step = "NoCode column";
                NoCodeColumn noCode = BuildNoCodeColumn(ctx, columns[0], chance, controller, tiles);

                step = "C# column";
                CodeColumn code = BuildCodeColumn(ctx, columns[1], chance, controller, tiles);

                step = "Chance events";
                WireChanceEvents(ctx, chance, controller, noCode, tiles);

                step = "info card";
                frame.InfoCard(info, InfoContent());

                step = "controller references";
                ctx.Set(controller, "_chance", chance);
                ctx.Set(controller, "_tileOdds", noCode.TileOdds);
                ctx.Set(controller, "_oddsCaptions", code.OddsCaptions);
                ctx.Set(controller, "_observedFills", code.ObservedFills);
                ctx.Set(controller, "_expectedMarkers", code.ExpectedMarkers);
                ctx.Set(controller, "_sampleValue", code.Sample);
                ctx.Set(controller, "_exactCaptions", code.ExactCaptions);
                ctx.Set(controller, "_exactHits", code.ExactHits);
                ctx.Set(controller, "_ticketsValue", code.Tickets);
                ctx.Set(controller, "_log", frame.Log);
            }
            catch (Exception exception)
            {
                throw DemoSceneKit.StepFailed(SceneName, step, exception);
            }
        }

        private static int ConfigureTable(DemoSceneBuildContext ctx, ChanceSystemBehaviour chance)
        {
            ChanceManager manager = chance.Manager;
            bool generated = DemoSceneBuildContext.IsGenerated(chance.gameObject);
            if (generated || manager.Count == 0)
            {
                // WHY: add raw weights with normalization off, then normalize once; per-add normalization would
                // rescale the first entries before the later ones exist.
                manager.AutoNormalize = false;
                manager.Clear();
                for (int i = 0; i < DefaultWeights.Length; i++)
                {
                    manager.AddEntry(DefaultWeights[i], DefaultLabels[i], false, i);
                }

                manager.AutoNormalize = true;
                if (!generated)
                {
                    ctx.Note("'" + chance.name + "' had an empty table; filled it with the demo rarities.");
                }
            }
            else
            {
                int relabeled = 0;
                for (int i = 0; i < manager.Count; i++)
                {
                    ChanceManager.Entry entry = manager.GetEntry(i);
                    if (string.IsNullOrEmpty(entry.Label) || PlaceholderLabel.IsMatch(entry.Label))
                    {
                        entry.Label = i < DefaultLabels.Length ? DefaultLabels[i] : "Tier " + (i + 1);
                        relabeled++;
                    }
                }

                if (relabeled > 0)
                {
                    ctx.Note("Named " + relabeled + " placeholder 'Chance N' entries of '" + chance.name +
                             "' after their rarity (weights unchanged).");
                }
            }

            // WHY: the cached total is not serialized; refresh it so the authored percentages read the saved weights.
            manager.InvalidateTotalWeight();
            ctx.MarkModified(chance);
            if (manager.Count > MaxTiles)
            {
                ctx.Warn("'" + chance.name + "' has " + manager.Count + " entries; the demo shows tiles and bars for the first " +
                         MaxTiles + ".");
            }

            return Mathf.Min(manager.Count, MaxTiles);
        }

        private static void ConfigureExactOdds(DemoSceneBuildContext ctx, ChanceDemoController controller)
        {
            IntegerChanceTable table = controller.ExactOdds;
            if (table == null)
            {
                throw new InvalidOperationException("ChanceDemoController.ExactOdds is null.");
            }

            if (!DemoSceneBuildContext.IsGenerated(controller.gameObject) && table.Count > 0)
            {
                return;
            }

            table.Clear();
            table.Add(1, "Jackpot", 0);
            table.Add(999, "Bonus", 1);
            table.Add(999_000, "Nothing", 2);
            ctx.MarkModified(controller);
        }

        private static void RetireLegacyLayout(DemoSceneBuildContext ctx)
        {
            GameObject canvas = ctx.FindHandAuthoredRoot("Canvas");
            if (canvas != null)
            {
                // WHY: a canvas that hosts the module component itself is gameplay, not just the old presentation.
                if (canvas.GetComponent<Canvas>() != null &&
                    canvas.GetComponentInChildren<ChanceSystemBehaviour>(true) == null &&
                    ToolsDemoBuildUtility.HasButtonCalling<ChanceSystemBehaviour>(canvas))
                {
                    ctx.Retire(canvas,
                        "pre-kit Roll button, rarity labels and counter; '[Demo] Frame' re-authors them with the same NoCode wiring");
                }
                else
                {
                    ctx.Warn("Kept hand-authored root 'Canvas': it is not the pre-kit Chance layout (it hosts a " +
                             "ChanceSystemBehaviour or no Button calls one). Delete it by hand if it duplicates '[Demo] Frame'.");
                }
            }

            GameObject lifecycle = ctx.FindHandAuthoredRoot("UnityLifecycleEvents");
            if (lifecycle == null)
            {
                return;
            }

            if (lifecycle.transform.childCount == 0 && ToolsDemoBuildUtility.CountLiveListeners(lifecycle) == 0)
            {
                ctx.Retire(lifecycle,
                    "its OnStart listeners only hid the retired rarity labels; the new highlights are authored inactive");
            }
            else
            {
                ctx.Note("Kept hand-authored 'UnityLifecycleEvents': it still has live listeners.");
            }
        }

        private static NoCodeColumn BuildNoCodeColumn(DemoSceneBuildContext ctx, DemoPanel column,
            ChanceSystemBehaviour chance, ChanceDemoController controller, int tiles)
        {
            NoCodeColumn result = new(tiles);
            column.Section("NoCode · ChanceSystemBehaviour");
            column.Note("Object '" + chance.name + "' · its events drive everything here");

            RectTransform row = column.Row("Tiles", 112f);
            HorizontalLayoutGroup group = ctx.Ensure<HorizontalLayoutGroup>(row.gameObject);
            group.padding = new RectOffset(4, 4, 4, 4);
            group.spacing = 10f;
            group.childAlignment = TextAnchor.MiddleCenter;
            group.childControlWidth = true;
            group.childControlHeight = true;
            group.childForceExpandWidth = true;
            group.childForceExpandHeight = true;
            for (int i = 0; i < tiles; i++)
            {
                BuildTile(ctx, row, chance, i, result);
            }

            Button[] buttons = column.ButtonRow("Roll", "Reset");
            TMP_Text lastDrop = column.ValueRow("Last drop · Events By Index", "-", true).Value;
            TMP_Text index = column.ValueRow("Index · OnIdGenerated", "-", true).Value;
            TMP_Text rolls = column.ValueRow("Rolls · Counter", "0", true).Value;

            result.LastDrop = TextSetter(ctx, lastDrop, 0f);
            result.Index = TextSetter(ctx, index, 0f);
            SetText rollsText = TextSetter(ctx, rolls, 0.25f);
            Counter counter = ctx.Ensure<Counter>(rolls.gameObject);
            ctx.Set(counter, "_valueMode", (int)CounterValueMode.Int);
            ctx.Set(counter, "_saveEnabled", false);
            ctx.Set(counter, "_saveKey", "Demo.Chance.Rolls");
            DemoWiring.AddDynamic<int>(counter.OnValueChangedInt, rollsText.Set);

            DemoWiring.AddVoid(buttons[0].onClick, chance.GenerateVoid);
            DemoWiring.AddInt(buttons[0].onClick, counter.Add, 1);

            DemoWiring.AddInt(buttons[1].onClick, counter.Set, 0);
            DemoWiring.AddString(buttons[1].onClick, result.LastDrop.Set, "-");
            DemoWiring.AddString(buttons[1].onClick, result.Index.Set, "-");
            for (int i = 0; i < tiles; i++)
            {
                DemoWiring.AddBool(buttons[1].onClick, result.Highlights[i].SetActive, false);
            }

            DemoWiring.AddVoid(buttons[1].onClick, controller.ResetStats);

            column.Section("Wiring · select the objects");
            column.Note("Btn_Roll -> Chance.GenerateVoid + Counter.Add(1)");
            column.Note("Events By Index[i] -> tile Selected.SetActive(true)");
            column.Note("OnIdGenerated -> hide highlights · Index · RecordRoll");
            return result;
        }

        private static void BuildTile(DemoSceneBuildContext ctx, RectTransform row, ChanceSystemBehaviour chance, int index,
            NoCodeColumn result)
        {
            ChanceManager.Entry entry = chance.Manager.GetEntry(index);
            string label = EntryLabel(entry, index);
            Color color = RarityColors[index % RarityColors.Length];
            RectTransform tile = ctx.UiChild(row, "Tile · " + label);

            Image highlight = DemoSceneKit.Image(ctx, tile, "Selected", color);
            DemoSceneKit.Stretch(highlight.rectTransform, -4f);
            highlight.gameObject.SetActive(false);

            Image body = DemoSceneKit.Image(ctx, tile, "Body", TileBody);
            DemoSceneKit.Stretch(body.rectTransform);

            Image stripe = DemoSceneKit.Image(ctx, body.transform, "Stripe", color);
            DemoSceneKit.SetAnchors(stripe.rectTransform, new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(12f, -16f),
                new Vector2(-12f, -10f));

            TMP_Text name = DemoSceneKit.Text(ctx, body.transform, "Name", label.ToUpperInvariant(), 15f, color,
                TextAlignmentOptions.Center, FontStyles.Bold);
            DemoSceneKit.SetAnchors(name.rectTransform, new Vector2(0f, 0.5f), new Vector2(1f, 1f), new Vector2(4f, 0f),
                new Vector2(-4f, -18f));
            name.textWrappingMode = TextWrappingModes.NoWrap;
            name.enableAutoSizing = true;
            name.fontSizeMin = 9f;
            name.fontSizeMax = 15f;
            name.characterSpacing = 2f;

            TMP_Text odds = DemoSceneKit.Text(ctx, body.transform, "Odds",
                Percent(chance.GetNormalizedWeight(index)), 30f, DemoPalette.Text, TextAlignmentOptions.Center,
                FontStyles.Bold);
            DemoSceneKit.SetAnchors(odds.rectTransform, new Vector2(0f, 0f), new Vector2(1f, 0.55f), new Vector2(4f, 6f),
                new Vector2(-4f, 0f));
            odds.textWrappingMode = TextWrappingModes.NoWrap;

            result.Highlights[index] = highlight.gameObject;
            result.TileOdds[index] = odds;
        }

        private static CodeColumn BuildCodeColumn(DemoSceneBuildContext ctx, DemoPanel column, ChanceSystemBehaviour chance,
            ChanceDemoController controller, int tiles)
        {
            CodeColumn result = new(tiles, controller.ExactOdds.Count);
            column.Section("C# · ChanceManager");
            column.Note("manager.TryEvaluate(out index, out entry)");
            for (int i = 0; i < tiles; i++)
            {
                string label = EntryLabel(chance.Manager.GetEntry(i), i);
                float expected = chance.GetNormalizedWeight(i);
                DemoBarRow bar = column.BarRow(label, RarityColors[i % RarityColors.Length]);
                Image fill = bar.Fill;
                TMP_Text caption = bar.Caption;
                caption.text = label + " · expected " + Percent(expected) + " · observed -";
                result.OddsCaptions[i] = caption;
                result.ObservedFills[i] = fill;
                result.ExpectedMarkers[i] = ToolsDemoBuildUtility.BarMarker(ctx, fill, "Expected", expected, MarkerColor);
            }

            result.Sample = column.ValueRow("Sample", "0 rolls", true).Value;
            Button[] sample = column.ButtonRow("Sample 100", "Sample 1,000");
            DemoWiring.AddInt(sample[0].onClick, controller.RollInCode, 100);
            DemoWiring.AddInt(sample[1].onClick, controller.RollInCode, 1000);

            column.Section("C# · IntegerChanceTable");
            IntegerChanceTable exact = controller.ExactOdds;
            long total = exact.TotalWeight;
            for (int i = 0; i < exact.Count; i++)
            {
                string label = exact.Entries[i].Label;
                string captionText = (string.IsNullOrEmpty(label) ? "#" + i : label) + " · " +
                                     exact.GetWeight(i).ToString("N0", CultureInfo.InvariantCulture) + " in " +
                                     total.ToString("N0", CultureInfo.InvariantCulture);
                DemoValueRow exactRow = column.ValueRow(captionText, "0 (exp. 0.00)", true);
                result.ExactHits[i] = exactRow.Value;
                result.ExactCaptions[i] = exactRow.Caption;
            }

            result.Tickets = column.ValueRow("Tickets drawn", "0", true).Value;
            Button[] draw = column.ButtonRow("Draw 10,000", "Draw 100,000");
            DemoWiring.AddInt(draw[0].onClick, controller.DrawTickets, 10_000);
            DemoWiring.AddInt(draw[1].onClick, controller.DrawTickets, 100_000);
            return result;
        }

        private static void WireChanceEvents(DemoSceneBuildContext ctx, ChanceSystemBehaviour chance,
            ChanceDemoController controller, NoCodeColumn noCode, int tiles)
        {
            ctx.OwnListeners(chance, chance.OnIdGenerated);
            ctx.OwnListeners(chance, chance.OnIndexAndWeightSelected);
            ctx.OwnListeners(chance, chance.OnRollComplete);
            for (int i = 0; i < chance.EventsByIndex.Count; i++)
            {
                ctx.OwnListeners(chance, chance.EventsByIndex[i]);
            }

            for (int i = 0; i < tiles; i++)
            {
                DemoWiring.AddBool(chance.OnIdGenerated, noCode.Highlights[i].SetActive, false);
            }

            DemoWiring.AddDynamic<int>(chance.OnIdGenerated, noCode.Index.Set);
            DemoWiring.AddDynamic<int>(chance.OnIdGenerated, controller.RecordRoll);

            for (int i = 0; i < tiles; i++)
            {
                UnityEvent byIndex = chance.GetOrAddEventForIndex(i);
                string label = EntryLabel(chance.Manager.GetEntry(i), i);
                string colored = "<color=" + ToolsDemoBuildUtility.Hex(RarityColors[i % RarityColors.Length]) + ">" +
                                 label.ToUpperInvariant() + "</color>";
                DemoWiring.AddBool(byIndex, noCode.Highlights[i].SetActive, true);
                DemoWiring.AddString(byIndex, noCode.LastDrop.Set, colored);
            }

            ctx.MarkModified(chance);
        }

        private static SetText TextSetter(DemoSceneBuildContext ctx, TMP_Text label, float animationSeconds)
        {
            SetText setter = ctx.Ensure<SetText>(label.gameObject);
            ctx.Set(setter, "text", label);
            ctx.Set(setter, "separator", ",");
            ctx.Set(setter, "startAdd", string.Empty);
            ctx.Set(setter, "endAdd", string.Empty);
            ctx.Set(setter, "_timeAnim", animationSeconds);
            return setter;
        }

        private static DemoInfoContent InfoContent()
        {
            return new DemoInfoContent
            {
                Title = "Neo.Tools.Random · ChanceSystem",
                Purpose = "Weighted loot rolls. 'Chance' (ChanceSystemBehaviour) owns the table: the left column " +
                          "reacts only through its persistent UnityEvents, the right column samples the same odds " +
                          "from C# and rolls exact integer odds.",
                RuntimeApi = "ChanceManager.TryEvaluate / GetNormalizedWeight · ChanceSystemBehaviour.GenerateId · " +
                             "IntegerChanceTable.Roll / TryGetOdds / GetIndex(ticket).",
                SceneWorkflow = "Select 'Chance': Events By Index[i] lights tile i and writes Last drop; OnIdGenerated " +
                                "clears the highlight, writes Index and calls ChanceDemoController.RecordRoll.",
                Verification = "Play: Roll a few times, then Sample 1,000 - the bars settle on the white expected " +
                               "markers. Ten Draw 100,000 presses average one jackpot.",
                Hints = new[]
                {
                    "Btn_Roll -> ChanceSystemBehaviour.GenerateVoid + Counter.Add(1) (NoCode)",
                    "Events By Index -> tile Selected.SetActive + SetText.Set (NoCode)",
                    "Sample / Draw -> ChanceDemoController -> ChanceManager / IntegerChanceTable (C#)"
                }
            };
        }

        private static string EntryLabel(ChanceManager.Entry entry, int index)
        {
            if (entry != null && !string.IsNullOrEmpty(entry.Label))
            {
                return entry.Label;
            }

            return index < DefaultLabels.Length ? DefaultLabels[index] : "Tier " + (index + 1);
        }

        private static string Percent(float share)
        {
            return share.ToString(share > 0f && share < 0.1f ? "0.0%" : "0%", CultureInfo.InvariantCulture);
        }

        private sealed class NoCodeColumn
        {
            public NoCodeColumn(int tiles)
            {
                Highlights = new GameObject[tiles];
                TileOdds = new TMP_Text[tiles];
            }

            public GameObject[] Highlights { get; }
            public TMP_Text[] TileOdds { get; }
            public SetText LastDrop { get; set; }
            public SetText Index { get; set; }
        }

        private sealed class CodeColumn
        {
            public CodeColumn(int tiles, int exactEntries)
            {
                OddsCaptions = new TMP_Text[tiles];
                ObservedFills = new Image[tiles];
                ExpectedMarkers = new RectTransform[tiles];
                ExactCaptions = new TMP_Text[exactEntries];
                ExactHits = new TMP_Text[exactEntries];
            }

            public TMP_Text[] OddsCaptions { get; }
            public Image[] ObservedFills { get; }
            public RectTransform[] ExpectedMarkers { get; }
            public TMP_Text Sample { get; set; }
            public TMP_Text[] ExactCaptions { get; }
            public TMP_Text[] ExactHits { get; }
            public TMP_Text Tickets { get; set; }
        }
    }
}
