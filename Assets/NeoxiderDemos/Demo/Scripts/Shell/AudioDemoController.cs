using System.Globalization;
using Neo.Audio;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Neo.Samples
{
    /// <summary>
    ///     Controller of the authored <b>Neo.Audio</b> demo scene. The UI, the <see cref="AM" /> sound entries and
    ///     music pools, the <see cref="AMSettings" /> service and the <see cref="MusicControl" /> component are saved
    ///     in AudioDemo.unity. Buttons call <c>AM.Play(id)</c>,
    ///     <see cref="MusicControl" /> and <see cref="AMSettings" /> directly through persistent UnityEvents (the
    ///     NoCode path); this component only logs those calls, mirrors the live state into the authored labels and
    ///     syncs the controls with the persisted settings on start. It builds no UI and creates no clips at runtime.
    /// </summary>
    [AddComponentMenu("Neoxider/Demos/Audio Demo")]
    public sealed class AudioDemoController : MonoBehaviour
    {
        [Header("Services")]
        [Tooltip("Scene audio manager with the demo's sound entries and music pools.")]
        [SerializeField] private AM _audio;

        [Tooltip("Player settings service: persisted volumes and mutes.")]
        [SerializeField] private AMSettings _settings;

        [Header("Live values")]
        [SerializeField] private TMP_Text _nowPlaying;
        [SerializeField] private DemoValueReadout _musicMuteReadout;
        [SerializeField] private DemoValueReadout _efxMuteReadout;

        [Header("Controls synced on start")]
        [SerializeField] private Slider _musicVolume;
        [SerializeField] private DemoValueReadout _musicVolumeReadout;
        [SerializeField] private Slider _efxVolume;
        [SerializeField] private DemoValueReadout _efxVolumeReadout;
        [SerializeField] private Slider _uiVolume;
        [SerializeField] private DemoValueReadout _uiVolumeReadout;
        [SerializeField] private Toggle _musicToggle;
        [SerializeField] private Toggle _efxToggle;

        [Header("Feed")]
        [SerializeField] private DemoLogFeed _log;

        private AM _subscribedAudio;
        private bool _started;
        private bool _warnedUnbuilt;

        /// <summary>True when the authored scene references are assigned.</summary>
        public bool IsSceneBuilt => _audio != null && _settings != null && _nowPlaying != null && _log != null &&
                                    _musicVolume != null && _efxVolume != null && _musicToggle != null &&
                                    _efxToggle != null;

        /// <summary>Audio manager the demo drives.</summary>
        public AM Audio => _audio;

        /// <summary>Settings service the demo drives.</summary>
        public AMSettings Settings => _settings;

        private void OnEnable()
        {
            if (_started)
            {
                Subscribe();
            }
        }

        private void Start()
        {
            _started = true;
            if (!IsSceneBuilt)
            {
                WarnUnbuilt();
            }

            ResolveServices();
            Subscribe();
            SyncControls();
            RefreshNowPlaying();
            if (_audio != null)
            {
                Log("AM ready: " + _audio.SoundEntries.Count + " sound entries, " + _audio.MusicEntries.Count +
                    " music pools (select 'Audio Demo' to see them)");
            }
        }

        private void OnDisable()
        {
            Unsubscribe();
        }

        /// <summary>Sound button listener next to the NoCode <c>AM.Play(id)</c> call.</summary>
        public void LogSound(string id)
        {
            Log("AM.I.Play(\"" + id + "\")");
        }

        /// <summary>Music slider listener next to <see cref="AMSettings.SetMusicVolume" />.</summary>
        public void LogMusicVolume(float value)
        {
            Log("AMSettings.SetMusicVolume(" + Format(value) + ") -> saved");
        }

        /// <summary>Effects slider listener next to <see cref="AMSettings.SetEfxVolume" />.</summary>
        public void LogEfxVolume(float value)
        {
            Log("AMSettings.SetEfxVolume(" + Format(value) + ") -> saved");
        }

        /// <summary>UI slider listener next to <see cref="AM.SetUiVolume" />.</summary>
        public void LogUiVolume(float value)
        {
            Log("AM.I.SetUiVolume(" + Format(value) + ")");
        }

        /// <summary>Music toggle listener next to <see cref="AMSettings.SetMusic" />.</summary>
        public void LogMusicEnabled(bool enabled)
        {
            Log("AMSettings.SetMusic(" + (enabled ? "true" : "false") + ") -> mute saved");
        }

        /// <summary>Effects toggle listener next to <see cref="AMSettings.SetEfx" />.</summary>
        public void LogEfxEnabled(bool enabled)
        {
            Log("AMSettings.SetEfx(" + (enabled ? "true" : "false") + ") -> mute saved");
        }

        private void ResolveServices()
        {
            if (_audio == null && AM.TryGetInstance(out AM audio))
            {
                _audio = audio;
            }

            if (_settings == null && AMSettings.TryGetInstance(out AMSettings settings))
            {
                _settings = settings;
            }
        }

        private void Subscribe()
        {
            if (_audio == null || ReferenceEquals(_subscribedAudio, _audio))
            {
                return;
            }

            Unsubscribe();
            _subscribedAudio = _audio;
            _audio.OnMusicStarted += HandleMusicStarted;
            _audio.OnRandomMusicTrackChanged += HandleTrackChanged;
            _audio.OnMusicStopped += HandleMusicStopped;
        }

        private void Unsubscribe()
        {
            // WHY ReferenceEquals: a destroyed manager still holds the managed delegates; unsubscribing them is safe.
            if (ReferenceEquals(_subscribedAudio, null))
            {
                return;
            }

            _subscribedAudio.OnMusicStarted -= HandleMusicStarted;
            _subscribedAudio.OnRandomMusicTrackChanged -= HandleTrackChanged;
            _subscribedAudio.OnMusicStopped -= HandleMusicStopped;
            _subscribedAudio = null;
        }

        private void HandleMusicStarted(AudioClip clip)
        {
            RefreshNowPlaying();
            Log("AM.OnMusicStarted -> " + Describe(clip));
        }

        private void HandleTrackChanged(AudioClip clip)
        {
            RefreshNowPlaying();
            Log("AM.OnRandomMusicTrackChanged -> " + Describe(clip) + " (Shuffle pool)");
        }

        private void HandleMusicStopped()
        {
            RefreshNowPlaying();
            Log("AM.OnMusicStopped");
        }

        private void SyncControls()
        {
            if (_settings != null)
            {
                SyncSlider(_musicVolume, _musicVolumeReadout, _settings.GetMusicVolumeNormalized());
                SyncSlider(_efxVolume, _efxVolumeReadout, _settings.GetEfxVolumeNormalized());
                SyncToggle(_musicToggle, !_settings.MuteMusicValue);
                SyncToggle(_efxToggle, !_settings.MuteEfxValue);
                if (_musicMuteReadout != null)
                {
                    _musicMuteReadout.SetBool(_settings.MuteMusicValue);
                }

                if (_efxMuteReadout != null)
                {
                    _efxMuteReadout.SetBool(_settings.MuteEfxValue);
                }
            }

            if (_audio != null)
            {
                SyncSlider(_uiVolume, _uiVolumeReadout, _audio.UiVolume);
            }
        }

        private void RefreshNowPlaying()
        {
            if (_nowPlaying == null)
            {
                return;
            }

            AudioClip clip = _audio != null ? _audio.GetCurrentMusicClip() : null;
            bool playing = _audio != null && _audio.Music != null && _audio.Music.isPlaying && clip != null;
            if (!playing)
            {
                _nowPlaying.text = "silence";
                return;
            }

            string pool = _audio.CurrentMusicId;
            _nowPlaying.text = string.IsNullOrEmpty(pool) ? clip.name : pool + " · " + clip.name;
        }

        private static void SyncSlider(Slider slider, DemoValueReadout readout, float value)
        {
            if (slider != null)
            {
                slider.SetValueWithoutNotify(value);
            }

            if (readout != null)
            {
                readout.SetFloat(value);
            }
        }

        private static void SyncToggle(Toggle toggle, bool isOn)
        {
            if (toggle != null)
            {
                toggle.SetIsOnWithoutNotify(isOn);
            }
        }

        private static string Describe(AudioClip clip)
        {
            return clip != null ? clip.name : "none";
        }

        private static string Format(float value)
        {
            return value.ToString("0.00", CultureInfo.InvariantCulture);
        }

        private void Log(string message)
        {
            if (_log != null)
            {
                _log.Log(message);
            }
        }

        private void WarnUnbuilt()
        {
            if (_warnedUnbuilt)
            {
                return;
            }

            _warnedUnbuilt = true;
            global::Neo.Demo.SampleDiagnostics.Warn(
                "[AudioDemoController] The scene UI is not authored. Re-import the scene from the Demo download.",
                this);
        }
    }
}
