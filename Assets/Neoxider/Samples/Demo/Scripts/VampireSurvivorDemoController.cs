using System;
using System.Collections.Generic;
using Neo.NPC;
using Neo.Rpg;
using Neo.Rpg.Components;
using Neo.Tools;
using UnityEngine;

namespace Neo.Rpg.Demo
{
    [AddComponentMenu("Neoxider/Samples/Vampire Survivor Demo Controller")]
    public sealed class VampireSurvivorDemoController : MonoBehaviour
    {
        [Header("Scene References")]
        [SerializeField] private RpgCharacter _player;
        [SerializeField] private Spawner[] _spawners = Array.Empty<Spawner>();

        [Header("Run Settings")]
        [SerializeField] private bool _startSpawnersOnPlay = true;
        [SerializeField] private bool _configureSpawnedEnemies = true;
        [SerializeField] [Min(0.1f)] private float _scanInterval = 0.5f;
        [SerializeField] [Min(1)] private int _enemyContactDamage = 12;
        [SerializeField] private bool _fallbackCloseRangeDamage = true;
        [SerializeField] [Min(0.1f)] private float _fallbackDamageRange = 8f;
        [SerializeField] [Min(0.1f)] private float _fallbackDamageInterval = 0.8f;
        [SerializeField] private bool _disablePlayerControlOnDeath = true;

        private readonly HashSet<RpgCharacter> _trackedEnemies = new();
        private readonly Dictionary<RpgCharacter, UnityEngine.Events.UnityAction> _enemyDeathListeners = new();
        private readonly List<Behaviour> _disabledOnDeath = new();

        private float _nextScanTime;
        private float _nextFallbackDamageTime;
        private int _spawnedCount;
        private int _killCount;
        private bool _playerDead;
        private string _lastEvent = "Ready";

        public RpgCharacter Player => _player;
        public int SpawnedCount => _spawnedCount;
        public int KillCount => _killCount;
        public int ActiveEnemyCount => CountActiveEnemies();
        public bool PlayerDead => _playerDead || (_player != null && _player.IsDead);
        public string LastEvent => _lastEvent;

        private void Awake()
        {
            AutoResolve();
        }

        private void Start()
        {
            RegisterPlayer();
            RegisterSpawners();
            ScanEnemies();

            if (_startSpawnersOnPlay)
            {
                StartSpawners();
            }
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
        }

        private void OnGUI()
        {
            const float width = 390f;
            GUILayout.BeginArea(new Rect(16f, 16f, width, 290f), GUI.skin.window);
            GUILayout.Label("Vampire Survivor Demo");
            DrawPlayerStatus();
            GUILayout.Space(6f);
            GUILayout.Label($"Enemies: {ActiveEnemyCount} active | Spawned: {_spawnedCount} | Killed: {_killCount}");
            GUILayout.Label($"Spawners: {CountRunningSpawners()}/{(_spawners != null ? _spawners.Length : 0)} running");
            GUILayout.Label($"Last: {_lastEvent}");

            GUILayout.Space(8f);
            GUILayout.BeginHorizontal();
            if (GUILayout.Button("Start")) StartSpawners();
            if (GUILayout.Button("Stop")) StopSpawners();
            if (GUILayout.Button("Damage Player")) DamagePlayer(25f);
            GUILayout.EndHorizontal();

            GUILayout.BeginHorizontal();
            if (GUILayout.Button("Kill Nearest")) KillNearestEnemy();
            if (GUILayout.Button("Reset Run")) ResetRun();
            GUILayout.EndHorizontal();

            GUILayout.EndArea();

            if (PlayerDead)
            {
                DrawDeathOverlay();
            }
        }

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
        }

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

