using UnityEngine;

namespace Neo.Bonus
{
    [CreateAssetMenu(fileName = "BetsDafualt", menuName = "Neoxider/Slot/Bets")]
    public class BetsData : ScriptableObject
    {
        [SerializeField] private int[] _bets = { 10, 20, 50, 100, 200, 500, 1000 };

        public int[] bets => _bets;
    }
}