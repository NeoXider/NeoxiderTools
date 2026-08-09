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
        bool SuppressLocalSimulation { get; }
        bool TryRoute(RpgCharacterNetworkCommand command);
        void NotifyStateChanged();
    }
}
