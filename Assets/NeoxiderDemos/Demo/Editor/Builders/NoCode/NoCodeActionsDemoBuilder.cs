using System;
using System.Globalization;
using Neo.Core.Level;
using Neo.NoCode;
using Neo.Progression;
using Neo.Quest;
using Neo.Rpg;
using Neo.Rpg.Components;
using Neo.Samples;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

namespace Neo.Demo.Editor.Builders
{
    /// <summary>
    ///     Authors the NoCode actions demo into NoCodeActionsDemo.unity around its hand-authored module objects:
    ///     'Level System', 'Progression System', 'Quest System' and 'Rpg Character', each with two *NoCodeAction
    ///     bridges as children. The pre-kit Canvas is retired; four kit panels show each module through
    ///     NoCodeFormattedText (and SetProgress for HP), the buttons call the bridges' Execute(), and the bridges' and
    ///     modules' UnityEvents write the result label and the log - all persistent listeners, no controller.
    /// </summary>
    internal static class NoCodeActionsDemoBuilder
    {
        internal const string ScenePath = "Scenes/NoCode/NoCodeActionsDemo.unity";
        internal const string LevelName = "Level System";
        internal const string ProgressionName = "Progression System";
        internal const string QuestName = "Quest System";
        internal const string RpgName = "Rpg Character";

        private const string SceneFile = "NoCodeActionsDemo.unity";

        private static readonly Color Accent = new(0.30f, 0.78f, 0.98f);
        private static readonly Color LevelColor = new(0.55f, 0.75f, 1f);
        private static readonly Color ProgressionColor = new(0.72f, 0.56f, 1f);
        private static readonly Color QuestColor = new(0.98f, 0.76f, 0.32f);
        private static readonly Color RpgColor = new(1f, 0.42f, 0.52f);

        [DemoSceneBuilder(ScenePath)]
        public static void Build(DemoSceneBuildContext ctx)
        {
            Modules modules = Step("find the hand-authored modules and bridges", () => FindModules(ctx));
            ModuleDemoSceneInfo info = Step("ensure the info service",
                () => ctx.FindOrCreate<ModuleDemoSceneInfo>("Services", "Demo Info"));
            Step("retire the pre-kit layout", () => RetireLegacyLayout(ctx));
            Step("upgrade the EventSystem input module", () => DemoSceneKit.UpgradeLegacyInputModules(ctx));
            Step("re-own the bridge and module listeners", () => PruneListeners(ctx, modules));

            DemoFrame frame = Step("author the frame", () => DemoSceneKit.Frame(ctx,
                new DemoFrameOptions("Neo.NoCode · Actions", Accent) { ContentWidth = 960f, LogLines = 5 }));
            DemoPanel content = frame.Content;
            Step("author the intro", () =>
                content.Note("Each button calls a *NoCodeAction bridge; module events write the results. No controller."));
            DemoPanel[] top = Step("author the first row", () => content.Columns("Level | Progression", 2, 20f));
            Step("author the Level panel", () => BuildLevel(ctx, top[0], modules, frame.Log));
            Step("author the Progression panel", () => BuildProgression(ctx, top[1], modules, frame.Log));
            Step("author the spacer", () => content.Spacer(4f));
            DemoPanel[] bottom = Step("author the second row", () => content.Columns("Quest | Rpg", 2, 20f));
            Step("author the Quest panel", () => BuildQuest(ctx, bottom[0], modules, frame.Log));
            Step("author the Rpg panel", () => BuildRpg(ctx, bottom[1], modules, frame.Log));
            Step("author the info card", () => frame.InfoCard(info, InfoContent()));
        }

