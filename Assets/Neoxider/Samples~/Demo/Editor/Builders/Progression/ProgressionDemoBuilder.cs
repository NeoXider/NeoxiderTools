using System;
using System.Collections.Generic;
using Neo.Core.Level;
using Neo.NoCode;
using Neo.Progression;
using Neo.Samples;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;
using Counter = Neo.Tools.Counter;
using InventoryComponent = Neo.Tools.InventoryComponent;
using InventoryDropper = Neo.Tools.InventoryDropper;
using Object = UnityEngine.Object;
using RewardTrackAsset = Neo.Progression.LevelCurveDefinition;
using XpCurveAsset = Neo.Core.Level.LevelCurveDefinition;

namespace Neo.Demo.Editor.Builders
{
    /// <summary>
    ///     Authors the Neo.Progression demo into Progression_Demo.unity. The hand-authored LevelComponent and
    ///     ProgressionManager (object "ProgressionDemoManager") are configured with demo data assets in
    ///     Data/Progression (reward track, unlock tree, perk tree); the left column drives XP through
    ///     ProgressionDemoUI (C# path), the right column runs pre-configured ProgressionNoCodeAction objects (NoCode
    ///     path), NoCodeBindText labels follow the reactive state, and module UnityEvents feed the log. Everything is
    ///     saved in the scene; nothing is built at Play. The unrelated pre-builder objects "PlayerInventory" and
    ///     "Progression_Managers" are retired once (their inventory autosaved the library default key); any other
    ///     inventory gets the demo key NeoDemo.Progression.Inventory.
    /// </summary>
    internal static class ProgressionDemoBuilder
    {
        private const string Scene = "Scenes/Progression_Demo.unity";
        private const string XpCurvePath = "Data/DemoLevelCurve.asset";
        private const string RewardTrackPath = "Data/Progression/DemoRewardTrack.asset";
        private const string UnlockTreePath = "Data/Progression/DemoUnlockTree.asset";
        private const string PerkTreePath = "Data/Progression/DemoPerkTree.asset";
        private const string LevelSaveKey = "NeoDemo.Progression.Level";
        private const string ProfileSaveKey = "NeoDemo.Progression.Profile";
        private const string InventorySaveKey = "NeoDemo.Progression.Inventory";
        private const string LeftoverInventoryName = "PlayerInventory";
        private const string LeftoverManagersName = "Progression_Managers";
        private const int XpPerClick = 25;

        private static readonly Color Accent = new(0.26f, 0.85f, 0.64f);
        private static readonly Color LevelYellow = new(1f, 0.84f, 0.35f);

        /// <summary>Reward track: level, legacy XP threshold (DemoLevelCurve), free perk points, premium perk points.</summary>
        private static readonly int[][] Track =
        {
            new[] { 2, 100, 1, 1 },
            new[] { 3, 250, 1, 0 },
            new[] { 4, 475, 1, 2 },
            new[] { 5, 800, 2, 0 },
            new[] { 6, 1250, 1, 1 },
            new[] { 7, 1850, 1, 0 },
            new[] { 8, 2625, 1, 2 },
            new[] { 9, 3600, 1, 0 },
            new[] { 10, 4800, 2, 3 }
        };

