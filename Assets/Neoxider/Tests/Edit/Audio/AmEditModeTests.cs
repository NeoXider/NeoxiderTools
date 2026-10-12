// WHY: this fixture deliberately exercises the pre-10.13 API - EnableRandomMusic / SetRandomMusicTracks -
// which is now [Obsolete] but must keep working unchanged for existing projects. That is the whole point of
// the fixture, so the deprecation warning is suppressed here rather than the calls being rewritten.
#pragma warning disable CS0618

using System;
using System.Reflection;
using Neo.Audio;
using NUnit.Framework;
using UnityEngine;

namespace Neo.Editor.Tests
{
    /// <summary>
    ///     EditMode coverage for <see cref="AM"/> beyond the basic volume tests in
    ///     <c>AudioEditModeTests</c>. AudioSources are injected via reflection (AM auto-creates
    ///     them only at runtime). Actual audio playback is not available in EditMode, so these
    ///     assert state, clip assignment, volume clamping, flags and C# event invocation counts.
    /// </summary>
    [TestFixture]
    public class AmEditModeTests
    {
        private GameObject _go;
        private AM _audioManager;
        private AudioSource _efx;
        private AudioSource _music;

        [SetUp]
        public void SetUp()
        {
            _go = new GameObject("AmEditModeTests");
            _audioManager = _go.AddComponent<AM>();
            _efx = _go.AddComponent<AudioSource>();
            _music = _go.AddComponent<AudioSource>();

            SetPrivateField("_efx", _efx);
            SetPrivateField("_music", _music);
        }

        [TearDown]
        public void TearDown()
        {
            // WHY: Stop any random-music UniTask loop spawned during a test before destroying the GameObject.
            if (_audioManager != null)
            {
                _audioManager.DisableRandomMusic();
            }

            if (_go != null)
            {
                UnityEngine.Object.DestroyImmediate(_go);
            }
        }

        [Test]
        public void SetVolume_Efx_ClampsAboveAndBelowRange()
        {
            _audioManager.SetVolume(5f, true);
            Assert.AreEqual(1f, _efx.volume, "Efx volume above range must clamp to 1.");

            _audioManager.SetVolume(-3f, true);
            Assert.AreEqual(0f, _efx.volume, "Efx volume below range must clamp to 0.");

            _audioManager.SetVolume(0.42f, true);
            Assert.AreEqual(0.42f, _efx.volume, 1e-5f, "In-range efx volume must pass through unchanged.");
        }

        [Test]
        public void SetVolume_Music_ClampsAboveAndBelowRange()
        {
            _audioManager.SetVolume(9f, false);
            Assert.AreEqual(1f, _music.volume, "Music volume above range must clamp to 1.");

            _audioManager.SetVolume(-0.5f, false);
            Assert.AreEqual(0f, _music.volume, "Music volume below range must clamp to 0.");

            _audioManager.SetVolume(0.33f, false);
            Assert.AreEqual(0.33f, _music.volume, 1e-5f, "In-range music volume must pass through unchanged.");
        }

        [Test]
        public void EnableRandomMusic_WithNoTracks_DoesNotEnable()
        {
            // WHY: _randomMusicTracks is left null (default) -> warning path, no enable.
            _audioManager.EnableRandomMusic();

            Assert.IsFalse(_audioManager.IsRandomMusicEnabled(),
                "EnableRandomMusic with an empty track list must not enable random music.");
        }

        [Test]
        public void EnableRandomMusic_WithEmptyArray_DoesNotEnable()
        {
            SetPrivateField("_randomMusicTracks", Array.Empty<AudioClip>());

            _audioManager.EnableRandomMusic();

            Assert.IsFalse(_audioManager.IsRandomMusicEnabled(),
                "EnableRandomMusic with a zero-length track list must not enable random music.");
        }

