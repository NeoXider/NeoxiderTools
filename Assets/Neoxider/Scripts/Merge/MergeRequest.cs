using System;
using System.Collections.Generic;

namespace Neo.Merge
{
    /// <summary>
    ///     Generic connected-group merge request. It is independent from Unity, grids, inventories and scene objects.
    /// </summary>
    public sealed class MergeRequest<TItem, TValue>
    {
        public IEnumerable<TItem> Items;
        public IEnumerable<TItem> Seeds;
        public Func<TItem, TValue> GetValue;
        public Action<TItem, TValue> SetValue;
        public Func<TItem, IEnumerable<TItem>> GetNeighbors;
        public Func<TItem, bool> CanUseItem;
        public Func<TValue, bool> IsEmptyValue;
        public Func<TValue, TValue, bool> AreValuesEqual;
        public Func<IReadOnlyList<TItem>, TItem, TItem> SelectResultItem;
        public Func<TValue, int, TValue> GetMergedValue;
        public TValue EmptyValue;
        public int MinGroupSize = 3;
        public MergeCascadeMode CascadeMode = MergeCascadeMode.None;
        public bool Mutate;
    }
}
