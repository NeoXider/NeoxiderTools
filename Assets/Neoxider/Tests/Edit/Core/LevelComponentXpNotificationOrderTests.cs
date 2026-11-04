using System.Collections.Generic;
using System.Reflection;
using Neo.Core.Level;
using NUnit.Framework;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Neo.Editor.Tests
{
    /// <summary>
    ///     <see cref="LevelComponent" /> publishes the XP and XP-to-next values of a level change before the level
    ///     listeners and <see cref="LevelComponent.OnLevelUp" /> run, so an XP label or progress bar bound next to a
    ///     level label never shows the old values inside a level callback; every <see cref="LevelComponent.AddXp" /> or
    ///     <see cref="LevelComponent.SetLevel" /> call notifies each reactive state exactly once.
    /// </summary>
    [TestFixture]
    public class LevelComponentXpNotificationOrderTests
    {
        private GameObject _host;
        private LevelComponent _level;

        [SetUp]
        public void SetUp()
        {
            _host = new GameObject(nameof(LevelComponentXpNotificationOrderTests));
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
        public void SetLevel_JumpUp_XpListenersHearTheThresholdBeforeLevelCallbacks()
        {
            int lastXpHeard = -1;
            List<int> xpSeenByLevelListener = new();
            List<int> xpSeenByLevelUp = new();
            _level.XpState.AddListener(value => lastXpHeard = value);
            _level.LevelState.AddListener(level => xpSeenByLevelListener.Add(lastXpHeard));
            _level.OnLevelUp.AddListener(level => xpSeenByLevelUp.Add(lastXpHeard));

            _level.SetLevel(5);

            Assert.That(xpSeenByLevelUp, Is.EqualTo(new[] { 400 }),
                "OnLevelUp must run after XpState listeners heard the level's threshold.");
            Assert.That(xpSeenByLevelListener, Is.Not.Empty);
            Assert.That(xpSeenByLevelListener[0], Is.EqualTo(400),
                "The first LevelState notification must already find the new XP published.");
            Assert.That(_level.XpStateValue, Is.EqualTo(400));
        }

        [Test]
        public void SetLevel_JumpDown_XpListenersHearTheLowerThresholdBeforeOnLevelUp()
        {
            _level.AddXp(950);
            Assert.That(_level.Level, Is.EqualTo(10), "Control: the default linear curve gives 100 XP per level.");
            int lastXpHeard = -1;
            int xpSeenByLevelUp = -1;
            _level.XpState.AddListener(value => lastXpHeard = value);
            _level.OnLevelUp.AddListener(level => xpSeenByLevelUp = lastXpHeard);

            _level.SetLevel(2);

            Assert.That(xpSeenByLevelUp, Is.EqualTo(100));
        }

        [Test]
        public void AddXp_CrossingALevel_NotifiesXpStateAsOftenAsAPlainGain()
        {
            List<int> xp = new();
            _level.XpState.AddListener(xp.Add);

            int beforeCrossing = xp.Count;
            _level.AddXp(250);
            int crossing = xp.Count - beforeCrossing;
            Assert.That(_level.Level, Is.EqualTo(3), "Control: 250 XP crosses two levels on the default curve.");

            int beforePlainGain = xp.Count;
            _level.AddXp(10);
            int plainGain = xp.Count - beforePlainGain;
            Assert.That(_level.Level, Is.EqualTo(3), "Control: 10 more XP stays inside level 3.");

            Assert.That(crossing, Is.EqualTo(plainGain),
                "The level change must not add an XpState notification of the total OnXpGained already published.");
            Assert.That(xp[xp.Count - 1], Is.EqualTo(260));
        }

        [Test]
        public void SetLevel_JumpUp_XpToNextListenersHearTheNewRemainderBeforeLevelCallbacks()
        {
            _level.AddXp(150);
            Assert.That(_level.XpToNextLevel, Is.EqualTo(50), "Control: 150 XP leaves 50 XP to level 3.");
            int lastXpToNextHeard = -1;
            List<int> xpToNextSeenByLevelListener = new();
            int xpToNextSeenByLevelUp = -1;
            _level.XpToNextLevelState.AddListener(value => lastXpToNextHeard = value);
            _level.LevelState.AddListener(level => xpToNextSeenByLevelListener.Add(lastXpToNextHeard));
            _level.OnLevelUp.AddListener(level => xpToNextSeenByLevelUp = lastXpToNextHeard);

            _level.SetLevel(5);

            Assert.That(xpToNextSeenByLevelListener, Is.EqualTo(new[] { 100 }),
                "LevelState listeners must find the new XP-to-next value already published.");
            Assert.That(xpToNextSeenByLevelUp, Is.EqualTo(100));
        }

        [Test]
        public void AddXp_CrossingALevel_XpToNextListenersHearTheNewRemainderBeforeLevelCallbacks()
        {
            _level.AddXp(30);
            int lastXpToNextHeard = -1;
            List<int> xpToNextSeenByLevelListener = new();
            _level.XpToNextLevelState.AddListener(value => lastXpToNextHeard = value);
            _level.LevelState.AddListener(level => xpToNextSeenByLevelListener.Add(lastXpToNextHeard));

            _level.AddXp(150);

            Assert.That(_level.Level, Is.EqualTo(2), "Control: 180 XP is level 2 on the default curve.");
            Assert.That(xpToNextSeenByLevelListener, Is.EqualTo(new[] { 20 }));
        }

        [TestCase(5)]
        [TestCase(2)]
        public void SetLevel_NotifiesEachReactiveStateExactlyOnce(int targetLevel)
        {
            _level.AddXp(150);
            List<int> xp = new();
            List<int> xpToNext = new();
            List<int> levels = new();
            _level.XpState.AddListener(xp.Add);
            _level.XpToNextLevelState.AddListener(xpToNext.Add);
            _level.LevelState.AddListener(levels.Add);

            _level.SetLevel(targetLevel);

            int threshold = (targetLevel - 1) * 100;
            Assert.That(xp, Is.EqualTo(new[] { threshold }));
            Assert.That(xpToNext, Is.EqualTo(new[] { 100 }));
            Assert.That(levels, Is.EqualTo(new[] { targetLevel }));
        }

        [TestCase(250)]
        [TestCase(10)]
        public void AddXp_NotifiesEachReactiveStateExactlyOnce(int amount)
        {
            List<int> xp = new();
            List<int> xpToNext = new();
            List<int> levels = new();
            _level.XpState.AddListener(xp.Add);
            _level.XpToNextLevelState.AddListener(xpToNext.Add);
            _level.LevelState.AddListener(levels.Add);

            _level.AddXp(amount);

            Assert.That(xp, Is.EqualTo(new[] { _level.TotalXp }));
            Assert.That(xpToNext, Is.EqualTo(new[] { _level.XpToNextLevel }));
            Assert.That(levels, Is.EqualTo(new[] { _level.Level }));
        }

        [Test]
        public void AddXp_FromAnOnLevelUpReward_PublishesTheFinalValues()
        {
            bool rewarded = false;
            _level.OnLevelUp.AddListener(level =>
            {
                if (!rewarded)
                {
                    rewarded = true;
                    _level.AddXp(30);
                }
            });
            int lastXpHeard = -1;
            int lastXpToNextHeard = -1;
            int lastLevelHeard = -1;
            _level.XpState.AddListener(value => lastXpHeard = value);
            _level.XpToNextLevelState.AddListener(value => lastXpToNextHeard = value);
            _level.LevelState.AddListener(value => lastLevelHeard = value);

            _level.AddXp(100);

            Assert.That(_level.TotalXp, Is.EqualTo(130));
            Assert.That(lastXpHeard, Is.EqualTo(130));
            Assert.That(lastXpToNextHeard, Is.EqualTo(70));
            Assert.That(lastLevelHeard, Is.EqualTo(2));
        }

        private void Configure(string name, object value)
        {
            FieldInfo field = typeof(LevelComponent).GetField(name, BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.That(field, Is.Not.Null, "LevelComponent." + name + " was renamed; update this test.");
            field.SetValue(_level, value);
        }
    }
}
