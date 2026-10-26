using System.Collections;
using System.Collections.Generic;
using Neo.Audio;
using Neo.Reactive;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using UnityEngine.UI;
using Object = UnityEngine.Object;

namespace Neo.Tests.Play
{
    public sealed class Audit20261023NeoAudioLifecycleTests
    {
        private readonly List<Object> _owned = new List<Object>();
        private readonly List<RandomMusicController> _controllers = new List<RandomMusicController>();
        private AM _manager;
        private bool _listenerWasPaused;
        private bool _managerAutoCreate;
        private bool _settingsAutoCreate;
        private Random.State _randomState;

        [UnitySetUp]
        public IEnumerator SetUp()
        {
            _listenerWasPaused = AudioListener.pause;
            _managerAutoCreate = AM.CreateInstance;
            _settingsAutoCreate = AMSettings.CreateInstance;
            _randomState = Random.state;
            AM.CreateInstance = false;
            AMSettings.CreateInstance = false;
            AudioListener.pause = false;
            _manager = CreateObject("Audio lifecycle audit").AddComponent<AM>();
            yield return null;
        }

        [UnityTearDown]
        public IEnumerator TearDown()
        {
            foreach (RandomMusicController controller in _controllers)
            {
                controller.Stop();
            }

            _controllers.Clear();
            AudioListener.pause = _listenerWasPaused;
            for (int index = _owned.Count - 1; index >= 0; index--)
            {
                if (_owned[index] != null)
                {
                    Object.Destroy(_owned[index]);
                }
            }

            _owned.Clear();
            yield return null;
            yield return null;
            AM.CreateInstance = _managerAutoCreate;
            AMSettings.CreateInstance = _settingsAutoCreate;
            Random.state = _randomState;
        }

        [UnityTest]
        public IEnumerator MusicMute_ReachesTheOutgoingCrossfadeAndRestoresIt()
        {
            AudioClip outgoingClip = CreateClip("Outgoing", 10f);
            _manager.SetMusicEntries(new MusicEntry("First", outgoingClip),
                new MusicEntry("Second", CreateClip("Incoming", 10f)));
            _manager.PlayMusicPool("First", MusicTransition.Instant);
            yield return null;
            Assert.That(_manager.Music.isPlaying, Is.True, "This regression requires enabled Unity audio.");
            _manager.PlayMusicPool("Second", MusicTransition.Fade(10f));
            AudioSource outgoing = FindOutgoingSource(outgoingClip);

            _manager.Music.mute = true;
            yield return null;

            Assert.That(outgoing.mute, Is.True, "A muted music channel must silence the old track too.");
            _manager.Music.mute = false;
            yield return null;
            Assert.That(outgoing.mute, Is.False);
        }

        [UnityTest]
        public IEnumerator StopDuringCrossfade_FadesEveryTrackFromItsOwnLevel()
        {
            AudioClip outgoingClip = CreateClip("Quiet outgoing", 10f);
            _manager.SetMusicEntries(new MusicEntry("First", outgoingClip) { Volume = 0.25f },
                new MusicEntry("Second", CreateClip("Loud incoming", 10f)));
            _manager.SetMusicVolume(1f);
            _manager.PlayMusicPool("First", MusicTransition.Instant);
            yield return null;
            Assert.That(_manager.Music.isPlaying, Is.True, "This regression requires enabled Unity audio.");
            _manager.PlayMusicPool("Second", MusicTransition.Fade(10f));
            AudioSource outgoing = FindOutgoingSource(outgoingClip);
            yield return new WaitForSecondsRealtime(0.05f);
            float incomingBefore = _manager.Music.volume;
            float outgoingBefore = outgoing.volume;
            Assert.That(incomingBefore, Is.GreaterThan(0f));
            Assert.That(outgoingBefore, Is.GreaterThan(incomingBefore * 2f));

            _manager.StopMusic(MusicTransition.Fade(10f));

            Assert.That(outgoing.volume, Is.GreaterThan(0f));
            Assert.That(outgoing.volume * incomingBefore,
                Is.EqualTo(_manager.Music.volume * outgoingBefore).Within(0.00001f),
                "Stopping must preserve the relative levels instead of copying the incoming level to every source.");
        }

