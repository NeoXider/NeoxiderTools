using System;
using System.Collections.Generic;
using Neo.Condition;
using Neo.Rpg;
using Neo.Rpg.Components;
using Neo.Rpg.Demo;
using Neo.Samples;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;
using ValueType = Neo.Condition.ValueType;

namespace Neo.Demo.Editor.Builders
{
    /// <summary>
    ///     Authors the Neo.Rpg quick demo (Scenes/RpgCharacterQuickDemo.unity). The hand-authored world - player
    ///     capsule, enemy cube, ground, light, camera and the RpgCharacterQuickDemo controller - is kept and configured:
    ///     both characters get RpgCharacterTemplate assets, every live row is a NoCode binding saved in the scene
    ///     (RpgResourceBinding, RpgStatBinding, RpgCharacter.OnLevelChanged), the NoCode buttons carry RpgNoCodeAction
    ///     and a NeoCondition drives the low-HP row. Duplicates left by the old runtime bootstrap are removed on the
    ///     first pass; a missing character is authored under "[Demo] World".
    /// </summary>
    internal static class RpgCharacterQuickDemoBuilder
    {
        internal const string ScenePath = "Scenes/RpgCharacterQuickDemo.unity";
        internal const string PlayerName = "Rpg Demo Player";
        internal const string EnemyName = "Rpg Demo Enemy";
        internal const string GroundName = "Rpg Demo Ground";
        internal const string LightName = "Directional Light";
        internal const string ControllerName = "RpgCharacterQuickDemo";
        internal const string CustomResource = "DarkMana";
        internal const string LowHpRowName = "Condition · Low HP";
        internal const string EnemyPanelName = "Enemy Card";
        internal const float LowHpThreshold = 0.35f;
        internal const string PlayerTemplatePath = DataFolder + "QuickDemoPlayer.asset";
        internal const string EnemyTemplatePath = DataFolder + "QuickDemoEnemy.asset";
        internal const string ProgressionPath = DataFolder + "QuickDemoProgression.asset";

        private const string SceneName = "RpgCharacterQuickDemo.unity";
        private const string DataFolder = "Data/RpgCharacterQuickDemo/";
        private const string WorldRootKey = "World";
        private const float ContentWidth = 560f;
        private const float SidePanelWidth = 440f;
        private const float PlayerHp = 100f;
        private const float PlayerStamina = 100f;
        private const float PlayerDarkMana = 40f;
        private const float PlayerVitality = 10f;
        private const float EnemyHp = 120f;

        [DemoSceneBuilder(ScenePath)]
        public static void Build(DemoSceneBuildContext ctx)
        {
            Step("remove bootstrap leftovers", () => RemoveLeftovers(ctx));
            Templates templates = Step("author the template assets", AuthorTemplates);
            RpgCharacter player = Step("set up " + PlayerName, () => SetUpCharacter(ctx, PlayerName,
                PrimitiveType.Capsule, new Vector3(-1.5f, 1f, 0f), templates.Player, "blue.mat"));
            RpgCharacter enemy = Step("set up " + EnemyName, () => SetUpCharacter(ctx, EnemyName,
                PrimitiveType.Cube, new Vector3(1.5f, 0.5f, 0f), templates.Enemy, "red.mat"));
            RpgCharacterQuickDemo controller = Step("find the demo controller", () => FindController(ctx));
            ModuleDemoSceneInfo info = Step("ensure the info component",
                () => ctx.FindOrCreate<ModuleDemoSceneInfo>("Services", "Demo Info"));

            DemoFrame frame = Step("author the frame", () => DemoSceneKit.Frame(ctx,
                new DemoFrameOptions("Neo.Rpg · RpgCharacter", RpgDemoBuilderUtility.Accent)
                {
                    Backdrop = DemoBackdrop.None,
                    Placement = DemoContentPlacement.Left,
                    ContentWidth = ContentWidth,
                    Overline = "NEOXIDER · RPG MODULE DEMO",
                    LogLines = 5
                }));
            Step("darken the camera background",
                () => RpgDemoBuilderUtility.DarkBackground(ctx, frame.Camera, RpgDemoBuilderUtility.Accent));

            PlayerCard card = Step("author the player card", () => BuildPlayerCard(ctx, frame, player, controller));
            Step("author the enemy and NoCode card", () => BuildEnemyCard(ctx, frame, player, enemy, controller));
            Step("author the info card", () => frame.InfoCard(info, InfoContent()));
            Step("assign the controller references", () =>
            {
                ctx.Set(controller, "_player", player);
                ctx.Set(controller, "_enemy", enemy);
                ctx.Set(controller, "_customManaId", CustomResource);
                ctx.Set(controller, "_enemyHitType", "Fire");
                ctx.Set(controller, "_progressValue", card.Progress);
                ctx.Set(controller, "_guardToggle", card.Guard);
                ctx.Set(controller, "_log", frame.Log);
            });
        }

