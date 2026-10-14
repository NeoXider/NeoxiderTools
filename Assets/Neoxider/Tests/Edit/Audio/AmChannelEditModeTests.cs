using System.Collections.Generic;
using System.Reflection;
using Neo.Audio;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using UnityEngine.Audio;
using UnityEngine.TestTools.Constraints;
using Is = UnityEngine.TestTools.Constraints.Is;

namespace Neo.Editor.Tests
{
    /// <summary>
    ///     EditMode coverage for the 10.13.11 additions to <see cref="AM" />: per-entry sound channels (Sfx / Ui)
    ///     with their own volume, mute and mixer group, the quiet <c>TryPlay</c> path and the placeholder flag,
    ///     positional one-shots (<c>PlayAt</c>) on a pooled set of voices, and the defaults that old serialized
    ///     entries load with.
    ///     <para>
    ///         Audio does not actually play in EditMode, so routing is asserted on the pooled voices AM hands
    ///         each shot to: their volume, mute, mixer group, pitch, position and spatial blend.
    ///     </para>
    /// </summary>
    [TestFixture]
    public class AmChannelEditModeTests
    {
        private GameObject _go;
        private AM _am;
        private AudioSource _efx;
        private AudioSource _music;
        private readonly List<string> _warnings = new List<string>();

        [SetUp]
        public void SetUp()
        {
            _go = new GameObject("AmChannelEditModeTests");
            _am = _go.AddComponent<AM>();
            _efx = _go.AddComponent<AudioSource>();
            _music = _go.AddComponent<AudioSource>();

            SetPrivateField("_efx", _efx);
            SetPrivateField("_music", _music);

            _warnings.Clear();
            Application.logMessageReceived += CaptureLog;
        }

        [TearDown]
        public void TearDown()
        {
            Application.logMessageReceived -= CaptureLog;
            NeoDiagnostics.ResetStaticState();

            if (_go != null)
            {
                UnityEngine.Object.DestroyImmediate(_go);
            }
        }

        // ---------------------------------------------------------------- defaults and migration

        [Test]
        public void NewEntries_DefaultToTheSfxChannel_FullySpatial_AndNotPlaceholders()
        {
            SoundEntry sound = new SoundEntry("hit");

            Assert.AreEqual(SoundChannel.Sfx, sound.Channel, "Every entry must keep playing on the effects channel.");
            Assert.AreEqual(1f, sound.SpatialBlend, 1e-5f, "PlayAt of an entry is fully 3D unless told otherwise.");
            Assert.IsFalse(sound.IsPlaceholder);
            Assert.IsFalse(new MusicEntry("menu").IsPlaceholder);
            Assert.AreEqual(0, (int)SoundChannel.Sfx,
                "Sfx must stay the zero value: it is what every entry saved before channels existed loads as.");
        }

        [Test]
        public void FreshManager_ChannelsKeepTheExistingEffectsBehaviour()
        {
            _efx.volume = 0.7f;

            Assert.AreEqual(0.7f, _am.GetChannelVolume(SoundChannel.Sfx), 1e-5f,
                "The Sfx channel volume IS the Efx AudioSource volume.");
            Assert.AreEqual(_am.EfxVolume, _am.GetChannelVolume(SoundChannel.Sfx), 1e-5f);
            Assert.AreEqual(1f, _am.GetChannelVolume(SoundChannel.Ui), 1e-5f);
            Assert.IsNull(_am.GetChannelMixerGroup(SoundChannel.Ui), "No UI group means: route like Sfx.");
            Assert.IsFalse(_am.IsChannelMuted(SoundChannel.Ui));
        }

        [Test]
        public void OldSerializedEntries_LoadOnTheSfxChannelWithTheDocumentedDefaults()
        {
            // WHY JSON: JsonUtility runs the same serializer as scenes and prefabs, so an entry written before
            // 10.13.11 - no _channel, _spatialBlend or _placeholder fields - loads here exactly as it would from
            // an old scene.
            const string oldData =
                "{\"_soundEntries\":[{\"_id\":\"hit\",\"_clips\":[],\"_volume\":0.5,\"_clipVolumes\":[]," +
                "\"_randomizePitch\":false,\"_pitchMin\":0.9,\"_pitchMax\":1.1}]," +
                "\"_musicEntries\":[{\"_id\":\"menu\",\"_clips\":[],\"_volume\":1.0,\"_mode\":0," +
                "\"_fadeDuration\":-1.0}],\"_dataVersion\":1}";

            JsonUtility.FromJsonOverwrite(oldData, _am);

            Assert.AreEqual(1, _am.SoundEntries.Count);
            SoundEntry entry = _am.SoundEntries[0];
            Assert.AreEqual("hit", entry.Id);
            Assert.AreEqual(0.5f, entry.Volume, 1e-5f, "Existing fields must survive.");
            Assert.IsFalse(entry.RandomizePitch);
            Assert.AreEqual(SoundChannel.Sfx, entry.Channel, "An old entry must land on the effects channel.");
            Assert.AreEqual(1f, entry.SpatialBlend, 1e-5f, "An old entry played with PlayAt must be 3D.");
            Assert.IsFalse(entry.IsPlaceholder, "An old empty entry must still be reported, not silenced.");
            Assert.IsFalse(_am.MusicEntries[0].IsPlaceholder);
            Assert.AreEqual(1f, _am.GetChannelVolume(SoundChannel.Ui), 1e-5f,
                "A manager saved before channels existed starts its UI channel at full volume.");
        }

