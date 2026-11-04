using System;
using System.Collections.Generic;
using Neo.Animations;
using Neo.Samples;
using Neo.Tools.View;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Neo.Demo.Editor.Builders
{
    /// <summary>
    ///     Authors the LightAnimator / MeshEmission demo into Light.unity. The scene's hand-authored rig - five point
    ///     lights with a <see cref="LightAnimator" /> each (RandomFlicker, Pulsing, SmoothTransition, PerlinNoise,
    ///     SinWave), emissive spheres with <see cref="MeshEmission" /> and world labels - stays untouched; the builder
    ///     adds the control card, a NoCode intensity readout (LightAnimator.Intensity.OnChanged → label) and the info
    ///     card. <see cref="LightDemoController" /> forwards the controls to the animators.
    /// </summary>
    internal static class LightDemoBuilder
    {
        private const string Scene = "Light.unity";

        private static readonly Color Accent = new(1f, 0.82f, 0.36f);

        [DemoSceneBuilder("Scenes/Tools/View/Light.unity")]
        public static void Build(DemoSceneBuildContext ctx)
        {
            string step = "hand-authored lights";
            try
            {
                List<LightAnimator> lights = ctx.FindAll<LightAnimator>();
                if (lights.Count == 0)
                {
                    throw new InvalidOperationException("no hand-authored LightAnimator found; the light rig of the " +
                                                        "scene is missing.");
                }

                LightAnimator featured = Featured(lights);

                step = "services";
                ModuleDemoSceneInfo info = ctx.FindOrCreate<ModuleDemoSceneInfo>("Services", "Demo Info");
                LightDemoController controller = ctx.FindOrCreate<LightDemoController>("Services", "Demo Controller");

                step = "frame";
                // WHY: no backdrop - the lit 3D scene is the demo; the card sits on the right, the info card left.
                DemoFrame frame = DemoSceneKit.Frame(ctx, new DemoFrameOptions("Neo.Tools.View · LightAnimator", Accent)
                {
                    Backdrop = DemoBackdrop.None,
                    Placement = DemoContentPlacement.Right,
                    ContentWidth = 460f,
                    LogLines = 4
                });
                DemoPanel card = frame.Content;

                step = "playback rows";
                card.Section("All lights · LightAnimator");
                Button[] playback = card.ButtonRow("Play", "Pause", "Resume", "Stop");
                DemoWiring.AddVoid(playback[0].onClick, controller.PlayAll);
                DemoWiring.AddVoid(playback[1].onClick, controller.PauseAll);
                DemoWiring.AddVoid(playback[2].onClick, controller.ResumeAll);
                DemoWiring.AddVoid(playback[3].onClick, controller.StopAll);
                DemoSliderRow speed = card.SliderRow("Animation speed", 0f, 8f, featured.AnimationSpeed, false, "0.0");
                DemoWiring.AddDynamic(speed.Slider.onValueChanged, controller.SetSpeed);
                DemoSliderRow maxIntensity = card.SliderRow("Max intensity", 1f, 8f,
                    Mathf.Clamp(featured.MaxIntensity, 1f, 8f), false, "0.0");
                DemoWiring.AddDynamic(maxIntensity.Slider.onValueChanged, controller.SetMaxIntensity);

                step = "clock rows";
                card.Section("Clock · ManualUpdate + Tick");
                Toggle manual = card.Toggle("Manual clock (ManualUpdate)", false);
                DemoWiring.AddDynamic(manual.onValueChanged, controller.SetManualClock);
                Button[] steps = card.ButtonRow("Step 0.1 s", "Step 1 s");
                DemoWiring.AddFloat(steps[0].onClick, controller.Step, 0.1f);
                DemoWiring.AddFloat(steps[1].onClick, controller.Step, 1f);
                DemoSliderRow timeScale = card.SliderRow("Time.timeScale", 0f, 1f, 1f, false, "0.00");
                DemoWiring.AddDynamic(timeScale.Slider.onValueChanged, controller.SetTimeScale);
                Toggle unscaled = card.Toggle("Unscaled time", featured.UseUnscaledTime);
                DemoWiring.AddDynamic(unscaled.onValueChanged, controller.SetUnscaledTime);

                step = "reactive readout";
                card.Section("Reactive intensity · NoCode");
                TMP_Text intensity = card.ValueLabel("Intensity of '" + featured.name + "'", "-");
                DemoValueReadout readout = DemoSceneKit.Readout(ctx, intensity, "0.00");
                // WHY: the rig is hand-authored, so the builder owns only its own listener on it: stale or generated
                // targets are pruned first and the readout call is added once.
                ctx.OwnListeners(featured, featured.Intensity.OnChanged);
                DemoWiring.AddDynamic(featured.Intensity.OnChanged, readout.SetFloat);
                ctx.MarkModified(featured);

                step = "info card";
                frame.InfoCard(info, new DemoInfoContent
                {
                    Title = "Neo.Tools.View · LightAnimator + MeshEmission",
                    Purpose = "Five point lights, one AnimationType each (labels in the scene). The spheres' " +
                              "MeshEmission follows its light, so the glow pulses with it.",
                    RuntimeApi = "lightAnimator.Play / Pause / Resume / Stop / ResetToOriginal; AnimationSpeed, " +
                                 "Min/MaxIntensity, AnimationType; ManualUpdate + Tick(dt); UseUnscaledTime; " +
                                 "Intensity (reactive).",
                    SceneWorkflow = "Select a 'Light (n)' object to see its LightAnimator; its Sphere child carries " +
                                    "MeshEmission. '" + featured.name + "' wires Intensity.OnChanged to the card label " +
                                    "in the Inspector.",
                    Verification = "Enter Play Mode: lights flicker/pulse, Manual clock freezes them until Step, and " +
                                   "with Unscaled time on they ignore Time.timeScale = 0.",
                    Hints = new[]
                    {
                        "Card -> LightDemoController -> the real LightAnimator API",
                        "Manual clock: ManualUpdate = true, then Tick(0.1) per Step",
                        "Intensity.OnChanged -> label: a NoCode reactive binding"
                    }
                });

                step = "controller references";
                ctx.Set(controller, "_lights", lights);
                ctx.Set(controller, "_speedSlider", speed.Slider);
                ctx.Set(controller, "_maxIntensitySlider", maxIntensity.Slider);
                ctx.Set(controller, "_timeScaleSlider", timeScale.Slider);
                ctx.Set(controller, "_manualClockToggle", manual);
                ctx.Set(controller, "_unscaledTimeToggle", unscaled);
                ctx.Set(controller, "_log", frame.Log);
            }
            catch (Exception exception)
            {
                throw DemoSceneKit.StepFailed(Scene, step, exception);
            }
        }

        private static LightAnimator Featured(List<LightAnimator> lights)
        {
            foreach (LightAnimator light in lights)
            {
                if (light.AnimationType == AnimationType.PerlinNoise)
                {
                    return light;
                }
            }

            return lights[0];
        }
    }
}
