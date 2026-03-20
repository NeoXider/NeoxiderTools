using System;
using UnityEngine.Events;

namespace Neo.Progression
{
    /// <summary>
    ///     Serializable UnityEvent wrapper for string payloads used by progression runtime components.
    /// </summary>
    [Serializable]
    public sealed class ProgressionStringEvent : UnityEvent<string>
    {
    }
}
