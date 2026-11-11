using System.Collections.Generic;
using Neo.Abilities;
using Neo.Tools;
using UnityEngine;

namespace Neo.Samples.Survivor
{
    /// <summary>
    ///     Game loop of the compact Vampire-Survivors-style demo built on Neo.Abilities + the Core resource/level
    ///     systems. The scene is authored (SurvivorDemoBuilder): camera, arena, <see cref="AbilitySystemBehaviour" />
    ///     with the config's library and projectile archetypes, the pooled enemy/orb/projectile templates, the player
    ///     and the <see cref="SurvivorHudView" /> are saved objects referenced below, so the whole wiring is visible
    ///     in the Hierarchy without entering Play Mode. This component only runs the game from its
    ///     <see cref="SurvivorConfig" />: spawning, XP and level-ups, upgrades, camera follow and game over.
    /// </summary>
    [AddComponentMenu("Neoxider/Survivor Demo/Survivor Game")]
    public sealed class SurvivorGame : MonoBehaviour
    {
        [Tooltip("The game as data: player, enemy spawn table, upgrades and tuning.")]
        [SerializeField] private SurvivorConfig _config;

        [Tooltip("Camera background color (applied by the scene builder).")]
        [SerializeField] private Color _background = new Color(0.05f, 0.06f, 0.12f);

        [Header("Authored scene")]
        [Tooltip("Ability hub holding the config's library and the projectile archetype templates.")]
        [SerializeField] private AbilitySystemBehaviour _hub;

        [SerializeField] private SurvivorHudView _hud;
        [SerializeField] private Camera _camera;

        [Tooltip("Parent of spawned enemies and XP orbs.")]
        [SerializeField] private Transform _actors;

        [Tooltip("Player avatar; re-registered from its UnitTemplate at the start of every run.")]
        [SerializeField] private SurvivorPlayerController _player;

        [Tooltip("Inactive enemy templates; matched to the config's enemy types by their AbilityUnitBehaviour template.")]
        [SerializeField] private SurvivorEnemy[] _enemyTemplates = System.Array.Empty<SurvivorEnemy>();

        [Tooltip("Inactive XP orb template.")]
        [SerializeField] private SurvivorXpOrb _orbTemplate;

        [Tooltip("Projectile templates bound in the hub's archetypes (their pools are retired with this game).")]
        [SerializeField] private GameObject[] _projectileTemplates = System.Array.Empty<GameObject>();

        private readonly List<SurvivorEnemy> _enemies = new List<SurvivorEnemy>();
        private readonly List<SurvivorXpOrb> _orbs = new List<SurvivorXpOrb>();
        private readonly Dictionary<SurvivorUpgrade, int> _upgradeTaken = new Dictionary<SurvivorUpgrade, int>();
        private readonly List<SurvivorUpgrade> _offerScratch = new List<SurvivorUpgrade>(3);
        private readonly List<SurvivorUpgrade> _offerPool = new List<SurvivorUpgrade>(16);
        private readonly List<AbilitySlot> _slotScratch = new List<AbilitySlot>(8);
        private readonly List<string> _pipLabels = new List<string>(8);

        private readonly Dictionary<SurvivorEnemyType, GameObject> _enemyTemplateByType =
            new Dictionary<SurvivorEnemyType, GameObject>();

        // WHY: exact source identities survive Unity's destroyed-object comparison during scene teardown;
        // storage roots may be shared by unrelated sources with the same name.
        private readonly List<GameObject> _pooledTemplates = new List<GameObject>();

        private float _time;
        private float _spawnTimer;
        private int _level;
        private int _xp;
        private int _pendingLevelUps;
        private int _kills;
        private bool _built;
        private bool _gameOver;
        private bool _levelUpOpen;
        private bool _warnedUnbuilt;

        public static SurvivorGame Active { get; private set; }

