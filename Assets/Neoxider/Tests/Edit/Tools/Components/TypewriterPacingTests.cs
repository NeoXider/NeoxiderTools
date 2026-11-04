using System.Reflection;
using Neo.Tools;
using NUnit.Framework;

namespace Neo.Editor.Tests.Tools
{
    /// <summary>
    ///     The typewriter carries each wait's overshoot into the next character, so a per-character delay shorter
    ///     than a frame keeps the configured rate instead of typing one character per frame; the carry is capped, so
    ///     one stalled frame does not reveal the rest of the line at once.
    /// </summary>
    public sealed class TypewriterPacingTests
    {
        private const float DurationSeconds = 2f;

        private static readonly MethodInfo TakeWaitMethod = typeof(TypewriterEffect).GetMethod("TakeWait",
            BindingFlags.Static | BindingFlags.NonPublic);

        private static readonly MethodInfo OvershootAfterWaitMethod = typeof(TypewriterEffect).GetMethod(
            "OvershootAfterWait", BindingFlags.Static | BindingFlags.NonPublic);

        private static readonly FieldInfo MaxCatchUpSecondsField = typeof(TypewriterEffect).GetField(
            "MaxCatchUpSeconds", BindingFlags.Static | BindingFlags.NonPublic);

        private enum OvershootMode
        {
            Production,
            Dropped,
            Uncapped
        }

        [TestCase(70f, 60)]
        [TestCase(70f, 30)]
        [TestCase(200f, 60)]
        [TestCase(45f, 60)]
        [TestCase(10f, 60)]
        public void FramePacedWaits_KeepTheConfiguredRate(float charactersPerSecond, int framesPerSecond)
        {
            int typed = SimulateTypedCharacters(charactersPerSecond, framesPerSecond, true);
            // WHY: the first character is typed on frame 0, then the remaining frames add their elapsed time.
            int frames = (int)(framesPerSecond * DurationSeconds);
            double expected = 1d + charactersPerSecond * (frames - 1) / framesPerSecond;

            Assert.That((double)typed, Is.InRange(expected - 2d, expected + 1d),
                $"{charactersPerSecond} cps at {framesPerSecond} fps must type about {expected:0.#} characters.");
        }

        [Test]
        public void DroppingTheOvershoot_CapsTheRateAtOneCharacterPerFrame()
        {
            int typed = SimulateTypedCharacters(70f, 60, false);

            Assert.That(typed, Is.LessThanOrEqualTo(60 * (int)DurationSeconds + 1),
                "positive control: the simulation reproduces the former one-character-per-frame cap");
        }

        [Test]
        public void TakeWait_DelayBelowHalfAMillisecond_DoesNotWait()
        {
            float overshoot = 0f;

            Assert.That(TakeWait(0.0002f, ref overshoot), Is.Zero, "Very fast runs still complete synchronously.");
            Assert.That(overshoot, Is.Zero);
        }

        [Test]
        public void TakeWait_SpendsTheOvershootBeforeWaitingAgain()
        {
            float overshoot = 0.03f;

            Assert.That(TakeWait(0.01f, ref overshoot), Is.Zero);
            Assert.That(overshoot, Is.EqualTo(0.02f).Within(1e-6f));
            Assert.That(TakeWait(0.01f, ref overshoot), Is.Zero);
            Assert.That(overshoot, Is.EqualTo(0.01f).Within(1e-6f));
            // WHY: 0.03 s of overshoot pays for exactly three 0.01 s characters; the float rest is below the minimum wait.
            Assert.That(TakeWait(0.01f, ref overshoot), Is.Zero);
            Assert.That(overshoot, Is.Zero.Within(1e-6f));
            Assert.That(TakeWait(0.01f, ref overshoot), Is.EqualTo(0.01f).Within(1e-5f));
            Assert.That(overshoot, Is.Zero);
        }

        [Test]
        public void OvershootAfterWait_ClockThatDidNotAdvance_FallsBackToOneDelayPerCharacter()
        {
            Assert.That(OvershootAfterWait(0.1f, 0f), Is.Zero, "A frozen clock must not stretch later waits.");
            Assert.That(OvershootAfterWait(0.1f, 0.125f), Is.EqualTo(0.025f).Within(1e-6f));
        }

        [Test]
        public void OvershootAfterWait_LongFrame_IsCappedAtTheCatchUpLimit()
        {
            float cap = MaxCatchUpSeconds();

            Assert.That(cap, Is.GreaterThan(0f).And.LessThanOrEqualTo(0.1f));
            Assert.That(OvershootAfterWait(1f / 70f, 2f), Is.EqualTo(cap), "A stalled frame carries at most the cap.");
            Assert.That(OvershootAfterWait(1f / 70f, 30f), Is.EqualTo(cap), "An app resume carries at most the cap.");
        }