        [DemoSceneBuilder(Scene)]
        public static void Build(DemoSceneBuildContext ctx)
        {
            // WHY: pre-builder leftovers unrelated to the demo; PlayerInventory autoloaded and autosaved the library
            // default key "Inventory_Default", so opening the sample overwrote the user's own default inventory.
            RetireLeftover(ctx, LeftoverInventoryName, typeof(InventoryComponent));
            RetireLeftover(ctx, LeftoverManagersName, typeof(Counter), typeof(InventoryDropper));
            DemoSaveKeys.UseForAll<InventoryComponent>(ctx, "_saveKey", InventorySaveKey);

            RewardTrackAsset track = DemoSceneAssets.EnsureAsset<RewardTrackAsset>(RewardTrackPath, ConfigureTrack);
            UnlockTreeDefinition unlockTree =
                DemoSceneAssets.EnsureAsset<UnlockTreeDefinition>(UnlockTreePath, ConfigureUnlockTree);
            PerkTreeDefinition perkTree = DemoSceneAssets.EnsureAsset<PerkTreeDefinition>(PerkTreePath, ConfigurePerkTree);
            ProgressionQuestBuilderSupport.ReportIssues(ctx, Scene, unlockTree, unlockTree.ValidateDefinition());
            ProgressionQuestBuilderSupport.ReportIssues(ctx, Scene, perkTree, perkTree.ValidateDefinition(unlockTree));

            // WHY: the scene's hand-authored "ProgressionDemoManager" carries all three; FindOrCreate only fills gaps.
            LevelComponent level = ctx.FindOrCreate<LevelComponent>("Services", "Level (LevelComponent)");
            ProgressionManager progression = ctx.FindOrCreate<ProgressionManager>("Services", "Progression Manager");
            ProgressionDemoUI ui = ctx.FindOrCreate<ProgressionDemoUI>("Services", "Demo Controller");
            ModuleDemoSceneInfo info = ctx.FindOrCreate<ModuleDemoSceneInfo>("Services", "Demo Info");

            ConfigureLevel(ctx, level);
            ConfigureManager(ctx, progression, level, track, unlockTree, perkTree);

            DemoFrame frame = DemoSceneKit.Frame(ctx,
                new DemoFrameOptions("Neo.Progression · Levels, Perks, Unlocks", Accent)
                {
                    // WHY: 920 px keeps the centered card clear of the 460 px info card in the top-left corner.
                    ContentWidth = 920f
                });
            DemoPanel[] columns = frame.Content.Columns("Level | Meta progression", 2, 24f);

            DemoPanel left = columns[0];
            left.Section("Level · LevelComponent (C#)");
            TMP_Text levelLabel = left.BigLabel("Level 1");
            levelLabel.color = LevelYellow;
            Image xpFill = left.Bar("XP in this level", Accent);
            TMP_Text levelXp = left.ValueLabel("Level XP", "0 / 100");
            TMP_Text totalXp = left.ValueLabel("Total XP", "0");
            BindText(ctx, totalXp, level, nameof(LevelComponent.XpState));
            TMP_Text toNext = left.ValueLabel("XP to next level", "100");
            BindText(ctx, toNext, level, nameof(LevelComponent.XpToNextLevelState));
            Button[] xp = left.ButtonRow("+" + XpPerClick + " XP", "+" + XpPerClick * 5 + " XP",
                "+" + XpPerClick * 20 + " XP");
            DemoWiring.AddInt(xp[0].onClick, ui.AddXp, XpPerClick);
            DemoWiring.AddInt(xp[1].onClick, ui.AddXp, XpPerClick * 5);
            DemoWiring.AddInt(xp[2].onClick, ui.AddXp, XpPerClick * 20);
            left.Note("Buttons -> ProgressionDemoUI.AddXp -> levelComponent.AddXp(n). The level-up event feeds the log.",
                44f);
            left.Note("Total XP and XP to next: NoCodeBindText bound to LevelComponent.XpState (no code).", 44f);

            DemoPanel right = columns[1];
            right.Section("Meta · ProgressionManager (NoCode)");
            TMP_Text perkPoints = right.ValueLabel("Perk points", "0");
            BindText(ctx, perkPoints, progression, nameof(ProgressionManager.PerkPointsState));
            TMP_Text premium = right.ValueLabel("Premium track", "off");
            Button[] grants = right.ButtonRow("+50 XP", "+1 perk point");

            right.Section("Unlock tree · TryUnlockNode");
            TMP_Text nodes = ProgressionQuestBuilderSupport.StatusText(right, "Unlock tree", "Forge (Lv 2)   Arena (Lv 4)",
                18f, 26f);
            Button[] unlock = right.ButtonRow("Unlock Forge", "Unlock Arena");

            right.Section("Perk tree · TryBuyPerk");
            TMP_Text perks = ProgressionQuestBuilderSupport.StatusText(right, "Perk tree",
                "Swift Feet (1 PP)   Iron Skin (2 PP)   Blade Master (3 PP)", 18f, 48f);
            Button[] buy = right.ButtonRow("Swift Feet", "Iron Skin", "Blade Master");
            Button[] meta = right.ButtonRow("Activate Premium", "Reset all");
            right.Note("green = owned · white = available · grey = locked (the log says why)", 44f);

            Transform actions = ctx.Child(ctx.Root("Services").transform, "Progression NoCode Actions").transform;
            Wire(grants[0], Action(ctx, actions, "Add 50 XP", progression, ui, ProgressionNoCodeActionType.AddXp, 50));
            Wire(grants[1], Action(ctx, actions, "Grant 1 Perk Point", progression, ui,
                ProgressionNoCodeActionType.GrantPerkPoints, 1));
            Wire(unlock[0], Action(ctx, actions, "Unlock Forge", progression, ui, ProgressionNoCodeActionType.UnlockNode,
                0, "forge"));
            Wire(unlock[1], Action(ctx, actions, "Unlock Arena", progression, ui, ProgressionNoCodeActionType.UnlockNode,
                0, "arena"));
            Wire(buy[0], Action(ctx, actions, "Buy Swift Feet", progression, ui, ProgressionNoCodeActionType.BuyPerk, 0,
                "swift_feet"));
            Wire(buy[1], Action(ctx, actions, "Buy Iron Skin", progression, ui, ProgressionNoCodeActionType.BuyPerk, 0,
                "iron_skin"));
            Wire(buy[2], Action(ctx, actions, "Buy Blade Master", progression, ui, ProgressionNoCodeActionType.BuyPerk, 0,
                "blade_master"));
            Wire(meta[0], Action(ctx, actions, "Activate Premium", progression, ui,
                ProgressionNoCodeActionType.ActivatePremium, 0));
            Wire(meta[1], Action(ctx, actions, "Reset Progression", progression, ui,
                ProgressionNoCodeActionType.ResetProgression, 0));

            WireModuleEvents(ctx, level, progression, ui);

            ctx.Set(ui, "_levelComponent", level);
            ctx.Set(ui, "_progression", progression);
            ctx.Set(ui, "_xpPerClick", XpPerClick);
            ctx.Set(ui, "_levelLabel", levelLabel);
            ctx.Set(ui, "_currentLevelXpValue", levelXp);
            ctx.Set(ui, "_xpFill", xpFill);
            ctx.Set(ui, "_premiumValue", premium);
            ctx.Set(ui, "_unlockTreeValue", nodes);
            ctx.Set(ui, "_perkTreeValue", perks);
            ctx.Set(ui, "_log", frame.Log);

            frame.InfoCard(info, new DemoInfoContent
            {
                Title = "Neo.Progression · ProgressionManager",
                Purpose = "LevelComponent turns XP into levels; ProgressionManager adds meta progression on top: a " +
                          "reward track pays perk points per level, perks cost points, unlock nodes gate content and " +
                          "premium opens a second reward lane.",
                RuntimeApi = "levelComponent.AddXp(n); progression.TryUnlockNode / TryBuyPerk(id, out reason) / " +
                             "ActivatePremium() / ResetProgression(); C# events NodeUnlocked, PerkPurchased, " +
                             "PremiumActivated.",
                SceneWorkflow = "ProgressionDemoManager: LevelComponent + ProgressionManager with the Data/Progression " +
                                "assets. [Demo] Services/Progression NoCode Actions: one ProgressionNoCodeAction per " +
                                "button.",
                Verification = "Play: +500 XP levels up and pays perk points; unlock Forge at level 2, buy Swift Feet " +
                               "then Blade Master; Activate Premium pays premium entries of reached levels.",
                Hints = new[]
                {
                    "XP buttons -> ProgressionDemoUI.AddXp -> LevelComponent.AddXp (C#)",
                    "Right column -> ProgressionNoCodeAction.Execute (NoCode, no scripts)",
                    "Perk points / Total XP labels: NoCodeBindText on reactive state",
                    "LevelComponent.OnLevelUp and ProgressionManager events -> ProgressionDemoUI"
                }
            });
        }

