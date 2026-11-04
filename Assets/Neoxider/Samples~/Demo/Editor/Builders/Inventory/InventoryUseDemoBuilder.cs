using System;
using System.Collections.Generic;
using System.Reflection;
using Neo.Samples;
using Neo.Tools;
using TMPro;
using UnityEditor;
using UnityEditor.Events;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;
using Object = UnityEngine.Object;

namespace Neo.Demo.Editor.Builders
{
    /// <summary>
    ///     Authors the Neo.Tools.Inventory demo into InventoryUseDemo.unity. The hand-authored catch / hotbar /
    ///     podium <see cref="InventoryComponent" />s and the 'Inventory demo - select and use' object
    ///     (<see cref="InventoryHand" />, <see cref="InventoryUseAction" />, <see cref="InventoryDemoController" />)
    ///     are kept and configured; everything else is saved scene content:
    ///     <list type="bullet">
    ///         <item>slot rows are <see cref="InventorySlotGridView" />s with manual <see cref="InventorySlotView" /> cells;</item>
    ///         <item>each hotbar cell's Button calls <see cref="InventoryHand.SetSlotIndex" /> (NoCode path);</item>
    ///         <item>count labels are <see cref="InventoryTotalCountText" /> (Total / Selected) and <see cref="InventoryItemCountText" />;</item>
    ///         <item>InventoryHand.OnUseSelectionRequested → InventoryUseAction.UseSelection → OnUsed → controller.OnCellUsed.</item>
    ///     </list>
    ///     Item names and icons come from an <see cref="InventoryDatabase" /> asset generated under Data/Inventory.
    /// </summary>
    internal static class InventoryUseDemoBuilder
    {
        private const string SceneName = "InventoryUseDemo";
        private const string HotbarName = "Hotbar - nine permanent slots";
        private const string CatchName = "Temporary catch - four slots";
        private const string PodiumName = "Podium - exact unique instance";
        private const string ControllerName = "Inventory demo - select and use";
        private const int CatchSlots = 4;
        private const float SideCellWidth = 90f;

        private static readonly Color Accent = new(0.3f, 0.85f, 0.7f);
        private static readonly Color SlotBody = new(0.24f, 0.26f, 0.34f, 1f);
        private static readonly Color IconTint = new(0.80f, 1f, 0.94f, 1f);