        private static Modules FindModules(DemoSceneBuildContext ctx)
        {
            LevelComponent level = LogicDemoBuildUtility.RequireHandAuthored<LevelComponent>(ctx, LevelName, SceneFile);
            ProgressionManager progression =
                LogicDemoBuildUtility.RequireHandAuthored<ProgressionManager>(ctx, ProgressionName, SceneFile);
            QuestManager quest = LogicDemoBuildUtility.RequireHandAuthored<QuestManager>(ctx, QuestName, SceneFile);
            RpgCharacter rpg = LogicDemoBuildUtility.RequireHandAuthored<RpgCharacter>(ctx, RpgName, SceneFile);
            return new Modules(level, progression, quest, rpg,
                RequireBridge<LevelNoCodeAction>(level, "Bridge_AddXp"),
                RequireBridge<LevelNoCodeAction>(level, "Bridge_SetLevel"),
                RequireBridge<ProgressionNoCodeAction>(progression, "Bridge_AddXp"),
                RequireBridge<ProgressionNoCodeAction>(progression, "Bridge_GrantPerk"),
                RequireBridge<QuestNoCodeAction>(quest, "Bridge_Accept"),
                RequireBridge<QuestNoCodeAction>(quest, "Bridge_CompleteObjective"),
                RequireBridge<RpgNoCodeAction>(rpg, "Bridge_TakeDamage"),
                RequireBridge<RpgNoCodeAction>(rpg, "Bridge_Heal"));
        }

        private static T RequireBridge<T>(Component module, string childName) where T : Component
        {
            Transform child = module.transform.Find(childName);
            T bridge = child != null ? child.GetComponent<T>() : null;
            if (bridge == null)
            {
                throw new InvalidOperationException("'" + module.name + "/" + childName + "' with " + typeof(T).Name +
                                                    " is missing. Restore " + SceneFile + " from version control.");
            }

            return bridge;
        }

        private static void RetireLegacyLayout(DemoSceneBuildContext ctx)
        {
            GameObject canvas = ctx.FindHandAuthoredRoot("Canvas");
            if (canvas == null)
            {
                return;
            }

            bool callsBridges = LogicDemoBuildUtility.HasButtonCalling<LevelNoCodeAction>(canvas) ||
                                LogicDemoBuildUtility.HasButtonCalling<ProgressionNoCodeAction>(canvas) ||
                                LogicDemoBuildUtility.HasButtonCalling<QuestNoCodeAction>(canvas) ||
                                LogicDemoBuildUtility.HasButtonCalling<RpgNoCodeAction>(canvas);
            bool hostsModules = canvas.GetComponentInChildren<LevelComponent>(true) != null ||
                                canvas.GetComponentInChildren<QuestManager>(true) != null ||
                                canvas.GetComponentInChildren<RpgCharacter>(true) != null;
            if (canvas.GetComponent<Canvas>() != null && callsBridges && !hostsModules)
            {
                ctx.Retire(canvas,
                    "pre-kit panels and buttons; '[Demo] Frame' re-authors them with the same bridge wiring");
            }
            else
            {
                ctx.Warn("Kept hand-authored root 'Canvas': it is not the pre-kit NoCode actions layout. Delete it by " +
                         "hand if it duplicates '[Demo] Frame'.");
            }
        }

        private static void PruneListeners(DemoSceneBuildContext ctx, Modules modules)
        {
            ctx.OwnListeners(modules.Level, modules.Level.OnLevelUp);
            ctx.OwnListeners(modules.Quest, modules.Quest.OnAnyQuestAccepted);
            ctx.OwnListeners(modules.Quest, modules.Quest.OnAnyQuestCompleted);
            ctx.OwnListeners(modules.Rpg, modules.Rpg.OnDeathEvent);
            foreach (LevelNoCodeAction bridge in new[] { modules.AddXp, modules.SetLevel })
            {
                ctx.OwnListeners(bridge, bridge.OnSuccess);
                ctx.OwnListeners(bridge, bridge.OnLevelUp);
                ctx.MarkModified(bridge);
            }

            foreach (ProgressionNoCodeAction bridge in new[] { modules.ProgressionXp, modules.GrantPerk })
            {
                PruneResultEvents(ctx, bridge, bridge.OnSuccess, bridge.OnFailed, bridge.OnResultMessage);
                ctx.MarkModified(bridge);
            }

            foreach (QuestNoCodeAction bridge in new[] { modules.Accept, modules.CompleteObjective })
            {
                PruneResultEvents(ctx, bridge, bridge.OnSuccess, bridge.OnFailed, bridge.OnResultMessage);
                ctx.MarkModified(bridge);
            }

            foreach (RpgNoCodeAction bridge in new[] { modules.Damage, modules.Heal })
            {
                PruneResultEvents(ctx, bridge, bridge.OnSuccess, bridge.OnFailed, bridge.OnResultMessage);
                ctx.MarkModified(bridge);
            }

            ctx.MarkModified(modules.Level);
            ctx.MarkModified(modules.Quest);
            ctx.MarkModified(modules.Rpg);
        }

