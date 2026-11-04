#if UNITY_EDITOR
using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using Neo.Audio;
using NUnit.Framework;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using Object = UnityEngine.Object;

namespace Neo.Tests.Play
{
    /// <summary>
    ///     Play-mode test of the authored AudioDemo.unity: its buttons call <see cref="AM" /> and
    ///     <see cref="MusicControl" /> through persistent UnityEvents, the AudioDemoController only logs and mirrors
    ///     state, and removing that controller in the middle of a crossfade leaves the borrowed music, the manager
    ///     and the scene's assets alone while detaching every event subscription it had.
    /// </summary>
    public sealed class AudioDemoScenePlayTests
    {
        private const string ScenePath = "Scenes/Audio/AudioDemo.unity";
        private const string PitchVoicePrefix = "PitchVoice_";
        private const string CrossfadeVoicePrefix = "MusicCrossfade_";
        private const int ShotPresses = 20;
        private const int PitchSeed = 20261104;

        private readonly SamplePlaySceneIsolation _isolation = new SamplePlaySceneIsolation();
        private readonly List<string> _logged = new List<string>();
        private Component _feed;
        private EventInfo _feedEvent;
        private Action<string> _feedListener;

        [UnityTearDown]
        public IEnumerator TearDown()
        {
            if (_feed != null && _feedEvent != null && _feedListener != null)
            {
                _feedEvent.RemoveEventHandler(_feed, _feedListener);
            }

            _feed = null;
            _feedEvent = null;
            _feedListener = null;
            _logged.Clear();
            yield return _isolation.End();
        }

