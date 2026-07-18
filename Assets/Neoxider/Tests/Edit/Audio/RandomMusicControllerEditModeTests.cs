using Neo.Audio;
using NUnit.Framework;
using UnityEngine;

namespace Neo.Editor.Tests
{
    /// <summary>
    ///     EditMode coverage for <see cref="RandomMusicController"/>, focused on the
    ///     Stop() event guard: OnStopped must fire only when playback was actually active,
    ///     so the defensive Stop() inside Start() does not spam subscribers.
    /// </summary>
    [TestFixture]
    public class RandomMusicControllerEditModeTests
    {
        private GameObject _go;
        private AudioSource _source;

        [SetUp]
        public void SetUp()
        {
            _go = new GameObject("RandomMusicControllerTests");
            _source = _go.AddComponent<AudioSource>();
        }

        [TearDown]
        public void TearDown()
        {
            if (_go != null)
            {
                Object.DestroyImmediate(_go);
            }
        }

        [Test]
        public void Stop_BeforeStart_DoesNotFireOnStopped()
        {
            var controller = new RandomMusicController();
            controller.Initialize(_source, new[] { CreateClip("t0"), CreateClip("t1") });

            int stopped = 0;
            controller.OnStopped += () => stopped++;

            controller.Stop();

            Assert.AreEqual(0, stopped,
                "Stop() on an idle controller must not raise OnStopped.");
        }

        [Test]
        public void Stop_AfterStart_FiresOnStoppedOnce()
        {
            var controller = new RandomMusicController();
            controller.Initialize(_source, new[] { CreateClip("t0"), CreateClip("t1") });

            int stopped = 0;
            controller.OnStopped += () => stopped++;

            controller.Start();
            controller.Stop();

            Assert.AreEqual(1, stopped,
                "Stop() after Start() must raise OnStopped exactly once.");
        }

        [Test]
        public void Start_DoesNotFireSpuriousOnStoppedFromDefensiveStop()
        {
            // WHY: Start() calls Stop() internally before (re)starting; that inner Stop must stay silent.
            var controller = new RandomMusicController();
            controller.Initialize(_source, new[] { CreateClip("t0"), CreateClip("t1") });

            int stopped = 0;
            controller.OnStopped += () => stopped++;

            controller.Start();

            Assert.AreEqual(0, stopped,
                "Start() must not raise OnStopped via its internal defensive Stop().");

            controller.Stop();
        }

        private static AudioClip CreateClip(string name)
        {
            return AudioClip.Create(name, 4410, 1, 44100, false);
        }
    }
}