        [Test]
        public void EnableThenDisableRandomMusic_TogglesStateCorrectly()
        {
            SetPrivateField("_randomMusicTracks", new[] { CreateClip("t0"), CreateClip("t1") });

            _audioManager.EnableRandomMusic();
            Assert.IsTrue(_audioManager.IsRandomMusicEnabled(),
                "With tracks present, EnableRandomMusic must enable random music.");

            _audioManager.DisableRandomMusic();
            Assert.IsFalse(_audioManager.IsRandomMusicEnabled(),
                "DisableRandomMusic must turn random music off.");
        }

        [Test]
        public void PlayMusicByClip_FiresOnMusicStartedOncePerCall()
        {
            int started = 0;
            _audioManager.OnMusicStarted += _ => started++;

            AudioClip clip = CreateClip("music");
            _audioManager.PlayMusicByClip(clip);

            Assert.AreEqual(1, started, "OnMusicStarted must fire exactly once per PlayMusicByClip call.");

            _audioManager.PlayMusicByClip(CreateClip("music2"));
            Assert.AreEqual(2, started, "A second PlayMusicByClip call must raise OnMusicStarted again.");
        }

        [Test]
        public void EnableRandomMusic_WhileMusicPlaying_FiresOnMusicStoppedOnce()
        {
            SetPrivateField("_randomMusicTracks", new[] { CreateClip("rnd0"), CreateClip("rnd1") });

            int stopped = 0;
            _audioManager.OnMusicStopped += () => stopped++;

            // WHY: Start single-track music so _music.isPlaying becomes true, then switching to random
            // music must stop it exactly once.
            _audioManager.PlayMusicByClip(CreateClip("single"));
            _audioManager.EnableRandomMusic();

            Assert.AreEqual(1, stopped,
                "Switching from playing single-track music to random music must raise OnMusicStopped exactly once.");
        }

        [Test]
        public void StopMusic_AfterPlayMusicByClip_FiresOnMusicStoppedOnce()
        {
            int stopped = 0;
            _audioManager.OnMusicStopped += () => stopped++;

            _audioManager.PlayMusicByClip(CreateClip("single"));
            _audioManager.StopMusic();

            Assert.AreEqual(1, stopped,
                "StopMusic while single-track music plays must raise OnMusicStopped exactly once.");
        }

        [Test]
        public void StopMusic_WhenNothingPlaying_DoesNotFire()
        {
            int stopped = 0;
            _audioManager.OnMusicStopped += () => stopped++;

            _audioManager.StopMusic();

            Assert.AreEqual(0, stopped,
                "StopMusic with nothing playing must not raise OnMusicStopped.");
        }

        [Test]
        public void SetRandomMusicTracks_ReplacesListUsedByEnableRandomMusic()
        {
            // WHY: no tracks serialized -> without SetRandomMusicTracks, EnableRandomMusic would warn and no-op.
            _audioManager.SetRandomMusicTracks(CreateClip("a"), CreateClip("b"));
            _audioManager.EnableRandomMusic();

            Assert.IsTrue(_audioManager.IsRandomMusicEnabled(),
                "EnableRandomMusic must use the list injected via SetRandomMusicTracks.");
        }

        [Test]
        public void GetCurrentMusicClip_ReturnsNullWhenNothingPlaying()
        {
            Assert.IsNull(_audioManager.GetCurrentMusicClip(),
                "No music assigned yet -> GetCurrentMusicClip must return null.");
        }

        [Test]
        public void GetCurrentMusicClip_ReturnsClipAfterPlayMusicByClip()
        {
            AudioClip clip = CreateClip("current");
            _audioManager.PlayMusicByClip(clip);

            Assert.AreSame(clip, _audioManager.GetCurrentMusicClip(),
                "GetCurrentMusicClip must return the clip passed to PlayMusicByClip.");
            Assert.AreSame(clip, _music.clip,
                "PlayMusicByClip must assign the clip to the music AudioSource.");
        }