        [UnityTest]
        public IEnumerator AudioDemo_AuthoredButtonsDriveAm_AndControllerRemovalDuringCrossfadeKeepsBorrowedMusic()
        {
            // WHY: re-armed in the body because the UTF log scope of [UnitySetUp] does not carry into it.
            LogAssert.ignoreFailingMessages = false;
            string path = SamplePlayScenes.RequireDemoScene(ScenePath);
            _isolation.Begin();
            AudioListener.pause = false;
            yield return SamplePlayScenes.Load(path);
            yield return SamplePlayScenes.WaitFrames(3);

            Scene scene = SceneManager.GetSceneByPath(path);
            Component controller = SamplePlayScenes.RequireActiveComponent("Neo.Samples.AudioDemoController");
            Assert.That((bool)SamplePlayScenes.GetProperty(controller, "IsSceneBuilt"), Is.True,
                "AudioDemo must be authored by AudioDemoBuilder (UI and wiring saved in the scene).");
            AM manager = (AM)SamplePlayScenes.GetProperty(controller, "Audio");
            Assert.That(manager, Is.Not.Null);
            Assert.That(manager.gameObject.scene == scene, Is.True, "The demo drives the scene's own AM.");
            Assert.That(AM.I, Is.SameAs(manager), "MusicControl reaches the scene's AM through AM.I.");
            TMP_Text nowPlaying = (TMP_Text)SamplePlayScenes.GetField(controller, "_nowPlaying");
            CaptureFeed((Component)SamplePlayScenes.GetField(controller, "_log"));
            HashSet<AudioClip> clipsBefore = new HashSet<AudioClip>(Resources.FindObjectsOfTypeAll<AudioClip>());
            int canvasesBefore = CountSceneCanvases(scene);

            SoundEntry shot = manager.GetSound("shot");
            Assert.That(shot, Is.Not.Null, "The builder authors the 'shot' sound entry.");
            Assert.That(shot.RandomizePitch, Is.True, "'shot' plays on a pitch voice, so the voice proves the play.");
            Assert.That(shot.PitchMax, Is.GreaterThan(shot.PitchMin), "'shot' authors a real pitch range.");
            UnityEngine.Random.State randomBefore = UnityEngine.Random.state;
            UnityEngine.Random.InitState(PitchSeed);
            try
            {
                SamplePlayScenes.ClickButton("Btn_Shot");
            }
            finally
            {
                UnityEngine.Random.state = randomBefore;
            }

            Assert.That(_logged, Has.Some.EqualTo("AM.I.Play(\"shot\")"),
                "Btn_Shot -> AudioDemoController.LogSound(\"shot\") is a persistent listener.");
            List<AudioSource> voices = ChildSources(manager.Efx, PitchVoicePrefix);
            Assert.That(voices, Is.Not.Empty, "Btn_Shot -> AM.Play(\"shot\") plays on a pitch voice of the Efx channel.");
            AudioSource played = (AudioSource)SamplePlayScenes.GetField(manager, "_lastOneShotSource");
            Assert.That(voices, Does.Contain(played),
                "The shot went out on a pitch voice of the Efx channel, not on the channel source itself.");
            Assert.That(played.pitch, Is.InRange(shot.PitchMin, shot.PitchMax), "The voice carries the entry's pitch.");
            Assert.That(played.pitch, Is.Not.EqualTo(1f),
                "The seeded roll moved the voice off the AudioSource default pitch, so the randomization reached it.");
            for (int i = 0; i < ShotPresses; i++)
            {
                SamplePlayScenes.ClickButton("Btn_Shot");
            }

            int voicePool = ChildSources(manager.Efx, PitchVoicePrefix).Count;
            Assert.That(voicePool, Is.LessThanOrEqualTo(8),
                ShotPresses + " presses reuse the authored 8-voice pool instead of growing it.");

            SamplePlayScenes.ClickButton("Btn_Theme (loop)");
            Assert.That(manager.CurrentMusicId, Is.EqualTo("theme"));
            AudioClip theme = manager.GetCurrentMusicClip();
            Assert.That(theme, Is.Not.Null);
            Assert.That(manager.GetMusicPool("theme").Clips, Does.Contain(theme));
            yield return null;
            Assert.That(manager.Music.isPlaying, Is.True, "This acceptance requires enabled Unity audio.");
            Assert.That(nowPlaying.text, Is.EqualTo("theme · " + theme.name),
                "AM.OnMusicStarted -> AudioDemoController refreshes the Now playing label.");

            MusicEntry pads = manager.GetMusicPool("pads");
            Assert.That(pads, Is.Not.Null);
            Assert.That(pads.Mode, Is.EqualTo(MusicPoolMode.Shuffle));
            SamplePlayScenes.ClickButton("Btn_Pads (shuffle)");
            Assert.That(manager.CurrentMusicId, Is.EqualTo("pads"));
            AudioClip firstPad = manager.GetCurrentMusicClip();
            Assert.That(pads.Clips, Does.Contain(firstPad));
            Assert.That(FadingVoices(manager, theme), Is.Not.Empty,
                "MusicControl.PlayPool(\"pads\") crossfades: the theme hands off to a fading voice.");
            Assert.That(nowPlaying.text, Is.EqualTo("pads · " + firstPad.name));

            SamplePlayScenes.ClickButton("Btn_Next track");
            AudioClip secondPad = manager.GetCurrentMusicClip();
            Assert.That(pads.Clips, Does.Contain(secondPad));
            Assert.That(secondPad, Is.Not.SameAs(firstPad), "NextTrack never repeats the current shuffle track.");
            Assert.That(_logged.Exists(line => line.Contains("OnRandomMusicTrackChanged") && line.Contains(secondPad.name)),
                Is.True, "AM.OnRandomMusicTrackChanged reaches the controller's log.");
            Assert.That(FadingVoices(manager, firstPad), Is.Not.Empty,
                "The previous pad is still fading out when the controller is removed.");

            string labelAtRemoval = nowPlaying.text;
            int loggedAtRemoval = _logged.Count;
            Object.Destroy(controller);
            yield return null;
            yield return null;

            Assert.That(controller == null, Is.True);
            Assert.That(manager != null, Is.True, "The controller borrows AM; removing it must not destroy the manager.");
            Assert.That(manager.Music.clip, Is.SameAs(secondPad));
            Assert.That(manager.Music.isPlaying, Is.True, "Removing the demo controller must not stop the borrowed music.");
            Assert.That(manager.CurrentMusicId, Is.EqualTo("pads"));
            Assert.That(theme != null && firstPad != null && secondPad != null, Is.True,
                "The controller owns no clip; the authored assets stay loaded.");

            SamplePlayScenes.ClickButton("Btn_Next track");
            AudioClip thirdPad = manager.GetCurrentMusicClip();
            Assert.That(thirdPad, Is.Not.SameAs(secondPad), "The NoCode wiring keeps driving AM without the controller.");
            Assert.That(_logged.GetRange(loggedAtRemoval, _logged.Count - loggedAtRemoval)
                    .Exists(line => line.Contains("OnRandomMusicTrackChanged")), Is.False,
                "The removed controller must have unsubscribed from AM.OnRandomMusicTrackChanged.");
            Assert.That(nowPlaying.text, Is.EqualTo(labelAtRemoval), "Nothing refreshes the label once its controller is gone.");
            int loggedBeforeShot = _logged.Count;
            SamplePlayScenes.ClickButton("Btn_Shot");
            Assert.That(_logged.Count, Is.EqualTo(loggedBeforeShot),
                "A persistent listener of a destroyed controller is skipped, not invoked.");

            double deadline = Time.realtimeSinceStartupAsDouble + 5d;
            while (FadingVoices(manager, null).Count > 0)
            {
                Assert.That(Time.realtimeSinceStartupAsDouble, Is.LessThan(deadline),
                    "The pads crossfade (1.2 s) did not finish within 5 real-time seconds.");
                yield return null;
            }

            Assert.That(manager.Music.isPlaying, Is.True, "The incoming pad keeps playing after the crossfade.");
            Assert.That(manager.CurrentMusicId, Is.EqualTo("pads"));
            Assert.That(pads.Clips, Does.Contain(manager.Music.clip));
            Assert.That(manager.Music.volume, Is.GreaterThan(0f));
            foreach (AudioSource fade in ChildSources(manager.Music, CrossfadeVoicePrefix))
            {
                Assert.That(fade.clip, Is.Null, fade.name + " releases its outgoing clip once the fade ends.");
            }

            List<string> createdClips = new List<string>();
            foreach (AudioClip clip in Resources.FindObjectsOfTypeAll<AudioClip>())
            {
                if (!clipsBefore.Contains(clip))
                {
                    createdClips.Add(clip.name);
                }
            }

            Assert.That(createdClips, Is.Empty, "The demo creates no AudioClips at runtime.");
            Assert.That(CountSceneCanvases(scene), Is.EqualTo(canvasesBefore), "The demo builds no UI at runtime.");
            Assert.That(ChildSources(manager.Efx, PitchVoicePrefix).Count, Is.EqualTo(voicePool),
                "Playing on after the removal reuses the same voices.");
        }

