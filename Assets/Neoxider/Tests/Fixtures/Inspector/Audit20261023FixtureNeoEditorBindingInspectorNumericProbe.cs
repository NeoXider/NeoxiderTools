using System;
using System.Collections.Generic;
using UnityEngine;

namespace Neo.Editor.Tests
{
    public sealed class Audit20261023FixtureNeoEditorBindingInspectorNumericProbe : MonoBehaviour
    {
        public int LastArgument;

        public int ReadByte(byte value)
        {
            LastArgument = value;
            return value * 2;
        }

        public int ReadShort(short value)
        {
            LastArgument = value;
            return value * 2;
        }

        public int ReadGeneric<T>(int value) => value;
        public void FloatAction(float value) { }
        public void IntAction(int value) { }
        public void DoubleAction(double value) { }
        public void LongAction(long value) { }
    }
}
