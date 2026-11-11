using UnityEngine;

namespace Neo.Tools
{
    /// <summary>
    ///     Base MonoBehaviour with default no-op IPoolable methods.
    ///     Inherit for pooled objects and override OnPoolGet/OnPoolRelease as needed.
    /// </summary>
    [NeoDoc("Tools/Spawner/PoolableBehaviour.md")]
    public abstract class PoolableBehaviour : MonoBehaviour, IPoolable
    {
        public virtual void OnPoolCreate()
        {
        }

        public virtual void OnPoolGet()
        {
        }

        public virtual void OnPoolRelease()
        {
        }
    }
}
