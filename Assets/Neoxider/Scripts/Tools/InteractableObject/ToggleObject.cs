using Neo.Reactive;
using UnityEngine;
using UnityEngine.Events;

namespace Neo
{
    namespace Tools
    {
        [NeoDoc("Tools/InteractableObject/ToggleObject.md")]
        [CreateFromMenu("Neoxider/Tools/Interact/ToggleObject", "Prefabs/Tools/Interact/Toggle Interactive.prefab")]
        [AddComponentMenu("Neoxider/" + "Tools/" + nameof(ToggleObject))]
        public class ToggleObject : MonoBehaviour
        {
            [Header("Settings")]
            public ReactivePropertyBool Value = new();

            /// <summary>Текущее состояние вкл/выкл (для NeoCondition и рефлексии).</summary>
            public bool ValueBool => Value.CurrentValue;

            [Header("Debug")] public bool toggleDebug;

            public UnityEvent<bool> OnChangeFlip;
            public UnityEvent ON;
            public UnityEvent OFF;

            private void OnValidate()
            {
                if (toggleDebug)
                {
                    toggleDebug = false;
                    Toggle();
                }
            }

            [Button]
            public void Toggle()
            {
                Set(!Value.CurrentValue);
            }

            [Button]
            public void Set(bool value)
            {
                Value.Value = value;
                OnChangeFlip?.Invoke(!value);

                if (value)
                {
                    ON?.Invoke();
                }
                else
                {
                    OFF?.Invoke();
                }
            }
        }
    }
}