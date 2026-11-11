using System;
using UnityEngine;

namespace Neo.Core.Resources
{
    [Serializable]
    public sealed class ResourceEntryData
    {
        [SerializeField] private string _id = "HP";
        [SerializeField] private float _current = 100f;
        [SerializeField] private float _max = 100f;

        public string Id { get => _id; set => _id = value ?? "HP"; }
        public float Current { get => _current; set => _current = value; }
        public float Max { get => _max; set => _max = value < 0f ? 0f : value; }
    }
}
