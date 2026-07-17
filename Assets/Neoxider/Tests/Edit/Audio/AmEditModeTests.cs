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
