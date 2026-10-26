using System;
using System.Collections.Generic;
using Neo;
using Neo.Reactive;
using Neo.Save;
using UnityEngine;
using UnityEngine.Events;

namespace Neo.Core.Resources
{
    /// <summary>
    ///     Manages one or more resource pools (HP, Mana, etc.): current/max, regen, limits, events.
    ///     Primary use: HP and Mana; also supports arbitrary pools by id. Implements IResourcePoolProvider.
    /// </summary>
    [NeoDoc("Core/Resources/Components/HealthComponent.md")]
    [CreateFromMenu("Neoxider/Core/Health Component")]
    [AddComponentMenu("Neoxider/Core/Health Component")]
    public sealed class HealthComponent : MonoBehaviour, IResourcePoolProvider
    {
        [Header("Pools")] [SerializeField] private List<ResourceEntryInspector> _pools = new()
        {
            new ResourceEntryInspector { id = RpgResourceId.Hp, current = 100f, max = 100f, restoreOnAwake = true },
            new ResourceEntryInspector { id = RpgResourceId.Mana, current = 50f, max = 50f, restoreOnAwake = true }
        };

        [Header("Persistence")] [SerializeField]
        private string _saveKey;

        [SerializeField] private bool _loadOnAwake;
        [SerializeField] private bool _autoSave = true;

        [Header("Global (only pool list changes)")]
        [Tooltip("Invoked when the list of pools is rebuilt (e.g. after init).")]
        [SerializeField]
        private UnityEvent _onPoolsChanged = new();

        private bool _initialized;

        private ResourcePoolModel _model;

        /// <summary>Invoked when the list of pools is rebuilt (add/remove pools, init).</summary>
        public UnityEvent OnPoolsChanged => _onPoolsChanged;

        /// <summary>Current HP (for NeoCondition); reads from the HP pool.</summary>
        public float HpCurrentValue => GetPoolCurrentValue(RpgResourceId.Hp);

        /// <summary>HP fraction 0-1 (for NeoCondition); reads from the HP pool.</summary>
        public float HpPercentValue => GetPoolPercentValue(RpgResourceId.Hp);

        /// <summary>Max HP (for NeoCondition); reads from the HP pool.</summary>
        public float HpMaxValue => GetPoolMaxValue(RpgResourceId.Hp);

        /// <summary>Current mana (for NeoCondition); reads from the Mana pool.</summary>
        public float ManaCurrentValue => GetPoolCurrentValue(RpgResourceId.Mana);

        /// <summary>Mana fraction 0-1 (for NeoCondition); reads from the Mana pool.</summary>
        public float ManaPercentValue => GetPoolPercentValue(RpgResourceId.Mana);

        /// <summary>Max mana (for NeoCondition); reads from the Mana pool.</summary>
        public float ManaMaxValue => GetPoolMaxValue(RpgResourceId.Mana);

        private void Awake()
        {
            EnsureInitialized();
        }

        private void Update()
        {
            EnsureInitialized();
            // WHY: no per-frame SyncAllReactive — every model mutation (incl. Tick regen/heal) raises
            // OnResourceChanged -> SyncReactiveFor; forcing notify each frame would spam unchanged listeners.
            _model?.Tick(Time.deltaTime);
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
                _model.OnResourceChanged -= HandleResourceChanged;
                _model.OnResourceDepleted -= HandleResourceDepleted;
            }
        }

        public float GetCurrent(string resourceId)
        {
            EnsureInitialized();
            return _model != null ? _model.GetCurrent(resourceId) : 0f;
        }

        public float GetMax(string resourceId)
        {
            EnsureInitialized();
            return _model != null ? _model.GetMax(resourceId) : 0f;
        }

        public bool IsDepleted(string resourceId)
        {
            EnsureInitialized();
            return _model != null && _model.IsDepleted(resourceId);
        }

        public bool TrySpend(string resourceId, float amount, out string failReason)
        {
            failReason = null;
            EnsureInitialized();
            if (_model == null)
            {
                failReason = "No model.";
                return false;
            }

            return _model.TrySpend(resourceId, amount, out failReason);
        }

        public float Decrease(string resourceId, float amount)
        {
            EnsureInitialized();
            if (_model == null)
            {
                return 0f;
            }

            float actual = _model.Decrease(resourceId, amount);
            if (TryGetPoolEntry(resourceId, out ResourceEntryInspector entryDecrease))
            {
                if (actual > 0f)
                {
                    entryDecrease.OnDamage?.Invoke(actual);
                }
            }

            return actual;
        }

