using System;
using System.Globalization;
using Neo.Core.Resources;
using Neo.Rpg.Components;
using Neo.Samples;
using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

namespace Neo.Rpg.Demo
{
    /// <summary>
    ///     Controller of the authored RpgCharacter quick demo (Scenes/RpgCharacterQuickDemo.unity). The scene
    ///     saves everything the demo shows: both characters with their RpgCharacterTemplate assets, the cards, the
    ///     RpgResourceBinding / RpgStatBinding rows, the RpgNoCodeAction buttons, the NeoCondition low-HP row, the log
    ///     feed and the info card. Buttons call the public methods below through persistent UnityEvents; each method
    ///     forwards to the real <see cref="RpgCharacter" /> API and logs the C# call. Nothing is built at runtime.
    /// </summary>
    [AddComponentMenu("Neoxider/Demo/" + nameof(RpgCharacterQuickDemo))]
    public sealed class RpgCharacterQuickDemo : MonoBehaviour
    {
        [Header("Characters")]
        [SerializeField] private RpgCharacter _player;
        [SerializeField] private RpgCharacter _enemy;

        [Tooltip("Custom resource id (RpgStatId preset Custom) refilled by RefillPlayerCustomResource.")]
        [SerializeField] private string _customManaId = "DarkMana";

        [Tooltip("RpgDamageInfo.DamageType used by HitEnemy.")]
        [SerializeField] private string _enemyHitType = "Fire";

        [Tooltip("Incoming damage multiplier of the guard IRpgDamageModifier toggled by SetPlayerGuard.")]
        [SerializeField] [Range(0f, 1f)] private float _guardDamageScale = 0.5f;

        [Header("Authored UI")]
        [Tooltip("Label refreshed from the code-side reactive XpState / UpgradePointsState.")]
        [SerializeField] private TMP_Text _progressValue;

        [SerializeField] private Toggle _guardToggle;
        [SerializeField] private DemoLogFeed _log;

        private readonly GuardModifier _guard = new();
        private UnityAction<float> _xpListener;
        private UnityAction<int> _pointsListener;
        private Action<RpgDamageInfo, float> _enemyDamageListener;
        private RpgCharacter _progressSource;
        private RpgCharacter _damageSource;
        private bool _guardActive;
        private bool _warnedUnbuilt;
        private bool _warnedMissingCharacters;

        public RpgCharacter Player => _player;
        public RpgCharacter Enemy => _enemy;
        public bool GuardActive => _guardActive;

        /// <summary>True when the authored scene references are assigned.</summary>
        public bool IsSceneBuilt => _player != null && _enemy != null && _progressValue != null && _log != null;

        private void OnEnable()
        {
            Subscribe();
        }

        private void OnDisable()
        {
            Unsubscribe();
            SetPlayerGuard(false);
        }

        private void Start()
        {
            if (!IsSceneBuilt)
            {
                WarnUnbuilt();
            }

            RefreshProgress();
            if (_guardToggle != null)
            {
                _guardToggle.SetIsOnWithoutNotify(_guardActive);
            }

            Log("RpgCharacter live · both characters use RpgCharacterTemplate assets");
        }

        /// <summary>Button target: <c>player.Damage(amount)</c>.</summary>
        public void DamagePlayer(float amount)
        {
            if (!RequireCharacters())
            {
                return;
            }

            float removed = _player.Damage(amount);
            Log("player.Damage(" + Format(amount) + ") -> -" + Format(removed) + " HP" + (_guardActive ? " (guard)" : string.Empty));
        }

        /// <summary>Button target: <c>player.Heal(amount)</c>.</summary>
        public void HealPlayer(float amount)
        {
            if (!RequireCharacters())
            {
                return;
            }

            float healed = _player.Heal(amount);
            Log("player.Heal(" + Format(amount) + ") -> +" + Format(healed) + " HP");
        }

        /// <summary>Button target: <c>player.Spend("Stamina", amount)</c>; fails when the pool is too low.</summary>
        public void SpendPlayerStamina(float amount)
        {
            if (!RequireCharacters())
            {
                return;
            }

            bool spent = _player.Spend(RpgResourceId.Stamina, amount);
            Log("player.Spend(\"Stamina\", " + Format(amount) + ") -> " + (spent ? "spent" : "not enough Stamina"));
        }

        /// <summary>Button target: <c>player.Refill(customId, amount)</c> on the custom resource.</summary>
        public void RefillPlayerCustomResource(float amount)
        {
            if (!RequireCharacters())
            {
                return;
            }

            float added = _player.Refill(_customManaId, amount);
            Log("player.Refill(\"" + _customManaId + "\", " + Format(amount) + ") -> +" + Format(added));
        }

        /// <summary>Button target: <c>player.AddXp(amount)</c> (shown through the reactive XpState).</summary>
        public void AddPlayerXp(float amount)
        {
            if (!RequireCharacters())
            {
                return;
            }

            _player.AddXp(amount);
            Log("player.AddXp(" + Format(amount) + ") -> XP " + Format(_player.XpValue));
        }

        /// <summary>Button target: <c>player.AddLevel(levels)</c>; every level grants an upgrade point.</summary>
        public void AddPlayerLevel(int levels)
        {
            if (!RequireCharacters())
            {
                return;
            }

            _player.AddLevel(levels);
            Log("player.AddLevel(" + levels + ") -> level " + _player.LevelValue + ", points " + _player.UpgradePointsValue);
        }

