using Neo.Rpg.Components;
using Neo.Rpg.Runtime;

namespace Neo.Rpg
{
    /// <summary>
    ///     Optional transport boundary used by <see cref="RpgCharacter"/> without taking a dependency
    ///     on a multiplayer implementation. The Mirror implementation lives in Neo.Rpg.Network.
    /// </summary>
    public interface IRpgCharacterNetworkAdapter
    {
        /// <summary>True when the character must not run regeneration and effect ticks locally (a pure client).</summary>
        bool SuppressLocalSimulation { get; }

        /// <summary>
        ///     Sends <paramref name="command"/> to the authority; <see langword="false"/> when the character must
        ///     execute it locally (server, host or offline).
        /// </summary>
        bool TryRoute(RpgCharacterNetworkCommand command);

        /// <summary>
        ///     Publishes the committed character state. Called once per completed step (a simulation frame, a hit,
        ///     a level-up, a <see cref="RpgCharacter.DeferSnapshotPublication"/> scope), never mid-step.
        /// </summary>
        void NotifyStateChanged();
    }
}