        public SurvivorConfig Config => _config;
        public SurvivorPlayerController Player => _player;
        public SurvivorHudView Hud => _hud;
        public bool IsPaused { get; private set; }
        public bool IsRunning => _built && !_gameOver;
        public bool IsGameOver => _gameOver;
        public bool IsLevelUpOpen => _levelUpOpen;
        public int Level => _level;
        public int Kills => _kills;
        public float SurvivedSeconds => _time;
        public int ActiveEnemyCount => _enemies.Count;

        /// <summary>True when the scene builder assigned every reference the game loop needs.</summary>
        public bool IsSceneBuilt => _config != null && _hub != null && _hud != null && _camera != null &&
                                    _actors != null && _player != null && _orbTemplate != null &&
                                    _enemyTemplates != null && _enemyTemplates.Length > 0;

        private void Awake()
        {
            Active = this;
            // WHY: capture the exact template identities while the references are alive, so removing this game
            // retires their pools even when it never started (unauthored scene) or is destroyed during teardown.
            TrackPooledTemplates();
        }

        private void OnDestroy()
        {
            if (Active == this)
            {
                Active = null;
            }

            try { ReleaseAllActors(); }
            finally { RetireOwnedPools(); }
        }

        private void Start()
        {
            if (_config == null)
            {
                WarnOnce("SurvivorGame: no SurvivorConfig assigned. Run Neoxider/Demo/Rebuild Active Demo Scene.");
                enabled = false;
                return;
            }

            if (!IsSceneBuilt || _hud != null && !_hud.IsBuilt)
            {
                WarnOnce("SurvivorGame: the scene is not authored (hub, HUD, camera, player or templates missing). " +
                         "Run Neoxider/Demo/Rebuild Active Demo Scene.");
                enabled = false;
                return;
            }

            if (PoolManager.I == null)
            {
                WarnOnce("SurvivorGame: the scene has no PoolManager. Run Neoxider/Demo/Rebuild Active Demo Scene.");
                enabled = false;
                return;
            }

            RegisterContent();
            MapEnemyTemplates();
            _built = true;
            StartRun();
        }

        private void Update()
        {
            if (!_built || IsPaused || _gameOver)
            {
                return;
            }

            float dt = Time.deltaTime;
            _time += dt;
            _hud.SetTimer(_time);

            if (_player == null || !_player.IsAlive)
            {
                GameOver();
                return;
            }

            TickSpawning(dt);
            FollowCamera(dt);
            RefreshHud();
        }

        /// <summary>Button target (game-over screen and the kit panel): starts a fresh run in the same scene.</summary>
        public void RestartRun()
        {
            if (_built)
            {
                StartRun();
            }
        }

        /// <summary>Button target (upgrade cards): applies the card at <paramref name="offerIndex" /> of the open offer.</summary>
        public void PickUpgrade(int offerIndex)
        {
            if (!_levelUpOpen)
            {
                return;
            }

            if (offerIndex >= 0 && offerIndex < _offerScratch.Count)
            {
                ApplyUpgrade(_offerScratch[offerIndex]);
            }

            _levelUpOpen = false;
            _pendingLevelUps = Mathf.Max(0, _pendingLevelUps - 1);
            _hud.HideUpgrades();

            if (_pendingLevelUps > 0)
            {
                OpenLevelUp();
            }
            else
            {
                CloseLevelUp();
            }
        }

        /// <summary>Button target (kit panel): adds exactly the XP missing for the next level to open the picker.</summary>
        public void GrantLevelUp()
        {
            if (!_built || _gameOver || IsPaused)
            {
                return;
            }

            AddXp(Mathf.Max(1, _config.XpForLevel(_level) - _xp));
        }

        private void RetireOwnedPools()
        {
            foreach (GameObject template in _pooledTemplates)
            {
                // WHY: RetirePool accepts an already-destroyed Unity source; do not use template == null.
                SpawnUtility.RetirePool(template);
            }

            _pooledTemplates.Clear();
        }

