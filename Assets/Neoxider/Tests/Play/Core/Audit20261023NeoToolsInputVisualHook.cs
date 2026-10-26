using System;
using UnityEngine;

namespace Neo.Tests.Play
{
    public sealed class Audit20261023NeoToolsInputVisualHook : MonoBehaviour
    {
        public Action Activated;

        private void OnEnable()
        {
            Activated?.Invoke();
        }
    }
}
