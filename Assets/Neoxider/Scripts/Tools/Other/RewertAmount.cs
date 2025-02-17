using UnityEngine;
using UnityEngine.Events;

namespace Neo.Tools
{
    public class RewertAmount : MonoBehaviour
    {
        public UnityEvent<float> OnChange;

        public void Amount(float amount)
        {
            OnChange?.Invoke(1 - amount);
        }
    }
}