        [Test]
        public void LegacyMigration_PutsTheMigratedSoundsOnTheSfxChannel()
        {
            SetPrivateField("_sounds", new[] { new Sound { clip = CreateClip("legacy"), volume = 0.4f } });
            SetPrivateField("_dataVersion", 0);

            _am.OnAfterDeserialize();

            SoundEntry entry = _am.SoundEntries[0];
            Assert.AreEqual(SoundChannel.Sfx, entry.Channel);
            Assert.AreEqual(1f, entry.SpatialBlend, 1e-5f);
            Assert.IsFalse(entry.IsPlaceholder);
            Assert.AreEqual(0.4f, entry.Volume, 1e-5f);
        }

        // ---------------------------------------------------------------- channel volumes and routing

        [Test]
        public void SetChannelVolume_SfxIsTheEfxSource_AndUiIsIndependent()
        {
            _am.SetChannelVolume(SoundChannel.Sfx, 0.5f);
            _am.SetChannelVolume(SoundChannel.Ui, 0.5f);

            Assert.AreEqual(0.5f, _efx.volume, 1e-5f, "SetChannelVolume(Sfx) must write the Efx source.");
            Assert.AreEqual(0.5f, _am.EfxVolume, 1e-5f);
            Assert.AreEqual(0.5f, _am.UiVolume, 1e-5f);

            _am.SetChannelVolume(SoundChannel.Ui, 0.2f);
            Assert.AreEqual(0.5f, _am.GetChannelVolume(SoundChannel.Sfx), 1e-5f, "UI must not move the Sfx channel.");

            _am.SetEfxVolume(0.3f);
            Assert.AreEqual(0.3f, _am.GetChannelVolume(SoundChannel.Sfx), 1e-5f, "SetEfxVolume IS the Sfx channel.");
            Assert.AreEqual(0.2f, _am.GetChannelVolume(SoundChannel.Ui), 1e-5f, "Sfx must not move the UI channel.");

            _am.SetUiVolume(3f);
            Assert.AreEqual(1f, _am.UiVolume, 1e-5f, "Channel volumes clamp to 0..1 like the Efx source.");
            _am.UiVolume = -1f;
            Assert.AreEqual(0f, _am.UiVolume, 1e-5f);
        }

        [Test]
        public void PitchedPlays_RouteEachVoiceToItsEntryChannelVolume()
        {
            _am.SetSoundEntries(PitchedEntry("hit", SoundChannel.Sfx), PitchedEntry("click", SoundChannel.Ui));
            _am.SetChannelVolume(SoundChannel.Sfx, 0.8f);
            _am.SetChannelVolume(SoundChannel.Ui, 0.25f);

            _am.Play("click");
            AudioSource uiVoice = LastShotSource();
            Assert.AreEqual(0.25f, uiVoice.volume, 1e-5f, "A UI entry must play at the UI channel volume.");

            _am.Play("hit");
            AudioSource sfxVoice = LastShotSource();
            Assert.AreNotSame(uiVoice, sfxVoice);
            Assert.AreEqual(0.8f, sfxVoice.volume, 1e-5f, "An Sfx entry must play at the Efx volume.");
        }

        [Test]
        public void PitchedPlays_RouteEachVoiceToItsChannelMixerGroup()
        {
            AudioMixerGroup sound = FindMixerGroup("Sound");
            AudioMixerGroup music = FindMixerGroup("Music");
            Assume.That(sound != null && music != null, "The package mixer (Audio/Mixer.mixer) is not in this project.");

            _am.SetSoundEntries(PitchedEntry("hit", SoundChannel.Sfx), PitchedEntry("click", SoundChannel.Ui));
            _am.SetChannelMixerGroup(SoundChannel.Sfx, sound);
            _am.SetChannelMixerGroup(SoundChannel.Ui, music);

            Assert.AreSame(sound, _efx.outputAudioMixerGroup, "The Sfx group IS the Efx source's output.");
            Assert.AreSame(music, _am.GetChannelMixerGroup(SoundChannel.Ui));

            _am.Play("click");
            Assert.AreSame(music, LastShotSource().outputAudioMixerGroup);
            _am.Play("hit");
            Assert.AreSame(sound, LastShotSource().outputAudioMixerGroup);

            _am.SetChannelMixerGroup(SoundChannel.Ui, null);
            _am.Play("click");
            Assert.AreSame(sound, LastShotSource().outputAudioMixerGroup,
                "A UI channel without its own group must route through the Sfx group.");
            Assert.IsNull(_am.GetChannelMixerGroup(SoundChannel.Ui), "The getter reports what was configured.");
        }

