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

                if (float.IsNaN(e.Max) || float.IsInfinity(e.Max) || e.Max < 0f)
                {
                    e.Max = 0f;
                }

                if (e.Max > 0f && e.Current > e.Max)
                {
                    e.Current = e.Max;
                }

                if (float.IsNaN(e.Current) || float.IsInfinity(e.Current) || e.Current < 0f)
                {
                    e.Current = 0f;
                }
            }
        }

        public ResourceProfileData Clone()
        {
            ResourceProfileData copy = new ResourceProfileData();
            if (_entries == null)
            {
                return copy;
            }

            foreach (ResourceEntryData e in _entries)
            {
                if (e != null)
                {
                    copy._entries.Add(new ResourceEntryData { Id = e.Id, Current = e.Current, Max = e.Max });
                }
            }

            return copy;
        }
    }
}
