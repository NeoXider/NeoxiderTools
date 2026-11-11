using Neo.Abilities;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Neo.Samples
{
    /// <summary>
    ///     Behaviour of the authored <b>Neo.Abilities NoCode</b> demo (AbilitiesNoCodeDemo.unity). The scene - frame,
    ///     NoCode buttons with their <see cref="AbilityNoCodeAction" />, <c>SetProgress</c> bars, the Auto Zap toggle and
    ///     the persistent listeners from the units and the caster into this component - is saved by the
    ///     AbilitiesNoCodeDemoBuilder; nothing is built at runtime. This component holds the C# path next to the
    ///     NoCode one (<see cref="AbilityCasterBehaviour.TryCastAtUnit" />, <see cref="AbilityUnitBehaviour.SetLevel" />)
    ///     and turns unit events into readable feedback.
    /// </summary>
    [AddComponentMenu("Neoxider/Demos/Abilities Demo Guide")]
    public sealed class AbilitiesDemoGuide : MonoBehaviour
    {
        private const float StatusRefreshInterval = 0.1f;

        [Tooltip("Demo canvas; hidden while this guide is disabled.")]
        [SerializeField] private Canvas _canvas;

        [Header("Units")]
        [SerializeField] private AbilityUnitBehaviour _player;
        [SerializeField] private AbilityUnitBehaviour _dummy;
        [SerializeField] private AbilityCasterBehaviour _caster;
        [SerializeField] private AbilityAutoCaster _autoCaster;

        [Header("Ability ids")]
        [SerializeField] private string _zapAbilityId = "zap";
        [SerializeField] private string _healAbilityId = "heal_pulse";

        [Header("Authored UI")]
        [SerializeField] private TMP_Text _status;
        [SerializeField] private TMP_Text _feedback;
        [SerializeField] private Toggle _autoZapToggle;
        [SerializeField] private DemoLogFeed _log;

        private bool _sawHeal;
        private bool _sawHit;
        private bool _warnedUnbuilt;
        private float _nextStatusTime;

        /// <summary>Live normalized cooldown used by the guide's READY indicator.</summary>
        public float ZapCooldown => _caster != null ? _caster.GetCooldownNormalized(_zapAbilityId) : 0f;

        /// <summary>Most recent gameplay result shown to the person running this sample.</summary>
        public string LastResult => _feedback != null ? _feedback.text : string.Empty;

        /// <summary>True when the builder assigned every authored reference.</summary>
        public bool IsSceneBuilt => _player != null && _dummy != null && _caster != null && _autoCaster != null &&
                                    _status != null && _feedback != null;

        private void Start()
        {
            if (!IsSceneBuilt)
            {
                WarnUnbuilt();
                return;
            }

            if (_autoZapToggle != null)
            {
                _autoZapToggle.SetIsOnWithoutNotify(_autoCaster.enabled);
            }

            RefreshStatus();
        }

        private void OnEnable()
        {
            if (_canvas != null)
            {
                _canvas.enabled = true;
            }
        }

        private void OnDisable()
        {
            if (_canvas != null)
            {
                _canvas.enabled = false;
            }
        }

        private void Update()
        {
            // WHY: the status line rebuilds strings; 10 Hz is imperceptible and keeps the frame allocation-free.
            if (Time.unscaledTime < _nextStatusTime)
            {
                return;
            }

            _nextStatusTime = Time.unscaledTime + StatusRefreshInterval;
            RefreshStatus();
        }

        /// <summary>Button target: the C# path - <see cref="AbilityCasterBehaviour.TryCastAtUnit" /> on the dummy.</summary>
        public void Zap()
        {
            if (!IsSceneBuilt)
            {
                WarnUnbuilt();
                return;
            }

            if (!_dummy.IsAlive)
            {
                SetFeedback("Dummy defeated. Reset demo to try another level.");
                return;
            }

            bool cast = _caster.TryCastAtUnit(_zapAbilityId, _dummy);
            Log("caster.TryCastAtUnit(\"" + _zapAbilityId + "\", dummy) -> " + (cast ? "cast" : "rejected"));
        }

        /// <summary>Flips the Auto Zap toggle (or the auto-caster when the toggle is missing).</summary>
        public void ToggleAuto()
        {
            if (_autoZapToggle != null)
            {
                _autoZapToggle.isOn = !_autoZapToggle.isOn;
                return;
            }

            if (_autoCaster != null)
            {
                _autoCaster.enabled = !_autoCaster.enabled;
                OnAutoZapChanged(_autoCaster.enabled);
            }
        }

        /// <summary>Toggle listener next to the NoCode <c>AbilityAutoCaster.enabled</c> binding: explains the switch.</summary>
        public void OnAutoZapChanged(bool enabled)
        {
            SetFeedback(enabled
                ? "Auto Zap attacks the opposing dummy whenever the ability is ready."
                : "Auto Zap paused. Manual buttons let you inspect one result at a time.");
            Log("AbilityAutoCaster.enabled = " + (enabled ? "true" : "false"));
        }

        /// <summary>Button target: cycles the player level 1 → 2 → 3 (Zap damage scales 12 / 20 / 30).</summary>
        public void NextLevel()
        {
            if (_player == null)
            {
                WarnUnbuilt();
                return;
            }

            int level = _player.Unit != null ? _player.Unit.Level % 3 + 1 : 1;
            _player.SetLevel(level);
            SetFeedback("Player level " + level + ". The next Zap uses the same asset's level-scaled damage.");
            Log("player.SetLevel(" + level + ")");
        }

        /// <summary>Button target: re-registers both units (full health, no cooldowns) and returns to level 1.</summary>
        public void ResetDemo()
        {
            if (!IsSceneBuilt)
            {
                WarnUnbuilt();
                return;
            }

            if (_autoZapToggle != null)
            {
                _autoZapToggle.SetIsOnWithoutNotify(false);
            }

            _autoCaster.enabled = false;
            _caster.enabled = false;
            _player.enabled = false;
            _dummy.enabled = false;
            _player.enabled = true;
            _dummy.enabled = true;
            _caster.enabled = true;
            _player.SetLevel(1);
            _sawHeal = false;
            _sawHit = false;
            SetFeedback("Reset: both units restored, cooldowns cleared, level 1. Hit Player 20, then Cast Heal.");
            Log("Reset: units re-registered from their UnitTemplate");
            RefreshStatus();
        }

        /// <summary>Persistent listener of the player's <see cref="AbilityUnitBehaviour.OnDamaged" />.</summary>
        public void OnPlayerDamaged(float amount)
        {
            SetFeedback("Player lost " + amount.ToString("0") + " HP. Now Cast Heal.");
        }

        /// <summary>Persistent listener of the player's <see cref="AbilityUnitBehaviour.OnHealed" />.</summary>
        public void OnPlayerHealed(float amount)
        {
            _sawHeal = true;
            SetFeedback("Heal restored " + amount.ToString("0") + " HP. " +
                        (_sawHit ? "Completed both healing and damage. Try level scaling or Reset." : "Next, Zap the dummy."));
        }

        /// <summary>Persistent listener of the dummy's <see cref="AbilityUnitBehaviour.OnDamaged" />.</summary>
        public void OnDummyDamaged(float amount)
        {
            _sawHit = true;
            SetFeedback("Zap dealt " + amount.ToString("0") + " damage to the dummy. " +
                        (_sawHeal ? "Completed both healing and damage. Try another level." : "Try Hit Player 20, then Cast Heal."));
        }

        /// <summary>Persistent listener of <see cref="AbilityCasterBehaviour.OnCastFailed" />.</summary>
        public void OnCastFailed(string reason)
        {
            SetFeedback("Cast rejected: " + reason + ". Wait for READY; defeated units need Reset.");
        }

        private void RefreshStatus()
        {
            if (_status == null || _player == null || _dummy == null || _caster == null)
            {
                return;
            }

            string heal = _caster.GetCooldownNormalized(_healAbilityId) > 0f ? "cooling down" : "READY";
            string zap = _caster.GetCooldownNormalized(_zapAbilityId) > 0f ? "cooling down" : "READY";
            _status.text = "Player " + _player.CurrentHealth.ToString("0") + "/" + _player.MaxHealth.ToString("0") +
                           " HP   ·   Dummy " + _dummy.CurrentHealth.ToString("0") + "/" + _dummy.MaxHealth.ToString("0") +
                           " HP   ·   Level " + (_player.Unit != null ? _player.Unit.Level : 1) +
                           "\nHeal: " + heal + "   ·   Zap: " + zap +
                           (!_dummy.IsAlive || !_player.IsAlive ? "   ·   Unit defeated: Reset to repeat" : string.Empty);
        }

        private void SetFeedback(string message)
        {
            if (_feedback != null)
            {
                _feedback.text = message;
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
                "[AbilitiesDemoGuide] The scene UI is not authored. Run Neoxider/Demo/Rebuild Active Demo Scene.", this);
        }
    }
}
