using System;
using Neo.Tests.Performance;
using Neo.Tools;
using NUnit.Framework;

namespace Neo.Editor.Tests
{
    public sealed class TimerTextFormatterTests
    {
        [TestCase("{0:F1}s")]
        [TestCase("{0:F1}s {2:F0}%")]
        [TestCase("{0,7:F2}|{1,-6:F1}|")]
        [TestCase("{{time}} {0:0.#} of {1:P0}")]
        [TestCase("{2:N0} points")]
        [TestCase("{0}")]
        [TestCase("no placeholders")]
        [TestCase("")]
        [TestCase("{0,2000}")]
        public void FloatFormats_MatchStringFormat(string format)
        {
            TimerTextFormatter formatter = new();
            float[][] samples =
            {
                new[] { 0f, 0f, 0f },
                new[] { 12.345f, 0.5f, 50f },
                new[] { -3.75f, 1f, 1234567f }
            };

            foreach (float[] sample in samples)
            {
                formatter.Format(format, sample[0], sample[1], sample[2]);
                Assert.That(formatter.ToString(), Is.EqualTo(string.Format(format, sample[0], sample[1], sample[2])));
            }
        }

        [TestCase("{0:00}:{1:00}")]
        [TestCase("{0:00}:{1:00}:{2:00}")]
        [TestCase("{0:D3} | {2,4}")]
        public void IntegerFormats_MatchStringFormat(string format)
        {
            TimerTextFormatter formatter = new();

            formatter.Format(format, 7, 5, 42);

            Assert.That(formatter.ToString(), Is.EqualTo(string.Format(format, 7, 5, 42)));
        }

        [Test]
        public void Format_ReportsOnlyRealChanges()
        {
            TimerTextFormatter formatter = new();

            Assert.That(formatter.Format("{0:F1}", 1.01f, 0f, 0f), Is.True);
            Assert.That(formatter.Format("{0:F1}", 1.04f, 0f, 0f), Is.False, "same rounded text");
            Assert.That(formatter.Format("{0:F1}", 1.06f, 0f, 0f), Is.True);
            formatter.Invalidate();
            Assert.That(formatter.Format("{0:F1}", 1.06f, 0f, 0f), Is.True);
        }

        [Test]
        public void UnusualSyntax_FallsBackAndInvalidIndexThrowsLikeStringFormat()
        {
            TimerTextFormatter formatter = new();

            formatter.Format("{0,2000}", 1f, 0f, 0f);
            Assert.That(formatter.IsAllocationFree, Is.False);
            formatter.Format("{0:F1}", 1f, 0f, 0f);
            Assert.That(formatter.IsAllocationFree, Is.True);
            Assert.Throws<FormatException>(() => formatter.Format("{3}", 1f, 2f, 3f));
            Assert.That(formatter.Format(null, 1f, 2f, 3f), Is.True);
            Assert.That(formatter.Length, Is.Zero);
        }

        [Test]
        public void WarmFormatting_DoesNotAllocate()
        {
            // WHY: One formatter per format string, like TimerObject; switching formats re-parses (allocates).
            TimerTextFormatter seconds = new();
            TimerTextFormatter clock = new();
            for (int i = 0; i < 8; i++)
            {
                seconds.Format("{0:F1}s {2:F0}%", i * 1.25f, 0.5f, i * 10f);
                clock.Format("{0:00}:{1:00}", i, i * 2, 0);
            }

            using GcAllocCounter allocationCounter = GcAllocCounter.CreateOrIgnore();
            allocationCounter.Begin();
            for (int i = 0; i < 200; i++)
            {
                seconds.Format("{0:F1}s {2:F0}%", i * 0.1f, 0.5f, i * 0.5f);
                clock.Format("{0:00}:{1:00}", i % 24, i % 60, 0);
            }

            long allocated = allocationCounter.End();
            Assert.That(allocated, Is.Zero);
        }
    }
}