        private static void PruneResultEvents(DemoSceneBuildContext ctx, Component owner, UnityEventBase success,
            UnityEventBase failed, UnityEventBase message)
        {
            ctx.OwnListeners(owner, success);
            ctx.OwnListeners(owner, failed);
            ctx.OwnListeners(owner, message);
        }

        private static void BuildLevel(DemoSceneBuildContext ctx, DemoPanel panel, Modules modules, DemoLogFeed log)
        {
            string type = typeof(LevelComponent).FullName;
            Header(panel, "1. Level · LevelComponent", LevelName, LevelColor);
            TMP_Text readout = panel.ValueLabel("NoCodeFormattedText", "Level 1 · XP 0");
            LogicDemoBuildUtility.FormattedText(ctx, readout, "Level {0:0} · XP {1:0}", NoCodeFloatUpdateMode.Reactive,
                0.16f, new LogicBindingSource(modules.Level.gameObject, type, nameof(LevelComponent.LevelState)),
                new LogicBindingSource(modules.Level.gameObject, type, nameof(LevelComponent.TotalXp)));

            Button[] buttons = panel.ButtonRow("+" + modules.AddXp.XpAmount + " XP",
                "Set Level " + modules.SetLevel.TargetLevel);
            DemoWiring.AddVoid(buttons[0].onClick, modules.AddXp.Execute);
            DemoWiring.AddVoid(buttons[1].onClick, modules.SetLevel.Execute);

            DemoValueReadout result = ResultRow(ctx, panel);
            DemoWiring.AddString(modules.AddXp.OnSuccess, result.SetText, "AddXp -> OnSuccess");
            DemoWiring.AddString(modules.SetLevel.OnSuccess, result.SetText, "SetLevel -> OnSuccess");
            DemoWiring.AddString(modules.AddXp.OnSuccess, log.Log,
                "LevelNoCodeAction.Execute(): AddXp " + modules.AddXp.XpAmount);
            DemoWiring.AddString(modules.SetLevel.OnSuccess, log.Log,
                "LevelNoCodeAction.Execute(): SetLevel " + modules.SetLevel.TargetLevel);
            DemoWiring.AddString(modules.Level.OnLevelUp, log.Log, "LevelComponent.OnLevelUp -> reached a new level");
        }

        private static void BuildProgression(DemoSceneBuildContext ctx, DemoPanel panel, Modules modules, DemoLogFeed log)
        {
            string type = typeof(ProgressionManager).FullName;
            Header(panel, "2. Progression · ProgressionManager", ProgressionName, ProgressionColor);
            TMP_Text readout = panel.ValueLabel("NoCodeFormattedText", "XP 0 · PP 0");
            LogicDemoBuildUtility.FormattedText(ctx, readout, "XP {0:0} · PP {1:0}", NoCodeFloatUpdateMode.Reactive,
                0.16f, new LogicBindingSource(modules.Progression.gameObject, type, nameof(ProgressionManager.TotalXp)),
                new LogicBindingSource(modules.Progression.gameObject, type,
                    nameof(ProgressionManager.AvailablePerkPoints)));

            Button[] buttons = panel.ButtonRow("+" + modules.ProgressionXp.XpAmount + " XP",
                "Grant " + modules.GrantPerk.PerkPointsAmount + " PP");
            DemoWiring.AddVoid(buttons[0].onClick, modules.ProgressionXp.Execute);
            DemoWiring.AddVoid(buttons[1].onClick, modules.GrantPerk.Execute);

            DemoValueReadout result = ResultRow(ctx, panel);
            WireResult(modules.ProgressionXp.OnResultMessage, modules.ProgressionXp.OnFailed, result, log);
            WireResult(modules.GrantPerk.OnResultMessage, modules.GrantPerk.OnFailed, result, log);
        }

