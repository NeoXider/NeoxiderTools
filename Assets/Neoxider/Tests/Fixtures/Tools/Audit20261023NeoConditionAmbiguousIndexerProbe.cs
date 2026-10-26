using UnityEngine;

namespace Neo.Editor.Tests.Edit
{
    public sealed class Audit20261023NeoConditionAmbiguousIndexerProbe : MonoBehaviour
    {
        public int Value;
        public int IndexerReads;

        public int this[int index]
        {
            get
            {
                IndexerReads++;
                return Value;
            }
        }

        public int this[string key]
        {
            get
            {
                IndexerReads++;
                return Value;
            }
        }
    }
}
