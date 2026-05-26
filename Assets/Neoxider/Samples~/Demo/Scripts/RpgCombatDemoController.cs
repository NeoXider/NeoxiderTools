using System;
using Neo.Rpg.Components;
using UnityEngine;

namespace Neo.Rpg.Demo
{
    [AddComponentMenu("Neoxider/Samples/RPG Combat Demo Controller")]
    public sealed class RpgCombatDemoController : MonoBehaviour
    {
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

        private float _nextPlayerAttackTime;
        private float _nextEnemyPressureTime;
        private float _restoreAtTime = -1f;
        private int _enemyDeaths;
        private string _lastEvent = "Ready";

        public RpgCharacter Player => _player;
        public int EnemyCount => _enemies != null ? _enemies.Length : 0;
        public int LivingEnemyCount => CountLivingEnemies();
        public bool PlayerDead => _player != null && _player.IsDead;
        public string LastEvent => _lastEvent;

        private void Awake()
        {
            AutoResolve();
            RegisterDeathListeners();
        }

        private void OnDestroy()
        {
            if (_player != null)
            {
                _player.OnDeathEvent.RemoveListener(HandlePlayerDeath);
            }
        }

        private void Update()
        {
            if (!_autoDemoLoop || _player == null || _player.IsDead)
            {
                return;
            }

            if (_autoRestoreWhenCleared && LivingEnemyCount == 0)
            {
                if (_restoreAtTime < 0f)
                {
                    _restoreAtTime = Time.time + _restoreAfterClearDelay;
                    _lastEvent = "Wave cleared; restoring soon";
                }
                else if (Time.time >= _restoreAtTime)
                {
                    RestoreAll();
                }

                return;
            }

            _restoreAtTime = -1f;

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

        private void OnGUI()
        {
            const float width = 360f;
            GUILayout.BeginArea(new Rect(16f, 16f, width, 260f), GUI.skin.window);
            GUILayout.Label("RPG Combat Demo");
            DrawCharacter("Player", _player);

            GUILayout.Space(6f);
            GUILayout.Label($"Enemies: {LivingEnemyCount}/{EnemyCount} alive | Deaths: {_enemyDeaths}");
            GUILayout.Label($"Last: {_lastEvent}");

            GUILayout.Space(8f);
            GUILayout.BeginHorizontal();
            if (GUILayout.Button("Player Hit")) DamageNearestEnemy();
            if (GUILayout.Button("Enemy Hit")) PressurePlayer();
            GUILayout.EndHorizontal();

            GUILayout.BeginHorizontal();
            _autoDemoLoop = GUILayout.Toggle(_autoDemoLoop, "Auto loop");
            _enemyPressureEnabled = GUILayout.Toggle(_enemyPressureEnabled, "Enemy pressure");
            GUILayout.EndHorizontal();

            if (GUILayout.Button("Restore All"))
            {
                RestoreAll();
            }

            if (PlayerDead)
            {
                GUILayout.Space(4f);
                GUILayout.Label("<color=red>Player dead. Press Restore All.</color>");
            }

            GUILayout.EndArea();
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

            _playerAttack ??= _player != null ? _player.GetComponent<RpgAttackController>() : null;

            if (_enemies == null || _enemies.Length == 0)
            {
                var found = new System.Collections.Generic.List<RpgCharacter>();
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

        [Button]
        public void DamageNearestEnemy()
        {
            RpgCharacter target = FindNearestLivingEnemy();
            if (target == null)
            {
                _lastEvent = "No living enemy";
                return;
            }

            FaceTarget(_player != null ? _player.transform : transform, target.transform);
            _playerAttack?.UsePrimaryAttack();

            float dealt = target.DamageType("Demo", _playerDemoDamage);
            _lastEvent = dealt > 0f
                ? $"Player dealt {dealt:0} to {target.name}"
                : $"Player attack did not affect {target.name}";
        }

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
                _lastEvent = "No enemy pressure";
                return;
            }

            float dealt = _player.DamageType("Enemy", _enemyPressureDamage);
            _lastEvent = dealt > 0f
                ? $"{attacker.name} dealt {dealt:0} to player"
                : "Enemy pressure did not affect player";
        }

        [Button]
        public void RestoreAll()
        {
            _enemyDeaths = 0;
            _restoreAtTime = -1f;
            _player?.Restore();

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

            _lastEvent = "Restored all combatants";
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
                if (enemy == null)
                {
                    continue;
                }

                RpgCharacter captured = enemy;
                enemy.OnDeathEvent.AddListener(() => HandleEnemyDeath(captured));
            }
        }

        private void HandlePlayerDeath()
        {
            _lastEvent = "Player died";
        }

        private void HandleEnemyDeath(RpgCharacter enemy)
        {
            _enemyDeaths++;
            _lastEvent = $"{enemy.name} died";

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

        private static void DrawCharacter(string label, RpgCharacter character)
        {
            if (character == null)
            {
                GUILayout.Label($"{label}: missing");
                return;
            }

            GUILayout.Label($"{label}: HP {character.HpValue:0}/{character.MaxHpValue:0} | " +
                            $"Level {character.LevelValue} | Dead {character.IsDead}");
            DrawBar(character.HpPercentValue);
        }

        private static void DrawBar(float value01)
        {
            Rect rect = GUILayoutUtility.GetRect(1f, 14f, GUILayout.ExpandWidth(true));
            Color previous = GUI.color;
            GUI.color = new Color(0.08f, 0.08f, 0.08f, 1f);
            GUI.DrawTexture(rect, Texture2D.whiteTexture);
            GUI.color = new Color(0.25f, 0.8f, 0.25f, 1f);
            GUI.DrawTexture(new Rect(rect.x, rect.y, rect.width * Mathf.Clamp01(value01), rect.height),
                Texture2D.whiteTexture);
            GUI.color = previous;
            GUI.Box(rect, GUIContent.none);
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
