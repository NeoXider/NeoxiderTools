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
        // WHY: debug hook to verify the editor instance is constructed
        static NeoCustomEditor()
        {
        }

        protected override void ProcessAttributeAssignments()
        {
            var targetObject = target as MonoBehaviour;
            if (targetObject == null)
            {
                return;
            }

            ComponentDrawer.ProcessComponentAttributes(targetObject);

            ResourceDrawer.ProcessResourceAttributes(targetObject);
        }
    }

#if MIRROR
    // WHY: Mirror's NetworkBehaviourInspector has [CustomEditor(typeof(NetworkBehaviour), true)].
    // Because it is not isFallback=true, it overrides our NeoCustomEditor (which is a MonoBehaviour fallback).
    // To ensure Neoxider components retain their beautiful custom UI when they inherit from NetworkBehaviour,
    // we define exact type editors here. Exact type editors always beat inheritance editors.

    [CustomEditor(typeof(Neo.Tools.Counter), true)]
    [CanEditMultipleObjects]
    public class CounterNeoEditor : NeoCustomEditor
    {
    }

    [CustomEditor(typeof(Shop.Money), true)]
    [CanEditMultipleObjects]
    public class MoneyNeoEditor : NeoCustomEditor
    {
    }

    [CustomEditor(typeof(Neo.Tools.PhysicsEvents3D), true)]
    [CanEditMultipleObjects]
    public class PhysicsEvents3DNeoEditor : NeoCustomEditor
    {
    }

    [CustomEditor(typeof(Neo.Tools.PhysicsEvents2D), true)]
    [CanEditMultipleObjects]
    public class PhysicsEvents2DNeoEditor : NeoCustomEditor
    {
    }

    [CustomEditor(typeof(Neo.Tools.Spawner), true)]
    [CanEditMultipleObjects]
    public class SpawnerNeoEditor : NeoCustomEditor
    {
    }

    [CustomEditor(typeof(Neo.Tools.InteractiveObject), true)]
    [CanEditMultipleObjects]
    public class InteractiveObjectNeoEditor : NeoCustomEditor
    {
    }

    [CustomEditor(typeof(Neo.Tools.Selector), true)]
    [CanEditMultipleObjects]
    public class SelectorNeoEditor : NeoCustomEditor
    {
    }

    [CustomEditor(typeof(Neo.Network.NeoNetworkPlayer), true)]
    [CanEditMultipleObjects]
    public class NeoNetworkPlayerNeoEditor : NeoCustomEditor
    {
    }

    [CustomEditor(typeof(Neo.Tools.RandomRange), true)]
    [CanEditMultipleObjects]
    public class RandomRangeNeoEditor : NeoCustomEditor
    {
    }

    // WHY: NetworkContextActionRelay has its OWN dedicated editor (NetworkContextActionRelayEditor)
    // that inherits from CustomEditorBase and draws a fully custom NoCode-style inspector.
    // Don't register a NeoCustomEditor fallback for it here — Unity would pick one of the two arbitrarily.

    [CustomEditor(typeof(Neo.Network.NetworkActionRelay), true)]
    [CanEditMultipleObjects]
    public class NetworkActionRelayNeoEditor : NeoCustomEditor
    {
    }

    [CustomEditor(typeof(Neo.Network.NetworkPropertySync), true)]
    [CanEditMultipleObjects]
    public class NetworkPropertySyncNeoEditor : NeoCustomEditor
    {
    }

    [CustomEditor(typeof(Neo.Network.NetworkOwnerFilter), true)]
    [CanEditMultipleObjects]
    public class NetworkOwnerFilterNeoEditor : NeoCustomEditor
    {
    }

    [CustomEditor(typeof(Neo.Tools.NetworkEventDispatcher), true)]
    [CanEditMultipleObjects]
    public class NetworkEventDispatcherNeoEditor : NeoCustomEditor
    {
    }

    // WHY: catch-all for every NeoNetworkComponent subclass (NetworkReactiveSync, NetworkPlayerName,
    // and any future one) so none silently fall back to Mirror's plain NetworkBehaviour inspector.
    // NetworkContextActionRelay keeps its own exact-type editor, which always beats this inherited one.
    [CustomEditor(typeof(Neo.Network.NeoNetworkComponent), true)]
    [CanEditMultipleObjects]
    public class NeoNetworkComponentNeoEditor : NeoCustomEditor
    {
    }

    // WHY: the movement controllers derive from NetworkBehaviour under MIRROR (INeoOptionalNetworked),
    // so Mirror's inspector would claim them without these exact-type overrides.
    [CustomEditor(typeof(Neo.Tools.PlayerController3DPhysics), true)]
    [CanEditMultipleObjects]
    public class PlayerController3DPhysicsNeoEditor : NeoCustomEditor
    {
    }

    [CustomEditor(typeof(Neo.Tools.PlayerController2DPhysics), true)]
    [CanEditMultipleObjects]
    public class PlayerController2DPhysicsNeoEditor : NeoCustomEditor
    {
    }
#endif
}
