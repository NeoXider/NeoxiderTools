using UnityEngine;

namespace Neo.Tools
{
    /// <summary>
    ///     Базовый MonoBehaviour с пустыми виртуальными методами IPoolable.
    ///     Наследуйте от него объекты из пула и переопределяйте OnPoolGet/OnPoolRelease при необходимости.
    /// </summary>
    [NeoDoc("Tools/Spawner/PoolableBehaviour.md")]
    public abstract class PoolableBehaviour : MonoBehaviour, IPoolable
    {
        public virtual void OnPoolCreate() { }

        public virtual void OnPoolGet() { }

        public virtual void OnPoolRelease() { }
    }
}
