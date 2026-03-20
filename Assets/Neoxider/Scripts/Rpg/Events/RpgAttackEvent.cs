using System;
using UnityEngine.Events;

namespace Neo.Rpg
{
    /// <summary>
    ///     Reusable event carrying an attack identifier.
    /// </summary>
    [Serializable]
    public sealed class RpgAttackEvent : UnityEvent<string>
    {
    }
}
