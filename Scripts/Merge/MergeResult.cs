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

        /// <summary>
        ///     True when a cascade hit <see cref="MergeRequest{TItem,TValue}.MaxCascadeIterations" /> and was stopped
        ///     early. Indicates a likely configuration bug (e.g. a merge rule that never reaches a stable value).
        /// </summary>
        public bool CascadeLimitReached;

        public bool HasChanges => Groups.Count > 0;
    }
}
