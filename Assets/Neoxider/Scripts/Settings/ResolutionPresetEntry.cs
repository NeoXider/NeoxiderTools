using System;
using UnityEngine;

namespace Neo.Settings
{
    /// <summary>Optional fixed resolution entry (width × height). Used when not building from <see cref="Screen.resolutions"/> only.</summary>
    [Serializable]
    public struct ResolutionPresetEntry
    {
        [Min(1)] public int Width;
        [Min(1)] public int Height;
    }
}