        private static void BuildQuest(DemoSceneBuildContext ctx, DemoPanel panel, Modules modules, DemoLogFeed log)
        {
            Header(panel, "3. Quest · QuestManager", QuestName, QuestColor);
            string questName = modules.Accept.Quest != null ? modules.Accept.Quest.name : "(no quest asset)";
            TMP_Text quest = panel.ValueLabel("Quest asset", questName);
            quest.richText = false;

            Button[] buttons = panel.ButtonRow("Accept", "Complete Obj " + modules.CompleteObjective.ObjectiveIndex);
            DemoWiring.AddVoid(buttons[0].onClick, modules.Accept.Execute);
            DemoWiring.AddVoid(buttons[1].onClick, modules.CompleteObjective.Execute);

            DemoValueReadout result = ResultRow(ctx, panel);
            WireResult(modules.Accept.OnResultMessage, modules.Accept.OnFailed, result, log);
            WireResult(modules.CompleteObjective.OnResultMessage, modules.CompleteObjective.OnFailed, result, log);
            DemoWiring.AddString(modules.Quest.OnAnyQuestAccepted, log.Log, "QuestManager.OnAnyQuestAccepted");
            DemoWiring.AddString(modules.Quest.OnAnyQuestCompleted, log.Log, "QuestManager.OnAnyQuestCompleted");
        }

        private static void BuildRpg(DemoSceneBuildContext ctx, DemoPanel panel, Modules modules, DemoLogFeed log)
        {
            string type = typeof(RpgCharacter).FullName;
            Header(panel, "4. Rpg · RpgCharacter", RpgName, RpgColor);
            Image fill = LogicDemoBuildUtility.FilledBar(ctx, panel, "HP · NoCodeFormattedText + SetProgress", RpgColor,
                0.8f, out TMP_Text hp);
            LogicDemoBuildUtility.FormattedText(ctx, hp, "{0:0} / {1:0}", NoCodeFloatUpdateMode.Reactive, 0.16f,
                new LogicBindingSource(modules.Rpg.gameObject, type, nameof(RpgCharacter.CurrentHp)),
                new LogicBindingSource(modules.Rpg.gameObject, type, nameof(RpgCharacter.MaxHp)));
            SetProgress progress = ctx.Ensure<SetProgress>(fill.transform.parent.parent.gameObject);
            LogicDemoBuildUtility.Bind(ctx, progress, modules.Rpg.gameObject, type, nameof(RpgCharacter.HpPercentValue),
                NoCodeFloatUpdateMode.Reactive);
            ctx.Set(progress, "_image", fill);
            ctx.Set(progress, "_slider", null);
            ctx.Set(progress, "_minValue", 0f);
            ctx.Set(progress, "_maxValue", 1f);

            Button[] buttons = panel.ButtonRow("Take " + Amount(modules.Damage) + " dmg", "Heal " + Amount(modules.Heal));
            DemoWiring.AddVoid(buttons[0].onClick, modules.Damage.Execute);
            DemoWiring.AddVoid(buttons[1].onClick, modules.Heal.Execute);

            DemoValueReadout result = ResultRow(ctx, panel);
            WireResult(modules.Damage.OnResultMessage, modules.Damage.OnFailed, result, log);
            WireResult(modules.Heal.OnResultMessage, modules.Heal.OnFailed, result, log);
            DemoWiring.AddString(modules.Rpg.OnDeathEvent, log.Log, "RpgCharacter.OnDeath");
        }

        private static void Header(DemoPanel panel, string caption, string objectName, Color color)
        {
            TMP_Text section = panel.Section(caption);
            section.color = color;
            panel.Note("Object '" + objectName + "' · 2 bridges as children");
        }

