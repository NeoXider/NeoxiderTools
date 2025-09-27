using TMPro;
using UnityEngine;

namespace Neo
{
    /// <summary>
    ///     Displays and monitors FPS (Frames Per Second) in the game.
    ///     Provides visual feedback about performance with color coding.
    /// </summary>
    [AddComponentMenu("Neoxider/Tools/" + nameof(FPS))]
    public class FPS : MonoBehaviour
    {
        [Header("Update Settings")] [Tooltip("How often to update the FPS display (in seconds)")] [SerializeField]
        private float updateInterval = 0.2f;

        [Tooltip("Number of samples to average FPS over")] [SerializeField]
        private int sampleSize = 60;

        [Header("Color Settings")] [Tooltip("FPS threshold for good performance (green)")] [SerializeField]
        private float goodFpsThreshold = 50f;

        [Tooltip("FPS threshold for warning performance (yellow)")] [SerializeField]
        private float warningFpsThreshold = 30f;

        [Header("Format Settings")] [Tooltip("Show decimal places in FPS")] [SerializeField]
        private bool showDecimals;

        [Tooltip("Show 'FPS' suffix in display")] [SerializeField]
        private bool showSuffix = true;

        private readonly Color criticalColor = Color.red;
        private readonly Color goodColor = Color.green;
        private readonly Color warningColor = Color.yellow;
        private float accumulatedFps;
        private int bufferIndex;

        private float[] fpsBuffer;

        [Header("UI Settings")] [Tooltip("Text component to display FPS")]
        private TMP_Text text;

        /// <summary>
        ///     Gets the current average FPS
        /// </summary>
        public float CurrentFps => accumulatedFps / sampleSize;

        private void Awake()
        {
            // Initialize the FPS buffer
            fpsBuffer = new float[sampleSize];
            bufferIndex = 0;
            accumulatedFps = 0f;

            // Ensure we have a text component
            if (text == null)
            {
                Debug.LogError($"[{nameof(FPS)}] No text component assigned!");
                enabled = false;
                return;
            }

            // Set target framerate to maximum
            Application.targetFrameRate = -1; // -1 means no limit
            QualitySettings.vSyncCount = 0; // Disable VSync for accurate measurements
        }

        private void Start()
        {
            InvokeRepeating(nameof(UpdateFpsDisplay), 0, updateInterval);
        }

        private void Update()
        {
            // Update the FPS buffer
            var currentFps = 1f / Time.deltaTime;
            accumulatedFps -= fpsBuffer[bufferIndex];
            fpsBuffer[bufferIndex] = currentFps;
            accumulatedFps += currentFps;
            bufferIndex = (bufferIndex + 1) % sampleSize;
        }

        private void OnValidate()
        {
            if (updateInterval < 0.1f) updateInterval = 0.1f;
            if (sampleSize < 1) sampleSize = 1;
            if (warningFpsThreshold >= goodFpsThreshold)
                warningFpsThreshold = goodFpsThreshold - 10f;
        }

        private void UpdateFpsDisplay()
        {
            var averageFps = accumulatedFps / sampleSize;

            // Format the FPS text
            var fpsText = showDecimals
                ? averageFps.ToString("F1")
                : Mathf.RoundToInt(averageFps).ToString();

            if (showSuffix)
                fpsText += " FPS";

            // Update text and color
            text.text = fpsText;
            text.color = GetFpsColor(averageFps);
        }

        private Color GetFpsColor(float fps)
        {
            if (fps >= goodFpsThreshold)
                return goodColor;
            if (fps >= warningFpsThreshold)
                return warningColor;
            return criticalColor;
        }

        /// <summary>
        ///     Sets the target framerate. Use -1 for unlimited.
        /// </summary>
        public void SetTargetFramerate(int target)
        {
            Application.targetFrameRate = target;
        }

        /// <summary>
        ///     Enables or disables VSync
        /// </summary>
        public void SetVSync(bool enabled)
        {
            QualitySettings.vSyncCount = enabled ? 1 : 0;
        }
    }
}