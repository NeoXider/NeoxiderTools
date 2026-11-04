using System;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.Serialization;

namespace Neo.Tools
{
    /// <summary>
    ///     ScriptableObject wrapper over <see cref="ChanceManager" />. Stores reusable chance configurations
    ///     that can be referenced across scenes and assets. For No-Code events (by index, on roll complete)
    ///     use <see cref="ChanceSystemBehaviour" /> with this asset assigned in the Inspector.
    /// </summary>
    [CreateAssetMenu(fileName = "Chance Data", menuName = "Neoxider/Tools/Random/Chance Data", order = 20)]
    [NeoDoc("Tools/Random/Data/ChanceData.md")]
    public class ChanceData : ScriptableObject
    {
        [SerializeField] [Tooltip("Chance configuration")]
        private ChanceManager manager = new();

        // WHY: still serialized, and [FormerlySerializedAs] keeps listeners saved under the old public field name
        // loading and firing; only the public accessor is obsolete, because a ScriptableObject must not carry
        // scene wiring.
        [SerializeField] [FormerlySerializedAs("OnIdGenerated")]
        [Tooltip("Obsolete: invoked when GenerateId() is called on this asset (e.g. from code). For scene events " +
                 "use ChanceSystemBehaviour.")]
        private UnityEvent<int> _onIdGenerated = new();

        /// <summary>
        ///     Invoked when <see cref="GenerateId" /> is called on this asset. Component rolls do not forward to it.
        /// </summary>
        [Obsolete("A ScriptableObject must not carry scene wiring. Subscribe to ChanceSystemBehaviour.OnIdGenerated " +
                  "(OnIndexSelected) with this asset assigned to it, or use the index returned by GenerateId().")]
        public UnityEvent<int> OnIdGenerated
        {
            get => _onIdGenerated;
            set => _onIdGenerated = value;
        }

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
            _onIdGenerated?.Invoke(id);
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
