using UnityEngine;

namespace Neo.Tools
{
    [CreateAssetMenu(fileName = "ChanceData", menuName = "Neoxider/ChanceData")]
    public class ChanceData : ScriptableObject
    {
        [SerializeField] private ChanceManager _chanceManager = new ChanceManager();
        public ChanceManager chanceManager => _chanceManager;

        private void OnValidate()
        {
            _chanceManager.NormalizeChances();
        }

        public int GenerateId()
        {
            int id = _chanceManager.GetChanceId();
            return id;
        }
    }
}