        /// <summary>Button target: <c>player.UpgradeStat(statId)</c>; needs an upgrade point.</summary>
        public void UpgradePlayerStat(string statId)
        {
            if (!RequireCharacters() || string.IsNullOrWhiteSpace(statId))
            {
                return;
            }

            if (!_player.CanUpgradeStat(statId))
            {
                Log("player.CanUpgradeStat(\"" + statId + "\") = false · press Level Up for a point");
                return;
            }

            _player.UpgradeStat(statId);
            Log("player.UpgradeStat(\"" + statId + "\") -> " + statId + " " + Format(_player.GetStat(statId)) +
                ", Max HP " + Format(_player.MaxHpValue));
        }

        /// <summary>Button target: typed hit with a source, <c>enemy.ApplyDamage(new RpgDamageInfo(...))</c>.</summary>
        public void HitEnemy(float amount)
        {
            if (!RequireCharacters())
            {
                return;
            }

            _enemy.ApplyDamage(new RpgDamageInfo(amount, _enemyHitType, _player));
        }

        /// <summary>Button target: <c>Restore()</c> on both characters (revives a dead one).</summary>
        public void RestoreAll()
        {
            if (!RequireCharacters())
            {
                return;
            }

            _player.Restore();
            _enemy.Restore();
            Log("player.Restore() + enemy.Restore() -> full resources");
        }

        /// <summary>
        ///     Toggle target: registers or removes an <see cref="IRpgDamageModifier" /> that scales the player's
        ///     incoming damage by <c>Guard Damage Scale</c>.
        /// </summary>
        public void SetPlayerGuard(bool on)
        {
            if (on == _guardActive)
            {
                return;
            }

            if (_player == null)
            {
                _guardActive = false;
                return;
            }

            _guardActive = on;
            _guard.Scale = _guardDamageScale;
            if (on)
            {
                _player.AddDamageModifier(_guard);
                Log("player.AddDamageModifier(guard ×" + _guardDamageScale.ToString("0.##", CultureInfo.InvariantCulture) + ")");
            }
            else
            {
                _player.RemoveDamageModifier(_guard);
                Log("player.RemoveDamageModifier(guard)");
            }

            if (_guardToggle != null && _guardToggle.isOn != on)
            {
                _guardToggle.SetIsOnWithoutNotify(on);
            }
        }

        private void Subscribe()
        {
            if (_player != null && _progressSource != _player)
            {
                UnsubscribeProgress();
                _xpListener ??= OnXpChanged;
                _pointsListener ??= OnPointsChanged;
                _player.XpState.AddListener(_xpListener);
                _player.UpgradePointsState.AddListener(_pointsListener);
                _progressSource = _player;
            }

            if (_enemy != null && _damageSource != _enemy)
            {
                UnsubscribeDamage();
                _enemyDamageListener ??= OnEnemyDamaged;
                _enemy.DamageApplied += _enemyDamageListener;
                _damageSource = _enemy;
            }
        }

        private void Unsubscribe()
        {
            UnsubscribeProgress();
            UnsubscribeDamage();
        }

        private void UnsubscribeProgress()
        {
            if (_progressSource != null)
            {
                _progressSource.XpState.RemoveListener(_xpListener);
                _progressSource.UpgradePointsState.RemoveListener(_pointsListener);
            }

            _progressSource = null;
        }

        private void UnsubscribeDamage()
        {
            if (_damageSource != null)
            {
                _damageSource.DamageApplied -= _enemyDamageListener;
            }

            _damageSource = null;
        }

        private void OnXpChanged(float xp)
        {
            RefreshProgress();
        }

        private void OnPointsChanged(int points)
        {
            RefreshProgress();
        }

        private void OnEnemyDamaged(RpgDamageInfo info, float applied)
        {
            string source = info.Source is Component component ? component.name : "no source";
            string type = string.IsNullOrEmpty(info.DamageType) ? "untyped" : info.DamageType;
            Log("enemy.DamageApplied -> -" + Format(applied) + " HP · " + type + " from " + source);
        }

        private void RefreshProgress()
        {
            if (_progressValue == null || _player == null)
            {
                return;
            }

            _progressValue.text = Format(_player.XpValue) + " XP · " + _player.UpgradePointsValue + " pts";
        }

        private bool RequireCharacters()
        {
            if (_player != null && _enemy != null)
            {
                return true;
            }

            if (!_warnedMissingCharacters)
            {
                _warnedMissingCharacters = true;
                global::Neo.Demo.SampleDiagnostics.Warn(
                    "[RpgCharacterQuickDemo] Player or Enemy is not assigned. Re-import the scene from the " +
                    "Demo download.", this);
            }

            return false;
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
                "[RpgCharacterQuickDemo] The scene UI is not authored. Re-import the scene from the Demo download.",
                this);
        }

        private static string Format(float value)
        {
            return value.ToString("0.#", CultureInfo.InvariantCulture);
        }

        /// <summary>Scales every incoming hit; the guard toggle registers it on the player.</summary>
        private sealed class GuardModifier : IRpgDamageModifier
        {
            public float Scale = 0.5f;

            public float ModifyIncomingDamage(RpgCharacter target, RpgDamageInfo info, float amount)
            {
                return amount * Scale;
            }
        }
    }
}
