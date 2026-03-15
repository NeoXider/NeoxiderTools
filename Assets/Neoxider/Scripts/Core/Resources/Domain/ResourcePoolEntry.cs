namespace Neo.Core.Resources
{
    /// <summary>
    ///     Data for a single resource pool (no UnityEngine). Used by ResourcePoolModel.
    /// </summary>
    public sealed class ResourcePoolEntry
    {
        public float Current { get; set; }
        public float Max { get; set; }
        public float RegenPerSecond { get; set; }
        public float RegenInterval { get; set; }
        public float MaxDecreaseAmount { get; set; } // -1 = no limit
        public float MaxIncreaseAmount { get; set; } // -1 = no limit
        public bool RestoreOnAwake { get; set; }
        public bool IgnoreCanHeal { get; set; }
        public float HealAmount { get; set; }
        public float HealDelay { get; set; }
    }
}
