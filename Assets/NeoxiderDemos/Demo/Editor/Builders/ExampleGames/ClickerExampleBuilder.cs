using System.Collections.Generic;
using Neo.Level;
using Neo.Samples;
using Neo.Shop;
using Neo.Tools;
using UnityEngine;
using ShopComponent = Neo.Shop.Shop;
using UiPages = Neo.UI.UI;

namespace Neo.Demo.Editor.Builders
{
    /// <summary>
    ///     Guide of the owner's hand-authored clicker (Scenes/_ExampleGame/ClickerExample.unity), a NoCode reference
    ///     game. The game - Pizza, Shop, the Canvas pages and the --System-- prefab - stays as authored; this builder
    ///     only adds the info card, upgrades the legacy input module and gives the shop and the persisting managers of
    ///     the --System-- prefab (Money, ScoreManager, LevelManager) demo-only save keys: with the library defaults
    ///     "Shop" / "Money" / "BestScore" / "LevelManager" the sample would read and overwrite the user's own
    ///     purchases, balance, best score and level progress.
    /// </summary>
    internal static class ClickerExampleBuilder
    {
        internal const string ScenePath = "Scenes/_ExampleGame/ClickerExample.unity";
        internal const string MoneySaveKey = "NeoDemo.Clicker.Money";
        internal const string ShopSaveKey = "NeoDemo.Clicker.Shop";
        internal const string BestScoreSaveKey = "NeoDemo.Clicker.BestScore";
        internal const string LevelSaveKey = "NeoDemo.Clicker.Level";

        private const string SceneName = "ClickerExample.unity";

        private static readonly Color Accent = DemoPalette.Warm;

        [DemoSceneBuilder(ScenePath)]
        public static void Build(DemoSceneBuildContext ctx)
        {
            ShopComponent shop = DemoSceneKit.Step(SceneName, "find the hand-authored Shop",
                () => ExampleGameGuide.Require<ShopComponent>(ctx, SceneName, "skin shop"));
            InteractiveObject pizza = DemoSceneKit.Step(SceneName, "find the clickable pizza",
                () => ExampleGameGuide.Require<InteractiveObject>(ctx, SceneName, "clickable pizza"));
            Money wallet = ExampleGameGuide.First<Money>(ctx);
            UiPages pages = ExampleGameGuide.First<UiPages>(ctx);

            DemoSceneKit.Step(SceneName, "give the shop and the --System-- managers demo-only save keys", () =>
            {
                DemoSaveKeys.Use(ctx, shop, "_keySave", ShopSaveKey);
                DemoSaveKeys.UseForAll<Money>(ctx, "_moneySave", MoneySaveKey);
                DemoSaveKeys.UseForAll<ScoreManager>(ctx, "_keySave", BestScoreSaveKey);
                DemoSaveKeys.UseForAll<LevelManager>(ctx, "_saveKey", LevelSaveKey);
            });

            ExampleGameGuide.AuthorInfoCard(ctx, SceneName, Content(ctx, shop, pizza, wallet, pages), Accent);
        }

        private static DemoInfoContent Content(DemoSceneBuildContext ctx, ShopComponent shop, InteractiveObject pizza,
            Money wallet, UiPages pages)
        {
            string pizzaName = ExampleGameGuide.NameOf(pizza);
            string shopName = ExampleGameGuide.NameOf(shop);
            List<string> hints = new()
            {
                pizzaName + ": InteractiveObject onInteractDown / onRightClick -> Money.Add(1), onClick -> " +
                "Animator.SetTrigger(Click)"
            };
            ExampleGameGuide.Hint(ctx, hints, wallet, "the Money wallet",
                () => DemoSceneBuildContext.PathOf(wallet.transform) + ": Money wallet; its T_money text list " +
                      "shows the coin count");
            hints.Add(shopName + ": one ShopItem per ShopItemData in the scroll list; its buttons -> Shop.Buy(index) " +
                      "(Auto Subscribe)");
            ExampleGameGuide.Hint(ctx, hints, pizza.GetComponent<ShopItem>(), "the ShopItem preview on " + pizzaName,
                () => pizzaName + " ShopItem = the shop preview: the selected skin is drawn on its sprite");
            ExampleGameGuide.Hint(ctx, hints, pages, "the UI page switcher",
                () => ExampleGameGuide.NameOf(pages) + ": UI pages Game / Shop; SHOP and Cancel -> UI.SetPage(1 / 0)");

            return new DemoInfoContent
            {
                Title = "Example game · Clicker",
                Purpose = "A complete clicker built only from library components and UnityEvents, without game " +
                          "scripts: earn coins on the pizza, spend them on skins in the shop.",
                RuntimeApi = "Money.Add(amount) / TrySpend, Shop.Buy(index) / TryBuy(itemId), UI.SetPage(index), " +
                             "InteractiveObject events.",
                SceneWorkflow = pizzaName + " = InteractiveObject + Animator + ShopItem; " + shopName +
                                " = Shop with its ShopItemData skins; Canvas = UI pages; --System-- = Money and " +
                                "the other managers.",
                Verification = "Click the pizza (left or right) or press E: coins rise. SHOP opens the list: buy a " +
                               "skin and the pizza changes. Coins and skins are saved under the demo keys " +
                               MoneySaveKey + " / " + ShopSaveKey + ".",
                Hints = hints.ToArray(),
                Corner = ModuleDemoSceneInfo.Corner.TopLeft,
                PanelWidth = ExampleGameGuide.CardWidth,
                StartCollapsed = false
            };
        }
    }
}