        public float Increase(string resourceId, float amount)
        {
            EnsureInitialized();
            if (_model == null)
            {
                return 0f;
            }

            float actual = _model.Increase(resourceId, amount);
            if (actual > 0f && TryGetPoolEntry(resourceId, out ResourceEntryInspector entryHeal))
            {
                entryHeal.OnHeal?.Invoke(actual);
            }

            return actual;
        }

        /// <summary>Deals damage to the HP pool (UnityEvent-callable, 1-arg). Returns void so it appears in the UnityEvent dropdown.</summary>
        public void Damage(float amount)
        {
            Decrease(RpgResourceId.Hp, amount);
        }

        /// <summary>Heals the HP pool (UnityEvent-callable, 1-arg). Returns void so it appears in the UnityEvent dropdown.</summary>
        public void Heal(float amount)
        {
            Increase(RpgResourceId.Hp, amount);
        }

        private float GetPoolCurrentValue(string resourceId)
        {
            EnsureInitialized();
            return TryGetPoolEntry(resourceId, out ResourceEntryInspector e) ? e.CurrentValue : 0f;
        }

        private float GetPoolPercentValue(string resourceId)
        {
            EnsureInitialized();
            return TryGetPoolEntry(resourceId, out ResourceEntryInspector e) ? e.PercentValue : 0f;
        }

        private float GetPoolMaxValue(string resourceId)
        {
            EnsureInitialized();
            return TryGetPoolEntry(resourceId, out ResourceEntryInspector e) ? e.MaxValue : 0f;
        }

        /// <summary>
        ///     Ensures the component is initialized (builds model from _pools). Call is safe if already initialized.
        ///     Used by EditMode tests and when API is used before Awake.
        /// </summary>
        public void EnsureInitialized()
        {
            if (_initialized)
            {
                return;
            }

            BuildModel();
            if (_loadOnAwake && !string.IsNullOrWhiteSpace(_saveKey))
            {
                LoadModel();
            }

            // Stage later pools and recreate their reactive objects before the first listener can read them.
            SyncAllReactive(notify: false);
            _model.OnResourceChanged += HandleResourceChanged;
            _model.OnResourceDepleted += HandleResourceDepleted;
            // Publish readiness before invoking listeners that may call back into the public API.
            _initialized = true;
            SyncAllReactive(true);
            _onPoolsChanged?.Invoke();
        }

        public void Restore(string resourceId)
        {
            EnsureInitialized();
            _model?.Restore(resourceId);
        }

        public void SetMaxHp(float max)
        {
            SetMax(RpgResourceId.Hp, max);
        }

        public void SetMax(string resourceId, float max)
        {
            EnsureInitialized();
            _model?.SetMax(resourceId, max);
            if (TryGetPoolEntry(resourceId, out ResourceEntryInspector entry))
            {
                entry.OnChangeMax?.Invoke(_model.GetMax(resourceId));
            }
        }

        public void Save()
        {
            EnsureInitialized();
            if (_model == null || string.IsNullOrWhiteSpace(_saveKey))
            {
                return;
            }

            ResourceProfileData data = new();
            foreach (ResourceEntryInspector p in _pools)
            {
                if (p == null || string.IsNullOrWhiteSpace(p.id))
                {
                    continue;
                }

                data.Entries.Add(new ResourceEntryData
                {
                    Id = p.id,
                    Current = _model.GetCurrent(p.id),
                    Max = _model.GetMax(p.id)
                });
            }

            string json = JsonUtility.ToJson(data, true);
            SaveProvider.SetString(_saveKey, json);
            SaveProvider.Save();
        }

        public void Load()
        {
            EnsureInitialized();
            LoadModel();
            SyncAllReactive();
        }

        private void LoadModel()
        {
            if (_model == null || string.IsNullOrWhiteSpace(_saveKey))
            {
                return;
            }

            string json = SaveProvider.GetString(_saveKey, string.Empty);
            if (string.IsNullOrEmpty(json))
            {
                return;
            }

            try
            {
                ResourceProfileData data = JsonUtility.FromJson<ResourceProfileData>(json);
                if (data?.Entries != null)
                {
                    data.Sanitize();
                    foreach (ResourceEntryData e in data.Entries)
                    {
                        if (string.IsNullOrWhiteSpace(e.Id))
                        {
                            continue;
                        }

                        _model.SetMax(e.Id, e.Max);
                        // WHY: SetCurrent restores the exact saved value; Restore+Decrease was capped by
                        // MaxDecreaseAmount and fired depleted/damage side effects while loading.
                        _model.SetCurrent(e.Id, e.Current);
                    }
                }
            }
            catch (Exception e)
            {
                NeoDiagnostics.LogWarning($"[HealthComponent] Load failed: {e.Message}");
            }
        }