        /// <summary>
        ///     Retires the hand-authored root <paramref name="name" /> when it still has its known leftover shape: no
        ///     children, exactly the <paramref name="shape" /> components and no reference from any other object. An
        ///     object the author changed is kept (noted); a kept inventory still gets the demo key.
        /// </summary>
        private static void RetireLeftover(DemoSceneBuildContext ctx, string name, params Type[] shape)
        {
            GameObject leftover = ctx.FindHandAuthoredRoot(name);
            if (leftover == null)
            {
                return;
            }

            string mismatch = LeftoverMismatch(leftover, shape);
            if (mismatch != null)
            {
                ctx.Note("Kept hand-authored '" + name + "': " + mismatch + ".");
                return;
            }

            ctx.Retire(leftover, "unreferenced pre-builder leftover outside the Progression demo");
        }

        private static string LeftoverMismatch(GameObject candidate, Type[] shape)
        {
            if (candidate.transform.childCount > 0)
            {
                return "it has children";
            }

            if (PrefabUtility.IsPartOfPrefabInstance(candidate))
            {
                return "it is a prefab instance";
            }

            Component[] components = candidate.GetComponents<Component>();
            if (components.Length != shape.Length + 1)
            {
                return "its components changed";
            }

            foreach (Type type in shape)
            {
                if (candidate.GetComponent(type) == null)
                {
                    return "it has no " + type.Name;
                }
            }

            return IsReferenced(candidate, components) ? "another object references it" : null;
        }

