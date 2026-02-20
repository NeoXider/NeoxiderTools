using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Events;

namespace Neo.Tools
{
    /// <summary>
    ///     Scene wrapper over <see cref="ChanceManager" /> with UnityEvents and optional ScriptableObject source.
    ///     Use for drop tables, random events, loot — configure in Inspector and subscribe without code.
    /// </summary>
    [NeoDoc("Tools/Random/ChanceSystemBehaviour.md")]
    [CreateFromMenu("Neoxider/Tools/Random/ChanceSystemBehaviour")]
    [AddComponentMenu("Neoxider/" + "Tools/" + nameof(ChanceSystemBehaviour))]
    public class ChanceSystemBehaviour : MonoBehaviour
    {
        [Header("Settings")]
        [SerializeField] [Tooltip("Inline chance configuration")]
        private ChanceManager manager = new();

        [SerializeField] [Tooltip("Optional ChanceData asset to copy configuration from at Awake")]
        private ChanceData chanceData;

        [Header("Events (No-Code)")]
        [Tooltip("Invoked with selected index. Use for generic logic (e.g. switch by index).")]
        public UnityEvent<int> OnIdGenerated;

        [Tooltip("Invoked with (index, normalizedWeight 0..1). Use when you need probability in UI or logic.")]
        [SerializeField] private UnityEvent<int, float> onIndexAndWeightSelected = new();

        [Tooltip("Invoked once after each roll. Use for UI refresh, SFX, etc.")]
        [SerializeField] private UnityEvent onRollComplete = new();

        [Tooltip("One event per entry: when index N is selected, event at position N is invoked. Size can differ from entries count.")]
        [SerializeField] private List<UnityEvent> eventsByIndex = new();

        [Header("Debug")]
        [SerializeField] [Tooltip("Log generated id and probability in the console (Editor only)")]
        private bool logDebugOnce;

        /// <summary>Invoked with the selected entry index. Same as OnIdGenerated.</summary>
        public UnityEvent<int> OnIndexSelected => OnIdGenerated;

        /// <summary>Invoked with (index, normalizedWeight).</summary>
        public UnityEvent<int, float> OnIndexAndWeightSelected => onIndexAndWeightSelected;

        /// <summary>Invoked once after each roll.</summary>
        public UnityEvent OnRollComplete => onRollComplete;

        /// <summary>Events invoked by selected index: when index i is chosen, eventsByIndex[i] is invoked (if present).</summary>
        public IReadOnlyList<UnityEvent> EventsByIndex => eventsByIndex;

        public ChanceManager Manager => manager;

        /// <summary>Last selected index after GenerateId() / EvaluateAndNotify(); -1 if not rolled yet.</summary>
        public int LastSelectedIndex { get; private set; } = -1;

        /// <summary>Last selected entry (by index) after GenerateId() / EvaluateAndNotify().</summary>
        public ChanceManager.Entry LastSelectedEntry { get; private set; }

        private void Awake()
        {
            if (chanceData != null)
            {
                manager.CopyFrom(chanceData.Manager);
            }

            manager.Sanitize();
            manager.EnsureUniqueIds();
        }

        private void OnValidate()
        {
#if UNITY_EDITOR
            if (logDebugOnce)
            {
                logDebugOnce = false;
                ChanceManager.Entry entry = manager?.Evaluate();
                if (entry != null)
                {
                    int index = manager.Entries.ToList().IndexOf(entry);
                    float normalized = index >= 0 ? manager.GetNormalizedWeight(index) : 0f;
                    Debug.Log(
                        $"[ChanceSystem] Sampled entry id={entry.CustomId}, weight={entry.Weight:F4}, normalized={normalized:F4}",
                        this);
                }
                else
                {
                    Debug.LogWarning("[ChanceSystem] No entries to sample", this);
                }
            }
#endif

            if (manager == null)
            {
                manager = new ChanceManager();
            }

            manager.Sanitize();
            manager.EnsureUniqueIds();
        }

        /// <summary>Calls GenerateId() (for use from UnityEvent/Button with no return value).</summary>
        public void GenerateVoid()
        {
            GenerateId();
        }

        /// <summary>Rolls once, invokes all events (OnIndexSelected, OnIndexAndWeightSelected, EventsByIndex[index], OnRollComplete), returns selected index.</summary>
        public int GenerateId()
        {
            return EvaluateAndNotify(manager.GetChanceId());
        }

        /// <summary>Returns selected index without invoking events.</summary>
        public int GetId()
        {
            return manager.GetChanceId();
        }

        /// <summary>Evaluates and invokes all events with the given index (e.g. from deterministic or external random).</summary>
        public void SetResultAndNotify(int index)
        {
            EvaluateAndNotify(index);
        }

        /// <summary>Rolls once and returns the entry; does not invoke events. For code-only use.</summary>
        public ChanceManager.Entry Evaluate()
        {
            return manager.Evaluate();
        }

        /// <summary>Rolls once, stores result in LastSelectedIndex/LastSelectedEntry, invokes all events, returns the entry.</summary>
        public ChanceManager.Entry EvaluateAndNotify()
        {
            int index = manager.GetChanceId();
            EvaluateAndNotify(index);
            return LastSelectedEntry;
        }

        private int EvaluateAndNotify(int index)
        {
            LastSelectedIndex = index;
            LastSelectedEntry = manager.GetEntry(index);
            float normalized = index >= 0 ? manager.GetNormalizedWeight(index) : 0f;

            OnIdGenerated?.Invoke(index);
            onIndexAndWeightSelected?.Invoke(index, normalized);

            if (index >= 0 && index < eventsByIndex.Count)
            {
                eventsByIndex[index]?.Invoke();
            }

            onRollComplete?.Invoke();
            return index;
        }

        public int AddChance(float weight)
        {
            return manager.AddChance(weight);
        }

        public void RemoveChance(int index)
        {
            manager.RemoveChance(index);
        }

        public float GetChance(int index)
        {
            return manager.GetChanceValue(index);
        }

        public float GetNormalizedWeight(int index)
        {
            return manager.GetNormalizedWeight(index);
        }

        public void SetChance(int index, float value)
        {
            manager.SetChanceValue(index, value);
        }

        public void ClearChances()
        {
            manager.Clear();
        }

        public void Normalize()
        {
            manager.Normalize();
        }

        /// <summary>Returns event for index (for code). Resize list if needed.</summary>
        public UnityEvent GetOrAddEventForIndex(int index)
        {
            while (eventsByIndex.Count <= index)
            {
                eventsByIndex.Add(new UnityEvent());
            }

            return eventsByIndex[index];
        }
    }
}