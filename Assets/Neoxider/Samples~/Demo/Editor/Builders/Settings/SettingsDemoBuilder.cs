using System;
using Neo.Samples;
using Neo.Settings;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Neo.Demo.Editor.Builders
{
    /// <summary>
    ///     Exemplar builder: authors the complete Neo.Settings demo into SettingsDemo.unity. Every control is a
    ///     saved scene object wired with persistent listeners, so the wiring is visible in the Inspector without
    ///     entering Play Mode. The service saves under a demo-only key prefix (the demo never overwrites a game's
    ///     settings), and the Input section shows the Deferred debounce collapsing a slider drag into one write.
    ///     Copy this file for a new demo (see dev-docs/DemoSceneBuilders.md).
    /// </summary>
    internal static class SettingsDemoBuilder
    {
        /// <summary>SaveProvider key prefix of the demo service (default game prefix is "Neo.Settings.").</summary>
        public const string SaveKeyPrefix = "Neo.Demo.Settings.";

        private const string Scene = "SettingsDemo.unity";

        private static readonly Color Accent = new(0.98f, 0.72f, 0.30f);

        private static readonly string[] PresetNames =
        {
            nameof(GraphicsPreset.Minimal),
            nameof(GraphicsPreset.Low),
            nameof(GraphicsPreset.Medium),
            nameof(GraphicsPreset.High),
            nameof(GraphicsPreset.Maximum)
        };

        [DemoSceneBuilder("Scenes/Settings/SettingsDemo.unity")]
        public static void Build(DemoSceneBuildContext ctx)
        {
            string step = "services";
            try
            {
                // WHY: hand-authored services win; FindOrCreate adds a generated one only when the scene has none.
                GameSettingsComponent service = ctx.FindOrCreate<GameSettingsComponent>("Services", "Game Settings Service");
                ModuleDemoSceneInfo info = ctx.FindOrCreate<ModuleDemoSceneInfo>("Services", "Demo Info");
                SettingsDemoController controller = ctx.FindOrCreate<SettingsDemoController>("Services", "Demo Controller");
                ctx.Set(service, "_saveKeyPrefix", SaveKeyPrefix);

                step = "frame";
                // WHY: 920 px keeps the centered card clear of the 460 px info card in the top-left corner.
                DemoFrame frame = DemoSceneKit.Frame(ctx, new DemoFrameOptions("Neo.Settings", Accent) { ContentWidth = 920f });
                DemoPanel card = frame.Content;

                step = "live values";
                card.Section("Live values · GameSettings");
                TMP_Text preset = card.ValueLabel("Graphics preset");
                TMP_Text framerate = card.ValueLabel("Framerate cap");
                TMP_Text vSync = card.ValueLabel("VSync");

                step = "input";
                card.Section("Input · Deferred save");
                DemoSliderRow sensitivity = card.SliderRow("Mouse sensitivity", 0.1f, 10f, 2f, false, "0.0");
                // WHY: NoCode path - the slider calls the service directly; the controller only watches the debounce.
                DemoWiring.AddDynamic(sensitivity.Slider.onValueChanged, service.SetMouseSensitivityForMenu);
                DemoWiring.AddDynamic(sensitivity.Slider.onValueChanged, controller.LogSensitivity);
                TMP_Text deferred = card.ValueLabel("Deferred save", "idle");
                TMP_Text stored = card.ValueLabel("Stored in SaveProvider", "-");
                deferred.textWrappingMode = TextWrappingModes.NoWrap;
                stored.textWrappingMode = TextWrappingModes.NoWrap;

                step = "graphics and performance";
                card.Section("Graphics and performance");
                TMP_Dropdown presetDropdown = card.Dropdown("Graphics preset", PresetNames, (int)GraphicsPreset.High);
                DemoWiring.AddDynamic(presetDropdown.onValueChanged, controller.SetGraphicsPreset);

                Button[] fps = card.ButtonRow("30 fps", "60 fps", "120 fps", "Unlimited");
                DemoWiring.AddInt(fps[0].onClick, controller.SetFramerateCap, 30);
                DemoWiring.AddInt(fps[1].onClick, controller.SetFramerateCap, 60);
                DemoWiring.AddInt(fps[2].onClick, controller.SetFramerateCap, 120);
                DemoWiring.AddInt(fps[3].onClick, controller.SetFramerateCap, -1);

                Toggle vSyncToggle = card.Toggle("VSync", false);
                DemoWiring.AddDynamic(vSyncToggle.onValueChanged, controller.SetVSync);

                step = "persistence";
                card.Section("Persistence");
                Button[] persistence = card.ButtonRow("Save settings", "Reset defaults");
                DemoWiring.AddVoid(persistence[0].onClick, controller.SaveSettings);
                DemoWiring.AddVoid(persistence[1].onClick, controller.ResetDefaults);

                step = "info card";
                frame.InfoCard(info, InfoContent());

                step = "controller references";
                ctx.Set(controller, "_settings", service);
                ctx.Set(controller, "_saveKeyPrefix", SaveKeyPrefix);
                ctx.Set(controller, "_sensitivityValue", sensitivity.Value);
                ctx.Set(controller, "_presetValue", preset);
                ctx.Set(controller, "_framerateValue", framerate);
                ctx.Set(controller, "_vSyncValue", vSync);
                ctx.Set(controller, "_deferredValue", deferred);
                ctx.Set(controller, "_storedValue", stored);
                ctx.Set(controller, "_sensitivitySlider", sensitivity.Slider);
                ctx.Set(controller, "_presetDropdown", presetDropdown);
                ctx.Set(controller, "_vSyncToggle", vSyncToggle);
                ctx.Set(controller, "_log", frame.Log);
            }
            catch (Exception exception)
            {
                throw new InvalidOperationException(
                    Scene + " build failed at step '" + step + "': " + exception.Message, exception);
            }
        }

        private static DemoInfoContent InfoContent()
        {
            return new DemoInfoContent
            {
                Title = "Neo.Settings · GameSettings",
                Purpose = "Static GameSettings facade backed by a GameSettingsComponent service. Every control " +
                          "calls the real Set* API, applies to the engine and persists through SaveProvider under the " +
                          "demo prefix " + SaveKeyPrefix + ".",
                RuntimeApi = "GameSettings.SetMouseSensitivity / SetGraphicsPreset / SetFramerateCap / SetVSync / " +
                             "ResetGroup / FlushPendingSettingsSave; GameSettingsComponent.HasPendingDeferredSave.",
                SceneWorkflow = "One GameSettingsComponent per scene; UI controls call it through persistent " +
                                "UnityEvents (select a control to see its wiring in the Inspector).",
                Verification = "Drag the slider: 'Deferred save' counts the changes and the stored value updates " +
                               "once after the drag. Framerate cap and VSync change the engine immediately.",
                Hints = new[]
                {
                    "Slider -> GameSettingsComponent.SetMouseSensitivityForMenu (NoCode, Deferred save)",
                    "Drag fast: many changes, one SaveProvider write after 0.35 s idle",
                    "Save -> FlushPendingSettingsSave; Reset -> ResetGroup per group"
                }
            };
        }
    }
}
