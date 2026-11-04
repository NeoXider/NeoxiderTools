using System;
using System.Collections;
using System.Collections.Generic;
using Neo.Audio;
using Neo.Save;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using UnityEngine.Audio;
using UnityEngine.TestTools;
using Object = UnityEngine.Object;

namespace Neo.Editor.Tests
{
    /// <summary>
    ///     AMSettings mute contract: binding a manager never forces the channels back on, a mute requested before
    ///     binding survives it, and the mute state persists through <see cref="SaveProvider" /> between sessions.
    /// </summary>
    public sealed class AmSettingsMutePersistenceTests
    {
        private const string MixerPath = "Assets/Neoxider/Audio/Mixer.mixer";
        private const string PackageMixerPath = "Packages/com.neoxider.tools/Audio/Mixer.mixer";

        private readonly List<Object> _owned = new List<Object>();
        private string _keyPrefix;

        private string MuteKey => _keyPrefix + ".Mute";

        [SetUp]
        public void SetUp()
        {
            _keyPrefix = "AmSettingsMuteTests-" + Guid.NewGuid().ToString("N");
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
        }

        [UnityTearDown]
        public IEnumerator LeavePlayModeAfterFailure()
        {
            if (Application.isPlaying)
            {
                yield return new ExitPlayMode();
            }
        }

        [Test]
        public void Initialize_AppliesAMuteRequestedBeforeBinding()
        {
            AM manager = CreateManager("Late bound manager");
            AMSettings settings = CreateSettings(false);

            settings.SetMusic(false);
            settings.SetEfx(false);
            Assert.That(settings.MuteMusicValue && settings.MuteEfxValue, Is.True,
                "A request made before binding must be visible to UI at once.");

            settings.Initialize(manager);

            Assert.That(manager.Music.mute, Is.True, "Binding must apply the earlier request, not force music on.");
            Assert.That(manager.IsChannelMuted(SoundChannel.Sfx), Is.True);
            Assert.That(settings.MuteMusicValue && settings.MuteEfxValue, Is.True);
        }

        [Test]
        public void Initialize_KeepsAMuteAlreadyOnTheManagerSources()
        {
            AM manager = CreateManager("Authored mute");
            manager.Music.mute = true;
            AMSettings settings = CreateSettings(false);

            settings.Initialize(manager);

            Assert.That(manager.Music.mute, Is.True, "Startup must not unmute a source muted before binding.");
            Assert.That(settings.MuteMusicValue, Is.True);
            Assert.That(manager.IsChannelMuted(SoundChannel.Sfx), Is.False);
            Assert.That(settings.MuteEfxValue, Is.False);
        }

        [Test]
        public void ToggleMusic_BeforeBinding_FlipsThePendingRequest()
        {
            AM manager = CreateManager("Toggled before binding");
            AMSettings settings = CreateSettings(false);

            settings.ToggleMusic();
            Assert.That(settings.MuteMusicValue, Is.True);
            settings.Initialize(manager);

            Assert.That(manager.Music.mute, Is.True);
        }

        [Test]
        public void ReplacementManager_InheritsTheCurrentMute()
        {
            AM first = CreateManager("First manager");
            AMSettings settings = CreateSettings(false);
            settings.Initialize(first);
            settings.SetMusic(false);

            AM second = CreateManager("Replacement manager");
            settings.Initialize(second);

            Assert.That(settings.music, Is.SameAs(second.Music));
            Assert.That(second.Music.mute, Is.True, "A replacement manager must not bring the music back on.");
            Assert.That(second.IsChannelMuted(SoundChannel.Sfx), Is.False);
        }

