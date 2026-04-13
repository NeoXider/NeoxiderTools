using Neo.Shop;
using UnityEngine;

namespace Neo.Tools
{
    public enum CounterModifyOperation
    {
        Add = 0,
        Subtract = 1,
        Multiply = 2,
        Divide = 3,
        Set = 4
    }

    [NeoDoc("Tools/Components/ModifyCounterByKey.md")]
    [CreateFromMenu("Neoxider/Tools/Components/ModifyCounterByKey")]
    [AddComponentMenu("Neoxider/Tools/ModifyCounterByKey")]
    public class ModifyCounterByKey : MonoBehaviour
    {
        [SerializeField] [Tooltip("The Save Key of the Counter (or Money) to modify.")]
        private string targetSaveKey = "Counter";

        [SerializeField] private CounterModifyOperation operation = CounterModifyOperation.Add;
        [SerializeField] private float value = 1f;

        [Button]
        public void Execute()
        {
            if (string.IsNullOrEmpty(targetSaveKey)) return;

            // Check if Money singleton matches the save key
            if (Money.HasInstance && Money.Instance != null && Money.Instance.SaveKey == targetSaveKey)
            {
                ApplyToMoney(Money.Instance);
                return;
            }

            // Otherwise, look for a Counter registered with this save key
            if (Counter.Registry.TryGetValue(targetSaveKey, out var counters) && counters != null && counters.Count > 0)
            {
                Counter targetCounter = null;

#if MIRROR
                if (Neo.Network.NeoNetworkState.IsClient)
                {
                    // If running as a client and there are multiple counters (e.g. one per player),
                    // find the one we have authority over (our own wallet).
                    targetCounter = counters.Find(c => c.isOwned);
                }
#endif
                
                if (targetCounter == null)
                {
                    targetCounter = counters[0]; // Fallback
                }

                if (targetCounter != null)
                {
                    ApplyToCounter(targetCounter);
                }
            }
            else
            {
                Debug.LogWarning($"[ModifyCounterByKey] No Counter or Money component found with SaveKey: '{targetSaveKey}'");
            }
        }

        private void ApplyToMoney(Money money)
        {
            switch (operation)
            {
                case CounterModifyOperation.Add:
                    money.Add(value);
                    break;
                case CounterModifyOperation.Subtract:
                    money.Spend(value); // Spend internally handles network propagation if successful
                    break;
                case CounterModifyOperation.Set:
                    money.SetMoney(value);
                    break;
                case CounterModifyOperation.Multiply:
                    money.SetMoney(money.money * value);
                    break;
                case CounterModifyOperation.Divide:
                    if (value != 0) money.SetMoney(money.money / value);
                    break;
            }
        }

        private void ApplyToCounter(Counter counter)
        {
            switch (operation)
            {
                case CounterModifyOperation.Add:
                    counter.Add(value);
                    break;
                case CounterModifyOperation.Subtract:
                    counter.Subtract(value);
                    break;
                case CounterModifyOperation.Set:
                    counter.Set(value);
                    break;
                case CounterModifyOperation.Multiply:
                    counter.Multiply(value);
                    break;
                case CounterModifyOperation.Divide:
                    if (value != 0) counter.Divide(value);
                    break;
            }
        }
    }
}