        private void CaptureFeed(Component feed)
        {
            Assert.That(feed, Is.Not.Null, "The authored log feed is wired to the controller.");
            _feedEvent = feed.GetType().GetEvent("Logged");
            Assert.That(_feedEvent, Is.Not.Null, "DemoLogFeed.Logged is the observation seam.");
            _feed = feed;
            _feedListener = line => _logged.Add(line);
            _feedEvent.AddEventHandler(feed, _feedListener);
        }

        private static List<AudioSource> ChildSources(AudioSource channel, string namePrefix)
        {
            List<AudioSource> result = new List<AudioSource>();
            Assert.That(channel, Is.Not.Null, "The authored AM has its channel source.");
            foreach (AudioSource source in channel.GetComponentsInChildren<AudioSource>(true))
            {
                if (source != channel && source.name.StartsWith(namePrefix, StringComparison.Ordinal))
                {
                    result.Add(source);
                }
            }

            return result;
        }

        private static List<AudioSource> FadingVoices(AM manager, AudioClip clip)
        {
            List<AudioSource> result = new List<AudioSource>();
            foreach (AudioSource fade in ChildSources(manager.Music, CrossfadeVoicePrefix))
            {
                if (fade.isPlaying && fade.clip != null && (clip == null || fade.clip == clip))
                {
                    result.Add(fade);
                }
            }

            return result;
        }

        private static int CountSceneCanvases(Scene scene)
        {
            int count = 0;
            foreach (Canvas canvas in Resources.FindObjectsOfTypeAll<Canvas>())
            {
                if (canvas.gameObject.scene == scene)
                {
                    count++;
                }
            }

            return count;
        }
    }
}
#endif