        private void RegisterContent()
        {
            // WHY: the authored hub already lists the library; registering again is idempotent and keeps a
            // config swapped in the Inspector playable without rebuilding the scene.
            if (_config.Library != null)
            {
                _hub.AddLibrary(_config.Library);
            }

            AbilitySystem system = _hub.System;
            for (int i = 0; i < _config.Upgrades.Count; i++)
            {
                SurvivorUpgrade up = _config.Upgrades[i];
                if (up == null)
                {
                    continue;
                }

                if (up.Modifier != null)
                {
                    system.RegisterModifier(up.Modifier.Blueprint);
                }

                if (up.Ability != null)
                {
                    system.RegisterAbility(up.Ability.Blueprint);
                }
            }
        }

        private void MapEnemyTemplates()
        {
            _enemyTemplateByType.Clear();
            for (int i = 0; i < _config.Enemies.Count; i++)
            {
                SurvivorEnemyType type = _config.Enemies[i];
                if (type == null || type.Template == null)
                {
                    continue;
                }

                GameObject template = FindEnemyTemplate(type.Template);
                if (template != null)
                {
                    _enemyTemplateByType[type] = template;
                }
                else
                {
                    WarnOnce("SurvivorGame: no authored enemy template for unit template '" + type.Template.name +
                             "'. Rebuild the scene after changing the config's enemy list.");
                }
            }
        }

        private GameObject FindEnemyTemplate(UnitTemplate unitTemplate)
        {
            for (int i = 0; i < _enemyTemplates.Length; i++)
            {
                SurvivorEnemy candidate = _enemyTemplates[i];
                if (candidate == null)
                {
                    continue;
                }

                // WHY: templates stay inactive, so read the serialized template instead of the Awake-cached unit.
                AbilityUnitBehaviour unit = candidate.GetComponent<AbilityUnitBehaviour>();
                if (unit != null && unit.Template == unitTemplate)
                {
                    return candidate.gameObject;
                }
            }

            return null;
        }

        private void TrackPooledTemplates()
        {
            _pooledTemplates.Clear();
            for (int i = 0; _enemyTemplates != null && i < _enemyTemplates.Length; i++)
            {
                if (_enemyTemplates[i] != null)
                {
                    _pooledTemplates.Add(_enemyTemplates[i].gameObject);
                }
            }

            if (_orbTemplate != null)
            {
                _pooledTemplates.Add(_orbTemplate.gameObject);
            }

            for (int i = 0; _projectileTemplates != null && i < _projectileTemplates.Length; i++)
            {
                if (_projectileTemplates[i] != null)
                {
                    _pooledTemplates.Add(_projectileTemplates[i]);
                }
            }
        }

        private void StartRun()
        {
            ReleaseAllActors();
            _upgradeTaken.Clear();

            _time = 0f;
            _spawnTimer = 0.6f;
            _level = 1;
            _xp = 0;
            _pendingLevelUps = 0;
            _kills = 0;
            _gameOver = false;
            _levelUpOpen = false;
            IsPaused = false;
            _hub.Paused = false;

            RespawnPlayer();
            _camera.transform.position = new Vector3(0f, 0f, -10f);

            _hud.HideUpgrades();
            _hud.HideGameOver();
            _hud.SetLevel(_level);
            _hud.SetKills(_kills);
            _hud.SetTimer(0f);
            _hud.SetXp(0f, _config.XpForLevel(_level));
            RefreshAbilityPips();
            RefreshHud();
        }

