using UnityEngine;
using UnityEngine.Events;

namespace Neo.Tools
{
    /// <summary>
    /// MonoBehaviour that provides a chance-based random selection system.
    /// Can be used to create random events, drops, or any probability-based mechanics in the scene.
    /// </summary>
    [AddComponentMenu("Neoxider/Random/Chance System")]
    public class ChanceSystemBehaviour : MonoBehaviour
    {
        [SerializeField, Tooltip("The chance manager containing probability configurations")]
        private ChanceManager _chanceManager = new ChanceManager();

        [SerializeField, Tooltip("Optional ChanceData asset to load configuration from")]
        private ChanceData _chanceData;

        /// <summary>
        /// Event invoked when a random ID is generated with the ID as parameter
        /// </summary>
        public UnityEvent<int> OnIdGenerated;

        /// <summary>
        /// The underlying chance manager
        /// </summary>
        public ChanceManager chanceManager => _chanceManager;

        private void Awake()
        {
            // If ChanceData is assigned, copy its configuration
            if (_chanceData != null)
            {
                LoadFromChanceData(_chanceData);
            }
        }

        private void OnValidate()
        {
            if (_chanceManager.chances.Count > 0)
            {
                // Add and remove last chance to trigger normalization
                float lastValue = _chanceManager.chances[_chanceManager.chances.Count - 1].value;
                _chanceManager.RemoveChance(_chanceManager.chances.Count - 1);
                _chanceManager.AddChance(lastValue);
            }
        }

        /// <summary>
        /// Generates a random ID and invokes the OnIdGenerated event
        /// </summary>
        public void GenerateId()
        {
            int id = GetId();
            OnIdGenerated?.Invoke(id);
        }

        /// <summary>
        /// Gets a random ID based on the configured probabilities
        /// </summary>
        /// <returns>The generated ID</returns>
        public int GetId()
        {
            return _chanceManager.GetChanceId();
        }

        /// <summary>
        /// Adds a new chance with the specified probability
        /// </summary>
        /// <param name="probability">Probability value between 0 and 1</param>
        /// <returns>Index of the newly added chance</returns>
        public int AddChance(float probability)
        {
            return _chanceManager.AddChance(probability);
        }

        /// <summary>
        /// Removes the chance at the specified index
        /// </summary>
        /// <param name="index">Index to remove</param>
        public void RemoveChance(int index)
        {
            _chanceManager.RemoveChance(index);
        }

        /// <summary>
        /// Gets the probability value at the specified index
        /// </summary>
        /// <param name="index">Index of the chance</param>
        /// <returns>Probability value</returns>
        public float GetChance(int index)
        {
            return _chanceManager.GetChanceValue(index);
        }

        /// <summary>
        /// Sets the probability value at the specified index
        /// </summary>
        /// <param name="index">Index of the chance</param>
        /// <param name="value">New probability value</param>
        public void SetChance(int index, float value)
        {
            _chanceManager.SetChanceValue(index, value);
        }

        /// <summary>
        /// Loads chance configuration from a ChanceData asset
        /// </summary>
        /// <param name="data">ChanceData asset to load from</param>
        public void LoadFromChanceData(ChanceData data)
        {
            if (data == null) return;

            _chanceManager.chances.Clear();
            foreach (var chance in data.chanceManager.chances)
            {
                _chanceManager.AddChance(chance.value);
            }
        }

        /// <summary>
        /// Clears all chances from the manager
        /// </summary>
        public void ClearChances()
        {
            _chanceManager.chances.Clear();
        }
    }
}