        [UnityTest]
        public IEnumerator MusicChannelChanges_PreserveFadeProgressAndScaleAllVoices()
        {
            AudioClip outgoingClip = CreateClip("Outgoing headroom", 10f);
            AudioClip incomingClip = CreateClip("Incoming", 10f);
            MusicEntry first = new MusicEntry("First", outgoingClip) { Volume = 1.5f };
            first.SetClipVolume(0, 2f);
            _manager.SetMusicEntries(first,
                new MusicEntry("Second", incomingClip) { Volume = 0.5f },
                new MusicEntry("Third", CreateClip("Rapid replacement", 10f)) { Volume = 0.8f });
            _manager.SetMusicVolume(0.2f);
            _manager.PlayMusicPool("First", MusicTransition.Instant);
            yield return null;
            Assert.That(_manager.Music.isPlaying, Is.True, "This regression requires enabled Unity audio.");
            _manager.PlayMusicPool("Second", MusicTransition.Fade(10f));
            AudioSource outgoing = FindOutgoingSource(outgoingClip);
            yield return new WaitForSecondsRealtime(0.05f);
            float incomingBefore = _manager.Music.volume;
            float outgoingBefore = outgoing.volume;
            Assert.That(incomingBefore, Is.GreaterThan(0f));

            _manager.SetMusicVolume(0f);
            Assert.That(_manager.Music.volume, Is.Zero);
            Assert.That(outgoing.volume, Is.Zero, "A zero channel must immediately silence an outgoing track.");
            _manager.SetMusicVolume(0.4f);
            Assert.That(_manager.Music.volume, Is.EqualTo(incomingBefore * 2f).Within(0.00001f));
            Assert.That(outgoing.volume, Is.EqualTo(outgoingBefore / 0.6f).Within(0.00001f),
                "Entry/trim headroom is clamped before applying the preserved fade envelope.");
            _manager.SetMusicVolume(0.1f);
            Assert.That(_manager.Music.volume, Is.EqualTo(incomingBefore * 0.5f).Within(0.00001f));
            Assert.That(outgoing.volume, Is.EqualTo(outgoingBefore * 0.5f).Within(0.00001f));

            _manager.PlayMusicPool("Third", MusicTransition.Fade(10f));
            AudioSource secondOutgoing = FindOutgoingSource(incomingClip);
            _manager.StopMusic(MusicTransition.Fade(10f));
            incomingBefore = _manager.Music.volume;
            outgoingBefore = outgoing.volume;
            float secondOutgoingBefore = secondOutgoing.volume;
            _manager.SetMusicVolume(0.2f);
            Assert.That(_manager.Music.volume, Is.EqualTo(incomingBefore * 2f).Within(0.00001f));
            Assert.That(outgoing.volume, Is.EqualTo(outgoingBefore * 2f).Within(0.00001f));
            Assert.That(secondOutgoing.volume, Is.EqualTo(secondOutgoingBefore * 2f).Within(0.00001f));
        }

        [UnityTest]
        public IEnumerator StandaloneListenerPause_HoldsTheTrackUntilResume()
        {
            AudioSource source = CreateObject("Standalone paused source").AddComponent<AudioSource>();
            source.ignoreListenerPause = false;
            RandomMusicController controller = CreateController(source, 0.05f);
            int tracks = 0;
            controller.OnTrackChanged += clip => tracks++;
            AudioListener.pause = true;

            controller.Start();
            AudioClip initial = controller.CurrentTrack;
            yield return new WaitForSecondsRealtime(0.2f);

            Assert.That(tracks, Is.EqualTo(1));
            Assert.That(controller.CurrentTrack, Is.SameAs(initial));
            AudioListener.pause = false;
            yield return new WaitForSecondsRealtime(0.15f);
            Assert.That(tracks, Is.GreaterThan(1));
        }

        [UnityTest]
        public IEnumerator StandaloneZeroPitch_HoldsTheTrackUntilPitchResumes()
        {
            AudioSource source = CreateObject("Standalone pitch source").AddComponent<AudioSource>();
            source.pitch = 0f;
            RandomMusicController controller = CreateController(source, 0.05f);
            int tracks = 0;
            controller.OnTrackChanged += clip => tracks++;

            controller.Start();
            yield return new WaitForSecondsRealtime(0.2f);

            Assert.That(tracks, Is.EqualTo(1));
            source.pitch = 2f;
            yield return new WaitForSecondsRealtime(0.15f);
            Assert.That(tracks, Is.GreaterThan(1));
        }