        [Test]
        public void ChannelMute_ReachesOnlyThatChannelsVoices()
        {
            _am.SetSoundEntries(PitchedEntry("hit", SoundChannel.Sfx), PitchedEntry("click", SoundChannel.Ui));

            _am.SetChannelMute(SoundChannel.Ui, true);

            Assert.IsTrue(_am.IsChannelMuted(SoundChannel.Ui));
            Assert.IsFalse(_am.IsChannelMuted(SoundChannel.Sfx));
            _am.Play("click");
            Assert.IsTrue(LastShotSource().mute);
            _am.Play("hit");
            Assert.IsFalse(LastShotSource().mute);

            _am.SetChannelMute(SoundChannel.Sfx, true);
            Assert.IsTrue(_efx.mute, "Sfx mute IS the Efx source mute (the flag AMSettings flips).");
        }

        [Test]
        public void SetChannelVolume_RetunesVoicesAlreadyRingingOnThatChannelOnly()
        {
            _am.SetSoundEntries(PitchedEntry("hit", SoundChannel.Sfx), PitchedEntry("click", SoundChannel.Ui));
            _am.SetChannelVolume(SoundChannel.Sfx, 0.9f);
            _am.Play("click");
            AudioSource uiVoice = LastShotSource();
            _am.Play("hit");
            AudioSource sfxVoice = LastShotSource();

            _am.SetChannelVolume(SoundChannel.Ui, 0.1f);

            Assert.AreEqual(0.1f, uiVoice.volume, 1e-5f, "A settings slider must move sounds already ringing.");
            Assert.AreEqual(0.9f, sfxVoice.volume, 1e-5f, "The other channel's voices must not move.");
        }

        [Test]
        public void UnpitchedUiPlay_UsesItsOwnSource_AndSfxOnlyProjectsNeverCreateIt()
        {
            SoundEntry hit = new SoundEntry("hit", CreateClip("hit")) { RandomizePitch = false };
            SoundEntry click = new SoundEntry("click", CreateClip("click"))
            {
                RandomizePitch = false,
                Channel = SoundChannel.Ui
            };
            _am.SetSoundEntries(hit, click);
            _am.SetChannelVolume(SoundChannel.Ui, 0.4f);

            _am.Play("hit");
            Assert.IsNull(GetPrivateField("_uiSource"), "A project that never uses UI must not get an extra source.");
            Assert.AreSame(_efx, LastShotSource(), "An unpitched Sfx one-shot plays on the Efx source.");

            _am.Play("click");
            AudioSource ui = (AudioSource)GetPrivateField("_uiSource");
            Assert.IsNotNull(ui, "The UI channel source is created on first use.");
            Assert.AreNotSame(_efx, ui);
            Assert.AreSame(ui, LastShotSource(), "An unpitched UI one-shot must play on the UI source, not on Efx.");
            Assert.AreSame(ui, _am.GetChannelSource(SoundChannel.Ui));
            Assert.AreSame(_efx, _am.GetChannelSource(SoundChannel.Sfx));
            Assert.AreEqual(0.4f, ui.volume, 1e-5f);
            Assert.AreEqual(0f, ui.spatialBlend, 1e-5f, "Interface sounds are 2D.");
            Assert.AreEqual(HideFlags.DontSave, ui.gameObject.hideFlags & HideFlags.DontSave,
                "The runtime UI source must never be saved into the scene or prefab.");
        }

        [Test]
        public void ChannelOverride_RoutesOnePlayWithoutChangingTheEntry()
        {
            SoundEntry hit = PitchedEntry("hit", SoundChannel.Sfx);
            _am.SetSoundEntries(hit);
            _am.SetChannelVolume(SoundChannel.Sfx, 0.9f);
            _am.SetChannelVolume(SoundChannel.Ui, 0.3f);

            _am.Play("hit", SoundOptions.Channel(SoundChannel.Ui));

            Assert.AreEqual(0.3f, LastShotSource().volume, 1e-5f);
            Assert.AreEqual(SoundChannel.Sfx, hit.Channel, "An override is for one call only.");
        }

