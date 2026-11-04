using System;
using System.Globalization;
using Neo.Bonus;
using Neo.Samples;
using Neo.Shop;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

namespace Neo.Demo.Editor.Builders
{
    /// <summary>
    ///     Authors the Neo.Bonus slot machine demo into SlotExample.unity. The hand-authored Canvas/SlotUI prefab
    ///     instance is kept (scaled into the stage between the frame cards) and configured with demo data assets in
    ///     Data/Bonus (bets, five paylines, a paytable) and a session-only Money wallet. Bet and payline buttons call
    ///     SpinController directly; price, payline count, payout and coins are bound to their labels without code.
    ///     The builder owns the persistent listeners of OnStartSpin, OnWin, OnLose, OnChangeBet and
    ///     OnChangeMoneyWin and rewrites them on every run.
    /// </summary>
    internal static class SlotExampleBuilder
    {
        private const string Scene = "SlotExample";
        private const string BetsPath = "Data/Bonus/SlotDemoBets.asset";
        private const string PaylinesPath = "Data/Bonus/SlotDemoPaylines.asset";
        private const string PaytablePath = "Data/Bonus/SlotDemoPaytable.asset";
        private const int StartingCoins = 500;
        private const int TopUpCoins = 100;
        private const int MatchLength = 3;
        private const float MachineScale = 0.56f;

        private static readonly Color Accent = new(1f, 0.76f, 0.24f);
        private static readonly Vector2 MachinePosition = new(0f, 15f);

        /// <summary>Line bets the Bet buttons cycle through.</summary>
        private static readonly int[] Bets = { 5, 10, 25, 50, 100 };

        /// <summary>Window rows per reel (row 0 = bottom): middle, top, bottom and both diagonals.</summary>
        private static readonly int[][] Paylines =
        {
            new[] { 1, 1, 1 },
            new[] { 2, 2, 2 },
            new[] { 0, 0, 0 },
            new[] { 2, 1, 0 },
            new[] { 0, 1, 2 }
        };

        /// <summary>Three-of-a-kind multiplier per symbol, in SpritesData order (later symbols reuse the last).</summary>
        private static readonly float[] Multipliers = { 2f, 3f, 5f, 10f };

