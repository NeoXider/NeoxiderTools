using UnityEngine;

namespace Neo.Bonus
{
    /// <summary>
    ///     ScriptableObject для хранения доступных ставок в слот-машине.
    ///     Позволяет настраивать список ставок через инспектор.
    /// </summary>
    [CreateAssetMenu(fileName = "Bets Data", menuName = "Neo/Bonus/Slot/Bets Data", order = 1)]
    public class BetsData : ScriptableObject
    {
        [Tooltip("Массив доступных ставок для слот-машины")] [SerializeField]
        private int[] _bets = { 10, 20, 50, 100, 200, 500, 1000 };

        /// <summary>
        ///     Массив доступных ставок.
        /// </summary>
        public int[] bets => _bets;
    }
}