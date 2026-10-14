using System.Collections;
using System.Collections.Generic;
using Neo.Audio;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using Object = UnityEngine.Object;

namespace Neo.Tests.Play
{
    /// <summary>
    ///     Play Mode teardown of the helper sources <see cref="AM" /> creates for itself (UI channel source, pitch
    ///     voices of both channels, positional voices). EditMode cannot cover this: OnDestroy is only sent in Play
    ///     Mode, and that is where destroying children from OnDestroy could report the same object twice.
    /// </summary>
    public sealed class AmTeardownPlayModeTests
    {
        private readonly List<string> _teardownLogs = new List<string>();

        [TearDown]
        public void StopCapturing()
        {
            Application.logMessageReceived -= CaptureLog;
        }

        [UnityTest]
        public IEnumerator DestroyingTheAmGameObject_WithCreatedVoices_LogsNothing()
        {
            GameObject host = new GameObject("AmTeardownPlayModeTests.Host");
            AM manager = host.AddComponent<AM>();
            yield return null;

            PlayOnEveryVoiceKind(manager);
            yield return null;

            // WHY the capture starts here and not at the top: adding AM at runtime in the Editor already warns
            // from AM.OnValidate (it creates the Music / Efx sources there), which is older than this test and
            // unrelated to teardown. Only what the destruction itself logs is under test.
            StartCapturing();
            Object.Destroy(host);
            yield return null;
            yield return null;

            Assert.IsEmpty(_teardownLogs, "Destroying AM with its helper voices must log nothing.");
        }

        [UnityTest]
        public IEnumerator RemovingOnlyTheAmComponent_DestroysItsHelperSourcesAndKeepsTheAuthoredOnes()
        {
            GameObject host = new GameObject("AmTeardownPlayModeTests.Host");
            try
            {
                AM manager = host.AddComponent<AM>();
                yield return null;

                PlayOnEveryVoiceKind(manager);
                AudioSource ui = manager.GetChannelSource(SoundChannel.Ui);
                AudioSource positional = manager.PlayAt(CreateClip("boom"), Vector3.one);
                AudioSource efx = manager.Efx;
                AudioSource music = manager.Music;
                yield return null;

                StartCapturing();
                Object.Destroy(manager);
                yield return null;
                yield return null;

                Assert.IsTrue(ui == null, "The UI channel source must not outlive AM.");
                Assert.IsTrue(positional == null, "Positional voices must not outlive AM.");
                Assert.AreEqual(0, CountHelperSources(host), "No pitch or positional voice may be left behind.");
                Assert.IsTrue(efx != null && music != null, "The Efx and Music sources are not helpers.");
                Assert.IsEmpty(_teardownLogs, "Removing AM must log nothing.");
            }
            finally
            {
                Object.Destroy(host);
            }
        }

        private void StartCapturing()
        {
            _teardownLogs.Clear();
            Application.logMessageReceived += CaptureLog;
        }

        private void CaptureLog(string condition, string stackTrace, LogType type)
        {
            _teardownLogs.Add(type + ": " + condition);
        }

        private static void PlayOnEveryVoiceKind(AM manager)
        {
            manager.SetSoundEntries(
                new SoundEntry("hit", CreateClip("hit")) { RandomizePitch = true },
                new SoundEntry("click", CreateClip("click")) { RandomizePitch = true, Channel = SoundChannel.Ui },
                new SoundEntry("tap", CreateClip("tap")) { RandomizePitch = false, Channel = SoundChannel.Ui });

            manager.Play("hit");
            manager.Play("click");
            manager.Play("tap");
            manager.PlayAt("hit", new Vector3(2f, 0f, 0f));
        }

        private static int CountHelperSources(GameObject host)
        {
            int count = 0;
            Transform[] all = host.GetComponentsInChildren<Transform>(true);
            for (int index = 0; index < all.Length; index++)
            {
                string name = all[index].name;
                if (name.StartsWith("PitchVoice_", System.StringComparison.Ordinal) ||
                    name.StartsWith("UiPitchVoice_", System.StringComparison.Ordinal) ||
                    name.StartsWith("PositionalVoice_", System.StringComparison.Ordinal) ||
                    name == "UiChannel")
                {
                    count++;
                }
            }

            return count;
        }

        private static AudioClip CreateClip(string name)
        {
            return AudioClip.Create(name, 4410, 1, 44100, false);
        }
    }
}