        [UnityTest]
        public IEnumerator StandaloneDestroyedSource_EndsTheSessionOnce()
        {
            AudioSource source = CreateObject("Standalone destroyed source").AddComponent<AudioSource>();
            RandomMusicController controller = CreateController(source, 10f);
            int stopped = 0;
            controller.OnStopped += () => stopped++;
            controller.Start();

            Object.Destroy(source);
            yield return null;
            yield return null;
            yield return null;

            Assert.That(controller.IsPlaying || controller.IsPaused, Is.False);
            Assert.That(stopped, Is.EqualTo(1));
            controller.Stop();
            Assert.That(stopped, Is.EqualTo(1));
        }

        [UnityTest]
        public IEnumerator ToggleSync_ReflectsMuteWithoutInvokingUserCallbacks()
        {
            AMSettings settings = CreateSettings();
            yield return null;
            GameObject host = CreateObject("Music toggle", true);
            host.SetActive(false);
            Toggle toggle = host.AddComponent<Toggle>();
            toggle.isOn = false;
            AudioControl control = host.AddComponent<AudioControl>();
            ConfigureControl(control, AudioControl.ControlType.Music);
            int callbacks = 0;
            toggle.onValueChanged.AddListener(value => callbacks++);
            host.SetActive(true);
            yield return null;

            Assert.That(toggle.isOn, Is.True);
            settings.SetMusic(false);
            Assert.That(toggle.isOn, Is.False, "Nonzero volume does not mean an unmuted channel.");
            settings.SetMusic(true);
            Assert.That(toggle.isOn, Is.True);
            Assert.That(callbacks, Is.Zero, "State synchronization must not impersonate UI input.");

            settings.SetMusic(false);
            toggle.isOn = true;
            Assert.That(_manager.Music.mute, Is.False, "Turning the control on must clear an external mute.");
            Assert.That(callbacks, Is.EqualTo(1));
            toggle.isOn = false;
            Assert.That(callbacks, Is.EqualTo(2));
            Assert.That(settings.GetMusicVolumeNormalized(), Is.Zero);
        }

        [UnityTest]
        public IEnumerator SliderSync_IsSilentAndStillAcceptsUserInput()
        {
            AMSettings settings = CreateSettings();
            yield return null;
            GameObject host = CreateObject("Music slider", true);
            host.SetActive(false);
            Slider slider = host.AddComponent<Slider>();
            AudioControl control = host.AddComponent<AudioControl>();
            ConfigureControl(control, AudioControl.ControlType.Music);
            int callbacks = 0;
            slider.onValueChanged.AddListener(value => callbacks++);
            host.SetActive(true);
            yield return null;

            Assert.That(slider.value, Is.EqualTo(settings.GetMusicVolumeNormalized()).Within(0.00001f));
            Assert.That(callbacks, Is.Zero);
            slider.value = 0.2f;
            Assert.That(callbacks, Is.EqualTo(1));
            Assert.That(settings.GetMusicVolumeNormalized(), Is.EqualTo(0.2f).Within(0.00001f));
        }

        [UnityTest]
        public IEnumerator CustomToggle_ReenableShowsTheCachedStateSilently()
        {
            GameObject host = CreateObject("Custom toggle", true);
            host.SetActive(false);
            Toggle toggle = host.AddComponent<Toggle>();
            AudioControl control = host.AddComponent<AudioControl>();
            ConfigureControl(control, AudioControl.ControlType.Custom);
            host.SetActive(true);
            yield return null;
            int callbacks = 0;
            toggle.onValueChanged.AddListener(value => callbacks++);

            host.SetActive(false);
            control.Set(false);
            host.SetActive(true);

            Assert.That(toggle.isOn, Is.False);
            Assert.That(callbacks, Is.Zero);
        }

