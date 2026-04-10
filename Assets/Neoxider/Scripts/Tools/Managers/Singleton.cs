using UnityEngine;

namespace Neo.Tools
{
    [NeoDoc("Tools/Managers/Singleton.md")]
    public class Singleton<T> : MonoBehaviour where T : Singleton<T>
    {
        /// <summary>
        ///     When enabled, a new singleton GameObject is created automatically if no instance is found.
        /// </summary>
        public static bool CreateInstance = false;

        private static T _instance;

        [Header("Settings")] [SerializeField] protected bool _dontDestroyOnLoad;
        [SerializeField] protected bool _setInstanceOnAwake = true;
        private bool _isInitialized;

        private static bool _searchFailed;

        /// <summary>
        ///     Gets the active singleton instance, creating or resolving it on first access when possible.
        /// </summary>
        public static T I
        {
            get
            {
                if (_instance == null && !_searchFailed)
                {
                    T[] all = FindObjectsByType<T>(FindObjectsInactive.Include, FindObjectsSortMode.None);
                    for (int i = 0; i < all.Length; i++)
                    {
                        if (all[i].SetInstanceOnAwakeEnabled)
                        {
                            _instance = all[i];
                            break;
                        }
                    }

                    if (_instance == null && CreateInstance)
                    {
                        GameObject obj = new(typeof(T).Name);
                        _instance = obj.AddComponent<T>();
                        _instance.OnInstanceCreated();
                    }

                    if (_instance == null)
                    {
                        _searchFailed = true;
                    }
                    else
                    {
                        if (!_instance._isInitialized)
                        {
                            _instance.Init();
                        }
                    }
                }

                return _instance;
            }
        }

        /// <summary>
        ///     Gets whether a singleton instance has already been resolved.
        /// </summary>
        public static bool IsInitialized => _instance != null;

        /// <summary>
        ///     Gets whether a singleton instance is currently available without forcing resolution.
        /// </summary>
        public static bool HasInstance => _instance != null;

        protected virtual bool DontDestroyOnLoadEnabled => _dontDestroyOnLoad;

        protected virtual bool SetInstanceOnAwakeEnabled => _setInstanceOnAwake;

        /// <summary>
        ///     <see langword="true"/> when this component is the same object as the static <c>_instance</c>.
        ///     Call after <see cref="Awake"/> base implementation: duplicates return <see langword="false"/> (another object holds <c>_instance</c>).
        /// </summary>
        protected bool IsCurrentSingletonInstance => ReferenceEquals(_instance, this as T);

        protected virtual void Awake()
        {
            if (SetInstanceOnAwakeEnabled)
            {
                if (_instance == null)
                {
                    _instance = this as T;
                    if (DontDestroyOnLoadEnabled)
                    {
                        DontDestroyOnLoad(gameObject);
                    }

                    Init();
                }
                else if (_instance != this)
                {
                    Destroy(gameObject);
                }
            }
        }

        protected virtual void OnInstanceCreated()
        {
        }

        protected virtual void Init()
        {
            if (_isInitialized)
            {
                return;
            }

            _isInitialized = true;
        }

        /// <summary>
        ///     Tries to get the current singleton instance without triggering a scene lookup.
        /// </summary>
        /// <param name="instance">Resolved singleton instance when available.</param>
        /// <returns><see langword="true" /> when an instance is already assigned; otherwise <see langword="false" />.</returns>
        public static bool TryGetInstance(out T instance)
        {
            instance = _instance;
            return instance != null;
        }

        /// <summary>
        ///     Destroys the current singleton instance and clears the cached reference.
        /// </summary>
        public static void DestroyInstance()
        {
            if (_instance != null)
            {
                Destroy(_instance.gameObject);
                _instance = null;
            }
        }

        protected virtual void OnDestroy()
        {
            if (_instance == this)
            {
                _instance = null;
            }
        }

#if UNITY_EDITOR
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetStaticState()
        {
            _instance = null;
            _searchFailed = false;
        }
#endif
    }
}
