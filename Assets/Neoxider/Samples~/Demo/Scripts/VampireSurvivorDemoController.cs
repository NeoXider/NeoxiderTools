using System;
using System.Collections.Generic;
using Neo.NPC;
using Neo.Rpg;
using Neo.Rpg.Components;
using Neo.Samples.Survivor;
using Neo.Tools;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

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

        private Canvas _hudCanvas;
        private TMP_Text _playerText;
        private Image _hpFill;
        private TMP_Text _statsText;
        private TMP_Text _spawnerText;
        private TMP_Text _eventText;
        private GameObject _deathOverlay;
        private float _nextHudRefreshTime;

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

            BuildHud();
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

            RefreshHud();
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

        /// <summary>Builds the whole demo HUD in code: compact top-left status panel plus a death overlay.</summary>
        private void BuildHud()
        {
            var canvasGo = new GameObject("VampireDemo HUD", typeof(Canvas), typeof(CanvasScaler),
                typeof(GraphicRaycaster));
            canvasGo.transform.SetParent(transform, false);
            _hudCanvas = canvasGo.GetComponent<Canvas>();
            _hudCanvas.renderMode = RenderMode.ScreenSpaceOverlay;
            _hudCanvas.sortingOrder = 100;
            var scaler = canvasGo.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920f, 1080f);
            scaler.matchWidthOrHeight = 0.5f;

            if (FindFirstObjectByType<EventSystem>() == null)
            {
                var eventSystem = new GameObject("EventSystem", typeof(EventSystem), typeof(StandaloneInputModule));
                eventSystem.transform.SetParent(transform, false);
            }

            var root = (RectTransform)canvasGo.transform;
            BuildStatusPanel(root);
            BuildDeathOverlay(root);
            RefreshHud();
        }

        private void BuildStatusPanel(RectTransform root)
        {
            Image panel = SurvivorUI.Image("StatusPanel", root, SurvivorUI.Ink);
            SurvivorUI.Anchor(panel.rectTransform, new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(0f, 1f),
                new Vector2(16f, -16f), new Vector2(400f, 236f));

            TMP_Text title = SurvivorUI.Label("Title", panel.transform, "Vampire Survivor Demo", 20f,
                SurvivorUI.Text, TextAlignmentOptions.Left, FontStyles.Bold);
            Row((RectTransform)title.transform, -10f, 26f);

            _playerText = SurvivorUI.Label("Player", panel.transform, "Player: missing", 14f, SurvivorUI.Text);
            Row((RectTransform)_playerText.transform, -38f, 20f);

            _hpFill = SurvivorUI.Bar("HPBar", panel.transform, SurvivorUI.Good, out RectTransform hpTrack);
            Row(hpTrack, -60f, 12f);

            _statsText = SurvivorUI.Label("Stats", panel.transform, "", 14f, SurvivorUI.Text);
            Row((RectTransform)_statsText.transform, -78f, 20f);

            _spawnerText = SurvivorUI.Label("Spawners", panel.transform, "", 14f, SurvivorUI.Text);
            Row((RectTransform)_spawnerText.transform, -98f, 20f);

            _eventText = SurvivorUI.Label("Event", panel.transform, "Last: Ready", 14f, SurvivorUI.Muted);
            Row((RectTransform)_eventText.transform, -118f, 20f);

            RectTransform rowA = Row(SurvivorUI.Rect("ButtonsA", panel.transform), -146f, 34f);
            HudButton(rowA, "Start", SurvivorUI.Good, 0f, 121f, StartSpawners);
            HudButton(rowA, "Stop", SurvivorUI.Danger, 127f, 121f, StopSpawners);
            HudButton(rowA, "Damage Player", SurvivorUI.Accent, 254f, 122f, () => DamagePlayer(25f));

            RectTransform rowB = Row(SurvivorUI.Rect("ButtonsB", panel.transform), -186f, 34f);
            HudButton(rowB, "Kill Nearest", SurvivorUI.Cyan, 0f, 185f, KillNearestEnemy);
            HudButton(rowB, "Reset Run", SurvivorUI.Panel, 191f, 185f, ResetRun);
        }

        private void BuildDeathOverlay(RectTransform root)
        {
            Image panel = SurvivorUI.Image("DeathOverlay", root, new Color(0.04f, 0.04f, 0.07f, 0.9f));
            SurvivorUI.Anchor(panel.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
                new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(380f, 150f));
            _deathOverlay = panel.gameObject;

            TMP_Text title = SurvivorUI.Label("Title", panel.transform, "YOU DIED", 34f, SurvivorUI.Danger,
                TextAlignmentOptions.Center, FontStyles.Bold);
            SurvivorUI.Anchor((RectTransform)title.transform, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f),
                new Vector2(0.5f, 1f), new Vector2(0f, -30f), new Vector2(360f, 44f));

            TMP_Text hint = SurvivorUI.Label("Hint", panel.transform, "Use Reset Run to restart.", 16f,
                SurvivorUI.Muted, TextAlignmentOptions.Center);
            SurvivorUI.Anchor((RectTransform)hint.transform, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f),
                new Vector2(0.5f, 1f), new Vector2(0f, -86f), new Vector2(360f, 26f));

            _deathOverlay.SetActive(false);
        }

        private void RefreshHud()
        {
            if (_hudCanvas == null)
            {
                return;
            }

            if (_player != null)
            {
                float hp01 = _player.HpPercentValue;
                SurvivorUI.SetFill(_hpFill, hp01);
                _hpFill.color = Color.Lerp(SurvivorUI.Danger, SurvivorUI.Good, hp01);
            }

            bool dead = PlayerDead;
            if (_deathOverlay.activeSelf != dead)
            {
                _deathOverlay.SetActive(dead);
            }

            // WHY: rebuilding label strings allocates; 10 Hz is imperceptible for a status readout.
            if (Time.unscaledTime < _nextHudRefreshTime)
            {
                return;
            }

            _nextHudRefreshTime = Time.unscaledTime + 0.1f;

            _playerText.text = _player == null
                ? "Player: missing"
                : $"Player HP: {_player.HpValue:0}/{_player.MaxHpValue:0} | " +
                  $"Level {_player.LevelValue} | Dead {_player.IsDead}";
            _statsText.text = $"Enemies: {ActiveEnemyCount} active | Spawned: {_spawnedCount} | Killed: {_killCount}";
            _spawnerText.text = $"Spawners: {CountRunningSpawners()}/{(_spawners != null ? _spawners.Length : 0)} running";
            _eventText.text = $"Last: {_lastEvent}";
        }

        /// <summary>Anchors a child to the panel top edge, stretched horizontally with side padding.</summary>
        private static RectTransform Row(RectTransform rt, float top, float height)
        {
            rt.anchorMin = new Vector2(0f, 1f);
            rt.anchorMax = new Vector2(1f, 1f);
            rt.pivot = new Vector2(0.5f, 1f);
            rt.offsetMin = new Vector2(12f, top - height);
            rt.offsetMax = new Vector2(-12f, top);
            return rt;
        }

        private static void HudButton(RectTransform parent, string label, Color color, float x, float width,
            UnityEngine.Events.UnityAction onClick)
        {
            Button button = SurvivorUI.Button(label, parent, color);
            var rt = (RectTransform)button.transform;
            rt.anchorMin = new Vector2(0f, 0f);
            rt.anchorMax = new Vector2(0f, 1f);
            rt.pivot = new Vector2(0f, 0.5f);
            rt.anchoredPosition = new Vector2(x, 0f);
            rt.sizeDelta = new Vector2(width, 0f);
            button.onClick.AddListener(onClick);

            TMP_Text text = SurvivorUI.Label("Label", button.transform, label, 15f, Color.white,
                TextAlignmentOptions.Center, FontStyles.Bold);
            SurvivorUI.Stretch((RectTransform)text.transform);
        }
    }
}