        private static void RemoveLeftovers(DemoSceneBuildContext ctx)
        {
            GameObject player = RpgDemoBuilderUtility.KeepSingleRoot(ctx, PlayerName, HasCharacter);
            RpgDemoBuilderUtility.KeepSingleRoot(ctx, EnemyName, HasCharacter);
            RpgDemoBuilderUtility.KeepSingleRoot(ctx, ControllerName,
                root => root.GetComponent<RpgCharacterQuickDemo>() != null);
            RpgDemoBuilderUtility.KeepSingleRoot(ctx, LightName, root => root.GetComponent<Light>() != null);

            List<GameObject> grounds = RpgDemoBuilderUtility.NamedRoots(ctx, GroundName);
            if (grounds.Count < 2)
            {
                return;
            }

            // WHY: the leftover ground was placed for the old bootstrap positions; keep the one under the player.
            Vector3 anchor = player != null ? player.transform.position : Vector3.zero;
            GameObject nearest = grounds[0];
            float best = float.PositiveInfinity;
            foreach (GameObject ground in grounds)
            {
                float distance = (ground.transform.position - anchor).sqrMagnitude;
                if (distance < best)
                {
                    best = distance;
                    nearest = ground;
                }
            }

            RpgDemoBuilderUtility.RemoveAllBut(ctx, grounds, nearest);
        }

        private static bool HasCharacter(GameObject root)
        {
            return root.GetComponent<RpgCharacter>() != null;
        }

        private static Templates AuthorTemplates()
        {
            RpgProgressionDefinition progression =
                DemoSceneAssets.EnsureAsset<RpgProgressionDefinition>(ProgressionPath, ConfigureProgression);
            RpgCharacterTemplate player = DemoSceneAssets.EnsureAsset<RpgCharacterTemplate>(PlayerTemplatePath,
                template => ConfigurePlayer(template, progression));
            RpgCharacterTemplate enemy = DemoSceneAssets.EnsureAsset<RpgCharacterTemplate>(EnemyTemplatePath,
                template => ConfigureEnemy(template, progression));
            return new Templates(player, enemy);
        }

        private static void ConfigureProgression(RpgProgressionDefinition progression)
        {
            progression.growthMode = RpgLevelGrowthMode.Hybrid;
            progression.upgradePointsPerLevel = 1;
            progression.autoApplyGrowthOnLevelUp = true;
            progression.upgradeRules = new[]
            {
                new RpgStatUpgradeRule
                {
                    statId = new RpgStatId(RpgStatPreset.Vitality),
                    increasePerPoint = 1f,
                    costPerUpgrade = 1,
                    maxUpgradeCount = -1,
                    derivedResourceModifiers = new[]
                    {
                        new RpgResourceModifier
                        {
                            resourceId = new RpgStatId(RpgStatPreset.Hp),
                            kind = RpgResourceModifierKind.AddMaxFlat,
                            value = 15f
                        }
                    }
                }
            };
        }

        private static void ConfigurePlayer(RpgCharacterTemplate template, RpgProgressionDefinition progression)
        {
            template.displayName = "Demo Player";
            template.description = "Quick demo player: HP, Stamina and the custom DarkMana resource.";
            template.resources = new[]
            {
                Resource(new RpgStatId(RpgStatPreset.Hp), PlayerHp, 1f, 0f, true),
                Resource(new RpgStatId(RpgStatPreset.Stamina), PlayerStamina, 15f, 1f, false),
                Resource(new RpgStatId(CustomResource), PlayerDarkMana, 2f, 0f, false)
            };
            template.stats = new[]
            {
                Stat(RpgStatPreset.Strength, 12f, true),
                Stat(RpgStatPreset.Vitality, PlayerVitality, false),
                Stat(RpgStatPreset.Defense, 5f, true)
            };
            template.progression = progression;
        }

