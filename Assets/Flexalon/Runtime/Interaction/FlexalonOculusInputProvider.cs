#if FLEXALON_OCULUS

using Oculus.Interaction;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Flexalon
{
    public class FlexalonOculusInputProvider : MonoBehaviour, InputProvider
    {
        public InputMode InputMode => InputMode.External;
        public bool Active => _states.Any(s => s == InteractableState.Select);
        public Ray Ray => default;
        public Vector3 UIPointer => default;
        public GameObject ExternalFocusedObject => _states.Any(s => s == InteractableState.Hover || s == InteractableState.Select) ? gameObject : null;

        private IInteractable[] _interactables;
        private IEnumerable<InteractableState> _states => _interactables.Select(i => i.State);

        public void Awake()
        {
            _interactables = GetComponents<IInteractable>();
            if (_interactables.Length == 0)
            {
                Debug.LogWarning("FlexalonOculusInputProvider should be placed next to Oculus Interactable component.");
            }
        }
    }
}

#endif