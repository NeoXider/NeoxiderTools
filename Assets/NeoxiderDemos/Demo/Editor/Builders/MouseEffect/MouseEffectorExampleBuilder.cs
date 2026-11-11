using System;
using System.Collections.Generic;
using Neo.Samples;
using Neo.Tools;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace Neo.Demo.Editor.Builders
{
    /// <summary>
    ///     Authors the MouseEffect demo (Scenes/Tools/MouseEffectorExample.unity) around its three hand-authored
    ///     MouseEffect objects (spawn, trail, follower), which stay as they were. The builder saves a
    ///     MouseInputManager with Hold events enabled (hold spawning needs them), the controls card, the info card
    ///     and the persistent wiring: the effect toggles call GameObject.SetActive directly, the onSpawn /
    ///     onStartFollow / onStopFollow events feed the log, and a <see cref="DemoPointerGate" /> keeps clicks on
    ///     the UI from spawning effects.
    /// </summary>
    internal static class MouseEffectorExampleBuilder
    {
        internal const string ScenePath = "Scenes/Tools/MouseEffectorExample.unity";
        internal const string ResetLabel = "Reset counter";
        internal const string InputRootKey = "Mouse Input";

        private const string SceneName = "MouseEffectorExample.unity";
        private static readonly Color Accent = DemoPalette.Cyan;

        // WHY: option index = SpawnTrigger value. Click is left out: MouseInputManager raises OnClick only when the
        // press and the release hit the same collider, and this scene has no colliders under the cursor.
        private static readonly string[] TriggerOptions =
        {
            nameof(MouseEffect.SpawnTrigger.Press),
            nameof(MouseEffect.SpawnTrigger.Hold),
            nameof(MouseEffect.SpawnTrigger.Release)
        };

        [DemoSceneBuilder(ScenePath)]
        public static void Build(DemoSceneBuildContext ctx)
        {
            Effects effects = Step("find the hand-authored MouseEffect objects", () => FindEffects(ctx));
            Camera camera = Step("find the scene camera", () => WorldDemoBuild.RequireNamed<Camera>(ctx, "Main Camera"));
            Step("use the kit input module on the hand-authored EventSystem",
                () => DemoSceneKit.UpgradeLegacyInputModule(ctx, ctx.Find<EventSystem>()));
            Step("configure the MouseInputManager", () => ConfigureInput(ctx, camera));
            ModuleDemoSceneInfo info = Step("ensure the info service",
                () => ctx.FindOrCreate<ModuleDemoSceneInfo>("Services", "Demo Info"));
            MouseEffectDemoController controller = Step("ensure the demo controller",
                () => ctx.FindOrCreate<MouseEffectDemoController>("Services", "Demo Controller"));

            DemoFrame frame = Step("author the frame", () => WorldDemoBuild.Frame(ctx, "Neo.Tools · MouseEffect", Accent));
            Step("author the controls card and wiring", () => BuildControls(ctx, frame.Content, effects, controller));
            Step("author the info card", () => frame.InfoCard(info, InfoContent(effects)));
            Step("gate the effects against UI presses",
                () => frame.GateWorldInput(effects.Spawn, effects.Trail, effects.Follow));
        }

        private static Effects FindEffects(DemoSceneBuildContext ctx)
        {
            MouseEffect spawn = null;
            MouseEffect trail = null;
            MouseEffect follow = null;
            List<MouseEffect> all = ctx.FindAll<MouseEffect>();
            foreach (MouseEffect effect in all)
            {
                if (spawn == null && effect.spawnPrefab != null)
                {
                    spawn = effect;
                }
                else if (trail == null && effect.trail != null)
                {
                    trail = effect;
                }
                else if (follow == null && effect.followObject != null)
                {
                    follow = effect;
                }
            }

            if (spawn == null)
            {
                throw new InvalidOperationException("expected a hand-authored MouseEffect with a Spawn Prefab " +
                                                    "(MouseEffectClick); found " + all.Count + " MouseEffect(s). Restore " +
                                                    SceneName + " from version control instead of rebuilding it.");
            }

            if (trail == null)
            {
                ctx.Warn(SceneName + ": no MouseEffect with a TrailRenderer; the trail toggle was skipped.");
            }

            if (follow == null)
            {
                ctx.Warn(SceneName + ": no MouseEffect with a Follow Object; the follower toggle was skipped.");
            }

            return new Effects(spawn, trail, follow);
        }

        private static void ConfigureInput(DemoSceneBuildContext ctx, Camera camera)
        {
            // WHY: MouseInputManager is a DontDestroyOnLoad singleton that destroys its whole GameObject when a
            // duplicate exists, so it gets its own top-level root instead of sharing "[Demo] Services".
            MouseInputManager input = ctx.Find<MouseInputManager>();
            if (input == null)
            {
                input = ctx.Ensure<MouseInputManager>(ctx.Root(InputRootKey));
            }

            // WHY: MouseEffect spawns during hold only from MouseInputManager.OnHold, which is off by default.
            ctx.Set(input, "enableHold", true);
            ctx.Set(input, "enablePress", true);
            ctx.Set(input, "enableRelease", true);
            ctx.Set(input, "enableClick", true);
            ctx.Set(input, "targetCamera", camera);
        }

        private static void BuildControls(DemoSceneBuildContext ctx, DemoPanel card, Effects effects,
            MouseEffectDemoController controller)
        {
            card.Section("Effects · one MouseEffect each");
            WorldDemoBuild.Hint(card, "Press and drag in the scene. Each toggle calls GameObject.SetActive on one " +
                                      "child of Effects.");
            EffectToggle(card, effects.Spawn, "Spawn");
            EffectToggle(card, effects.Trail, "Trail");
            EffectToggle(card, effects.Follow, "Follower");

            MouseEffect spawn = effects.Spawn;
            card.Section("Spawn settings · " + spawn.name);
            TMP_Dropdown trigger = card.Dropdown("Spawn trigger", TriggerOptions,
                Mathf.Clamp((int)spawn.spawnTrigger, 0, TriggerOptions.Length - 1));
            DemoWiring.AddDynamic(trigger.onValueChanged, controller.SetSpawnTrigger);
            Toggle duringHold = WorldDemoBuild.CompactToggle(card, "Spawn during hold · spawnDuringHold",
                spawn.spawnDuringHold);
            DemoWiring.AddDynamic(duringHold.onValueChanged, controller.SetSpawnDuringHold);
            DemoSliderRow interval = WorldDemoBuild.CompactSlider(card, "Hold interval, s · holdInterval", 0.016f, 0.3f,
                Mathf.Clamp(spawn.holdInterval, 0.016f, 0.3f), false, "0.000");
            DemoWiring.AddDynamic(interval.Slider.onValueChanged, controller.SetHoldInterval);
            DemoSliderRow lifetime = WorldDemoBuild.CompactSlider(card, "Lifetime, s · spawnLifetime", 0.2f, 3f,
                Mathf.Clamp(spawn.spawnLifetime, 0.2f, 3f), false, "0.0");
            DemoWiring.AddDynamic(lifetime.Slider.onValueChanged, controller.SetSpawnLifetime);
            TMP_Text spawned = card.CompactValue("Spawned · onSpawn", "0");
            Button reset = card.Button(ResetLabel);
            DemoWiring.AddVoid(reset.onClick, controller.ResetCounter);
            DemoLogFeed log = card.LogRow(5, "press in the scene");

            WireEffectEvents(ctx, effects, controller, log);

            ctx.Set(controller, "_spawnEffect", spawn);
            ctx.Set(controller, "_triggerDropdown", trigger);
            ctx.Set(controller, "_duringHoldToggle", duringHold);
            ctx.Set(controller, "_intervalSlider", interval.Slider);
            ctx.Set(controller, "_lifetimeSlider", lifetime.Slider);
            ctx.Set(controller, "_spawnedValue", spawned);
            ctx.Set(controller, "_log", log);
        }

        private static void EffectToggle(DemoPanel card, MouseEffect effect, string what)
        {
            if (effect == null)
            {
                return;
            }

            GameObject host = effect.gameObject;
            Toggle toggle = WorldDemoBuild.CompactToggle(card, what + " · " + host.name, host.activeSelf);
            // WHY: NoCode path - the toggle switches the hand-authored effect object itself.
            DemoWiring.AddDynamic(toggle.onValueChanged, host.SetActive);
        }

        private static void WireEffectEvents(DemoSceneBuildContext ctx, Effects effects,
            MouseEffectDemoController controller, DemoLogFeed log)
        {
            MouseEffect spawn = effects.Spawn;
            ctx.OwnListeners(spawn, spawn.onSpawn);
            DemoWiring.AddVoid(spawn.onSpawn, controller.CountSpawn);
            DemoWiring.AddString(spawn.onSpawn, log.Log, spawn.name + ".onSpawn");
            ctx.MarkModified(spawn);

            foreach (MouseEffect effect in new[] { effects.Trail, effects.Follow })
            {
                if (effect == null)
                {
                    continue;
                }

                ctx.OwnListeners(effect, effect.onStartFollow);
                ctx.OwnListeners(effect, effect.onStopFollow);
                DemoWiring.AddString(effect.onStartFollow, log.Log, effect.name + ".onStartFollow");
                DemoWiring.AddString(effect.onStopFollow, log.Log, effect.name + ".onStopFollow");
                ctx.MarkModified(effect);
            }
        }

        private static DemoInfoContent InfoContent(Effects effects)
        {
            string spawn = effects.Spawn.name;
            string trail = effects.Trail != null ? effects.Trail.name : "a trail effect";
            string follow = effects.Follow != null ? effects.Follow.name : "a follower effect";
            return WorldDemoBuild.Info(
                "Neo.Tools · MouseEffect",
                "Three MouseEffect components listen to MouseInputManager: " + spawn + " spawns circles, " + trail +
                " drags a TrailRenderer and " + follow + " shows a follower while the button is held.",
                "MouseEffect fields spawnTrigger, spawnDuringHold, holdInterval, spawnLifetime; SetTargetCamera(cam); " +
                "events onSpawn, onStartFollow, onStopFollow. MouseInputManager.I.OnPress / OnHold / OnRelease.",
                "[Demo] " + InputRootKey + " holds the MouseInputManager with Enable Hold on (hold spawning needs it). " +
                "The effect toggles " +
                "call GameObject.SetActive on the Effects children directly.",
                "Press and drag in the scene: circles, trail and follower appear; the log counts onSpawn.");
        }

        private static T Step<T>(string step, Func<T> action)
        {
            return DemoSceneKit.Step(SceneName, step, action);
        }

        private static void Step(string step, Action action)
        {
            DemoSceneKit.Step(SceneName, step, action);
        }

        private sealed class Effects
        {
            public Effects(MouseEffect spawn, MouseEffect trail, MouseEffect follow)
            {
                Spawn = spawn;
                Trail = trail;
                Follow = follow;
            }

            public MouseEffect Spawn { get; }
            public MouseEffect Trail { get; }
            public MouseEffect Follow { get; }
        }
    }
}
