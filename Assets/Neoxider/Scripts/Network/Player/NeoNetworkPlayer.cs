#if MIRROR
using Mirror;
#endif
using UnityEngine;
using UnityEngine.Events;

namespace Neo.Network
{
    /// <summary>
    ///     Base component for a networked player object.
    ///     Provides helpers for local-player detection, camera setup,
    ///     and input routing.
    ///     <para>Without Mirror this behaves as a simple MonoBehaviour
    ///     that always reports <see cref="IsLocalPlayer"/> as <c>true</c>.</para>
    /// </summary>
    [NeoDoc("Network/NeoNetworkPlayer.md")]
    [AddComponentMenu("Neoxider/Network/" + nameof(NeoNetworkPlayer))]
    public class NeoNetworkPlayer :
#if MIRROR
        NetworkBehaviour
#else
        MonoBehaviour
#endif
    {
        [Header("Local Player Setup")]
        [Tooltip("GameObjects to enable only for the local player (e.g. Camera, Input, UI).")]
        [SerializeField] private GameObject[] _localOnlyObjects;

        [Tooltip("GameObjects to disable for the local player (e.g. name tag above own head).")]
        [SerializeField] private GameObject[] _remoteOnlyObjects;

        [Header("Events")]
        [SerializeField] private UnityEvent _onLocalPlayerStarted = new();
        [SerializeField] private UnityEvent _onRemotePlayerStarted = new();

        /// <summary>Raised when this instance is confirmed as the local player.</summary>
        public UnityEvent OnLocalPlayerStarted => _onLocalPlayerStarted;

        /// <summary>Raised when this instance belongs to a remote player.</summary>
        public UnityEvent OnRemotePlayerStarted => _onRemotePlayerStarted;

        /// <summary>
        ///     Whether this player object represents the local (controlling) player.
        ///     Always <c>true</c> in solo mode.
        /// </summary>
        public bool IsLocalPlayer
        {
            get
            {
#if MIRROR
                return isLocalPlayer;
#else
                return true;
#endif
            }
        }

        /// <summary>
        ///     Whether this player object has authority over its state.
        ///     Always <c>true</c> in solo mode.
        /// </summary>
        public bool HasAuthority
        {
            get
            {
#if MIRROR
                return isOwned;
#else
                return true;
#endif
            }
        }

#if MIRROR
        public override void OnStartLocalPlayer()
        {
            base.OnStartLocalPlayer();
            SetupLocalPlayer();
        }

        public override void OnStartClient()
        {
            base.OnStartClient();
            if (!isLocalPlayer)
            {
                SetupRemotePlayer();
            }
        }
#else
        private void Start()
        {
            SetupLocalPlayer();
        }
#endif

        private void SetupLocalPlayer()
        {
            SetObjectsActive(_localOnlyObjects, true);
            SetObjectsActive(_remoteOnlyObjects, false);
            SetChildAudioListenersEnabled(true);
            _onLocalPlayerStarted?.Invoke();
            Debug.Log($"[NeoNetworkPlayer] Local player started: {gameObject.name}");
        }

        private void SetupRemotePlayer()
        {
            SetChildAudioListenersEnabled(false);
            SetObjectsActive(_localOnlyObjects, false);
            SetObjectsActive(_remoteOnlyObjects, true);
            _onRemotePlayerStarted?.Invoke();
            Debug.Log($"[NeoNetworkPlayer] Remote player started: {gameObject.name}");
        }

        /// <summary>
        ///     Unity allows only one active <see cref="AudioListener"/> in the loaded world.
        ///     Remote player prefabs often still contain a listener on the camera; disable it even
        ///     when <see cref="_localOnlyObjects"/> is not wired in the Inspector.
        /// </summary>
        private void SetChildAudioListenersEnabled(bool enabled)
        {
            var listeners = GetComponentsInChildren<AudioListener>(true);
            for (int i = 0; i < listeners.Length; i++)
            {
                if (listeners[i] != null)
                {
                    listeners[i].enabled = enabled;
                }
            }
        }

        private static void SetObjectsActive(GameObject[] objects, bool active)
        {
            if (objects == null) return;
            for (int i = 0; i < objects.Length; i++)
            {
                if (objects[i] != null)
                {
                    objects[i].SetActive(active);
                }
            }
        }
    }
}