        private static void ConfigureEnemy(RpgCharacterTemplate template, RpgProgressionDefinition progression)
        {
            template.displayName = "Demo Enemy";
            template.description = "Quick demo target dummy.";
            template.resources = new[]
            {
                Resource(new RpgStatId(RpgStatPreset.Hp), EnemyHp, 2f, 0f, true)
            };
            template.stats = new[]
            {
                Stat(RpgStatPreset.Strength, 8f, true),
                Stat(RpgStatPreset.Defense, 5f, true)
            };
            template.progression = progression;
        }

        private static RpgResourceDefinition Resource(RpgStatId id, float max, float regen, float pauseAfterSpend,
            bool pauseAfterDamage)
        {
            return new RpgResourceDefinition
            {
                id = id,
                startCurrent = max,
                startMax = max,
                restoreOnAwake = true,
                restoreToFull = true,
                regen = new RpgRegenDefinition
                {
                    enabled = regen > 0f,
                    mode = RpgRegenMode.FlatPerSecond,
                    value = regen,
                    pauseAfterSpend = pauseAfterSpend > 0f,
                    pauseAfterSpendSeconds = pauseAfterSpend,
                    pauseAfterDamage = pauseAfterDamage,
                    pauseAfterDamageSeconds = 3f
                }
            };
        }

        private static RpgStatDefinition Stat(RpgStatPreset preset, float value, bool affectedByLevel)
        {
            return new RpgStatDefinition
            {
                id = new RpgStatId(preset),
                baseValue = value,
                affectedByLevel = affectedByLevel,
                growth = new RpgStatGrowthRule { BaseValue = 0f, AddPerLevel = 1f }
            };
        }

        private static RpgCharacter SetUpCharacter(DemoSceneBuildContext ctx, string objectName, PrimitiveType shape,
            Vector3 position, RpgCharacterTemplate template, string material)
        {
            List<GameObject> named = RpgDemoBuilderUtility.NamedRoots(ctx, objectName);
            GameObject body;
            if (named.Count > 0)
            {
                body = named[0];
            }
            else
            {
                body = ctx.Primitive(ctx.Root(WorldRootKey).transform, objectName, shape);
                body.transform.SetPositionAndRotation(position, Quaternion.identity);
                ctx.Note("'" + objectName + "' was missing; authored it under [Demo] " + WorldRootKey + ".");
            }

            RpgCharacter character = ctx.Ensure<RpgCharacter>(body);
            ctx.Set(character, "_template", template);
            ctx.Set(character, "_applyTemplateOnAwake", true);
            ctx.Set(character, "isNetworked", false);
            ctx.Set(character, "_loadOnAwake", false);
            ctx.Set(character, "_autoSave", false);
            RpgDemoBuilderUtility.Paint(ctx, character, material);
            return character;
        }

        private static RpgCharacterQuickDemo FindController(DemoSceneBuildContext ctx)
        {
            List<RpgCharacterQuickDemo> controllers = ctx.FindAll<RpgCharacterQuickDemo>();
            return controllers.Count > 0
                ? controllers[0]
                : ctx.FindOrCreate<RpgCharacterQuickDemo>("Services", "Demo Controller");
        }