        [Test]
        public void UnpitchedUiPlay_IsHeardOnTheUiSource_NotOnTheEfxSource()
        {
            GameObject probeObject = new GameObject("AmChannelEditModeTests.Probe");
            try
            {
                AudioSource probe = probeObject.AddComponent<AudioSource>();
                probe.PlayOneShot(AudioClip.Create("probe", 44100, 1, 44100, false), 1f);
                Assume.That(probe.isPlaying, "This editor does not report a one-shot through isPlaying.");
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(probeObject);
            }

            _am.SetSoundEntries(new SoundEntry("click", AudioClip.Create("click", 44100, 1, 44100, false))
            {
                RandomizePitch = false,
                Channel = SoundChannel.Ui
            });

            _am.Play("click");

            Assert.IsTrue(_am.GetChannelSource(SoundChannel.Ui).isPlaying, "The UI source must carry the click.");
            Assert.IsFalse(_efx.isPlaying, "The Efx source must stay silent for a UI click.");
        }

        [Test]
        public void PitchVoices_AreNeverSharedAcrossChannels_SoARingingTailKeepsItsLevel()
        {
            _am.SetSoundEntries(PitchedEntry("boom", SoundChannel.Sfx), PitchedEntry("click", SoundChannel.Ui));
            _am.SetChannelVolume(SoundChannel.Sfx, 0.2f);
            _am.SetChannelVolume(SoundChannel.Ui, 1f);

            _am.Play("boom");
            AudioSource sfxTail = LastShotSource();
            HashSet<AudioSource> uiVoices = new HashSet<AudioSource>();
            for (int index = 0; index < 40; index++)
            {
                _am.Play("click");
                uiVoices.Add(LastShotSource());
            }

            _am.SetChannelMute(SoundChannel.Ui, true);
            _am.SetChannelVolume(SoundChannel.Ui, 0.9f);

            Assert.IsFalse(uiVoices.Contains(sfxTail),
                "A UI click must never take over a voice that may still be ringing an Sfx tail.");
            Assert.AreEqual(0.2f, sfxTail.volume, 1e-5f,
                "The Sfx tail must keep the Sfx level instead of jumping to the UI level (5x louder here).");
            Assert.IsFalse(sfxTail.mute, "Muting UI must not reach an Sfx tail.");

            AudioSource uiTail = LastShotSource();
            for (int index = 0; index < 40; index++)
            {
                _am.Play("boom");
                Assert.AreNotSame(uiTail, LastShotSource(), "An Sfx shot must never take over a UI voice.");
            }

            Assert.IsTrue(uiTail.mute);
            Assert.AreEqual(0.9f, uiTail.volume, 1e-5f);
        }

        [Test]
        public void OutOfRangeChannelValue_PlaysAndIsTrackedAsSfx()
        {
            SoundEntry odd = new SoundEntry("odd", CreateClip("odd")) { Channel = (SoundChannel)7 };
            _am.SetSoundEntries(odd);
            _am.SetChannelVolume(SoundChannel.Sfx, 0.5f);

            _am.Play("odd");
            AudioSource voice = LastShotSource();
            AudioSource positional = _am.PlayAt("odd", Vector3.zero);

            Assert.GreaterOrEqual(System.Array.IndexOf((AudioSource[])GetPrivateField("_pitchSources"), voice), 0,
                "An unknown channel value must play on the Sfx rotation.");
            Assert.AreEqual(0.5f, voice.volume, 1e-5f);
            Assert.AreEqual(0.5f, positional.volume, 1e-5f);
            Assert.IsNull(GetPrivateField("_uiSource"), "An unknown channel value must not be treated as UI.");

            _am.SetChannelVolume(SoundChannel.Sfx, 0.25f);
            Assert.AreEqual(0.25f, voice.volume, 1e-5f);
            Assert.AreEqual(0.25f, positional.volume, 1e-5f,
                "The voice is stored under the normalised channel, so the Sfx refresh must find it.");
        }

        [Test]
        public void OnDestroy_DestroysTheHelperSourcesAmCreated_ButNotTheAuthoredOnes()
        {
            _am.SetSoundEntries(PitchedEntry("hit", SoundChannel.Sfx), PitchedEntry("click", SoundChannel.Ui));
            _am.Play("hit");
            AudioSource sfxVoice = LastShotSource();
            _am.Play("click");
            AudioSource uiVoice = LastShotSource();
            AudioSource positional = _am.PlayAt("hit", Vector3.zero);
            AudioSource ui = _am.GetChannelSource(SoundChannel.Ui);

            // WHY reflection: EditMode does not send OnDestroy to a component that is not [ExecuteAlways].
            MethodInfo onDestroy = typeof(AM).GetMethod("OnDestroy", BindingFlags.NonPublic | BindingFlags.Instance);
            Assert.IsNotNull(onDestroy);
            onDestroy.Invoke(_am, null);

            Assert.IsTrue(sfxVoice == null, "Sfx pitch voices must not outlive AM.");
            Assert.IsTrue(uiVoice == null, "UI pitch voices must not outlive AM.");
            Assert.IsTrue(positional == null, "Positional voices must not outlive AM.");
            Assert.IsTrue(ui == null, "The UI channel source must not outlive AM.");
            Assert.IsTrue(_efx != null && _music != null, "The authored Efx and Music sources are not AM's to destroy.");
        }

