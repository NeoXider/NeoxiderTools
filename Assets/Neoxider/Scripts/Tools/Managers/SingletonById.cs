using System.Collections.Generic;
using UnityEngine;

namespace Neo.Tools
{
    /// <summary>
    ///     Singleton keyed by Id: only one live instance per Id at a time.
    ///     When a second object with the same Id appears, the previous one is destroyed (new wins).
    ///     Optionally survives scene loads via DontDestroyOnLoad.
    /// </summary>
    /// <typeparam name="T">Component type inheriting SingletonById&lt;T&gt;.</typeparam>
    [NeoDoc("Tools/Managers/SingletonById.md")]
    [DisallowMultipleComponent]
    public class SingletonById<T> : MonoBehaviour where T : SingletonById<T>
    {
        private static readonly Dictionary<string, T> ById = new();

        [Header("Singleton by Id")]
        [Tooltip("Unique identifier. Only one instance per Id; when a new one appears, the previous is destroyed.")]
        [SerializeField]
        private string _id = "Default";

        [Tooltip("Do not destroy this object when loading new scenes (persists across scenes).")] [SerializeField]
        private bool _dontDestroyOnLoad;

        /// <summary>Unique Id for this singleton.</summary>
        public string Id => _id;

        protected virtual void Awake()
        {
            if (string.IsNullOrEmpty(_id))
            {
                Debug.LogWarning($"[SingletonById] Id is not set on {gameObject.name}; instance is not registered.",
                    this);
                return;
            }

            if (ById.TryGetValue(_id, out T existing) && existing != null && existing != this)
            {
                Destroy(existing.gameObject);
            }

            ById[_id] = (T)this;

            if (_dontDestroyOnLoad)
            {
                DontDestroyOnLoad(gameObject);
            }
        }

        protected virtual void OnDestroy()
        {
            if (string.IsNullOrEmpty(_id))
            {
                return;
            }

            if (ById.TryGetValue(_id, out T current) && current == this)
            {
                ById.Remove(_id);
            }
        }

        /// <summary>Get instance by Id, or null.</summary>
        public static T Get(string id)
        {
            if (string.IsNullOrEmpty(id))
            {
                return null;
            }

            return ById.TryGetValue(id, out T instance) ? instance : null;
        }

        /// <summary>Whether a live instance exists for the Id.</summary>
        public static bool Has(string id)
        {
            return Get(id) != null;
        }
    }
}
