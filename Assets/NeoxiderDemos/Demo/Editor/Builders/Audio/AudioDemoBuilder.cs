using System;
using Neo.Audio;
using Neo.Samples;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Neo.Demo.Editor.Builders
{
    /// <summary>
    ///     Authors the Neo.Audio demo into AudioDemo.unity: the hand-authored <see cref="AM" /> gets sound entries
    ///     (ids, multi-clip sets, random pitch, the UI channel) and two music pools (Loop and Shuffle) from demo
    ///     clip assets; an <see cref="AMSettings" /> service with demo-only save keys and a <see cref="MusicControl" />
    ///     component sit under [Demo] Services; the card's buttons, sliders and toggles call those components
    ///     directly through persistent UnityEvents. <see cref="AudioDemoController" /> only logs and syncs.
    /// </summary>
    internal static class AudioDemoBuilder
    {
        private const string Scene = "AudioDemo.unity";
        private const string ClipFolder = "Effects/Audio/";
        private const string SaveKeyPrefix = "Neo.Demo.Audio.";

        private static readonly Color Accent = new(0.25f, 0.79f, 0.94f);

        [DemoSceneBuilder("Scenes/Audio/AudioDemo.unity")]
        public static void Build(DemoSceneBuildContext ctx)
        {
            string step = "services";
            try
            {
                // WHY: the scene's hand-authored "Audio Demo" object (AM + ModuleDemoSceneInfo) and "Demo Controller"
                // win; FindOrCreate adds generated services only for what the scene does not have.
                AM audio = ctx.FindOrCreate<AM>("Services", "Audio Manager");
                AMSettings settings = ctx.FindOrCreate<AMSettings>("Services", "Audio Settings");
                MusicControl music = ctx.FindOrCreate<MusicControl>("Services", "Music Control");
                ModuleDemoSceneInfo info = ctx.FindOrCreate<ModuleDemoSceneInfo>("Services", "Demo Info");
                AudioDemoController controller = ctx.FindOrCreate<AudioDemoController>("Services", "Demo Controller");

                step = "audio sources";
                EnsureSources(ctx, audio);

                step = "sound entries and music pools";
                ConfigureAudio(ctx, audio);

                step = "settings service";
                ConfigureSettings(ctx, settings, music);

                step = "frame";
                DemoFrame frame = DemoSceneKit.Frame(ctx, new DemoFrameOptions("Neo.Audio", Accent) { ContentWidth = 920f });
                DemoPanel card = frame.Content;

                step = "sound buttons";
                card.Section("Sound entries · AM.Play(id)");
                Button[] sounds = card.ButtonRow("Shot", "Pickup", "Explosion", "UI click");
                WireSound(sounds[0], audio, controller, "shot");
                WireSound(sounds[1], audio, controller, "pickup");
                WireSound(sounds[2], audio, controller, "explosion");
                PlayAudioBtn uiClick = ctx.Ensure<PlayAudioBtn>(sounds[3].gameObject);
                ctx.Set(uiClick, "_soundId", "ui_click");
                ctx.Set(uiClick, "_triggerMode", (int)PlayAudioBtn.TriggerMode.PointerClick);
                ctx.Set(uiClick, "_volume", -1f);
                DemoWiring.AddString(sounds[3].onClick, frame.Log.Log,
                    "PlayAudioBtn (pointer click) -> AM.I.Play(\"ui_click\") on the UI channel");
                card.Note("Explosion holds 2 clips + random pitch · UI click is a PlayAudioBtn on the UI channel");

                step = "music pools";
                card.Section("Music pools · MusicControl");
                TMP_Text nowPlaying = card.ValueLabel("Now playing", "silence");
                Button[] pools = card.ButtonRow("Theme (loop)", "Pads (shuffle)", "Next track", "Stop music");
                DemoWiring.AddString(pools[0].onClick, music.PlayPool, "theme");
                DemoWiring.AddString(pools[0].onClick, frame.Log.Log, "MusicControl.PlayPool(\"theme\")");
                DemoWiring.AddString(pools[1].onClick, music.PlayPool, "pads");
                DemoWiring.AddString(pools[1].onClick, frame.Log.Log, "MusicControl.PlayPool(\"pads\")");
                DemoWiring.AddVoid(pools[2].onClick, music.NextTrack);
                DemoWiring.AddString(pools[2].onClick, frame.Log.Log, "MusicControl.NextTrack() -> AM.TryNextMusicTrack");
                DemoWiring.AddVoid(pools[3].onClick, music.StopMusic);
                DemoWiring.AddString(pools[3].onClick, frame.Log.Log, "MusicControl.StopMusic()");

                step = "player settings";
                card.Section("Player settings · AMSettings (saved)");
                DemoPanel[] columns = card.Columns("Player settings", 2, 24f);
                DemoSliderRow musicVolume = columns[0].SliderRow("Music volume", 0f, 1f, 0.5f, false, "0.00");
                DemoWiring.AddDynamic(musicVolume.Slider.onValueChanged, settings.SetMusicVolume);
                DemoWiring.AddDynamic(musicVolume.Slider.onValueChanged, controller.LogMusicVolume);
                DemoSliderRow efxVolume = columns[0].SliderRow("Effects volume", 0f, 1f, 1f, false, "0.00");
                DemoWiring.AddDynamic(efxVolume.Slider.onValueChanged, settings.SetEfxVolume);
                DemoWiring.AddDynamic(efxVolume.Slider.onValueChanged, controller.LogEfxVolume);
                DemoSliderRow uiVolume = columns[0].SliderRow("UI volume", 0f, 1f, 1f, false, "0.00");
                DemoWiring.AddDynamic(uiVolume.Slider.onValueChanged, audio.SetUiVolume);
                DemoWiring.AddDynamic(uiVolume.Slider.onValueChanged, controller.LogUiVolume);

                Toggle musicOn = columns[1].Toggle("Music on", true);
                DemoWiring.AddDynamic(musicOn.onValueChanged, settings.SetMusic);
                DemoWiring.AddDynamic(musicOn.onValueChanged, controller.LogMusicEnabled);
                Toggle efxOn = columns[1].Toggle("Effects on", true);
                DemoWiring.AddDynamic(efxOn.onValueChanged, settings.SetEfx);
                DemoWiring.AddDynamic(efxOn.onValueChanged, controller.LogEfxEnabled);
                // WHY: the reactive mute flags drive their labels through persistent listeners - no code involved.
                DemoValueReadout musicMuted = DemoSceneKit.Readout(ctx,
                    columns[1].ValueLabel("MuteMusic (reactive)", "-"), "0", onText: "MUTED", offText: "audible");
                DemoWiring.AddDynamic(settings.MuteMusic.OnChanged, musicMuted.SetBool);
                DemoValueReadout efxMuted = DemoSceneKit.Readout(ctx,
                    columns[1].ValueLabel("MuteEfx (reactive)", "-"), "0", onText: "MUTED", offText: "audible");
                DemoWiring.AddDynamic(settings.MuteEfx.OnChanged, efxMuted.SetBool);
                card.Note("Saved through SaveProvider (keys " + SaveKeyPrefix + "*): restart Play Mode and they return.");

                step = "info card";
                frame.InfoCard(info, new DemoInfoContent
                {
                    Title = "Neo.Audio · AM + AMSettings",
                    Purpose = "One AM plays addressable sound entries and crossfading music pools; AMSettings keeps " +
                              "the player's volumes and mutes. Every control calls the real components directly.",
                    RuntimeApi = "AM.I.Play(\"shot\") / PlayAt / PlayMusicPool(\"pads\") / NextMusicTrack / StopMusic / " +
                                 "SetUiVolume; AMSettings.I.SetMusicVolume / SetEfx / SetMusic; events OnMusicStarted, " +
                                 "OnRandomMusicTrackChanged, OnMusicStopped.",
                    SceneWorkflow = "Select 'Audio Demo' to see the AM Sounds and Music lists (ids, clip sets, pitch, " +
                                    "Loop / Shuffle pools); [Demo] Services holds AMSettings and MusicControl. Buttons, " +
                                    "sliders and toggles are wired in their On Click / On Value Changed lists.",
                    Verification = "Enter Play Mode: each sound button plays its entry, Theme loops, Pads shuffle " +
                                   "with crossfades, and mutes/volumes survive a restart.",
                    Hints = new[]
                    {
                        "Buttons -> AM.Play(id) directly (NoCode); 'UI click' is a PlayAudioBtn",
                        "Music buttons -> MusicControl.PlayPool / NextTrack / StopMusic",
                        "Sliders and toggles -> AMSettings (saved); MuteMusic/MuteEfx.OnChanged -> labels"
                    }
                });

                step = "controller references";
                ctx.Set(controller, "_audio", audio);
                ctx.Set(controller, "_settings", settings);
                ctx.Set(controller, "_nowPlaying", nowPlaying);
                ctx.Set(controller, "_musicMuteReadout", musicMuted);
                ctx.Set(controller, "_efxMuteReadout", efxMuted);
                ctx.Set(controller, "_musicVolume", musicVolume.Slider);
                ctx.Set(controller, "_musicVolumeReadout", musicVolume.Readout);
                ctx.Set(controller, "_efxVolume", efxVolume.Slider);
                ctx.Set(controller, "_efxVolumeReadout", efxVolume.Readout);
                ctx.Set(controller, "_uiVolume", uiVolume.Slider);
                ctx.Set(controller, "_uiVolumeReadout", uiVolume.Readout);
                ctx.Set(controller, "_musicToggle", musicOn);
                ctx.Set(controller, "_efxToggle", efxOn);
                ctx.Set(controller, "_log", frame.Log);
            }
            catch (Exception exception)
            {
                throw DemoSceneKit.StepFailed(Scene, step, exception);
            }
        }

        private static void EnsureSources(DemoSceneBuildContext ctx, AM audio)
        {
            if (!DemoSceneBuildContext.IsGenerated(audio.gameObject))
            {
                if (audio.Efx == null || audio.Music == null)
                {
                    ctx.Warn("AM on '" + DemoSceneBuildContext.PathOf(audio.transform) +
                             "' has no Efx/Music AudioSource assigned; AM creates them on first use.");
                }

                return;
            }

            // WHY: AM creates missing sources on a delayed editor call; created outside the build context they would
            // be pruned by the next rebuild, so a generated AM gets authored sources instead.
            AudioSource musicSource = ctx.Ensure<AudioSource>(ctx.Child(audio.transform, "Music"));
            musicSource.playOnAwake = false;
            musicSource.loop = true;
            musicSource.volume = 0.7f;
            musicSource.priority = 126;
            AudioSource efxSource = ctx.Ensure<AudioSource>(ctx.Child(audio.transform, "Efx"));
            efxSource.playOnAwake = false;
            efxSource.loop = false;
            efxSource.volume = 1f;
            efxSource.priority = 127;
            ctx.Set(audio, "_music", musicSource);
            ctx.Set(audio, "_efx", efxSource);
        }

        private static void ConfigureAudio(DemoSceneBuildContext ctx, AM audio)
        {
            ctx.Set(audio, "_playMusicOnStart", false);
            ctx.Set(audio, "_startupMusicId", "theme");
            ctx.Set(audio, "_crossfadeMusic", true);
            ctx.Set(audio, "_musicFadeDuration", 0.8f);

            SoundEntry shot = new("shot", Clip("weapon_player.ogg")) { Volume = 0.8f };
            SoundEntry pickup = new("pickup", Clip("get_pikup.ogg"));
            SoundEntry explosion = new("explosion", Clip("explosion_asteroid.ogg"), Clip("explosion_player.ogg"))
            {
                Volume = 0.9f
            };
            explosion.SetPitchRange(0.85f, 1.1f);
            SoundEntry click = new("ui_click", Clip("demo_ui_click.ogg"))
            {
                Channel = SoundChannel.Ui,
                RandomizePitch = false,
                SpatialBlend = 0f
            };
            audio.SetSoundEntries(shot, pickup, explosion, click);

            MusicEntry theme = new("theme", Clip("music_background.ogg")) { Mode = MusicPoolMode.Loop, Volume = 0.8f };
            MusicEntry pads = new("pads", Clip("demo_pad_a_minor.ogg"), Clip("demo_pad_f_major.ogg"),
                Clip("demo_pad_c_major.ogg"))
            {
                Mode = MusicPoolMode.Shuffle,
                FadeDuration = 1.2f
            };
            audio.SetMusicEntries(theme, pads);
            ctx.MarkModified(audio);
        }

        private static void ConfigureSettings(DemoSceneBuildContext ctx, AMSettings settings, MusicControl music)
        {
            // WHY: demo-only keys, so trying the demo never overwrites the volumes of the game it is imported into.
            ctx.Set(settings, "persistVolume", true);
            ctx.Set(settings, "persistMute", true);
            ctx.Set(settings, "saveKeyMaster", SaveKeyPrefix + "MasterVolume");
            ctx.Set(settings, "saveKeyMusic", SaveKeyPrefix + "MusicVolume");
            ctx.Set(settings, "saveKeyEfx", SaveKeyPrefix + "EfxVolume");
            ctx.Set(settings, "saveKeyMute", SaveKeyPrefix + "Mute");
            ctx.Set(settings, "startMusicVolume", 0.5f);
            ctx.Set(settings, "startEfxVolume", 1f);

            ctx.Set(music, "_poolId", "theme");
            ctx.Set(music, "_playOnEnable", false);
            ctx.Set(music, "_transition", (int)MusicControl.TransitionMode.Default);
        }

        private static void WireSound(Button button, AM audio, AudioDemoController controller, string id)
        {
            DemoWiring.AddString(button.onClick, audio.Play, id);
            DemoWiring.AddString(button.onClick, controller.LogSound, id);
        }

        private static AudioClip Clip(string fileName)
        {
            return DemoSceneAssets.LoadDemoAsset<AudioClip>(ClipFolder + fileName);
        }
    }
}
