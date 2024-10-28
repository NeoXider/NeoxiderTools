//v.1.0.1
using System;
using System.Threading;
using System.Threading.Tasks;

namespace Neoxider
{
    public interface ITimerSubscriber
    {
        void OnTimerStart();
        void OnTimerEnd();
        void OnTimerUpdate(float remainingTime, float progress);
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

        public float duration;
        private float updateInterval = 0.1f;
        private bool isRunning;

        public bool allowEventsInEditor = true;

        private CancellationTokenSource cancellationTokenSource;

        public Timer(float duration, float updateInterval = 0.1f)
        {
            this.duration = duration;
            this.updateInterval = updateInterval;
        }

        public void ResetTimer(float newDuration, float newUpdateInterval = 0.1f)
        {
            StopTimer();
            duration = newDuration;
            updateInterval = newUpdateInterval;
        }

        public async void StartTimer()
        {
            if (isRunning)
            {
                await Task.Delay(100);
            }

            if (!isRunning)
            {
                isRunning = true;
#if !UNITY_EDITOR
            OnTimerStart?.Invoke();
#else
                if (allowEventsInEditor)
                {
                    OnTimerStart?.Invoke();
                }
#endif
                cancellationTokenSource = new CancellationTokenSource();
                try
                {
                    await TimerCoroutine(cancellationTokenSource.Token);
#if !UNITY_EDITOR
                OnTimerEnd?.Invoke();
#else
                    if (allowEventsInEditor)
                    {
                        OnTimerEnd?.Invoke();
                    }
#endif
                }
                catch (OperationCanceledException)
                {
                    // Timer was cancelled
                }
                isRunning = false;
            }
        }

        public void StopTimer()
        {
            if (isRunning)
            {
                cancellationTokenSource.Cancel();
            }
        }

        public async void RestartTimer()
        {

            StopTimer();
            await Task.Delay(100);
            cancellationTokenSource = null;
            StartTimer();
        }

        public bool IsTimerRunning()
        {
            return isRunning;
        }

        private async Task TimerCoroutine(CancellationToken cancellationToken)
        {
            float remainingTime = duration;
            var wait = Task.Delay(TimeSpan.FromSeconds(updateInterval), cancellationToken);

            while (remainingTime > 0)
            {
                await wait;
                remainingTime -= updateInterval;
                float progress = (remainingTime / duration);
#if !UNITY_EDITOR
            OnTimerUpdate?.Invoke(remainingTime, progress);
#else
                if (allowEventsInEditor)
                {
                    OnTimerUpdate?.Invoke(remainingTime, progress);
                }
#endif
            }
        }
    }
}