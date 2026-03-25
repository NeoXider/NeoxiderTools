using UnityEngine;

namespace Neo.Bonus
{
    /// <summary>
    ///     ScriptableObject holding available bets for the slot machine.
    ///     Configure the bet list in the inspector.
    /// </summary>
    [CreateAssetMenu(fileName = "Bets Data", menuName = "Neoxider/Bonus/Slot/Bets Data", order = 1)]
    public class BetsData : ScriptableObject
    {
        [Tooltip("Available bets array for slot machine")] [SerializeField]
        private int[] _bets = { 10, 20, 50, 100, 200, 500, 1000 };

        /// <summary>
        ///     Array of available bets.
        /// </summary>
        public int[] bets => _bets;
    }
}
