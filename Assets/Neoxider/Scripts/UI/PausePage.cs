using UnityEngine;

namespace Neo
{
    namespace Tools
    {
        public class PausePage : MonoBehaviour
        {
            [SerializeField] private bool useTimeScale = true;

            [SerializeField] private float timeScale;

            [SerializeField] private bool sendPause = true;

            private float lastTimeScale;

            private void OnEnable()
            {
                lastTimeScale = Time.timeScale;

                if (useTimeScale)
                    Time.timeScale = 0f;

                if (sendPause)
                    GM.I?.Pause();
            }

            private void OnDisable()
            {
                if (useTimeScale)
                    Time.timeScale = lastTimeScale;

                if (sendPause)
                    GM.I?.Resume();
            }

            private void OnValidate()
            {
                if (TryGetComponent(out Animator animator)) animator.updateMode = AnimatorUpdateMode.UnscaledTime;
            }
        }
    }
}