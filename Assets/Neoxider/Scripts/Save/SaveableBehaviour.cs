using UnityEngine;

namespace Neo.Save
{
    /// <summary>
    ///     A base class for MonoBehaviours that should be part of the save system.
    ///     Automatically handles registration and unregistration with the SaveManager.
    /// </summary>
    [NeoDoc("Save/README.md")]
    public abstract class SaveableBehaviour : MonoBehaviour, ISaveableComponent
    {
        protected virtual void OnEnable()
        {
            SaveManager.Register(this);
        }

        protected virtual void OnDisable()
        {
            // WHY: the global quit save writes only registered components — flush this component's
            // auto-saved fields before it leaves the registry so session changes survive a disable.
            SaveManager.SaveAutoFields(this);
            SaveManager.Unregister(this);
        }

        public virtual void OnDataLoaded()
        {
        }
    }
}
