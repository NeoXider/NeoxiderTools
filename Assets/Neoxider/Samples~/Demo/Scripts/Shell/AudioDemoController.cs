using Neo.Audio;
using TMPro;
using UnityEngine;

namespace Neo.Samples
{
    /// <summary>
    ///     Bright, self-contained demo for the <b>Neo.Audio</b> module. Ensures an <see cref="AM" /> singleton
    ///     exists (creating one if the scene has none), then plays fully procedural <see cref="AudioClip" />s
    ///     (sine beep, major chord, noise burst, looping pad) through the real <see cref="AM" /> API and wires
    ///     a volume slider and a music toggle to it. Every press is logged with the exact call it makes.
    ///     Robust in an empty scene: the shell builds its own camera, canvas and EventSystem.
    /// </summary>
    [AddComponentMenu("Neoxider/Demos/Audio Demo")]
    public sealed class AudioDemoController : MonoBehaviour
    {
        private const int SampleRate = 44100;

        private AM _am;
        private NeoDemoShell.Context _shell;
        private TMP_Text _musicValue;

        private AudioClip _beep;
        private AudioClip _chord;
        private AudioClip _noise;
        private AudioClip _pad;
        private AudioClip _padHigh;
        private AudioClip _padLow;

        private void Start()
        {
            _shell = NeoDemoShell.Build("Neo.Audio", new Color(0.25f, 0.79f, 0.94f));

            NeoDemoShell.ShowInfoCardOnce(
                "Neo.Audio · AM",
                "Central audio manager. All clips below are generated in code with AudioClip.Create — no imported assets.",
                "Press Beep / Chord / Noise to call AM.I.Play(clip)",
                "Volume slider drives AM.I.SetEfxVolume(v)",
                "Music toggle loops a generated pad via AM.I.PlayMusicByClip(clip)",
                "Random row shuffles generated pads via AM.I.EnableRandomMusic()");

            EnsureAudioManager();
            BuildClips();

            _shell.AddButtonRow(
                ("Beep", PlayBeep),
                ("Chord", PlayChord),
                ("Noise burst", PlayNoise));

            _shell.AddSlider("Effects volume", 0f, 1f, 1f, v =>
            {
                _am.SetEfxVolume(v);
                _shell.Log($"AM.I.SetEfxVolume({v:0.00})");
            });

            _musicValue = _shell.AddValueLabel("Music (looping pad)");
            _musicValue.text = "OFF";
            _shell.AddToggle("Music on/off", false, ToggleMusic);

            _shell.AddButtonRow(
                ("Random music", StartRandomMusic),
                ("Stop music", StopAllMusic));

            _am.OnRandomMusicTrackChanged += OnRandomTrackChanged;
            _am.OnMusicStopped += OnMusicStopped;

            _shell.Log("AM ready — clips generated at runtime");
        }

        private void EnsureAudioManager()
        {
            _am = AM.I;
            if (_am == null)
            {
                // WHY: no AM in the scene — opt the singleton into lazy creation, then resolve it.
                AM.CreateInstance = true;
                _am = AM.I;
                _shell.Log("Created AM singleton (GameObject + AM)");
            }
            else
            {
                _shell.Log("Reused existing AM.I from scene");
            }
        }

        private void PlayBeep()
        {
            _am.Play(_beep);
            _shell.Log("AM.I.Play(beep 880Hz sine)");
        }

        private void PlayChord()
        {
            _am.Play(_chord);
            _shell.Log("AM.I.Play(chord A-major)");
        }

        private void PlayNoise()
        {
            _am.Play(_noise);
            _shell.Log("AM.I.Play(noise burst)");
        }

        private void ToggleMusic(bool on)
        {
            if (on)
            {
                // WHY: music AudioSource loops by default (AM.CreateMusic), so the short pad tiles seamlessly.
                _am.PlayMusicByClip(_pad);
                _musicValue.text = "ON";
                _shell.Log("AM.I.PlayMusicByClip(pad) — looping");
            }
            else
            {
                _am.StopMusic();
                _musicValue.text = "OFF";
                _shell.Log("AM.I.StopMusic()");
            }
        }