        // ---------------------------------------------------------------- AMSettings drives the channels

        [Test]
        public void AmSettingsEfxVolume_ReachesPitchedVoicesThatAreStillRinging()
        {
            AMSettings settings = CreateSettings();
            _am.SetSoundEntries(PitchedEntry("hit", SoundChannel.Sfx));
            _am.Play("hit");
            AudioSource voice = LastShotSource();

            settings.SetEfxVolume(0.3f);

            Assert.AreEqual(0.3f, _efx.volume, 1e-5f);
            Assert.AreEqual(0.3f, _am.EfxVolume, 1e-5f);
            Assert.AreEqual(0.3f, voice.volume, 1e-5f, "The effects slider must move a pitched voice still ringing.");
            Assert.AreEqual(0.3f, settings.GetEfxVolumeNormalized(), 1e-5f);
        }

        [Test]
        public void AmSettingsSetEfx_MutesRingingSfxVoices_AndLeavesTheUiChannelAlone()
        {
            AMSettings settings = CreateSettings();
            _am.SetSoundEntries(PitchedEntry("hit", SoundChannel.Sfx), PitchedEntry("click", SoundChannel.Ui));
            _am.Play("hit");
            AudioSource sfxVoice = LastShotSource();
            _am.Play("click");
            AudioSource uiVoice = LastShotSource();

            settings.SetEfx(false);

            Assert.IsTrue(_efx.mute);
            Assert.IsTrue(sfxVoice.mute, "Turning effects off must silence a pitched voice still ringing.");
            Assert.IsTrue(settings.MuteEfxValue);
            Assert.IsFalse(uiVoice.mute, "The effects toggle is the Sfx channel only.");
            Assert.IsFalse(_am.IsChannelMuted(SoundChannel.Ui));

            settings.SetEfx(true);

            Assert.IsFalse(_efx.mute);
            Assert.IsFalse(sfxVoice.mute);
            Assert.IsFalse(settings.MuteEfxValue);
        }

        [Test]
        public void AmSettingsToggleAllAudio_AlsoSilencesAndRestoresTheUiChannel()
        {
            AMSettings settings = CreateSettings();
            _am.SetSoundEntries(PitchedEntry("click", SoundChannel.Ui));
            _am.Play("click");
            AudioSource uiVoice = LastShotSource();

            settings.ToggleAllAudio();

            Assert.IsTrue(_music.mute);
            Assert.IsTrue(_efx.mute);
            Assert.IsTrue(_am.IsChannelMuted(SoundChannel.Ui), "All audio off must include the UI channel (no mixer).");
            Assert.IsTrue(uiVoice.mute);

            settings.ToggleAllAudio();

            Assert.IsFalse(_music.mute);
            Assert.IsFalse(_efx.mute);
            Assert.IsFalse(_am.IsChannelMuted(SoundChannel.Ui));
            Assert.IsFalse(uiVoice.mute);
        }

        [Test]
        public void AmSettingsMuteState_IsReadFromAm_NotFromAStaleCachedSource()
        {
            AMSettings settings = CreateSettings();
            // WHY: AMSettings caches AM's effects source once, in Start. A source cached before AM's was replaced
            // must not decide what the reactive mute state reports.
            AudioSource stale = _go.AddComponent<AudioSource>();
            typeof(AMSettings).GetProperty("efx", BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.Instance)
                .SetValue(settings, stale);

            settings.SetEfx(false);

            Assert.IsTrue(_am.IsChannelMuted(SoundChannel.Sfx), "The mute goes to AM's Sfx channel.");
            Assert.IsTrue(settings.MuteEfxValue, "MuteEfx must report AM's state, not the stale cached source.");
            Assert.IsFalse(stale.mute);

            settings.SetEfx(true);
            Assert.IsFalse(settings.MuteEfxValue);

            settings.ToggleAllAudio();
            Assert.IsTrue(_efx.mute);
            Assert.IsTrue(settings.MuteEfxValue, "ToggleAllAudio must report AM's state too.");
        }

        // ---------------------------------------------------------------- TryPlay and placeholders