        [DemoSceneBuilder("Scenes/Inventory/InventoryUseDemo.unity")]
        public static void Build(DemoSceneBuildContext ctx)
        {
            string step = "data assets";
            try
            {
                InventoryDatabase database = EnsureDatabase(ctx);

                step = "services";
                InventoryDemoController controller =
                    ctx.FindOrCreate<InventoryDemoController>("Services", "Demo Controller");
                ModuleDemoSceneInfo info = ctx.FindOrCreate<ModuleDemoSceneInfo>("Services", "Demo Info");

                step = "inventories";
                InventoryComponent hotbar = ResolveInventory(ctx, controller, "_hotbar", HotbarName);
                InventoryComponent catchBag = ResolveInventory(ctx, controller, "_catchBag", CatchName);
                InventoryComponent podium = ResolveInventory(ctx, controller, "_podium", PodiumName);
                ConfigureInventory(ctx, hotbar, InventoryDemoController.HotbarSlots, "InventoryDemo_1000", database);
                ConfigureInventory(ctx, catchBag, CatchSlots, "InventoryDemo_2000", database);
                ConfigureInventory(ctx, podium, 1, "InventoryDemo_3000", database);

                step = "hand and use action";
                InventoryHand hand = ResolveOn<InventoryHand>(ctx, controller, "_hand");
                InventoryUseAction cellUse = ResolveOn<InventoryUseAction>(ctx, controller, "_cellUse");
                ConfigureHand(ctx, hand, hotbar, cellUse);

                step = "frame";
                DemoFrame frame = DemoSceneKit.Frame(ctx,
                    new DemoFrameOptions("Inventory · collect, keep, use", Accent) { ContentWidth = 920f });
                DemoPanel card = frame.Content;
                TMP_Text goal = card.Paragraph("1 Deposit catch.  2 Select PET, then Use to place it.\n" +
                                               "3 Select CELL, then Use: gain 25 HP and spend one CELL.", 60f);

                step = "hotbar";
                card.Section("Hotbar · 9 slots · click a slot or press 1-9");
                RectTransform hotbarRow = SlotRow(ctx, card, "Hotbar slots", 92f, true);
                List<InventorySlotView> hotbarCells = new();
                GameObject[] highlights = new GameObject[InventoryDemoController.HotbarSlots];
                for (int i = 0; i < InventoryDemoController.HotbarSlots; i++)
                {
                    SlotCell cell = BuildCell(ctx, hotbarRow, "Btn_Slot " + (i + 1), (i + 1).ToString(), -1f, true);
                    // WHY: NoCode path - the slot button calls the module directly; the controller only reacts to
                    // InventoryHand.OnEquippedChanged.
                    DemoWiring.AddInt(cell.Button.onClick, hand.SetSlotIndex, i);
                    cell.Highlight.SetActive(i == 0);
                    hotbarCells.Add(cell.View);
                    highlights[i] = cell.Highlight;
                }

                BindGrid(ctx, hotbarRow, hotbar, hotbarCells);

                step = "readouts";
                TMP_Text selected = card.ValueLabel("Selected slot · InventoryHand", "Slot 1 · -");
                DemoPanel.FitOneLine(selected, 14f, 22f);
                DemoPanel[] counts = card.Columns("Counts", 2, 20f);
                TMP_Text selectedCount = Compact(counts[0], "Same item in hotbar · Selected mode", "0");
                InventoryTotalCountText selectedCountText = ctx.Ensure<InventoryTotalCountText>(selectedCount.gameObject);
                ConfigureTotalCount(ctx, selectedCountText, hotbar, selectedCount, InventoryCountViewMode.Selected, "{0}");
                TMP_Text cellCount = Compact(counts[1], "CELL units · InventoryItemCountText", "0");
                InventoryItemCountText cellCountText = ctx.Ensure<InventoryItemCountText>(cellCount.gameObject);
                ctx.Set(cellCountText, "_inventory", hotbar);
                ctx.Set(cellCountText, "_targetText", cellCount);
                ctx.Set(cellCountText, "_itemId", InventoryDemoController.CellId);
                ctx.Set(cellCountText, "_format", "{0}");

                step = "catch and podium";
                DemoPanel[] containers = card.Columns("Catch | Podium", 2, 20f);
                containers[0].Section("Temporary catch · " + CatchSlots + " slots");
                RectTransform catchRow = SlotRow(ctx, containers[0], "Catch slots", 80f, false);
                List<InventorySlotView> catchCells = new();
                for (int i = 0; i < CatchSlots; i++)
                {
                    catchCells.Add(BuildCell(ctx, catchRow, "Catch Slot " + (i + 1), null, SideCellWidth, false).View);
                }

                BindGrid(ctx, catchRow, catchBag, catchCells);
                TMP_Text catchCount = Compact(containers[0], "Items · InventoryTotalCountText", "0 / " + CatchSlots);
                InventoryTotalCountText catchCountText = ctx.Ensure<InventoryTotalCountText>(catchCount.gameObject);
                ConfigureTotalCount(ctx, catchCountText, catchBag, catchCount, InventoryCountViewMode.Total,
                    "{0} / " + CatchSlots);

                containers[1].Section("Podium · exact unique instance");
                RectTransform podiumRow = SlotRow(ctx, containers[1], "Podium slot", 80f, false);
                SlotCell podiumCell = BuildCell(ctx, podiumRow, "Podium Slot", null, SideCellWidth, false);
                TMP_Text podiumLabel = DemoSceneKit.Text(ctx, podiumRow, "Description", "Empty · select a PET and Use",
                    17f, DemoPalette.Text, TextAlignmentOptions.MidlineLeft);
                LayoutElement podiumLabelSize = ctx.Ensure<LayoutElement>(podiumLabel.gameObject);
                podiumLabelSize.flexibleWidth = 1f;
                podiumLabelSize.minWidth = 120f;
                BindGrid(ctx, podiumRow, podium, new List<InventorySlotView> { podiumCell.View });
                containers[1].Note("InventoryTransferService keeps UID + JSON state");

                step = "health and actions";
                DemoBarRow health = card.BarRow("HP · a CELL restores 25", DemoPalette.Good, 0.45f);
                Image healthBar = health.Fill;
                TMP_Text healthLabel = health.Caption;
                healthLabel.text = "HP 45 / 100 · a CELL restores 25";

                Button[] moves = card.ButtonRow("Collect specimen", "Deposit catch", "Recall from podium");
                DemoWiring.AddVoid(moves[0].onClick, controller.Collect);
                DemoWiring.AddVoid(moves[1].onClick, controller.Deposit);
                DemoWiring.AddVoid(moves[2].onClick, controller.Recall);
                Button[] actions = card.ButtonRow("Use selected [E]", "Take 20 damage", "Reset demo");
                DemoWiring.AddVoid(actions[0].onClick, controller.UseSelected);
                DemoWiring.AddVoid(actions[1].onClick, controller.Damage);
                DemoWiring.AddVoid(actions[2].onClick, controller.ResetDemo);
                card.Note("Select a Btn_Slot object: its OnClick calls InventoryHand.SetSlotIndex (NoCode)");

                step = "module events";
                WireModuleEvents(ctx, controller, hotbar, catchBag, podium, hand, cellUse);

                step = "info card";
                frame.InfoCard(info, InfoContent());

                step = "controller references";
                ctx.Set(controller, "_catchBag", catchBag);
                ctx.Set(controller, "_hotbar", hotbar);
                ctx.Set(controller, "_podium", podium);
                ctx.Set(controller, "_hand", hand);
                ctx.Set(controller, "_cellUse", cellUse);
                ctx.Set(controller, "_goalLabel", goal);
                ctx.Set(controller, "_selectedLabel", selected);
                ctx.Set(controller, "_podiumLabel", podiumLabel);
                ctx.Set(controller, "_healthLabel", healthLabel);
                ctx.Set(controller, "_healthBar", healthBar);
                ctx.Set(controller, "_slotHighlights", highlights);
                ctx.Set(controller, "_log", frame.Log);
            }
            catch (Exception exception)
            {
                throw DemoSceneKit.StepFailed(SceneName, step, exception);
            }
        }