        [TestCase(70f, 2f)]
        [TestCase(70f, 30f)]
        [TestCase(200f, 2f)]
        public void StalledFrame_TypesAtMostTheCatchUpBurst(float charactersPerSecond, float stallSeconds)
        {
            float[] frames = StalledFrames(stallSeconds);
            int bound = 2 + (int)System.Math.Ceiling(MaxCatchUpSeconds() * charactersPerSecond);

            int[] typed = SimulateFrames(charactersPerSecond, frames, OvershootMode.Production);
            int[] uncapped = SimulateFrames(charactersPerSecond, frames, OvershootMode.Uncapped);

            Assert.That(uncapped[StallFrameIndex], Is.GreaterThan(bound * 4),
                "positive control: an uncapped carry types the whole stall in one frame");
            Assert.That(typed[StallFrameIndex], Is.LessThanOrEqualTo(bound),
                $"{charactersPerSecond} cps after a {stallSeconds} s frame must type at most {bound} characters.");
            Assert.That(typed[StallFrameIndex + 1], Is.LessThanOrEqualTo(2 + (int)(charactersPerSecond / 60f)),
                "The frame after the stall is back to the normal pace.");
        }

        /// <summary>Total characters typed in <see cref="DurationSeconds" /> of fixed-length frames.</summary>
        private static int SimulateTypedCharacters(float charactersPerSecond, int framesPerSecond,
            bool carryOvershoot)
        {
            int frames = (int)(framesPerSecond * DurationSeconds);
            float[] frameLengths = new float[frames];
            for (int f = 0; f < frames; f++)
            {
                frameLengths[f] = 1f / framesPerSecond;
            }

            int[] typedPerFrame = SimulateFrames(charactersPerSecond, frameLengths,
                carryOvershoot ? OvershootMode.Production : OvershootMode.Dropped);
            int typed = 0;
            for (int f = 0; f < typedPerFrame.Length; f++)
            {
                typed += typedPerFrame[f];
            }

            return typed;
        }

        /// <summary>
        ///     Characters typed in each frame of <paramref name="frameLengths" />; frame 0 starts the run. A wait
        ///     resumes like UniTask.Delay: on the first later frame whose summed frame time reaches it.
        /// </summary>
        private static int[] SimulateFrames(float charactersPerSecond, float[] frameLengths, OvershootMode mode)
        {
            float delay = 1f / charactersPerSecond;
            int[] typedPerFrame = new int[frameLengths.Length];
            float overshoot = 0f;
            float pendingWait = 0f;
            float waited = 0f;
            bool waiting = false;
            for (int f = 0; f < frameLengths.Length; f++)
            {
                if (waiting)
                {
                    waited += frameLengths[f];
                    if (waited < pendingWait)
                    {
                        continue;
                    }

                    overshoot = mode switch
                    {
                        OvershootMode.Production => OvershootAfterWait(pendingWait, waited),
                        OvershootMode.Uncapped => waited - pendingWait,
                        _ => 0f
                    };
                    waiting = false;
                }

                int guard = 0;
                while (!waiting && guard++ < 100000)
                {
                    typedPerFrame[f]++;
                    float wait = TakeWait(delay, ref overshoot);
                    if (wait > 0f)
                    {
                        waiting = true;
                        pendingWait = wait;
                        waited = 0f;
                    }
                }
            }

            return typedPerFrame;
        }

        private const int StallFrameIndex = 30;

        private static float[] StalledFrames(float stallSeconds)
        {
            float[] frames = new float[StallFrameIndex + 30];
            for (int f = 0; f < frames.Length; f++)
            {
                frames[f] = 1f / 60f;
            }

            frames[StallFrameIndex] = stallSeconds;
            return frames;
        }

        private static float MaxCatchUpSeconds()
        {
            Assert.That(MaxCatchUpSecondsField, Is.Not.Null, "TypewriterEffect.MaxCatchUpSeconds expected");
            return (float)MaxCatchUpSecondsField.GetRawConstantValue();
        }

        private static float TakeWait(float delay, ref float overshoot)
        {
            Assert.That(TakeWaitMethod, Is.Not.Null, "TypewriterEffect.TakeWait(float, ref float) expected");
            object[] arguments = { delay, overshoot };
            float wait = (float)TakeWaitMethod.Invoke(null, arguments);
            overshoot = (float)arguments[1];
            return wait;
        }

        private static float OvershootAfterWait(float wait, float elapsed)
        {
            Assert.That(OvershootAfterWaitMethod, Is.Not.Null, "TypewriterEffect.OvershootAfterWait expected");
            return (float)OvershootAfterWaitMethod.Invoke(null, new object[] { wait, elapsed });
        }
    }
}
