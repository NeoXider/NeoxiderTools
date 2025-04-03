using UnityEngine;
using UnityEngine.Events;

namespace Neo
{
    namespace Tools
    {
        [AddComponentMenu("Neoxider/" + "Tools/" + nameof(ToggleObject))]
        public class ToggleObject : MonoBehaviour
        {
            public bool value;

            public bool toggleDebug = false;

            public UnityEvent<bool> OnChange;
            public UnityEvent<bool> OnChangeFlip;
            public UnityEvent ON;
            public UnityEvent OFF;

            public void Toggle()
            {
                Set(!value);
            }

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

            private void OnValidate()
            {
                if (toggleDebug)
                {
                    toggleDebug = false;
                    Toggle();
                }
            }
        }
    }
}