        [UnityTest]
        public IEnumerator LateSettings_ReenableRecoversInputAndOnlyRestoresOwnedInteractability()
        {
            GameObject host = CreateObject("Late settings slider", true);
            host.SetActive(false);
            Slider slider = host.AddComponent<Slider>();
            AudioControl control = host.AddComponent<AudioControl>();
            ConfigureControl(control, AudioControl.ControlType.Music);
            int inputs = 0;
            slider.onValueChanged.AddListener(value => inputs++);
            if (NeoDiagnostics.RuntimeErrorsEnabled)
            {
                LogAssert.Expect(LogType.Error, "[AudioControl] AMSettings was not found in the scene.");
            }

            host.SetActive(true);
            yield return null;
            Assert.That(slider.interactable, Is.False);

            control.enabled = false;
            Assert.That(slider.interactable, Is.True, "Removing the binding gate restores only its own change.");
            control.enabled = true;
            Assert.That(slider.interactable, Is.False);
            AMSettings settings = CreateSettings();
            yield return null;
            host.SetActive(false);
            host.SetActive(true);
            yield return null;

            Assert.That(slider.interactable, Is.True);
            Assert.That(slider.value, Is.EqualTo(settings.GetMusicVolumeNormalized()).Within(0.00001f));
            Assert.That(inputs, Is.Zero);
            slider.value = 0.23f;
            Assert.That(inputs, Is.EqualTo(1));
            Assert.That(_manager.MusicVolume, Is.EqualTo(0.23f).Within(0.00001f));
            for (int index = 0; index < 3; index++)
            {
                host.SetActive(false);
                host.SetActive(true);
            }

            slider.value = 0.37f;
            Assert.That(inputs, Is.EqualTo(2), "Only user input should publish a slider value change.");
            Assert.That(_manager.MusicVolume, Is.EqualTo(0.37f).Within(0.00001f));
        }

        [UnityTest]
        public IEnumerator LateSettings_ExplicitBindingPreservesAnAuthoredDisabledToggle()
        {
            GameObject host = CreateObject("Authored disabled toggle", true);
            host.SetActive(false);
            Toggle toggle = host.AddComponent<Toggle>();
            toggle.interactable = false;
            AudioControl control = host.AddComponent<AudioControl>();
            ConfigureControl(control, AudioControl.ControlType.Music);
            if (NeoDiagnostics.RuntimeErrorsEnabled)
            {
                LogAssert.Expect(LogType.Error, "[AudioControl] AMSettings was not found in the scene.");
            }

            host.SetActive(true);
            yield return null;
            AMSettings settings = CreateSettings();
            yield return null;

            control.Initialize(settings);
            control.Initialize(settings);
            Assert.That(toggle.interactable, Is.False);
            Assert.That(settings.MuteMusic.ListenerCount, Is.EqualTo(1));
            settings.SetMusic(false);
            Assert.That(toggle.isOn, Is.False);
            settings.SetMusic(true);
            Assert.That(toggle.isOn, Is.True);
            Object.Destroy(control);
            yield return null;
            Assert.That(toggle.interactable, Is.False);
            Assert.That(settings.MuteMusic.ListenerCount, Is.Zero);
        }

        [UnityTest]
        public IEnumerator ReplacementSettings_MoveTheSubscriptionAndKeepRefreshSilent()
        {
            AMSettings first = CreateSettings();
            yield return null;
            ReactivePropertyBool retiredMute = first.MuteMusic;
            GameObject host = CreateObject("Replacement toggle", true);
            host.SetActive(false);
            Toggle toggle = host.AddComponent<Toggle>();
            AudioControl control = host.AddComponent<AudioControl>();
            ConfigureControl(control, AudioControl.ControlType.Music);
            int inputs = 0;
            toggle.onValueChanged.AddListener(value => inputs++);
            host.SetActive(true);
            yield return null;
            Assert.That(retiredMute.ListenerCount, Is.EqualTo(1));

            host.SetActive(false);
            Assert.That(retiredMute.ListenerCount, Is.Zero);
            Object.Destroy(first.gameObject);
            yield return null;
            AMSettings second = CreateSettings();
            yield return null;
            host.SetActive(true);
            control.Initialize(second);
            control.Initialize(second);
            Assert.That(second.MuteMusic.ListenerCount, Is.EqualTo(1));
            Assert.That(retiredMute.ListenerCount, Is.Zero);
            retiredMute.Value = true;
            Assert.That(toggle.isOn, Is.True);
            second.SetMusic(false);
            Assert.That(toggle.isOn, Is.False);
            Assert.That(inputs, Is.Zero);
            toggle.isOn = true;
            Assert.That(inputs, Is.EqualTo(1));
            Assert.That(_manager.Music.mute, Is.False);

            host.SetActive(false);
            Assert.That(second.MuteMusic.ListenerCount, Is.Zero);
            second.SetMusic(false);
            Assert.That(toggle.isOn, Is.True, "A disabled view must not receive settings notifications.");
            host.SetActive(true);
            Assert.That(second.MuteMusic.ListenerCount, Is.EqualTo(1));
            Assert.That(toggle.isOn, Is.False);
            Assert.That(inputs, Is.EqualTo(1));
        }

