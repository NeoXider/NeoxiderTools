using System;
using System.Collections.Generic;
using Neo.Save;
using UnityEngine;
using UnityEngine.Events;

namespace Neo.Core.Resources
{
    /// <summary>
    ///     Manages one or more resource pools (HP, Mana, etc.): current/max, regen, limits, events.
    ///     Primary use: HP and Mana; also supports arbitrary pools by id. Implements IResourcePoolProvider.
    /// </summary>
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

        /// <summary>Текущее HP (для NeoCondition); читает из пула HP.</summary>
        public float HpCurrentValue => GetPoolCurrentValue(RpgResourceId.Hp);

        /// <summary>Доля HP 0–1 (для NeoCondition); читает из пула HP.</summary>
        public float HpPercentValue => GetPoolPercentValue(RpgResourceId.Hp);

        /// <summary>Макс. HP (для NeoCondition); читает из пула HP.</summary>
        public float HpMaxValue => GetPoolMaxValue(RpgResourceId.Hp);

        /// <summary>Текущая мана (для NeoCondition); читает из пула Mana.</summary>
        public float ManaCurrentValue => GetPoolCurrentValue(RpgResourceId.Mana);

        /// <summary>Доля маны 0–1 (для NeoCondition); читает из пула Mana.</summary>
        public float ManaPercentValue => GetPoolPercentValue(RpgResourceId.Mana);

        /// <summary>Макс. мана (для NeoCondition); читает из пула Mana.</summary>
        public float ManaMaxValue => GetPoolMaxValue(RpgResourceId.Mana);

        private void Awake()
        {
            EnsureInitialized();
        }

        private void Update()
        {
            EnsureInitialized();
            _model?.Tick(Time.deltaTime);
            SyncAllReactive();
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
            SyncReactiveFor(resourceId);
            if (TryGetPoolEntry(resourceId, out ResourceEntryInspector entryDecrease))
            {
                if (actual > 0f)
                {
                    entryDecrease.OnDamage?.Invoke(actual);
                }

                if (_model.IsDepleted(resourceId))
                {
                    entryDecrease.OnDeath?.Invoke();
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
            SyncReactiveFor(resourceId);
            if (actual > 0f && TryGetPoolEntry(resourceId, out ResourceEntryInspector entryHeal))
            {
                entryHeal.OnHeal?.Invoke(actual);
            }

            return actual;
        }

        private float GetPoolCurrentValue(string resourceId)
        {
            return TryGetPoolEntry(resourceId, out ResourceEntryInspector e) ? e.CurrentValue : 0f;
        }

        private float GetPoolPercentValue(string resourceId)
        {
            return TryGetPoolEntry(resourceId, out ResourceEntryInspector e) ? e.PercentValue : 0f;
        }

        private float GetPoolMaxValue(string resourceId)
        {
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
                Load();
            }

            SyncAllReactive();
            _model.OnResourceChanged += HandleResourceChanged;
            _model.OnResourceDepleted += HandleResourceDepleted;
            _initialized = true;
        }

        public void Restore(string resourceId)
        {
            EnsureInitialized();
            _model?.Restore(resourceId);
            SyncReactiveFor(resourceId);
        }

        public void SetMaxHp(float max)
        {
            SetMax(RpgResourceId.Hp, max);
        }

        public void SetMax(string resourceId, float max)
        {
            EnsureInitialized();
            _model?.SetMax(resourceId, max);
            SyncReactiveFor(resourceId);
            if (TryGetPoolEntry(resourceId, out ResourceEntryInspector entry))
            {
                entry.OnChangeMax?.Invoke(max);
            }
        }

        public void Save()
        {
            EnsureInitialized();
            if (_model == null || string.IsNullOrWhiteSpace(_saveKey))
            {
                return;
            }

            var data = new ResourceProfileData();
            foreach (ResourceEntryInspector p in _pools)
            {
                if (string.IsNullOrWhiteSpace(p.id))
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
                        float cur = e.Current;
                        float m = _model.GetMax(e.Id);
                        if (cur > m)
                        {
                            cur = m;
                        }

                        _model.Restore(e.Id);
                        _model.Decrease(e.Id, _model.GetMax(e.Id) - cur);
                    }

                    SyncAllReactive();
                }
            }
            catch (Exception e)
            {
                Debug.LogWarning($"[HealthComponent] Load failed: {e.Message}");
            }
        }

        private void BuildModel()
        {
            _model = new ResourcePoolModel();
            foreach (ResourceEntryInspector p in _pools)
            {
                if (string.IsNullOrWhiteSpace(p.id))
                {
                    continue;
                }

                var entry = new ResourcePoolEntry
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
                float pct = entry.Max > 0f ? Mathf.Clamp01(entry.Current / entry.Max) : 0f;
                p.CurrentState.SetValueWithoutNotify(entry.Current);
                p.PercentState.SetValueWithoutNotify(pct);
                p.MaxState.SetValueWithoutNotify(entry.Max);
                p.CurrentState.ForceNotify();
                p.PercentState.ForceNotify();
                p.MaxState.ForceNotify();
            }

            _onPoolsChanged?.Invoke();
        }

        private void SyncReactiveFor(string resourceId)
        {
            if (_model == null || !TryGetPoolEntry(resourceId, out ResourceEntryInspector entry))
            {
                return;
            }

            float cur = _model.GetCurrent(resourceId);
            float max = _model.GetMax(resourceId);
            float percent = max > 0f ? Mathf.Clamp01(cur / max) : 0f;

            entry.CurrentState.SetValueWithoutNotify(cur);
            entry.PercentState.SetValueWithoutNotify(percent);
            entry.MaxState.SetValueWithoutNotify(max);
            entry.CurrentState.ForceNotify();
            entry.PercentState.ForceNotify();
            entry.MaxState.ForceNotify();
        }

        private void SyncAllReactive()
        {
            if (_model == null)
            {
                return;
            }

            foreach (ResourceEntryInspector p in _pools)
            {
                if (string.IsNullOrWhiteSpace(p.id))
                {
                    continue;
                }

                SyncReactiveFor(p.id);
            }
        }

        private void HandleResourceChanged(string id, float current, float max)
        {
            if (TryGetPoolEntry(id, out ResourceEntryInspector entry))
            {
                entry.OnChanged?.Invoke(current, max);
            }

            SyncReactiveFor(id);
        }

        private void HandleResourceDepleted(string id)
        {
            if (string.Equals(id, RpgResourceId.Hp, StringComparison.OrdinalIgnoreCase) &&
                TryGetPoolEntry(id, out ResourceEntryInspector entry))
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
                if (string.Equals(_pools[i].id, resourceId, StringComparison.OrdinalIgnoreCase))
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
