using System.Collections.Generic;
using System.Reflection;
using Neo.Core.Level;
using NUnit.Framework;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Neo.Editor.Tests
{
    /// <summary>
    ///     <see cref="LevelComponent" /> reactive states around a direct <see cref="LevelComponent.SetLevel" />:
    ///     the model moves the total XP to the level's threshold, and <see cref="LevelComponent.XpState" /> code
    ///     listeners must receive that jump, not only <see cref="LevelComponent.LevelState" /> listeners.
    /// </summary>
    [TestFixture]
    public class LevelComponentReactiveStateTests
    {
        private GameObject _host;
        private LevelComponent _level;

        [SetUp]
        public void SetUp()
        {
            _host = new GameObject(nameof(LevelComponentReactiveStateTests));
            _host.SetActive(false);
            _level = _host.AddComponent<LevelComponent>();
            Configure("_loadOnAwake", false);
            Configure("_autoSave", false);
            _level.EnsureInitialized();
        }

        [TearDown]
        public void TearDown()
        {
            Object.DestroyImmediate(_host);
        }

        [Test]
        public void SetLevel_JumpUp_NotifiesXpStateWithTheLevelThreshold()
        {
            List<int> xp = new();
            List<int> levels = new();
            _level.XpState.AddListener(xp.Add);
            _level.LevelState.AddListener(levels.Add);

            _level.SetLevel(5);

            Assert.That(xp, Is.Not.Empty, "XpState listeners must see the XP jump caused by SetLevel.");
            Assert.That(xp[xp.Count - 1], Is.EqualTo(400));
            Assert.That(levels[levels.Count - 1], Is.EqualTo(5));
            Assert.That(_level.XpStateValue, Is.EqualTo(400));
            Assert.That(_level.TotalXp, Is.EqualTo(400));
        }

        [Test]
        public void SetLevel_JumpDown_NotifiesXpStateWithTheLowerThreshold()
        {
            _level.AddXp(950);
            Assert.That(_level.Level, Is.EqualTo(10), "Control: the default linear curve gives 100 XP per level.");
            List<int> xp = new();
            _level.XpState.AddListener(xp.Add);

            _level.SetLevel(2);

            Assert.That(xp, Is.Not.Empty);
            Assert.That(xp[xp.Count - 1], Is.EqualTo(100));
            Assert.That(_level.XpToNextLevelStateValue, Is.EqualTo(100));
        }

        [Test]
        public void SetLevel_SameLevel_ResetsXpToTheThresholdAndNotifiesXpState()
        {
            _level.AddXp(450);
            Assert.That(_level.Level, Is.EqualTo(5));
            int levelUps = 0;
            List<int> xp = new();
            _level.OnLevelUp.AddListener(level => levelUps++);
            _level.XpState.AddListener(xp.Add);

            _level.SetLevel(5);

            Assert.That(levelUps, Is.Zero, "The level did not change, so there is no level event.");
            Assert.That(xp, Is.Not.Empty, "The XP still moved from 450 to the level's threshold.");
            Assert.That(xp[xp.Count - 1], Is.EqualTo(400));
        }

        private void Configure(string name, object value)
        {
            FieldInfo field = typeof(LevelComponent).GetField(name, BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.That(field, Is.Not.Null, "LevelComponent." + name + " was renamed; update this test.");
            field.SetValue(_level, value);
        }
    }
}