        private static bool IsReferenced(GameObject candidate, Component[] own)
        {
            HashSet<Object> targets = new(own) { candidate };
            foreach (GameObject root in candidate.scene.GetRootGameObjects())
            {
                if (root == candidate)
                {
                    continue;
                }

                foreach (Component component in root.GetComponentsInChildren<Component>(true))
                {
                    if (component == null)
                    {
                        continue;
                    }

                    using SerializedObject serialized = new(component);
                    SerializedProperty iterator = serialized.GetIterator();
                    while (iterator.Next(true))
                    {
                        if (iterator.propertyType == SerializedPropertyType.ObjectReference &&
                            iterator.objectReferenceValue != null && targets.Contains(iterator.objectReferenceValue))
                        {
                            return true;
                        }
                    }
                }
            }

            return false;
        }

        private static void ConfigureLevel(DemoSceneBuildContext ctx, LevelComponent level)
        {
            XpCurveAsset curve = level.LevelCurveDefinition;
            if (curve == null)
            {
                string path = DemoSampleRoots.Demo + "/" + XpCurvePath;
                curve = AssetDatabase.LoadAssetAtPath<XpCurveAsset>(path);
                if (curve == null)
                {
                    throw new InvalidOperationException(Scene + ": the LevelComponent has no XP curve and " + path +
                                                        " (Neo.Core.Level.LevelCurveDefinition) is missing.");
                }

                ctx.Set(level, "_levelCurve", curve);
            }

            ctx.Set(level, "_useXp", true);
            ctx.Set(level, "_startXp", 0);
            ctx.Set(level, "_startLevel", 1);
            // WHY: a demo key keeps the sample from overwriting the default Core level profile of the user's game.
            ctx.Set(level, "_saveKey", LevelSaveKey);
            ctx.Set(level, "_loadOnAwake", false);
            ctx.Set(level, "_autoSave", false);
            // WHY: reactive mirrors are serialized; show the start state in the Inspector instead of stale play values.
            ProgressionQuestBuilderSupport.TrySet(ctx, level, "LevelState._value", 1);
            ProgressionQuestBuilderSupport.TrySet(ctx, level, "XpState._value", 0);
            ProgressionQuestBuilderSupport.TrySet(ctx, level, "XpToNextLevelState._value",
                Mathf.Max(0, curve.GetRequiredXpForLevel(2) - curve.GetRequiredXpForLevel(1)));
        }

        private static void ConfigureManager(DemoSceneBuildContext ctx, ProgressionManager progression,
            LevelComponent level, RewardTrackAsset track, UnlockTreeDefinition unlockTree, PerkTreeDefinition perkTree)
        {
            ctx.Set(progression, "_levelProvider", level);
            ctx.Set(progression, "_levelCurve", track);
            ctx.Set(progression, "_unlockTree", unlockTree);
            ctx.Set(progression, "_perkTree", perkTree);
            ctx.Set(progression, "_saveKey", ProfileSaveKey);
            // WHY: every Play session starts fresh, so the demo is replayable and never persists into a real profile.
            ctx.Set(progression, "_loadOnAwake", false);
            ctx.Set(progression, "_autoSave", false);
        }

