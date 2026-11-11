using System.Globalization;
using Neo.Tools.View;
using UnityEngine;
using UnityEngine.UI;

namespace Neo.Samples
{
    /// <summary>
    ///     Controller of the authored <b>Neo.Tools.View</b> light demo (Light.unity). The five hand-authored point
    ///     lights each carry a <see cref="LightAnimator" /> with a different <see cref="Neo.Animations.AnimationType" />
    ///     and a sphere whose <see cref="MeshEmission" /> follows the light. The control card, the reactive
    ///     intensity readout and the info card are saved in the scene; the buttons,
    ///     sliders and toggles call the public methods below through persistent UnityEvents. The component only
    ///     forwards to the real <see cref="LightAnimator" /> API (Play / Pause / Stop, speed and range, the manual
    ///     <see cref="LightAnimator.Tick" /> clock, unscaled time) and logs every call. It builds nothing at runtime.
    /// </summary>
    [AddComponentMenu("Neoxider/Demos/Light Demo")]
    public sealed class LightDemoController : MonoBehaviour
    {
        [Header("Scene")]
        [Tooltip("Animators driven by the card (the hand-authored lights of the scene).")]
        [SerializeField] private LightAnimator[] _lights = new LightAnimator[0];

        [Header("Controls synced on start")]
        [SerializeField] private Slider _speedSlider;
        [SerializeField] private Slider _maxIntensitySlider;
        [SerializeField] private Slider _timeScaleSlider;
        [SerializeField] private Toggle _manualClockToggle;
        [SerializeField] private Toggle _unscaledTimeToggle;

        [Header("Feed")]
        [SerializeField] private DemoLogFeed _log;

        private float _timeScaleOnStart = 1f;
        private bool _changedTimeScale;
        private bool _warnedUnbuilt;

        /// <summary>True when the authored scene references are assigned.</summary>
        public bool IsSceneBuilt => _lights != null && _lights.Length > 0 && _log != null && _speedSlider != null;

        /// <summary>Animators driven by the demo.</summary>
        public LightAnimator[] Lights => _lights;

        /// <summary>Whether the demo currently advances the animators itself through <see cref="LightAnimator.Tick" />.</summary>
        public bool ManualClock { get; private set; }

        private void Start()
        {
            if (!IsSceneBuilt)
            {
                WarnUnbuilt();
            }

            _timeScaleOnStart = Time.timeScale;
            if (_timeScaleSlider != null)
            {
                _timeScaleSlider.SetValueWithoutNotify(Time.timeScale);
            }

            if (_manualClockToggle != null)
            {
                _manualClockToggle.SetIsOnWithoutNotify(false);
            }

            LightAnimator first = FirstLight();
            if (first != null)
            {
                if (_speedSlider != null)
                {
                    _speedSlider.SetValueWithoutNotify(first.AnimationSpeed);
                }

                if (_maxIntensitySlider != null)
                {
                    _maxIntensitySlider.SetValueWithoutNotify(first.MaxIntensity);
                }

                if (_unscaledTimeToggle != null)
                {
                    _unscaledTimeToggle.SetIsOnWithoutNotify(first.UseUnscaledTime);
                }
            }

            Log(CountLights() + " LightAnimators play five AnimationTypes; spheres follow through MeshEmission");
        }

        private void OnDisable()
        {
            // WHY: Time.timeScale is global; a demo must not leave the next scene in slow motion.
            if (_changedTimeScale)
            {
                Time.timeScale = _timeScaleOnStart;
                _changedTimeScale = false;
            }
        }

        /// <summary>Button target: <see cref="LightAnimator.Play" /> on every light.</summary>
        public void PlayAll()
        {
            ForEachLight(LightCall.Play);
            Log("LightAnimator.Play() ×" + CountLights());
        }

        /// <summary>Button target: <see cref="LightAnimator.Pause" /> on every light.</summary>
        public void PauseAll()
        {
            ForEachLight(LightCall.Pause);
            Log("LightAnimator.Pause() - lights hold their current intensity");
        }

        /// <summary>Button target: <see cref="LightAnimator.Resume" /> on every light.</summary>
        public void ResumeAll()
        {
            ForEachLight(LightCall.Resume);
            Log("LightAnimator.Resume()");
        }

        /// <summary>Button target: <see cref="LightAnimator.Stop" /> then <see cref="LightAnimator.ResetToOriginal" />.</summary>
        public void StopAll()
        {
            ForEachLight(LightCall.Stop);
            Log("LightAnimator.Stop() + ResetToOriginal() - back to the authored baseline");
        }

