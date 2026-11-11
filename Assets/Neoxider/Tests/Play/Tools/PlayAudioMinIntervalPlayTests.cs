using System.Collections;
using Neo.Audio;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace Neo.Tests.Play
{
    /// <summary>PlayAudio Min Interval: repeated calls inside the window are dropped.</summary>
    public sealed class PlayAudioMinIntervalPlayTests
    {
        [UnityTest]
        public IEnumerator MinInterval_DropsCallsInsideTheWindow_AndPlaysAgainAfterIt()
        {
            GameObject host = new GameObject("Sfx");
            PlayAudio audio = host.AddComponent<PlayAudio>();
            audio.MinInterval = 0.2f;

            audio.AudioPlay();
            float first = audio.LastPlayTime;
            Assert.That(float.IsNegativeInfinity(first), Is.False, "The first call is accepted.");

            audio.AudioPlay();
            yield return null;
            audio.AudioPlay();
            Assert.That(audio.LastPlayTime, Is.EqualTo(first), "Calls inside the window are dropped.");

            yield return new WaitForSecondsRealtime(0.3f);
            audio.AudioPlay();
            Assert.That(audio.LastPlayTime, Is.GreaterThan(first), "A call after the window plays again.");
            Object.Destroy(host);
        }

        [UnityTest]
        public IEnumerator MinIntervalZero_AcceptsEveryCall()
        {
            GameObject host = new GameObject("Sfx");
            PlayAudio audio = host.AddComponent<PlayAudio>();

            audio.AudioPlay();
            float first = audio.LastPlayTime;
            yield return null;
            yield return null;
            audio.AudioPlay();
            Assert.That(audio.LastPlayTime, Is.GreaterThan(first));
            Object.Destroy(host);
        }
    }
}
