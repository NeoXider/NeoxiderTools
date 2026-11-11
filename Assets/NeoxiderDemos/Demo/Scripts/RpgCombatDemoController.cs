using System;
using System.Collections.Generic;
using Neo.Rpg.Components;
using Neo.Samples;
using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

namespace Neo.Rpg.Demo
{
    /// <summary>
    ///     Drives the RPG combat demo loop (the player hits the nearest enemy, enemies pressure the player, the wave
    ///     restores once cleared) and mirrors it into the authored HUD of RpgCombatNpcDemo.unity. The HUD, the health
    ///     bar canvas, the log feed and the info card are saved in the scene; buttons and
    ///     toggles call the public methods below through persistent UnityEvents. Nothing is built at runtime.
    /// </summary>
    [AddComponentMenu("Neoxider/Samples/RPG Combat Demo Controller")]
    public sealed class RpgCombatDemoController : MonoBehaviour
    {
        private const string PlayerDamageType = "Demo";
        private const string PressureDamageType = "Enemy";

        [Header("Scene References")]
        [SerializeField] private RpgCharacter _player;
        [SerializeField] private RpgAttackController _playerAttack;
        [SerializeField] private RpgCharacter[] _enemies = Array.Empty<RpgCharacter>();

        [Header("Demo Loop")]
        [SerializeField] private bool _autoDemoLoop = true;
        [SerializeField] [Min(0.1f)] private float _playerAttackInterval = 1.1f;
        [SerializeField] [Min(1f)] private float _playerDemoDamage = 16f;
        [SerializeField] private bool _enemyPressureEnabled = true;
        [SerializeField] [Min(0.1f)] private float _enemyPressureInterval = 1.3f;
        [SerializeField] [Min(1f)] private float _enemyPressureDamage = 10f;
        [SerializeField] private bool _hideEnemiesOnDeath;
        [SerializeField] private bool _autoRestoreWhenCleared = true;
        [SerializeField] [Min(0.1f)] private float _restoreAfterClearDelay = 2f;

        [Header("Authored HUD")]
        [SerializeField] private TMP_Text _enemiesValue;
        [SerializeField] private TMP_Text _lastEventValue;
        [SerializeField] private Toggle _autoToggle;
        [SerializeField] private Toggle _pressureToggle;
        [SerializeField] private DemoLogFeed _log;

        private readonly Dictionary<RpgCharacter, UnityAction> _enemyDeathListeners = new();
        private float _nextPlayerAttackTime;
        private float _nextEnemyPressureTime;
        private float _restoreAtTime = -1f;
        private int _enemyDeaths;
        private string _lastEvent = "Ready";
        private bool _loopCall;
        private bool _warnedUnbuilt;
        private RpgCharacter _damageSource;
        private Action<RpgDamageInfo, float> _playerDamageListener;

        private int _shownLiving = -1;
        private int _shownTotal = -1;
        private int _shownDeaths = -1;
        private string _shownEvent;

        public RpgCharacter Player => _player;
        public int EnemyCount => _enemies != null ? _enemies.Length : 0;
        public int LivingEnemyCount => CountLivingEnemies();
        public bool PlayerDead => _player != null && _player.IsDead;
        public string LastEvent => _lastEvent;
        public bool AutoDemoLoop => _autoDemoLoop;
        public bool EnemyPressureEnabled => _enemyPressureEnabled;

        /// <summary>True when the authored HUD references are assigned.</summary>
        public bool IsSceneBuilt => _enemiesValue != null && _lastEventValue != null && _log != null;

        private void Awake()
        {
            AutoResolve();
            RegisterDeathListeners();
            SubscribePlayerDamage();
        }

        private void Start()
        {
            if (!IsSceneBuilt)
            {
                WarnUnbuilt();
            }

            SyncToggles();
            Log("Combat loop · player " + Describe(_player) + " vs " + EnemyCount + " enemies");
        }

        private void OnDestroy()
        {
            if (_player != null)
            {
                _player.OnDeathEvent.RemoveListener(HandlePlayerDeath);
            }

            foreach (KeyValuePair<RpgCharacter, UnityAction> pair in _enemyDeathListeners)
            {
                if (pair.Key != null)
                {
                    pair.Key.OnDeathEvent.RemoveListener(pair.Value);
                }
            }

            _enemyDeathListeners.Clear();
            if (_damageSource != null)
            {
                _damageSource.DamageApplied -= _playerDamageListener;
            }

            _damageSource = null;
        }

