using UnityEngine;
using UnityEngine.Events;

namespace Neo.Tools
{
    /// <summary>
    ///     ScriptableObject wrapper over <see cref="ChanceManager" />. Stores reusable chance configurations
    ///     that can be referenced across scenes and assets. For No-Code events (by index, on roll complete)
    ///     use <see cref="ChanceSystemBehaviour" /> with this asset assigned in the Inspector.
    /// </summary>
    [CreateAssetMenu(fileName = "Chance Data", menuName = "Neoxider/Tools/Random/Chance Data", order = 20)]
    public class ChanceData : ScriptableObject
    {
        [SerializeField] [Tooltip("Chance configuration")]
        private ChanceManager manager = new();

        [Tooltip("Invoked when GenerateId() is called on this asset (e.g. from code). For scene events use ChanceSystemBehaviour.")]
        public UnityEvent<int> OnIdGenerated;

        public ChanceManager Manager => manager;

        private void OnValidate()
        {
            if (manager == null)
            {
                manager = new ChanceManager();
            }

            manager.Sanitize();
            manager.EnsureUniqueIds();
        }

        public int GenerateId()
        {
            int id = manager.GetChanceId();
            OnIdGenerated?.Invoke(id);
            return id;
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
    }
}