        [Test]
        public void TryPlay_MissingOrEmptyId_ReturnsFalseWithoutAWarning()
        {
            _am.SetSoundEntries(new SoundEntry("hit", CreateClip("hit")));
            NeoDiagnostics.Configure(warnings: true);

            Assert.IsFalse(_am.TryPlay("nope"));
            Assert.IsFalse(_am.TryPlay(string.Empty));
            Assert.IsFalse(_am.TryPlay((string)null));
            Assert.IsFalse(_am.TryPlay("nope", 0.5f));
            Assert.IsFalse(_am.TryPlay("nope", SoundOptions.NoPitch));
            Assert.AreEqual(0, AmWarningCount(), "TryPlay must never log.");

            // Control: the same miss through Play does warn, so the silence above is TryPlay's doing.
            _am.Play("nope");
            Assert.AreEqual(1, AmWarningCount());
        }

        [Test]
        public void TryPlay_EntryWithoutAClip_ReturnsFalseWithoutAWarning()
        {
            _am.SetSoundEntries(new SoundEntry("todo"), new SoundEntry("nulls", null, null));
            NeoDiagnostics.Configure(warnings: true);

            Assert.IsFalse(_am.TryPlay("todo"));
            Assert.IsFalse(_am.TryPlay("nulls"));
            Assert.AreEqual(0, AmWarningCount());
        }

        [Test]
        public void TryPlay_PlayableEntry_ReturnsTrue()
        {
            _am.SetSoundEntries(new SoundEntry("hit", CreateClip("hit")),
                new SoundEntry("click", CreateClip("click")) { Channel = SoundChannel.Ui, RandomizePitch = false });

            Assert.IsTrue(_am.TryPlay("hit"));
            Assert.IsTrue(_am.TryPlay("click", 0.5f));
        }

        [Test]
        public void PlaceholderEntry_IsSilentOnEveryPlayPath()
        {
            SoundEntry todo = new SoundEntry("todo") { IsPlaceholder = true };
            _am.SetSoundEntries(todo, new SoundEntry("broken"));
            NeoDiagnostics.Configure(warnings: true);

            _am.Play("todo");
            _am.Play(0);
            _am.Play("todo", 0.5f);
            Assert.IsNull(_am.PlayAt("todo", Vector3.one));
            Assert.IsFalse(_am.TryPlay("todo"));
            Assert.AreEqual(0, AmWarningCount(), "A placeholder is empty on purpose and must not warn.");

            // Control: an empty entry that is NOT a placeholder still reports itself, by id and by index.
            _am.Play("broken");
            _am.Play(1);
            Assert.AreEqual(2, AmWarningCount());
        }

        [Test]
        public void PlaceholderEntry_WithAClip_PlaysNormally()
        {
            _am.SetSoundEntries(new SoundEntry("todo", CreateClip("delivered")) { IsPlaceholder = true });

            Assert.IsTrue(_am.TryPlay("todo"), "Once the sound is delivered the flag no longer matters.");
            Assert.IsNotNull(_am.PlayAt("todo", Vector3.zero));
        }

        [Test]
        public void PlaceholderMusicPool_KeepsTheCurrentMusicAndStaysSilent()
        {
            AudioClip menu = CreateClip("menu");
            _am.SetMusicEntries(new MusicEntry("menu", menu), new MusicEntry("boss") { IsPlaceholder = true });
            _am.PlayMusicPool("menu");
            NeoDiagnostics.Configure(warnings: true);

            _am.PlayMusicPool("boss");
            _am.PlayMusicPool(1);

            Assert.AreEqual(0, AmWarningCount());
            Assert.AreEqual("menu", _am.CurrentMusicId, "Asking for a pool that has no music yet must not drop the current one.");
            Assert.AreSame(menu, _music.clip);
        }

        // ---------------------------------------------------------------- PlayAt

        [Test]
        public void PlayAt_PlacesAPooledVoiceAtThePointWithTheEntrySpatialBlend()
        {
            AudioClip boom = CreateClip("boom");
            _am.SetSoundEntries(new SoundEntry("boom", boom) { RandomizePitch = false, SpatialBlend = 0.75f });
            Vector3 point = new Vector3(3f, 4f, 5f);

            AudioSource voice = _am.PlayAt("boom", point);

            Assert.IsNotNull(voice);
            Assert.AreNotSame(_efx, voice, "A positional sound must not move the shared effects source.");
            Assert.AreSame(boom, voice.clip);
            Assert.AreEqual(0f, Vector3.Distance(point, voice.transform.position), 1e-4f);
            Assert.AreEqual(0.75f, voice.spatialBlend, 1e-5f);
            Assert.AreEqual(1f, voice.pitch, 1e-5f, "No randomisation means the clip's own pitch.");
            Assert.IsFalse(voice.loop);
            Assert.AreEqual(HideFlags.DontSave, voice.gameObject.hideFlags & HideFlags.DontSave);
        }

