using System;
using System.Collections.Generic;
using Neo.Rpg.Components;
using Neo.Rpg.Demo;
using Neo.Samples;
using Neo.Tools;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Object = UnityEngine.Object;

namespace Neo.Demo.Editor.Builders
{
    /// <summary>
    ///     Authors the HUD of the first-person Vampire Survivor demo (Scenes/VampireSurvivorMCP.unity). The game -
    ///     the "first Person Controller" player (RpgCharacter, AuraWeapon), the WaveSpawner_1..3 Spawners, the ground
    ///     NavMesh and the enemy prefabs - stays hand-authored; the builder assigns the controller's explicit
    ///     references and authors the kit frame (status card, persistent buttons, log feed, death overlay), the
    ///     canvas used by <see cref="RpgWorldHealthBars" /> and the info card.
    /// </summary>
    internal static class VampireSurvivorDemoBuilder
    {
        internal const string ScenePath = "Scenes/VampireSurvivorMCP.unity";
        internal const string DeathOverlayName = "DeathOverlay";

        private const string Scene = "VampireSurvivorMCP.unity";
        private const string PlayerTag = "Player";
        private const float DamageAmount = 25f;
        private const int HealthBarSortingOrder = 40;

        private static readonly Color Accent = DemoPalette.Danger;

        [DemoSceneBuilder(ScenePath)]
        public static void Build(DemoSceneBuildContext ctx)
        {
            RpgCharacter player = DemoSceneKit.Step(Scene, "find the hand-authored player", () => FindPlayer(ctx));
            Spawner[] spawners = DemoSceneKit.Step(Scene, "find the hand-authored spawners", () => FindSpawners(ctx));
            VampireSurvivorDemoController controller = DemoSceneKit.Step(Scene, "find or author the controller",
                () => ctx.FindOrCreate<VampireSurvivorDemoController>("Services", "Vampire Survivor Controller"));
            RpgWorldHealthBars bars = DemoSceneKit.Step(Scene, "find or author the enemy health bars",
                () => HealthBars(ctx, controller));
            ModuleDemoSceneInfo info = DemoSceneKit.Step(Scene, "find or author the ModuleDemoSceneInfo",
                () => ctx.FindOrCreate<ModuleDemoSceneInfo>("Services", "Demo Info"));

            DemoFrame frame = DemoSceneKit.Step(Scene, "author the frame", () => DemoSceneKit.Frame(ctx,
                new DemoFrameOptions("Neo.Rpg · Vampire Survivor", Accent)
                {
                    Backdrop = DemoBackdrop.None,
                    Placement = DemoContentPlacement.Left,
                    ContentWidth = 440f,
                    Overline = "NEOXIDER · FIRST-PERSON DEMO"
                }));
            DemoSceneKit.Step(Scene, "use the kit input module", () => DemoSceneKit.EnsureEventSystem(ctx, DemoInputModule.Auto, true));
            Status status = DemoSceneKit.Step(Scene, "author the status card", () => StatusCard(frame.Content, spawners));
            DemoSceneKit.Step(Scene, "author the control buttons", () => Controls(frame.Content, controller));
            GameObject death = DemoSceneKit.Step(Scene, "author the death overlay", () => DeathOverlay(frame));
            DemoSceneKit.Step(Scene, "author the health-bar canvas", () => HealthBarCanvas(ctx, bars));
            DemoSceneKit.Step(Scene, "author the info card", () =>
            {
                ctx.Set(info, "_showRuntimeOverlay", true);
                frame.InfoCard(info, InfoContent());
            });

            DemoSceneKit.Step(Scene, "assign the controller references", () =>
            {
                ctx.Set(controller, "_player", player);
                ctx.Set(controller, "_spawners", spawners);
                ctx.Set(controller, "_hpFill", status.HealthFill);
                ctx.Set(controller, "_playerValue", status.Player);
                ctx.Set(controller, "_enemiesValue", status.Enemies);
                ctx.Set(controller, "_spawnersValue", status.Spawners);
                ctx.Set(controller, "_deathOverlay", death);
                ctx.Set(controller, "_log", frame.Log);
                ctx.Set(controller, "_healthFull", DemoPalette.Good);
                ctx.Set(controller, "_healthEmpty", DemoPalette.Danger);
                ctx.Set(controller, "_healthBars", bars);
            });
        }

        private static RpgCharacter FindPlayer(DemoSceneBuildContext ctx)
        {
            foreach (RpgCharacter character in ctx.FindAll<RpgCharacter>())
            {
                if (character.CompareTag(PlayerTag))
                {
                    return character;
                }
            }

            throw new InvalidOperationException("no hand-authored RpgCharacter tagged '" + PlayerTag + "'. The game is " +
                                                "hand-authored; restore the scene from version control instead of rebuilding it.");
        }

        private static Spawner[] FindSpawners(DemoSceneBuildContext ctx)
        {
            List<Spawner> spawners = ctx.FindAll<Spawner>();
            if (spawners.Count == 0)
            {
                throw new InvalidOperationException("no hand-authored Spawner (WaveSpawner_1..3). Restore the scene from " +
                                                    "version control instead of rebuilding it.");
            }

            return spawners.ToArray();
        }