        [UnityTest]
        public IEnumerator LateManager_ExplicitInitializeIsIdempotentAcrossStartAndReenable()
        {
            Object.Destroy(_manager.gameObject);
            yield return null;
            yield return null;
            AMSettings settings = CreateSettings();
            if (NeoDiagnostics.RuntimeErrorsEnabled)
            {
                LogAssert.Expect(LogType.Error, "[AMSettings] Audio manager (AM) is not assigned!");
            }

            yield return null;
            Assert.That(settings.music, Is.Null);
            settings.startMusicVolume = 0.31f;
            settings.startEfxVolume = 0.67f;
            _manager = CreateObject("Late manager").AddComponent<AM>();
            settings.Initialize(_manager);
            Assert.That(settings.music, Is.SameAs(_manager.Music));
            Assert.That(settings.efx, Is.SameAs(_manager.Efx));
            Assert.That(settings.GetMusicVolumeNormalized(), Is.EqualTo(0.31f).Within(0.00001f));
            Assert.That(settings.GetEfxVolumeNormalized(), Is.EqualTo(0.67f).Within(0.00001f));

            AudioClip clip = CreateClip("Keep current music", 10f);
            settings.SetMusicVolume(0.23f);
            settings.SetEfxVolume(0.41f);
            settings.SetMusic(false);
            _manager.PlayMusicByClip(clip, MusicTransition.Instant);
            settings.enabled = false;
            settings.enabled = true;
            settings.Initialize(_manager);
            yield return null;
            Assert.That(_manager.GetCurrentMusicClip(), Is.SameAs(clip));
            Assert.That(_manager.MusicVolume, Is.EqualTo(0.23f).Within(0.00001f));
            Assert.That(_manager.EfxVolume, Is.EqualTo(0.41f).Within(0.00001f));
            Assert.That(_manager.Music.mute, Is.True, "Re-enable must not replay startup unmute.");
        }

        [UnityTest]
        public IEnumerator LateManager_ReenableRetriesTheMissingBinding()
        {
            Object.Destroy(_manager.gameObject);
            yield return null;
            yield return null;
            AMSettings settings = CreateSettings();
            if (NeoDiagnostics.RuntimeErrorsEnabled)
            {
                LogAssert.Expect(LogType.Error, "[AMSettings] Audio manager (AM) is not assigned!");
            }

            yield return null;
            Assert.That(settings.efx, Is.Null);
            _manager = CreateObject("Manager after settings Start").AddComponent<AM>();
            yield return null;

            settings.enabled = false;
            settings.enabled = true;
            settings.SetEfxVolume(0.29f);
            settings.SetEfx(false);
            Assert.That(settings.efx, Is.SameAs(_manager.Efx));
            Assert.That(settings.music, Is.SameAs(_manager.Music));
            Assert.That(_manager.EfxVolume, Is.EqualTo(0.29f).Within(0.00001f));
            Assert.That(_manager.Efx.mute && settings.MuteEfxValue, Is.True);
        }

        [UnityTest]
        public IEnumerator StandaloneRebind_StoppedObserverCannotLeaveTheOldSourcePlaying()
        {
            AudioSource first = CreateObject("Old source").AddComponent<AudioSource>();
            AudioSource second = CreateObject("New source").AddComponent<AudioSource>();
            AudioClip firstClip = CreateClip("Old track", 10f);
            AudioClip secondClip = CreateClip("New track", 10f);
            RandomMusicController controller = new RandomMusicController();
            _controllers.Add(controller);
            controller.Initialize(first, new[] { firstClip });
            bool restarted = false;
            int stopped = 0;
            System.Action onStopped = () =>
            {
                stopped++;
                if (!restarted)
                {
                    restarted = true;
                    controller.Start();
                }
            };
            controller.OnStopped += onStopped;
            try
            {
                controller.Start();
                yield return null;
                Assert.That(first.isPlaying, Is.True, "This regression requires enabled Unity audio.");
                controller.Initialize(second, new[] { secondClip });
                yield return null;
                yield return null;
                Assert.That(restarted, Is.True);
                Assert.That(stopped, Is.EqualTo(1));
                Assert.That(controller.IsPlaying || controller.IsPaused, Is.False);
                Assert.That(first.isPlaying || second.isPlaying, Is.False);
                Assert.That(controller.CurrentTrack, Is.Null);

                controller.Start();
                yield return null;
                Assert.That(second.isPlaying, Is.True);
                Assert.That(first.isPlaying, Is.False);
                Assert.That(controller.CurrentTrack, Is.SameAs(secondClip));
            }
            finally
            {
                controller.OnStopped -= onStopped;
                controller.Stop();
            }
        }