        /// <summary>Slider target: <see cref="LightAnimator.AnimationSpeed" /> of every light.</summary>
        public void SetSpeed(float speed)
        {
            float value = Mathf.Max(0f, speed);
            for (int i = 0; i < _lights.Length; i++)
            {
                if (_lights[i] != null)
                {
                    _lights[i].AnimationSpeed = value;
                }
            }

            Log("AnimationSpeed = " + Format(value));
        }

        /// <summary>Slider target: <see cref="LightAnimator.MaxIntensity" /> of every light (kept above the minimum).</summary>
        public void SetMaxIntensity(float maxIntensity)
        {
            for (int i = 0; i < _lights.Length; i++)
            {
                LightAnimator light = _lights[i];
                if (light != null)
                {
                    light.MaxIntensity = Mathf.Max(light.MinIntensity, maxIntensity);
                }
            }

            Log("MaxIntensity = " + Format(maxIntensity));
        }

        /// <summary>
        ///     Toggle target: with the manual clock on, <see cref="LightAnimator.ManualUpdate" /> stops the animators'
        ///     own Update and only <see cref="Step" /> advances them through <see cref="LightAnimator.Tick" />.
        /// </summary>
        public void SetManualClock(bool manual)
        {
            ManualClock = manual;
            for (int i = 0; i < _lights.Length; i++)
            {
                if (_lights[i] != null)
                {
                    _lights[i].ManualUpdate = manual;
                }
            }

            Log(manual
                ? "ManualUpdate = true - lights freeze until you Step them"
                : "ManualUpdate = false - lights run on their own Update again");
        }

        /// <summary>Button target: advances every light by <paramref name="seconds" /> through <see cref="LightAnimator.Tick" />.</summary>
        public void Step(float seconds)
        {
            for (int i = 0; i < _lights.Length; i++)
            {
                if (_lights[i] != null)
                {
                    _lights[i].Tick(seconds);
                }
            }

            Log("LightAnimator.Tick(" + Format(seconds) + ")" + (ManualClock ? string.Empty : " (turn on Manual clock to see single steps)"));
        }

        /// <summary>Slider target: global <see cref="Time.timeScale" /> (restored when the demo is disabled).</summary>
        public void SetTimeScale(float timeScale)
        {
            Time.timeScale = Mathf.Clamp(timeScale, 0f, 1f);
            _changedTimeScale = true;
            Log("Time.timeScale = " + Format(Time.timeScale));
        }

        /// <summary>Toggle target: <see cref="LightAnimator.UseUnscaledTime" /> of every light.</summary>
        public void SetUnscaledTime(bool unscaled)
        {
            for (int i = 0; i < _lights.Length; i++)
            {
                if (_lights[i] != null)
                {
                    _lights[i].UseUnscaledTime = unscaled;
                }
            }

            Log(unscaled
                ? "UseUnscaledTime = true - lights ignore Time.timeScale"
                : "UseUnscaledTime = false - lights follow Time.timeScale");
        }

        private enum LightCall
        {
            Play,
            Pause,
            Resume,
            Stop
        }

        private void ForEachLight(LightCall call)
        {
            for (int i = 0; i < _lights.Length; i++)
            {
                LightAnimator light = _lights[i];
                if (light == null)
                {
                    continue;
                }

                switch (call)
                {
                    case LightCall.Play:
                        light.Play();
                        break;
                    case LightCall.Pause:
                        light.Pause();
                        break;
                    case LightCall.Resume:
                        light.Resume();
                        break;
                    case LightCall.Stop:
                        light.Stop();
                        light.ResetToOriginal();
                        break;
                }
            }
        }

        private LightAnimator FirstLight()
        {
            for (int i = 0; i < _lights.Length; i++)
            {
                if (_lights[i] != null)
                {
                    return _lights[i];
                }
            }

            return null;
        }

        private int CountLights()
        {
            int count = 0;
            for (int i = 0; i < _lights.Length; i++)
            {
                if (_lights[i] != null)
                {
                    count++;
                }
            }

            return count;
        }

        private static string Format(float value)
        {
            return value.ToString("0.##", CultureInfo.InvariantCulture);
        }

        private void Log(string message)
        {
            if (_log != null)
            {
                _log.Log(message);
            }
        }

        private void WarnUnbuilt()
        {
            if (_warnedUnbuilt)
            {
                return;
            }

            _warnedUnbuilt = true;
            global::Neo.Demo.SampleDiagnostics.Warn(
                "[LightDemoController] The scene card is not authored. Re-import the scene from the Demo download.",
                this);
        }
    }
}
