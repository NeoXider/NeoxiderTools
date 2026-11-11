using System;
using System.Collections.Generic;
using Neo.Samples;
using Neo.Shop;
using Neo.UI;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;
using Object = UnityEngine.Object;
using ShopComponent = Neo.Shop.Shop;

namespace Neo.Demo.Editor.Builders
{
    /// <summary>
    ///     Authors the Neo.Shop demo into Shop.unity. The hand-authored <see cref="ShopComponent" /> (catalog, save key)
    ///     and <see cref="Money" /> wallet stay; the builder authors the storefront (one <see cref="ShopItem" /> card per
    ///     catalog item with <see cref="ButtonPrice" /> and <see cref="ShopPurchaseButtonView" />, a preview slot), the
    ///     wallet card (a <see cref="TextMoney" /> label and NoCode <see cref="Money.Add" /> buttons), the typed
    ///     <see cref="ShopComponent.TryBuy(ShopItemData)" /> / runtime price / reset rows, the log feed and the info card,
    ///     then points the Shop at the authored cards and wires its events to <see cref="ShopDemoController" />. The
    ///     pre-kit portrait storefront canvas is retired once.
    /// </summary>
    internal static class ShopDemoBuilder
    {
        private const string Scene = "Shop.unity";
        private const string LegacyCanvasName = "Canvas";
        private const int MaxCards = 4;
        private const float CardRowHeight = 340f;
        private const float StartBalance = 200f;
        internal const string ShopSaveKey = "NeoDemo.Shop.Shop";
        internal const string MoneySaveKey = "NeoDemo.Shop.Money";

        private static readonly Color Accent = new(1f, 0.78f, 0.30f);
        private static readonly Color CardColor = new(0.20f, 0.21f, 0.29f, 1f);
        private static readonly Color PreviewColor = new(1f, 1f, 1f, 0.06f);
        private static readonly Vector2 LegacyReferenceResolution = new(1080f, 1920f);

        [DemoSceneBuilder("Scenes/Shop/Shop.unity")]
        public static void Build(DemoSceneBuildContext ctx)
        {
            ShopComponent shop = Step("hand-authored Shop",
                () => ShopNpcBuildSupport.RequireHandAuthored<ShopComponent>(ctx, "catalog, purchase flow and save"));
            Money money = Step("hand-authored Money",
                () => ShopNpcBuildSupport.RequireHandAuthored<Money>(ctx, "the wallet the shop spends from"));
            ShopItemData[] catalog = Step("catalog", () => RequireCatalog(shop));
            ShopItemData saleItem = MostExpensive(catalog);

            ModuleDemoSceneInfo info = Step("services", () => ctx.FindOrCreate<ModuleDemoSceneInfo>("Services", "Demo Info"));
            ShopDemoController controller = Step("services",
                () => ctx.FindOrCreate<ShopDemoController>("Services", "Demo Controller"));

            Step("retire the pre-kit storefront", () => RetireLegacyStorefront(ctx));

            DemoFrame frame = Step("frame", () => DemoSceneKit.Frame(ctx,
                new DemoFrameOptions("Neo.Shop · Shop + Money", Accent) { ContentWidth = 900f, LogLines = 5 }));

            Storefront storefront = Step("storefront", () => BuildStorefront(ctx, frame.Content, shop, catalog));
            Controls controls = Step("controls", () => BuildControls(frame, money, controller, catalog, saleItem));

            Step("shop configuration", () => ConfigureShop(ctx, shop, money, controller, storefront));
            Step("wallet configuration", () => ConfigureWallet(ctx, money, controls.Balance));

            Step("info card", () => frame.InfoCard(info, new DemoInfoContent
            {
                Title = "Neo.Shop · Shop + Money",
                Purpose = "A storefront made of the module's own components: Shop (catalog, purchase flow, save), " +
                          "Money (wallet) and one ShopItem card per item with ButtonPrice + ShopPurchaseButtonView " +
                          "(Buy / Equip / Equipped / Unaffordable). Nothing is built at runtime.",
                RuntimeApi = "shop.TryBuy(item) -> ShopPurchaseStatus; Buy / Select / IsOwned / CanAfford / GetPrice; " +
                             "SetRuntimePrice / ClearRuntimePrice; money.Add / Spend / CanSpend; events OnPurchasedId, " +
                             "OnPurchaseFailedId, OnSelectId, OnShopChanged.",
                SceneWorkflow = "Select 'Shop': its Shop Items are the cards in '[Demo] Frame/Content/Storefront " +
                                "Cards' and Auto Subscribe binds each price button to Buy. 'Money' is the wallet; the " +
                                "balance label is a TextMoney. Shop's string events and On Shop Changed call 'Demo Controller'.",
                Verification = "Play: buy the cards - " + CatalogSummary(catalog) + ". With too few coins a card turns " +
                               "Unaffordable and its button disables. Purchases and the balance persist between plays " +
                               "until Reset.",
                Hints = new[]
                {
                    "Price button -> Shop.Buy (Auto Subscribe) -> Money.Spend",
                    "+25 / +100 / Empty -> Money directly (NoCode persistent calls)",
                    "TryBuy row -> ShopDemoController -> shop.TryBuy -> ShopPurchaseStatus",
                    "ShopItem.OnSelectItem -> Equipped Glow.SetActive(true)"
                },
                Corner = ModuleDemoSceneInfo.Corner.TopLeft,
                PanelWidth = 400f,
                StartCollapsed = false
            }));

            Step("demo save keys", () =>
            {
                DemoSaveKeys.Use(ctx, shop, "_keySave", ShopSaveKey);
                DemoSaveKeys.Use(ctx, money, "_moneySave", MoneySaveKey);
            });

            Step("controller references", () =>
            {
                ctx.Set(controller, "_shop", shop);
                ctx.Set(controller, "_money", money);
                ctx.Set(controller, "_saleItem", saleItem);
                ctx.Set(controller, "_saleFactor", 0.5f);
                ctx.Set(controller, "_startBalance", StartBalance);
                ctx.Set(controller, "_shopSaveKey", ReadString(shop, "_keySave", "Shop"));
                ctx.Set(controller, "_statusValue", controls.Status);
                ctx.Set(controller, "_equippedValue", controls.Equipped);
                ctx.Set(controller, "_ownedValue", controls.Owned);
                ctx.Set(controller, "_salePriceValue", controls.SalePrice);
                ctx.Set(controller, "_log", frame.Log);
            });
        }