        [DemoSceneBuilder("Scenes/Bonuses/SlotExample.unity")]
        public static void Build(DemoSceneBuildContext ctx)
        {
            SpinController slot = BonusDemoStage.FindOrInstantiate<SpinController>(ctx, BonusDemoStage.SlotPrefabGuid,
                "SlotUI", Scene);
            SpritesData sprites = slot.allSpritesData;
            if (sprites == null || sprites.visuals == null || sprites.visuals.Length == 0)
            {
                throw new InvalidOperationException(Scene + ": SlotUI has no Sprites Data with symbols; assign one on " +
                                                    "its SpinController before building the demo.");
            }

            Canvas stage = BonusDemoStage.ConfigureStageCanvas(ctx, slot, Scene);
            BonusDemoStage.Backdrop(ctx, stage, Accent);
            LayoutMachine(ctx, slot);

            BetsData bets = DemoSceneAssets.EnsureAsset<BetsData>(BetsPath, ConfigureBets);
            LinesData paylines = DemoSceneAssets.EnsureAsset<LinesData>(PaylinesPath, ConfigurePaylines);
            SpriteMultiplayerData paytable = DemoSceneAssets.EnsureAsset<SpriteMultiplayerData>(PaytablePath,
                asset => ConfigurePaytable(asset, sprites));

            ModuleDemoSceneInfo info = ctx.FindOrCreate<ModuleDemoSceneInfo>("Services", "Demo Info");
            Money wallet = BonusDemoStage.Wallet(ctx, "SlotDemoCoins");
            SlotDemoController controller = ctx.FindOrCreate<SlotDemoController>("Services", "Demo Controller");

            ctx.Set(slot, "betsData", bets);
            ctx.Set(slot, "checkSpin.isActive", true);
            ctx.Set(slot, "checkSpin._linesData", paylines);
            ctx.Set(slot, "checkSpin._spritesMultiplierData", paytable);
            ctx.Set(slot, "checkSpin._sequenceLength", MatchLength);
            ctx.Set(slot, "_moneyGameObject", wallet.gameObject);
            ctx.Set(slot, "_priceOnLine", true);
            ctx.Set(slot, "_autoPayout", true);
            ctx.Set(slot, "_betsId", 0);
            ctx.Set(slot, "_countLine", 1);

            DemoFrame frame = DemoSceneKit.Frame(ctx, BonusDemoStage.FrameOptions("Neo.Bonus · Slot Machine", Accent));
            DemoPanel card = frame.Content;

            card.Section("Wallet · Neo.Shop.Money");
            TMP_Text coins = card.ValueLabel("Coins", Format(StartingCoins));
            Button topUp = card.Button("+" + TopUpCoins + " coins");
            DemoWiring.AddFloat(topUp.onClick, wallet.Add, TopUpCoins);

            card.Section("Bet · SpinController");
            TMP_Text price = card.ValueLabel("Spin price", Format(SlotSpinMath.ComputeSpinPrice(Bets[0], 1, true)));
            TMP_Text lineCount = card.ValueLabel("Paylines", "1");
            Button[] betButtons = card.ButtonRow("Bet -", "Bet +", "Max bet");
            DemoWiring.AddVoid(betButtons[0].onClick, slot.RemoveBet);
            DemoWiring.AddVoid(betButtons[1].onClick, slot.AddBet);
            DemoWiring.AddVoid(betButtons[2].onClick, slot.SetMaxBet);
            Button[] lineButtons = card.ButtonRow("Lines -", "Lines +");
            DemoWiring.AddVoid(lineButtons[0].onClick, slot.RemoveLine);
            DemoWiring.AddVoid(lineButtons[1].onClick, slot.AddLine);
            card.Note("Price = bet × paylines. Paylines: 1 middle, 2 top, 3 bottom, 4 and 5 diagonals.", 44f);

            card.Section("Spin · 3 in a row pays");
            PaytableRow(ctx, card, sprites);
            Button[] spinButtons = card.ButtonRow("Spin", "Force a win");
            DemoWiring.AddVoid(spinButtons[0].onClick, controller.Spin);
            DemoWiring.AddVoid(spinButtons[1].onClick, controller.ForceWin);
            DemoSliderRow chance = card.SliderRow("Chance to win (ChanceWin)", 0f, 1f, slot.ChanceWin, false, "0%");
            DemoWiring.AddDynamic(chance.Slider.onValueChanged, controller.SetChanceWin);
            TMP_Text lastPayout = card.ValueLabel("Last payout");
            TMP_Text result = card.ValueLabel("Result", "press SPIN");

            // WHY: NoCode path - SpinController and Money write these labels themselves, no controller code involved.
            ctx.Set(slot, "_textCountLine", lineCount);
            ctx.Set(wallet, "t_money", new[] { coins });

            // WHY: SlotUI is a hand-authored prefab instance, so the kit does not reset its events; clearing first
            // keeps renamed or removed demo listeners from surviving a rebuild.
            DemoWiring.Clear(slot.OnStartSpin);
            DemoWiring.Clear(slot.OnWin);
            DemoWiring.Clear(slot.OnLose);
            DemoWiring.Clear(slot.OnChangeBet);
            DemoWiring.Clear(slot.OnChangeMoneyWin);
            DemoWiring.AddDynamic(slot.OnChangeBet, price.SetText);
            DemoWiring.AddDynamic(slot.OnChangeMoneyWin, lastPayout.SetText);
            DemoWiring.AddVoid(slot.OnStartSpin, controller.OnSpinStarted);
            DemoWiring.AddDynamic(slot.OnWin, controller.OnSpinWon);
            DemoWiring.AddVoid(slot.OnLose, controller.OnSpinLost);
            ctx.MarkModified(slot);

            ctx.Set(controller, "_slot", slot);
            ctx.Set(controller, "_wallet", wallet);
            ctx.Set(controller, "_startingCoins", StartingCoins);
            ctx.Set(controller, "_forcedWinSymbol", sprites.visuals[sprites.visuals.Length - 1].id);
            ctx.Set(controller, "_chanceSlider", chance.Slider);
            ctx.Set(controller, "_resultValue", result);
            ctx.Set(controller, "_log", frame.Log);

            frame.InfoCard(info, new DemoInfoContent
            {
                Title = "Neo.Bonus · SpinController",
                Purpose = "Slot machine: SpinController spins the reels, charges bet × paylines from a wallet, " +
                          "checks paylines (CheckSpin) and pays bet × multiplier for each winning line.",
                RuntimeApi = "slot.StartSpin() / AddBet / RemoveBet / SetMaxBet / AddLine / RemoveLine / ChanceWin / " +
                             "ForceNextOutcomeOnPayline / GetLastResult(); SlotSpinMath for price and payout.",
                SceneWorkflow = "Select Canvas/SlotUI: Money Game Object = [Demo] Services/Wallet (Money); Bets, " +
                                "Lines and Sprites Multiplier data live in Data/Bonus; its events drive the card labels.",
                Verification = "Play: Spin charges the price; three equal symbols on an active payline pay; " +
                               "Force a win queues a winning outcome on payline 1.",
                Hints = new[]
                {
                    "Bet / Lines buttons -> SpinController.AddBet / RemoveBet / SetMaxBet / AddLine / RemoveLine",
                    "OnChangeBet and OnChangeMoneyWin -> TMP_Text.SetText (no code)",
                    "The SPIN button on the machine calls SpinController.StartSpin directly"
                }
            });
        }

        private static void LayoutMachine(DemoSceneBuildContext ctx, SpinController slot)
        {
            RectTransform rect = (RectTransform)slot.transform;
            Vector2 center = new(0.5f, 0.5f);
            rect.anchorMin = center;
            rect.anchorMax = center;
            rect.pivot = center;
            rect.anchoredPosition = MachinePosition;
            rect.localScale = new Vector3(MachineScale, MachineScale, MachineScale);
            ctx.MarkModified(rect);
        }

