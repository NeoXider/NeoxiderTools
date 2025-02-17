using UnityEngine;
using UnityEngine.Events;

namespace Neo.Tools
{
    [AddComponentMenu("Neoxider/Tools/EvadeManager")]
    public class Evade : MonoBehaviour, ITimerSubscriber
    {
        [SerializeField] private float _evadeDuration = 1f;
        [SerializeField] private float _reloadTime = 2f;
        public bool reloadImmediately = true;
        public bool isEvade = false;

        private Timer _reloadTimer;

        public UnityEvent OnEvadeStarted;
        public UnityEvent<float> OnEvadePercent;
        public UnityEvent OnEvadeCompleted;
        public UnityEvent OnReloadStarted;
        public UnityEvent OnReloadCompleted;

        private void Awake()
        {
            _reloadTimer = new Timer(_reloadTime);

            _reloadTimer.OnTimerStart += OnTimerStart;
            _reloadTimer.OnTimerUpdate += OnTimerUpdate;
            _reloadTimer.OnTimerEnd += OnTimerEnd;
        }

        private void OnDestroy()
        {
            _reloadTimer.OnTimerStart -= OnTimerStart;
            _reloadTimer.OnTimerUpdate -= OnTimerUpdate;
            _reloadTimer.OnTimerEnd -= OnTimerEnd;
        }

        public void StartEvade()
        {
            if (_reloadTimer.IsTimerRunning())
                return;

            isEvade = true;
            OnEvadeStarted.Invoke();
            Invoke(nameof(OnEvadeComplete), _evadeDuration);
            OnReloadStarted.Invoke();

            if (reloadImmediately)
            {
                _reloadTimer.StartTimer();
            }
        }

        private void OnEvadeComplete()
        {
            isEvade = false;
            OnEvadeCompleted.Invoke();

            if (!reloadImmediately)
            {
                _reloadTimer.StartTimer();
            }
        }

        public void OnTimerStart() { }

        public void OnTimerEnd()
        {
            OnReloadCompleted.Invoke();
        }

        public void OnTimerUpdate(float remainingTime, float progress)
        {
            OnEvadePercent.Invoke(progress);
        }
    }
}