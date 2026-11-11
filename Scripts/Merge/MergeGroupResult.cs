using System.Collections.Generic;

namespace Neo.Merge
{
    /// <summary>
    ///     Describes one resolved merge group.
    /// </summary>
    public sealed class MergeGroupResult<TItem, TValue>
    {
        public List<TItem> Items = new();
        public List<TItem> ClearedItems = new();
        public TItem SeedItem;
        public TItem ResultItem;
        public TValue SourceValue;
        public TValue ResultValue;
    }
}
