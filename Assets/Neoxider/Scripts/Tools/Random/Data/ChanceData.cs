using UnityEngine;
using UnityEngine.Events;

namespace Neo.Tools
{
    /// <summary>
    /// ScriptableObject that manages probability-based random selection.
    /// Can be used to create reusable chance configurations in the project.
    /// </summary>
    [CreateAssetMenu(fileName = "ChanceData", menuName = "Neoxider/Random/ChanceData")]
    public class ChanceData : ScriptableObject
    {
        [SerializeField, Tooltip("The chance manager containing probability configurations")]
        private ChanceManager _chanceManager = new ChanceManager();
        
        /// <summary>
        /// Event invoked when a random ID is generated
        /// </summary>
        public UnityEvent<int> OnIdGenerated;

        /// <summary>
        /// The underlying chance manager
        /// </summary>
        public ChanceManager chanceManager => _chanceManager;

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
        /// Generates a random ID based on the configured probabilities
        /// </summary>
        /// <returns>The generated ID</returns>
        public int GenerateId()
        {
            int id = _chanceManager.GetChanceId();
            OnIdGenerated?.Invoke(id);
            return id;
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
        /// Clears all chances from the manager
        /// </summary>
        public void ClearChances()
        {
            _chanceManager.chances.Clear();
        }
    }
}
