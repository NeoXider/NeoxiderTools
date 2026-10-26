using UnityEngine;

namespace Neo.Editor.Tests.Edit
{
    public sealed class Audit20261023NeoConditionValueProbe : MonoBehaviour
    {
        public int Value;
        public int Limit;
        public int Calls;
        public int LastArgument;
        public int IndexerReads;

        public int this[int index]
        {
            get
            {
                IndexerReads++;
                return Value;
            }
        }

        public int ReadShort(short value)
        {
            Calls++;
            LastArgument = value;
            return value + 1;
        }

        public int ReadByte(byte value)
        {
            Calls++;
            LastArgument = value;
            return value + 1;
        }

        public int GenericOnly<T>(int value)
        {
            return value;
        }
    }
}