        private void Update()
        {
            RefreshHud();

            if (!_autoDemoLoop || _player == null || _player.IsDead)
            {
                return;
            }

            if (_autoRestoreWhenCleared && LivingEnemyCount == 0)
            {
                if (_restoreAtTime < 0f)
                {
                    _restoreAtTime = Time.time + _restoreAfterClearDelay;
                    SetEvent("Wave cleared · restoring soon", true);
                }
                else if (Time.time >= _restoreAtTime)
                {
                    RestoreAll();
                }

                return;
            }

            _restoreAtTime = -1f;
            _loopCall = true;
            try
            {
                if (Time.time >= _nextPlayerAttackTime)
                {
                    DamageNearestEnemy();
                    _nextPlayerAttackTime = Time.time + _playerAttackInterval;
                }

                if (_enemyPressureEnabled && Time.time >= _nextEnemyPressureTime)
                {
                    PressurePlayer();
                    _nextEnemyPressureTime = Time.time + _enemyPressureInterval;
                }
            }
            finally
            {
                _loopCall = false;
            }
        }

        /// <summary>Fallback scene search for references that are not assigned.</summary>
        [Button]
        public void AutoResolve()
        {
            if (_player == null)
            {
                GameObject playerObject = GameObject.FindGameObjectWithTag("Player");
                if (playerObject != null)
                {
                    _player = playerObject.GetComponentInChildren<RpgCharacter>(true);
                    if (_player == null)
                    {
                        _player = playerObject.GetComponentInParent<RpgCharacter>();
                    }
                }
            }

            if (_player == null)
            {
                foreach (RpgCharacter character in FindObjectsByType<RpgCharacter>(FindObjectsInactive.Include,
                             FindObjectsSortMode.None))
                {
                    if (character.CompareTag("Player"))
                    {
                        _player = character;
                        break;
                    }
                }
            }

            if (_playerAttack == null && _player != null)
            {
                _playerAttack = _player.GetComponent<RpgAttackController>();
            }

            if (_enemies == null || _enemies.Length == 0)
            {
                List<RpgCharacter> found = new();
                foreach (RpgCharacter character in FindObjectsByType<RpgCharacter>(FindObjectsInactive.Include,
                             FindObjectsSortMode.None))
                {
                    if (character != _player && (character.CompareTag("Enemy") || character.gameObject.name.Contains("Npc")))
                    {
                        found.Add(character);
                    }
                }

                _enemies = found.ToArray();
            }
        }

        /// <summary>Button target: the player swings at the nearest living enemy (typed hit with a source).</summary>
        [Button]
        public void DamageNearestEnemy()
        {
            RpgCharacter target = FindNearestLivingEnemy();
            if (target == null)
            {
                SetEvent("No living enemy", !_loopCall);
                return;
            }

            FaceTarget(_player != null ? _player.transform : transform, target.transform);
            if (_playerAttack != null)
            {
                _playerAttack.UsePrimaryAttack();
            }

            float dealt = target.ApplyDamage(new RpgDamageInfo(_playerDemoDamage, PlayerDamageType, _player));
            SetEvent(dealt > 0f
                ? "Player dealt " + Format(dealt) + " to " + target.name
                : "Player attack did not affect " + target.name, !_loopCall);
        }

        /// <summary>Button target: the nearest living enemy hits the player (reported through DamageApplied).</summary>
        [Button]
        public void PressurePlayer()
        {
            if (_player == null || _player.IsDead)
            {
                return;
            }

            RpgCharacter attacker = FindNearestLivingEnemy();
            if (attacker == null)
            {
                SetEvent("No enemy pressure", !_loopCall);
                return;
            }

            if (_player.ApplyDamage(new RpgDamageInfo(_enemyPressureDamage, PressureDamageType, attacker)) <= 0f)
            {
                SetEvent("Enemy pressure did not affect player", !_loopCall);
            }
        }

        /// <summary>Button target: restores the player and every enemy (revives the dead).</summary>
        [Button]
        public void RestoreAll()
        {
            _enemyDeaths = 0;
            _restoreAtTime = -1f;
            if (_player != null)
            {
                _player.Restore();
            }

            if (_enemies != null)
            {
                for (int i = 0; i < _enemies.Length; i++)
                {
                    RpgCharacter enemy = _enemies[i];
                    if (enemy == null)
                    {
                        continue;
                    }

                    enemy.gameObject.SetActive(true);
                    enemy.Restore();
                }
            }

            SetEvent("Restored all combatants · Restore()", true);
        }

        /// <summary>Toggle target: runs or pauses the automatic attack loop.</summary>
        public void SetAutoDemoLoop(bool enabled)
        {
            if (_autoDemoLoop == enabled)
            {
                return;
            }

            _autoDemoLoop = enabled;
            Log(enabled ? "Auto loop on" : "Auto loop off · move with WASD, attack with the left mouse button");
        }

        /// <summary>Toggle target: enables or disables the scripted enemy pressure.</summary>
        public void SetEnemyPressure(bool enabled)
        {
            if (_enemyPressureEnabled == enabled)
            {
                return;
            }

            _enemyPressureEnabled = enabled;
            Log(enabled ? "Enemy pressure on" : "Enemy pressure off · only the NPC components hit now");
        }

