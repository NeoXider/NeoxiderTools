using UnityEngine;

namespace Neo.Tools
{
    /// <summary>
    ///     Utility to control game time (timeScale) via UnityEvents. Perfect for NoCode pausing.
    /// </summary>
    [NeoDoc("Tools/GameTimeController.md")]
    [AddComponentMenu("Neoxider/Tools/" + nameof(GameTimeController))]
    public sealed class GameTimeController : MonoBehaviour
    {
        [Tooltip("If true, automatically resets timeScale to 1 when this component is destroyed/Awake.")]
        [SerializeField] private bool resetOnAwake = true;

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
            Time.timeScale = 0f;
        }

        /// <summary>
        ///     Sets Time.timeScale to 1.
        /// </summary>
        public void ResumeGame()
        {
            Time.timeScale = 1f;
        }

        /// <summary>
        ///     Sets Time.timeScale to a specific float value.
        /// </summary>
        public void SetTimeScale(float scale)
        {
            Time.timeScale = Mathf.Max(0f, scale);
        }
    }
}