        [Test]
        public void PlayAt_SpatialOverrideAndClipOverload()
        {
            SoundEntry boom = new SoundEntry("boom", CreateClip("boom")) { RandomizePitch = false };
            _am.SetSoundEntries(boom);

            AudioSource voice = _am.PlayAt("boom", Vector3.zero, SoundOptions.Spatial(0.25f));
            Assert.AreEqual(0.25f, voice.spatialBlend, 1e-5f);
            Assert.AreEqual(1f, boom.SpatialBlend, 1e-5f, "An override is for one call only.");

            AudioClip direct = CreateClip("direct");
            AudioSource clipVoice = _am.PlayAt(direct, Vector3.right, 0.5f, 0.6f, SoundChannel.Ui);
            Assert.AreSame(direct, clipVoice.clip);
            Assert.AreEqual(0.6f, clipVoice.spatialBlend, 1e-5f);
            Assert.AreEqual(0.5f, clipVoice.volume, 1e-5f, "UI channel 1 x volume 0.5.");
        }

        [Test]
        public void PlayAt_VolumeIsChannelTimesEntry_AndFollowsTheChannel()
        {
            _am.SetSoundEntries(
                new SoundEntry("boom", CreateClip("boom")) { RandomizePitch = false, Volume = 0.8f },
                new SoundEntry("ping", CreateClip("ping")) { RandomizePitch = false, Channel = SoundChannel.Ui },
                new SoundEntry("quiet", CreateClip("quiet")) { RandomizePitch = false, Volume = 2f });
            _am.SetChannelVolume(SoundChannel.Sfx, 0.5f);
            _am.SetChannelVolume(SoundChannel.Ui, 0.25f);

            AudioSource boom = _am.PlayAt("boom", Vector3.zero);
            AudioSource ping = _am.PlayAt("ping", Vector3.zero);
            AudioSource quiet = _am.PlayAt("quiet", Vector3.zero);

            Assert.AreEqual(0.4f, boom.volume, 1e-5f, "Sfx 0.5 x entry 0.8.");
            Assert.AreEqual(0.25f, ping.volume, 1e-5f, "A UI entry plays at the UI channel volume.");
            Assert.AreEqual(1f, quiet.volume, 1e-5f, "The 0..2 entry headroom lifts a quiet clip: 0.5 x 2.");

            _am.SetChannelVolume(SoundChannel.Sfx, 1f);
            Assert.AreEqual(0.8f, boom.volume, 1e-5f, "A ringing positional sound follows its channel.");
            Assert.AreEqual(0.25f, ping.volume, 1e-5f, "...and only its channel.");

            _am.SetChannelMute(SoundChannel.Ui, true);
            Assert.IsTrue(ping.mute);
            Assert.IsFalse(boom.mute);
        }

        [Test]
        public void PlayAt_TakesThe3DSettingsFromTheEfxSourceOnEveryChannel_WithoutDoppler()
        {
            _efx.rolloffMode = AudioRolloffMode.Linear;
            _efx.minDistance = 2f;
            _efx.maxDistance = 40f;
            _efx.spread = 90f;
            _efx.dopplerLevel = 1f;
            _am.SetSoundEntries(new SoundEntry("boom", CreateClip("boom")),
                new SoundEntry("ping", CreateClip("ping")) { Channel = SoundChannel.Ui });

            AudioSource[] voices = { _am.PlayAt("boom", Vector3.zero), _am.PlayAt("ping", Vector3.one) };

            for (int index = 0; index < voices.Length; index++)
            {
                AudioSource voice = voices[index];
                Assert.AreEqual(AudioRolloffMode.Linear, voice.rolloffMode);
                Assert.AreEqual(2f, voice.minDistance, 1e-5f);
                Assert.AreEqual(40f, voice.maxDistance, 1e-5f);
                Assert.AreEqual(90f, voice.spread, 1e-4f);
                Assert.AreEqual(0f, voice.dopplerLevel, 1e-5f,
                    "A voice teleported and started in one frame must not get a doppler blip.");
            }
        }

        [Test]
        public void PlayAt_ReusesAFixedPoolOfVoices()
        {
            SetPrivateField("_positionalVoices", 3);
            _am.SetSoundEntries(new SoundEntry("boom", CreateClip("boom")));

            HashSet<AudioSource> voices = new HashSet<AudioSource>();
            AudioSource first = null;
            for (int index = 0; index < 9; index++)
            {
                AudioSource voice = _am.PlayAt("boom", new Vector3(index, 0f, 0f));
                Assert.IsNotNull(voice);
                first ??= voice;
                voices.Add(voice);
                Assert.AreEqual((float)index, voice.transform.position.x, 1e-4f, "Each sound goes to its own point.");
            }

            Assert.AreEqual(3, voices.Count, "Nine positional sounds must share the three pooled voices.");
            Assert.IsTrue(voices.Contains(first));
        }