        [Test]
        public void Play_AudioClipOverload_DoesNotThrowForValidOrNullClip()
        {
            AudioClip clip = CreateClip("efx");

            Assert.DoesNotThrow(() => _audioManager.Play(clip),
                "Play(AudioClip) with a valid injected AudioSource must not throw.");
            Assert.DoesNotThrow(() => _audioManager.Play((AudioClip)null),
                "Play(AudioClip) with a null clip must be handled gracefully (warning path, no exception).");
        }

        [Test]
        public void PlayMusicByClip_HandlesNullClipGracefully()
        {
            // WHY: Establish a known clip, then a null call must early-out without clearing it or throwing.
            AudioClip clip = CreateClip("keep");
            _audioManager.PlayMusicByClip(clip);

            Assert.DoesNotThrow(() => _audioManager.PlayMusicByClip((AudioClip)null),
                "PlayMusicByClip(null) must not throw.");
            Assert.AreSame(clip, _music.clip,
                "PlayMusicByClip(null) must not overwrite the currently assigned clip.");
        }

        [Test]
        public void PlayMusicByClip_NullClip_DoesNotFireOnMusicStarted()
        {
            int started = 0;
            _audioManager.OnMusicStarted += _ => started++;

            _audioManager.PlayMusicByClip((AudioClip)null);

            Assert.AreEqual(0, started,
                "A null clip must not raise OnMusicStarted (it returns before playback).");
        }

        [Test]
        public void PlayAmbience_LoopsOnItsOwnSourceAtTheRelativeLevelOfTheMusicChannel()
        {
            AudioClip lake = CreateClip("lake");
            _audioManager.SetVolume(0.8f, false);
            _audioManager.AmbienceVolume = 0.25f;

            _audioManager.PlayAmbience(lake, 0f);

            AudioSource ambience = _audioManager.Ambience;
            Assert.IsNotNull(ambience, "The ambience source is created on first use.");
            Assert.AreNotSame(_music, ambience, "Ambience must not take over the music source.");
            Assert.AreSame(lake, ambience.clip);
            Assert.IsTrue(ambience.loop);
            Assert.AreEqual(0.2f, ambience.volume, 1e-4f, "music channel 0.8 x ambience 0.25");
        }

        [Test]
        public void Ambience_FollowsMusicChannelVolumeAndMute()
        {
            _audioManager.AmbienceVolume = 0.5f;
            _audioManager.PlayAmbience(CreateClip("lake"), 0f);

            _music.mute = true;
            _audioManager.SetVolume(0.4f, false);

            Assert.IsTrue(_audioManager.Ambience.mute, "Muting music mutes the bed under it.");
            Assert.AreEqual(0.2f, _audioManager.Ambience.volume, 1e-4f);

            _audioManager.AmbienceVolume = 0f;
            Assert.AreEqual(0f, _audioManager.Ambience.volume, 1e-4f);
        }

        [Test]
        public void PlayAmbience_CreatedSourceIsNotSavedWithTheScene()
        {
            _audioManager.PlayAmbience(CreateClip("lake"), 0f);

            Assert.AreEqual(HideFlags.DontSave, _audioManager.Ambience.gameObject.hideFlags & HideFlags.DontSave,
                "An edit-time call must not leave an Ambience child saved into the scene, like PitchVoice_*.");
        }

        [Test]
        public void StopAmbience_StopsTheLoopAndSilencesIt()
        {
            _audioManager.PlayAmbience(CreateClip("lake"), 0f);
            AudioSource ambience = _audioManager.Ambience;
            Assume.That(ambience.isPlaying, "EditMode audio playback is unavailable on this machine.");

            _audioManager.StopAmbience(0f);

            Assert.IsFalse(ambience.isPlaying);
            Assert.IsNull(_audioManager.CurrentAmbienceClip);
            Assert.AreEqual(0f, ambience.volume, 1e-4f);
        }

