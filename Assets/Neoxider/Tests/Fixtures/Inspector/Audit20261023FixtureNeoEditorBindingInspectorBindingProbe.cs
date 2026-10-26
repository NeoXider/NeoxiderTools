using System;
using System.Collections.Generic;
using UnityEngine;
using Neo.NoCode;
using Neo.Condition;

namespace Neo.Editor.Tests
{
    public sealed class Audit20261023FixtureNeoEditorBindingInspectorBindingProbe : MonoBehaviour
    {
        public ComponentFloatBinding Binding = new();
        public ConditionEntry Entry = new();
    }
}
