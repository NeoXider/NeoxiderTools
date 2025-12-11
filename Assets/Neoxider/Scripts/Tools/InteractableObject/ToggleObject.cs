using UnityEngine;
using UnityEngine.Events;
#if ODIN_INSPECTOR
#endif

namespace Neo
{
    namespace Tools
    {
        [AddComponentMenu("Neo/" + "Tools/" + nameof(ToggleObject))]
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
#if ODIN_INSPECTOR
            [Button]
#else
            [ButtonAttribute]
#endif
            public void Toggle()
            {
                Set(!value);
            }
#if ODIN_INSPECTOR
            [Button]
#else
            [ButtonAttribute]
#endif
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