        [Test]
        public void StopAmbience_WhenNothingStarted_DoesNotCreateASource()
        {
            Assert.DoesNotThrow(() => _audioManager.StopAmbience(0f));
            Assert.IsNull(_audioManager.Ambience);
        }

        [Test]
        public void PlayAmbience_Null_StopsTheRunningLoop()
        {
            _audioManager.PlayAmbience(CreateClip("lake"), 0f);
            AudioSource ambience = _audioManager.Ambience;
            Assume.That(ambience.isPlaying, "EditMode audio playback is unavailable on this machine.");

            _audioManager.PlayAmbience(null, 0f);

            Assert.IsFalse(ambience.isPlaying, "PlayAmbience(null) is the documented way to stop the bed.");
            Assert.IsNull(_audioManager.CurrentAmbienceClip);
        }

        [Test]
        public void PlayAmbience_Null_WithoutASource_IsANoOp()
        {
            Assert.DoesNotThrow(() => _audioManager.PlayAmbience(null));
            Assert.IsNull(_audioManager.Ambience, "Stopping nothing must not create the ambience source.");
        }

        [Test]
        public void PlayAmbience_SameClipAlreadyPlaying_IsANoOp()
        {
            AudioClip lake = CreateClip("lake");
            _audioManager.PlayAmbience(lake, 0f);
            AudioSource ambience = _audioManager.Ambience;
            Assume.That(ambience.isPlaying, "EditMode audio playback is unavailable on this machine.");

            // WHY: a sentinel level proves the second call returned early instead of re-applying the volume.
            ambience.volume = 0.123f;
            _audioManager.PlayAmbience(lake, 0f);

            Assert.AreSame(ambience, _audioManager.Ambience);
            Assert.AreSame(lake, ambience.clip);
            Assert.IsTrue(ambience.isPlaying);
            Assert.AreEqual(0.123f, ambience.volume, 1e-4f, "Asking for the clip already looping must not touch it.");
        }

        [Test]
        public void Ambience_CopiesADirectMusicMuteInLateUpdate()
        {
            _audioManager.PlayAmbience(CreateClip("lake"), 0f);
            AudioSource ambience = _audioManager.Ambience;
            Assert.IsFalse(ambience.mute);

            // WHY: AMSettings.SetMusic flips the source's mute with no AM call; only LateUpdate can follow it.
            _music.mute = true;
            Assert.IsFalse(ambience.mute, "Nothing but LateUpdate may have copied the flag yet.");

            InvokeLateUpdate();
            Assert.IsTrue(ambience.mute, "LateUpdate must copy the music mute onto the ambience.");

            _music.mute = false;
            InvokeLateUpdate();
            Assert.IsFalse(ambience.mute, "LateUpdate must copy the unmute as well.");
        }

        [Test]
        public void ApplyStartVolumes_RelevelsAnAmbienceStartedEarlier()
        {
            _audioManager.SetVolume(1f, false);
            _audioManager.AmbienceVolume = 0.5f;
            _audioManager.PlayAmbience(CreateClip("lake"), 0f);
            Assert.AreEqual(0.5f, _audioManager.Ambience.volume, 1e-4f);

            _audioManager.StartVolumeMusic = 0.4f;
            _audioManager.ApplyStartVolumes();

            Assert.AreEqual(0.4f, _music.volume, 1e-4f);
            Assert.AreEqual(0.2f, _audioManager.Ambience.volume, 1e-4f,
                "An ambience started before AMSettings.Start must follow the start music volume.");
        }

