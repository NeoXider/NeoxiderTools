using System;
using System.Collections.Generic;
using UnityEngine;

namespace Neo.Editor.Tests
{
    public sealed class Audit20261023FixtureReactivePropertyLifetimeDestroyableSubscriber : MonoBehaviour
    {
        public int Calls;

        public void Handle(int value)
        {
            Calls++;
        }
    }
}
