using Neo.Tools;
using NUnit.Framework;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Neo.Editor.Tests
{
    /// <summary>Resume speed bookkeeping of <see cref="GameTimeController" />.</summary>
    public sealed class GameTimeControllerTests
    {
        private float _previousScale;
        private GameObject _host;
        private GameTimeController _controller;

        [SetUp]
        public void SetUp()
        {
            _previousScale = Time.timeScale;
            Time.timeScale = 1f;
            _host = new GameObject(nameof(GameTimeControllerTests));
            _controller = _host.AddComponent<GameTimeController>();
        }

        [TearDown]
        public void TearDown()
        {
            if (_host != null)
            {
                Object.DestroyImmediate(_host);
            }

            Time.timeScale = _previousScale;
        }

        [Test]
        public void TogglePause_ResumesToTheSlowMotionThatWasRunning()
        {
            _controller.SetTimeScale(0.5f);

            _controller.TogglePause();
            Assert.That(_controller.IsPaused, Is.True);
            _controller.TogglePause();

            Assert.That(Time.timeScale, Is.EqualTo(0.5f));
        }

        [Test]
        public void TogglePause_AfterResumeGame_DoesNotReturnToAStaleSlowMotion()
        {
            _controller.SetTimeScale(0.5f);
            _controller.PauseGame();
            _controller.ResumeGame();
            _controller.SetTimeScale(0f);
            Assert.That(_controller.IsPaused, Is.True);

            _controller.TogglePause();

            Assert.That(Time.timeScale, Is.EqualTo(1f));
            Assert.That(_controller.ResumeScale, Is.EqualTo(1f));
        }

        [Test]
        public void SetTimeScaleZero_RemembersTheRunningSpeedLikePauseGame()
        {
            _controller.SetTimeScale(0.25f);
            _controller.SetTimeScale(0f);

            _controller.TogglePause();

            Assert.That(Time.timeScale, Is.EqualTo(0.25f));
        }

        [Test]
        public void ResumeGame_ResetsTheResumeScaleToNormalSpeed()
        {
            _controller.SetTimeScale(2f);
            Assert.That(_controller.ResumeScale, Is.EqualTo(2f), "positive control: a running speed is remembered");

            _controller.ResumeGame();

            Assert.That(_controller.ResumeScale, Is.EqualTo(1f));
            Assert.That(Time.timeScale, Is.EqualTo(1f));
        }
    }
}
