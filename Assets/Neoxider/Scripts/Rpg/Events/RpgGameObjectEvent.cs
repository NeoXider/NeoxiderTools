using System;
using UnityEngine;
using UnityEngine.Events;

namespace Neo.Rpg
{
    /// <summary>
    /// Reusable event carrying a GameObject payload.
    /// </summary>
    [Serializable]
    public sealed class RpgGameObjectEvent : UnityEvent<GameObject>
    {
    }
}