        /// <summary>One chip per symbol: its sprite and its three-of-a-kind multiplier.</summary>
        private static void PaytableRow(DemoSceneBuildContext ctx, DemoPanel card, SpritesData sprites)
        {
            RectTransform row = card.Row("Paytable", 56f);
            HorizontalLayoutGroup layout = ctx.Ensure<HorizontalLayoutGroup>(row.gameObject);
            layout.padding = new RectOffset(0, 0, 0, 0);
            layout.spacing = 6f;
            layout.childAlignment = TextAnchor.MiddleCenter;
            layout.childControlWidth = true;
            layout.childControlHeight = true;
            layout.childForceExpandWidth = true;
            layout.childForceExpandHeight = true;

            for (int i = 0; i < sprites.visuals.Length; i++)
            {
                SlotVisualData visual = sprites.visuals[i];
                if (visual == null)
                {
                    continue;
                }

                Image chip = DemoSceneKit.Image(ctx, row, "Symbol " + visual.id, DemoPalette.Track);
                HorizontalLayoutGroup chipLayout = ctx.Ensure<HorizontalLayoutGroup>(chip.gameObject);
                chipLayout.padding = new RectOffset(4, 6, 4, 4);
                chipLayout.spacing = 4f;
                chipLayout.childAlignment = TextAnchor.MiddleCenter;
                chipLayout.childControlWidth = true;
                chipLayout.childControlHeight = true;
                chipLayout.childForceExpandWidth = false;
                chipLayout.childForceExpandHeight = false;

                Image icon = DemoSceneKit.Image(ctx, chip.transform, "Icon", Color.white, false);
                icon.sprite = visual.sprite;
                icon.preserveAspect = true;
                icon.enabled = visual.sprite != null;
                LayoutElement iconSize = ctx.Ensure<LayoutElement>(icon.gameObject);
                iconSize.minWidth = 40f;
                iconSize.minHeight = 40f;
                iconSize.preferredWidth = 40f;
                iconSize.preferredHeight = 40f;

                TMP_Text multiplier = DemoSceneKit.Text(ctx, chip.transform, "Multiplier",
                    "×" + MultiplierFor(i).ToString("0.#", CultureInfo.InvariantCulture), 20f, Accent,
                    TextAlignmentOptions.MidlineLeft, FontStyles.Bold);
                multiplier.textWrappingMode = TextWrappingModes.NoWrap;
            }
        }

        private static void ConfigureBets(BetsData asset)
        {
            SerializedObject serialized = new(asset);
            SerializedProperty bets = BonusDemoStage.Require(serialized, "_bets", Scene);
            bets.arraySize = Bets.Length;
            for (int i = 0; i < Bets.Length; i++)
            {
                bets.GetArrayElementAtIndex(i).intValue = Bets[i];
            }

            serialized.ApplyModifiedPropertiesWithoutUndo();
        }

        private static void ConfigurePaylines(LinesData asset)
        {
            SerializedObject serialized = new(asset);
            SerializedProperty lines = BonusDemoStage.Require(serialized, "_lines", Scene);
            lines.arraySize = Paylines.Length;
            for (int i = 0; i < Paylines.Length; i++)
            {
                SerializedProperty rows = lines.GetArrayElementAtIndex(i).FindPropertyRelative("corY");
                rows.arraySize = Paylines[i].Length;
                for (int column = 0; column < Paylines[i].Length; column++)
                {
                    rows.GetArrayElementAtIndex(column).intValue = Paylines[i][column];
                }
            }

            serialized.ApplyModifiedPropertiesWithoutUndo();
        }

        private static void ConfigurePaytable(SpriteMultiplayerData asset, SpritesData sprites)
        {
            SerializedObject serialized = new(asset);
            BonusDemoStage.Require(serialized, "_generate", Scene).boolValue = false;
            BonusDemoStage.Require(serialized, "_spritesData", Scene).objectReferenceValue = sprites;
            BonusDemoStage.Require(serialized, "_minCount", Scene).intValue = MatchLength;
            BonusDemoStage.Require(serialized, "_maxCount", Scene).intValue = MatchLength;
            SerializedProperty entries = BonusDemoStage.Require(serialized, "_spritesMultiplier.spriteMults", Scene);
            entries.arraySize = sprites.visuals.Length;
            for (int i = 0; i < sprites.visuals.Length; i++)
            {
                SerializedProperty entry = entries.GetArrayElementAtIndex(i);
                entry.FindPropertyRelative("id").intValue = sprites.visuals[i] != null ? sprites.visuals[i].id : i;
                SerializedProperty counts = entry.FindPropertyRelative("countMult");
                counts.arraySize = 1;
                SerializedProperty count = counts.GetArrayElementAtIndex(0);
                count.FindPropertyRelative("count").intValue = MatchLength;
                count.FindPropertyRelative("mult").floatValue = MultiplierFor(i);
            }

            serialized.ApplyModifiedPropertiesWithoutUndo();
        }

        private static float MultiplierFor(int symbolIndex)
        {
            return Multipliers[Mathf.Clamp(symbolIndex, 0, Multipliers.Length - 1)];
        }

        private static string Format(int value)
        {
            return value.ToString(CultureInfo.InvariantCulture);
        }
    }
}