        private static InventoryDatabase EnsureDatabase(DemoSceneBuildContext ctx)
        {
            InventoryItemData specimen = DemoSceneAssets.EnsureAsset<InventoryItemData>(
                "Data/Inventory/InventoryDemoSpecimen.asset",
                data => ConfigureItem(ctx, data, InventoryDemoController.SpecimenId, "PET",
                    "Unique specimen. Level and mutation live in the instance's JSON component state.",
                    DemoSceneAssets.Disc, true));
            InventoryItemData cell = DemoSceneAssets.EnsureAsset<InventoryItemData>(
                "Data/Inventory/InventoryDemoCell.asset",
                data => ConfigureItem(ctx, data, InventoryDemoController.CellId, "CELL",
                    "Stackable energy cell. Use consumes exactly one and restores 25 HP.",
                    DemoSceneAssets.Glow, false));
            return DemoSceneAssets.EnsureAsset<InventoryDatabase>("Data/Inventory/InventoryDemoDatabase.asset",
                database => ctx.Set(database, "_items", new List<InventoryItemData> { specimen, cell }));
        }

        private static void ConfigureItem(DemoSceneBuildContext ctx, InventoryItemData data, int id, string displayName,
            string description, Sprite icon, bool instanceState)
        {
            ctx.Set(data, "_itemId", id);
            ctx.Set(data, "_displayName", displayName);
            ctx.Set(data, "_description", description);
            ctx.Set(data, "_icon", icon);
            // WHY: -1 keeps the stacks unlimited, exactly like the scene behaved before it had a database.
            ctx.Set(data, "_maxStack", -1);
            ctx.Set(data, "_supportsInstanceState", instanceState);
        }

        private static InventoryComponent ResolveInventory(DemoSceneBuildContext ctx, InventoryDemoController controller,
            string field, string objectName)
        {
            InventoryComponent referenced = ReadReference<InventoryComponent>(controller, field);
            if (referenced != null && !DemoSceneBuildContext.IsGenerated(referenced.gameObject))
            {
                return referenced;
            }

            foreach (InventoryComponent candidate in ctx.FindAll<InventoryComponent>())
            {
                if (candidate.gameObject.name == objectName)
                {
                    return candidate;
                }
            }

            // WHY: generated fallback for a scene without the hand-authored containers (tests, a fresh scene).
            GameObject host = ctx.Child(ctx.Root("Inventories").transform, objectName);
            return ctx.Ensure<InventoryComponent>(host);
        }