        private static ShopItemData[] RequireCatalog(ShopComponent shop)
        {
            ShopItemData[] items = shop.ShopItemDatas;
            if (items == null || items.Length == 0)
            {
                throw new InvalidOperationException("'" + shop.name + "' has no ShopItemData in its catalog; assign the " +
                                                    "Resources/Shop items before rebuilding.");
            }

            if (items.Length > MaxCards)
            {
                throw new InvalidOperationException("the storefront row fits " + MaxCards + " cards, the catalog has " +
                                                    items.Length + ".");
            }

            for (int i = 0; i < items.Length; i++)
            {
                if (items[i] == null)
                {
                    throw new InvalidOperationException("catalog entry " + i + " of '" + shop.name + "' is empty.");
                }
            }

            return items;
        }

        private static ShopItemData MostExpensive(ShopItemData[] catalog)
        {
            ShopItemData best = catalog[0];
            for (int i = 1; i < catalog.Length; i++)
            {
                if (catalog[i].price > best.price)
                {
                    best = catalog[i];
                }
            }

            return best;
        }

        private static void RetireLegacyStorefront(DemoSceneBuildContext ctx)
        {
            foreach (GameObject root in ctx.Scene.GetRootGameObjects())
            {
                if (root.name != LegacyCanvasName || DemoSceneBuildContext.IsGenerated(root))
                {
                    continue;
                }

                CanvasScaler scaler = root.GetComponent<CanvasScaler>();
                bool legacyLayout = root.GetComponent<Canvas>() != null && scaler != null &&
                                    scaler.referenceResolution == LegacyReferenceResolution &&
                                    root.transform.Find("Scroll View") != null;
                // WHY: a canvas that hosts the Shop or the wallet is gameplay, not just the old view.
                bool hostsModule = root.GetComponentInChildren<ShopComponent>(true) != null ||
                                   root.GetComponentInChildren<Money>(true) != null;
                if (legacyLayout && !hostsModule)
                {
                    ctx.Retire(root, "pre-kit portrait storefront (1080x1920 scroll list); the cards, preview and " +
                                     "wallet label are authored in '[Demo] Frame' now");
                }
                else
                {
                    ctx.Warn("Kept hand-authored root 'Canvas': it is not the pre-kit Shop storefront. Delete it by hand " +
                             "if it duplicates '[Demo] Frame'.");
                }
            }
        }

