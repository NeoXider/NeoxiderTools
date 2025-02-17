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
                this.key = k;
                this.onKeyPressed = new UnityEvent();
            }
        }

        public KeyEventPair[] keyEventPairs = {
            new KeyEventPair(KeyCode.Escape),
            new KeyEventPair(KeyCode.Space),
            new KeyEventPair(KeyCode.E),
            new KeyEventPair(KeyCode.R),
            new KeyEventPair(KeyCode.I),
            new KeyEventPair(KeyCode.T),
            new KeyEventPair(KeyCode.W),
            new KeyEventPair(KeyCode.A),
            new KeyEventPair(KeyCode.S),
            new KeyEventPair(KeyCode.D)};

        void Update()
        {
            foreach (var pair in keyEventPairs)
            {
                if (Input.GetKeyDown(pair.key))
                {
                    pair.onKeyPressed?.Invoke();
                }
            }
        }
    }
}