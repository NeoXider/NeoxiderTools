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
    ///     Authors the Neo.Bonus reward loop into BonusRewardsDemo.unity from the module's own components, wired in the
    ///     scene without a demo script: a <see cref="Collection" /> of six pizzas (data assets in Data/Bonus/Pizzas) shown by
    ///     a <see cref="CollectionVisualManager" /> through <see cref="ItemCollection" /> slot views and an
    ///     <see cref="ItemCollectionInfo" /> details row, a <see cref="Box" /> chest that grants a random locked pizza, a
    ///     <see cref="CooldownReward" /> gift that fills the chest, the legacy <see cref="TimeReward" /> next to it for
    ///     comparison and a session-only <see cref="Money" /> wallet. Every save key the scene writes contains the
    ///     NeoDemo. namespace (after the component's fixed prefix where it has one), so the demo never reads or writes a
    ///     game's real saves.
    /// </summary>
    internal static class BonusRewardsDemoBuilder
    {
        internal const string ScenePath = "Scenes/Bonuses/BonusRewardsDemo.unity";
        internal const string SaveNamespace = "NeoDemo.";
        internal const string CollectionSavePrefix = "NeoDemo.Bonus.Pizza.";
        internal const string ChestSaveName = "NeoDemo.Bonus.Chest.";
        internal const string GiftAddKey = "NeoDemo.Bonus.Gift";
        internal const string CoinsAddKey = "NeoDemo.Bonus.Coins";
        internal const string WalletSaveKey = "NeoDemo.Bonus.Wallet";
        internal const string ClaimGiftLabel = "Claim gift";
        internal const string GiftNowLabel = "Gift now";
        internal const string RestartGiftLabel = "Restart";
        internal const string ClaimCoinsLabel = "Claim coins";
        internal const string CoinsNowLabel = "Coins now";
        internal const string AddProgressLabel = "+100 progress";
        internal const string OpenChestLabel = "Open chest";
        internal const string UnlockAllLabel = "Unlock all";
        internal const string ResetCollectionLabel = "Reset collection";
        internal const string ResetDemoLabel = "Reset demo progress";
        internal const float GiftCooldownSeconds = 10f;
        internal const float CoinsCooldownSeconds = 20f;
        internal const float CoinsPerClaim = 25f;
        internal const float ChestStep = 100f;
        internal const float ChestPrice = 300f;

        private const string Scene = "BonusRewardsDemo";
        private const string CatalogFolder = "Data/Bonus/Pizzas/";
        private const string ChestSpritePath = "Sprites/UI/AnimationFlyChest.png";

        /// <summary>Sprites/UI/White-Icon/Locker-Locked@2x.png of the package (GUIDs survive the UPM install, paths do not).</summary>
        private const string LockIconGuid = "c4d0c6e1f186f2345bc4f69af45e7d32";

        /// <summary>Sprites/UI/White-Icon/Question@2x.png of the package.</summary>
        private const string QuestionIconGuid = "6180f8a297df6894d8ba919e5d1f92af";

        private const float ContentWidth = 880f;
        private const float RewardsWidth = 440f;
        private const float InfoCardWidth = 400f;
        private const float SlotRowHeight = 172f;
        private const float DetailsRowHeight = 84f;
        private const float ChestRowHeight = 104f;
        private const float TimerUpdateInterval = 0.1f;

        private static readonly Color Accent = new(1f, 0.66f, 0.30f);
        private static readonly Color SlotColor = new(0.20f, 0.21f, 0.29f, 1f);
        private static readonly Color PreviewColor = new(1f, 1f, 1f, 0.06f);
        private static readonly Color LockedShade = new(0.05f, 0.05f, 0.09f, 0.62f);
        private static readonly Color LockIconColor = new(1f, 1f, 1f, 0.92f);
        private static readonly Color RareColor = new(0.40f, 0.74f, 1f);
        private static readonly Color EpicColor = new(0.80f, 0.52f, 1f);
        private static readonly Color LegendaryColor = new(1f, 0.78f, 0.30f);

        /// <summary>The catalog in index order; the save keys are index based, so keep the order when adding pizzas.</summary>
        internal static readonly PizzaSpec[] Pizzas =
        {
            new("PizzaDiavola", "Diavola", "Spicy salami, chili and sweet peppers.", ItemRarity.Rare,
                "Sprites/Clicker/Resource 1@4.png"),
            new("PizzaMediterranea", "Mediterranea", "Red onion, olives, tomato and basil.", ItemRarity.Common,
                "Sprites/Clicker/Resource 2@4.png"),
            new("PizzaVegetariana", "Vegetariana", "Broccoli, olives, tomato and peppers.", ItemRarity.Common,
                "Sprites/Clicker/Resource 3@4.png"),
            new("PizzaHawaiian", "Hawaiian", "Ham and pineapple: the most argued-about pizza.", ItemRarity.Epic,
                "Sprites/Clicker/Resource 4@4.png"),
            new("PizzaPepperoni", "Pepperoni", "Classic pepperoni with a basil leaf.", ItemRarity.Common,
                "Sprites/Clicker/Resource 5@4.png"),
            new("PizzaFunghi", "Funghi", "Mushrooms, tomato, olives and fresh herbs.", ItemRarity.Legendary,
                "Sprites/Clicker/Resource 6@4.png")
        };

        [DemoSceneBuilder(ScenePath)]
        public static void Build(DemoSceneBuildContext ctx)
        {
            ItemCollectionData[] catalog = Step("ensure the pizza catalog in " + CatalogFolder, EnsureCatalog);
            ModuleDemoSceneInfo info = Step("ensure the info service",
                () => ctx.FindOrCreate<ModuleDemoSceneInfo>("Services", "Demo Info"));
            Collection collection = Step("configure the collection", () => ConfigureCollection(ctx, catalog));
            Money wallet = Step("configure the wallet", () => BonusDemoStage.Wallet(ctx, WalletSaveKey));
            CooldownReward gift = Step("configure the gift timer (CooldownReward)", () => ConfigureGift(ctx));
            // WHY: the scene compares CooldownReward with the legacy TimeReward that existing scenes still use; only
            // the TimeReward-typed lines silence CS0618, so any other [Obsolete] API used here still warns.
#pragma warning disable CS0618
            TimeReward coins = Step("configure the legacy coin timer (TimeReward)", () => ConfigureLegacyCoins(ctx));
#pragma warning restore CS0618

            DemoFrame frame = Step("author the frame", () => DemoSceneKit.Frame(ctx,
                new DemoFrameOptions("Neo.Bonus · Rewards & Collection", Accent)
                {
                    ContentWidth = ContentWidth,
                    ContentOffsetY = 0f,
                    LogLines = 4
                }));

            Step("author the collection card", () => BuildCollectionCard(ctx, frame, collection, catalog));
            Box box = Step("author the chest (Box)", () => BuildChest(ctx, frame, collection));
            Step("author the rewards card",
                () => BuildRewards(ctx, frame, new RewardParts(collection, box, gift, coins, wallet)));
            Step("author the info card", () => frame.InfoCard(info, InfoContent()));
        }

        private static ItemCollectionData[] EnsureCatalog()
        {
            ItemCollectionData[] catalog = new ItemCollectionData[Pizzas.Length];
            for (int i = 0; i < Pizzas.Length; i++)
            {
                PizzaSpec spec = Pizzas[i];
                int index = i;
                Sprite sprite = DemoSceneAssets.LoadDemoAsset<Sprite>(spec.SpritePath);
                catalog[i] = DemoSceneAssets.EnsureAsset<ItemCollectionData>(CatalogFolder + spec.AssetName + ".asset",
                    data => ConfigurePizza(data, spec, sprite, index));
            }

            return catalog;
        }

        private static void ConfigurePizza(ItemCollectionData data, PizzaSpec spec, Sprite sprite, int index)
        {
            SerializedObject serialized = new(data);
            BonusDemoStage.Require(serialized, "_itemName", Scene).stringValue = spec.Name;
            BonusDemoStage.Require(serialized, "_description", Scene).stringValue = spec.Description;
            BonusDemoStage.Require(serialized, "_sprite", Scene).objectReferenceValue = sprite;
            BonusDemoStage.Require(serialized, "_itemType", Scene).intValue = index;
            BonusDemoStage.Require(serialized, "_rarity", Scene).intValue = (int)spec.Rarity;
            BonusDemoStage.Require(serialized, "_category", Scene).intValue = 0;
            serialized.ApplyModifiedPropertiesWithoutUndo();
        }

        private static Collection ConfigureCollection(DemoSceneBuildContext ctx, ItemCollectionData[] catalog)
        {
            Collection collection = ctx.FindOrCreate<Collection>("Services", "Pizza Collection");
            ctx.Set(collection, "_saveKeyPrefix", CollectionSavePrefix);
            ctx.Set(collection, "_randomPrize", true);
            ctx.Set(collection, "_itemCollectionDatas", catalog);
            ctx.Set(collection, "_enabledItems", new bool[catalog.Length]);
            ctx.Set(collection, "_dontDestroyOnLoad", false);
            // WHY: every view references this collection explicitly, so the demo never claims the global Collection.I.
            ctx.Set(collection, "_setInstanceOnAwake", false);
            return collection;
        }

        private static CooldownReward ConfigureGift(DemoSceneBuildContext ctx)
        {
            CooldownReward gift = ctx.FindOrCreate<CooldownReward>("Services", "Gift (CooldownReward)");
            ctx.Set(gift, "_cooldownSeconds", GiftCooldownSeconds);
            ctx.Set(gift, "_updateInterval", TimerUpdateInterval);
            ctx.Set(gift, "_rewardAvailableOnStart", true);
            ctx.Set(gift, "_maxRewardsPerTake", 1);
            ctx.Set(gift, "_addKey", GiftAddKey);
            ctx.Set(gift, "_startTakeReward", false);
            ctx.Set(gift, "_autoClaim", false);
            ctx.Set(gift, "_startTimerOnStart", true);
            ctx.Set(gift, "_saveTimeOnTakeReward", true);
            // WHY: CooldownReward saves under LastRewardTime<Add Key>; the inherited TimerObject key is unused but would
            // otherwise show the library default in the Inspector.
            ctx.Set(gift, "saveKey", GiftAddKey);
            return gift;
        }

#pragma warning disable CS0618
        private static TimeReward ConfigureLegacyCoins(DemoSceneBuildContext ctx)
        {
            TimeReward coins = ctx.FindOrCreate<TimeReward>("Services", "Coins (TimeReward, legacy)");
#pragma warning restore CS0618
            ctx.Set(coins, "secondsToWaitForReward", CoinsCooldownSeconds);
            ctx.Set(coins, "updateTime", TimerUpdateInterval);
            ctx.Set(coins, "_rewardAvailableOnStart", true);
            ctx.Set(coins, "_maxRewardsPerTake", 1);
            ctx.Set(coins, "_addKey", CoinsAddKey);
            ctx.Set(coins, "startTakeReward", false);
            ctx.Set(coins, "_startTimerOnStart", true);
            ctx.Set(coins, "_saveTimeOnTakeReward", true);
            return coins;
        }

        private static void BuildCollectionCard(DemoSceneBuildContext ctx, DemoFrame frame, Collection collection,
            ItemCollectionData[] catalog)
        {
            DemoPanel card = frame.Content;
            DemoLogFeed log = frame.Log;
            card.Section("1 · Collection · six pizzas to unlock");
            card.Note("Open the chest to unlock a random pizza, then click an unlocked slot for its details.");

            RectTransform grid = card.Row("Pizza Slots", SlotRowHeight);
            HorizontalLayoutGroup layout = ctx.Ensure<HorizontalLayoutGroup>(grid.gameObject);
            layout.padding = new RectOffset(0, 0, 0, 0);
            layout.spacing = 12f;
            layout.childAlignment = TextAnchor.MiddleCenter;
            layout.childControlWidth = true;
            layout.childControlHeight = true;
            layout.childForceExpandWidth = true;
            layout.childForceExpandHeight = true;
            layout.childScaleWidth = false;
            layout.childScaleHeight = false;

            Sprite lockIcon = PackageSprite(LockIconGuid, "Locker-Locked@2x.png");
            ItemCollection[] slots = new ItemCollection[catalog.Length];
            for (int i = 0; i < catalog.Length; i++)
            {
                slots[i] = BuildSlot(ctx, grid, catalog[i], Pizzas[i], collection, lockIcon);
            }

            CollectionVisualManager manager = ctx.Ensure<CollectionVisualManager>(grid.gameObject);
            ctx.Set(manager, "_items", slots);
            ctx.Set(manager, "_collection", collection);
            ctx.Set(manager, "_enableSetItem", true);
            ctx.Set(manager, "_dontDestroyOnLoad", false);
            // WHY: a manager left in the global slot by a game scene would make a second singleton destroy this row.
            ctx.Set(manager, "_setInstanceOnAwake", false);

            ItemCollectionInfo details = BuildDetails(ctx, card, collection);
            DemoWiring.AddDynamic(manager.OnSetItem, details.SetItemId);
            DemoWiring.AddString(manager.OnSetItem, log.Log,
                "CollectionVisualManager.OnSetItem › ItemCollectionInfo.SetItemId");

            DemoBarRow completion = card.BarRow("Completion · Collection.OnCompletionPercentageChanged", Accent, 0f, true);
            TMP_Text percentLabel = DemoSceneKit.Text(ctx, completion.Row, "Percent", "0%", 15f, DemoPalette.Text,
                TextAlignmentOptions.TopRight, FontStyles.Bold);
            DemoSceneKit.Anchor(percentLabel.rectTransform, new Vector2(1f, 1f), new Vector2(1f, 1f), new Vector2(1f, 1f),
                Vector2.zero, new Vector2(120f, 18f));
            DemoValueReadout percent = DemoSceneKit.Readout(ctx, percentLabel, "0%");

            Button[] buttons = card.ButtonRow(UnlockAllLabel, ResetCollectionLabel);
            DemoWiring.AddVoid(buttons[0].onClick, collection.UnlockAllItems);
            DemoWiring.AddString(buttons[0].onClick, log.Log, "Collection.UnlockAllItems()");
            DemoWiring.AddVoid(buttons[1].onClick, collection.ClearCollection);
            DemoWiring.AddString(buttons[1].onClick, log.Log, "Collection.ClearCollection() · every pizza locked again");

            DemoWiring.AddSetter(collection.OnCompletionPercentageChanged, completion.Fill, "fillAmount");
            DemoWiring.AddDynamic(collection.OnCompletionPercentageChanged, percent.SetFloat);
            DemoWiring.AddString(collection.OnItemAdded, log.Log, "Collection.OnItemAdded · a slot unlocks");
            DemoWiring.AddString(collection.OnGetItem, log.Log, "Collection.OnGetItem · the chest picked a locked pizza");
        }

        private static ItemCollection BuildSlot(DemoSceneBuildContext ctx, RectTransform grid, ItemCollectionData data,
            PizzaSpec spec, Collection collection, Sprite lockIcon)
        {
            RectTransform slot = ctx.UiChild(grid, "Slot · " + spec.Name);
            Image background = ctx.Ensure<Image>(slot.gameObject);
            DemoSceneKit.StyleImage(background, SlotColor, true, true);
            Button button = ctx.Ensure<Button>(slot.gameObject);
            button.interactable = true;
            button.transition = Selectable.Transition.ColorTint;
            button.targetGraphic = background;
            ColorBlock colors = ColorBlock.defaultColorBlock;
            colors.normalColor = Color.white;
            colors.highlightedColor = Color.white;
            colors.selectedColor = Color.white;
            colors.pressedColor = new Color(0.85f, 0.85f, 0.9f, 1f);
            colors.colorMultiplier = 1f;
            button.colors = colors;

            Image picture = DemoSceneKit.Image(ctx, slot, "Picture", Color.white, false, false);
            picture.sprite = data.Sprite;
            picture.preserveAspect = true;
            DemoSceneKit.Anchor(picture.rectTransform, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f),
                new Vector2(0f, -10f), new Vector2(92f, 92f));

            TMP_Text name = DemoSceneKit.Text(ctx, slot, "Name", spec.Name, 18f, DemoPalette.Text,
                TextAlignmentOptions.Center, FontStyles.Bold);
            DemoSceneKit.SetAnchors(name.rectTransform, new Vector2(0f, 0f), new Vector2(1f, 0f), new Vector2(6f, 28f),
                new Vector2(-6f, 52f));
            name.textWrappingMode = TextWrappingModes.NoWrap;
            name.overflowMode = TextOverflowModes.Ellipsis;

            TMP_Text rarity = DemoSceneKit.Text(ctx, slot, "Rarity", spec.Rarity.ToString(), 13f, RarityColor(spec.Rarity),
                TextAlignmentOptions.Center, FontStyles.Bold);
            DemoSceneKit.SetAnchors(rarity.rectTransform, new Vector2(0f, 0f), new Vector2(1f, 0f), new Vector2(6f, 8f),
                new Vector2(-6f, 26f));
            rarity.characterSpacing = 4f;

            Image shade = DemoSceneKit.Image(ctx, slot, "Locked", LockedShade, true, false);
            DemoSceneKit.Stretch(shade.rectTransform);
            Image icon = DemoSceneKit.Image(ctx, shade.transform, "Lock Icon", LockIconColor, false, false);
            icon.sprite = lockIcon;
            icon.preserveAspect = true;
            DemoSceneKit.Anchor(icon.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
                new Vector2(0.5f, 0.5f), new Vector2(0f, 24f), new Vector2(40f, 40f));

            ItemCollection view = ctx.Ensure<ItemCollection>(slot.gameObject);
            ctx.Set(view, "_button", button);
            ctx.Set(view, "_itemImage", picture);
            ctx.Set(view, "_collection", collection);
            ctx.Set(view, "_itemInfo", null);
            DemoWiring.AddBool(view.OnActive, shade.gameObject.SetActive, false);
            DemoWiring.AddBool(view.OnDeactivated, shade.gameObject.SetActive, true);

            // WHY: a fresh demo has nothing unlocked; the saved scene shows the state Play starts in.
            shade.gameObject.SetActive(true);
            return view;
        }

        private static ItemCollectionInfo BuildDetails(DemoSceneBuildContext ctx, DemoPanel card, Collection collection)
        {
            RectTransform row = card.Row("Pizza Details", DetailsRowHeight);
            Image background = ctx.Ensure<Image>(row.gameObject);
            DemoSceneKit.StyleImage(background, PreviewColor, true, false);

            Image picture = DemoSceneKit.Image(ctx, row, "Picture", Color.white, false, false);
            picture.sprite = PackageSprite(QuestionIconGuid, "Question@2x.png");
            picture.preserveAspect = true;
            DemoSceneKit.Anchor(picture.rectTransform, new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), new Vector2(0f, 0.5f),
                new Vector2(12f, 0f), new Vector2(64f, 64f));

            TMP_Text name = DemoSceneKit.Text(ctx, row, "Name", "Pick an unlocked pizza", 24f, DemoPalette.Text,
                TextAlignmentOptions.BottomLeft, FontStyles.Bold);
            DemoSceneKit.SetAnchors(name.rectTransform, new Vector2(0f, 0.5f), new Vector2(1f, 1f), new Vector2(92f, 0f),
                new Vector2(-16f, -8f));
            name.textWrappingMode = TextWrappingModes.NoWrap;
            name.overflowMode = TextOverflowModes.Ellipsis;

            TMP_Text description = DemoSceneKit.Text(ctx, row, "Description",
                "Locked slots cannot be selected: Enable Set Item gates OnSetItem.", 16f, DemoPalette.Muted,
                TextAlignmentOptions.TopLeft);
            DemoSceneKit.SetAnchors(description.rectTransform, new Vector2(0f, 0f), new Vector2(1f, 0.5f),
                new Vector2(92f, 6f), new Vector2(-16f, -2f));

            ItemCollectionInfo info = ctx.Ensure<ItemCollectionInfo>(row.gameObject);
            ctx.Set(info, "_collection", collection);
            ctx.Set(info, "_textName", name);
            ctx.Set(info, "_textDescription", description);
            ctx.Set(info, "_imageItem", picture);
            ctx.Set(info, "_textDefaultValue", string.Empty);
            ctx.Set(info, "_setNativeSize", false);
            return info;
        }

        private static Box BuildChest(DemoSceneBuildContext ctx, DemoFrame frame, Collection collection)
        {
            DemoPanel card = frame.Content;
            DemoLogFeed log = frame.Log;
            card.Section("2 · Box · fill the chest, open it for a pizza");

            RectTransform row = card.Row("Chest", ChestRowHeight);
            Image chest = DemoSceneKit.Image(ctx, row, "Chest Icon", Color.white, false, false);
            chest.sprite = DemoSceneAssets.LoadDemoAsset<Sprite>(ChestSpritePath);
            chest.preserveAspect = true;
            DemoSceneKit.Anchor(chest.rectTransform, new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), new Vector2(0f, 0.5f),
                new Vector2(4f, 0f), new Vector2(96f, 96f));

            TMP_Text caption = DemoSceneKit.Text(ctx, row, "Caption",
                "Box progress · +" + Format(ChestStep, "0") + " per gift, " + Format(ChestPrice, "0") +
                " opens the chest", 16f, DemoPalette.Muted, TextAlignmentOptions.BottomLeft);
            DemoSceneKit.SetAnchors(caption.rectTransform, new Vector2(0f, 0.5f), new Vector2(1f, 1f),
                new Vector2(118f, 14f), new Vector2(-128f, -8f));

            Image track = DemoSceneKit.Image(ctx, row, "Bar", DemoPalette.Track);
            DemoSceneKit.SetAnchors(track.rectTransform, new Vector2(0f, 0.5f), new Vector2(1f, 0.5f),
                new Vector2(118f, -11f), new Vector2(-128f, 11f));
            Image fill = DemoSceneKit.Image(ctx, track.transform, "Fill", Accent);
            // WHY: Box drives Image.fillAmount, which only shows on a Filled image with a plain sprite.
            fill.sprite = DemoSceneAssets.WhiteSquare;
            fill.type = Image.Type.Filled;
            fill.fillMethod = Image.FillMethod.Horizontal;
            fill.fillOrigin = (int)Image.OriginHorizontal.Left;
            fill.fillAmount = 0f;
            DemoSceneKit.SetAnchors(fill.rectTransform, Vector2.zero, Vector2.one, new Vector2(2f, 2f),
                new Vector2(-2f, -2f));

            TMP_Text value = DemoSceneKit.Text(ctx, row, "Value", "0/" + Format(ChestPrice, "0"), 20f, DemoPalette.Text,
                TextAlignmentOptions.TopLeft, FontStyles.Bold);
            DemoSceneKit.SetAnchors(value.rectTransform, new Vector2(0f, 0f), new Vector2(1f, 0.5f), new Vector2(118f, 4f),
                new Vector2(-128f, -16f));

            Image prizeSlot = DemoSceneKit.Image(ctx, row, "Prize Slot", PreviewColor, true, false);
            DemoSceneKit.Anchor(prizeSlot.rectTransform, new Vector2(1f, 0.5f), new Vector2(1f, 0.5f),
                new Vector2(1f, 0.5f), new Vector2(-4f, 0f), new Vector2(96f, 96f));
            TMP_Text unknown = DemoSceneKit.Text(ctx, prizeSlot.transform, "Unknown", "?", 44f, DemoPalette.Muted,
                TextAlignmentOptions.Center, FontStyles.Bold);
            DemoSceneKit.Stretch(unknown.rectTransform);
            Image prize = DemoSceneKit.Image(ctx, prizeSlot.transform, "Prize", Color.white, false, false);
            prize.preserveAspect = true;
            DemoSceneKit.Stretch(prize.rectTransform, 6f);
            // WHY: Box.OnEnable hides the prize the same way; it grows in when a pizza is taken.
            prize.rectTransform.localScale = Vector3.zero;

            Box box = ctx.Ensure<Box>(row.gameObject);
            ctx.Set(box, "_saveName", ChestSaveName);
            ctx.Set(box, "_collection", collection);
            ctx.Set(box, "_boxImage", null);
            ctx.Set(box, "_bar", fill);
            ctx.Set(box, "_textProgress", null);
            ctx.Set(box, "_textMaxProgress", null);
            ctx.Set(box, "_textProgressMaxProgress", value);
            ctx.Set(box, "_animItem", prize.gameObject);
            ctx.Set(box, "_itemPrize", prize);
            ctx.Set(box, "_addProgress", ChestStep);
            ctx.Set(box, "_maxProgress", ChestPrice);
            ctx.Set(box, "_progress", 0f);

            Button[] buttons = card.ButtonRow(AddProgressLabel, OpenChestLabel);
            DemoWiring.AddVoid(buttons[0].onClick, box.AddProgress);
            DemoWiring.AddString(buttons[0].onClick, log.Log, "Box.AddProgress() · +" + Format(ChestStep, "0"));
            DemoWiring.AddVoid(buttons[1].onClick, box.TakePrize);
            // WHY: TakePrize is silent when every pizza is unlocked (Collection.GetPrize returns null), so each click
            // is logged and the note below says how to play again.
            DemoWiring.AddString(buttons[1].onClick, log.Log, "Box.TakePrize()");
            card.Note("A full collection grants nothing: Reset collection to play again.");
            // WHY: the chest starts empty; Box.OnChangeProgress enables the button once a full price is stored.
            buttons[1].interactable = false;
            DemoWiring.AddSetter(box.OnChangeProgress, buttons[1], "interactable");
            DemoWiring.AddString(box.OnTakePrize, log.Log,
                "Box.OnTakePrize · " + Format(ChestPrice, "0") + " progress paid for a pizza");
            return box;
        }

        private static void BuildRewards(DemoSceneBuildContext ctx, DemoFrame frame, RewardParts parts)
        {
            DemoPanel panel = frame.Panel("Rewards", DemoPanelPlacement.Right, RewardsWidth);
            DemoLogFeed log = frame.Log;

            panel.Section("3 · CooldownReward · free gift");
            panel.Note("A gift every " + Format(GiftCooldownSeconds, "0") + " s adds " + Format(ChestStep, "0") +
                       " to the chest. A claim restarts the full cooldown.", 42f);
            DemoValueRow giftRow = panel.ValueRow("Next gift in", "0.0 s");
            DemoValueReadout giftTimer = DemoSceneKit.Readout(ctx, giftRow.Value, "0.0", string.Empty, " s");
            Button[] giftButtons = panel.ButtonRow(ClaimGiftLabel, GiftNowLabel, RestartGiftLabel);
            DemoWiring.AddVoid(giftButtons[0].onClick, parts.Gift.Take);
            DemoWiring.AddVoid(giftButtons[1].onClick, parts.Gift.SetRewardAvailableNow);
            DemoWiring.AddString(giftButtons[1].onClick, log.Log, "CooldownReward.SetRewardAvailableNow()");
            DemoWiring.AddVoid(giftButtons[2].onClick, parts.Gift.RestartTime);
            DemoWiring.AddString(giftButtons[2].onClick, log.Log,
                "CooldownReward.RestartTime() · " + Format(GiftCooldownSeconds, "0") + " s again");
            DemoWiring.AddDynamic(parts.Gift.RemainingTime.OnChanged, giftTimer.SetFloat);
            DemoWiring.AddVoid(parts.Gift.OnRewardClaimed, parts.Box.AddProgress);
            DemoWiring.AddString(parts.Gift.OnRewardClaimed, log.Log, "CooldownReward.OnRewardClaimed › Box.AddProgress()");
            DemoWiring.AddString(parts.Gift.OnRewardAvailable, log.Log, "CooldownReward.OnRewardAvailable · gift ready");

            panel.Section("4 · TimeReward · legacy");
            panel.Note("Legacy, kept for old scenes: it keeps the unclaimed remainder. New scenes use CooldownReward. +" +
                       Format(CoinsPerClaim, "0") + " coins every " + Format(CoinsCooldownSeconds, "0") + " s.", 62f);
            DemoValueRow coinsRow = panel.ValueRow("Next coins in", "0.0 s");
            DemoValueReadout coinsTimer = DemoSceneKit.Readout(ctx, coinsRow.Value, "0.0", string.Empty, " s");
            Button[] coinButtons = panel.ButtonRow(ClaimCoinsLabel, CoinsNowLabel);
            DemoWiring.AddVoid(coinButtons[0].onClick, parts.Coins.Take);
            DemoWiring.AddVoid(coinButtons[1].onClick, parts.Coins.SetRewardAvailableNow);
            DemoWiring.AddString(coinButtons[1].onClick, log.Log, "TimeReward.SetRewardAvailableNow()");
            DemoWiring.AddDynamic(parts.Coins.OnTimeUpdated, coinsTimer.SetFloat);
            DemoWiring.AddFloat(parts.Coins.OnRewardClaimed, parts.Wallet.Add, CoinsPerClaim);
            DemoWiring.AddString(parts.Coins.OnRewardClaimed, log.Log,
                "TimeReward.OnRewardClaimed › Money.Add(" + Format(CoinsPerClaim, "0") + ")");
            TMP_Text balance = panel.ValueLabel("Coins (Money)", "0");
            ctx.Set(parts.Wallet, "t_money", new[] { balance });

            panel.Section("Demo saves · " + SaveNamespace + "*");
            panel.Note("Every key holds " + SaveNamespace + " (e.g. " + ChestSaveName + "progress), so a game's own " +
                       "saves stay untouched.", 62f);
            Button reset = panel.Button(ResetDemoLabel);
            DemoWiring.AddVoid(reset.onClick, parts.Collection.ClearCollection);
            DemoWiring.AddFloat(reset.onClick, DemoWiring.Setter<float>(parts.Box, "progress"), 0f);
            // WHY: the progress setter only redraws; a zero step raises OnChangeProgress so the Open chest button follows.
            DemoWiring.AddFloat(reset.onClick, parts.Box.AddProgress, 0f);
            DemoWiring.AddVoid(reset.onClick, parts.Gift.SetRewardAvailableNow);
            DemoWiring.AddVoid(reset.onClick, parts.Coins.SetRewardAvailableNow);
            DemoWiring.AddFloat(reset.onClick, parts.Wallet.SetCurrentMoney, 0f);
            DemoWiring.AddString(reset.onClick, log.Log,
                "Reset · pizzas locked, chest empty, both timers ready, wallet 0");
        }

        private static DemoInfoContent InfoContent()
        {
            return new DemoInfoContent
            {
                Title = "Neo.Bonus · Rewards & Collection",
                Purpose = "A reward loop made only of Bonus components and wired in the scene: CooldownReward gives a " +
                          "timed gift, the gift fills the Box chest, a full chest grants a random locked pizza from the " +
                          "Collection, and CollectionVisualManager redraws the ItemCollection slots.",
                RuntimeApi = "collection.TryAddItem(i) / GetPrize() / ClearCollection() / Set; box.AddProgress() / " +
                             "TakePrize() / CheckProgress; gift.TakeReward() / CanTakeReward() / RemainingTime / " +
                             "SetRewardAvailableNow().",
                SceneWorkflow = "[Demo] Services: Pizza Collection (catalog in Data/Bonus/Pizzas), Gift (CooldownReward), " +
                                "Coins (TimeReward, legacy), Wallet (Money). The slot row holds the " +
                                "CollectionVisualManager, the chest row the Box, the details row the ItemCollectionInfo.",
                Verification = "Play: Claim gift, Gift now, Claim gift until the chest shows " + Format(ChestPrice, "0") +
                               "/" + Format(ChestPrice, "0") + ", then Open chest: a random pizza unlocks and can be " +
                               "selected. Reset demo progress locks every pizza, empties the chest, makes both " +
                               "timers ready and zeroes the wallet; every key it writes stays under " + SaveNamespace +
                               "*.",
                Hints = new[]
                {
                    "Claim gift › CooldownReward.Take › OnRewardClaimed › Box.AddProgress",
                    "Open chest › Box.TakePrize › Collection.GetPrize › the slot unlocks",
                    "Unlocked slot › CollectionVisualManager.OnSetItem › ItemCollectionInfo.SetItemId"
                },
                Corner = ModuleDemoSceneInfo.Corner.TopLeft,
                PanelWidth = InfoCardWidth,
                StartCollapsed = false
            };
        }

        private static Color RarityColor(ItemRarity rarity)
        {
            switch (rarity)
            {
                case ItemRarity.Rare:
                    return RareColor;
                case ItemRarity.Epic:
                    return EpicColor;
                case ItemRarity.Legendary:
                    return LegendaryColor;
                default:
                    return DemoPalette.Muted;
            }
        }

        private static Sprite PackageSprite(string guid, string label)
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            Sprite sprite = string.IsNullOrEmpty(path) ? null : AssetDatabase.LoadAssetAtPath<Sprite>(path);
            if (sprite == null)
            {
                throw new InvalidOperationException(Scene + ": the package sprite " + label + " (guid " + guid +
                                                    ") was not found; reimport com.neoxider.tools.");
            }

            return sprite;
        }

        private static string Format(float value, string format)
        {
            return value.ToString(format, CultureInfo.InvariantCulture);
        }

        private static T Step<T>(string step, Func<T> action)
        {
            return DemoSceneKit.Step(Scene, step, action);
        }

        private static void Step(string step, Action action)
        {
            DemoSceneKit.Step(Scene, step, action);
        }

        /// <summary>One catalog entry: data asset file name, display texts, rarity and the Demo sprite it shows.</summary>
        internal readonly struct PizzaSpec
        {
            public PizzaSpec(string assetName, string name, string description, ItemRarity rarity, string spritePath)
            {
                AssetName = assetName;
                Name = name;
                Description = description;
                Rarity = rarity;
                SpritePath = spritePath;
            }

            public string AssetName { get; }
            public string Name { get; }
            public string Description { get; }
            public ItemRarity Rarity { get; }
            public string SpritePath { get; }
        }

        private readonly struct RewardParts
        {
#pragma warning disable CS0618
            public RewardParts(Collection collection, Box box, CooldownReward gift, TimeReward coins, Money wallet)
#pragma warning restore CS0618
            {
                Collection = collection;
                Box = box;
                Gift = gift;
                Coins = coins;
                Wallet = wallet;
            }

            public Collection Collection { get; }
            public Box Box { get; }
            public CooldownReward Gift { get; }
#pragma warning disable CS0618
            public TimeReward Coins { get; }
#pragma warning restore CS0618
            public Money Wallet { get; }
        }
    }
}
