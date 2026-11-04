using System;
using UnityEngine;

namespace Neo.Tests.Play
{
    /// <summary>Test-only raw Unity callback control; no filtering, networking or manual event entrypoints.</summary>
    public sealed class Audit20261102Collision3DNativeControl : MonoBehaviour
    {
        public event Action<Collision> Entered;
        public event Action<Collision> Stayed;
        public event Action<Collision> Exited;

        private void OnCollisionEnter(Collision collision)
        {
            Entered?.Invoke(collision);
        }

        private void OnCollisionStay(Collision collision)
        {
            Stayed?.Invoke(collision);
        }

        private void OnCollisionExit(Collision collision)
        {
            Exited?.Invoke(collision);
        }
    }
}
