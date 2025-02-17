using UnityEngine;
using UnityEngine.Events;

namespace Neo
{
    [AddComponentMenu("Neoxider/" + "Tools/" + nameof(TimerObject))]
    public class TimerObject : MonoBehaviour
    {
        public float time = 1;
        [Min(0.015f)]
        public float updateTime = 1;

        public bool increasing = true;
        public bool activ = true;
        public bool startOnAwake = true;

        [Space]
        public float timer;

        [Space]
        public UnityEvent<float> OnChange;
        public UnityEvent<float> OnProgress;
        public UnityEvent OnFinish;

        private void Start()
        {
            if (startOnAwake)
            {
                StartTimer(time, updateTime);
            }
        }

        public void StartTimer(float time = 1, float updateTime = 0.015f)
        {
            Stop();

            this.time = time;
            this.updateTime = updateTime;
            activ = true;
            InvokeRepeating(nameof(TimerUpdate), 0, updateTime);
        }

        private void TimerUpdate()
        {
            if (activ)
            {
                timer += updateTime;

                if (timer > time)
                    timer = time;

                Actions();
            }
        }

        private void Actions()
        {
            if (increasing)
            {
                OnChange?.Invoke(timer);
                float progress = timer / time;
                OnProgress?.Invoke(progress);
            }
            else
            {
                float timerReverse = time - timer;
                OnChange?.Invoke(timerReverse);
                float progressReverse = 1 - (timer / time);
                OnProgress?.Invoke(progressReverse);
            }

            if (timer >= time)
            {
                Stop();
                activ = false;
                OnFinish?.Invoke();
            }
        }

        public void Pause(bool pause = true)
        {
            activ = !pause;
        }

        public void Stop()
        {
            CancelInvoke();
            activ = false;
            timer = 0;
        }

        public void Restart()
        {
            StartTimer(time, updateTime);
        }
    }
}