        private void BuildModel()
        {
            _model = new ResourcePoolModel();
            _pools ??= new List<ResourceEntryInspector>();
            foreach (ResourceEntryInspector p in _pools)
            {
                if (p == null || string.IsNullOrWhiteSpace(p.id))
                {
                    continue;
                }

                ResourcePoolEntry entry = new()
                {
                    Current = p.current,
                    Max = p.max,
                    RegenPerSecond = p.regenPerSecond,
                    RegenInterval = p.regenInterval > 0f ? p.regenInterval : 1f,
                    MaxDecreaseAmount = p.maxDecreaseAmount < 0f ? -1f : p.maxDecreaseAmount,
                    MaxIncreaseAmount = p.maxIncreaseAmount < 0f ? -1f : p.maxIncreaseAmount,
                    RestoreOnAwake = p.restoreOnAwake,
                    IgnoreCanHeal = p.ignoreCanHeal,
                    HealAmount = p.healAmount,
                    HealDelay = p.healDelay
                };

                if (entry.RestoreOnAwake)
                {
                    entry.Current = entry.Max;
                }

                _model.AddPool(p.id, entry);
            }
        }

        private void SyncReactiveFor(string resourceId, bool forceNotify = false, bool notify = true)
        {
            if (_model == null || !TryGetPoolEntry(resourceId, out ResourceEntryInspector entry))
            {
                return;
            }

            float cur = _model.GetCurrent(resourceId);
            float max = _model.GetMax(resourceId);
            float percent = max > 0f ? Mathf.Clamp01(cur / max) : 0f;

            entry.CurrentState ??= new ReactivePropertyFloat();
            entry.PercentState ??= new ReactivePropertyFloat();
            entry.MaxState ??= new ReactivePropertyFloat();
            bool currentChanged = forceNotify || !entry.CurrentState.CurrentValue.Equals(cur);
            bool percentChanged = forceNotify || !entry.PercentState.CurrentValue.Equals(percent);
            bool maxChanged = forceNotify || !entry.MaxState.CurrentValue.Equals(max);

            entry.CurrentState.SetValueWithoutNotify(cur);
            entry.PercentState.SetValueWithoutNotify(percent);
            entry.MaxState.SetValueWithoutNotify(max);
            if (!notify)
            {
                return;
            }

            if (currentChanged)
            {
                entry.CurrentState.ForceNotify();
            }

            if (percentChanged)
            {
                entry.PercentState.ForceNotify();
            }

            if (maxChanged)
            {
                entry.MaxState.ForceNotify();
            }
        }

        private void SyncAllReactive(bool forceNotify = false, bool notify = true)
        {
            if (_model == null)
            {
                return;
            }

            foreach (ResourceEntryInspector p in _pools)
            {
                if (p == null || string.IsNullOrWhiteSpace(p.id))
                {
                    continue;
                }

                SyncReactiveFor(p.id, forceNotify, notify);
            }
        }

        private void HandleResourceChanged(string id, float current, float max)
        {
            SyncReactiveFor(id);
            if (TryGetPoolEntry(id, out ResourceEntryInspector entry))
            {
                entry.OnChanged?.Invoke(current, max);
            }
        }

        private void HandleResourceDepleted(string id)
        {
            if (TryGetPoolEntry(id, out ResourceEntryInspector entry))
            {
                entry.OnDeath?.Invoke();
            }
        }

        private bool TryGetPoolEntry(string resourceId, out ResourceEntryInspector entry)
        {
            if (string.IsNullOrEmpty(resourceId) || _pools == null)
            {
                entry = null;
                return false;
            }

            for (int i = 0; i < _pools.Count; i++)
            {
                if (_pools[i] != null && string.Equals(_pools[i].id, resourceId, StringComparison.OrdinalIgnoreCase))
                {
                    entry = _pools[i];
                    return true;
                }
            }

            entry = null;
            return false;
        }
    }
}
