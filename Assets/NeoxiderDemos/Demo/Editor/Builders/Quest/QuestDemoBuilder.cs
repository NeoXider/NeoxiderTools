using System;
using Neo.Quest;
using Neo.Samples;
using Neo.Shop;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

namespace Neo.Demo.Editor.Builders
{
    /// <summary>
    ///     Authors the Neo.Quest demo into QuestDemo.unity: three QuestConfig assets in Data/Quest, a QuestManager
    ///     that knows them and pays a Money reward through its On Quest Completed UnityEvent (no code), a journal
    ///     column driven by QuestDemoController (C# path) and a world-events column of pre-configured
    ///     QuestNoCodeAction objects that credit objectives by Target Id (NoCode path). Everything is saved in the
    ///     scene; nothing is built at Play.
    /// </summary>
    internal static class QuestDemoBuilder
    {
        private const string Scene = "Scenes/Quests/QuestDemo.unity";
        private const string SaveKey = "NeoDemo.Quests";
        private const string WalletSaveKey = "NeoDemo.QuestGold";
        private const float GoldPerQuest = 25f;

        private static readonly Color Accent = new(1f, 0.72f, 0.25f);

        private static readonly QuestSpec[] Quests =
        {
            new("Data/Quest/DemoQuest_WolfHunt.asset", "demo_wolf_hunt", "Wolf Hunt",
                "Thin the pack prowling the forest road.",
                new ObjectiveSpec(QuestObjectiveType.KillCount, "wolf", 3, "Hunt forest wolves")),
            new("Data/Quest/DemoQuest_HerbalAid.asset", "demo_herbal_aid", "Herbal Aid",
                "Gather herbs, then report to the healer.",
                new ObjectiveSpec(QuestObjectiveType.CollectCount, "herb", 4, "Gather moon herbs"),
                new ObjectiveSpec(QuestObjectiveType.Talk, "healer", 1, "Talk to the healer")),
            new("Data/Quest/DemoQuest_ScoutRuins.asset", "demo_scout_ruins", "Scout Ruins",
                "Find the entrance to the sunken ruins.",
                new ObjectiveSpec(QuestObjectiveType.ReachPoint, "ruins_gate", 1, "Reach the ruins gate"))
        };

