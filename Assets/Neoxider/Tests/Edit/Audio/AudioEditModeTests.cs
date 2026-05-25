using System.Reflection;
using Neo.Audio;
using NUnit.Framework;
using UnityEngine;

namespace Neo.Editor.Tests
{
    [TestFixture]
    public class AudioEditModeTests
    {
        private GameObject _go;
        private AM _audioManager;
        private AudioSource _efx;
        private AudioSource _music;

        [SetUp]
        public void SetUp()
        {
            _go = new GameObject("AudioEditModeTests");
            _audioManager = _go.AddComponent<AM>();
            _efx = _go.AddComponent<AudioSource>();
            _music = _go.AddComponent<AudioSource>();

            SetPrivateField("_efx", _efx);
            SetPrivateField("_music", _music);
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
        public void SetVolume_ClampsEfxAndMusicVolumes()
        {
            _audioManager.SetVolume(2f, true);
            _audioManager.SetVolume(-1f, false);

            Assert.AreEqual(1f, _efx.volume);
            Assert.AreEqual(0f, _music.volume);
        }

        [Test]
        public void ApplyStartVolumes_AppliesConfiguredStartupVolumes()
        {
            _audioManager.startVolumeEfx = 0.25f;
            _audioManager.startVolumeMusic = 0.75f;

            _audioManager.ApplyStartVolumes();

            Assert.AreEqual(0.25f, _efx.volume);
            Assert.AreEqual(0.75f, _music.volume);
        }

        private void SetPrivateField(string fieldName, object value)
        {
            typeof(AM).GetField(fieldName, BindingFlags.NonPublic | BindingFlags.Instance)
                ?.SetValue(_audioManager, value);
        }
    }
}