        private static void WireModuleEvents(DemoSceneBuildContext ctx, LevelComponent level,
            ProgressionManager progression, ProgressionDemoUI ui)
        {
            // WHY: these events of the hand-authored components belong to the demo; clearing first keeps renamed or
            // removed demo listeners from surviving a rebuild (the kit only resets generated objects).
            DemoWiring.Clear(level.OnLevelUp);
            DemoWiring.Clear(level.OnXpGained);
            DemoWiring.AddDynamic(level.OnLevelUp, ui.HandleLevelUp);
            DemoWiring.AddVoid(level.OnXpGained, ui.RefreshView);

            DemoWiring.Clear(progression.OnNodeUnlocked);
            DemoWiring.Clear(progression.OnPerkPurchased);
            DemoWiring.Clear(progression.OnPremiumActivated);
            DemoWiring.Clear(progression.OnProfileReset);
            DemoWiring.Clear(progression.OnPerkPointsChanged);
            DemoWiring.AddDynamic(progression.OnNodeUnlocked, ui.HandleNodeUnlocked);
            DemoWiring.AddDynamic(progression.OnPerkPurchased, ui.HandlePerkPurchased);
            DemoWiring.AddVoid(progression.OnPremiumActivated, ui.HandlePremiumActivated);
            DemoWiring.AddVoid(progression.OnProfileReset, ui.HandleProgressionReset);
            DemoWiring.AddVoid(progression.OnPerkPointsChanged, ui.RefreshView);

            ctx.MarkModified(level);
            ctx.MarkModified(progression);
        }

        private static ProgressionNoCodeAction Action(DemoSceneBuildContext ctx, Transform parent, string name,
            ProgressionManager manager, ProgressionDemoUI ui, ProgressionNoCodeActionType type, int amount,
            string id = "")
        {
            ProgressionNoCodeAction action = ctx.Ensure<ProgressionNoCodeAction>(ctx.Child(parent, name));
            ctx.Set(action, "_actionType", (int)type);
            ctx.Set(action, "_manager", manager);
            ctx.Set(action, "_xpAmount", type == ProgressionNoCodeActionType.AddXp ? amount : 0);
            ctx.Set(action, "_perkPointsAmount", type == ProgressionNoCodeActionType.GrantPerkPoints ? amount : 0);
            ctx.Set(action, "_nodeId", type == ProgressionNoCodeActionType.UnlockNode ? id : string.Empty);
            ctx.Set(action, "_perkId", type == ProgressionNoCodeActionType.BuyPerk ? id : string.Empty);
            DemoWiring.AddDynamic(action.OnResultMessage, ui.LogNoCodeResult);
            return action;
        }

        private static void Wire(Button button, ProgressionNoCodeAction action)
        {
            DemoWiring.AddVoid(button.onClick, action.Execute);
        }

        /// <summary>NoCodeBindText on the value label: follows a ReactivePropertyInt of a module component.</summary>
        private static void BindText(DemoSceneBuildContext ctx, TMP_Text label, Component source, string member)
        {
            NoCodeBindText binding = ctx.Ensure<NoCodeBindText>(label.gameObject);
            ctx.Set(binding, "_fallbackText", label);
            ctx.Set(binding, "_binding._useSceneSearch", false);
            ctx.Set(binding, "_binding._sourceRoot", source.gameObject);
            ctx.Set(binding, "_binding._componentTypeName", source.GetType().FullName);
            ctx.Set(binding, "_binding._componentOccurrence", 0);
            ctx.Set(binding, "_binding._memberName", member);
            ctx.Set(binding, "_updateMode", (int)NoCodeFloatUpdateMode.Reactive);
        }

        private static void ConfigureTrack(RewardTrackAsset asset)
        {
            SerializedObject serialized = new(asset);
            SerializedProperty levels = ProgressionQuestBuilderSupport.Require(serialized, "_levels", Scene);
            levels.arraySize = Track.Length;
            for (int i = 0; i < Track.Length; i++)
            {
                int[] entry = Track[i];
                SerializedProperty element = levels.GetArrayElementAtIndex(i);
                Relative(element, "_level").intValue = entry[0];
                Relative(element, "_requiredXp").intValue = entry[1];
                Relative(element, "_grantedPerkPoints").intValue = entry[2];
                SerializedProperty rewards = Relative(element, "_rewards");
                rewards.arraySize = entry[3] > 0 ? 1 : 0;
                if (entry[3] > 0)
                {
                    WriteReward(rewards.GetArrayElementAtIndex(0), ProgressionRewardType.PerkPoints, entry[3], true,
                        "Premium lane bonus");
                }
            }

            serialized.ApplyModifiedPropertiesWithoutUndo();
        }

