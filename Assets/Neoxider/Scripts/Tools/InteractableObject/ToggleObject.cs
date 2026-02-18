using UnityEngine;
using UnityEngine.Events;

namespace Neo
{
    namespace Tools
    {
        [NeoDoc("Tools/InteractableObject/ToggleObject.md")]
        [CreateFromMenu("Neoxider/Tools/ToggleObject", "Prefabs/Tools/Interact/Toggle Interactive.prefab")]
        [AddComponentMenu("Neoxider/" + "Tools/" + nameof(ToggleObject))]
        public class ToggleObject : MonoBehaviour
        {
            [Header("Settings")] public bool value;

            [Header("Debug")] public bool toggleDebug;

            public UnityEvent<bool> OnChange;
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
                Set(!value);
            }

            [Button]
            public void Set(bool value)
            {
                this.value = value;

                OnChange?.Invoke(value);
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
