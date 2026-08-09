using Neo.Reactive;
using UnityEngine;
using UnityEngine.Events;

namespace Neo.Tools
{
    [NeoDoc("Tools/InteractableObject/ToggleObject.md")]
    [CreateFromMenu("Neoxider/Tools/Interact/ToggleObject", "Prefabs/Tools/Interact/Toggle Interactive.prefab")]
    [AddComponentMenu("Neoxider/" + "Tools/" + nameof(ToggleObject))]
    public class ToggleObject : MonoBehaviour
    {
        [Header("Settings")] public ReactivePropertyBool Value = new();

        [Header("Debug")] public bool toggleDebug;

        public UnityEvent<bool> OnChangeFlip;
        public UnityEvent ON;
        public UnityEvent OFF;

        /// <summary>Current on/off state (for NeoCondition and reflection).</summary>
        public bool ValueBool => Value.CurrentValue;

        private void OnValidate()
        {
            if (!toggleDebug)
            {
                return;
            }

            toggleDebug = false;
            if (Application.isPlaying)
            {
                Toggle();
            }
        }

        [Button(PlayModeOnly = true)]
        public void Toggle()
        {
            Set(!Value.CurrentValue);
        }

        [Button(PlayModeOnly = true)]
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
