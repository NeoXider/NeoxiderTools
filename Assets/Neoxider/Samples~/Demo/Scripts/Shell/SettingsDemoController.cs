using Neo.Settings;
using TMPro;
using UnityEngine;

namespace Neo.Samples
{
    /// <summary>
    ///     Bright, self-contained demo for the <b>Neo.Settings</b> module. Bootstraps the real
    ///     <see cref="GameSettingsComponent" /> singleton (creating one if the scene has none) so the static
    ///     <see cref="GameSettings" /> facade is attached, then exposes the most demonstrable settings —
    ///     mouse sensitivity, graphics preset, framerate cap and VSync — through their real Set* calls.
    ///     Framerate cap and VSync have an immediate engine effect. Save / Reset use the real persistence
    ///     path and every change is logged. Robust in an empty scene.
    /// </summary>
    [AddComponentMenu("Neoxider/Demos/Settings Demo")]
    public sealed class SettingsDemoController : MonoBehaviour
    {
        private static readonly int[] FpsPresets = { 30, 60, 120, -1 };

        private NeoDemoShell.Context _shell;
        private TMP_Text _sensValue;
        private TMP_Text _presetValue;
        private TMP_Text _fpsValue;
        private TMP_Text _vsyncValue;
        private bool _subscribed;

        private void Start()
        {
            _shell = NeoDemoShell.Build("Neo.Settings", new Color(0.98f, 0.72f, 0.30f));

            NeoDemoShell.ShowInfoCardOnce(
                "Neo.Settings · GameSettings",
                "Static GameSettings facade backed by a GameSettingsComponent service. Set* calls apply and persist.",
                "Framerate cap + VSync change the engine immediately",
                "Preset buttons call GameSettings.SetGraphicsPreset(...)",
                "Save settings → FlushPendingSettingsSave; Reset → ResetGroup per group");

            EnsureService();

            _sensValue = _shell.AddValueLabel("Mouse sensitivity");
            _presetValue = _shell.AddValueLabel("Graphics preset");
            _fpsValue = _shell.AddValueLabel("Framerate cap");
            _vsyncValue = _shell.AddValueLabel("VSync");

            _shell.AddSlider("Mouse sensitivity", 0.1f, 10f, GameSettings.MouseSensitivity, v =>
            {
                GameSettings.SetMouseSensitivity(v);
                _shell.Log($"GameSettings.SetMouseSensitivity({v:0.0})");
            });

            _shell.AddButtonRow(
                ("Low", () => SetPreset(GraphicsPreset.Low)),
                ("Medium", () => SetPreset(GraphicsPreset.Medium)),
                ("High", () => SetPreset(GraphicsPreset.High)),
                ("Max", () => SetPreset(GraphicsPreset.Maximum)));

            _shell.AddButtonRow(
                ("30 fps", () => SetFps(0)),
                ("60 fps", () => SetFps(1)),
                ("120 fps", () => SetFps(2)),
                ("Unlimited", () => SetFps(3)));

            _shell.AddToggle("VSync", GameSettings.VSync, on =>
            {
                GameSettings.SetVSync(on);
                _shell.Log($"GameSettings.SetVSync({on}) → QualitySettings.vSyncCount");
            });

            _shell.AddButtonRow(("Save settings", SaveSettings), ("Reset defaults", ResetDefaults));

            GameSettings.OnSettingsChanged += RefreshLabels;
            _subscribed = true;
            RefreshLabels();
            _shell.Log("GameSettingsComponent attached — GameSettings live");
        }

        private void OnDestroy()
        {
            if (_subscribed)
            {
                GameSettings.OnSettingsChanged -= RefreshLabels;
            }
        }

        private void EnsureService()
        {
            if (GameSettingsComponent.HasInstance)
            {
                _shell.Log("Reused existing GameSettingsComponent.I");
                return;
            }

            // Lazily create the settings service; its Init() calls GameSettings.Attach + LoadState.
            GameSettingsComponent.CreateInstance = true;
            _ = GameSettingsComponent.I;
            _shell.Log("Created GameSettingsComponent singleton");
        }

        private void SetPreset(GraphicsPreset preset)
        {
            GameSettings.SetGraphicsPreset(preset);
            _shell.Log($"GameSettings.SetGraphicsPreset({preset})");
        }

        private void SetFps(int index)
        {
            int fps = FpsPresets[index];
            GameSettings.SetFramerateCap(fps);
            _shell.Log($"GameSettings.SetFramerateCap({fps}) → Application.targetFrameRate");
        }

        private void SaveSettings()
        {
            GameSettings.FlushPendingSettingsSave(); // writes current values to SaveProvider
            Neo.Save.SaveProvider.Save(); // flush provider to disk
            _shell.Log("GameSettings.FlushPendingSettingsSave() → persisted");
        }

        private void ResetDefaults()
        {
            GameSettings.ResetGroup(SettingsGroup.Input);
            GameSettings.ResetGroup(SettingsGroup.Graphics);
            GameSettings.ResetGroup(SettingsGroup.Performance);
            _shell.Log("GameSettings.ResetGroup(Input/Graphics/Performance)");
        }

        private void RefreshLabels()
        {
            if (_sensValue == null)
            {
                return;
            }

            _sensValue.text = GameSettings.MouseSensitivity.ToString("0.0");
            _presetValue.text = GameSettings.GraphicsPreset.ToString();
            _fpsValue.text = GameSettings.FramerateCap < 0 ? "Unlimited" : GameSettings.FramerateCap + " fps";
            _vsyncValue.text = GameSettings.VSync ? "ON" : "OFF";
        }
    }
}