            _lastEvent = "Spawners started";
        }

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

            _lastEvent = "Spawners stopped";
        }

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
            _lastEvent = "Run reset";
            ScanEnemies();
        }

        public void DamagePlayer(float amount)
        {
            if (_player == null || _player.IsDead)
            {
                return;
            }

            float dealt = _player.DamageType("Demo", amount);
            _lastEvent = dealt > 0f ? $"Player took {dealt:0}" : "Player damage blocked";
        }

        public void KillNearestEnemy()
        {
            RpgCharacter enemy = FindNearestLivingEnemy();
            if (enemy == null)
            {
                _lastEvent = "No living enemy";
                return;
            }

            enemy.Damage(enemy.MaxHpValue + 999f);
            _lastEvent = $"Killed {enemy.name}";
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
            _lastEvent = spawned != null ? $"Spawned {spawned.name}" : "Spawner emitted null";
        }

        private void ScanEnemies()
        {
            if (!_configureSpawnedEnemies)
            {
                return;
            }

            foreach (RpgCharacter character in FindObjectsByType<RpgCharacter>(FindObjectsInactive.Exclude,
                         FindObjectsSortMode.None))
            {
                if (character != null && character != _player && character.CompareTag("Enemy"))
                {
                    ConfigureEnemy(character);
                }
            }
        }

        private void ConfigureEnemy(RpgCharacter enemy)
        {
            if (enemy == null || enemy == _player)
            {
                return;
            }

            _trackedEnemies.Add(enemy);

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

            if (enemy.GetComponent<RpgDeathHandler>() == null)
            {
                enemy.gameObject.AddComponent<RpgDeathHandler>();
            }
        }

        private void HandleEnemyDeath(RpgCharacter enemy)
        {
            _killCount++;
            _lastEvent = enemy != null ? $"{enemy.name} died" : "Enemy died";
        }

        private void HandlePlayerDeath()
        {
            _playerDead = true;
            _lastEvent = "Player died";
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
                _lastEvent = $"{enemy.name} hit player for {dealt:0}";
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

        private void DrawPlayerStatus()
        {
            if (_player == null)
            {
                GUILayout.Label("Player: missing");
                return;
            }

            GUILayout.Label($"Player HP: {_player.HpValue:0}/{_player.MaxHpValue:0} | " +
                            $"Level {_player.LevelValue} | Dead {_player.IsDead}");
            DrawBar(_player.HpPercentValue);
        }

        private static void DrawDeathOverlay()
        {
            Rect rect = new(Screen.width * 0.5f - 180f, Screen.height * 0.5f - 60f, 360f, 120f);
            Color previous = GUI.color;
            GUI.color = new Color(0f, 0f, 0f, 0.75f);
            GUI.DrawTexture(rect, Texture2D.whiteTexture);
            GUI.color = previous;

            GUILayout.BeginArea(rect, GUI.skin.window);
            GUILayout.FlexibleSpace();
            var style = new GUIStyle(GUI.skin.label)
            {
                alignment = TextAnchor.MiddleCenter,
                fontSize = 28,
                fontStyle = FontStyle.Bold
            };
            GUILayout.Label("<color=red>YOU DIED</color>", style);
            GUILayout.Label("Use Reset Run to restart.", new GUIStyle(GUI.skin.label)
            {
                alignment = TextAnchor.MiddleCenter
            });
            GUILayout.FlexibleSpace();
            GUILayout.EndArea();
        }

        private static void DrawBar(float value01)
        {
            Rect rect = GUILayoutUtility.GetRect(1f, 16f, GUILayout.ExpandWidth(true));
            Color previous = GUI.color;
            GUI.color = new Color(0.08f, 0.08f, 0.08f, 1f);
            GUI.DrawTexture(rect, Texture2D.whiteTexture);
            GUI.color = new Color(0.2f, 0.75f, 0.25f, 1f);
            GUI.DrawTexture(new Rect(rect.x, rect.y, rect.width * Mathf.Clamp01(value01), rect.height),
                Texture2D.whiteTexture);
            GUI.color = previous;
            GUI.Box(rect, GUIContent.none);
        }
    }
}
