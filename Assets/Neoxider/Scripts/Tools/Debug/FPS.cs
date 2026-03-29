using TMPro;
using UnityEngine;

namespace Neo.Tools
{
    /// <summary>
    ///     Displays and monitors FPS (Frames Per Second) in the game.
    ///     Provides visual feedback about performance with color coding.
    /// </summary>
    [NeoDoc("Tools/Debug/FPS.md")]
    [CreateFromMenu("Neoxider/Tools/Debug/FPS")]
    [AddComponentMenu("Neoxider/" + "Tools/" + nameof(FPS))]
    public class FPS : MonoBehaviour
    {
        private const float MinDeltaTime = 1e-5f;
        private const float MaxInstantFps = 100000f;

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

        [Header("Behaviour")]
        [Tooltip(
            "If true, on Awake sets Application.targetFrameRate = -1 and QualitySettings.vSyncCount = 0. Off by default so the counter does not override project quality settings.")]
        [SerializeField]
        private bool unlockFramerateOnAwake;

        private readonly Color criticalColor = Color.red;
        private readonly Color goodColor = Color.green;
        private readonly Color warningColor = Color.yellow;
        private float accumulatedFps;
        private int bufferIndex;
        private float[] fpsBuffer;
        private float nextDisplayTime;
        private int samplesAccumulated;

        [Header("UI Settings")] [Tooltip("Text component to display FPS")]
        [SerializeField]
        private TMP_Text text;

        /// <summary>
        ///     Rolling average FPS over filled samples (same value as shown when the buffer has updated at least once).
        /// </summary>
        public float CurrentFps => GetAverageFps();

        private void Awake()
        {
            if (text == null)
            {
                Debug.LogError($"[{nameof(FPS)}] No text component assigned!");
                enabled = false;
                return;
            }

            RebuildFpsBuffer();

            if (unlockFramerateOnAwake)
            {
                Application.targetFrameRate = -1;
                QualitySettings.vSyncCount = 0;
            }
        }

        private void OnEnable()
        {
            if (text == null)
            {
                return;
            }

            EnsureBufferMatchesSampleSize();
            nextDisplayTime = Time.unscaledTime;
        }

        private void Update()
        {
            if (text == null)
            {
                return;
            }

            EnsureBufferMatchesSampleSize();

            float dt = Time.unscaledDeltaTime;
            if (dt < MinDeltaTime)
            {
                return;
            }

            float instantFps = Mathf.Clamp(1f / dt, 0f, MaxInstantFps);

            accumulatedFps -= fpsBuffer[bufferIndex];
            fpsBuffer[bufferIndex] = instantFps;
            accumulatedFps += instantFps;
            bufferIndex = (bufferIndex + 1) % sampleSize;

            if (samplesAccumulated < sampleSize)
            {
                samplesAccumulated++;
            }

            if (Time.unscaledTime >= nextDisplayTime)
            {
                nextDisplayTime = Time.unscaledTime + Mathf.Max(0.05f, updateInterval);
                UpdateFpsDisplay();
            }
        }

        private void OnValidate()
        {
            updateInterval = Mathf.Max(0.05f, updateInterval);
            sampleSize = Mathf.Max(1, sampleSize);

            if (warningFpsThreshold >= goodFpsThreshold)
            {
                warningFpsThreshold = goodFpsThreshold - 10f;
            }
        }

        private void EnsureBufferMatchesSampleSize()
        {
            sampleSize = Mathf.Max(1, sampleSize);
            if (fpsBuffer == null || fpsBuffer.Length != sampleSize)
            {
                RebuildFpsBuffer();
            }
        }

        private void RebuildFpsBuffer()
        {
            sampleSize = Mathf.Max(1, sampleSize);
            fpsBuffer = new float[sampleSize];
            bufferIndex = 0;
            accumulatedFps = 0f;
            samplesAccumulated = 0;
        }

        private float GetAverageFps()
        {
            if (fpsBuffer == null || samplesAccumulated <= 0)
            {
                return 0f;
            }

            int divisor = Mathf.Min(samplesAccumulated, sampleSize);
            return divisor > 0 ? accumulatedFps / divisor : 0f;
        }

        private void UpdateFpsDisplay()
        {
            float averageFps = GetAverageFps();

            string fpsText = showDecimals
                ? averageFps.ToString("F1")
                : Mathf.RoundToInt(averageFps).ToString();

            if (showSuffix)
            {
                fpsText += " FPS";
            }

            text.text = fpsText;
            text.color = GetFpsColor(averageFps);
        }

        private Color GetFpsColor(float fps)
        {
            if (fps >= goodFpsThreshold)
            {
                return goodColor;
            }

            if (fps >= warningFpsThreshold)
            {
                return warningColor;
            }

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
