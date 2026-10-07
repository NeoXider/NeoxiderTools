using UnityEngine;
#if MIRROR
using Mirror;
#endif

namespace Neo.Network
{
    /// <summary>
    ///     Base class for networked singleton managers.
    ///     When Mirror is installed, inherits from <see cref="Mirror.NetworkBehaviour"/>.
    ///     Without Mirror, falls back to <see cref="MonoBehaviour"/> with standard singleton logic.
    /// </summary>
    /// <typeparam name="T">Concrete manager type.</typeparam>
    [NeoDoc("Network/NetworkSingleton.md")]
    public class NetworkSingleton<T> :
#if MIRROR
        NetworkBehaviour
#else
        MonoBehaviour
#endif
        where T : NetworkSingleton<T>
    {
        private static T _instance;
        private static bool _searchFailed;
        private static int _searchFailedFrame = -1;
        private static int _searchFailedEpoch = -1;

        [Header("Singleton")]
        [Tooltip("When enabled, the singleton object will not be destroyed on scene load.")]
        [SerializeField]
        protected bool _dontDestroyOnLoad;

        [Tooltip("When enabled, the script will automatically assign itself as the singleton instance during Awake.")]
        [SerializeField]
        protected bool _setInstanceOnAwake = true;

        private bool _isInitialized;

        /// <summary>
        ///     Gets the active singleton instance, resolving it on first access.
        ///     <para>
        ///         A failed lookup is cached only for the rest of the current frame (and until the next scene load or
        ///         unload), so a singleton that appears later - an additively loaded scene, an object enabled by the
        ///         network start, an instance created one frame after the first caller - is found as soon as it exists.
        ///         Before 10.18.0 a miss was cached for the whole play session.
        ///     </para>
        /// </summary>
        public static T I
        {
            get
            {
                if (_instance == null && !IsFailedSearchCached)
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

                    if (_instance == null)
                    {
                        _searchFailed = true;
                        _searchFailedFrame = Time.frameCount;
                        _searchFailedEpoch = NetworkSingletonSearchState.Epoch;
                    }
                    else if (!_instance._isInitialized)
                    {
                        _instance.Init();
                    }
                }

                return _instance;
            }
        }

        /// <summary>
        ///     True while a previous empty lookup is still trusted: same frame, no scene load or unload since.
        /// </summary>
        private static bool IsFailedSearchCached =>
            _searchFailed
            && _searchFailedFrame == Time.frameCount
            && _searchFailedEpoch == NetworkSingletonSearchState.Epoch;

        /// <summary>Gets whether a singleton instance is currently available.</summary>
        public static bool HasInstance => _instance != null;

        /// <summary>Gets whether the singleton has been resolved and initialized.</summary>
        public static bool IsInitialized => _instance != null && _instance._isInitialized;

        /// <summary>Backwards-compatible singleton alias.</summary>
        public static T Instance => I;

        protected virtual bool DontDestroyOnLoadEnabled => _dontDestroyOnLoad;
        protected virtual bool SetInstanceOnAwakeEnabled => _setInstanceOnAwake;

        /// <summary>
        ///     When <see cref="SetInstanceOnAwakeEnabled"/> is true and another instance is already
        ///     registered, the default behaviour is to destroy this GameObject. Override and return
        ///     <see langword="false"/> when multiple instances are valid (e.g. several
        ///     <see cref="Neo.Shop.Money"/> wallets keyed by different save keys).
        /// </summary>
        protected virtual bool DestroyGameObjectOnDuplicateSingleton => true;

        /// <summary>
        ///     <see langword="true"/> when this component is the static instance.
        /// </summary>
        protected bool IsCurrentSingletonInstance => ReferenceEquals(_instance, this as T);

#if MIRROR
        /// <summary>
        ///     Whether this instance has authority to perform server-side mutations.
        ///     In host mode the host has authority; in dedicated server mode the server does.
        /// </summary>
        public bool HasServerAuthority => NeoNetworkState.IsServer;
#else
        /// <summary>
        ///     In solo mode every instance has full authority.
        /// </summary>
        public bool HasServerAuthority => true;
#endif

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
                    if (DestroyGameObjectOnDuplicateSingleton)
                    {
                        Destroy(gameObject);
                    }
                }
            }
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
        public static bool TryGetInstance(out T instance)
        {
            instance = _instance;
            return instance != null;
        }

        /// <summary>
        ///     Forgets a cached empty lookup so the next <see cref="I"/> searches the scenes again, even within the same
        ///     frame. Call it right after you create or enable the singleton object by hand.
        /// </summary>
        public static void ForgetFailedSearch()
        {
            _searchFailed = false;
            _searchFailedFrame = -1;
            _searchFailedEpoch = -1;
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

        internal static void ResetStaticStateForRuntime()
        {
            _instance = null;
            ForgetFailedSearch();
        }
    }
}
