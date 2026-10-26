using System.Collections.Generic;
using Neo.Audio;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Neo.Editor.Tests
{
    public sealed class Audit20261023NeoAudioRegressionTests
    {
        private readonly List<Object> _owned = new List<Object>();
        private AM _manager;
        private Random.State _randomState;

        [SetUp]
        public void SetUp()
        {
            _randomState = Random.state;
            _manager = CreateObject("Audio audit").AddComponent<AM>();
        }

        [TearDown]
        public void TearDown()
        {
            for (int index = _owned.Count - 1; index >= 0; index--)
            {
                if (_owned[index] != null)
                {
                    Object.DestroyImmediate(_owned[index]);
                }
            }

            _owned.Clear();
            Random.state = _randomState;
        }

        [Test]
        public void SparseClipList_NeverRepeatsWhenAnAlternativeExists()
        {
            Random.State randomState = Random.state;
            try
            {
                Random.InitState(20261023);
                AudioClip first = CreateClip("First");
                AudioClip second = CreateClip("Second");
                AudioClip[] clips = new AudioClip[256];
                clips[0] = first;
                clips[255] = second;
                SoundEntry entry = new SoundEntry("Sparse", clips);
                entry.SetClipVolume(0, 0.2f);
                entry.SetClipVolume(255, 0.8f);
                AudioClip previous = entry.ClipAt(0);

                for (int index = 0; index < 128; index++)
                {
                    AudioClip current = entry.NextClip();
                    Assert.That(current, Is.Not.Null);
                    Assert.That(current, Is.Not.SameAs(previous), "A usable alternative must beat the previous clip.");
                    Assert.That(entry.LastClipVolume, Is.EqualTo(current == first ? 0.2f : 0.8f).Within(0.00001f));
                    previous = current;
                }

                entry.Clips = new[] { first, null };
                entry.ResetClipHistory();
                Assert.That(entry.NextClip(), Is.SameAs(first));
                Assert.That(entry.NextClip(), Is.SameAs(first), "A lone usable clip may repeat.");
                entry.Clips = new AudioClip[2];
                Assert.That(entry.NextClip(), Is.Null);
            }
            finally
            {
                Random.state = randomState;
            }
        }

        [TestCase(false)]
        [TestCase(true)]
        public void FailedPoolSelection_PreservesClipIdentityGainAndEvents(bool missingExplicitTrack)
        {
            AudioClip original = CreateClip("Original");
            MusicEntry current = new MusicEntry("Current", original) { Volume = 0.4f };
            current.SetClipVolume(0, 0.5f);
            MusicEntry invalid = missingExplicitTrack
                ? new MusicEntry("Invalid", CreateClip("Other"), null)
                : new MusicEntry("Invalid");
            _manager.SetMusicEntries(current, invalid);
            _manager.SetMusicVolume(0.8f);
            _manager.PlayMusicPool("Current", MusicTransition.Instant);
            int started = 0;
            _manager.OnMusicStarted += clip => started++;

            MusicOptions options = MusicOptions.Volume(1.8f).WithInstant();
            if (missingExplicitTrack)
            {
                options = options.WithTrack(1);
            }

            _manager.PlayMusicPool("Invalid", options);

            Assert.That(_manager.CurrentMusicEntry, Is.SameAs(current));
            Assert.That(_manager.CurrentMusicId, Is.EqualTo("Current"));
            Assert.That(_manager.GetCurrentMusicClip(), Is.SameAs(original));
            Assert.That(started, Is.Zero, "A rejected request must not announce the old clip as newly started.");
            Assert.That(_manager.Music.volume, Is.EqualTo(0.16f).Within(0.00001f));
            _manager.SetMusicVolume(0.6f);
            Assert.That(_manager.Music.volume, Is.EqualTo(0.12f).Within(0.00001f));
        }

        [Test]
        public void FailedNextTrack_DoesNotReplaceThePlayingVolumeOverride()
        {
            AudioClip original = CreateClip("Original");
            MusicEntry entry = new MusicEntry("Pool", original, null);
            entry.SetClipVolume(0, 0.5f);
            _manager.SetMusicEntries(entry);
            _manager.SetMusicVolume(0.8f);
            _manager.PlayMusicPool("Pool", MusicOptions.Track(0).WithVolume(0.4f).WithInstant());
            int changed = 0;
            _manager.OnRandomMusicTrackChanged += clip => changed++;

            bool result = _manager.TryNextMusicTrack(MusicOptions.Track(1).WithVolume(1.8f).WithInstant());
            _manager.SetMusicVolume(0.6f);

            Assert.That(result, Is.False);
            Assert.That(changed, Is.Zero);
            Assert.That(_manager.GetCurrentMusicClip(), Is.SameAs(original));
            Assert.That(_manager.Music.volume, Is.EqualTo(0.12f).Within(0.00001f));
        }

        [Test]
        public void SuccessfulNextTrack_RetainsThePoolVolumeOverride()
        {
            AudioClip first = CreateClip("First");
            AudioClip second = CreateClip("Second");
            _manager.SetMusicEntries(new MusicEntry("Pool", first, second) { Volume = 1f });
            _manager.SetMusicVolume(0.8f);
            _manager.PlayMusicPool("Pool", MusicOptions.Track(0).WithVolume(0.3f).WithInstant());

            bool changed = _manager.TryNextMusicTrack(MusicOptions.Track(1).WithInstant());

            Assert.That(changed, Is.True);
            Assert.That(_manager.GetCurrentMusicClip(), Is.SameAs(second));
            Assert.That(_manager.Music.volume, Is.EqualTo(0.24f).Within(0.00001f));
        }

        [Test]
        public void EmptyLegacyRandomPool_DoesNotClaimTheExistingTrack()
        {
            MusicEntry original = new MusicEntry("Original", CreateClip("Original"));
            _manager.SetMusicEntries(original);
            _manager.PlayMusicPool("Original", MusicTransition.Instant);
#pragma warning disable CS0618
            _manager.SetRandomMusicTracks(new AudioClip[] { null });
            _manager.EnableRandomMusic();
#pragma warning restore CS0618

            Assert.That(_manager.CurrentMusicEntry, Is.SameAs(original));
            Assert.That(_manager.IsRandomMusicEnabled(), Is.False);
        }

        [Test]
        public void SettingsInitialize_BindsSourcesCreatedByRuntimeInitialization()
        {
            SerializedObject managerData = new SerializedObject(_manager);
            managerData.FindProperty("_efx").objectReferenceValue = null;
            managerData.FindProperty("_music").objectReferenceValue = null;
            managerData.ApplyModifiedPropertiesWithoutUndo();
            Assert.That(_manager.Efx, Is.Null, "The public initialization contract must start with no effects source.");
            Assert.That(_manager.Music, Is.Null, "The public initialization contract must start with no music source.");

            AMSettings settings = CreateSettings();

            Assert.That(settings.efx, Is.SameAs(_manager.Efx).And.Not.Null);
            Assert.That(settings.music, Is.SameAs(_manager.Music).And.Not.Null);
            settings.SetEfxVolume(0.25f);
            settings.SetMusicVolume(0.35f);
            settings.SetEfx(false);
            settings.SetMusic(false);
            Assert.That(_manager.Efx.volume, Is.EqualTo(0.25f).Within(0.00001f));
            Assert.That(_manager.MusicVolume, Is.EqualTo(0.35f).Within(0.00001f));
            Assert.That(_manager.Efx.mute && _manager.Music.mute, Is.True);
        }

        [TestCase(SoundChannel.Sfx)]
        [TestCase(SoundChannel.Ui)]
        public void ToggleAllAudio_UnmutesWhenOnlyOneEffectsChannelWasMuted(SoundChannel muted)
        {
            AMSettings settings = CreateSettings();
            _manager.SetChannelMute(muted, true);
            Assert.That(_manager.IsChannelMuted(muted), Is.True, "The selected channel must really be muted before toggling.");
            Assert.That(_manager.IsChannelMuted(muted == SoundChannel.Sfx ? SoundChannel.Ui : SoundChannel.Sfx), Is.False);
            Assert.That(_manager.Music.mute, Is.False);

            settings.ToggleAllAudio();

            Assert.That(_manager.IsChannelMuted(SoundChannel.Sfx), Is.False);
            Assert.That(_manager.IsChannelMuted(SoundChannel.Ui), Is.False);
            Assert.That(_manager.Music.mute, Is.False);
            Assert.That(settings.MuteEfxValue || settings.MuteMusicValue, Is.False);
            settings.ToggleAllAudio();
            Assert.That(_manager.IsChannelMuted(SoundChannel.Sfx), Is.True);
            Assert.That(_manager.IsChannelMuted(SoundChannel.Ui), Is.True);
            Assert.That(_manager.Music.mute, Is.True);
        }

        [Test]
        public void NullStandaloneTrack_EndsPlaybackAndRaisesStoppedOnlyOnce()
        {
            RandomMusicController controller = new RandomMusicController();
            controller.Initialize(CreateObject("Standalone").AddComponent<AudioSource>(), new AudioClip[] { null });
            int stopped = 0;
            controller.OnStopped += () => stopped++;

            controller.Start();

            Assert.That(controller.IsPlaying || controller.IsPaused, Is.False);
            Assert.That(stopped, Is.EqualTo(1));
            controller.Stop();
            Assert.That(stopped, Is.EqualTo(1));
        }

        [Test]
        public void ReinitializeStandalone_StopsThePreviousSessionBeforeReplacingItsSource()
        {
            RandomMusicController controller = new RandomMusicController();
            AudioSource first = CreateObject("First source").AddComponent<AudioSource>();
            AudioSource second = CreateObject("Second source").AddComponent<AudioSource>();
            AudioClip secondClip = CreateClip("Second");
            controller.Initialize(first, new[] { CreateClip("First") });
            int stopped = 0;
            controller.OnStopped += () => stopped++;
            try
            {
                controller.Start();
                controller.Pause();
                controller.Initialize(second, new[] { secondClip });

                Assert.That(controller.IsPlaying || controller.IsPaused, Is.False);
                Assert.That(stopped, Is.EqualTo(1));
                Assert.That(controller.CurrentTrack, Is.Null);
                controller.Start();
                Assert.That(controller.CurrentTrack, Is.SameAs(secondClip));
                Assert.That(controller.IsPlaying, Is.True);
            }
            finally
            {
                controller.Stop();
            }

            Assert.That(stopped, Is.EqualTo(2));
        }

        [TestCase(SettingMixer.MixerParameterType.Music)]
        [TestCase(SettingMixer.MixerParameterType.Efx)]
        [TestCase(SettingMixer.MixerParameterType.Custom)]
        public void MixerMigration_PreservesAnExplicitNonDefaultTarget(SettingMixer.MixerParameterType target)
        {
            SettingMixer mixer = CreateObject("Mixer").AddComponent<SettingMixer>();
            using (SerializedObject data = new SerializedObject(mixer))
            {
                data.FindProperty("migratedLegacyParameter").boolValue = false;
                data.FindProperty("parameterType").enumValueIndex = (int)target;
                data.FindProperty("customParameterName").stringValue = "MasterVolume";
                // Apply a real authoring edit; Unity must deliver OnValidate without a manual lifecycle call.
                Assert.That(data.ApplyModifiedPropertiesWithoutUndo(), Is.True);
                data.Update();
                Assert.That(data.FindProperty("migratedLegacyParameter").boolValue, Is.True,
                    "Native authoring validation must run before target preservation is meaningful.");
            }

            Assert.That(mixer.parameterType, Is.EqualTo(target));
            Assert.That(mixer.customParameterName, Is.EqualTo("MasterVolume"));
        }

        [Test]
        public void MixerMigration_StillRecognizesTheLegacyMusicParameter()
        {
            SettingMixer mixer = CreateObject("Legacy mixer").AddComponent<SettingMixer>();
            Assert.That(mixer.parameterType, Is.EqualTo(SettingMixer.MixerParameterType.Master));
            using (SerializedObject data = new SerializedObject(mixer))
            {
                data.FindProperty("migratedLegacyParameter").boolValue = false;
                data.FindProperty("parameterType").enumValueIndex = (int)SettingMixer.MixerParameterType.Master;
                data.FindProperty("customParameterName").stringValue = "MusicVolume";
                Assert.That(data.ApplyModifiedPropertiesWithoutUndo(), Is.True);
                data.Update();
                Assert.That(data.FindProperty("migratedLegacyParameter").boolValue, Is.True,
                    "Native authoring validation must admit the legacy migration.");
            }

            Assert.That(mixer.parameterType, Is.EqualTo(SettingMixer.MixerParameterType.Music));
            Assert.That(mixer.customParameterName, Is.EqualTo("MusicVolume"));
        }

        [Test]
        public void Rebind_IgnoresStoppedCallbackRestartAndNestedInitialize()
        {
            AudioSource first = CreateObject("First source").AddComponent<AudioSource>();
            AudioSource second = CreateObject("Requested source").AddComponent<AudioSource>();
            AudioSource nested = CreateObject("Nested source").AddComponent<AudioSource>();
            AudioClip secondClip = CreateClip("Requested track");
            RandomMusicController controller = new RandomMusicController();
            controller.Initialize(first, new[] { CreateClip("First track") });
            int stopped = 0;
            controller.OnStopped += () =>
            {
                stopped++;
                if (stopped == 1)
                {
                    controller.Start();
                    controller.Initialize(nested, new[] { secondClip });
                }
            };

            try
            {
                controller.Start();
                controller.Initialize(second, new[] { secondClip });
                Assert.That(controller.IsPlaying || controller.IsPaused, Is.False);
                Assert.That(controller.CurrentTrack, Is.Null);
                Assert.That(stopped, Is.EqualTo(1));

                controller.Start();
                Assert.That(second.clip, Is.SameAs(secondClip));
                Assert.That(nested.clip, Is.Null, "Nested rebinding must not steal the accepted target.");
                Assert.That(controller.CurrentTrack, Is.SameAs(secondClip));
            }
            finally
            {
                controller.Stop();
            }
        }

        [Test]
        public void Rebind_ObserverFailureStillFinishesStoppedOnTheNewSource()
        {
            AudioSource first = CreateObject("First source").AddComponent<AudioSource>();
            AudioSource second = CreateObject("Second source").AddComponent<AudioSource>();
            AudioClip secondClip = CreateClip("Second track");
            RandomMusicController controller = new RandomMusicController();
            controller.Initialize(first, new[] { CreateClip("First track") });
            System.Action fail = () => throw new System.InvalidOperationException("Stopped observer");
            try
            {
                controller.Start();
                controller.OnStopped += fail;
                Assert.Throws<System.InvalidOperationException>(() => controller.Initialize(second, new[] { secondClip }));
                Assert.That(controller.IsPlaying || controller.IsPaused, Is.False);
                Assert.That(controller.CurrentTrack, Is.Null);
                controller.OnStopped -= fail;
                controller.Start();
                Assert.That(second.clip, Is.SameAs(secondClip));
            }
            finally
            {
                controller.OnStopped -= fail;
                controller.Stop();
            }
        }

        [Test]
        public void Restart_RespectsAReplacementStartedAndPausedByTheStoppedObserver()
        {
            RandomMusicController controller = new RandomMusicController();
            controller.Initialize(CreateObject("Source").AddComponent<AudioSource>(), new[] { CreateClip("Track") });
            bool restarted = false;
            controller.OnStopped += () =>
            {
                if (!restarted)
                {
                    restarted = true;
                    controller.Start();
                    controller.Pause();
                }
            };
            try
            {
                controller.Start();
                controller.Start();
                Assert.That(restarted, Is.True);
                Assert.That(controller.IsPaused, Is.True, "The stale outer Start must not replace or unpause the newer request.");
                Assert.That(controller.IsPlaying, Is.False);
            }
            finally
            {
                restarted = true;
                controller.Stop();
            }
        }

        [Test]
        public void ReleaseClips_ClearsOnlyMatchingPositionalAndAmbiencePlayback()
        {
            AudioClip owned = CreateClip("Owned transient clip");
            AudioClip shared = CreateClip("Shared clip");
            AudioSource ownedVoice = _manager.PlayAt(owned, Vector3.zero, 1f, 0f);
            AudioSource sharedVoice = _manager.PlayAt(shared, Vector3.one, 1f, 0f);
            _manager.PlayAmbience(owned, 0f);
            AudioSource ambience = _manager.Ambience;
            _manager.PlayMusicByClip(shared, MusicTransition.Instant);

            _manager.ReleaseClips(owned);
            _manager.ReleaseClips(owned);

            Assert.That(ownedVoice.clip, Is.Null);
            Assert.That(sharedVoice.clip, Is.SameAs(shared));
            Assert.That(_manager.GetCurrentMusicClip(), Is.SameAs(shared));
            Assert.That(_manager.Ambience, Is.SameAs(ambience).And.Not.Null, "Cleanup must retain the source itself.");
            Assert.That(ambience.clip, Is.Null);
            Assert.That(ambience.volume, Is.Zero);
            Assert.That(owned != null && shared != null, Is.True, "The caller owns clip destruction.");
        }

        [Test]
        public void ReleaseClips_DetachesOnlyRetiredLegacyRandomTrackReferences()
        {
            AudioClip owned = CreateClip("Owned random track");
            AudioClip shared = CreateClip("Shared current track");
            AudioClip[] retiredTracks = { owned };
            AudioClip[] mixedTracks = { owned, null, shared };
#pragma warning disable CS0618
            _manager.SetRandomMusicTracks(retiredTracks);
            _manager.EnableRandomMusic();
            _manager.PlayMusicByClip(shared, MusicTransition.Instant);
            _manager.ReleaseClips(owned);
            _manager.EnableRandomMusic();
            Assert.That(_manager.GetCurrentMusicClip(), Is.SameAs(shared));
            Assert.That(_manager.IsRandomMusicEnabled(), Is.False);
            Assert.That(retiredTracks[0], Is.SameAs(owned), "Release must not rewrite the caller's array.");

            _manager.SetRandomMusicTracks(mixedTracks);
            _manager.ReleaseClips(owned);
            _manager.EnableRandomMusic();
#pragma warning restore CS0618
            Assert.That(_manager.GetCurrentMusicClip(), Is.SameAs(shared));
            Assert.That(_manager.IsRandomMusicEnabled(), Is.True);
            Assert.That(_manager.CurrentMusicEntry.ClipAt(0), Is.Null);
            Assert.That(_manager.CurrentMusicEntry.ClipAt(2), Is.SameAs(shared), "Surviving legacy indices must stay stable.");
            Assert.That(mixedTracks[0], Is.SameAs(owned));
        }

        private AMSettings CreateSettings()
        {
            GameObject host = CreateObject("Settings");
            host.SetActive(false);
            AMSettings settings = host.AddComponent<AMSettings>();
            using (SerializedObject data = new SerializedObject(settings))
            {
                data.FindProperty("persistVolume").boolValue = false;
                data.ApplyModifiedPropertiesWithoutUndo();
                data.Update();
                Assert.That(data.FindProperty("persistVolume").boolValue, Is.False,
                    "Pure settings contract tests must not write to a save provider.");
            }

            host.SetActive(true);
            settings.Initialize(_manager);
            Assert.That(settings.efx, Is.SameAs(_manager.Efx).And.Not.Null);
            Assert.That(settings.music, Is.SameAs(_manager.Music).And.Not.Null);
            return settings;
        }

        private GameObject CreateObject(string name)
        {
            GameObject host = new GameObject(name);
            _owned.Add(host);
            return host;
        }

        private AudioClip CreateClip(string name)
        {
            AudioClip clip = AudioClip.Create(name, 4410, 1, 44100, false);
            _owned.Add(clip);
            return clip;
        }
    }
}
