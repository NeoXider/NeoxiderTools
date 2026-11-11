using UnityEngine;

namespace Neo.Tools
{
    /// <summary>
    ///     Scene singleton base: the first enabled instance wins in <c>Awake</c>, later duplicates destroy their
    ///     GameObject. <see cref="I" /> resolves lazily (scene search, optional creation) and the statics are
    ///     cleared before each Editor play session, so it is safe with domain reload disabled.
    /// </summary>
    /// <typeparam name="T">The concrete singleton type (<c>class GameAudio : Singleton&lt;GameAudio&gt;</c>).</typeparam>
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
        private bool _isBeingDestroyed;

        private static bool _searchFailed;

        /// <summary>
        ///     Gets the active singleton instance, creating or resolving it on first access when possible.
        /// </summary>
        public static T I
        {
            get
            {
                if (_instance == null)
                {
                    // WHY: Short-circuit: if a previous search already found nothing and lazy creation
                    // is disabled, skip the expensive scene scan until the state is reset.
                    if (_searchFailed && !CreateInstance)
                    {
                        return null;
                    }

                    T[] all = FindObjectsByType<T>(FindObjectsInactive.Include, FindObjectsSortMode.None);
                    for (int i = 0; i < all.Length; i++)
                    {
                        if (!all[i]._isBeingDestroyed && all[i].SetInstanceOnAwakeEnabled)
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
                        _searchFailed = false;
                        if (!_instance._isInitialized)
                        {
                            if (Application.isPlaying && _instance.DontDestroyOnLoadEnabled)
                            {
                                DontDestroyOnLoad(_instance.gameObject);
                            }

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

        /// <summary>Backwards-compatible singleton alias for <see cref="I"/>.</summary>
        public static T Instance => I;

        protected virtual bool DontDestroyOnLoadEnabled => _dontDestroyOnLoad;

        protected virtual bool SetInstanceOnAwakeEnabled => _setInstanceOnAwake;

        /// <summary>
        ///     <see langword="true"/> when this component is the same object as the static <c>_instance</c>.
        ///     Call after <see cref="Awake"/> base implementation: duplicates return <see langword="false"/> (another object holds <c>_instance</c>).
        /// </summary>
        protected bool IsCurrentSingletonInstance => ReferenceEquals(_instance, this as T);

        protected virtual void Awake()
        {
            if (SetInstanceOnAwakeEnabled && !_isBeingDestroyed)
            {
                if (_instance != null && _instance != this)
                {
                    Destroy(gameObject);
                    return;
                }

                _instance = this as T;
                _searchFailed = false;
                if (Application.isPlaying && DontDestroyOnLoadEnabled)
                {
                    DontDestroyOnLoad(gameObject);
                }

                if (!_isInitialized)
                {
                    Init();
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
        /// <remarks>
        ///     In Play Mode the GameObject is destroyed at the end of the frame (<c>Destroy</c>); in Edit Mode, where
        ///     <c>Destroy</c> is not allowed, it is destroyed immediately.
        /// </remarks>
        public static void DestroyInstance()
        {
            if (_instance != null)
            {
                T instance = _instance;
                instance._isBeingDestroyed = true;
                _instance = null;
                _searchFailed = false;
                if (Application.isPlaying)
                {
                    Destroy(instance.gameObject);
                }
                else
                {
                    DestroyImmediate(instance.gameObject);
                }
            }
        }

        protected virtual void OnDestroy()
        {
            if (_instance == this)
            {
                _instance = null;
            }
        }

        internal static void ResetStaticStateForRuntime()
        {
            _instance = null;
            _searchFailed = false;
        }
    }
}