        [Test]
        public void ReplacementManager_KeepsTheUiChannelMuteAndItsSavedBit()
        {
            using (Audit20261023NeoEditorTestsSaveScope saveScope = new Audit20261023NeoEditorTestsSaveScope())
            {
                AM first = CreateManager("All off, first manager");
                AMSettings settings = CreateSettings(true);
                settings.Initialize(first);
                settings.ToggleAllAudio();
                Assert.That(SaveProvider.GetInt(MuteKey, -1), Is.EqualTo(1 | 2 | 8));

                // WHY destroyed first: the replacement must not depend on reading the previous manager.
                Object.DestroyImmediate(first.gameObject);
                AM second = CreateManager("All off, replacement manager");
                settings.Initialize(second);

                Assert.That(second.IsChannelMuted(SoundChannel.Ui), Is.True,
                    "A replacement manager must not bring interface clicks back with all audio off.");
                Assert.That(second.Music.mute && second.IsChannelMuted(SoundChannel.Sfx), Is.True);
                Assert.That(SaveProvider.GetInt(MuteKey, -1), Is.EqualTo(1 | 2 | 8), "Binding writes nothing.");

                settings.SetMusic(false);
                Assert.That(SaveProvider.GetInt(MuteKey, -1), Is.EqualTo(1 | 2 | 8),
                    "The next mute change must keep the saved UI bit.");

                settings.ToggleAllAudio();
                Assert.That(second.IsChannelMuted(SoundChannel.Ui), Is.False);
                Assert.That(SaveProvider.GetInt(MuteKey, -1), Is.Zero);
            }
        }

        [Test]
        public void MuteState_PersistsAndIsRestoredByTheNextSession()
        {
            using (Audit20261023NeoEditorTestsSaveScope saveScope = new Audit20261023NeoEditorTestsSaveScope())
            {
                AM firstManager = CreateManager("First session");
                AMSettings first = CreateSettings(true);
                first.Initialize(firstManager);
                Assert.That(SaveProvider.HasKey(MuteKey), Is.False, "Binding alone must not write the mute key.");

                first.SetMusic(false);
                first.ToggleEfx();
                Assert.That(SaveProvider.GetInt(MuteKey, -1), Is.EqualTo(3), "Music (1) and Efx (2) are muted.");

                AM secondManager = CreateManager("Second session");
                AMSettings second = CreateSettings(true);
                second.Initialize(secondManager);

                Assert.That(secondManager.Music.mute, Is.True, "The saved music mute must survive a restart.");
                Assert.That(secondManager.IsChannelMuted(SoundChannel.Sfx), Is.True);
                Assert.That(second.MuteMusicValue && second.MuteEfxValue, Is.True);

                second.SetMusic(true);
                Assert.That(SaveProvider.GetInt(MuteKey, -1), Is.EqualTo(2));
            }
        }

        [Test]
        public void RequestBeforeBinding_WinsOverThePersistedMute()
        {
            using (Audit20261023NeoEditorTestsSaveScope saveScope = new Audit20261023NeoEditorTestsSaveScope())
            {
                SaveProvider.SetInt(MuteKey, 3);
                AM manager = CreateManager("Consumer-owned flag");
                AMSettings settings = CreateSettings(true);

                settings.SetMusic(true);
                settings.Initialize(manager);

                Assert.That(manager.Music.mute, Is.False, "The consumer's explicit request is the newest intent.");
                Assert.That(manager.IsChannelMuted(SoundChannel.Sfx), Is.True, "Unrequested channels use the save.");
            }
        }

        [Test]
        public void ToggleAllAudio_PersistsTheUiChannelMuteToo()
        {
            using (Audit20261023NeoEditorTestsSaveScope saveScope = new Audit20261023NeoEditorTestsSaveScope())
            {
                AM firstManager = CreateManager("All off");
                AMSettings first = CreateSettings(true);
                first.Initialize(firstManager);
                first.ToggleAllAudio();
                Assert.That(SaveProvider.GetInt(MuteKey, -1), Is.EqualTo(1 | 2 | 8));

                AM secondManager = CreateManager("All off, next session");
                AMSettings second = CreateSettings(true);
                second.Initialize(secondManager);

                Assert.That(secondManager.IsChannelMuted(SoundChannel.Ui), Is.True,
                    "UI clicks must stay silent after a restart with all audio off.");
                Assert.That(secondManager.Music.mute && secondManager.IsChannelMuted(SoundChannel.Sfx), Is.True);
            }
        }