        private static DemoValueReadout ResultRow(DemoSceneBuildContext ctx, DemoPanel panel)
        {
            TMP_Text value = panel.ValueLabel("Result", "-");
            value.fontSize = 17f;
            value.enableAutoSizing = true;
            value.fontSizeMin = 11f;
            value.fontSizeMax = 17f;
            value.textWrappingMode = TextWrappingModes.NoWrap;
            value.overflowMode = TextOverflowModes.Ellipsis;
            value.richText = false;
            DemoValueReadout readout = ctx.Ensure<DemoValueReadout>(value.gameObject);
            readout.Configure(value, "0");
            return readout;
        }

        private static void WireResult(UnityEvent<string> message, UnityEvent<string> failed, DemoValueReadout result,
            DemoLogFeed log)
        {
            DemoWiring.AddDynamic(message, result.SetText);
            DemoWiring.AddDynamic(message, log.Log);
            DemoWiring.AddDynamic(failed, result.SetText);
        }

        private static string Amount(RpgNoCodeAction bridge)
        {
            SerializedProperty amount = new SerializedObject(bridge).FindProperty("_amount");
            float value = amount != null ? amount.floatValue : 0f;
            return value.ToString("0.#", CultureInfo.InvariantCulture);
        }

        private static DemoInfoContent InfoContent()
        {
            return new DemoInfoContent
            {
                Title = "Neo.NoCode · Actions",
                Purpose = "NoCode bridges turn module calls into components: each *NoCodeAction holds one typed call " +
                          "(AddXp, Accept, TakeDamage...) and exposes Execute() plus result events.",
                RuntimeApi = "Same calls in C#: LevelComponent.AddXp, ProgressionManager.TryAddXp, " +
                             "QuestManager.AcceptQuest, RpgCharacter.Damage / Heal; bridge.Execute() from code.",
                SceneWorkflow = "Select a bridge (for example '" + LevelName + "/Bridge_AddXp'): action type and amount " +
                                "are Inspector fields; buttons call Execute(), result events write the labels.",
                Verification = "Play: +XP raises the level, Accept then Complete finishes the quest, Take dmg lowers " +
                               "the HP bar.",
                Hints = new[]
                {
                    "Button.onClick -> *NoCodeAction.Execute()",
                    "OnResultMessage / OnSuccess -> result label and log",
                    "NoCodeFormattedText / SetProgress -> module values"
                },
                Corner = ModuleDemoSceneInfo.Corner.TopLeft,
                PanelWidth = 440f,
                StartCollapsed = false
            };
        }

        private static void Step(string step, Action action)
        {
            DemoSceneKit.Step(SceneFile, step, action);
        }

        private static T Step<T>(string step, Func<T> action)
        {
            return DemoSceneKit.Step(SceneFile, step, action);
        }

        private sealed class Modules
        {
            public Modules(LevelComponent level, ProgressionManager progression, QuestManager quest, RpgCharacter rpg,
                LevelNoCodeAction addXp, LevelNoCodeAction setLevel, ProgressionNoCodeAction progressionXp,
                ProgressionNoCodeAction grantPerk, QuestNoCodeAction accept, QuestNoCodeAction completeObjective,
                RpgNoCodeAction damage, RpgNoCodeAction heal)
            {
                Level = level;
                Progression = progression;
                Quest = quest;
                Rpg = rpg;
                AddXp = addXp;
                SetLevel = setLevel;
                ProgressionXp = progressionXp;
                GrantPerk = grantPerk;
                Accept = accept;
                CompleteObjective = completeObjective;
                Damage = damage;
                Heal = heal;
            }

            public LevelComponent Level { get; }
            public ProgressionManager Progression { get; }
            public QuestManager Quest { get; }
            public RpgCharacter Rpg { get; }
            public LevelNoCodeAction AddXp { get; }
            public LevelNoCodeAction SetLevel { get; }
            public ProgressionNoCodeAction ProgressionXp { get; }
            public ProgressionNoCodeAction GrantPerk { get; }
            public QuestNoCodeAction Accept { get; }
            public QuestNoCodeAction CompleteObjective { get; }
            public RpgNoCodeAction Damage { get; }
            public RpgNoCodeAction Heal { get; }
        }
    }
}
