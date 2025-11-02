using System.Linq;
using UnityEngine;
using UnityEngine.Events;

namespace Neo.Tools
{
    /// <summary>
    ///     Scene wrapper over <see cref="ChanceManager" /> with UnityEvents and optional ScriptableObject source.
    ///     Useful for drop tables, random events etc.
    /// </summary>
    [AddComponentMenu("Neoxider/Random/Chance System")]
    public class ChanceSystemBehaviour : MonoBehaviour
    {
        [SerializeField] [Tooltip("Inline chance configuration")]
        private ChanceManager manager = new();

        [SerializeField] [Tooltip("Optional ChanceData asset to copy configuration from at Awake")]
        private ChanceData chanceData;

        [SerializeField] [Tooltip("Log generated id and probability in the console (Editor only)")]
        private bool logDebugOnce;

        public UnityEvent<int> OnIdGenerated;

        public ChanceManager Manager => manager;

        private void Awake()
        {
            if (chanceData != null) manager.CopyFrom(chanceData.Manager);

            manager.Sanitize();
            manager.EnsureUniqueIds();
        }

        private void OnValidate()
        {
#if UNITY_EDITOR
            if (logDebugOnce)
            {
                logDebugOnce = false;
                var entry = manager?.Evaluate();
                if (entry != null)
                {
                    var index = manager.Entries.ToList().IndexOf(entry);
                    var normalized = index >= 0 ? manager.GetNormalizedWeight(index) : 0f;
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

            if (manager == null) manager = new ChanceManager();

            manager.Sanitize();
            manager.EnsureUniqueIds();
        }

        public void GenerateVoid()
        {
            GenerateId();
        }
        
        public int GenerateId()
        {
            var id = manager.GetChanceId();
            OnIdGenerated?.Invoke(id);
            return id;
        }

        public int GetId()
        {
            return manager.GetChanceId();
        }

        public ChanceManager.Entry Evaluate()
        {
            return manager.Evaluate();
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
    }
}