        private void RespawnPlayer()
        {
            // WHY: a disable/enable cycle destroys the domain unit and registers a fresh one from the UnitTemplate,
            // which resets health, granted abilities, modifiers and cooldowns without recreating the object.
            GameObject avatar = _player.gameObject;
            avatar.SetActive(false);
            avatar.transform.position = Vector3.zero;
            AbilityUnitBehaviour unit = avatar.GetComponent<AbilityUnitBehaviour>();
            if (unit != null && _config.PlayerTemplate != null)
            {
                // WHY: SetTemplate only applies while disabled; following the config keeps a swapped config playable.
                unit.SetTemplate(_config.PlayerTemplate);
            }

            avatar.SetActive(true);
            _player.Initialize(this);
        }

        private void TickSpawning(float dt)
        {
            _spawnTimer -= dt;
            if (_spawnTimer > 0f)
            {
                return;
            }

            _spawnTimer = _config.SpawnIntervalAt(_time);
            SpawnEnemy();
        }

        private void SpawnEnemy()
        {
            SurvivorEnemyType type = PickEnemyType();
            if (type == null || !_enemyTemplateByType.TryGetValue(type, out GameObject template))
            {
                return;
            }

            Vector3 origin = _player != null ? _player.transform.position : Vector3.zero;
            float angle = Random.value * Mathf.PI * 2f;
            float dist = _config.CameraSize + 1.5f;
            Vector3 pos = origin + new Vector3(Mathf.Cos(angle), Mathf.Sin(angle), 0f) * dist;
            float e = _config.ArenaExtent;
            pos.x = Mathf.Clamp(pos.x, -e, e);
            pos.y = Mathf.Clamp(pos.y, -e, e);
            pos.z = 0f;

            GameObject go = PoolManager.Get(template, pos, Quaternion.identity);
            if (go == null)
            {
                return;
            }

            go.transform.SetParent(_actors, true);
            SurvivorEnemy enemy = go.GetComponent<SurvivorEnemy>();
            enemy.Spawned(type, this, _config.EnemyHealthMultiplier(_time));
            _enemies.Add(enemy);
        }

        private SurvivorEnemyType PickEnemyType()
        {
            float total = 0f;
            for (int i = 0; i < _config.Enemies.Count; i++)
            {
                SurvivorEnemyType t = _config.Enemies[i];
                if (IsSpawnable(t))
                {
                    total += Mathf.Max(0f, t.SpawnWeight);
                }
            }

            if (total <= 0f)
            {
                return null;
            }

            float roll = Random.value * total;
            for (int i = 0; i < _config.Enemies.Count; i++)
            {
                SurvivorEnemyType t = _config.Enemies[i];
                if (!IsSpawnable(t))
                {
                    continue;
                }

                roll -= Mathf.Max(0f, t.SpawnWeight);
                if (roll <= 0f)
                {
                    return t;
                }
            }

            return null;
        }

        private bool IsSpawnable(SurvivorEnemyType type)
        {
            return type != null && type.Template != null && _time >= type.UnlockTime &&
                   _enemyTemplateByType.ContainsKey(type);
        }

        /// <summary>Nearest living enemy to a point within range, or null. Used by the player's auto-caster.</summary>
        public SurvivorEnemy FindNearestEnemy(Vector3 point, float range)
        {
            SurvivorEnemy best = null;
            float bestSqr = range * range;
            for (int i = 0; i < _enemies.Count; i++)
            {
                SurvivorEnemy e = _enemies[i];
                if (e == null || e.UnitBehaviour == null || !e.UnitBehaviour.IsAlive)
                {
                    continue;
                }

                float sqr = (e.transform.position - point).sqrMagnitude;
                if (sqr <= bestSqr)
                {
                    bestSqr = sqr;
                    best = e;
                }
            }

            return best;
        }

        public void HandleEnemyDeath(SurvivorEnemy enemy)
        {
            _enemies.Remove(enemy);
            _kills++;
            _hud.SetKills(_kills);
            SpawnOrb(enemy.transform.position, enemy.Type != null ? enemy.Type.XpReward : 1);
            PoolManager.Release(enemy.gameObject);
        }

