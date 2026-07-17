using System.Collections.Generic;
using Neo.Abilities;
using Neo.Tools;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace Neo.Samples.Survivor
{
    /// <summary>
    ///     A compact, modular Vampire-Survivors-style demo built entirely on Neo.Abilities + the Core
    ///     resource/level systems. Everything — arena, camera, player, enemies, projectiles and HUD —
    ///     is assembled in code from a single <see cref="SurvivorConfig" /> asset, so swapping the config
    ///     (templates, abilities, modifiers, upgrades) produces a different game on the same code. Drop
    ///     this one component into an empty scene, assign a config, and press Play.
    /// </summary>
    [AddComponentMenu("Neoxider/Survivor Demo/Survivor Game")]
    public sealed class SurvivorGame : MonoBehaviour
    {
        [Tooltip("The game as data. Swap it to build a different survivor game on the same code.")]
        [SerializeField] private SurvivorConfig _config;

        [Tooltip("Camera background color.")]
        [SerializeField] private Color _background = new Color(0.05f, 0.06f, 0.12f);

        private readonly List<SurvivorEnemy> _enemies = new List<SurvivorEnemy>();
        private readonly List<SurvivorXpOrb> _orbs = new List<SurvivorXpOrb>();
        private readonly Dictionary<int, int> _upgradeTaken = new Dictionary<int, int>();
        private readonly List<SurvivorUpgrade> _offerScratch = new List<SurvivorUpgrade>(3);
        private readonly List<AbilitySlot> _slotScratch = new List<AbilitySlot>(8);

        private AbilitySystemBehaviour _hub;
        private SurvivorHud _hud;
        private Camera _camera;
        private Transform _actors;
        private Transform _templates;
        private GameObject _playerTemplateContainer;

        private readonly Dictionary<SurvivorEnemyType, GameObject> _enemyTemplates =
            new Dictionary<SurvivorEnemyType, GameObject>();

        private GameObject _orbTemplate;
        private SurvivorPlayerController _player;

        private float _time;
        private float _spawnTimer;
        private int _level;
        private int _xp;
        private int _pendingLevelUps;
        private int _kills;
        private bool _built;
        private bool _gameOver;

        public static SurvivorGame Active { get; private set; }

        public SurvivorConfig Config => _config;
        public SurvivorPlayerController Player => _player;
        public bool IsPaused { get; private set; }

        private void Awake()
        {
            Active = this;
        }

        private void OnDestroy()
        {
            if (Active == this)
            {
                Active = null;
            }
        }

        private void Start()
        {
            if (_config == null)
            {
                Debug.LogError("SurvivorGame: no SurvivorConfig assigned.", this);
                enabled = false;
                return;
            }

            BuildOnce();
            StartRun();
        }

        private void Update()
        {
            float unscaled = Time.unscaledDeltaTime;
            _hud?.Tick(unscaled);

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

        private void BuildOnce()
        {
            if (_built)
            {
                return;
            }

            if (PoolManager.I == null)
            {
                // WHY: Left at scene root so its DontDestroyOnLoad promotion is clean.
                new GameObject("PoolManager").AddComponent<PoolManager>();
            }

            _actors = new GameObject("Actors").transform;
            _actors.SetParent(transform, false);
            _templates = new GameObject("Templates").transform;
            _templates.SetParent(transform, false);
            _templates.gameObject.SetActive(false);

            SetupCamera();
            SetupArena();
            SetupSystem();
            SetupTemplates();
            SetupCanvas();

            _built = true;
        }

        private void SetupCamera()
        {
            _camera = Camera.main;
            if (_camera == null)
            {
                var camGo = new GameObject("Main Camera") { tag = "MainCamera" };
                _camera = camGo.AddComponent<Camera>();
            }

            _camera.orthographic = true;
            _camera.orthographicSize = _config.CameraSize;
            _camera.backgroundColor = _background;
            _camera.clearFlags = CameraClearFlags.SolidColor;
            _camera.transform.position = new Vector3(0f, 0f, -10f);
            _camera.transform.rotation = Quaternion.identity;
        }

        private void SetupArena()
        {
            float e = _config.ArenaExtent;

            // WHY: Neon frame (behind) — a bright rounded rect whose edge peeks around the darker floor.
            SpriteRenderer frame = NewSprite("Frame", _actors, SurvivorArt.RoundedRect,
                new Color(0.49f, 0.36f, 0.94f, 0.85f), -21);
            frame.drawMode = SpriteDrawMode.Sliced;
            frame.size = new Vector2(e * 2f + 0.9f, e * 2f + 0.9f);

            // WHY: Dark playfield floor (front) leaves a thin neon border showing.
            SpriteRenderer floor = NewSprite("Floor", _actors, SurvivorArt.RoundedRect,
                new Color(0.08f, 0.09f, 0.15f), -20);
            floor.drawMode = SpriteDrawMode.Sliced;
            floor.size = new Vector2(e * 2f + 0.4f, e * 2f + 0.4f);
        }

        private void SetupSystem()
        {
            var hubGo = new GameObject("AbilitySystem");
            hubGo.transform.SetParent(transform, false);
            _hub = hubGo.AddComponent<AbilitySystemBehaviour>();
            AbilitySystem system = _hub.System;

            if (_config.Library != null)
            {
                _hub.AddLibrary(_config.Library);
            }

            // WHY: Make sure every upgrade's content is registered even if omitted from the library.
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

            RegisterProjectileArchetypes(system);
        }

        private void RegisterProjectileArchetypes(AbilitySystem system)
        {
            var seen = new HashSet<string>();
            if (_config.Library != null)
            {
                for (int i = 0; i < _config.Library.Abilities.Count; i++)
                {
                    AbilityDefinition def = _config.Library.Abilities[i];
                    string archetype = def != null ? def.Blueprint.ProjectileArchetypeId : null;
                    if (!string.IsNullOrEmpty(archetype) && seen.Add(archetype))
                    {
                        _hub.AddArchetype(archetype, BuildProjectileTemplate(archetype));
                    }
                }
            }
        }

        private GameObject BuildProjectileTemplate(string archetype)
        {
            var go = new GameObject("Projectile_" + archetype);
            go.transform.SetParent(_templates, false);
            SpriteRenderer glow = NewSprite("Glow", go.transform, SurvivorArt.Glow,
                new Color(0.25f, 0.79f, 0.94f, 0.55f), 3);
            glow.transform.localScale = Vector3.one * 0.9f;
            SpriteRenderer core = NewSprite("Core", go.transform, SurvivorArt.Disc,
                new Color(0.85f, 0.98f, 1f), 4);
            core.transform.localScale = Vector3.one * 0.4f;
            go.AddComponent<AbilityProjectileBehaviour>();
            go.SetActive(false);
            return go;
        }

        private void SetupTemplates()
        {
            for (int i = 0; i < _config.Enemies.Count; i++)
            {
                SurvivorEnemyType type = _config.Enemies[i];
                if (type == null || type.Template == null)
                {
                    continue;
                }

                var go = new GameObject("Enemy_" + type.Template.name);
                go.transform.SetParent(_templates, false);
                go.SetActive(false);
                SpriteRenderer body = NewSprite("Body", go.transform, SurvivorArt.Disc, type.Color, 1);
                body.transform.localScale = Vector3.one * (type.Radius * 2f);
                var unit = go.AddComponent<AbilityUnitBehaviour>();
                unit.SetTemplate(type.Template);
                go.AddComponent<SurvivorEnemy>();
                _enemyTemplates[type] = go;
            }

            _orbTemplate = new GameObject("XpOrb");
            _orbTemplate.transform.SetParent(_templates, false);
            _orbTemplate.SetActive(false);
            SpriteRenderer orbGlow = NewSprite("Glow", _orbTemplate.transform, SurvivorArt.Glow,
                new Color(1f, 0.85f, 0.3f, 0.5f), 1);
            orbGlow.transform.localScale = Vector3.one * 0.55f;
            SpriteRenderer orbCore = NewSprite("Core", _orbTemplate.transform, SurvivorArt.Disc,
                new Color(1f, 0.82f, 0.28f), 2);
            orbCore.transform.localScale = Vector3.one * 0.28f;
            _orbTemplate.AddComponent<SurvivorXpOrb>();
        }

        private void SetupCanvas()
        {
            var canvasGo = new GameObject("Survivor HUD", typeof(Canvas), typeof(CanvasScaler),
                typeof(GraphicRaycaster));
            canvasGo.transform.SetParent(transform, false);
            var canvas = canvasGo.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 100;
            var scaler = canvasGo.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920f, 1080f);
            scaler.matchWidthOrHeight = 0.5f;

            if (Object.FindFirstObjectByType<EventSystem>() == null)
            {
                var es = new GameObject("EventSystem", typeof(EventSystem), typeof(StandaloneInputModule));
                es.transform.SetParent(transform, false);
            }

            _hud = new SurvivorHud();
            _hud.Build((RectTransform)canvasGo.transform);
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
            IsPaused = false;
            _hub.Paused = false;

            SpawnPlayer();

            _hud.HideUpgrades();
            _hud.HideGameOver();
            _hud.SetLevel(_level);
            _hud.SetKills(_kills);
            _hud.SetTimer(0f);
            _hud.SetXp(0f, _config.XpForLevel(_level));
            RefreshAbilityPips();
            RefreshHud();
        }

        private void SpawnPlayer()
        {
            if (_player != null)
            {
                Destroy(_player.gameObject);
            }

            var go = new GameObject("Player");
            go.transform.SetParent(_actors, false);
            go.SetActive(false);
            go.transform.position = Vector3.zero;
            SpriteRenderer glow = NewSprite("Glow", go.transform, SurvivorArt.Glow,
                new Color(_config.PlayerColor.r, _config.PlayerColor.g, _config.PlayerColor.b, 0.4f), 4);
            glow.transform.localScale = Vector3.one * 1.4f;
            SpriteRenderer body = NewSprite("Body", go.transform, SurvivorArt.Disc, _config.PlayerColor, 5);
            body.transform.localScale = Vector3.one * (_config.PlayerRadius * 2f);

            var unit = go.AddComponent<AbilityUnitBehaviour>();
            unit.SetTemplate(_config.PlayerTemplate);
            _player = go.AddComponent<SurvivorPlayerController>();
            go.SetActive(true);
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
            if (type == null || !_enemyTemplates.TryGetValue(type, out GameObject template))
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
            var enemy = go.GetComponent<SurvivorEnemy>();
            enemy.Spawned(type, this, _config.EnemyHealthMultiplier(_time));
            _enemies.Add(enemy);
        }

        private SurvivorEnemyType PickEnemyType()
        {
            float total = 0f;
            for (int i = 0; i < _config.Enemies.Count; i++)
            {
                SurvivorEnemyType t = _config.Enemies[i];
                if (t != null && t.Template != null && _time >= t.UnlockTime)
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
                if (t == null || t.Template == null || _time < t.UnlockTime)
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
            GameObject go = PoolManager.Get(_orbTemplate, position, Quaternion.identity);
            if (go == null)
            {
                return;
            }

            go.transform.SetParent(_actors, true);
            var orb = go.GetComponent<SurvivorXpOrb>();
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
                CloseLevelUp();
                return;
            }

            _hud.ShowUpgrades(_offerScratch, PickUpgrade);
        }

        private void PickUpgrade(int offerIndex)
        {
            if (offerIndex >= 0 && offerIndex < _offerScratch.Count)
            {
                ApplyUpgrade(_offerScratch[offerIndex]);
            }

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

        private void CloseLevelUp()
        {
            IsPaused = false;
            _hub.Paused = false;
        }

        private void ApplyUpgrade(SurvivorUpgrade upgrade)
        {
            AbilityUnit unit = _player != null ? _player.Unit : null;
            if (unit == null)
            {
                return;
            }

            int id = upgrade.GetInstanceID();
            _upgradeTaken.TryGetValue(id, out int taken);
            _upgradeTaken[id] = taken + 1;

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
            var pool = new List<SurvivorUpgrade>();
            for (int i = 0; i < _config.Upgrades.Count; i++)
            {
                SurvivorUpgrade up = _config.Upgrades[i];
                if (up == null)
                {
                    continue;
                }

                if (up.MaxTimes > 0)
                {
                    _upgradeTaken.TryGetValue(up.GetInstanceID(), out int taken);
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

                pool.Add(up);
            }

            for (int n = 0; n < 3 && pool.Count > 0; n++)
            {
                int idx = Random.Range(0, pool.Count);
                result.Add(pool[idx]);
                pool.RemoveAt(idx);
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

            unit?.System.GetSlots(unit.Id, _slotScratch);
            for (int i = 0; i < _slotScratch.Count; i++)
            {
                _hud.SetPip(i, _slotScratch[i].NormalizedCooldown);
            }
        }

        private void RefreshAbilityPips()
        {
            AbilityUnit unit = _player != null ? _player.Unit : null;
            var labels = new List<string>();
            if (unit != null)
            {
                unit.System.GetSlots(unit.Id, _slotScratch);
                for (int i = 0; i < _slotScratch.Count; i++)
                {
                    string dn = _slotScratch[i].Blueprint.DisplayName;
                    labels.Add(string.IsNullOrEmpty(dn) ? "?" : dn.Substring(0, 1).ToUpperInvariant());
                }
            }

            _hud.SetAbilities(labels);
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
            IsPaused = true;
            _hub.Paused = true;

            int s = Mathf.FloorToInt(_time);
            string stats = $"Survived  {s / 60:00}:{s % 60:00}\n{_kills} kills   ·   reached level {_level}";
            _hud.ShowGameOver(stats, StartRun);
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

        private static SpriteRenderer NewSprite(string name, Transform parent, Sprite sprite, Color color,
            int order)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            var sr = go.AddComponent<SpriteRenderer>();
            sr.sprite = sprite;
            sr.color = color;
            sr.sortingOrder = order;
            return sr;
        }
    }
}
