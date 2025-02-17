//v.1.0.1
using System;
using System.Threading;
using System.Threading.Tasks;

namespace Neo
{
    public interface ITimerSubscriber
    {
        public void OnTimerStart();
        public void OnTimerEnd();
        public void OnTimerUpdate(float remainingTime, float progress);
    }

    public class Timer
    {
        ///by   Neoxider
        /// <summary>
        /// </summary>
        ///<example>
        ///<code>
        /// <![CDATA[
        ///     NewTimer timer = new NewTimer(1, 0.05f);
        ///     timer.OnTimerStart  += ()            => Debug.Log("Timer started");
        ///     timer.OnTimerUpdate += remainingTime => Debug.Log("Remaining time: " + remainingTime);
        ///     timer.OnTimerEnd    += ()            => Debug.Log("Timer ended");
        /// ]]>
        ///</code>
        ///</example>
        public event Action OnTimerStart;
        public event Action OnTimerEnd;
        public event Action<float, float> OnTimerUpdate;

        private float duration;
        private float updateInterval = 0.05f;
        private bool isRunning;

        private CancellationTokenSource cancellationTokenSource;

        public Timer(float duration, float updateInterval = 0.05f)
        {
            this.duration = duration;
            this.updateInterval = updateInterval;
        }

        public void ResetTimer(float newDuration, float newUpdateInterval = 0.05f)
        {
            StopTimer();
            duration = newDuration;
            updateInterval = newUpdateInterval;
        }

        public async Task StartTimer()
        {
            if (isRunning) return;

            isRunning = true;
            OnTimerStart?.Invoke();

            cancellationTokenSource = new CancellationTokenSource();
            try
            {
                await TimerCoroutine(cancellationTokenSource.Token);
                OnTimerEnd?.Invoke();
            }
            catch (OperationCanceledException)
            {
                // Таймер был отменен
            }
            finally
            {
                isRunning = false;
            }
        }

        public void StopTimer()
        {
            if (isRunning && cancellationTokenSource != null)
            {
                cancellationTokenSource.Cancel();
                cancellationTokenSource.Dispose();
                cancellationTokenSource = null;
            }
        }

        public async Task RestartTimer()
        {
            StopTimer();
            await StartTimer();
        }

        public bool IsTimerRunning()
        {
            return isRunning;
        }

        private async Task TimerCoroutine(CancellationToken cancellationToken)
        {
            float remainingTime = duration;

            while (remainingTime > 0 && !cancellationToken.IsCancellationRequested)
            {
                await Task.Delay(TimeSpan.FromSeconds(updateInterval), cancellationToken);
                remainingTime -= updateInterval;
                float progress = (duration - remainingTime) / duration;
                OnTimerUpdate?.Invoke(remainingTime, progress);
            }
        }
    }
}