        private static RpgWorldHealthBars HealthBars(DemoSceneBuildContext ctx, VampireSurvivorDemoController controller)
        {
            RpgWorldHealthBars bars = ctx.Find<RpgWorldHealthBars>();
            if (bars != null)
            {
                return bars;
            }

            bars = ctx.Ensure<RpgWorldHealthBars>(controller.gameObject);
            ctx.Set(bars, "_targetTag", "Enemy");
            return bars;
        }

        private static Status StatusCard(DemoPanel card, Spawner[] spawners)
        {
            card.Section("Player · RpgCharacter");
            Image health = card.Bar("Health", DemoPalette.Good, 1f);
            TMP_Text player = card.Label("100 / 100 HP · Lv 1", 20f);
            card.Section("Waves · Spawner + NpcNavigation");
            TMP_Text enemies = card.Label("0 active · 0 spawned · 0 killed", 18f);
            TMP_Text running = card.Label("0 / " + spawners.Length + " running", 18f);
            return new Status(health, player, enemies, running);
        }

        private static void Controls(DemoPanel card, VampireSurvivorDemoController controller)
        {
            card.Section("Controls -> VampireSurvivorDemoController");
            Button[] waves = card.ButtonRow("Start", "Stop");
            Button[] combat = card.ButtonRow("Damage Player", "Kill Nearest");
            Button reset = card.Button("Reset Run");
            AbilitiesDemoBuild.LightLabels(waves);
            AbilitiesDemoBuild.LightLabels(combat);
            AbilitiesDemoBuild.LightLabels(reset);
            DemoWiring.AddVoid(waves[0].onClick, controller.StartSpawners);
            DemoWiring.AddVoid(waves[1].onClick, controller.StopSpawners);
            DemoWiring.AddFloat(combat[0].onClick, controller.DamagePlayer, DamageAmount);
            DemoWiring.AddVoid(combat[1].onClick, controller.KillNearestEnemy);
            DemoWiring.AddVoid(reset.onClick, controller.ResetRun);
            card.Note("Esc frees the cursor for the buttons; WASD + mouse to move and look.");
        }

        private static GameObject DeathOverlay(DemoFrame frame)
        {
            DemoPanel panel = frame.Panel(DeathOverlayName, DemoPanelPlacement.Center, 380f);
            TMP_Text title = panel.BigLabel("YOU DIED");
            title.color = DemoPalette.Danger;
            panel.Note("Use Reset Run to restart.");
            GameObject overlay = panel.Rect.gameObject;
            // WHY: saved hidden; VampireSurvivorDemoController shows it while the player is dead.
            overlay.SetActive(false);
            return overlay;
        }

        private static void HealthBarCanvas(DemoSceneBuildContext ctx, RpgWorldHealthBars bars)
        {
            GameObject root = ctx.Root("Health Bars", true);
            Canvas canvas = DemoSceneKit.OverlayCanvas(ctx, root, HealthBarSortingOrder, new Vector2(1920f, 1080f));
            RectTransform barRoot = ctx.UiChild(root.transform, "Bars");
            DemoSceneKit.Stretch(barRoot);
            ctx.Set(bars, "_canvas", canvas);
            ctx.Set(bars, "_barRoot", barRoot);
            ctx.Set(bars, "_createCanvasIfMissing", false);
            ctx.Set(bars, "_sortingOrder", HealthBarSortingOrder);
            // WHY: the controller tracks every spawned enemy and feeds the bars, so the scene search stays off.
            ctx.Set(bars, "_scanSceneForTargets", false);
        }

        private static DemoInfoContent InfoContent()
        {
            return new DemoInfoContent
            {
                Title = "Neo.Rpg · Vampire Survivor (3D)",
                Purpose = "First-person survival arena: three Spawner waves of RpgCharacter enemies chase the player " +
                          "through NpcNavigation while the AuraWeapon damages everything close.",
                RuntimeApi = "Spawner.StartSpawn / StopSpawn / OnObjectSpawned, RpgCharacter.DamageType / Restore / " +
                             "OnDeathEvent, NpcNavigation.SetFollowTarget.",
                SceneWorkflow = "first Person Controller: PlayerController3DPhysics + RpgCharacter + AuraWeapon; " +
                                "WaveSpawner_1..3: Spawner; controller: VampireSurvivorDemoController + RpgWorldHealthBars.",
                Verification = "Enemies spawn and chase; Damage Player until YOU DIED appears; Reset Run restores.",
                Hints = new[]
                {
                    "Esc frees the cursor to click the buttons",
                    "Buttons -> VampireSurvivorDemoController (persistent OnClick)",
                    "[Demo] Health Bars: canvas of RpgWorldHealthBars"
                },
                Corner = ModuleDemoSceneInfo.Corner.TopRight,
                PanelWidth = 420f,
                StartCollapsed = false
            };
        }

        private readonly struct Status
        {
            public Status(Image healthFill, TMP_Text player, TMP_Text enemies, TMP_Text spawners)
            {
                HealthFill = healthFill;
                Player = player;
                Enemies = enemies;
                Spawners = spawners;
            }

            public Image HealthFill { get; }
            public TMP_Text Player { get; }
            public TMP_Text Enemies { get; }
            public TMP_Text Spawners { get; }
        }
    }
}