        [Test]
        public void MutePersistence_IsOffWhenEitherSwitchIsOff()
        {
            using (Audit20261023NeoEditorTestsSaveScope saveScope = new Audit20261023NeoEditorTestsSaveScope())
            {
                AMSettings noVolume = CreateSettings(false);
                noVolume.Initialize(CreateManager("Persistence off"));
                noVolume.SetMusic(false);
                Assert.That(SaveProvider.HasKey(MuteKey), Is.False, "Persist Volume off writes no save keys at all.");

                AMSettings noMute = CreateSettings(true);
                noMute.PersistMute = false;
                noMute.Initialize(CreateManager("Mute persistence off"));
                noMute.SetMusic(false);
                Assert.That(SaveProvider.HasKey(MuteKey), Is.False);
                Assert.That(noMute.PersistVolume, Is.True);
            }
        }

        // WHY: a Play Mode case; outside Play Mode the editor does not apply SetFloat to exposed mixer parameters,
        // so the EditMode version of this test was always ignored and the muted-Master contract never ran.
        [UnityTest]
        public IEnumerator SetMasterVolume_WhileMasterIsMuted_StaysSilentAndBecomesTheRestoredLevel()
        {
            yield return new EnterPlayMode();
            AudioMixer mixer = AssetDatabase.LoadAssetAtPath<AudioMixer>(MixerPath);
            if (mixer == null)
            {
                mixer = AssetDatabase.LoadAssetAtPath<AudioMixer>(PackageMixerPath);
            }

            if (mixer == null)
            {
                Assert.Ignore("The package mixer asset is not available in this project layout.");
            }

            try
            {
                AMSettings settings = CreateSettings(false);
                settings.audioMixer = mixer;
                settings.Initialize(CreateManager("Mixer master"));
                settings.SetMasterVolume(0.5f);
                if (Mathf.Abs(settings.GetMasterVolumeNormalized() - 0.5f) > 0.001f)
                {
                    Assert.Ignore("Exposed mixer parameters are not writable on this editor host.");
                }

                settings.ToggleMaster();
                Assert.That(settings.MuteMasterValue, Is.True);
                Assert.That(settings.GetMasterVolumeNormalized(), Is.Zero);

                settings.SetMasterVolume(0.25f);
                Assert.That(settings.GetMasterVolumeNormalized(), Is.Zero,
                    "A volume change while muted must not make Master audible behind the mute.");
                Assert.That(settings.MuteMasterValue, Is.True);

                settings.ToggleMaster();
                Assert.That(settings.MuteMasterValue, Is.False);
                Assert.That(settings.GetMasterVolumeNormalized(), Is.EqualTo(0.25f).Within(0.001f),
                    "Unmuting restores the level chosen while muted, not the stale pre-mute level.");
            }
            finally
            {
                mixer.ClearFloat("MasterVolume");
            }

            yield return new ExitPlayMode();
        }

        private AM CreateManager(string name)
        {
            AM manager = CreateObject(name).AddComponent<AM>();
            // WHY: the first channel call creates AM's Music and Efx sources, as Start would at runtime.
            manager.SetEfxVolume(1f);
            Assert.That(manager.Music, Is.Not.Null);
            return manager;
        }

        private AMSettings CreateSettings(bool persist)
        {
            GameObject host = CreateObject("Settings");
            host.SetActive(false);
            AMSettings settings = host.AddComponent<AMSettings>();
            using (SerializedObject data = new SerializedObject(settings))
            {
                data.FindProperty("persistVolume").boolValue = persist;
                data.FindProperty("persistMute").boolValue = true;
                data.FindProperty("saveKeyMute").stringValue = MuteKey;
                data.FindProperty("saveKeyMusic").stringValue = _keyPrefix + ".Music";
                data.FindProperty("saveKeyEfx").stringValue = _keyPrefix + ".Efx";
                data.FindProperty("saveKeyMaster").stringValue = _keyPrefix + ".Master";
                data.ApplyModifiedPropertiesWithoutUndo();
            }

            host.SetActive(true);
            return settings;
        }

        private GameObject CreateObject(string name)
        {
            GameObject host = new GameObject("AmSettingsMuteTests." + name);
            _owned.Add(host);
            return host;
        }
    }
}