        private static void ConfigureUnlockTree(UnlockTreeDefinition asset)
        {
            SerializedObject serialized = new(asset);
            SerializedProperty nodes = ProgressionQuestBuilderSupport.Require(serialized, "_nodes", Scene);
            nodes.arraySize = 2;
            WriteNode(nodes.GetArrayElementAtIndex(0), "forge", "Forge",
                "Crafting station. Unlocking it pays 1 perk point.", 2, Array.Empty<string>(),
                ProgressionRewardType.PerkPoints, 1);
            WriteNode(nodes.GetArrayElementAtIndex(1), "arena", "Arena",
                "Combat trials. Needs the Forge; unlocking it pays 150 XP.", 4, new[] { "forge" },
                ProgressionRewardType.Xp, 150);
            serialized.ApplyModifiedPropertiesWithoutUndo();
        }

        private static void ConfigurePerkTree(PerkTreeDefinition asset)
        {
            SerializedObject serialized = new(asset);
            SerializedProperty perks = ProgressionQuestBuilderSupport.Require(serialized, "_perks", Scene);
            perks.arraySize = 3;
            WritePerk(perks.GetArrayElementAtIndex(0), "swift_feet", "Swift Feet", "+10% move speed.", 1, 1,
                Array.Empty<string>(), Array.Empty<string>());
            WritePerk(perks.GetArrayElementAtIndex(1), "iron_skin", "Iron Skin", "+15% armor. Needs level 3.", 2, 3,
                Array.Empty<string>(), Array.Empty<string>());
            WritePerk(perks.GetArrayElementAtIndex(2), "blade_master", "Blade Master",
                "+20% melee damage. Needs Swift Feet and the Forge.", 3, 1, new[] { "swift_feet" }, new[] { "forge" });
            serialized.ApplyModifiedPropertiesWithoutUndo();
        }

        private static void WriteNode(SerializedProperty node, string id, string displayName, string description,
            int requiredLevel, string[] prerequisites, ProgressionRewardType rewardType, int rewardAmount)
        {
            Relative(node, "_id").stringValue = id;
            Relative(node, "_displayName").stringValue = displayName;
            Relative(node, "_description").stringValue = description;
            Relative(node, "_icon").objectReferenceValue = null;
            Relative(node, "_unlockedByDefault").boolValue = false;
            Relative(node, "_requiredLevel").intValue = requiredLevel;
            ProgressionQuestBuilderSupport.SetStrings(Relative(node, "_prerequisiteNodeIds"), prerequisites);
            Relative(node, "_conditions").arraySize = 0;
            SerializedProperty rewards = Relative(node, "_rewards");
            rewards.arraySize = 1;
            WriteReward(rewards.GetArrayElementAtIndex(0), rewardType, rewardAmount, false, displayName + " unlock reward");
        }

        private static void WritePerk(SerializedProperty perk, string id, string displayName, string description,
            int cost, int requiredLevel, string[] prerequisitePerks, string[] requiredNodes)
        {
            Relative(perk, "_id").stringValue = id;
            Relative(perk, "_displayName").stringValue = displayName;
            Relative(perk, "_description").stringValue = description;
            Relative(perk, "_icon").objectReferenceValue = null;
            Relative(perk, "_purchasedByDefault").boolValue = false;
            Relative(perk, "_cost").intValue = cost;
            Relative(perk, "_requiredLevel").intValue = requiredLevel;
            ProgressionQuestBuilderSupport.SetStrings(Relative(perk, "_prerequisitePerkIds"), prerequisitePerks);
            ProgressionQuestBuilderSupport.SetStrings(Relative(perk, "_requiredUnlockNodeIds"), requiredNodes);
            Relative(perk, "_conditions").arraySize = 0;
            Relative(perk, "_rewards").arraySize = 0;
        }

        private static void WriteReward(SerializedProperty reward, ProgressionRewardType type, int amount, bool premium,
            string description)
        {
            Relative(reward, "_rewardType").intValue = (int)type;
            Relative(reward, "_amount").intValue = amount;
            Relative(reward, "_moneyAmount").floatValue = 0f;
            Relative(reward, "_moneyTarget").objectReferenceValue = null;
            Relative(reward, "_collectionTarget").objectReferenceValue = null;
            Relative(reward, "_collectionItem").objectReferenceValue = null;
            Relative(reward, "_questManager").objectReferenceValue = null;
            Relative(reward, "_quest").objectReferenceValue = null;
            Relative(reward, "_description").stringValue = description;
            Relative(reward, "_isPremium").boolValue = premium;
        }

        private static SerializedProperty Relative(SerializedProperty owner, string path)
        {
            return ProgressionQuestBuilderSupport.Relative(owner, path, Scene);
        }
    }
}