        private static PlayerCard BuildPlayerCard(DemoSceneBuildContext ctx, DemoFrame frame, RpgCharacter player,
            RpgCharacterQuickDemo controller)
        {
            DemoPanel card = frame.Content;
            card.Section("Player · " + PlayerName);
            card.Note("Template QuickDemoPlayer · rows use RpgResourceBinding");
            RpgDemoBuilderUtility.ResourceRow(ctx, card, "HP", player, new RpgStatId(RpgStatPreset.Hp),
                DemoPalette.Good, PlayerHp, PlayerHp);
            RpgDemoBuilderUtility.ResourceRow(ctx, card, "Stamina", player, new RpgStatId(RpgStatPreset.Stamina),
                DemoPalette.Cyan, PlayerStamina, PlayerStamina);
            RpgDemoBuilderUtility.ResourceRow(ctx, card, CustomResource + " (custom id)", player,
                new RpgStatId(CustomResource), DemoPalette.Accent, PlayerDarkMana, PlayerDarkMana);
            LowHpRow(ctx, card, player);

            TMP_Text level = card.ValueLabel("Level", "1");
            ctx.OwnListeners(player, player.OnLevelChangedEvent);
            DemoWiring.AddDynamic(player.OnLevelChangedEvent, DemoSceneKit.Readout(ctx, level, "0").SetInt);
            ctx.MarkModified(player);
            TMP_Text progress = card.ValueLabel("XP · points", "0 XP · 0 pts");
            RpgDemoBuilderUtility.StatRow(ctx, card, "Vitality", player, new RpgStatId(RpgStatPreset.Vitality),
                PlayerVitality);

            card.Section("Call the C# API");
            Button[] health = card.ButtonRow("Damage 25", "Heal 15");
            DemoWiring.AddFloat(health[0].onClick, controller.DamagePlayer, 25f);
            DemoWiring.AddFloat(health[1].onClick, controller.HealPlayer, 15f);

            Button[] resources = card.ButtonRow("Spend Stamina 30", "Refill DarkMana 15");
            DemoWiring.AddFloat(resources[0].onClick, controller.SpendPlayerStamina, 30f);
            DemoWiring.AddFloat(resources[1].onClick, controller.RefillPlayerCustomResource, 15f);

            Button[] growth = card.ButtonRow("Add 50 XP", "Level Up", "Upgrade Vitality");
            DemoWiring.AddFloat(growth[0].onClick, controller.AddPlayerXp, 50f);
            DemoWiring.AddInt(growth[1].onClick, controller.AddPlayerLevel, 1);
            DemoWiring.AddString(growth[2].onClick, controller.UpgradePlayerStat, nameof(RpgStatPreset.Vitality));

            Toggle guard = card.Toggle("Guard (IRpgDamageModifier ×0.5)", false);
            DemoWiring.AddDynamic(guard.onValueChanged, controller.SetPlayerGuard);
            card.Note("Every button logs its RpgCharacter call below");
            return new PlayerCard(progress, guard);
        }

        /// <summary>
        ///     Row with a NeoCondition that reads RpgCharacter.HpPercentValue every 0.2 s and swaps the idle hint for
        ///     the red warning through OnResult / OnInvertedResult (GameObject.SetActive) - no code involved.
        /// </summary>
        private static void LowHpRow(DemoSceneBuildContext ctx, DemoPanel card, RpgCharacter player)
        {
            RectTransform row = card.Row(LowHpRowName, 30f);
            TMP_Text idle = DemoSceneKit.Text(ctx, row, "Idle", "NeoCondition watches HP: below 35% shows a warning",
                16f, DemoPalette.Muted, TextAlignmentOptions.MidlineLeft);
            DemoSceneKit.Stretch(idle.rectTransform);
            TMP_Text warning = DemoSceneKit.Text(ctx, row, "Warning", "LOW HP · NeoCondition fired (HP below 35%)", 18f,
                DemoPalette.Danger, TextAlignmentOptions.MidlineLeft, FontStyles.Bold);
            DemoSceneKit.Stretch(warning.rectTransform);

            NeoCondition condition = ctx.Ensure<NeoCondition>(row.gameObject);
            ctx.Set(condition, "isNetworked", false);
            ctx.Set(condition, "_logicMode", (int)LogicMode.AND);
            ctx.Set(condition, "_checkMode", (int)CheckMode.Interval);
            ctx.Set(condition, "_checkInterval", 0.2f);
            ctx.Set(condition, "_checkOnStart", true);
            ctx.Set(condition, "_onlyOnChange", true);

            // WHY: AddCondition appends; clearing first keeps rebuilds idempotent.
            SerializedObject serialized = new(condition);
            serialized.FindProperty("_conditions").ClearArray();
            serialized.ApplyModifiedPropertiesWithoutUndo();
            condition.AddCondition(new ConditionEntry
            {
                Source = SourceMode.Component,
                SourceObject = player.gameObject,
                ComponentTypeName = typeof(RpgCharacter).FullName,
                PropertyName = nameof(RpgCharacter.HpPercentValue),
                CurrentValueType = ValueType.Float,
                Compare = CompareOp.LessOrEqual,
                ThresholdFloat = LowHpThreshold
            });
            ctx.MarkModified(condition);

            DemoWiring.AddDynamic(condition.OnResult, warning.gameObject.SetActive);
            DemoWiring.AddDynamic(condition.OnInvertedResult, idle.gameObject.SetActive);
            warning.gameObject.SetActive(false);
            idle.gameObject.SetActive(true);
        }

