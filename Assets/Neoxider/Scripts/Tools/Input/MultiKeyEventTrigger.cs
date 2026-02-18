using System;
using UnityEngine;
using UnityEngine.Events;

namespace Neo.Tools
{
    [NeoDoc("Tools/Input/MultiKeyEventTrigger.md")]
    [AddComponentMenu("Neo/" + "Tools/" + nameof(MultiKeyEventTrigger))]
    public class MultiKeyEventTrigger : MonoBehaviour
    {
        [Header("Key Bindings")] public KeyEventPair[] keyEventPairs =
        {
            new(KeyCode.Escape),
            new(KeyCode.Space),
            new(KeyCode.E),
            new(KeyCode.R),
            new(KeyCode.I),
            new(KeyCode.T),
            new(KeyCode.W),
            new(KeyCode.A),
            new(KeyCode.S),
            new(KeyCode.D)
        };

        private void Update()
        {
            foreach (KeyEventPair pair in keyEventPairs)
            {
                if (Input.GetKeyDown(pair.key))
                {
                    pair.onKeyPressed?.Invoke();
                }
            }
        }

        [Serializable]
        public struct KeyEventPair
        {
            public KeyCode key;
            public UnityEvent onKeyPressed;

            public KeyEventPair(KeyCode k)
            {
                key = k;
                onKeyPressed = new UnityEvent();
            }
        }
    }
}