        private static T ResolveOn<T>(DemoSceneBuildContext ctx, InventoryDemoController controller, string field)
            where T : Component
        {
            T referenced = ReadReference<T>(controller, field);
            if (referenced != null && !DemoSceneBuildContext.IsGenerated(referenced.gameObject))
            {
                return referenced;
            }

            T found = ctx.Find<T>();
            if (found != null)
            {
                return found;
            }

            if (!DemoSceneBuildContext.IsGenerated(controller.gameObject))
            {
                ctx.Note("Added " + typeof(T).Name + " to hand-authored '" + controller.name + "'.");
            }

            return ctx.Ensure<T>(controller.gameObject);
        }

        private static void ConfigureInventory(DemoSceneBuildContext ctx, InventoryComponent inventory, int slots,
            string saveKey, InventoryDatabase database)
        {
            // WHY: three InventoryComponents share the scene; none may claim the singleton (later ones would be
            // destroyed in Awake) and the sample never persists between runs.
            ctx.Set(inventory, "_setInstanceOnAwake", false);
            ctx.Set(inventory, "_storageMode", InventoryStorageMode.SlotGrid);
            ctx.Set(inventory, "_slotCount", slots);
            ctx.Set(inventory, "_autoLoad", false);
            ctx.Set(inventory, "_autoSave", false);
            ctx.Set(inventory, "_saveKey", saveKey);
            ctx.Set(inventory, "_database", database);
        }

        private static void ConfigureHand(DemoSceneBuildContext ctx, InventoryHand hand, InventoryComponent hotbar,
            InventoryUseAction cellUse)
        {
            ctx.Set(hand, "_inventory", hotbar);
            ctx.Set(hand, "_handAnchor", hand.transform);
            ctx.Set(hand, "_autoFindInventory", false);
            // WHY: E goes through InventoryDemoController.UseSelected, which applies the sample's rules first
            // (a PET is placed, a CELL only heals when HP is not full).
            ctx.Set(hand, "_allowUseInput", false);
            ctx.Set(hand, "_allowDropInput", false);
            ctx.Set(hand, "_usePhysicalSlotIndices", true);

            ctx.Set(cellUse, "_hand", hand);
            // WHY: the hand → action call is a persistent listener (visible in the Inspector) instead of the
            // runtime subscription; both at once would consume two CELLs per use.
            ctx.Set(cellUse, "_listenToHand", false);
            ctx.Set(cellUse, "_itemId", InventoryDemoController.CellId);
            ctx.Set(cellUse, "_consumeAmount", 1);
        }

        private static void WireModuleEvents(DemoSceneBuildContext ctx, InventoryDemoController controller,
            InventoryComponent hotbar, InventoryComponent catchBag, InventoryComponent podium, InventoryHand hand,
            InventoryUseAction cellUse)
        {
            ctx.OwnListeners(hotbar, hotbar.OnInventoryChanged);
            ctx.OwnListeners(catchBag, catchBag.OnInventoryChanged);
            ctx.OwnListeners(podium, podium.OnInventoryChanged);
            ctx.OwnListeners(hand, hand.OnEquippedChanged);
            ctx.OwnListeners(hand, hand.OnUseSelectionRequested);
            ctx.OwnListeners(cellUse, cellUse.OnUsed);

            DemoWiring.AddVoid(hotbar.OnInventoryChanged, controller.Refresh);
            DemoWiring.AddVoid(catchBag.OnInventoryChanged, controller.Refresh);
            DemoWiring.AddVoid(podium.OnInventoryChanged, controller.Refresh);
            DemoWiring.AddDynamic<int>(hand.OnEquippedChanged, controller.OnEquippedChanged);
            DemoWiring.AddDynamic<InventorySelection>(hand.OnUseSelectionRequested, cellUse.UseSelection);
            DemoWiring.AddDynamic<InventoryItemRecord>(cellUse.OnUsed, controller.OnCellUsed);

            ctx.MarkModified(hotbar);
            ctx.MarkModified(catchBag);
            ctx.MarkModified(podium);
            ctx.MarkModified(hand);
            ctx.MarkModified(cellUse);
        }

        private static RectTransform SlotRow(DemoSceneBuildContext ctx, DemoPanel panel, string name, float height,
            bool stretchCells)
        {
            RectTransform row = panel.Row(name, height);
            HorizontalLayoutGroup group = ctx.Ensure<HorizontalLayoutGroup>(row.gameObject);
            group.padding = new RectOffset(4, 4, 4, 4);
            group.spacing = 8f;
            group.childAlignment = TextAnchor.MiddleLeft;
            group.childControlWidth = true;
            group.childControlHeight = true;
            group.childForceExpandWidth = stretchCells;
            group.childForceExpandHeight = true;
            return row;
        }

