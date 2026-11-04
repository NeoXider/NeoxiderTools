using Neo.Rpg;
using Neo.Rpg.Runtime;
using UnityEngine;

namespace Neo.Tests.Edit.RPG
{
    /// <summary>
    ///     Character network adapter probe that swallows every command and counts the routed ones.
    /// </summary>
    /// <remarks>
    ///     WHY: test MonoBehaviours live in the runtime fixture assembly; Unity 6.3 rejects AddComponent for
    ///     MonoBehaviours declared inside the Editor-only test assembly.
    /// </remarks>
    public sealed class RpgRoutingAdapterProbe : MonoBehaviour, IRpgCharacterNetworkAdapter
    {
        /// <summary>Number of commands routed through this adapter.</summary>
        public int Routed { get; private set; }

        /// <inheritdoc />
        public bool SuppressLocalSimulation => false;

        /// <inheritdoc />
        public bool TryRoute(RpgCharacterNetworkCommand command)
        {
            Routed++;
            return true;
        }

        /// <inheritdoc />
        public void NotifyStateChanged()
        {
        }
    }
}
