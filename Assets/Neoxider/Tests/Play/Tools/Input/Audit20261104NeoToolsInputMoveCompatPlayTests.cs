using System.Collections;
using Neo.Tests.Performance;
using Neo.Tools;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace Neo.Tests.Play
{
    /// <summary>
    ///     <see cref="MoveInputCompat.ReadMove()" /> on a real virtual gamepad through the real PlayerLoop update, with the
    ///     optional backend forced by the driver: it reports the native left stick in the frame it changes, reads a
    ///     release live and allocates nothing once warm.
    /// </summary>
    public sealed class Audit20261104NeoToolsInputMoveCompatPlayTests
    {
        private const int Iterations = 2048;
        private const string MissingAxis = "Neo Missing Move Axis";
        private static readonly Vector2 LeftStickInput = new(-0.55f, 0.4f);

        [SetUp]
        public void SetUp()
        {
            OptionalInputSystemAdapter.ResetStaticState();
        }

        [TearDown]
        public void TearDown()
        {
            OptionalInputSystemAdapter.ResetStaticState();
        }

        [UnityTest]
        public IEnumerator ReadMove_RealVirtualGamepad_MatchesTheStickReadsLiveAndAllocatesNothing()
        {
            using (Audit20261023NeoToolsInteractableObjectInputDriver input = new())
            using (InputVirtualGamepad gamepad = new())
            {
                yield return null;
                yield return null;
                gamepad.Queue(LeftStickInput, Vector2.zero, false);
                yield return null;

                Assert.That(InputBackendCompat.UsesLegacyInput, Is.False, input.Diagnostics);
                Vector2 stick = gamepad.ReadNativeStick("leftStick");
                Assert.That(stick.sqrMagnitude, Is.GreaterThan(0.01f),
                    "Positive control: the queued stick reached the virtual gamepad. " + gamepad.Describe());
                Vector2 move = MoveInputCompat.ReadMove(MissingAxis, MissingAxis);
                Assert.That(move, Is.EqualTo(OptionalInputSystemAdapter.ReadMove()), gamepad.Describe());
                Assert.That(move.x, Is.EqualTo(stick.x), "An idle keyboard leaves the stick in charge.");
                Assert.That(move.y, Is.EqualTo(stick.y));

                Assert.That(Poll(stick, 64), Is.EqualTo(64), gamepad.Describe());
                using (GcAllocCounter counter = GcAllocCounter.CreateOrIgnore())
                {
                    counter.Begin();
                    int correct = Poll(stick, Iterations);
                    long allocationCount = counter.End();
                    Assert.That(correct, Is.EqualTo(Iterations), gamepad.Describe());
                    Assert.That(allocationCount, Is.Zero, "Exactly zero native GC.Alloc calls for warmed ReadMove.");
                }

                gamepad.Queue(Vector2.zero, Vector2.zero, false);
                yield return null;
                Assert.That(MoveInputCompat.ReadMove(), Is.EqualTo(Vector2.zero),
                    "A released stick must be read live. " + gamepad.Describe());
            }
        }

        private static int Poll(Vector2 expected, int count)
        {
            int correct = 0;
            for (int i = 0; i < count; i++)
            {
                Vector2 move = MoveInputCompat.ReadMove();
                if (move.x == expected.x && move.y == expected.y)
                {
                    correct++;
                }
            }

            return correct;
        }
    }
}