        private void StartRandomMusic()
        {
            _am.SetRandomMusicTracks(_pad, _padHigh, _padLow);
            _am.EnableRandomMusic();
            _musicValue.text = "RANDOM";
            _shell.Log("AM.I.SetRandomMusicTracks(3 pads) + EnableRandomMusic()");
        }

        private void StopAllMusic()
        {
            _am.StopMusic();
            _musicValue.text = "OFF";
            _shell.Log("AM.I.StopMusic()");
        }

        private void OnRandomTrackChanged(AudioClip clip)
        {
            _shell.Log($"AM.OnRandomMusicTrackChanged → {clip.name}");
        }

        private void OnMusicStopped()
        {
            _musicValue.text = "OFF";
            _shell.Log("AM.OnMusicStopped");
        }

        private void OnDestroy()
        {
            if (_am != null)
            {
                _am.OnRandomMusicTrackChanged -= OnRandomTrackChanged;
                _am.OnMusicStopped -= OnMusicStopped;
            }
        }

        private void BuildClips()
        {
            _beep = MakeTone("demo_beep", 0.16f, 880f, 0.9f);
            _chord = MakeChord("demo_chord", 0.55f, new[] { 440f, 554.37f, 659.25f });
            _noise = MakeNoise("demo_noise", 0.22f);
            _pad = MakePad("demo_pad", 1.6f, 110f);
            _padHigh = MakePad("demo_pad_high", 1.6f, 165f);
            _padLow = MakePad("demo_pad_low", 1.6f, 82.5f);
        }

        private static AudioClip MakeTone(string name, float seconds, float freq, float amp)
        {
            int count = Mathf.CeilToInt(SampleRate * seconds);
            var data = new float[count];
            for (int i = 0; i < count; i++)
            {
                float t = i / (float)SampleRate;
                float env = Envelope(i, count);
                data[i] = Mathf.Sin(2f * Mathf.PI * freq * t) * amp * env;
            }

            return ToClip(name, data);
        }

        private static AudioClip MakeChord(string name, float seconds, float[] freqs)
        {
            int count = Mathf.CeilToInt(SampleRate * seconds);
            var data = new float[count];
            for (int i = 0; i < count; i++)
            {
                float t = i / (float)SampleRate;
                float env = Envelope(i, count);
                float sum = 0f;
                for (int f = 0; f < freqs.Length; f++)
                {
                    sum += Mathf.Sin(2f * Mathf.PI * freqs[f] * t);
                }

                data[i] = sum / freqs.Length * 0.9f * env;
            }

            return ToClip(name, data);
        }

        private static AudioClip MakeNoise(string name, float seconds)
        {
            int count = Mathf.CeilToInt(SampleRate * seconds);
            var data = new float[count];
            var rng = new System.Random(12345);
            for (int i = 0; i < count; i++)
            {
                float env = Envelope(i, count);
                data[i] = (float)(rng.NextDouble() * 2.0 - 1.0) * 0.7f * env;
            }

            return ToClip(name, data);
        }

        private static AudioClip MakePad(string name, float seconds, float baseFreq)
        {
            // WHY: a soft, seamlessly-looping pad — two detuned saw-ish partials, no hard start/stop envelope.
            int count = Mathf.CeilToInt(SampleRate * seconds);
            var data = new float[count];
            for (int i = 0; i < count; i++)
            {
                float t = i / (float)SampleRate;
                float a = Mathf.Sin(2f * Mathf.PI * baseFreq * t);
                float b = Mathf.Sin(2f * Mathf.PI * (baseFreq * 1.5f) * t);
                float c = Mathf.Sin(2f * Mathf.PI * (baseFreq * 2.01f) * t) * 0.4f;
                data[i] = (a * 0.5f + b * 0.3f + c) * 0.35f;
            }

            return ToClip(name, data);
        }

        private static float Envelope(int i, int count)
        {
            // WHY: short attack + exponential-ish decay so effect clips don't click.
            float attack = Mathf.Clamp01(i / (SampleRate * 0.005f));
            float pos = i / (float)count;
            float decay = Mathf.Pow(1f - pos, 1.6f);
            return attack * decay;
        }

        private static AudioClip ToClip(string name, float[] data)
        {
            AudioClip clip = AudioClip.Create(name, data.Length, 1, SampleRate, false);
            clip.SetData(data, 0);
            return clip;
        }
    }
}
