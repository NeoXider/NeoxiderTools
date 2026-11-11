using UnityEngine;

namespace Neo.Tools
{
    /// <summary>
    ///     Utility to control game time (timeScale) via UnityEvents. Perfect for NoCode pausing.
    /// </summary>
    [NeoDoc("Tools/Time/GameTimeController.md")]
    [CreateFromMenu("Neoxider/Tools/GameTimeController")]
    [AddComponentMenu("Neoxider/Tools/" + nameof(GameTimeController))]
    public sealed class GameTimeController : MonoBehaviour
    {
        [Tooltip("If true, automatically resets timeScale to 1 when this component is destroyed/Awake.")]
        [SerializeField]
        private bool resetOnAwake = true;

        // WHY: The last running speed this controller set or observed. ResumeGame resets it to 1 and every non-zero
        // SetTimeScale records it, so TogglePause never resumes to a slow-motion value from an earlier, finished pause.
        private float _lastRunningScale = 1f;

        /// <summary>True while <c>Time.timeScale</c> is zero.</summary>
        public bool IsPaused => Time.timeScale == 0f;

        /// <summary>
        ///     Speed <see cref="TogglePause" /> resumes to: the last non-zero scale set through this controller (or
        ///     observed when it paused), and 1 after <see cref="ResumeGame" />.
        /// </summary>
        public float ResumeScale => _lastRunningScale;

        private void Awake()
        {
            if (resetOnAwake)
            {
                ResumeGame();
            }
        }

        private void OnDestroy()
        {
            if (resetOnAwake)
            {
                ResumeGame();
            }
        }

        /// <summary>
        ///     Sets Time.timeScale to 0.
        /// </summary>
        public void PauseGame()
        {
            RememberRunningScale();
            Time.timeScale = 0f;
        }

        /// <summary>
        ///     Sets Time.timeScale to 1.
        /// </summary>
        public void ResumeGame()
        {
            _lastRunningScale = 1f;
            Time.timeScale = 1f;
        }

        /// <summary>
        ///     Pauses, or resumes to <see cref="ResumeScale" />: the speed that was running before the pause (for
        ///     example a slow-motion scale). One-button NoCode pause toggle.
        /// </summary>
        public void TogglePause()
        {
            if (IsPaused)
            {
                Time.timeScale = _lastRunningScale > 0f ? _lastRunningScale : 1f;
                return;
            }

            PauseGame();
        }

        /// <summary>
        ///     Sets Time.timeScale to a finite non-negative value. Non-finite inputs leave the speed unchanged.
        ///     A non-zero value becomes <see cref="ResumeScale" />; zero pauses like <see cref="PauseGame" />.
        /// </summary>
        public void SetTimeScale(float scale)
        {
            if (float.IsNaN(scale) || float.IsInfinity(scale))
            {
                return;
            }

            float clamped = Mathf.Max(0f, scale);
            if (clamped > 0f)
            {
                _lastRunningScale = clamped;
            }
            else
            {
                RememberRunningScale();
            }

            Time.timeScale = clamped;
        }

        private void RememberRunningScale()
        {
            float current = Time.timeScale;
            if (current > 0f)
            {
                _lastRunningScale = current;
            }
        }
    }
}