        [DemoSceneBuilder(Scene)]
        public static void Build(DemoSceneBuildContext ctx)
        {
            QuestConfig[] quests = new QuestConfig[Quests.Length];
            for (int i = 0; i < Quests.Length; i++)
            {
                QuestSpec spec = Quests[i];
                quests[i] = DemoSceneAssets.EnsureAsset<QuestConfig>(spec.Path, asset => Configure(asset, spec));
                ProgressionQuestBuilderSupport.ReportIssues(ctx, Scene, quests[i], quests[i].ValidateDefinition());
            }

            QuestManager manager = ctx.FindOrCreate<QuestManager>("Services", "Quest Manager");
            Money wallet = ctx.FindOrCreate<Money>("Services", "Gold Wallet (Money)");
            QuestDemoController controller = ctx.FindOrCreate<QuestDemoController>("Services", "Demo Controller");
            ModuleDemoSceneInfo info = ctx.FindOrCreate<ModuleDemoSceneInfo>("Services", "Demo Info");

            ConfigureManager(ctx, manager, wallet, quests);
            ConfigureWallet(ctx, wallet);

            DemoFrame frame = DemoSceneKit.Frame(ctx, new DemoFrameOptions("Neo.Quest · QuestManager", Accent)
            {
                // WHY: 920 px keeps the centered card clear of the 460 px info card in the top-left corner.
                ContentWidth = 920f
            });
            DemoPanel[] columns = frame.Content.Columns("Journal | World events", 2, 24f);

            DemoPanel left = columns[0];
            left.Section("Quest journal · C# API");
            string[] titles = new string[quests.Length];
            for (int i = 0; i < quests.Length; i++)
            {
                titles[i] = quests[i].Title;
            }

            Button[] select = left.ButtonRow(titles);
            for (int i = 0; i < select.Length; i++)
            {
                DemoWiring.AddInt(select[i].onClick, controller.Select, i);
                // WHY: authored in the selected state of the first quest; the controller re-tints on selection.
                select[i].image.color = i == 0 ? Accent : DemoPalette.Muted;
            }

            TMP_Text title = left.BigLabel(quests[0].Title);
            TMP_Text description = ProgressionQuestBuilderSupport.StatusText(left, "Description", quests[0].Description,
                18f, 26f);
            description.color = DemoPalette.Muted;
            Image progress = left.Bar("Objectives progress", Accent);
            TMP_Text status = left.ValueLabel("Status", "Not started");
            TMP_Text done = left.ValueLabel("Objectives done", "0 / " + quests[0].Objectives.Count);
            TMP_Text objective = ProgressionQuestBuilderSupport.StatusText(left, "Objective", "Objective: press Accept",
                18f, 26f);
            Button[] flow = left.ButtonRow("Accept", "+1 Progress", "Complete step");
            DemoWiring.AddVoid(flow[0].onClick, controller.Accept);
            DemoWiring.AddVoid(flow[1].onClick, controller.AddProgress);
            DemoWiring.AddVoid(flow[2].onClick, controller.CompleteStep);
            Button[] manage = left.ButtonRow("Fail", "Restart", "Reset all");
            DemoWiring.AddVoid(manage[0].onClick, controller.Fail);
            DemoWiring.AddVoid(manage[1].onClick, controller.Restart);
            DemoWiring.AddVoid(manage[2].onClick, controller.ResetAll);
            left.Note("Buttons -> QuestDemoController -> TryAcceptQuest / AddObjectiveProgress / FailQuest / RestartQuest",
                44f);

            DemoPanel right = columns[1];
            right.Section("World events · QuestNoCodeAction");
            right.Note("Enemy and pickup prefabs credit quests by Target Id - no quest reference, no code.", 44f);
            Button[] kills = right.ButtonRow("Kill wolf", "Kill 3 wolves");
            Button[] herbs = right.ButtonRow("Pick herb", "Pick 2 herbs");
            Button[] visits = right.ButtonRow("Talk to healer", "Reach ruins gate");

            Transform actions = ctx.Child(ctx.Root("Services").transform, "Quest NoCode Actions").transform;
            Wire(kills[0], Action(ctx, actions, "Kill wolf", QuestNoCodeAction.ActionType.NotifyKill, "wolf", 1, controller));
            Wire(kills[1], Action(ctx, actions, "Kill 3 wolves", QuestNoCodeAction.ActionType.NotifyKill, "wolf", 3,
                controller));
            Wire(herbs[0], Action(ctx, actions, "Pick herb", QuestNoCodeAction.ActionType.NotifyCollect, "herb", 1,
                controller));
            Wire(herbs[1], Action(ctx, actions, "Pick 2 herbs", QuestNoCodeAction.ActionType.NotifyCollect, "herb", 2,
                controller));
            Wire(visits[0], Action(ctx, actions, "Talk to healer", QuestNoCodeAction.ActionType.NotifyTalk, "healer", 1,
                controller));
            Wire(visits[1], Action(ctx, actions, "Reach ruins gate", QuestNoCodeAction.ActionType.NotifyReachPoint,
                "ruins_gate", 1, controller));

            right.Section("Reward · On Quest Completed");
            TMP_Text gold = right.ValueLabel("Gold (Money)", "0");
            right.Note("Inspector: Quest Manager › On Quest Completed -> Money.Add(" + GoldPerQuest + ")", 44f);
            right.Section("All quests · GetState");
            TMP_Text journal = ProgressionQuestBuilderSupport.StatusText(right, "Journal",
                "Wolf Hunt · Not started\nHerbal Aid · Not started\nScout Ruins · Not started", 17f, 96f,
                TextAlignmentOptions.TopLeft);

            ctx.Set(wallet, "t_money", new[] { gold });

            ctx.Set(controller, "_manager", manager);
            ctx.Set(controller, "_quests", quests);
            ctx.Set(controller, "_selectButtons", select);
            ctx.Set(controller, "_selectedTint", Accent);
            ctx.Set(controller, "_idleTint", DemoPalette.Muted);
            ctx.Set(controller, "_titleValue", title);
            ctx.Set(controller, "_descriptionValue", description);
            ctx.Set(controller, "_progressFill", progress);
            ctx.Set(controller, "_statusValue", status);
            ctx.Set(controller, "_objectiveValue", objective);
            ctx.Set(controller, "_doneValue", done);
            ctx.Set(controller, "_journalValue", journal);
            ctx.Set(controller, "_log", frame.Log);

            frame.InfoCard(info, new DemoInfoContent
            {
                Title = "Neo.Quest · QuestManager",
                Purpose = "QuestConfig assets define objectives; QuestManager tracks quest state, credits progress by " +
                          "quest and index or by Target Id, saves through SaveProvider and raises events for UI and " +
                          "rewards.",
                RuntimeApi = "manager.TryAcceptQuest / AddObjectiveProgress / NotifyKill(id, n) / NotifyCollect / " +
                             "NotifyTalk / NotifyReachPoint / FailQuest / RestartQuest; C# events QuestAccepted, " +
                             "ObjectiveProgress, QuestCompleted.",
                SceneWorkflow = "[Demo] Services: Quest Manager (Known Quests = Data/Quest assets), Gold Wallet " +
                                "(Money), Quest NoCode Actions (one QuestNoCodeAction per world button).",
                Verification = "Play: Wolf Hunt -> Accept -> Kill 3 wolves completes it and pays 25 gold. States " +
                               "persist between sessions; Reset all replays.",
                Hints = new[]
                {
                    "Journal buttons -> QuestDemoController (C# API)",
                    "World buttons -> QuestNoCodeAction.Execute (NoCode, by Target Id)",
                    "Quest Manager › On Quest Completed -> Money.Add (reward without code)"
                }
            });
        }

