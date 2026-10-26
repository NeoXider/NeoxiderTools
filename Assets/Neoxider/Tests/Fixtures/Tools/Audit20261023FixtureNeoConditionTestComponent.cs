using System;
using System.Collections.Generic;
using UnityEngine;

namespace Neo.Editor.Tests.Edit
{
    public class Audit20261023FixtureNeoConditionTestComponent : MonoBehaviour
    {
        public int Health { get; set; } = 100;
        public float Speed = 5.5f;
        public bool IsAlive = true;
        public string Name = "Hero";

        public int GetDamage(int modifier)
        {
            return 10 + modifier;
        }
    }
}