        [Test]
        public void AmSettingsMusicSlider_MovesTheAmbienceWithTheMusic()
        {
            AMSettings settings = CreateSettings();
            _audioManager.SetVolume(0.8f, false);
            _audioManager.AmbienceVolume = 0.5f;
            _audioManager.PlayAmbience(CreateClip("lake"), 0f);
            Assert.AreEqual(0.4f, _audioManager.Ambience.volume, 1e-4f);

            settings.SetMusicVolume(0f);

            Assert.AreEqual(0f, _music.volume, 1e-4f);
            Assert.AreEqual(0f, _audioManager.MusicVolume, 1e-4f, "The slider must move AM's music channel.");
            Assert.AreEqual(0f, _audioManager.Ambience.volume, 1e-4f, "A music slider at zero silences the bed too.");

            settings.SetMusicVolume(0.6f);

            Assert.AreEqual(0.6f, _music.volume, 1e-4f);
            Assert.AreEqual(0.3f, _audioManager.Ambience.volume, 1e-4f, "Restoring the slider restores the bed.");
            Assert.AreEqual(0.6f, settings.GetMusicVolumeNormalized(), 1e-4f);
        }

        [Test]
        public void AmSettingsMasterSlider_MovesTheAmbienceWithTheMusic()
        {
            AMSettings settings = CreateSettings();
            _audioManager.SetVolume(1f, false);
            _audioManager.AmbienceVolume = 0.5f;
            _audioManager.PlayAmbience(CreateClip("lake"), 0f);

            settings.SetMusicAndEfxVolume(0.2f);

            Assert.AreEqual(0.2f, _efx.volume, 1e-4f);
            Assert.AreEqual(0.2f, _music.volume, 1e-4f);
            Assert.AreEqual(0.1f, _audioManager.Ambience.volume, 1e-4f);
        }

        [Test]
        public void AmSettingsMusicSlider_WithAnEntryTrim_SavesTheChannelNotTheProduct()
        {
            AMSettings settings = CreateSettings();
            // WHY: a music entry played at half volume makes channel x trim differ from the channel, which is the
            // case where reading music.volume back instead of the channel would corrupt the saved slider value.
            typeof(AM).GetField("_currentMusicEntryVolume",
                    System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic)
                .SetValue(_audioManager, 0.5f);

            settings.SetMusicVolume(0.8f);

            Assert.AreEqual(0.8f, settings.GetMusicVolumeNormalized(), 1e-4f,
                "The slider and the save key must hold the channel value, not channel x trim.");
            Assert.AreEqual(0.4f, _music.volume, 1e-4f,
                "Without a mixer the source plays at channel x the current music entry trim.");
        }

        private AMSettings CreateSettings()
        {
            // WHY: Start (which wires AM, the sources and persistence) does not run in EditMode, so the same
            // wiring is injected here; persistence is off so the test never writes save keys.
            AMSettings settings = _go.AddComponent<AMSettings>();
            const BindingFlags instanceFlags = BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.Instance;
            typeof(AMSettings).GetField("_am", instanceFlags).SetValue(settings, _audioManager);
            typeof(AMSettings).GetField("persistVolume", instanceFlags).SetValue(settings, false);
            typeof(AMSettings).GetProperty("music", instanceFlags).SetValue(settings, _music);
            typeof(AMSettings).GetProperty("efx", instanceFlags).SetValue(settings, _efx);
            return settings;
        }

        private void InvokeLateUpdate()
        {
            MethodInfo lateUpdate = typeof(AM).GetMethod("LateUpdate", BindingFlags.NonPublic | BindingFlags.Instance);
            Assert.IsNotNull(lateUpdate, "AM.LateUpdate is what keeps the ambience mute in step with the music.");
            lateUpdate.Invoke(_audioManager, null);
        }

        private static AudioClip CreateClip(string name)
        {
            // WHY: 0.1s of mono silence is enough for clip identity / length math.
            return AudioClip.Create(name, 4410, 1, 44100, false);
        }

        private void SetPrivateField(string fieldName, object value)
        {
            typeof(AM).GetField(fieldName, BindingFlags.NonPublic | BindingFlags.Instance)
                ?.SetValue(_audioManager, value);
        }
    }
}
