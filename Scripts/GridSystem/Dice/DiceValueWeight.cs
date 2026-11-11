using System;
using UnityEngine;

namespace Neo.GridSystem.Dice
{
    /// <summary>
    ///     Serializable weighted dice value entry used by DicePieceGenerator.
    /// </summary>
    [Serializable]
    public struct DiceValueWeight
    {
        [SerializeField] private int value;
        [SerializeField, Min(0)] private int weight;

        public DiceValueWeight(int value, int weight)
        {
            this.value = value;
            this.weight = Mathf.Max(0, weight);
        }

        public int Value => value;
        public int Weight => weight;
    }
}
