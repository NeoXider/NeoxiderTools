using System;
using Neo.Reactive;
using Neo.Save;
using UnityEngine;
using UnityEngine.Events;

namespace Neo.Core.Level
{
    /// <summary>
    ///     MonoBehaviour that provides level and optional XP via curve. Implements ILevelProvider; use for player, battle
    ///     pass, etc.
    /// </summary>
    [AddComponentMenu("Neoxider/Core/Level Component")]
    public sealed class LevelComponent : MonoBehaviour, ILevelProvider
    {
        [Header("Curve")] [SerializeField] private LevelCurveDefinition _levelCurve;

        [SerializeField] private bool _useXp = true;
        [SerializeField] [Min(0)] private int _startXp;
        [SerializeField] [Min(1)] private int _startLevel = 1;
        [SerializeField] private bool _hasMaxLevel;
        [SerializeField] [Min(0)] private int _maxLevel;

        [Header("Persistence")] [SerializeField]
        private string _saveKey = "Neo.Core.Level.Profile";

        [SerializeField] private bool _loadOnAwake = true;
        [SerializeField] private bool _autoSave = true;

        [Header("Reactive State")] public ReactivePropertyInt LevelState = new(1);

        public ReactivePropertyInt XpState = new();
        public ReactivePropertyInt XpToNextLevelState = new();

        [Header("Events")] [SerializeField] private UnityEventInt _onLevelUp = new();

        [SerializeField] private UnityEvent _onXpGained = new();
        [SerializeField] private UnityEvent _onProfileLoaded = new();
        [SerializeField] private UnityEvent _onProfileSaved = new();
        private bool _initialized;

        private LevelModel _model;

        public LevelCurveDefinition LevelCurveDefinition
        {
            get => _levelCurve;
            set => _levelCurve = value;
        }

        public UnityEventInt OnLevelUp => _onLevelUp;
        public UnityEvent OnXpGained => _onXpGained;
        public UnityEvent OnProfileLoaded => _onProfileLoaded;
        public UnityEvent OnProfileSaved => _onProfileSaved;

        /// <summary>Current level value (for NeoCondition and reactive binding).</summary>
        public int LevelStateValue => LevelState.CurrentValue;

        /// <summary>Current XP value (for NeoCondition and reactive binding).</summary>
        public int XpStateValue => XpState.CurrentValue;

        /// <summary>Current XP to next level (for NeoCondition and reactive binding).</summary>
        public int XpToNextLevelStateValue => XpToNextLevelState.CurrentValue;

        private void Awake()
        {
            EnsureInitialized();
        }

        public void Reset()
        {
            EnsureModel();
            _model.SetState(_startXp, _startLevel);
            _model.SetMaxLevel(_hasMaxLevel ? _maxLevel : 0);
            SyncReactiveFromModel();
        }

        private void OnDisable()
        {
            if (_autoSave && _initialized && !string.IsNullOrWhiteSpace(_saveKey))
            {
                Save();
            }
        }

        private void OnDestroy()
        {
            if (_model != null)
            {
                _model.OnLevelChanged -= HandleLevelChanged;
                _model.OnXpGained -= HandleXpGained;
            }
        }

        public bool UseXp
        {
            get
            {
                EnsureInitialized();
                return _model != null ? _model.UseXp : _useXp;
            }
        }

        public int Level
        {
            get
            {
                EnsureInitialized();
                return _model != null ? _model.CurrentLevel : _startLevel;
            }
        }

        public int TotalXp
        {
            get
            {
                EnsureInitialized();
                return _model != null ? _model.TotalXp : _startXp;
            }
        }

        public int XpToNextLevel
        {
            get
            {
                EnsureInitialized();
                return _model != null ? _model.XpToNextLevel : 0;
            }
        }

        public bool HasMaxLevel
        {
            get
            {
                EnsureInitialized();
                return _model != null ? _model.HasMaxLevel : _hasMaxLevel;
            }
        }

        public int MaxLevel
        {
            get
            {
                EnsureInitialized();
                return _model != null ? _model.MaxLevel : _maxLevel;
            }
        }

