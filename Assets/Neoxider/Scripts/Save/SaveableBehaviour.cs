using UnityEngine;

namespace Neo.Save
{
    /// <summary>
    ///     A base class for MonoBehaviours that should be part of the save system.
    ///     Automatically handles registration and unregistration with the SaveManager.
    /// </summary>
    public abstract class SaveableBehaviour : MonoBehaviour, ISaveableComponent
    {
        protected virtual void OnEnable()
        {
            SaveManager.Register(this);
        }

        protected virtual void OnDisable()
        {
        }

        public virtual void OnDataLoaded()
        {
        }
    }
}