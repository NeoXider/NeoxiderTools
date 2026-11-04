using System.Collections;
using Neo.Condition;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
#if MIRROR
using Mirror;
#endif

namespace Neo.Tests.Play
{
    /// <summary>
    ///     Native PlayerLoop checks for NeoCondition scheduling while <see cref="Time.timeScale"/> is 0 (pause menus):
    ///     EveryFrame keeps checking (unscaled throttle), Interval keeps checking only with UseUnscaledTime, and the
    ///     default scaled Interval keeps its paused contract.
    /// </summary>
    public sealed class NeoConditionTimeScaleTests
    {
        private GameObject _root;
        private float _previousTimeScale;

        [SetUp]
        public void SetUp()
        {
            _previousTimeScale = Time.timeScale;
        }

        [TearDown]
        public void TearDown()
        {
            Time.timeScale = _previousTimeScale;
            if (_root != null)
            {
                Object.Destroy(_root);
            }
        }

        [UnityTest]
        public IEnumerator EveryFrame_KeepsCheckingWhileTimeScaleIsZero()
        {
            int checks = 0;
            NeoCondition condition = CreateCondition(CheckMode.EveryFrame, false);
            condition.OnResult.AddListener(_ => checks++);
            _root.SetActive(true);
            yield return null;

            Time.timeScale = 0f;
            yield return WaitRealtime(0.1f);
            int before = checks;
            yield return WaitRealtime(0.3f);

            Assert.That(checks - before, Is.GreaterThan(1),
                "The EveryFrame throttle must use unscaled time, otherwise its gate never reopens while paused.");
        }

        [UnityTest]
        public IEnumerator Interval_UseUnscaledTime_KeepsCheckingWhileTimeScaleIsZero()
        {
            int checks = 0;
            NeoCondition condition = CreateCondition(CheckMode.Interval, true);
            condition.OnResult.AddListener(_ => checks++);
            _root.SetActive(true);
            yield return null;

            Time.timeScale = 0f;
            yield return WaitRealtime(0.1f);
            int before = checks;
            yield return WaitRealtime(0.4f);

            Assert.That(condition.UseUnscaledTime, Is.True);
            Assert.That(checks - before, Is.GreaterThan(1), "Real-time intervals continue while the game is paused.");
        }

        [UnityTest]
        public IEnumerator Interval_ScaledDefault_PausesWhileTimeScaleIsZero()
        {
            int checks = 0;
            NeoCondition condition = CreateCondition(CheckMode.Interval, false);
            condition.OnResult.AddListener(_ => checks++);
            _root.SetActive(true);
            yield return null;

            Time.timeScale = 0f;
            yield return WaitRealtime(0.1f);
            int before = checks;
            yield return WaitRealtime(0.4f);

            Assert.That(checks, Is.EqualTo(before), "The default Interval mode keeps counting game time.");
            Time.timeScale = 1f;
            yield return WaitRealtime(0.3f);
            Assert.That(checks, Is.GreaterThan(before), "Positive control: checks resume with game time.");
        }

        private NeoCondition CreateCondition(CheckMode mode, bool unscaled)
        {
            _root = new GameObject(nameof(NeoConditionTimeScaleTests));
            _root.SetActive(false);
#if MIRROR
            _root.AddComponent<NetworkIdentity>();
#endif
            NeoCondition condition = _root.AddComponent<NeoCondition>();
            condition.OnlyOnChange = false;
            condition.CheckInterval = 0.05f;
            condition.UseUnscaledTime = unscaled;
            condition.Mode = mode;
            return condition;
        }

        private static IEnumerator WaitRealtime(float seconds)
        {
            float until = Time.realtimeSinceStartup + seconds;
            while (Time.realtimeSinceStartup < until)
            {
                yield return null;
            }
        }
    }
}
