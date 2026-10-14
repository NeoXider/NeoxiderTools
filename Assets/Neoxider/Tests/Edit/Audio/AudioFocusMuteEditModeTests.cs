using Neo.Audio;
using NUnit.Framework;
using UnityEngine;

namespace Neo.Editor.Tests
{
    /// <summary>
    ///     <see cref="AudioFocusMute" />: losing focus silences the listener, regaining it restores the volume it
    ///     found, and the component never touches a volume it did not take.
    /// </summary>
    [TestFixture]
    public class AudioFocusMuteEditModeTests
    {
        private GameObject _go;
        private AudioFocusMute _mute;
        private float _volumeBefore;

        [SetUp]
        public void SetUp()
        {
            _volumeBefore = AudioListener.volume;
            AudioListener.volume = 0.6f;
            _go = new GameObject("AudioFocusMute_Test");
            _mute = _go.AddComponent<AudioFocusMute>();
        }

        [TearDown]
        public void TearDown()
        {
            if (_go != null)
            {
                Object.DestroyImmediate(_go);
            }

            AudioListener.volume = _volumeBefore;
        }

        [Test]
        public void LosingFocus_Silences_AndRegainingIt_RestoresTheVolume()
        {
            _mute.SetFocused(false);
            Assert.AreEqual(0f, AudioListener.volume);
            Assert.IsTrue(_mute.IsMuted);

            _mute.SetFocused(false);
            _mute.SetFocused(true);

            Assert.AreEqual(0.6f, AudioListener.volume, 1e-4f, "A repeated blur must not remember zero as the volume.");
            Assert.IsFalse(_mute.IsMuted);
        }

        [Test]
        public void RegainingFocus_WithoutAMute_LeavesTheVolumeAlone()
        {
            AudioListener.volume = 0.3f;
            _mute.SetFocused(true);
            Assert.AreEqual(0.3f, AudioListener.volume, 1e-4f);
        }

        [Test]
        public void TurnedOff_DoesNotMute_AndTurningOffWhileMutedRestores()
        {
            _mute.SetFocused(false);
            _mute.MuteWhenUnfocused = false;
            Assert.AreEqual(0.6f, AudioListener.volume, 1e-4f);

            _mute.SetFocused(false);
            Assert.AreEqual(0.6f, AudioListener.volume, 1e-4f);
            Assert.IsFalse(_mute.IsMuted);
        }

        [Test]
        public void DisabledComponent_DoesNotMute()
        {
            _mute.enabled = false;
            _mute.SetFocused(false);
            Assert.AreEqual(0.6f, AudioListener.volume, 1e-4f);
        }

        [Test]
        public void Install_ReusesTheExistingInstance_AndAppliesTheEditorOption()
        {
            Assert.AreSame(_mute, AudioFocusMute.Install(false));
            Assert.IsFalse(_mute.MuteInEditor);
        }

        [Test]
        public void Install_WithoutAnInstance_CreatesOne()
        {
            Object.DestroyImmediate(_go);
            _go = null;

            AudioFocusMute created = AudioFocusMute.Install();
            try
            {
                Assert.IsNotNull(created);
                Assert.IsTrue(created.MuteInEditor);
            }
            finally
            {
                Object.DestroyImmediate(created.gameObject);
            }
        }

        [Test]
        public void MuteInEditorOff_DoesNotMuteTheEditor_AndRestoresAnActiveMute()
        {
            _mute.SetFocused(false);
            _mute.MuteInEditor = false;
            Assert.AreEqual(0.6f, AudioListener.volume, 1e-4f, "Turning the editor option off while muted restores.");

            _mute.SetFocused(false);
            Assert.AreEqual(0.6f, AudioListener.volume, 1e-4f);
        }
    }
}
