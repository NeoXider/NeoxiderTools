using UnityEngine;
using UnityEngine.Events;

namespace Neo.Tools
{
    [AddComponentMenu("Neo/" + "Tools/" + nameof(RevertAmount))]
    public class RevertAmount : MonoBehaviour
    {
        public UnityEvent<float> OnChange;

        public void Amount(float amount)
        {
            OnChange?.Invoke(1 - amount);
        }
    }
}