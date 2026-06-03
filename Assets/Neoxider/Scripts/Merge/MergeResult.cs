using System.Collections.Generic;

namespace Neo.Merge
{
    /// <summary>
    ///     Contains all groups and changed items produced by a merge request.
    /// </summary>
    public sealed class MergeResult<TItem, TValue>
    {
        public List<MergeGroupResult<TItem, TValue>> Groups = new();
        public List<TItem> ChangedItems = new();

        public bool HasChanges => Groups.Count > 0;
    }
}