        private static Storefront BuildStorefront(DemoSceneBuildContext ctx, DemoPanel card, ShopComponent shop,
            ShopItemData[] catalog)
        {
            card.Section("Storefront · Shop + ShopItem cards");
            card.Note("Click a price button: Shop.Buy -> Money.Spend -> the card turns Equip / Equipped");

            RectTransform row = card.Row("Storefront Cards", CardRowHeight);
            HorizontalLayoutGroup layout = ctx.Ensure<HorizontalLayoutGroup>(row.gameObject);
            layout.padding = new RectOffset(6, 6, 6, 6);
            layout.spacing = 24f;
            layout.childAlignment = TextAnchor.MiddleCenter;
            layout.childControlWidth = true;
            layout.childControlHeight = true;
            layout.childForceExpandWidth = true;
            layout.childForceExpandHeight = true;
            layout.childScaleWidth = false;
            layout.childScaleHeight = false;

            List<ShopItem> cards = new();
            for (int i = 0; i < catalog.Length; i++)
            {
                cards.Add(BuildCard(ctx, row, catalog[i], shop));
            }

            card.Section("Preview · Shop Item Preview");
            ShopItem preview = BuildPreview(ctx, card, catalog[0]);
            return new Storefront(row, cards, preview);
        }

        private static ShopItem BuildCard(DemoSceneBuildContext ctx, RectTransform row, ShopItemData item,
            ShopComponent shop)
        {
            RectTransform card = ctx.UiChild(row, "Item · " + item.nameItem);

            // WHY: the glow is a sibling drawn before the background, so it shows as an outline around the card.
            Image glow = DemoSceneKit.Image(ctx, card, "Equipped Glow", Accent, true, false);
            DemoSceneKit.Stretch(glow.rectTransform, -5f);
            Image background = DemoSceneKit.Image(ctx, card, "Background", CardColor, true, false);
            DemoSceneKit.Stretch(background.rectTransform);

            Image picture = DemoSceneKit.Image(ctx, card, "Picture", Color.white, false, false);
            picture.sprite = item.sprite;
            picture.preserveAspect = true;
            DemoSceneKit.Anchor(picture.rectTransform, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f),
                new Vector2(0f, -14f), new Vector2(160f, 160f));

            TMP_Text name = DemoSceneKit.Text(ctx, card, "Name", item.nameItem, 26f, DemoPalette.Text,
                TextAlignmentOptions.Center, FontStyles.Bold);
            DemoSceneKit.SetAnchors(name.rectTransform, new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(10f, -212f),
                new Vector2(-10f, -180f));
            name.textWrappingMode = TextWrappingModes.NoWrap;
            name.overflowMode = TextOverflowModes.Ellipsis;

            TMP_Text kind = DemoSceneKit.Text(ctx, card, "Kind", KindOf(item), 15f, DemoPalette.Muted,
                TextAlignmentOptions.Center);
            DemoSceneKit.SetAnchors(kind.rectTransform, new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(10f, -236f),
                new Vector2(-10f, -214f));

            PriceButton price = BuildPriceButton(ctx, card, item);

            ShopItem shopItem = ctx.Ensure<ShopItem>(card.gameObject);
            ctx.Set(shopItem, "_textName", name);
            ctx.Set(shopItem, "_textDescription", null);
            ctx.Set(shopItem, "_textPrice", null);
            ctx.Set(shopItem, "_imageItem", picture);
            ctx.Set(shopItem, "_imageIco", null);
            ctx.Set(shopItem, "buttonPrice", price.View);
            ctx.Set(shopItem, "buttonBuy", price.Button);
            // WHY: NoCode outline - Shop.PropagateSelectionVisual calls ShopItem.Select, whose events toggle the glow.
            // The events have no initializer, so a freshly added ShopItem may not have deserialized them yet.
            shopItem.OnSelectItem ??= new UnityEvent();
            shopItem.OnDeselectItem ??= new UnityEvent();
            DemoWiring.AddBool(shopItem.OnSelectItem, glow.gameObject.SetActive, true);
            DemoWiring.AddBool(shopItem.OnDeselectItem, glow.gameObject.SetActive, false);

