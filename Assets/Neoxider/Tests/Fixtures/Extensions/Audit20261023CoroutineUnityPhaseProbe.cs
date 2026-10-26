using UnityEngine;

namespace Neo.Editor.Tests
{
    public sealed class Audit20261023CoroutineUnityPhaseProbe : MonoBehaviour
    {
        public int UpdateFrame { get; private set; } = -1;
        public int LateUpdateFrame { get; private set; } = -1;
        public int FixedCount { get; private set; }
        public string LastPhase { get; private set; }

        private void FixedUpdate()
        {
            FixedCount++;
            LastPhase = "FixedUpdate";
        }

        private void Update()
        {
            UpdateFrame = Time.frameCount;
            LastPhase = "Update";
        }

        private void LateUpdate()
        {
            LateUpdateFrame = Time.frameCount;
            LastPhase = "LateUpdate";
        }
    }
}
