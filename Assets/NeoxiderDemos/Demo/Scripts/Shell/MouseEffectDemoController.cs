using System;
using Neo.Tools;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Neo.Samples
{
    /// <summary>
    ///     Controller of the authored <b>MouseEffect</b> demo (Scenes/Tools/MouseEffectorExample.unity). The three
    ///     hand-authored MouseEffect objects stay as they were; the scene also saves the UI, a
    ///     MouseInputManager with Hold events enabled, the effect on/off toggles (wired straight to
    ///     GameObject.SetActive) and the onSpawn / onStartFollow / onStopFollow log wiring. This component only sets
    ///     the public MouseEffect fields that have no setter method and counts spawns. It builds no UI at runtime.
    /// </summary>
    [AddComponentMenu("Neoxider/Demos/Mouse Effect Demo")]
    public sealed class MouseEffectDemoController : MonoBehaviour
    {
        [Header("Scene")]
        [Tooltip("The MouseEffect that spawns prefabs (MouseEffectClick).")]
        [SerializeField] private MouseEffect _spawnEffect;

        [Header("Controls synced on start")]
        [SerializeField] private TMP_Dropdown _triggerDropdown;
        [SerializeField] private Toggle _duringHoldToggle;
        [SerializeField] private Slider _intervalSlider;
        [SerializeField] private Slider _lifetimeSlider;

        [Header("Live values")]
        [SerializeField] private TMP_Text _spawnedValue;

        [Header("Feed")]
        [SerializeField] private DemoLogFeed _log;

        private int _spawned;
        private bool _warnedUnbuilt;

        /// <summary>True when the authored scene references are assigned.</summary>
        public bool IsSceneBuilt => _spawnEffect != null && _log != null && _spawnedValue != null;

        public int SpawnedCount => _spawned;

        private void Start()
        {
            if (!IsSceneBuilt)
            {
                WarnUnbuilt();
            }

            SyncControls();
            ShowSpawned();
            Log("MouseEffect live: press and drag in the scene");
        }

        /// <summary>Dropdown target: <see cref="MouseEffect.SpawnTrigger" /> by its enum value.</summary>
        public void SetSpawnTrigger(int trigger)
        {
            if (_spawnEffect == null || !Enum.IsDefined(typeof(MouseEffect.SpawnTrigger), trigger))
            {
                return;
            }

            _spawnEffect.spawnTrigger = (MouseEffect.SpawnTrigger)trigger;
            Log("MouseEffectClick.spawnTrigger = " + _spawnEffect.spawnTrigger);
        }

        /// <summary>Toggle target: keep spawning every Hold Interval while the button is held.</summary>
        public void SetSpawnDuringHold(bool enabled)
        {
            if (_spawnEffect == null)
            {
                return;
            }

            _spawnEffect.spawnDuringHold = enabled;
            Log("MouseEffectClick.spawnDuringHold = " + enabled);
        }

        /// <summary>Slider target: seconds between spawns while holding.</summary>
        public void SetHoldInterval(float seconds)
        {
            if (_spawnEffect != null)
            {
                _spawnEffect.holdInterval = Mathf.Max(0.005f, seconds);
            }
        }

        /// <summary>Slider target: lifetime of spawned prefabs in seconds (0 keeps them).</summary>
        public void SetSpawnLifetime(float seconds)
        {
            if (_spawnEffect != null)
            {
                _spawnEffect.spawnLifetime = Mathf.Max(0f, seconds);
            }
        }

        /// <summary>MouseEffect.onSpawn listener.</summary>
        public void CountSpawn()
        {
            _spawned++;
            ShowSpawned();
        }

        /// <summary>Button target: resets the spawn counter.</summary>
        public void ResetCounter()
        {
            _spawned = 0;
            ShowSpawned();
            Log("Spawn counter reset");
        }

        private void SyncControls()
        {
            if (_spawnEffect == null)
            {
                return;
            }

            if (_triggerDropdown != null)
            {
                _triggerDropdown.SetValueWithoutNotify((int)_spawnEffect.spawnTrigger);
            }

            if (_duringHoldToggle != null)
            {
                _duringHoldToggle.SetIsOnWithoutNotify(_spawnEffect.spawnDuringHold);
            }

            if (_intervalSlider != null)
            {
                _intervalSlider.SetValueWithoutNotify(_spawnEffect.holdInterval);
            }

            if (_lifetimeSlider != null)
            {
                _lifetimeSlider.SetValueWithoutNotify(_spawnEffect.spawnLifetime);
            }
        }

        private void ShowSpawned()
        {
            if (_spawnedValue != null)
            {
                _spawnedValue.SetText("{0}", _spawned);
            }
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
                "[MouseEffectDemoController] The scene UI is not authored. Re-import the scene from the Demo download.",
                this);
        }
    }
}
