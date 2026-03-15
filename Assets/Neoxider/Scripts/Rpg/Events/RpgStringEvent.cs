using System;
using UnityEngine.Events;

namespace Neo.Rpg
{
    /// <summary>
    /// Serializable UnityEvent wrapper for string payloads used by RPG runtime components.
    /// </summary>
    [Serializable]
    public sealed class RpgStringEvent : UnityEvent<string>
    {
    }
}