        private static void BindGrid(DemoSceneBuildContext ctx, RectTransform row, InventoryComponent inventory,
            List<InventorySlotView> cells)
        {
            InventorySlotGridView grid = ctx.Ensure<InventorySlotGridView>(row.gameObject);
            ctx.Set(grid, "_inventory", inventory);
            ctx.Set(grid, "_autoFindInventory", false);
            ctx.Set(grid, "_refreshOnLoaded", true);
            ctx.Set(grid, "_refreshNextFrameOnEnable", true);
            ctx.Set(grid, "_slotPrefab", null);
            ctx.Set(grid, "_slotsRoot", row);
            ctx.Set(grid, "_manualSlots", cells);
            // WHY: a click selects the hand slot here; the grid's click-to-transfer would also move items.
            ctx.Set(grid, "_enableClickTransfer", false);
        }

        private static SlotCell BuildCell(DemoSceneBuildContext ctx, RectTransform row, string name, string key,
            float width, bool clickable)
        {
            RectTransform cell = ctx.UiChild(row, name);
            LayoutElement size = ctx.Ensure<LayoutElement>(cell.gameObject);
            size.minWidth = width > 0f ? width : -1f;
            size.preferredWidth = width > 0f ? width : -1f;
            size.flexibleWidth = width > 0f ? 0f : 1f;

            Image highlight = DemoSceneKit.Image(ctx, cell, "Selected", DemoPalette.Warm);
            DemoSceneKit.Stretch(highlight.rectTransform, -4f);

            Image body = DemoSceneKit.Image(ctx, cell, "Body", SlotBody, true, true);
            DemoSceneKit.Stretch(body.rectTransform);

            if (key != null)
            {
                TMP_Text keyLabel = DemoSceneKit.Text(ctx, cell, "Key", key, 13f, DemoPalette.Muted,
                    TextAlignmentOptions.TopLeft, FontStyles.Bold);
                DemoSceneKit.Stretch(keyLabel.rectTransform, 6f);
            }

            RectTransform item = ctx.UiChild(cell, "Item");
            DemoSceneKit.Stretch(item);
            Image icon = DemoSceneKit.Image(ctx, item, "Icon", IconTint, false);
            DemoSceneKit.Anchor(icon.rectTransform, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f),
                new Vector2(0f, -6f), new Vector2(30f, 30f));
            icon.preserveAspect = true;
            // WHY: InventoryItemView assigns the database icon and enables the image at runtime.
            icon.enabled = false;

            TMP_Text itemName = DemoSceneKit.Text(ctx, item, "Name", "-", 15f, DemoPalette.Text,
                TextAlignmentOptions.Center, FontStyles.Bold);
            DemoSceneKit.SetAnchors(itemName.rectTransform, new Vector2(0f, 0f), new Vector2(1f, 0f), new Vector2(2f, 18f),
                new Vector2(-2f, 36f));
            itemName.textWrappingMode = TextWrappingModes.NoWrap;
            TMP_Text itemCount = DemoSceneKit.Text(ctx, item, "Count", "×0", 14f, Accent, TextAlignmentOptions.Center,
                FontStyles.Bold);
            DemoSceneKit.SetAnchors(itemCount.rectTransform, new Vector2(0f, 0f), new Vector2(1f, 0f), new Vector2(2f, 2f),
                new Vector2(-2f, 20f));
            itemCount.textWrappingMode = TextWrappingModes.NoWrap;

            InventoryItemView itemView = ctx.Ensure<InventoryItemView>(item.gameObject);
            ctx.Set(itemView, "_itemId", -1);
            ctx.Set(itemView, "_iconImage", icon);
            ctx.Set(itemView, "_nameText", itemName);
            ctx.Set(itemView, "_countText", itemCount);
            ctx.Set(itemView, "_root", item.gameObject);
            ctx.Set(itemView, "_countFormat", "×{0}");

            TMP_Text empty = DemoSceneKit.Text(ctx, cell, "Empty", "empty", 13f,
                DemoPalette.WithAlpha(DemoPalette.Muted, 0.7f), TextAlignmentOptions.Center);
            DemoSceneKit.Stretch(empty.rectTransform);