            ShopPurchaseButtonView view = ctx.Ensure<ShopPurchaseButtonView>(card.gameObject);
            ctx.Set(view, "_shop", shop);
            ctx.Set(view, "_item", shopItem);
            ctx.Set(view, "_buttonPrice", price.View);
            ctx.Set(view, "_button", price.Button);
            ctx.Set(view, "_disableButtonWhenUnaffordable", true);

            glow.gameObject.SetActive(false);
            return shopItem;
        }

        private static PriceButton BuildPriceButton(DemoSceneBuildContext ctx, RectTransform card, ShopItemData item)
        {
            RectTransform rect = ctx.UiChild(card, "Price Button");
            DemoSceneKit.SetAnchors(rect, new Vector2(0f, 0f), new Vector2(1f, 0f), new Vector2(16f, 16f),
                new Vector2(-16f, 72f));
            Image image = ctx.Ensure<Image>(rect.gameObject);
            DemoSceneKit.StyleImage(image, Accent, true, true);
            Button button = ctx.Ensure<Button>(rect.gameObject);
            button.interactable = true;
            button.transition = Selectable.Transition.ColorTint;
            button.targetGraphic = image;
            ColorBlock colors = ColorBlock.defaultColorBlock;
            colors.normalColor = Color.white;
            colors.highlightedColor = Color.white;
            colors.selectedColor = Color.white;
            colors.pressedColor = new Color(0.85f, 0.85f, 0.9f, 1f);
            colors.disabledColor = new Color(0.42f, 0.42f, 0.48f, 0.65f);
            colors.colorMultiplier = 1f;
            button.colors = colors;

            Image coin = DemoSceneKit.Image(ctx, rect, "Coin", DemoPalette.Ink, false, false);
            coin.sprite = DemoSceneAssets.Ring;
            coin.preserveAspect = true;
            DemoSceneKit.Anchor(coin.rectTransform, new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), new Vector2(0f, 0.5f),
                new Vector2(16f, 0f), new Vector2(26f, 26f));

            string initial = item.price > 0 ? item.price.ToString() : "Equip";
            TMP_Text label = DemoSceneKit.Text(ctx, rect, "Label", initial, 24f, DemoPalette.Ink,
                TextAlignmentOptions.Center, FontStyles.Bold);
            DemoSceneKit.SetAnchors(label.rectTransform, Vector2.zero, Vector2.one, new Vector2(36f, 4f),
                new Vector2(-12f, -4f));
            label.textWrappingMode = TextWrappingModes.NoWrap;
            label.enableAutoSizing = true;
            label.fontSizeMin = 14f;
            label.fontSizeMax = 24f;

            ButtonPrice view = ctx.Ensure<ButtonPrice>(rect.gameObject);
            TMP_Text[] labels = { label };
            ctx.Set(view, "_textPrice", labels);
            ctx.Set(view, "_textButton", labels);
            ctx.Set(view, "_visual.buy", new[] { coin.gameObject });
            ctx.Set(view, "_visual.select", Array.Empty<GameObject>());
            ctx.Set(view, "_visual.selected", Array.Empty<GameObject>());
            ctx.Set(view, "_visual.unaffordable", Array.Empty<GameObject>());
            ctx.Set(view, "_price", Mathf.Max(0, item.price));
            ctx.Set(view, "_textPrice_0", false);
            ctx.Set(view, "_textButtonAndPrice", true);
            ctx.Set(view, "_type", (int)(item.price > 0 ? ButtonPrice.ButtonType.Buy : ButtonPrice.ButtonType.Select));
            ctx.Set(view, "_textBuy", string.Empty);
            ctx.Set(view, "_textSelect", "Equip");
            ctx.Set(view, "_textSelected", "Equipped");
            ctx.Set(view, "_textUnaffordable", "Buy");
            ctx.Set(view, "_editorView", true);

