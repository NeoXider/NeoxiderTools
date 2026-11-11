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
    [CreateFromMenu("Neoxider/Core/Level Component")]
    [AddComponentMenu("Neoxider/Core/Level Component")]
    [NeoDoc("Core/Level/Components/LevelComponent.md")]
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

        private ReactiveStates _publishedInCall;

        [Flags]
        private enum ReactiveStates
        {
            None = 0,
            Level = 1,
            Xp = 2,
            XpToNext = 4,
            All = Level | Xp | XpToNext
        }

        public LevelCurveDefinition LevelCurveDefinition
        {
            get => _levelCurve;
            set
            {
                if (_levelCurve == value)
                {
                    return;
                }

                _levelCurve = value;
                if (_model == null)
                {
                    return;
                }

                ApplyCurveFromDefinition();
                SyncReactiveFromModel();
            }
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

        /// <summary>
        ///     Resets level and XP back to the configured starting values.
        ///     WHY: this used to be named <c>Reset</c>, which Unity reserves as an editor message sent when the user
        ///     picks "Reset" on the component - so opening that menu silently wiped the player's progress.
        /// </summary>
        public void ResetProgress()
        {
            EnsureInitialized();
            RestoreModelState(_startXp, _startLevel, _hasMaxLevel ? _maxLevel : 0);
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

        /// <summary>
        ///     Progress through the current level in [0, 1] (1 at the max level). Bind it to a SetProgress / Image fill
        ///     for an XP bar.
        /// </summary>
        public float XpProgress01
        {
            get
            {
                EnsureInitialized();
                return _model != null ? _model.LevelProgress01 : 0f;
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
            EnsureInitialized();
            ReactiveStates outerCall = BeginReactiveCall();
            try
            {
                _model.AddXp(amount);
                PublishStatesNotYetPublished();
            }
            finally
            {
                EndReactiveCall(outerCall);
            }
        }

        public void SetLevel(int level)
        {
            EnsureInitialized();
            ReactiveStates outerCall = BeginReactiveCall();
            try
            {
                _model.SetLevel(level);
                PublishStatesNotYetPublished();
            }
            finally
            {
                EndReactiveCall(outerCall);
            }
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
            // WHY: Queries from load/reactive listeners must see initialization in progress instead of starting it again.
            _initialized = true;
            try
            {
                if (_loadOnAwake && !string.IsNullOrWhiteSpace(_saveKey))
                {
                    Load();
                }
                else
                {
                    SyncReactiveFromModel();
                }
            }
            catch
            {
                _initialized = false;
                throw;
            }
        }

        public LevelProfileData GetProfileSnapshot()
        {
            EnsureInitialized();
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
            if (!string.IsNullOrEmpty(json))
            {
                try
                {
                    LevelProfileData data = JsonUtility.FromJson<LevelProfileData>(json);
                    if (data != null)
                    {
                        data.Sanitize();
                        RestoreModelState(data.TotalXp, data.CurrentLevel, data.MaxLevel);
                    }
                }
                catch (Exception e)
                {
                    NeoDiagnostics.LogWarning($"[LevelComponent] Load failed: {e.Message}");
                }
            }

            SyncReactiveFromModel();
            _onProfileLoaded?.Invoke();
        }

        private void RestoreModelState(int totalXp, int level, int maxLevel)
        {
            // WHY: Restore under the incoming cap; observers must not receive an intermediate clamp of the old profile.
            _model.OnLevelChanged -= HandleLevelChanged;
            try
            {
                _model.SetMaxLevel(maxLevel);
                _model.SetState(totalXp, level);
            }
            finally
            {
                _model.OnLevelChanged += HandleLevelChanged;
            }
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
            _model.SetCurveDefinition(_levelCurve);
        }

        private void SyncReactiveFromModel()
        {
            if (_model == null)
            {
                return;
            }

            WriteReactiveValues();
            _publishedInCall = ReactiveStates.All;
            // WHY: XP and XP-to-next are published before the level, so a level listener never finds an XP binding
            // that still shows the previous profile.
            XpState.ForceNotify();
            XpToNextLevelState.ForceNotify();
            LevelState.ForceNotify();
        }

        private void WriteReactiveValues()
        {
            LevelState.SetValueWithoutNotify(_model.CurrentLevel);
            XpState.SetValueWithoutNotify(_model.TotalXp);
            XpToNextLevelState.SetValueWithoutNotify(_model.XpToNextLevel);
        }

        private ReactiveStates BeginReactiveCall()
        {
            ReactiveStates outerCall = _publishedInCall;
            _publishedInCall = ReactiveStates.None;
            return outerCall;
        }

        private void EndReactiveCall(ReactiveStates outerCall)
        {
            // WHY: a nested AddXp/SetLevel (an OnLevelUp reward, for example) has already published its final values,
            // so the enclosing call does not repeat those notifications.
            _publishedInCall |= outerCall;
        }

        /// <summary>
        ///     Ends an <see cref="AddXp" /> or <see cref="SetLevel" /> call: every state is published at least once per
        ///     call, but a state the model events already published with its final value is not notified again.
        /// </summary>
        private void PublishStatesNotYetPublished()
        {
            ReactiveStates published = _publishedInCall;
            bool xpPending = (published & ReactiveStates.Xp) == 0 || XpState.CurrentValue != _model.TotalXp;
            bool xpToNextPending = (published & ReactiveStates.XpToNext) == 0 ||
                                   XpToNextLevelState.CurrentValue != _model.XpToNextLevel;
            bool levelPending = (published & ReactiveStates.Level) == 0 ||
                                LevelState.CurrentValue != _model.CurrentLevel;
            WriteReactiveValues();
            _publishedInCall = ReactiveStates.All;
            if (xpPending)
            {
                XpState.ForceNotify();
            }

            if (xpToNextPending)
            {
                XpToNextLevelState.ForceNotify();
            }

            if (levelPending)
            {
                LevelState.ForceNotify();
            }
        }

        private void HandleLevelChanged(int previousLevel, int newLevel)
        {
            // WHY: SetLevel moves TotalXp to the level's threshold without raising OnXpGained, so XP and XP-to-next
            // listeners (NoCode bindings included) hear the new values here, before the level listeners and OnLevelUp
            // run. A value HandleXpGained already published in this AddXp is not notified again.
            bool xpChanged = XpState.CurrentValue != _model.TotalXp;
            bool xpToNextChanged = XpToNextLevelState.CurrentValue != _model.XpToNextLevel;
            WriteReactiveValues();
            _publishedInCall |= ReactiveStates.Level;
            if (xpChanged)
            {
                _publishedInCall |= ReactiveStates.Xp;
                XpState.ForceNotify();
            }

            if (xpToNextChanged)
            {
                _publishedInCall |= ReactiveStates.XpToNext;
                XpToNextLevelState.ForceNotify();
            }

            LevelState.ForceNotify();
            _onLevelUp?.Invoke(newLevel);
        }

        private void HandleXpGained(int added, int newTotal)
        {
            bool xpToNextChanged = XpToNextLevelState.CurrentValue != _model.XpToNextLevel;
            WriteReactiveValues();
            _publishedInCall |= ReactiveStates.Xp;
            XpState.ForceNotify();
            if (xpToNextChanged)
            {
                _publishedInCall |= ReactiveStates.XpToNext;
                XpToNextLevelState.ForceNotify();
            }

            _onXpGained?.Invoke();
        }
    }
}