        private static void ConfigureManager(DemoSceneBuildContext ctx, QuestManager manager, Money wallet,
            QuestConfig[] quests)
        {
            ctx.Set(manager, "_knownQuests", quests);
            // WHY: a demo key keeps the sample from mixing its quest states into the default key of the user's game.
            ctx.Set(manager, "_saveKey", SaveKey);
            ctx.Set(manager, "_autoSave", true);
            ctx.Set(manager, "_autoLoad", true);
            ctx.Set(manager, "_editorQuestId", quests[0].Id);
            ctx.Set(manager, "_editorObjectiveIndex", 0);

            // WHY: the reward is NoCode wiring on the manager's own UnityEvent; cleared first in case the manager is
            // hand-authored (the kit only resets events of generated objects).
            DemoWiring.Clear(manager.OnQuestCompleted);
            DemoWiring.AddFloat(manager.OnQuestCompleted, wallet.Add, GoldPerQuest);
            ctx.MarkModified(manager);
        }

        private static void ConfigureWallet(DemoSceneBuildContext ctx, Money wallet)
        {
            ProgressionQuestBuilderSupport.TrySet(ctx, wallet, "isNetworked", false);
            // WHY: session-only gold; the demo never writes the default "Money" balance of the user's game.
            ctx.Set(wallet, "_persistMoney", false);
            ctx.Set(wallet, "_moneySave", WalletSaveKey);

            // WHY: with Mirror installed Money requires a NetworkIdentity; an offline scene object needs a baked
            // sceneId (Mirror assigns it in the editor) or entering Play stops with "needs to be resaved".
            Component identity = wallet.GetComponent("NetworkIdentity");
            if (identity != null)
            {
                SerializedProperty sceneId = new SerializedObject(identity).FindProperty("sceneId");
                if (sceneId != null && sceneId.longValue == 0)
                {
                    ctx.Warn(Scene + ": '" + wallet.name + "' NetworkIdentity has no sceneId yet; open and save the " +
                             "scene once in the editor before entering Play.");
                }
            }
        }

        private static QuestNoCodeAction Action(DemoSceneBuildContext ctx, Transform parent, string name,
            QuestNoCodeAction.ActionType type, string targetId, int amount, QuestDemoController controller)
        {
            QuestNoCodeAction action = ctx.Ensure<QuestNoCodeAction>(ctx.Child(parent, name));
            ctx.Set(action, "_actionType", (int)type);
            ctx.Set(action, "_quest", null);
            ctx.Set(action, "_objectiveIndex", 0);
            ctx.Set(action, "_targetId", targetId);
            ctx.Set(action, "_amount", amount);
            ctx.Set(action, "_flowConfig", null);
            DemoWiring.AddDynamic(action.OnResultMessage, controller.LogNoCodeResult);
            return action;
        }

        private static void Wire(Button button, QuestNoCodeAction action)
        {
            DemoWiring.AddVoid(button.onClick, action.Execute);
        }

        private static void Configure(QuestConfig asset, QuestSpec spec)
        {
            SerializedObject serialized = new(asset);
            Require(serialized, "_id").stringValue = spec.Id;
            Require(serialized, "_title").stringValue = spec.Title;
            Require(serialized, "_description").stringValue = spec.Description;
            Require(serialized, "_icon").objectReferenceValue = null;
            Require(serialized, "_startConditions").arraySize = 0;
            Require(serialized, "_nextQuestIds").arraySize = 0;
            SerializedProperty objectives = Require(serialized, "_objectives");
            objectives.arraySize = spec.Objectives.Length;
            for (int i = 0; i < spec.Objectives.Length; i++)
            {
                ObjectiveSpec objective = spec.Objectives[i];
                SerializedProperty element = objectives.GetArrayElementAtIndex(i);
                Relative(element, "_type").intValue = (int)objective.Type;
                Relative(element, "_targetId").stringValue = objective.TargetId;
                Relative(element, "_requiredCount").intValue = objective.RequiredCount;
                Relative(element, "_displayText").stringValue = objective.DisplayText;
            }

            serialized.ApplyModifiedPropertiesWithoutUndo();
        }

        private static SerializedProperty Require(SerializedObject serialized, string path)
        {
            return ProgressionQuestBuilderSupport.Require(serialized, path, Scene);
        }

        private static SerializedProperty Relative(SerializedProperty owner, string path)
        {
            return ProgressionQuestBuilderSupport.Relative(owner, path, Scene);
        }

        private sealed class QuestSpec
        {
            public QuestSpec(string path, string id, string title, string description, params ObjectiveSpec[] objectives)
            {
                Path = path;
                Id = id;
                Title = title;
                Description = description;
                Objectives = objectives ?? Array.Empty<ObjectiveSpec>();
            }

            public string Path { get; }
            public string Id { get; }
            public string Title { get; }
            public string Description { get; }
            public ObjectiveSpec[] Objectives { get; }
        }

        private readonly struct ObjectiveSpec
        {
            public ObjectiveSpec(QuestObjectiveType type, string targetId, int requiredCount, string displayText)
            {
                Type = type;
                TargetId = targetId;
                RequiredCount = requiredCount;
                DisplayText = displayText;
            }

            public QuestObjectiveType Type { get; }
            public string TargetId { get; }
            public int RequiredCount { get; }
            public string DisplayText { get; }
        }
    }
}
