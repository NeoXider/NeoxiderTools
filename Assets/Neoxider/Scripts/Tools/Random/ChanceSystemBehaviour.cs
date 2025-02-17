using UnityEngine;
using UnityEngine.Events;

namespace Neo.Tools
{
    public class ChanceSystemBehaviour : MonoBehaviour
    {
        [SerializeField] private ChanceManager _chanceManager = new ChanceManager();

        public UnityEvent<int> OnBonusIdGenerated;

        private void OnValidate()
        {
            _chanceManager.NormalizeChances();
        }

        public void GenerateBonus()
        {
            GetBonusId();
        }

        public int GetBonusId()
        {
            int bonusId = _chanceManager.GetChanceId();
            OnBonusIdGenerated.Invoke(bonusId);
            return bonusId;
        }
    }
}