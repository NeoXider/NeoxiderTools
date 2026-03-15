using System;
using System.Collections.Generic;
using UnityEngine;

namespace Neo.Core.Resources
{
    /// <summary>
    ///     Serializable data for resource pools persistence.
    /// </summary>
    [Serializable]
    public sealed class ResourceProfileData
    {
        [SerializeField] private List<ResourceEntryData> _entries = new();

        public List<ResourceEntryData> Entries => _entries;

        public void Sanitize()
        {
            if (_entries == null)
            {
                _entries = new List<ResourceEntryData>();
            }

            for (int i = _entries.Count - 1; i >= 0; i--)
            {
                ResourceEntryData e = _entries[i];
                if (e == null || string.IsNullOrWhiteSpace(e.Id))
                {
                    _entries.RemoveAt(i);
                    continue;
                }

                if (e.Max < 0f)
                {
                    e.Max = 0f;
                }

                if (e.Current > e.Max)
                {
                    e.Current = e.Max;
                }

                if (e.Current < 0f)
                {
                    e.Current = 0f;
                }
            }
        }

        public ResourceProfileData Clone()
        {
            var copy = new ResourceProfileData();
            foreach (ResourceEntryData e in _entries)
            {
                copy._entries.Add(new ResourceEntryData { Id = e.Id, Current = e.Current, Max = e.Max });
            }

            return copy;
        }
    }
}