        public void HandleOrbCollected(SurvivorXpOrb orb, int value)
        {
            _orbs.Remove(orb);
            PoolManager.Release(orb.gameObject);
            AddXp(value);
        }

        private void SpawnOrb(Vector3 position, int value)
        {
            if (_orbTemplate == null || value <= 0)
            {
                return;
            }

            position.z = 0f;
            GameObject go = PoolManager.Get(_orbTemplate.gameObject, position, Quaternion.identity);
            if (go == null)
            {
                return;
            }

            go.transform.SetParent(_actors, true);
            SurvivorXpOrb orb = go.GetComponent<SurvivorXpOrb>();
            orb.Spawned(this, value);
            _orbs.Add(orb);
        }

        public void AddXp(int value)
        {
            _xp += value;
            int needed = _config.XpForLevel(_level);
            while (_xp >= needed)
            {
                _xp -= needed;
                _level++;
                _pendingLevelUps++;
                needed = _config.XpForLevel(_level);
            }

            _hud.SetLevel(_level);
            _hud.SetXp(_xp, needed);

            if (_pendingLevelUps > 0 && !IsPaused && !_gameOver)
            {
                OpenLevelUp();
            }
        }

        private void OpenLevelUp()
        {
            IsPaused = true;
            _hub.Paused = true;

            BuildOffer(_offerScratch);
            if (_offerScratch.Count == 0)
            {
                _pendingLevelUps = 0;
                CloseLevelUp();
                return;
            }

            _levelUpOpen = true;
            _hud.ShowUpgrades(_offerScratch);
        }

        private void CloseLevelUp()
        {
            _levelUpOpen = false;
            IsPaused = false;
            _hub.Paused = false;
        }

        private void ApplyUpgrade(SurvivorUpgrade upgrade)
        {
            AbilityUnit unit = _player != null ? _player.Unit : null;
            if (unit == null || upgrade == null)
            {
                return;
            }

            // WHY: the upgrade asset itself is the identity - an instance id needs a per-Unity-version
            // switch (6.5 replaced it with EntityId) and buys nothing here.
            _upgradeTaken.TryGetValue(upgrade, out int taken);
            _upgradeTaken[upgrade] = taken + 1;

            switch (upgrade.Kind)
            {
                case SurvivorUpgradeKind.PermanentModifier:
                    if (upgrade.Modifier != null)
                    {
                        unit.System.ApplyModifier(upgrade.Modifier.Blueprint.Id, unit.Id, unit.Id);
                    }

                    break;

                case SurvivorUpgradeKind.GrantAbility:
                    if (upgrade.Ability != null)
                    {
                        unit.System.RegisterAbility(upgrade.Ability.Blueprint);
                        _player.Grant(upgrade.Ability.Blueprint.Id);
                        RefreshAbilityPips();
                    }

                    break;

                case SurvivorUpgradeKind.MaxHealth:
                    float newMax = unit.MaxHealth + upgrade.HealthBonus;
                    unit.Resources.SetMax(AbilityResourceIds.Health, newMax);
                    unit.Resources.Restore(AbilityResourceIds.Health);
                    break;
            }
        }

        private void BuildOffer(List<SurvivorUpgrade> result)
        {
            result.Clear();
            _offerPool.Clear();
            for (int i = 0; i < _config.Upgrades.Count; i++)
            {
                SurvivorUpgrade up = _config.Upgrades[i];
                if (up == null)
                {
                    continue;
                }

                if (up.MaxTimes > 0)
                {
                    _upgradeTaken.TryGetValue(up, out int taken);
                    if (taken >= up.MaxTimes)
                    {
                        continue;
                    }
                }

                // WHY: A grant-ability upgrade the player already owns is not useful twice.
                if (up.Kind == SurvivorUpgradeKind.GrantAbility && up.Ability != null && PlayerHasAbility(up.Ability.Blueprint.Id))
                {
                    continue;
                }

                _offerPool.Add(up);
            }

            int cards = Mathf.Min(3, _hud.CardCount);
            for (int n = 0; n < cards && _offerPool.Count > 0; n++)
            {
                int idx = Random.Range(0, _offerPool.Count);
                result.Add(_offerPool[idx]);
                _offerPool.RemoveAt(idx);
            }
        }