            coin.gameObject.SetActive(item.price > 0);
            return new PriceButton(button, view);
        }

        private static ShopItem BuildPreview(DemoSceneBuildContext ctx, DemoPanel card, ShopItemData first)
        {
            RectTransform row = card.Row("Preview Slot", 96f);
            Image background = ctx.Ensure<Image>(row.gameObject);
            DemoSceneKit.StyleImage(background, PreviewColor, true, false);

            Image picture = DemoSceneKit.Image(ctx, row, "Picture", Color.white, false, false);
            picture.sprite = first.sprite;
            picture.preserveAspect = true;
            DemoSceneKit.Anchor(picture.rectTransform, new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), new Vector2(0f, 0.5f),
                new Vector2(14f, 0f), new Vector2(76f, 76f));

            TMP_Text name = DemoSceneKit.Text(ctx, row, "Name", first.nameItem, 28f, DemoPalette.Text,
                TextAlignmentOptions.BottomLeft, FontStyles.Bold);
            DemoSceneKit.SetAnchors(name.rectTransform, new Vector2(0f, 0.5f), new Vector2(1f, 1f), new Vector2(106f, 0f),
                new Vector2(-16f, -10f));

            TMP_Text caption = DemoSceneKit.Text(ctx, row, "Caption",
                "Shop Item Preview slot · Shop.ShowPreview follows the last bought or equipped item", 16f,
                DemoPalette.Muted, TextAlignmentOptions.TopLeft);
            DemoSceneKit.SetAnchors(caption.rectTransform, new Vector2(0f, 0f), new Vector2(1f, 0.5f), new Vector2(106f, 8f),
                new Vector2(-16f, -2f));

            ShopItem preview = ctx.Ensure<ShopItem>(row.gameObject);
            ctx.Set(preview, "_textName", name);
            ctx.Set(preview, "_imageItem", picture);
            ctx.Set(preview, "buttonPrice", null);
            ctx.Set(preview, "buttonBuy", null);
            return preview;
        }

        private static Controls BuildControls(DemoFrame frame, Money money, ShopDemoController controller,
            ShopItemData[] catalog, ShopItemData saleItem)
        {
            DemoPanel panel = frame.Panel("Controls", DemoPanelPlacement.Right, 440f);
            DemoLogFeed log = frame.Log;

            panel.Section("Wallet · Money (NoCode)");
            TMP_Text balance = panel.BigLabel(StartBalance.ToString("0") + " coins");
            Button[] wallet = panel.ButtonRow("+25", "+100", "Empty");
            DemoWiring.AddFloat(wallet[0].onClick, money.Add, 25f);
            DemoWiring.AddString(wallet[0].onClick, log.Log, "money.Add(25) -> TextMoney updates");
            DemoWiring.AddFloat(wallet[1].onClick, money.Add, 100f);
            DemoWiring.AddString(wallet[1].onClick, log.Log, "money.Add(100) -> TextMoney updates");
            DemoWiring.AddFloat(wallet[2].onClick, money.SetCurrentMoney, 0f);
            DemoWiring.AddString(wallet[2].onClick, log.Log, "money.SetCurrentMoney(0) -> priced cards turn Unaffordable");

            panel.Section("Typed purchase · TryBuy");
            string[] names = new string[catalog.Length];
            for (int i = 0; i < catalog.Length; i++)
            {
                names[i] = catalog[i].nameItem;
            }

            Button[] tryBuy = panel.ButtonRow(names);
            for (int i = 0; i < catalog.Length; i++)
            {
                DemoWiring.AddObject(tryBuy[i].onClick, controller.TryBuy, catalog[i]);
            }

            TMP_Text status = ShopNpcBuildSupport.CompactValue(panel, "Last status", "-");
            TMP_Text equipped = ShopNpcBuildSupport.CompactValue(panel, "Equipped", "-");
            TMP_Text owned = ShopNpcBuildSupport.CompactValue(panel, "Owned", "0 / " + catalog.Length);

            panel.Section("Runtime price · SetRuntimePrice");
            TMP_Text salePrice = ShopNpcBuildSupport.CompactValue(panel, "Price of " + saleItem.nameItem,
                saleItem.price.ToString());
            Button[] sale = panel.ButtonRow("Sale -50%", "End sale");
            DemoWiring.AddVoid(sale[0].onClick, controller.StartSale);
            DemoWiring.AddVoid(sale[1].onClick, controller.EndSale);

            panel.Section("Save · SaveProvider");
            Button reset = panel.Button("Reset shop + wallet");
            DemoWiring.AddVoid(reset.onClick, controller.ResetDemo);

            return new Controls(balance, status, equipped, owned, salePrice);
        }

        private static void ConfigureShop(DemoSceneBuildContext ctx, ShopComponent shop, Money money,
            ShopDemoController controller, Storefront storefront)
        {
            ctx.Set(shop, "_purchaseFlow", ShopPurchaseFlow.BuyAndEquip);
            ctx.Set(shop, "_shopItemPreview", storefront.Preview);
            ctx.Set(shop, "_shopItems", storefront.Cards);
            ctx.Set(shop, "_container", storefront.Row);
            // WHY: the cards are authored, so Shop must not spawn rows (and must not deactivate a scene template).
            ctx.Set(shop, "_prefab", null);
            ctx.Set(shop, "_autoSpawnItems", false);
            ctx.Set(shop, "_autoSubscribe", true);
            ctx.Set(shop, "_propagateSelectionVisual", true);
            ctx.Set(shop, "moneySpendSource", money.gameObject);

            ctx.OwnListeners(shop, shop.OnPurchasedId, controller);
            ctx.OwnListeners(shop, shop.OnPurchaseFailedId, controller);
            ctx.OwnListeners(shop, shop.OnSelectId, controller);
            ctx.OwnListeners(shop, shop.OnShopChanged, controller);
            DemoWiring.AddDynamic(shop.OnPurchasedId, controller.OnItemPurchased);
            DemoWiring.AddDynamic(shop.OnPurchaseFailedId, controller.OnPurchaseFailed);
            DemoWiring.AddDynamic(shop.OnSelectId, controller.OnItemSelected);
            DemoWiring.AddVoid(shop.OnShopChanged, controller.RefreshLabels);
            ctx.MarkModified(shop);
        }

        private static void ConfigureWallet(DemoSceneBuildContext ctx, Money money, TMP_Text balance)
        {
            // WHY: the legacy money text was retired with the old canvas; TextMoney below is the reactive view now.
            ctx.Set(money, "t_money", Array.Empty<TMP_Text>());
            ctx.MarkModified(money);

            TextMoney textMoney = ctx.Ensure<TextMoney>(balance.gameObject);
            ctx.Set(textMoney, "text", balance);
            ctx.Set(textMoney, "_moneySource", money);
            ctx.Set(textMoney, "_moneySaveKey", string.Empty);
            ctx.Set(textMoney, "startAdd", string.Empty);
            ctx.Set(textMoney, "endAdd", " coins");
        }

        private static string CatalogSummary(ShopItemData[] catalog)
        {
            string[] parts = new string[catalog.Length];
            for (int i = 0; i < catalog.Length; i++)
            {
                string price = catalog[i].price > 0 ? catalog[i].price + " coins" : "free, equips at once";
                parts[i] = catalog[i].nameItem + " (" + price + ")";
            }

            return string.Join(", ", parts);
        }

        private static string KindOf(ShopItemData item)
        {
            if (!item.isSinglePurchase)
            {
                return "consumable";
            }

            return item.price > 0 ? "one-time unlock" : "free starter";
        }

        private static string ReadString(Object owner, string path, string fallback)
        {
            SerializedProperty property = new SerializedObject(owner).FindProperty(path);
            return property != null && property.propertyType == SerializedPropertyType.String &&
                   !string.IsNullOrEmpty(property.stringValue)
                ? property.stringValue
                : fallback;
        }

        private static void Step(string step, Action action)
        {
            DemoSceneKit.Step(Scene, step, action);
        }

        private static T Step<T>(string step, Func<T> action)
        {
            return DemoSceneKit.Step(Scene, step, action);
        }

        private readonly struct PriceButton
        {
            public PriceButton(Button button, ButtonPrice view)
            {
                Button = button;
                View = view;
            }

            public Button Button { get; }
            public ButtonPrice View { get; }
        }

        private sealed class Storefront
        {
            public Storefront(RectTransform row, List<ShopItem> cards, ShopItem preview)
            {
                Row = row;
                Cards = cards;
                Preview = preview;
            }

            public RectTransform Row { get; }
            public List<ShopItem> Cards { get; }
            public ShopItem Preview { get; }
        }

        private sealed class Controls
        {
            public Controls(TMP_Text balance, TMP_Text status, TMP_Text equipped, TMP_Text owned, TMP_Text salePrice)
            {
                Balance = balance;
                Status = status;
                Equipped = equipped;
                Owned = owned;
                SalePrice = salePrice;
            }

            public TMP_Text Balance { get; }
            public TMP_Text Status { get; }
            public TMP_Text Equipped { get; }
            public TMP_Text Owned { get; }
            public TMP_Text SalePrice { get; }
        }
    }
}
