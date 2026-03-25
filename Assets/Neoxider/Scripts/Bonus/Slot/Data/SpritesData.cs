using System;
using UnityEngine;

namespace Neo.Bonus
{
    /// <summary>
    ///     Visual data for one slot machine symbol.
    /// </summary>
    [Serializable]
    public class SlotVisualData
    {
        [Tooltip("Element ID, assigned automatically from array index.")]
        public int id;

        [Tooltip("Symbol sprite for slot machine display")]
        public Sprite sprite;

        [Tooltip("Symbol description")] [TextArea(1, 3)]
        public string description;
    }

    /// <summary>
    ///     ScriptableObject holding visual data for all slot machine symbols.
    ///     Assigns each symbol an ID from its array index automatically.
    /// </summary>
    [CreateAssetMenu(fileName = "Sprites Data", menuName = "Neoxider/Bonus/Slot/Sprites Data", order = 3)]
    public class SpritesData : ScriptableObject
    {
        [Tooltip("Visual data array for all slot machine symbols")] [SerializeField]
        private SlotVisualData[] _visuals;

        /// <summary>
        ///     Array of symbol visual data.
        /// </summary>
        public SlotVisualData[] visuals => _visuals;

        private void OnValidate()
        {
            if (_visuals == null)
            {
                return;
            }

            // Assign IDs from array index automatically
            for (int i = 0; i < _visuals.Length; i++)
            {
                if (_visuals[i] != null)
                {
                    _visuals[i].id = i;
                }
            }
        }
    }
}
