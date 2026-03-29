using System;
using UnityEngine;

namespace Neo.Settings
{
    /// <summary>Maps a named preset to a Unity QualitySettings level index.</summary>
    [Serializable]
    public struct GraphicsPresetLevelMapping
    {
        public GraphicsPreset Preset;
        [Min(0)] public int QualityLevelIndex;
    }
}
