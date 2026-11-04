using UnityEngine;

namespace Neo.Editor.Tests
{
    /// <summary>Receiver with two parameterless methods for NetworkContextActionRelay method-cache tests.</summary>
    public sealed class NetworkLibraryPassMethodReceiver : MonoBehaviour
    {
        public int AlphaCalls;
        public int BetaCalls;
        public float LastValue;

        public void Alpha()
        {
            AlphaCalls++;
        }

        public void Beta()
        {
            BetaCalls++;
        }

        public void Beta(float value)
        {
            BetaCalls++;
            LastValue = value;
        }
    }
}
