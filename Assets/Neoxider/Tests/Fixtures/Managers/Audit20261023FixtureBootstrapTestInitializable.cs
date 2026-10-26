using System;
using System.Collections.Generic;
using UnityEngine;
using Neo.Tools;

namespace Neo.Tools.Tests
{
    public sealed class Audit20261023FixtureBootstrapTestInitializable : MonoBehaviour, IInit
    {
        private List<int> _initOrder;

        public int InitPriority { get; private set; }

        public void Init()
        {
            _initOrder.Add(InitPriority);
        }

        public void Setup(int priority, List<int> initOrder)
        {
            InitPriority = priority;
            _initOrder = initOrder;
        }
    }
}
