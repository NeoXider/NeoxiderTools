using System;
using Neo.Rpg.Components;
using Neo.Samples.Survivor;
using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace Neo.Rpg.Demo
{
    /// <summary>
    ///     Drives the RPG combat demo loop and builds a compact runtime uGUI HUD panel (top-left corner)
    ///     with player stats, enemy counters, action buttons and demo toggles — no imported UI assets.
    /// </summary>
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

        private TMP_Text _playerText;
        private Image _hpFill;
        private TMP_Text _enemiesText;
        private TMP_Text _lastText;
        private Toggle _autoToggle;
        private Toggle _pressureToggle;
        private GameObject _deadWarning;

        private float _shownHp = float.NaN;
        private float _shownMaxHp = float.NaN;
        private int _shownLevel = int.MinValue;
        private bool _shownDead;
        private bool _shownMissing;
        private int _shownLiving = -1;
        private int _shownTotal = -1;
        private int _shownDeaths = -1;
        private string _shownEvent;

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

        private void Start()
        {
            BuildHud();
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

        /// <summary>
        ///     Builds the corner HUD in code: overlay canvas + a rounded auto-sizing panel with the same
        ///     controls the old IMGUI window had (stats, HP bar, hit buttons, toggles, restore).
        /// </summary>
        private void BuildHud()
        {
            var canvasGo = new GameObject("Combat HUD", typeof(Canvas), typeof(CanvasScaler),
                typeof(GraphicRaycaster));
            canvasGo.transform.SetParent(transform, false);
            var canvas = canvasGo.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 100;
            var scaler = canvasGo.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920f, 1080f);
            scaler.matchWidthOrHeight = 0.5f;

            EnsureEventSystem();

            Image panel = SurvivorUI.Image("Panel", canvas.transform, SurvivorUI.Panel);
            RectTransform panelRt = panel.rectTransform;
            SurvivorUI.Anchor(panelRt, new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(0f, 1f),
                new Vector2(16f, -16f), new Vector2(360f, 0f));

            var layout = panel.gameObject.AddComponent<VerticalLayoutGroup>();
            layout.padding = new RectOffset(14, 14, 12, 14);
            layout.spacing = 8f;
            layout.childControlWidth = true;
            layout.childControlHeight = true;
            layout.childForceExpandWidth = true;
            layout.childForceExpandHeight = false;

            var fitter = panel.gameObject.AddComponent<ContentSizeFitter>();
            fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

            SurvivorUI.Label("Title", panel.transform, "RPG Combat Demo", 22f, SurvivorUI.Text,
                TextAlignmentOptions.Left, FontStyles.Bold);

            Image underline = SurvivorUI.Image("Underline", panel.transform, SurvivorUI.Accent);
            underline.raycastTarget = false;
            underline.gameObject.AddComponent<LayoutElement>().preferredHeight = 3f;

            _playerText = SurvivorUI.Label("PlayerStats", panel.transform, "Player: missing", 17f,
                SurvivorUI.Text, TextAlignmentOptions.Left, FontStyles.Bold);

            _hpFill = SurvivorUI.Bar("HPBar", panel.transform, SurvivorUI.Good, out RectTransform hpTrack);
            hpTrack.gameObject.AddComponent<LayoutElement>().preferredHeight = 16f;

            _enemiesText = SurvivorUI.Label("Enemies", panel.transform, "Enemies: 0/0 alive | Deaths: 0",
                16f, SurvivorUI.Muted);

            _lastText = SurvivorUI.Label("LastEvent", panel.transform, "Last: Ready", 16f, SurvivorUI.Cyan);

            RectTransform actions = BuildRow("Actions", panel.transform, 40f);
            AddButton(actions, "Player Hit", SurvivorUI.Accent, DamageNearestEnemy);
            AddButton(actions, "Enemy Hit", SurvivorUI.Danger, PressurePlayer);

            RectTransform toggles = BuildRow("Toggles", panel.transform, 32f);
            _autoToggle = AddToggle(toggles, "Auto loop", _autoDemoLoop, value => _autoDemoLoop = value);
            _pressureToggle = AddToggle(toggles, "Enemy pressure", _enemyPressureEnabled,
                value => _enemyPressureEnabled = value);

            Button restore = AddButton(panel.transform, "Restore All", SurvivorUI.Good, RestoreAll);
            restore.gameObject.AddComponent<LayoutElement>().preferredHeight = 40f;

            _deadWarning = SurvivorUI.Label("DeadWarning", panel.transform, "Player dead. Press Restore All.",
                16f, SurvivorUI.Danger, TextAlignmentOptions.Center, FontStyles.Bold).gameObject;
            _deadWarning.SetActive(false);
        }

        private void EnsureEventSystem()
        {
            if (UnityEngine.Object.FindFirstObjectByType<EventSystem>() != null)
            {
                return;
            }

            var go = new GameObject("EventSystem", typeof(EventSystem));
            go.transform.SetParent(transform, false);
#if ENABLE_INPUT_SYSTEM && !ENABLE_LEGACY_INPUT_MANAGER
            Type moduleType = Type.GetType(
                "UnityEngine.InputSystem.UI.InputSystemUIInputModule, Unity.InputSystem");
            if (moduleType != null)
            {
                go.AddComponent(moduleType);
            }
            else
            {
                go.AddComponent<StandaloneInputModule>();
            }
#else
            go.AddComponent<StandaloneInputModule>();
#endif
        }

        private static RectTransform BuildRow(string name, Transform parent, float height)
        {
            RectTransform row = SurvivorUI.Rect(name, parent);
            var rowLayout = row.gameObject.AddComponent<HorizontalLayoutGroup>();
            rowLayout.spacing = 8f;
            rowLayout.childControlWidth = true;
            rowLayout.childControlHeight = true;
            rowLayout.childForceExpandWidth = true;
            rowLayout.childForceExpandHeight = true;
            row.gameObject.AddComponent<LayoutElement>().preferredHeight = height;
            return row;
        }

        private static Button AddButton(Transform parent, string label, Color color, UnityAction onClick)
        {
            Button button = SurvivorUI.Button(label, parent, color);
            TMP_Text text = SurvivorUI.Label("Label", button.transform, label, 17f, Color.white,
                TextAlignmentOptions.Center, FontStyles.Bold);
            SurvivorUI.Stretch(text.rectTransform);
            button.onClick.AddListener(onClick);
            return button;
        }

        private static Toggle AddToggle(Transform parent, string label, bool initial, UnityAction<bool> onChanged)
        {
            Image background = SurvivorUI.Image(label, parent, SurvivorUI.Track);
            var toggle = background.gameObject.AddComponent<Toggle>();
            toggle.targetGraphic = background;

            Image box = SurvivorUI.Image("Box", background.transform, new Color(0f, 0f, 0f, 0.35f));
            RectTransform boxRt = box.rectTransform;
            boxRt.anchorMin = new Vector2(0f, 0.5f);
            boxRt.anchorMax = new Vector2(0f, 0.5f);
            boxRt.pivot = new Vector2(0f, 0.5f);
            boxRt.anchoredPosition = new Vector2(7f, 0f);
            boxRt.sizeDelta = new Vector2(18f, 18f);

            Image check = SurvivorUI.Image("Check", box.transform, SurvivorUI.Accent);
            check.raycastTarget = false;
            SurvivorUI.Stretch(check.rectTransform, 3f);
            toggle.graphic = check;

            TMP_Text text = SurvivorUI.Label("Label", background.transform, label, 14f, SurvivorUI.Text,
                TextAlignmentOptions.MidlineLeft, FontStyles.Bold);
            RectTransform textRt = text.rectTransform;
            textRt.anchorMin = Vector2.zero;
            textRt.anchorMax = Vector2.one;
            textRt.offsetMin = new Vector2(30f, 0f);
            textRt.offsetMax = new Vector2(-4f, 0f);

            toggle.SetIsOnWithoutNotify(initial);
            toggle.onValueChanged.AddListener(onChanged);
            return toggle;
        }

        // WHY: labels rebuild only when a value actually changes to avoid per-frame string garbage.
        private void RefreshHud()
        {
            if (_hpFill == null)
            {
                return;
            }

            RefreshPlayerLine();

            int living = LivingEnemyCount;
            int total = EnemyCount;
            if (living != _shownLiving || total != _shownTotal || _enemyDeaths != _shownDeaths)
            {
                _shownLiving = living;
                _shownTotal = total;
                _shownDeaths = _enemyDeaths;
                _enemiesText.text = $"Enemies: {living}/{total} alive | Deaths: {_enemyDeaths}";
            }

            if (!ReferenceEquals(_lastEvent, _shownEvent))
            {
                _shownEvent = _lastEvent;
                _lastText.text = "Last: " + _lastEvent;
            }

            if (_autoToggle.isOn != _autoDemoLoop)
            {
                _autoToggle.SetIsOnWithoutNotify(_autoDemoLoop);
            }

            if (_pressureToggle.isOn != _enemyPressureEnabled)
            {
                _pressureToggle.SetIsOnWithoutNotify(_enemyPressureEnabled);
            }

            bool dead = PlayerDead;
            if (_deadWarning.activeSelf != dead)
            {
                _deadWarning.SetActive(dead);
            }
        }

        private void RefreshPlayerLine()
        {
            if (_player == null)
            {
                if (!_shownMissing)
                {
                    _shownMissing = true;
                    _shownHp = float.NaN;
                    _playerText.text = "Player: missing";
                    SurvivorUI.SetFill(_hpFill, 0f);
                }

                return;
            }

            _shownMissing = false;
            float hp = _player.HpValue;
            float max = _player.MaxHpValue;
            int level = _player.LevelValue;
            bool dead = _player.IsDead;

            if (Mathf.Approximately(hp, _shownHp) && Mathf.Approximately(max, _shownMaxHp) &&
                level == _shownLevel && dead == _shownDead)
            {
                return;
            }

            _shownHp = hp;
            _shownMaxHp = max;
            _shownLevel = level;
            _shownDead = dead;
            _playerText.text = $"Player: HP {hp:0}/{max:0} | Level {level} | Dead {dead}";

            float percent = Mathf.Clamp01(_player.HpPercentValue);
            SurvivorUI.SetFill(_hpFill, percent);
            _hpFill.color = Color.Lerp(SurvivorUI.Danger, SurvivorUI.Good, percent);
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
