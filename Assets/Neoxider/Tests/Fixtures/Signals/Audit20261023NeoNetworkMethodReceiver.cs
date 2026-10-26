using System;
using System.Collections.Generic;
using UnityEngine;

namespace Neo.Editor.Tests
{
    public sealed class Audit20261023NeoNetworkMethodReceiver : MonoBehaviour
    {
        public int StringCalls;
        public int ParameterlessCalls;
        public string LastString = "unchanged";

        public void Record()
        {
            ParameterlessCalls++;
        }

        public void Record(string value)
        {
            StringCalls++;
            LastString = value;
        }
    }
}