        [UnityTest]
        public IEnumerator ReleaseOwnedClips_PreservesOtherCurrentAndOutgoingMusic()
        {
            AudioClip firstOwner = CreateClip("First owner", 10f);
            AudioClip shared = CreateClip("Shared outgoing", 10f);
            AudioClip secondOwner = CreateClip("Second owner", 10f);
            _manager.PlayMusicByClip(firstOwner, MusicTransition.Instant);
            yield return null;
            Assert.That(_manager.Music.isPlaying, Is.True, "This regression requires enabled Unity audio.");
            _manager.PlayMusicByClip(shared, MusicTransition.Fade(10f));
            AudioSource firstOutgoing = FindOutgoingSource(firstOwner);
            _manager.PlayMusicByClip(secondOwner, MusicTransition.Fade(10f));
            AudioSource sharedOutgoing = FindOutgoingSource(shared);
            int stopped = 0;
            _manager.OnMusicStopped += () => stopped++;

            _manager.ReleaseClips(firstOwner);
            Assert.That(firstOutgoing.clip, Is.Null);
            Assert.That(firstOutgoing.isPlaying, Is.False);
            Assert.That(_manager.GetCurrentMusicClip(), Is.SameAs(secondOwner));
            Assert.That(_manager.Music.isPlaying && sharedOutgoing.isPlaying, Is.True);
            Assert.That(sharedOutgoing.clip, Is.SameAs(shared));
            Assert.That(stopped, Is.Zero);

            _manager.ReleaseClips(secondOwner);
            _manager.ReleaseClips(secondOwner);
            Assert.That(_manager.GetCurrentMusicClip(), Is.Null);
            Assert.That(_manager.CurrentMusicEntry, Is.Null);
            Assert.That(sharedOutgoing.clip, Is.SameAs(shared));
            Assert.That(sharedOutgoing.isPlaying, Is.True, "An unrelated outgoing fade must keep its lifetime.");
            Assert.That(stopped, Is.EqualTo(1));
            yield return null;
            Assert.That(sharedOutgoing.isPlaying, Is.True);
        }

        private AMSettings CreateSettings()
        {
            GameObject host = CreateObject("Settings");
            host.SetActive(false);
            AMSettings settings = host.AddComponent<AMSettings>();
            JsonUtility.FromJsonOverwrite("{\"persistVolume\":false}", settings);
            host.SetActive(true);
            return settings;
        }

        private static void ConfigureControl(AudioControl control, AudioControl.ControlType type)
        {
            JsonUtility.FromJsonOverwrite($"{{\"controlType\":{(int)type},\"backendType\":0}}", control);
        }

        private RandomMusicController CreateController(AudioSource source, float seconds)
        {
            RandomMusicController controller = new RandomMusicController();
            controller.Initialize(source, new[] { CreateClip("First standalone", seconds), CreateClip("Second standalone", seconds) });
            _controllers.Add(controller);
            return controller;
        }

        private AudioSource FindOutgoingSource(AudioClip clip)
        {
            foreach (AudioSource source in _manager.GetComponentsInChildren<AudioSource>())
            {
                if (source != _manager.Music && source.clip == clip)
                {
                    return source;
                }
            }

            Assert.Fail("The public pool switch must keep the outgoing clip on a fade source.");
            return null;
        }

        private GameObject CreateObject(string name, bool ui = false)
        {
            GameObject host = ui ? new GameObject(name, typeof(RectTransform)) : new GameObject(name);
            _owned.Add(host);
            return host;
        }

        private AudioClip CreateClip(string name, float seconds)
        {
            AudioClip clip = AudioClip.Create(name, Mathf.CeilToInt(44100f * seconds), 1, 44100, false);
            _owned.Add(clip);
            return clip;
        }
    }
}