            InventorySlotView view = ctx.Ensure<InventorySlotView>(cell.gameObject);
            ctx.Set(view, "_itemView", itemView);
            ctx.Set(view, "_emptyRoot", empty.gameObject);
            ctx.Set(view, "_selectionHighlight", null);

            Button button = null;
            if (clickable)
            {
                button = ctx.Ensure<Button>(cell.gameObject);
                button.interactable = true;
                button.transition = Selectable.Transition.ColorTint;
                button.targetGraphic = body;
                ColorBlock colors = ColorBlock.defaultColorBlock;
                colors.normalColor = new Color(0.86f, 0.86f, 0.9f, 1f);
                colors.highlightedColor = Color.white;
                colors.selectedColor = new Color(0.86f, 0.86f, 0.9f, 1f);
                colors.pressedColor = new Color(0.7f, 0.75f, 0.8f, 1f);
                colors.colorMultiplier = 1f;
                button.colors = colors;
            }
            else
            {
                body.raycastTarget = false;
            }

            // WHY: the authored state is an empty slot; InventorySlotView activates the item at runtime.
            item.gameObject.SetActive(false);
            empty.gameObject.SetActive(true);
            highlight.gameObject.SetActive(false);
            return new SlotCell(button, view, highlight.gameObject);
        }

        private static void ConfigureTotalCount(DemoSceneBuildContext ctx, InventoryTotalCountText label,
            InventoryComponent inventory, TMP_Text target, InventoryCountViewMode mode, string format)
        {
            ctx.Set(label, "_inventory", inventory);
            ctx.Set(label, "_targetText", target);
            ctx.Set(label, "_mode", mode);
            ctx.Set(label, "_format", format);
        }

        private static TMP_Text Compact(DemoPanel panel, string caption, string initial)
        {
            TMP_Text value = panel.ValueRow(caption, initial, true).Value;
            DemoPanel.FitOneLine(value, 12f, 20f);
            return value;
        }

        private static T ReadReference<T>(Object owner, string field) where T : Object
        {
            SerializedProperty property = new SerializedObject(owner).FindProperty(field);
            if (property == null || property.propertyType != SerializedPropertyType.ObjectReference)
            {
                throw new ArgumentException(owner.GetType().Name + " has no object field '" + field + "'.");
            }

            return property.objectReferenceValue as T;
        }

        private static DemoInfoContent InfoContent()
        {
            return new DemoInfoContent
            {
                Title = "Neo.Tools.Inventory · select and use",
                Purpose = "Three ordinary InventoryComponents in Slot Grid mode: a temporary catch, a nine-slot hotbar " +
                          "and a one-slot podium. Unique PET instances keep their UID and JSON state across " +
                          "transfers; InventoryHand + InventoryUseAction consume exactly one CELL from the selected slot.",
                RuntimeApi = "InventoryComponent.AddItemInstance / TryCaptureSelection / GetPhysicalSlotRecord · " +
                             "InventoryTransferService.TransferSelection · InventoryHand.SetSlotIndex / " +
                             "TryGetEquippedSelection / UseEquippedItem · InventoryUseAction.TryUse.",
                SceneWorkflow = "Select '" + HotbarName + "', '" + CatchName + "', '" + PodiumName + "' and '" +
                                ControllerName + "' (InventoryHand, InventoryUseAction, InventoryDemoController). " +
                                "Slot rows are InventorySlotGridView with manual InventorySlotView cells.",
                Verification = "Play: Deposit catch, select the PET slot and Use - it moves to the podium with the same " +
                               "UID; Recall brings it back. Take damage, select CELL, Use: +25 HP, one CELL consumed.",
                Hints = new[]
                {
                    "Btn_Slot N -> InventoryHand.SetSlotIndex(N-1) (NoCode); keys 1-9 do the same",
                    "InventoryHand.OnUseSelectionRequested -> InventoryUseAction.UseSelection -> OnUsed -> InventoryDemoController.OnCellUsed",
                    "Labels: InventoryTotalCountText (Total, Selected) and InventoryItemCountText - no code"
                }
            };
        }

        private readonly struct SlotCell
        {
            public SlotCell(Button button, InventorySlotView view, GameObject highlight)
            {
                Button = button;
                View = view;
                Highlight = highlight;
            }

            public Button Button { get; }
            public InventorySlotView View { get; }
            public GameObject Highlight { get; }
        }
    }
}
