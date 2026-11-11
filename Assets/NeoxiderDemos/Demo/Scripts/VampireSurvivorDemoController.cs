using System;
using System.Collections.Generic;
using Neo.NPC;
using Neo.Rpg;
using Neo.Rpg.Components;
using Neo.Samples;
using Neo.Tools;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Neo.Rpg.Demo
{
    /// <summary>
    ///     Run controller of the first-person Vampire Survivor demo (VampireSurvivorMCP.unity): starts the scene
    ///     <see cref="Spawner" />s, points every spawned enemy at the player and tracks it in the
    ///     <see cref="RpgWorldHealthBars" />, counts kills and handles the player's death. An enemy is configured once
    ///     per life: on its <see cref="Spawner.OnObjectSpawned" /> event, or when the fallback scene search first sees
    ///     it (hand-placed enemies, or one enabled again after it died or was disabled). While it feeds the bars it
    ///     turns their own tag search off. Enemy death (deactivation) is the job of the RpgDeathHandler authored on the
    ///     enemy prefabs. The HUD (status card, buttons, log feed, death overlay, health-bar canvas) is saved in the
    ///     scene; buttons call the public methods below through persistent UnityEvents and this
    ///     component only refreshes the authored labels. It builds no UI at runtime.
    /// </summary>
    [AddComponentMenu("Neoxider/Samples/Vampire Survivor Demo Controller")]
    public sealed class VampireSurvivorDemoController : MonoBehaviour
    {
        private const float HudRefreshInterval = 0.1f;
        private const string EnemyTag = "Enemy";

        [Header("Scene References")]
        [SerializeField] private RpgCharacter _player;
        [SerializeField] private Spawner[] _spawners = Array.Empty<Spawner>();

        [Tooltip("Enemy health bars. Falls back to the component on this GameObject.")]
        [SerializeField] private RpgWorldHealthBars _healthBars;

        [Tooltip("Track every configured enemy in the health bars. When the bars search for the same 'Enemy' tag and " +
                 "Configure Spawned Enemies is on, their own scene search is turned off at Awake, since the enemy " +
                 "search of this controller already finds those characters.")]
        [SerializeField] private bool _feedHealthBars = true;

        [Header("Run Settings")]
        [SerializeField] private bool _startSpawnersOnPlay = true;
        [SerializeField] private bool _configureSpawnedEnemies = true;

        [Tooltip("Interval of the fallback scene search for 'Enemy' characters no wired Spawner reported. Enemies " +
                 "configured in their current life are skipped.")]
        [SerializeField] [Min(0.1f)] private float _scanInterval = 0.5f;
        [SerializeField] [Min(1)] private int _enemyContactDamage = 12;
        [SerializeField] private bool _fallbackCloseRangeDamage = true;
        [SerializeField] [Min(0.1f)] private float _fallbackDamageRange = 8f;
        [SerializeField] [Min(0.1f)] private float _fallbackDamageInterval = 0.8f;
        [SerializeField] private bool _disablePlayerControlOnDeath = true;

        [Header("Authored HUD")]
        [Tooltip("Fill of the player health bar; width and color follow the health fraction.")]
        [SerializeField] private Image _hpFill;

        [SerializeField] private TMP_Text _playerValue;
        [SerializeField] private TMP_Text _enemiesValue;
        [SerializeField] private TMP_Text _spawnersValue;

        [Tooltip("Shown while the player is dead.")]
        [SerializeField] private GameObject _deathOverlay;

        [Tooltip("Event feed; every run event is logged here.")]
        [SerializeField] private DemoLogFeed _log;

        [SerializeField] private Color _healthFull = new(0.26f, 0.85f, 0.64f);
        [SerializeField] private Color _healthEmpty = new(1f, 0.36f, 0.48f);

        private readonly HashSet<RpgCharacter> _trackedEnemies = new();
        private readonly HashSet<RpgCharacter> _configuredEnemies = new();
        private readonly Dictionary<RpgCharacter, UnityEngine.Events.UnityAction> _enemyDeathListeners = new();
        private readonly List<Behaviour> _disabledOnDeath = new();

        private float _nextScanTime;
        private float _nextFallbackDamageTime;
        private int _spawnedCount;
        private int _killCount;
        private bool _playerDead;
        private string _lastEvent = "Ready";
        private float _nextHudRefreshTime;
        private bool _warnedUnbuilt;
        private bool _warnedMissingDeathHandler;

        public RpgCharacter Player => _player;
        public int SpawnedCount => _spawnedCount;
        public int KillCount => _killCount;
        public int ActiveEnemyCount => CountActiveEnemies();
        public bool PlayerDead => _playerDead || (_player != null && _player.IsDead);
        public string LastEvent => _lastEvent;

        /// <summary>True when the authored HUD references are assigned.</summary>
        public bool IsHudBuilt => _hpFill != null && _playerValue != null && _enemiesValue != null &&
                                  _spawnersValue != null && _deathOverlay != null;

        private void Awake()
        {
            AutoResolve();
            ClaimHealthBarFeed();
        }

        private void Start()
        {
            if (!IsHudBuilt)
            {
                WarnUnbuilt();
            }

            RegisterPlayer();
            RegisterSpawners();
            ScanEnemies();

            if (_startSpawnersOnPlay)
            {
                StartSpawners();
            }

            RefreshHud(true);
        }

        private void OnDestroy()
        {
            if (_player != null)
            {
                _player.OnDeathEvent.RemoveListener(HandlePlayerDeath);
            }

            foreach (KeyValuePair<RpgCharacter, UnityEngine.Events.UnityAction> pair in _enemyDeathListeners)
            {
                if (pair.Key != null)
                {
                    pair.Key.OnDeathEvent.RemoveListener(pair.Value);
                }
            }

            if (_spawners != null)
            {
                for (int i = 0; i < _spawners.Length; i++)
                {
                    if (_spawners[i] != null)
                    {
                        _spawners[i].OnObjectSpawned.RemoveListener(HandleObjectSpawned);
                    }
                }
            }
        }

        private void Update()
        {
            if (Time.time >= _nextScanTime)
            {
                ScanEnemies();
                _nextScanTime = Time.time + _scanInterval;
            }

            if (_fallbackCloseRangeDamage && _player != null && !_player.IsDead &&
                Time.time >= _nextFallbackDamageTime)
            {
                ApplyCloseRangeFallbackDamage();
                _nextFallbackDamageTime = Time.time + _fallbackDamageInterval;
            }

            RefreshHud(false);
        }

        /// <summary>Scene-search fallback for references that are not assigned.</summary>
        [Button]
        public void AutoResolve()
        {
            if (_player == null)
            {
                GameObject playerObject = GameObject.FindGameObjectWithTag("Player");
                if (playerObject != null)
                {
                    _player = playerObject.GetComponentInChildren<RpgCharacter>(true) ??
                              playerObject.GetComponentInParent<RpgCharacter>();
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

            if (_spawners == null || _spawners.Length == 0)
            {
                _spawners = FindObjectsByType<Spawner>(FindObjectsInactive.Include, FindObjectsSortMode.None);
            }

            if (_healthBars == null)
            {
                _healthBars = GetComponent<RpgWorldHealthBars>();
            }
        }

        /// <summary>Button target: <see cref="Spawner.StartSpawn" /> on every scene spawner.</summary>
        [Button]
        public void StartSpawners()
        {
            if (_spawners == null)
            {
                return;
            }

            for (int i = 0; i < _spawners.Length; i++)
            {
                _spawners[i]?.StartSpawn();
            }

            SetEvent("Spawners started (Spawner.StartSpawn)");
        }

        /// <summary>Button target: <see cref="Spawner.StopSpawn" /> on every scene spawner.</summary>
        [Button]
        public void StopSpawners()
        {
            if (_spawners == null)
            {
                return;
            }

            for (int i = 0; i < _spawners.Length; i++)
            {
                _spawners[i]?.StopSpawn();
            }

            SetEvent("Spawners stopped (Spawner.StopSpawn)");
        }

        /// <summary>Button target: restores the player, re-enables control and hides the tracked enemies.</summary>
        [Button]
        public void ResetRun()
        {
            _playerDead = false;
            _killCount = 0;
            _spawnedCount = 0;
            _player?.Restore();
            SetPlayerControlEnabled(true);

            foreach (RpgCharacter enemy in _trackedEnemies)
            {
                if (enemy != null && enemy.gameObject.scene.IsValid())
                {
                    enemy.gameObject.SetActive(false);
                }
            }

            _trackedEnemies.Clear();
            _configuredEnemies.Clear();
            SetEvent("Run reset (RpgCharacter.Restore)");
            ScanEnemies();
            RefreshHud(true);
        }

        /// <summary>Button target: typed damage through <see cref="RpgCharacter.DamageType" />.</summary>
        public void DamagePlayer(float amount)
        {
            if (_player == null || _player.IsDead)
            {
                return;
            }

            float dealt = _player.DamageType("Demo", amount);
            SetEvent(dealt > 0f ? $"Player took {dealt:0} (RpgCharacter.DamageType)" : "Player damage blocked");
        }

        /// <summary>Button target: lethal damage to the nearest living tracked enemy.</summary>
        public void KillNearestEnemy()
        {
            RpgCharacter enemy = FindNearestLivingEnemy();
            if (enemy == null)
            {
                SetEvent("No living enemy");
                return;
            }

            enemy.Damage(enemy.MaxHpValue + 999f);
            SetEvent($"Killed {CleanName(enemy.name)}");
        }

        private void RegisterPlayer()
        {
            if (_player == null)
            {
                return;
            }

            _player.OnDeathEvent.RemoveListener(HandlePlayerDeath);
            _player.OnDeathEvent.AddListener(HandlePlayerDeath);
            _playerDead = _player.IsDead;
        }

        private void RegisterSpawners()
        {
            if (_spawners == null)
            {
                return;
            }

            for (int i = 0; i < _spawners.Length; i++)
            {
                if (_spawners[i] == null)
                {
                    continue;
                }

                _spawners[i].OnObjectSpawned.RemoveListener(HandleObjectSpawned);
                _spawners[i].OnObjectSpawned.AddListener(HandleObjectSpawned);
            }
        }

        private void HandleObjectSpawned(GameObject spawned)
        {
            _spawnedCount++;
            ConfigureEnemy(spawned != null ? spawned.GetComponentInChildren<RpgCharacter>(true) : null);
            SetEvent(spawned != null ? $"Spawned {CleanName(spawned.name)}" : "Spawner emitted null");
        }

        private void ScanEnemies()
        {
            if (!_configureSpawnedEnemies)
            {
                return;
            }

            // WHY: an enemy disabled without dying (or without a spawner event) is configured again once it is back.
            foreach (RpgCharacter enemy in _trackedEnemies)
            {
                if (enemy == null || !enemy.gameObject.activeInHierarchy)
                {
                    _configuredEnemies.Remove(enemy);
                }
            }

            foreach (RpgCharacter character in FindObjectsByType<RpgCharacter>(FindObjectsInactive.Exclude,
                         FindObjectsSortMode.None))
            {
                if (character != null && character != _player && !character.IsDead &&
                    !_configuredEnemies.Contains(character) && character.CompareTag(EnemyTag))
                {
                    ConfigureEnemy(character);
                }
            }
        }

        /// <summary>
        ///     Lets this controller be the only target feed of the health bars when its enemy search covers the same
        ///     characters as theirs.
        /// </summary>
        private void ClaimHealthBarFeed()
        {
            if (!_feedHealthBars || !_configureSpawnedEnemies || _healthBars == null ||
                !string.Equals(_healthBars.TargetTag, EnemyTag, StringComparison.Ordinal))
            {
                return;
            }

            // WHY: ScanEnemies already finds and tracks every active 'Enemy' character, so the bars' own search
            // would only repeat it on each of their scans.
            _healthBars.ScanSceneForTargets = false;
        }

        private void ConfigureEnemy(RpgCharacter enemy)
        {
            if (enemy == null || enemy == _player)
            {
                return;
            }

            _trackedEnemies.Add(enemy);
            _configuredEnemies.Add(enemy);

            if (!_enemyDeathListeners.ContainsKey(enemy))
            {
                UnityEngine.Events.UnityAction action = () => HandleEnemyDeath(enemy);
                _enemyDeathListeners[enemy] = action;
                enemy.OnDeathEvent.AddListener(action);
            }

            foreach (RpgContactDamage contact in enemy.GetComponentsInChildren<RpgContactDamage>(true))
            {
                contact.SetTarget(_player != null ? _player.transform : null);
                contact.SetTargetReceiver(_player);
                contact.SetDamage(_enemyContactDamage);
            }

            foreach (NpcNavigation navigation in enemy.GetComponentsInChildren<NpcNavigation>(true))
            {
                if (_player != null)
                {
                    navigation.SetMode(NpcNavigation.NavigationMode.FollowTarget);
                    navigation.SetFollowTarget(_player.transform);
                    navigation.SetRunning(true);
                    navigation.Resume();
                }
            }

            if (_feedHealthBars && _healthBars != null)
            {
                _healthBars.Track(enemy);
            }
        }

        private void HandleEnemyDeath(RpgCharacter enemy)
        {
            _killCount++;
            _configuredEnemies.Remove(enemy);
            SetEvent(enemy != null ? $"{CleanName(enemy.name)} died" : "Enemy died");

            // WHY: the enemy prefabs carry an authored RpgDeathHandler; this only keeps hand-made enemies from
            // standing around dead, matching the handler's default Deactivate action.
            if (enemy != null && !enemy.TryGetComponent(out RpgDeathHandler _))
            {
                WarnMissingDeathHandler(enemy);
                enemy.gameObject.SetActive(false);
            }
        }

        private void HandlePlayerDeath()
        {
            _playerDead = true;
            SetEvent("Player died - Reset Run restarts");
            if (_disablePlayerControlOnDeath)
            {
                SetPlayerControlEnabled(false);
            }
        }

        private void ApplyCloseRangeFallbackDamage()
        {
            RpgCharacter enemy = FindNearestLivingEnemy();
            if (enemy == null || _player == null)
            {
                return;
            }

            float distance = Vector3.Distance(enemy.transform.position, _player.transform.position);
            if (distance > _fallbackDamageRange)
            {
                return;
            }

            float dealt = _player.DamageType("Enemy", _enemyContactDamage);
            if (dealt > 0f)
            {
                SetEvent($"{CleanName(enemy.name)} hit player for {dealt:0}");
            }
        }

        private RpgCharacter FindNearestLivingEnemy()
        {
            if (_player == null)
            {
                return null;
            }

            RpgCharacter best = null;
            float bestSqrDistance = float.PositiveInfinity;
            Vector3 origin = _player.transform.position;

            foreach (RpgCharacter enemy in _trackedEnemies)
            {
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

        private int CountActiveEnemies()
        {
            int count = 0;
            foreach (RpgCharacter enemy in _trackedEnemies)
            {
                if (enemy != null && enemy.gameObject.activeInHierarchy && !enemy.IsDead)
                {
                    count++;
                }
            }

            return count;
        }

        private int CountRunningSpawners()
        {
            if (_spawners == null)
            {
                return 0;
            }

            int count = 0;
            for (int i = 0; i < _spawners.Length; i++)
            {
                if (_spawners[i] != null && _spawners[i].isSpawning)
                {
                    count++;
                }
            }

            return count;
        }

        private void SetPlayerControlEnabled(bool enabled)
        {
            if (_player == null)
            {
                return;
            }

            if (!enabled)
            {
                _disabledOnDeath.Clear();
                foreach (Behaviour behaviour in _player.GetComponents<Behaviour>())
                {
                    if (behaviour == null || behaviour == this || behaviour is RpgCharacter)
                    {
                        continue;
                    }

                    string typeName = behaviour.GetType().Name;
                    if (typeName.Contains("PlayerController") || typeName.Contains("KeyboardMover") ||
                        typeName.Contains("MouseInput"))
                    {
                        if (behaviour.enabled)
                        {
                            behaviour.enabled = false;
                            _disabledOnDeath.Add(behaviour);
                        }
                    }
                }

                return;
            }

            for (int i = 0; i < _disabledOnDeath.Count; i++)
            {
                if (_disabledOnDeath[i] != null)
                {
                    _disabledOnDeath[i].enabled = true;
                }
            }

            _disabledOnDeath.Clear();
        }

        private void RefreshHud(bool force)
        {
            if (_player != null && _hpFill != null)
            {
                float hp01 = Mathf.Clamp01(_player.HpPercentValue);
                RectTransform fill = _hpFill.rectTransform;
                fill.anchorMax = new Vector2(hp01, fill.anchorMax.y);
                _hpFill.color = Color.Lerp(_healthEmpty, _healthFull, hp01);
            }

            bool dead = PlayerDead;
            if (_deathOverlay != null && _deathOverlay.activeSelf != dead)
            {
                _deathOverlay.SetActive(dead);
            }

            // WHY: rebuilding label strings allocates; 10 Hz is imperceptible for a status readout.
            if (!force && Time.unscaledTime < _nextHudRefreshTime)
            {
                return;
            }

            _nextHudRefreshTime = Time.unscaledTime + HudRefreshInterval;

            if (_playerValue != null)
            {
                _playerValue.text = _player == null
                    ? "missing"
                    : $"{_player.HpValue:0} / {_player.MaxHpValue:0} HP · Lv {_player.LevelValue}" +
                      (_player.IsDead ? " · DEAD" : string.Empty);
            }

            if (_enemiesValue != null)
            {
                _enemiesValue.text = $"{ActiveEnemyCount} active · {_spawnedCount} spawned · {_killCount} killed";
            }

            if (_spawnersValue != null)
            {
                _spawnersValue.text = $"{CountRunningSpawners()} / {(_spawners != null ? _spawners.Length : 0)} running";
            }
        }

        private void SetEvent(string message)
        {
            _lastEvent = message;
            if (_log != null)
            {
                _log.Log(message);
            }
        }

        private static string CleanName(string objectName)
        {
            return string.IsNullOrEmpty(objectName) ? "Enemy" : objectName.Replace("(Clone)", string.Empty).Trim();
        }

        private void WarnUnbuilt()
        {
            if (_warnedUnbuilt)
            {
                return;
            }

            _warnedUnbuilt = true;
            global::Neo.Demo.SampleDiagnostics.Warn(
                "[VampireSurvivorDemoController] The HUD is not authored. Re-import the scene from the Demo download.",
                this);
        }

        private void WarnMissingDeathHandler(RpgCharacter enemy)
        {
            if (_warnedMissingDeathHandler)
            {
                return;
            }

            _warnedMissingDeathHandler = true;
            global::Neo.Demo.SampleDiagnostics.Warn(
                "[VampireSurvivorDemoController] '" + CleanName(enemy.name) + "' has no RpgDeathHandler, so the " +
                "controller deactivates it on death. Add RpgDeathHandler to the enemy prefab.", enemy);
        }
    }
}