        private void RegisterDeathListeners()
        {
            if (_player != null)
            {
                _player.OnDeathEvent.RemoveListener(HandlePlayerDeath);
                _player.OnDeathEvent.AddListener(HandlePlayerDeath);
            }

            if (_enemies == null)
            {
                return;
            }

            for (int i = 0; i < _enemies.Length; i++)
            {
                RpgCharacter enemy = _enemies[i];
                if (enemy == null || _enemyDeathListeners.ContainsKey(enemy))
                {
                    continue;
                }

                RpgCharacter captured = enemy;
                UnityAction listener = () => HandleEnemyDeath(captured);
                _enemyDeathListeners.Add(enemy, listener);
                enemy.OnDeathEvent.AddListener(listener);
            }
        }

        private void SubscribePlayerDamage()
        {
            if (_player == null || _damageSource == _player)
            {
                return;
            }

            _playerDamageListener ??= HandlePlayerDamaged;
            _player.DamageApplied += _playerDamageListener;
            _damageSource = _player;
        }

        private void HandlePlayerDamaged(RpgDamageInfo info, float applied)
        {
            string source = info.Source is Component component ? component.name : "Unknown";
            string type = string.IsNullOrEmpty(info.DamageType) ? "untyped" : info.DamageType;
            SetEvent(source + " hit Player for " + Format(applied) + " (" + type + ")", !_loopCall);
        }

        private void HandlePlayerDeath()
        {
            SetEvent("Player died · press Restore All", true);
        }

        private void HandleEnemyDeath(RpgCharacter enemy)
        {
            _enemyDeaths++;
            SetEvent(enemy.name + " died", true);

            if (_hideEnemiesOnDeath)
            {
                enemy.gameObject.SetActive(false);
            }
        }

        private RpgCharacter FindNearestLivingEnemy()
        {
            if (_enemies == null || _player == null)
            {
                return null;
            }

            RpgCharacter best = null;
            float bestSqrDistance = float.PositiveInfinity;
            Vector3 origin = _player.transform.position;

            for (int i = 0; i < _enemies.Length; i++)
            {
                RpgCharacter enemy = _enemies[i];
                if (enemy == null || !enemy.gameObject.activeInHierarchy || enemy.IsDead)
                {
                    continue;
                }

                float sqrDistance = (enemy.transform.position - origin).sqrMagnitude;
                if (sqrDistance < bestSqrDistance)
                {
                    best = enemy;
                    bestSqrDistance = sqrDistance;
                }
            }

            return best;
        }

        private int CountLivingEnemies()
        {
            if (_enemies == null)
            {
                return 0;
            }

            int count = 0;
            for (int i = 0; i < _enemies.Length; i++)
            {
                RpgCharacter enemy = _enemies[i];
                if (enemy != null && enemy.gameObject.activeInHierarchy && !enemy.IsDead)
                {
                    count++;
                }
            }

            return count;
        }

        // WHY: labels rebuild only when a value actually changes to avoid per-frame string garbage.
        private void RefreshHud()
        {
            if (_enemiesValue != null)
            {
                int living = LivingEnemyCount;
                int total = EnemyCount;
                if (living != _shownLiving || total != _shownTotal || _enemyDeaths != _shownDeaths)
                {
                    _shownLiving = living;
                    _shownTotal = total;
                    _shownDeaths = _enemyDeaths;
                    _enemiesValue.text = living + " / " + total + " · deaths " + _enemyDeaths;
                }
            }

            if (_lastEventValue != null && !ReferenceEquals(_lastEvent, _shownEvent))
            {
                _shownEvent = _lastEvent;
                _lastEventValue.text = _lastEvent;
            }

            SyncToggles();
        }

        private void SyncToggles()
        {
            if (_autoToggle != null && _autoToggle.isOn != _autoDemoLoop)
            {
                _autoToggle.SetIsOnWithoutNotify(_autoDemoLoop);
            }

            if (_pressureToggle != null && _pressureToggle.isOn != _enemyPressureEnabled)
            {
                _pressureToggle.SetIsOnWithoutNotify(_enemyPressureEnabled);
            }
        }

        private void SetEvent(string message, bool log)
        {
            _lastEvent = message;
            if (log)
            {
                Log(message);
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
                "[RpgCombatDemoController] The HUD is not authored. Re-import the scene from the Demo download.", this);
        }

        private static string Describe(Component component)
        {
            return component != null ? component.name : "missing";
        }

        private static string Format(float value)
        {
            return value.ToString("0", System.Globalization.CultureInfo.InvariantCulture);
        }

        private static void FaceTarget(Transform actor, Transform target)
        {
            if (actor == null || target == null)
            {
                return;
            }

            Vector3 direction = target.position - actor.position;
            direction.y = 0f;
            if (direction.sqrMagnitude > 0.0001f)
            {
                actor.rotation = Quaternion.LookRotation(direction.normalized, Vector3.up);
            }
        }
    }
}
