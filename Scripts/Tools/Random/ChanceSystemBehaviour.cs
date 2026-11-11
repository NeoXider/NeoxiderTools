using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

namespace Neo.Tools
{
    /// <summary>
    ///     Scene wrapper over <see cref="ChanceManager" /> with UnityEvents and optional ScriptableObject source.
    ///     Use for drop tables, random events, loot  - configure in Inspector and subscribe without code.
    /// </summary>
    [NeoDoc("Tools/Random/ChanceSystemBehaviour.md")]
    [CreateFromMenu("Neoxider/Tools/Random/ChanceSystemBehaviour")]
    [AddComponentMenu("Neoxider/" + "Tools/" + nameof(ChanceSystemBehaviour))]
    public class ChanceSystemBehaviour : MonoBehaviour
    {
        [Header("Settings")] [SerializeField] [Tooltip("Inline chance configuration")]
        private ChanceManager manager = new();

        [SerializeField] [Tooltip("Optional ChanceData asset to copy configuration from at Awake")]
        private ChanceData chanceData;

        [Header("Events (No-Code)")]
        [Tooltip("Invoked with selected index. Use for generic logic (e.g. switch by index).")]
        public UnityEvent<int> OnIdGenerated;

        [Tooltip("Invoked with (index, normalizedWeight 0..1). Use when you need probability in UI or logic.")]
        [SerializeField]
        private UnityEvent<int, float> onIndexAndWeightSelected = new();

        [Tooltip("Invoked once after each roll. Use for UI refresh, SFX, etc.")] [SerializeField]
        private UnityEvent onRollComplete = new();

        [Tooltip(
            "One event per entry: when index N is selected, event at position N is invoked. Size can differ from entries count.")]
        [SerializeField]
        private List<UnityEvent> eventsByIndex = new();

        [Header("Unique draw (offers, cards)")]
        [Tooltip("How many different entries GenerateUniqueVoid() draws at once, for example 3 upgrade cards.")]
        [SerializeField] [Min(1)]
        private int drawCount = 3;

        [Tooltip("Invoked before a unique draw announces its picks: hide every option here, then Events By Index " +
                 "shows the drawn ones.")]
        [SerializeField]
        private UnityEvent onDrawStarted = new();

        [Header("Debug")] [SerializeField] [Tooltip("Log generated id and probability in the console (Editor only)")]
        private bool logDebugOnce;

        private readonly List<int> _lastDraw = new();
        private readonly List<int> _picksTaken = new();

        /// <summary>Invoked with the selected entry index. Same as OnIdGenerated.</summary>
        public UnityEvent<int> OnIndexSelected => OnIdGenerated;

        /// <summary>Invoked with (index, normalizedWeight).</summary>
        public UnityEvent<int, float> OnIndexAndWeightSelected => onIndexAndWeightSelected;

        /// <summary>Invoked once after each roll.</summary>
        public UnityEvent OnRollComplete => onRollComplete;

        /// <summary>Events invoked by selected index: when index i is chosen, eventsByIndex[i] is invoked (if present).</summary>
        public IReadOnlyList<UnityEvent> EventsByIndex => eventsByIndex;

        /// <summary>Inline chance table rolled by this component.</summary>
        public ChanceManager Manager => manager;

        /// <summary>Last selected index after GenerateId() / EvaluateAndNotify(); -1 if not rolled yet.</summary>
        public int LastSelectedIndex { get; private set; } = -1;

        /// <summary>Last selected entry (by index) after GenerateId() / EvaluateAndNotify().</summary>
        public ChanceManager.Entry LastSelectedEntry { get; private set; }

        /// <summary>Indices of the last unique draw, in draw order.</summary>
        public IReadOnlyList<int> LastDraw => _lastDraw;

        /// <summary>How often each entry was taken with <see cref="ConsumePick" />, in entry order.</summary>
        public IReadOnlyList<int> PicksTaken => _picksTaken;

        /// <summary>Invoked before a unique draw announces its picks.</summary>
        public UnityEvent OnDrawStarted => onDrawStarted;

        /// <summary>Entries drawn by <see cref="GenerateUniqueVoid" />.</summary>
        public int DrawCount
        {
            get => drawCount;
            set => drawCount = Mathf.Max(1, value);
        }

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
                    int index = IndexOfEntry(entry);
                    float normalized = index >= 0 ? manager.GetNormalizedWeight(index) : 0f;
                    NeoDiagnostics.Log(
                        $"[ChanceSystem] Sampled entry id={entry.CustomId}, weight={entry.Weight:F4}, normalized={normalized:F4}",
                        this);
                }
                else
                {
                    NeoDiagnostics.LogWarning("[ChanceSystem] No entries to sample", this);
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

        private int IndexOfEntry(ChanceManager.Entry entry)
        {
            IReadOnlyList<ChanceManager.Entry> entries = manager.Entries;
            for (int i = 0; i < entries.Count; i++)
            {
                if (ReferenceEquals(entries[i], entry))
                {
                    return i;
                }
            }

            return -1;
        }

        /// <summary>Calls GenerateId() (for use from UnityEvent/Button with no return value).</summary>
        public void GenerateVoid()
        {
            GenerateId();
        }

        /// <summary>
        ///     Rolls once, invokes all events (OnIndexSelected, OnIndexAndWeightSelected, EventsByIndex[index],
        ///     OnRollComplete), returns selected index.
        /// </summary>
        public int GenerateId()
        {
            return EvaluateAndNotify(manager.GetChanceId());
        }

        /// <summary>Draws <see cref="DrawCount" /> different entries (for UnityEvents and buttons).</summary>
        public void GenerateUniqueVoid()
        {
            GenerateUnique(drawCount);
        }

        /// <summary>
        ///     Draws up to <paramref name="count" /> different entries by weight, skipping entries whose
        ///     <c>Max Picks</c> is used up. Invokes On Draw Started, then for every pick On Index Selected,
        ///     On Index And Weight Selected and its Events By Index entry, then On Roll Complete once.
        ///     Returns the number drawn.
        /// </summary>
        public int GenerateUnique(int count)
        {
            EnsurePickCounters();
            manager.PickUnique(Mathf.Max(0, count), _lastDraw, _picksTaken);
            onDrawStarted?.Invoke();
            for (int i = 0; i < _lastDraw.Count; i++)
            {
                NotifyPick(_lastDraw[i]);
            }

            onRollComplete?.Invoke();
            return _lastDraw.Count;
        }

        /// <summary>
        ///     Records that entry <paramref name="index" /> was taken (for example its card was chosen). Entries with
        ///     <c>Max Picks</c> drop out of later unique draws once the limit is reached.
        /// </summary>
        public void ConsumePick(int index)
        {
            if (index < 0 || index >= manager.Count)
            {
                return;
            }

            EnsurePickCounters();
            _picksTaken[index]++;
        }

        /// <summary>Forgets every recorded pick (new run, new shop).</summary>
        public void ResetPicks()
        {
            _picksTaken.Clear();
        }

        /// <summary>True when entry <paramref name="index" /> can still be drawn by a unique draw.</summary>
        public bool HasPicksLeft(int index)
        {
            return manager.HasPicksLeft(index, _picksTaken);
        }

        private void EnsurePickCounters()
        {
            while (_picksTaken.Count < manager.Count)
            {
                _picksTaken.Add(0);
            }
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
            ChanceManager.Entry entry = manager.GetEntry(index);
            EvaluateAndNotify(index);
            return entry;
        }

        private int EvaluateAndNotify(int index)
        {
            NotifyPick(index);
            onRollComplete?.Invoke();
            return index;
        }

        private void NotifyPick(int index)
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
