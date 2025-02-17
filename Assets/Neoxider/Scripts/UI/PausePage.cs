using UnityEngine;

namespace Neo
{
    namespace Tools
    {
        public class PausePage : MonoBehaviour
        {
            private float timeScale;

            private void OnEnable()
            {
                timeScale = Time.timeScale;
                Time.timeScale = 0f;
            }

            private void OnDisable()
            {
                Time.timeScale = timeScale;
            }

            private void OnValidate()
            {
                if(TryGetComponent(out Animator animator))
                {
                    animator.updateMode = AnimatorUpdateMode.UnscaledTime;
                }
            }
        }
    }
}