        [Test]
        public void PlayAt_DoesNotAllocateOnceThePoolIsWarm()
        {
            _am.SetSoundEntries(new SoundEntry("boom", CreateClip("boom")), PitchedEntry("hit", SoundChannel.Ui));
            Vector3 point = new Vector3(1f, 2f, 3f);
            TestDelegate burst = () =>
            {
                for (int index = 0; index < 20; index++)
                {
                    _am.PlayAt("boom", point);
                    _am.Play("hit");
                }
            };

            // WHY the same delegate runs once before the measurement: twenty shots fill both voice pools (the
            // one-time GameObject creation), and the first call of a freshly compiled lambda allocates once on
            // its own (its string literals), which is not a per-call cost of AM.
            burst();

            Assert.That(burst, Is.Not.AllocatingGCMemory());
        }

        // ---------------------------------------------------------------- pitch

        [Test]
        public void PitchRandomisation_StaysInsideTheEntryRange_ForPlayAndPlayAt()
        {
            SoundEntry hit = new SoundEntry("hit", CreateClip("a"), CreateClip("b"));
            hit.SetPitchRange(0.8f, 0.9f);
            _am.SetSoundEntries(hit);

            for (int index = 0; index < 100; index++)
            {
                _am.Play("hit");
                float pitch = LastShotSource().pitch;
                Assert.That(pitch, Is.InRange(0.8f, 0.9f), "Play must detune inside the entry range.");

                float positional = _am.PlayAt("hit", Vector3.zero).pitch;
                Assert.That(positional, Is.InRange(0.8f, 0.9f), "PlayAt must detune inside the entry range.");

                float overridden = _am.PlayAt("hit", Vector3.zero, SoundOptions.Pitch(1.2f, 1.3f)).pitch;
                Assert.That(overridden, Is.InRange(1.2f, 1.3f), "A per-call range replaces the entry's.");
            }
        }

        // ---------------------------------------------------------------- helpers

        private static SoundEntry PitchedEntry(string id, SoundChannel channel)
        {
            return new SoundEntry(id, CreateClip(id)) { RandomizePitch = true, Channel = channel };
        }

        /// <summary>The source that took the most recent one-shot: a channel source or a pitch voice.</summary>
        private AudioSource LastShotSource()
        {
            AudioSource source = (AudioSource)GetPrivateField("_lastOneShotSource");
            Assert.IsTrue(source != null, "A one-shot must have been played.");
            return source;
        }

        private AMSettings CreateSettings()
        {
            // WHY: AMSettings.Start (which wires AM and the sources) does not run in EditMode, so the same wiring is
            // injected here; persistence is off so the test never writes save keys.
            AMSettings settings = _go.AddComponent<AMSettings>();
            const BindingFlags flags = BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.Instance;
            typeof(AMSettings).GetField("_am", flags).SetValue(settings, _am);
            typeof(AMSettings).GetField("persistVolume", flags).SetValue(settings, false);
            typeof(AMSettings).GetProperty("music", flags).SetValue(settings, _music);
            typeof(AMSettings).GetProperty("efx", flags).SetValue(settings, _efx);
            return settings;
        }

        private int AmWarningCount()
        {
            int count = 0;
            for (int index = 0; index < _warnings.Count; index++)
            {
                if (_warnings[index].StartsWith("[AM]", System.StringComparison.Ordinal))
                {
                    count++;
                }
            }

            return count;
        }

        private void CaptureLog(string condition, string stackTrace, LogType type)
        {
            if (type == LogType.Warning)
            {
                _warnings.Add(condition);
            }
        }

        private static AudioMixerGroup FindMixerGroup(string name)
        {
            string[] paths =
            {
                "Assets/Neoxider/Audio/Mixer.mixer",
                "Packages/com.neoxider.tools/Audio/Mixer.mixer"
            };

            for (int index = 0; index < paths.Length; index++)
            {
                AudioMixer mixer = AssetDatabase.LoadAssetAtPath<AudioMixer>(paths[index]);
                if (mixer == null)
                {
                    continue;
                }

                AudioMixerGroup[] groups = mixer.FindMatchingGroups(name);
                if (groups != null && groups.Length > 0)
                {
                    return groups[0];
                }
            }

            return null;
        }

        private static AudioClip CreateClip(string name)
        {
            return AudioClip.Create(name, 4410, 1, 44100, false);
        }

        private object GetPrivateField(string fieldName)
        {
            FieldInfo field = typeof(AM).GetField(fieldName, BindingFlags.NonPublic | BindingFlags.Instance);
            Assert.IsNotNull(field, $"AM no longer has a field named '{fieldName}'.");
            return field.GetValue(_am);
        }

        private void SetPrivateField(string fieldName, object value)
        {
            FieldInfo field = typeof(AM).GetField(fieldName, BindingFlags.NonPublic | BindingFlags.Instance);
            Assert.IsNotNull(field, $"AM no longer has a field named '{fieldName}'.");
            field.SetValue(_am, value);
        }
    }
}
