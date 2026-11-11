using System;
using System.Collections.Generic;
using Neo.NPC.Combat;
using Neo.Rpg;
using Neo.Rpg.Components;
using Neo.Rpg.Demo;
using Neo.Samples;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Neo.Demo.Editor.Builders
{
    /// <summary>
    ///     Decorates the hand-authored RPG combat scene (Scenes/RpgCombatNpcDemo.unity). The combatants - Player
    ///     (RpgCharacter + RpgAttackController + KeyboardMover), MeleeNpc (RpgContactDamage), RangedNpc
    ///     (NpcRpgCombatBrain) - and the RpgCombatDemoController stay hand-authored; this builder assigns every reference
    ///     explicitly, authors the HUD card, the health bar canvas, the log feed and the info card, and wires the
    ///     player/NPC UnityEvents to them with persistent listeners.
    /// </summary>
    internal static class RpgCombatNpcDemoBuilder
    {
        internal const string ScenePath = "Scenes/RpgCombatNpcDemo.unity";
        internal const string HealthBarsRootKey = "Health Bars";
        internal const string StatusRowName = "Status · Player";
        internal const string LastEventRowName = "Event · Last";

        private const string SceneName = "RpgCombatNpcDemo.unity";
        private const string PlayerTag = "Player";
        private const string EnemyTag = "Enemy";
        private const float ContentWidth = 500f;
        private const float InfoWidth = 440f;
        private const int HealthBarsSortingOrder = 40;

        private static readonly string[] EnemyMaterials = { "red.mat", "yellow.mat", "green.mat" };

        [DemoSceneBuilder(ScenePath)]
        public static void Build(DemoSceneBuildContext ctx)
        {
            CombatScene scene = Step("find the hand-authored combatants", () => FindCombat(ctx));
            ModuleDemoSceneInfo info = Step("ensure the info component",
                () => ctx.FindOrCreate<ModuleDemoSceneInfo>("Services", "Demo Info"));
            Step("assign the controller references", () => AssignController(ctx, scene));
            Step("paint the combatants", () => PaintCombatants(ctx, scene));

            DemoFrame frame = Step("author the frame", () => DemoSceneKit.Frame(ctx,
                new DemoFrameOptions("Neo.Rpg · Combat with NPCs", RpgDemoBuilderUtility.Accent)
                {
                    Backdrop = DemoBackdrop.None,
                    Placement = DemoContentPlacement.Left,
                    ContentWidth = ContentWidth,
                    Overline = "NEOXIDER · RPG MODULE DEMO",
                    LogLines = 5
                }));
            Step("darken the camera background",
                () => RpgDemoBuilderUtility.DarkBackground(ctx, frame.Camera, RpgDemoBuilderUtility.Accent));
            Step("author the health bar canvas", () => AuthorHealthBars(ctx, scene, frame.Camera));
            Hud hud = Step("author the combat HUD", () => BuildHud(ctx, frame, scene));
            Step("wire the player and NPC events", () => WireSceneEvents(ctx, scene, hud, frame.Log));
            Step("author the info card", () => frame.InfoCard(info, InfoContent(scene)));
            Step("assign the HUD references", () =>
            {
                ctx.Set(scene.Controller, "_enemiesValue", hud.Enemies);
                ctx.Set(scene.Controller, "_lastEventValue", hud.LastEvent);
                ctx.Set(scene.Controller, "_autoToggle", hud.AutoLoop);
                ctx.Set(scene.Controller, "_pressureToggle", hud.Pressure);
                ctx.Set(scene.Controller, "_log", frame.Log);
            });
        }

        private static CombatScene FindCombat(DemoSceneBuildContext ctx)
        {
            List<RpgCombatDemoController> controllers = ctx.FindAll<RpgCombatDemoController>();
            if (controllers.Count != 1)
            {
                throw new InvalidOperationException("expected exactly one hand-authored RpgCombatDemoController, found " +
                                                    controllers.Count + ". This builder decorates the hand-authored " +
                                                    "combat scene; restore it from version control instead.");
            }

            RpgCharacter player = null;
            List<RpgCharacter> enemies = new();
            foreach (RpgCharacter character in ctx.FindAll<RpgCharacter>())
            {
                if (character.CompareTag(PlayerTag))
                {
                    if (player != null)
                    {
                        throw new InvalidOperationException("two RpgCharacters are tagged Player ('" + player.name +
                                                            "', '" + character.name + "'); keep one.");
                    }

                    player = character;
                }
                else if (character.CompareTag(EnemyTag))
                {
                    enemies.Add(character);
                }
            }

            if (player == null)
            {
                throw new InvalidOperationException("no RpgCharacter is tagged Player.");
            }

            if (enemies.Count == 0)
            {
                throw new InvalidOperationException("no RpgCharacter is tagged Enemy.");
            }

            RpgCombatDemoController controller = controllers[0];
            RpgWorldHealthBars healthBars = ctx.Find<RpgWorldHealthBars>();
            if (healthBars == null)
            {
                healthBars = ctx.Ensure<RpgWorldHealthBars>(controller.gameObject);
                ctx.Set(healthBars, "_targetTag", EnemyTag);
                ctx.Note("Added RpgWorldHealthBars to '" + controller.name + "'.");
            }

            RpgContactDamage contact = null;
            NpcRpgCombatBrain brain = null;
            foreach (RpgCharacter enemy in enemies)
            {
                if (contact == null)
                {
                    contact = enemy.GetComponent<RpgContactDamage>();
                }

                if (brain == null)
                {
                    brain = enemy.GetComponent<NpcRpgCombatBrain>();
                }
            }

            return new CombatScene(controller, player, player.GetComponent<RpgAttackController>(), enemies, healthBars,
                contact, brain);
        }

        private static void AssignController(DemoSceneBuildContext ctx, CombatScene scene)
        {
            ctx.Set(scene.Controller, "_player", scene.Player);
            ctx.Set(scene.Controller, "_playerAttack", scene.PlayerAttack);
            ctx.Set(scene.Controller, "_enemies", scene.Enemies);
        }

        private static void PaintCombatants(DemoSceneBuildContext ctx, CombatScene scene)
        {
            RpgDemoBuilderUtility.Paint(ctx, scene.Player, "blue.mat");
            for (int i = 0; i < scene.Enemies.Count; i++)
            {
                RpgDemoBuilderUtility.Paint(ctx, scene.Enemies[i], EnemyMaterials[i % EnemyMaterials.Length]);
            }
        }

        /// <summary>
        ///     Own overlay canvas under the frame; RpgWorldHealthBars only pools the bar views inside it. The NPCs are
        ///     assigned as its scene targets, so it never searches the scene at runtime.
        /// </summary>
        private static void AuthorHealthBars(DemoSceneBuildContext ctx, CombatScene scene, Camera camera)
        {
            RpgWorldHealthBars healthBars = scene.HealthBars;
            ctx.Set(healthBars, "_sceneTargets", scene.Enemies);
            ctx.Set(healthBars, "_scanSceneForTargets", false);
            GameObject root = ctx.Root(HealthBarsRootKey, true);
            Canvas canvas = DemoSceneKit.OverlayCanvas(ctx, root, HealthBarsSortingOrder, new Vector2(1920f, 1080f));
            RectTransform bars = ctx.UiChild(root.transform, "Bars");
            DemoSceneKit.Stretch(bars);
            ctx.Set(healthBars, "_canvas", canvas);
            ctx.Set(healthBars, "_barRoot", bars);
            ctx.Set(healthBars, "_createCanvasIfMissing", false);
            ctx.Set(healthBars, "_sortingOrder", HealthBarsSortingOrder);
            ctx.Set(healthBars, "_worldCamera", camera);
        }

        private static Hud BuildHud(DemoSceneBuildContext ctx, DemoFrame frame, CombatScene scene)
        {
            DemoPanel card = frame.Content;
            RpgCombatDemoController controller = scene.Controller;

            card.Section("Player · RpgCharacter + RpgAttackController");
            InitialHp(scene.Player, out float hp, out float maxHp);
            RpgDemoBuilderUtility.ResourceRow(ctx, card, "Player HP", scene.Player, new RpgStatId(RpgStatPreset.Hp),
                DemoPalette.Good, hp, maxHp);
            StatusRow(ctx, card, out TMP_Text alive, out TMP_Text dead);
            TMP_Text enemies = card.ValueLabel("Enemies alive",
                scene.Enemies.Count + " / " + scene.Enemies.Count + " · deaths 0");
            TMP_Text lastEvent = EventLine(ctx, card, LastEventRowName, controller.LastEvent);

            DemoValueReadout brain = null;
            if (scene.Brain != null)
            {
                TMP_Text decision = card.ValueLabel(scene.Brain.name + " brain", "-");
                brain = DemoSceneKit.Readout(ctx, decision, "0");
            }

            card.Section("Controls");
            Button[] hits = card.ButtonRow("Player Hit", "Enemy Hit");
            DemoWiring.AddVoid(hits[0].onClick, controller.DamageNearestEnemy);
            DemoWiring.AddVoid(hits[1].onClick, controller.PressurePlayer);
            if (scene.PlayerAttack != null)
            {
                Button slash = card.Button("NoCode Sword Slash");
                RpgDemoBuilderUtility.NoCodeButton(ctx, slash, RpgNoCodeActionType.UsePrimaryAttack, null, 0f, null,
                    frame.Log, scene.PlayerAttack);
            }
            else
            {
                ctx.Warn("'" + scene.Player.name + "' has no RpgAttackController; the NoCode attack button is skipped.");
            }

            Toggle autoLoop = card.Toggle("Auto loop", controller.AutoDemoLoop);
            DemoWiring.AddDynamic(autoLoop.onValueChanged, controller.SetAutoDemoLoop);
            Toggle pressure = card.Toggle("Enemy pressure", controller.EnemyPressureEnabled);
            DemoWiring.AddDynamic(pressure.onValueChanged, controller.SetEnemyPressure);
            Button restore = card.Button("Restore All");
            DemoWiring.AddVoid(restore.onClick, controller.RestoreAll);
            card.Note("WASD moves · left click swings the sword");
            return new Hud(enemies, lastEvent, autoLoop, pressure, alive, dead, brain);
        }

        /// <summary>Alive hint / dead warning pair switched by the player's OnDeath and OnRevived events.</summary>
        private static void StatusRow(DemoSceneBuildContext ctx, DemoPanel card, out TMP_Text alive, out TMP_Text dead)
        {
            RectTransform row = card.Row(StatusRowName, 30f);
            alive = DemoSceneKit.Text(ctx, row, "Alive", "Alive · RpgCharacter.OnDeath / OnRevived switch this row", 16f,
                DemoPalette.Muted, TextAlignmentOptions.MidlineLeft);
            DemoSceneKit.Stretch(alive.rectTransform);
            dead = DemoSceneKit.Text(ctx, row, "Dead", "PLAYER DEAD · press Restore All", 18f, DemoPalette.Danger,
                TextAlignmentOptions.MidlineLeft, FontStyles.Bold);
            DemoSceneKit.Stretch(dead.rectTransform);
            dead.gameObject.SetActive(false);
            alive.gameObject.SetActive(true);
        }

        /// <summary>Full-width single line (ellipsis) for long runtime messages.</summary>
        private static TMP_Text EventLine(DemoSceneBuildContext ctx, DemoPanel card, string rowName, string initial)
        {
            RectTransform row = card.Row(rowName, 26f);
            TMP_Text text = DemoSceneKit.Text(ctx, row, "Text", initial, 17f, DemoPalette.Cyan,
                TextAlignmentOptions.MidlineLeft);
            DemoSceneKit.Stretch(text.rectTransform);
            text.textWrappingMode = TextWrappingModes.NoWrap;
            text.overflowMode = TextOverflowModes.Ellipsis;
            return text;
        }

        private static void WireSceneEvents(DemoSceneBuildContext ctx, CombatScene scene, Hud hud, DemoLogFeed log)
        {
            RpgCharacter player = scene.Player;
            ctx.OwnListeners(player, player.OnDeathEvent);
            DemoWiring.AddBool(player.OnDeathEvent, hud.Dead.gameObject.SetActive, true);
            DemoWiring.AddBool(player.OnDeathEvent, hud.Alive.gameObject.SetActive, false);
            ctx.OwnListeners(player, player.OnRevivedEvent);
            DemoWiring.AddBool(player.OnRevivedEvent, hud.Dead.gameObject.SetActive, false);
            DemoWiring.AddBool(player.OnRevivedEvent, hud.Alive.gameObject.SetActive, true);
            ctx.MarkModified(player);

            RpgContactDamage contact = scene.Contact;
            if (contact != null)
            {
                ctx.OwnListeners(contact, contact.OnTargetFound);
                DemoWiring.AddString(contact.OnTargetFound, log.Log,
                    contact.name + " found the player · RpgContactDamage.OnTargetFound");
                ctx.OwnListeners(contact, contact.OnTargetLost);
                DemoWiring.AddString(contact.OnTargetLost, log.Log,
                    contact.name + " lost the player · RpgContactDamage.OnTargetLost");
                ctx.MarkModified(contact);
            }
            else
            {
                ctx.Note("No enemy with RpgContactDamage; its log wiring is skipped.");
            }

            NpcRpgCombatBrain brain = scene.Brain;
            if (brain != null && hud.Brain != null)
            {
                ctx.OwnListeners(brain, brain.OnDecisionChanged);
                DemoWiring.AddDynamic(brain.OnDecisionChanged, hud.Brain.SetText);
                ctx.OwnListeners(brain, brain.OnAttackTriggered);
                DemoWiring.AddString(brain.OnAttackTriggered, log.Log,
                    brain.name + " attacks · NpcRpgCombatBrain.OnAttackTriggered");
                ctx.MarkModified(brain);
            }
            else
            {
                ctx.Note("No enemy with NpcRpgCombatBrain; the brain row is skipped.");
            }
        }

        private static void InitialHp(RpgCharacter character, out float current, out float max)
        {
            current = 100f;
            max = 100f;
            RpgCharacterTemplate template = character.Template;
            if (template == null || template.resources == null)
            {
                return;
            }

            foreach (RpgResourceDefinition definition in template.resources)
            {
                if (definition != null && definition.id.Value == new RpgStatId(RpgStatPreset.Hp).Value)
                {
                    max = definition.startMax;
                    current = definition.restoreToFull ? definition.startMax : definition.startCurrent;
                    return;
                }
            }
        }

        private static DemoInfoContent InfoContent(CombatScene scene)
        {
            string contact = scene.Contact != null ? scene.Contact.name : "the melee NPC";
            string brain = scene.Brain != null ? scene.Brain.name : "the ranged NPC";
            return new DemoInfoContent
            {
                Title = "Neo.Rpg · Combat with NPCs",
                Purpose = scene.Player.name + " (RpgCharacter + RpgAttackController + KeyboardMover) against " +
                          scene.Enemies.Count + " NPCs: " + contact + " deals RpgContactDamage, " + brain +
                          " is driven by NpcRpgCombatBrain. HUD, health bars and log are scene objects.",
                RuntimeApi = "target.ApplyDamage(new RpgDamageInfo(16, \"Demo\", player)); player.DamageApplied += " +
                             "(info, applied) => ...; attack.UsePrimaryAttack(); character.Restore().",
                SceneWorkflow = "Select " + scene.Controller.name + " for the loop settings. " + contact +
                                " (RpgContactDamage) and " + brain + " (NpcRpgCombatBrain) events feed the log and the " +
                                "HUD; the player's OnDeath / OnRevived switch the status row.",
                Verification = "Turn Auto loop off and walk into " + contact + " with WASD: every contact hit is " +
                               "logged with its source. Let the player die, then press Restore All.",
                Hints = Array.Empty<string>(),
                Corner = ModuleDemoSceneInfo.Corner.TopRight,
                PanelWidth = InfoWidth,
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

        private sealed class CombatScene
        {
            public CombatScene(RpgCombatDemoController controller, RpgCharacter player, RpgAttackController playerAttack,
                List<RpgCharacter> enemies, RpgWorldHealthBars healthBars, RpgContactDamage contact,
                NpcRpgCombatBrain brain)
            {
                Controller = controller;
                Player = player;
                PlayerAttack = playerAttack;
                Enemies = enemies;
                HealthBars = healthBars;
                Contact = contact;
                Brain = brain;
            }

            public RpgCombatDemoController Controller { get; }
            public RpgCharacter Player { get; }
            public RpgAttackController PlayerAttack { get; }
            public List<RpgCharacter> Enemies { get; }
            public RpgWorldHealthBars HealthBars { get; }
            public RpgContactDamage Contact { get; }
            public NpcRpgCombatBrain Brain { get; }
        }

        private sealed class Hud
        {
            public Hud(TMP_Text enemies, TMP_Text lastEvent, Toggle autoLoop, Toggle pressure, TMP_Text alive,
                TMP_Text dead, DemoValueReadout brain)
            {
                Enemies = enemies;
                LastEvent = lastEvent;
                AutoLoop = autoLoop;
                Pressure = pressure;
                Alive = alive;
                Dead = dead;
                Brain = brain;
            }

            public TMP_Text Enemies { get; }
            public TMP_Text LastEvent { get; }
            public Toggle AutoLoop { get; }
            public Toggle Pressure { get; }
            public TMP_Text Alive { get; }
            public TMP_Text Dead { get; }
            public DemoValueReadout Brain { get; }
        }
    }
}
