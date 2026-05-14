using UnityEditor;
using UnityEngine;

namespace Neo.Editor
{
    /// <summary>
    ///     Custom inspector drawer that handles automatic component and resource assignment based on attributes.
    ///     Supports finding components in scene, on GameObject, and loading from Resources.
    ///     Works with Odin Inspector by using DrawDefaultInspector when Odin is active.
    /// </summary>
    [CustomEditor(typeof(MonoBehaviour), true, isFallback = true)]
    [CanEditMultipleObjects]
    public class NeoCustomEditor : CustomEditorBase
    {
        // Debug: verify the editor instance is constructed
        static NeoCustomEditor()
        {
            //Debug.Log("[NeoCustomEditor] Class loaded and registered as CustomEditor for MonoBehaviour");
        }

        // Debug: verify the editor instance is constructed

        protected override void ProcessAttributeAssignments()
        {
            var targetObject = target as MonoBehaviour;
            if (targetObject == null)
            {
                return;
            }

            // Process component attributes
            ComponentDrawer.ProcessComponentAttributes(targetObject);

            // Process resource attributes
            ResourceDrawer.ProcessResourceAttributes(targetObject);
        }
    }

#if MIRROR
    // Mirror's NetworkBehaviourInspector has [CustomEditor(typeof(NetworkBehaviour), true)].
    // Because it is not isFallback=true, it overrides our NeoCustomEditor (which is a MonoBehaviour fallback).
    // To ensure Neoxider components retain their beautiful custom UI when they inherit from NetworkBehaviour,
    // we define exact type editors here. Exact type editors always beat inheritance editors.

    [CustomEditor(typeof(Neo.Tools.Counter), true)]
    [CanEditMultipleObjects]
    public class CounterNeoEditor : NeoCustomEditor { }

    [CustomEditor(typeof(Neo.Shop.Money), true)]
    [CanEditMultipleObjects]
    public class MoneyNeoEditor : NeoCustomEditor { }

    [CustomEditor(typeof(Neo.Tools.PhysicsEvents3D), true)]
    [CanEditMultipleObjects]
    public class PhysicsEvents3DNeoEditor : NeoCustomEditor { }

    [CustomEditor(typeof(Neo.Tools.PhysicsEvents2D), true)]
    [CanEditMultipleObjects]
    public class PhysicsEvents2DNeoEditor : NeoCustomEditor { }

    [CustomEditor(typeof(Neo.Tools.Spawner), true)]
    [CanEditMultipleObjects]
    public class SpawnerNeoEditor : NeoCustomEditor { }

    [CustomEditor(typeof(Neo.Tools.InteractiveObject), true)]
    [CanEditMultipleObjects]
    public class InteractiveObjectNeoEditor : NeoCustomEditor { }

    [CustomEditor(typeof(Neo.Tools.Selector), true)]
    [CanEditMultipleObjects]
    public class SelectorNeoEditor : NeoCustomEditor { }

    [CustomEditor(typeof(Neo.Rpg.RpgCombatant), true)]
    [CanEditMultipleObjects]
    public class RpgCombatantNeoEditor : NeoCustomEditor { }

    [CustomEditor(typeof(Neo.Network.NeoNetworkPlayer), true)]
    [CanEditMultipleObjects]
    public class NeoNetworkPlayerNeoEditor : NeoCustomEditor { }

    [CustomEditor(typeof(Neo.Tools.RandomRange), true)]
    [CanEditMultipleObjects]
    public class RandomRangeNeoEditor : NeoCustomEditor { }

    [CustomEditor(typeof(Neo.Network.NetworkContextActionRelay), true)]
    [CanEditMultipleObjects]
    public class NetworkContextActionRelayNeoEditor : NeoCustomEditor { }
#endif
}