        public void AddXp(int amount)
        {
            EnsureModel();
            _model.AddXp(amount);
            SyncReactiveFromModel();
        }

        public void SetLevel(int level)
        {
            EnsureModel();
            _model.SetLevelDirect(level);
            SyncReactiveFromModel();
        }

        /// <summary>
        ///     Ensures the component is initialized (builds model, optional load from save). Safe to call multiple times.
        ///     Used by EditMode tests and when API is used before Awake.
        /// </summary>
        public void EnsureInitialized()
        {
            if (_initialized)
            {
                return;
            }

            EnsureModel();
            if (_loadOnAwake && !string.IsNullOrWhiteSpace(_saveKey))
            {
                Load();
            }

            SyncReactiveFromModel();
            _model.OnLevelChanged += HandleLevelChanged;
            _model.OnXpGained += HandleXpGained;
            _initialized = true;
        }

        public LevelProfileData GetProfileSnapshot()
        {
            EnsureModel();
            return new LevelProfileData
            {
                TotalXp = _model.TotalXp,
                CurrentLevel = _model.CurrentLevel,
                MaxLevel = _model.MaxLevel
            };
        }

        public void Save()
        {
            if (_model == null || string.IsNullOrWhiteSpace(_saveKey))
            {
                return;
            }

            LevelProfileData data = GetProfileSnapshot();
            string json = JsonUtility.ToJson(data, true);
            SaveProvider.SetString(_saveKey, json);
            SaveProvider.Save();
            _onProfileSaved?.Invoke();
        }

        public void Load()
        {
            if (_model == null || string.IsNullOrWhiteSpace(_saveKey))
            {
                return;
            }

            string json = SaveProvider.GetString(_saveKey, string.Empty);
            if (string.IsNullOrEmpty(json))
            {
                _onProfileLoaded?.Invoke();
                return;
            }

            try
            {
                LevelProfileData data = JsonUtility.FromJson<LevelProfileData>(json);
                if (data != null)
                {
                    data.Sanitize();
                    _model.SetState(data.TotalXp, data.CurrentLevel);
                    _model.SetMaxLevel(data.MaxLevel);
                    SyncReactiveFromModel();
                }
            }
            catch (Exception e)
            {
                Debug.LogWarning($"[LevelComponent] Load failed: {e.Message}");
            }

            _onProfileLoaded?.Invoke();
        }

        private void EnsureModel()
        {
            if (_model == null)
            {
                _model = new LevelModel();
                _model.SetUseXp(_useXp);
                _model.SetMaxLevel(_hasMaxLevel ? _maxLevel : 0);
                ApplyCurveFromDefinition();
                _model.SetState(_startXp, _startLevel);
                _model.OnLevelChanged += HandleLevelChanged;
                _model.OnXpGained += HandleXpGained;
            }
        }

        private void ApplyCurveFromDefinition()
        {
            if (_levelCurve != null)
            {
                _model.SetCurveDefinition(_levelCurve);
            }
        }

        private void SyncReactiveFromModel()
        {
            if (_model == null)
            {
                return;
            }

            LevelState.SetValueWithoutNotify(_model.CurrentLevel);
            XpState.SetValueWithoutNotify(_model.TotalXp);
            XpToNextLevelState.SetValueWithoutNotify(_model.XpToNextLevel);
            LevelState.ForceNotify();
            XpState.ForceNotify();
            XpToNextLevelState.ForceNotify();
        }

        private void HandleLevelChanged(int previousLevel, int newLevel)
        {
            LevelState.SetValueWithoutNotify(_model.CurrentLevel);
            XpToNextLevelState.SetValueWithoutNotify(_model.XpToNextLevel);
            LevelState.ForceNotify();
            XpToNextLevelState.ForceNotify();
            _onLevelUp?.Invoke(newLevel);
        }

        private void HandleXpGained(int added, int newTotal)
        {
            XpState.SetValueWithoutNotify(_model.TotalXp);
            XpState.ForceNotify();
            _onXpGained?.Invoke();
        }
    }
}
