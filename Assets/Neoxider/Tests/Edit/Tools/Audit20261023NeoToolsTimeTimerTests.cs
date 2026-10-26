using System.Threading;
using Cysharp.Threading.Tasks;
using Neo.Tools;
using NUnit.Framework;
using Timer = Neo.Tools.Timer;

namespace Neo.Editor.Tests
{
    public class Audit20261023NeoToolsTimeTimerTests
    {
        [Test]
        public void PreCancelledStart_DoesNotStartOrComplete()
        {
            using CancellationTokenSource cancellation = new();
            using Timer timer = new(0f);
            int started = 0;
            int completed = 0;
            timer.OnTimerStart.AddListener(() => started++);
            timer.OnTimerEnd.AddListener(() => completed++);
            cancellation.Cancel();

            UniTask run = timer.StartAsync(cancellation.Token);

            Assert.That(run.Status, Is.EqualTo(UniTaskStatus.Succeeded));
            run.GetAwaiter().GetResult();
            Assert.That(started, Is.Zero);
            Assert.That(completed, Is.Zero);
            Assert.That(timer.IsRunning, Is.False);
        }

        [Test]
        public void StopFromStartHandler_CompletesWithoutCountdownOrCompletionEvent()
        {
            using Timer timer = new(10f);
            int completed = 0;
            timer.OnTimerStart.AddListener(timer.Stop);
            timer.OnTimerEnd.AddListener(() => completed++);

            UniTask run = timer.StartAsync();

            Assert.That(run.Status, Is.EqualTo(UniTaskStatus.Succeeded));
            run.GetAwaiter().GetResult();
            Assert.That(timer.IsRunning, Is.False);
            Assert.That(timer.RemainingTime, Is.EqualTo(10f));
            Assert.That(completed, Is.Zero);
        }

        [Test]
        public void CancellationFromPausedStart_ClearsFlagsWithoutCompletion()
        {
            using CancellationTokenSource cancellation = new();
            using Timer timer = new(0f);
            int completed = 0;
            timer.OnTimerStart.AddListener(() =>
            {
                timer.Pause();
                cancellation.Cancel();
            });
            timer.OnTimerEnd.AddListener(() => completed++);

            timer.StartAsync(cancellation.Token).GetAwaiter().GetResult();

            Assert.That(timer.IsRunning, Is.False);
            Assert.That(timer.IsPaused, Is.False);
            Assert.That(completed, Is.Zero);
        }

        [Test]
        public void LoopCompletion_NotifiesAndAllowsHandlerToStop()
        {
            using Timer timer = new(10f, looping: true);
            int completed = 0;
            timer.OnTimerStart.AddListener(() => timer.SetProgress(1f));
            timer.OnTimerEnd.AddListener(() =>
            {
                completed++;
                timer.Stop();
            });

            UniTask run = timer.StartAsync();

            Assert.That(run.Status, Is.EqualTo(UniTaskStatus.Succeeded));
            run.GetAwaiter().GetResult();
            Assert.That(completed, Is.EqualTo(1));
            Assert.That(timer.IsRunning, Is.False);
            Assert.That(timer.RemainingTime, Is.EqualTo(10f));
        }

        [Test]
        public void CompletionHandler_CanStartAnotherRun()
        {
            using Timer timer = new(0f);
            int started = 0;
            int completed = 0;
            timer.OnTimerStart.AddListener(() =>
            {
                started++;
                if (started == 2)
                {
                    timer.Stop();
                }
            });
            timer.OnTimerEnd.AddListener(() =>
            {
                completed++;
                if (completed == 1)
                {
                    timer.Start();
                }
            });

            timer.StartAsync().GetAwaiter().GetResult();

            Assert.That(started, Is.EqualTo(2));
            Assert.That(completed, Is.EqualTo(1), "The second run is stopped by its start handler.");
            Assert.That(timer.IsRunning, Is.False);
        }

        [Test]
        public void DurationShrink_ClampsRemainingAndProgress()
        {
            using Timer timer = new(10f);
            timer.SetRemainingTime(8f);

            timer.Duration = 2f;

            Assert.That(timer.RemainingTime, Is.EqualTo(2f));
            Assert.That(timer.Progress, Is.Zero);
        }

        [Test]
        public void DurationShrinkFromStartHandler_DoesNotExposeNegativeRemaining()
        {
            using Timer timer = new(10f);
            float remainingAtChange = -1f;
            float progressAtChange = -1f;
            timer.OnTimerStart.AddListener(() =>
            {
                timer.SetRemainingTime(2f);
                timer.Duration = 1f;
                remainingAtChange = timer.RemainingTime;
                progressAtChange = timer.Progress;
            });

            UniTask run = timer.StartAsync();
            Assert.That(run.Status, Is.EqualTo(UniTaskStatus.Succeeded));
            run.GetAwaiter().GetResult();

            Assert.That(remainingAtChange, Is.Zero);
            Assert.That(progressAtChange, Is.EqualTo(1f));
            Assert.That(timer.IsRunning, Is.False);
        }

        [Test]
        public void AddTimeAfterPartialProgress_SubtractsOnceWhenDurationShrinks()
        {
            using Timer timer = new(10f);
            timer.SetRemainingTime(8f);

            timer.AddTime(-5f);

            Assert.That(timer.Duration, Is.EqualTo(5f));
            Assert.That(timer.RemainingTime, Is.EqualTo(3f));
            Assert.That(timer.Progress, Is.EqualTo(0.4f).Within(0.0001f));
        }
    }
}
