#if MIRROR
using System.Collections;
using Mirror;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace Neo.Tests.Play
{
    public sealed class Audit20261023NeoTestsPlayNetworkWaitTests
    {
        private NetworkEventDispatcherTests _fixture;

        [UnityTearDown]
        public IEnumerator CleanupInterruptedFixture()
        {
            if (_fixture != null)
            {
                yield return _fixture.TearDown();
                _fixture = null;
            }
        }

        [Test]
        public void SatisfiedCondition_CompletesWithoutAnExtraFrame()
        {
            IEnumerator wait = NetworkTestHelper.WaitUntil(() => true, 0f);
            Assert.That(wait.MoveNext(), Is.False);
        }

        [Test]
        public void PendingCondition_YieldsUntilItBecomesTrue()
        {
            bool ready = false;
            IEnumerator wait = NetworkTestHelper.WaitUntil(() => ready);
            Assert.That(wait.MoveNext(), Is.True);
            ready = true;
            Assert.That(wait.MoveNext(), Is.False);
        }

        [Test]
        public void ExpiredCondition_FailsWithTheOperationName()
        {
            IEnumerator wait = NetworkTestHelper.WaitUntil(() => false, 0f, "test handshake");
            AssertionException failure = Assert.Throws<AssertionException>(() => wait.MoveNext());
            StringAssert.Contains("test handshake", failure.Message);
            StringAssert.Contains("Server active:", failure.Message);
        }

        [UnityTest]
        public IEnumerator PausedGame_StillExpiresTheNetworkDeadline()
        {
            float previousTimeScale = Time.timeScale;
            Time.timeScale = 0f;
            try
            {
                IEnumerator wait = NetworkTestHelper.WaitUntil(() => false, 0.03f, "paused handshake");
                AssertionException failure = null;
                double outerDeadline = Time.realtimeSinceStartupAsDouble + 1d;
                while (failure == null && Time.realtimeSinceStartupAsDouble < outerDeadline)
                {
                    try
                    {
                        if (!wait.MoveNext())
                        {
                            break;
                        }
                    }
                    catch (AssertionException exception)
                    {
                        failure = exception;
                    }

                    if (failure == null)
                    {
                        yield return null;
                    }
                }

                Assert.That(failure, Is.Not.Null, "A stalled operation must fail even when scaled time is stopped.");
                StringAssert.Contains("paused handshake", failure.Message);
            }
            finally
            {
                Time.timeScale = previousTimeScale;
            }
        }

        [UnityTest]
        public IEnumerator NetworkFixture_TeardownReleasesItsOriginalPlayerTemplate()
        {
            _fixture = new NetworkEventDispatcherTests();
            yield return _fixture.SetUp();
            GameObject managerObject = NetworkManager.singleton.gameObject;
            GameObject playerTemplate = NetworkManager.singleton.playerPrefab;
            Assert.That(playerTemplate, Is.Not.Null);

            yield return _fixture.TearDown();
            _fixture = null;

            Assert.That(managerObject == null, Is.True);
            Assert.That(playerTemplate == null, Is.True,
                "Stopping the host destroys runtime players; the fixture must also release the original template.");
        }
    }
}
#endif