        private bool PlayerHasAbility(string abilityId)
        {
            AbilityUnit unit = _player != null ? _player.Unit : null;
            if (unit == null)
            {
                return false;
            }

            unit.System.GetSlots(unit.Id, _slotScratch);
            for (int i = 0; i < _slotScratch.Count; i++)
            {
                if (string.Equals(_slotScratch[i].Blueprint.Id, abilityId, System.StringComparison.OrdinalIgnoreCase))
                {
                    return true;
                }
            }

            return false;
        }

        private void RefreshHud()
        {
            AbilityUnit unit = _player != null ? _player.Unit : null;
            if (unit != null)
            {
                _hud.SetHealth(unit.Health, unit.MaxHealth);
            }

            _hud.SetXp(_xp, _config.XpForLevel(_level));

            if (unit == null)
            {
                return;
            }

            unit.System.GetSlots(unit.Id, _slotScratch);
            for (int i = 0; i < _slotScratch.Count; i++)
            {
                _hud.SetPip(i, _slotScratch[i].NormalizedCooldown);
            }
        }

        private void RefreshAbilityPips()
        {
            AbilityUnit unit = _player != null ? _player.Unit : null;
            _pipLabels.Clear();
            if (unit != null)
            {
                unit.System.GetSlots(unit.Id, _slotScratch);
                for (int i = 0; i < _slotScratch.Count; i++)
                {
                    string dn = _slotScratch[i].Blueprint.DisplayName;
                    _pipLabels.Add(string.IsNullOrEmpty(dn) ? "?" : dn.Substring(0, 1).ToUpperInvariant());
                }
            }

            _hud.SetAbilities(_pipLabels);
        }

        private void FollowCamera(float dt)
        {
            if (_camera == null || _player == null)
            {
                return;
            }

            float halfH = _config.CameraSize;
            float halfW = halfH * _camera.aspect;
            float e = _config.ArenaExtent;
            Vector3 target = _player.transform.position;
            float limX = Mathf.Max(0f, e - halfW);
            float limY = Mathf.Max(0f, e - halfH);
            target.x = Mathf.Clamp(target.x, -limX, limX);
            target.y = Mathf.Clamp(target.y, -limY, limY);
            target.z = -10f;
            _camera.transform.position = Vector3.Lerp(_camera.transform.position, target,
                1f - Mathf.Exp(-10f * dt));
        }

        private void GameOver()
        {
            if (_gameOver)
            {
                return;
            }

            _gameOver = true;
            _levelUpOpen = false;
            IsPaused = true;
            _hub.Paused = true;
            _hud.HideUpgrades();

            int s = Mathf.FloorToInt(_time);
            string stats = $"Survived  {s / 60:00}:{s % 60:00}\n{_kills} kills   ·   reached level {_level}";
            _hud.ShowGameOver(stats);
        }

        private void ReleaseAllActors()
        {
            for (int i = _enemies.Count - 1; i >= 0; i--)
            {
                if (_enemies[i] != null)
                {
                    PoolManager.Release(_enemies[i].gameObject);
                }
            }

            _enemies.Clear();

            for (int i = _orbs.Count - 1; i >= 0; i--)
            {
                if (_orbs[i] != null)
                {
                    PoolManager.Release(_orbs[i].gameObject);
                }
            }

            _orbs.Clear();
        }

        private void WarnOnce(string message)
        {
            if (_warnedUnbuilt)
            {
                return;
            }

            _warnedUnbuilt = true;
            global::Neo.Demo.SampleDiagnostics.Warn(message, this);
        }
    }
}
