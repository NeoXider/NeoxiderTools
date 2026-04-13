using UnityEditor;

namespace Neo.Editor
{
    // These specific editors are required to override Mirror's default generic NetworkBehaviourInspector
    // which prevents the Neoxider UI from drawing on Neo tools that inherit from NetworkBehaviour.
    
    [CustomEditor(typeof(Neo.Tools.InteractiveObject), true)]
    [CanEditMultipleObjects]
    public class InteractiveObjectNeoEditor : NeoCustomEditor { }

    [CustomEditor(typeof(Neo.Tools.NetworkEventDispatcher), true)]
    [CanEditMultipleObjects]
    public class NetworkEventDispatcherNeoEditor : NeoCustomEditor { }

    [CustomEditor(typeof(Neo.Condition.NeoCondition), true)]
    [CanEditMultipleObjects]
    public class NeoConditionNeoEditor : NeoCustomEditor { }

    [CustomEditor(typeof(Neo.Tools.Counter), true)]
    [CanEditMultipleObjects]
    public class CounterNeoEditor : NeoCustomEditor { }

    [CustomEditor(typeof(Neo.Tools.Selector), true)]
    [CanEditMultipleObjects]
    public class SelectorNeoEditor : NeoCustomEditor { }

    [CustomEditor(typeof(Neo.Tools.Spawner), true)]
    [CanEditMultipleObjects]
    public class SpawnerNeoEditor : NeoCustomEditor { }
}
