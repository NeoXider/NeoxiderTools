using System;
using System.Collections.Generic;
using System.Reflection;
using Neo.Audio;
using Neo.Tests.Performance;
using NUnit.Framework;
using UnityEngine;
using Object = UnityEngine.Object;
using Random = UnityEngine.Random;

namespace Neo.Editor.Tests
{
    /// <summary>
    ///     Allocation guard for AM's per-shot paths: once the voice pools are warm, playing effects by id, on the UI
    ///     channel, with random pitch, at a position, and running AM's LateUpdate allocate nothing.
    /// </summary>
    public sealed class AmHotPathAllocationTests
    {
        private const int Iterations = 200;

        private readonly List<Object> _owned = new List<Object>();
        private Random.State _randomState;

        [SetUp]
        public void SetUp()
        {
            _randomState = Random.state;
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
        public void WarmShots_DoNotAllocate()
        {
            GameObject host = new GameObject("AmHotPathAllocationTests");
            _owned.Add(host);
            AM manager = host.AddComponent<AM>();
            SoundEntry hit = new SoundEntry("hit", CreateClip("hit A"), CreateClip("hit B"));
            SoundEntry click = new SoundEntry("click", CreateClip("click")) { Channel = SoundChannel.Ui };
            SoundEntry flat = new SoundEntry("flat", CreateClip("flat")) { RandomizePitch = false };
            manager.SetSoundEntries(hit, click, flat);
            manager.SetEfxVolume(1f);
            MethodInfo lateUpdateMethod = typeof(AM).GetMethod("LateUpdate", BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.That(lateUpdateMethod, Is.Not.Null);
            Action lateUpdate = (Action)Delegate.CreateDelegate(typeof(Action), manager, lateUpdateMethod);
            Vector3 position = new Vector3(1f, 2f, 3f);

            // WHY 40 warm rounds: every pitch and positional voice of both rotations must exist before measuring.
            for (int i = 0; i < 40; i++)
            {
                Shoot(manager, lateUpdate, position);
            }

            using GcAllocCounter allocationCounter = GcAllocCounter.CreateOrIgnore();
            allocationCounter.Begin();
            for (int i = 0; i < Iterations; i++)
            {
                Shoot(manager, lateUpdate, position);
            }

            long allocated = allocationCounter.End();

            Assert.That(allocated, Is.EqualTo(0L));
        }

        private static void Shoot(AM manager, Action lateUpdate, Vector3 position)
        {
            manager.Play("hit");
            manager.Play("click");
            manager.Play("flat", 0.5f);
            manager.TryPlay("hit");
            manager.PlayAt("hit", position);
            lateUpdate();
        }

        private AudioClip CreateClip(string name)
        {
            AudioClip clip = AudioClip.Create(name, 4410, 1, 44100, false);
            _owned.Add(clip);
            return clip;
        }
    }
}
