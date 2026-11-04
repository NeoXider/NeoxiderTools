using System.Globalization;
using Neo.Bonus;
using Neo.Samples;
using Neo.Shop;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Neo.Demo.Editor.Builders
{
    /// <summary>
    ///     Authors the Neo.Bonus wheel of fortune demo into WheelFortuneExample.unity. The hand-authored
    ///     Canvas/WheelFortune prefab instance is kept (wheel and its SPIN/STOP buttons are scaled into the stage
    ///     between the frame cards); [Demo] Services gets a session-only Money wallet, a WheelMoneyWin payout and the
    ///     demo controller; every sector label shows its prize from the same table that WheelMoneyWin pays.
    ///     The builder owns the persistent listeners of <see cref="WheelFortune.OnWinIdVariant" /> and rewrites them
    ///     on every run.
    /// </summary>
    internal static class WheelFortuneExampleBuilder
    {
        private const string Scene = "WheelFortuneExample";
        private const float WheelScale = 0.68f;

        private static readonly Color Accent = new(0.98f, 0.45f, 0.62f);

        /// <summary>Coins per sector id; the wheel labels and WheelMoneyWin.wins are both written from it.</summary>
        private static readonly int[] Prizes = { 10, 50, 20, 100, 30, 250 };

        private static readonly Vector2 WheelPosition = new(0f, 55f);
        private static readonly Vector2 StopButtonPosition = new(-170f, -285f);
        private static readonly Vector2 SpinButtonPosition = new(170f, -285f);

        [DemoSceneBuilder("Scenes/Bonuses/WheelFortuneExample.unity")]
        public static void Build(DemoSceneBuildContext ctx)
        {
            WheelFortune wheel = BonusDemoStage.FindOrInstantiate<WheelFortune>(ctx, BonusDemoStage.WheelPrefabGuid,
                "WheelFortune", Scene);
            BonusDemoStage.ConfigureStageCanvas(ctx, wheel, Scene);
            BonusDemoStage.Place(ctx, wheel.transform, "Wheel Back", WheelPosition, WheelScale, Scene);
            BonusDemoStage.Place(ctx, wheel.transform, "Button Stop", StopButtonPosition, WheelScale, Scene);
            BonusDemoStage.Place(ctx, wheel.transform, "Button Roll", SpinButtonPosition, WheelScale, Scene);

            ModuleDemoSceneInfo info = ctx.FindOrCreate<ModuleDemoSceneInfo>("Services", "Demo Info");
            Money wallet = BonusDemoStage.Wallet(ctx, "WheelDemoCoins");
            WheelMoneyWin payout = ctx.FindOrCreate<WheelMoneyWin>("Services", "Wheel Money Win");
            WheelFortuneDemoController controller =
                ctx.FindOrCreate<WheelFortuneDemoController>("Services", "Demo Controller");

            DemoFrame frame = DemoSceneKit.Frame(ctx, BonusDemoStage.FrameOptions("Neo.Bonus · Wheel of Fortune", Accent));
            DemoPanel card = frame.Content;

            card.Section("1 · Spin the wheel");
            card.Note("Press SPIN under the wheel, then STOP: it slows down, snaps to a sector and raises " +
                      "OnWinIdVariant(sector).", 62f);
            TMP_Text state = card.ValueLabel("State", nameof(WheelFortune.SpinState.Idle));
            TMP_Text canUse = card.ValueLabel("Can spin (canUse)", "yes");
            Button[] wheelButtons = card.ButtonRow("Re-arm wheel", "Spin from code");
            DemoWiring.AddVoid(wheelButtons[0].onClick, controller.Rearm);
            DemoWiring.AddVoid(wheelButtons[1].onClick, controller.SpinFromCode);

            card.Section("2 · Prize -> wallet");
            TMP_Text sector = card.ValueLabel("Last sector");
            TMP_Text prize = card.ValueLabel("Last prize");
            TMP_Text coins = card.ValueLabel("Coins (Money)", "0");
            card.Note("OnWinIdVariant -> WheelMoneyWin.Win(id) -> Money.Add(wins[id])", 44f);

            int[] wins = WinsFor(wheel);
            ctx.Set(payout, "wins", wins);
            ctx.Set(payout, "prize", prize);
            ctx.Set(payout, "_wallet", wallet);
            ctx.Set(wallet, "t_money", new[] { coins });
            LabelSectors(ctx, wheel, wins);

            // WHY: the wheel is a hand-authored prefab instance, so the kit does not reset its events; clearing first
            // keeps renamed or removed demo listeners from surviving a rebuild.
            DemoWiring.Clear(wheel.OnWinIdVariant);
            DemoWiring.AddDynamic(wheel.OnWinIdVariant, payout.Win);
            DemoWiring.AddDynamic(wheel.OnWinIdVariant, controller.OnWheelResult);
            ctx.MarkModified(wheel);

            ctx.Set(controller, "_wheel", wheel);
            ctx.Set(controller, "_payout", payout);
            ctx.Set(controller, "_wallet", wallet);
            ctx.Set(controller, "_stateValue", state);
            ctx.Set(controller, "_canUseValue", canUse);
            ctx.Set(controller, "_sectorValue", sector);
            ctx.Set(controller, "_log", frame.Log);

            frame.InfoCard(info, new DemoInfoContent
            {
                Title = "Neo.Bonus · WheelFortune",
                Purpose = "Wheel of fortune on uGUI. Spin() accelerates, Stop() slows it down and snaps to a sector, " +
                          "OnWinIdVariant(sector) reports the result. WheelMoneyWin turns the sector into coins.",
                RuntimeApi = "wheel.Spin() / Stop() / canUse / State / GetPrize(id) / OnWinIdVariant; " +
                             "WheelFortune.ResolveSectorIndex(...) without a scene; WheelMoneyWin.Win(id).",
                SceneWorkflow = "Select Canvas/WheelFortune: OnWinIdVariant calls Wheel Money Win and Demo Controller. " +
                                "[Demo] Services holds Wallet (Money) and the prize table (wins).",
                Verification = "Play: SPIN, then STOP. The sector under the arrow pays its prize; the single-use " +
                               "wheel locks until Re-arm wheel.",
                Hints = new[]
                {
                    "SPIN / STOP -> WheelFortune.Spin / Stop (prefab buttons)",
                    "Sector labels = WheelMoneyWin.wins: the number under the arrow is what you win",
                    "Spin from code -> Spin(), then Stop() after 1.5 s"
                }
            });
        }

        private static int[] WinsFor(WheelFortune wheel)
        {
            int count = wheel.Items != null && wheel.Items.Length > 0 ? wheel.Items.Length : Prizes.Length;
            int[] wins = new int[count];
            for (int i = 0; i < count; i++)
            {
                wins[i] = Prizes[i % Prizes.Length];
            }

            return wins;
        }

        private static void LabelSectors(DemoSceneBuildContext ctx, WheelFortune wheel, int[] wins)
        {
            GameObject[] items = wheel.Items;
            if (items == null || items.Length == 0)
            {
                ctx.Warn(Scene + ": WheelFortune has no prize items; sector labels were not written.");
                return;
            }

            for (int i = 0; i < items.Length && i < wins.Length; i++)
            {
                TMP_Text label = items[i] != null ? items[i].GetComponentInChildren<TMP_Text>(true) : null;
                if (label == null)
                {
                    ctx.Warn(Scene + ": prize item " + i + " has no TMP_Text; its label was left as authored.");
                    continue;
                }

                label.text = wins[i].ToString(CultureInfo.InvariantCulture);
                ctx.MarkModified(label);
            }
        }
    }
}
