using System.Collections.Generic;
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
        private readonly List<AudioClip> _createdClips = new List<AudioClip>();
        private readonly List<RandomMusicController> _controllers = new List<RandomMusicController>();
        private GameObject _go;
        private AudioSource _source;

        private UnityEngine.Random.State _previousRandomState;

        [SetUp]
        public void SetUp()
        {
            _previousRandomState = UnityEngine.Random.state;
            _go = new GameObject("RandomMusicControllerTests");
            _source = _go.AddComponent<AudioSource>();
        }

        [TearDown]
        public void TearDown()
        {
            try
            {
                foreach (RandomMusicController controller in _controllers) controller.Stop();
                _controllers.Clear();
            }
            finally
            {
                try { if (_go != null) UnityEngine.Object.DestroyImmediate(_go); }
                finally { ReleaseClipsAndRestoreState(); }
            }
        }

        private void ReleaseClipsAndRestoreState()
        {
            try
            {
                foreach (AudioClip clip in _createdClips) UnityEngine.Object.DestroyImmediate(clip);
            }
            finally
            {
                _createdClips.Clear();
                UnityEngine.Random.state = _previousRandomState;
            }
        }

        [Test]
        public void Stop_BeforeStart_DoesNotFireOnStopped()
        {
            RandomMusicController controller = CreateController();
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
            RandomMusicController controller = CreateController();
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
            RandomMusicController controller = CreateController();
            controller.Initialize(_source, new[] { CreateClip("t0"), CreateClip("t1") });

            int stopped = 0;
            controller.OnStopped += () => stopped++;

            try
            {
                controller.Start();
                Assert.AreEqual(0, stopped,
                    "Start() must not raise OnStopped via its internal defensive Stop().");
            }
            finally
            {
                controller.Stop();
            }
        }

        private RandomMusicController CreateController()
        {
            RandomMusicController controller = new RandomMusicController();
            _controllers.Add(controller);
            return controller;
        }

        private AudioClip CreateClip(string name)
        {
            AudioClip clip = AudioClip.Create(name, 4410, 1, 44100, false);
            _createdClips.Add(clip);
            return clip;
        }
    }
}