        private static void BuildEnemyCard(DemoSceneBuildContext ctx, DemoFrame frame, RpgCharacter player,
            RpgCharacter enemy, RpgCharacterQuickDemo controller)
        {
            DemoPanel panel = frame.Panel(EnemyPanelName, DemoPanelPlacement.BottomRight, SidePanelWidth);
            panel.Section("Enemy · " + EnemyName);
            RpgDemoBuilderUtility.ResourceRow(ctx, panel, "HP", enemy, new RpgStatId(RpgStatPreset.Hp),
                DemoPalette.Danger, EnemyHp, EnemyHp);
            Button[] hits = panel.ButtonRow("Fire Hit 40", "Restore All");
            DemoWiring.AddFloat(hits[0].onClick, controller.HitEnemy, 40f);
            DemoWiring.AddVoid(hits[1].onClick, controller.RestoreAll);
            panel.Note("Fire Hit = enemy.ApplyDamage(new RpgDamageInfo(40, \"Fire\", player))", 40f);

            panel.Section("NoCode · RpgNoCodeAction");
            TMP_Text result = panel.ValueLabel("Result", "-");
            DemoValueReadout resultReadout = DemoSceneKit.Readout(ctx, result, "0");
            Button[] noCode = panel.ButtonRow("NoCode Heal Player 20", "NoCode Damage Enemy 30");
            RpgDemoBuilderUtility.NoCodeButton(ctx, noCode[0], RpgNoCodeActionType.Heal, player, 20f, resultReadout,
                frame.Log);
            RpgDemoBuilderUtility.NoCodeButton(ctx, noCode[1], RpgNoCodeActionType.TakeDamage, enemy, 30f,
                resultReadout, frame.Log);
            panel.Note("Select a NoCode button: its RpgNoCodeAction holds the action.", 40f);
        }

        private static DemoInfoContent InfoContent()
        {
            return new DemoInfoContent
            {
                Title = "Neo.Rpg · RpgCharacter",
                Purpose = "One RpgCharacter holds resources (HP, Stamina, custom DarkMana), stats, level, upgrade " +
                          "points and the damage pipeline. Both characters are configured by RpgCharacterTemplate " +
                          "assets, not code.",
                RuntimeApi = "player.Damage / Heal / Spend(\"Stamina\", 30) / Refill(\"DarkMana\", 15) / AddLevel / " +
                             "UpgradeStat(\"Vitality\"); enemy.ApplyDamage(new RpgDamageInfo(40, \"Fire\", player)); " +
                             "AddDamageModifier; DamageApplied event.",
                SceneWorkflow = "Select " + PlayerName + " (Template field). Rows carry RpgResourceBinding / " +
                                "RpgStatBinding, NoCode buttons carry RpgNoCodeAction, the Low HP row carries a " +
                                "NeoCondition.",
                Verification = "Damage the player below 35% HP: the Low HP row turns red without code. Level Up " +
                               "grants a point; Upgrade Vitality adds +15 Max HP.",
                Hints = Array.Empty<string>(),
                Corner = ModuleDemoSceneInfo.Corner.TopRight,
                PanelWidth = SidePanelWidth,
                StartCollapsed = false
            };
        }

        private static void Step(string step, Action action)
        {
            DemoSceneKit.Step(SceneName, step, action);
        }

        private static T Step<T>(string step, Func<T> action)
        {
            return DemoSceneKit.Step(SceneName, step, action);
        }

        private sealed class Templates
        {
            public Templates(RpgCharacterTemplate player, RpgCharacterTemplate enemy)
            {
                Player = player;
                Enemy = enemy;
            }

            public RpgCharacterTemplate Player { get; }
            public RpgCharacterTemplate Enemy { get; }
        }

        private sealed class PlayerCard
        {
            public PlayerCard(TMP_Text progress, Toggle guard)
            {
                Progress = progress;
                Guard = guard;
            }

            public TMP_Text Progress { get; }
            public Toggle Guard { get; }
        }
    }
}
