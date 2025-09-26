using UnityEngine;
using UnityEngine.Events;
using System.Collections.Generic;

namespace Neo.Tools
{
    public class MultiKeyEventTrigger : MonoBehaviour
    {
        [System.Serializable]
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

        public KeyEventPair[] keyEventPairs =
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
            foreach (var pair in keyEventPairs)
                if (Input.GetKeyDown(pair.key))
                    pair.onKeyPressed?.Invoke();
        }
    }
}