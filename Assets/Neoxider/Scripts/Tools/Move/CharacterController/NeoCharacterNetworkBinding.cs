using CMF;
using UnityEngine;
#if MIRROR
using Mirror;
#endif

namespace Neo.Tools
{
    /// <summary>
    ///     Makes a CMF character prefab multiplayer-ready with Mirror: only the local player simulates and reads input,
    ///     every remote copy is a passive proxy driven by <c>NetworkTransform</c>.
    /// </summary>
    /// <remarks>
    ///     CMF is a single-player controller — it runs its motor on every instance, so without this component each
    ///     client would locally simulate all players and fight the incoming snapshots.
    ///     <para>
    ///         Add this to the character root next to <c>AdvancedWalkerController</c>. With Mirror installed a
    ///         <see cref="NetworkTransformUnreliable" /> is required on the same GameObject and is configured
    ///         automatically (<c>syncDirection = ClientToServer</c>, target = this transform). Without Mirror the
    ///         component compiles to a no-op, so the same prefab still works in a single-player project.
    ///     </para>
    ///     <para>
    ///         Client authority means clients report their own position — the standard trade-off for responsive
    ///         movement. Validate positions server-side if your game needs to resist cheating.
    ///     </para>
    /// </remarks>
    [NeoDoc("Tools/Move/CharacterController/NeoCharacterNetworkBinding.md")]
    [AddComponentMenu("Neoxider/" + "Tools/" + nameof(NeoCharacterNetworkBinding))]
#if MIRROR
    [RequireComponent(typeof(NetworkIdentity))]
    [RequireComponent(typeof(NetworkTransformUnreliable))]
    [DefaultExecutionOrder(-100)]
#endif
    public class NeoCharacterNetworkBinding :
#if MIRROR
        NetworkBehaviour
#else
        MonoBehaviour
#endif
    {
        [Header("Local player only")]
        [Tooltip("Disabled on remote proxies so they are driven purely by NetworkTransform instead of re-simulating.")]
        [SerializeField]
        private Controller _controller;

        [SerializeField] private Mover _mover;
        [SerializeField] private NeoCharacterInput _characterInput;

        [Tooltip("Camera rig of this character. Only the local player's rig stays active — remote rigs would fight for the view.")]
        [SerializeField]
        private GameObject _cameraRig;

        [Tooltip("Extra objects enabled only for the local player (audio listener, UI, name tag hiding, ...).")]
        [SerializeField]
        private GameObject[] _localOnlyObjects = System.Array.Empty<GameObject>();

        [Header("Physics")]
        [Tooltip("Make the Rigidbody kinematic on remote proxies so local physics does not fight incoming snapshots.")]
        [SerializeField]
        private bool _kinematicOnRemote = true;

        [SerializeField] private Rigidbody _rigidbody;

        /// <summary>
        ///     True when this instance is allowed to read input and simulate: the local player, or any instance in a
        ///     project running without Mirror or outside an active network session.
        /// </summary>
        public bool HasInputAuthority
        {
            get
            {
#if MIRROR
                return !IsNetworkActive() || isLocalPlayer;
#else
                return true;
#endif
            }
        }

        private void Awake()
        {
            ResolveReferences();
#if MIRROR
            ConfigureNetworkTransform();
#endif
        }

        private void Start()
        {
            ApplyAuthority();
        }

#if MIRROR
        /// <inheritdoc />
        public override void OnStartLocalPlayer()
        {
            base.OnStartLocalPlayer();
            ApplyAuthority();
        }

        /// <inheritdoc />
        public override void OnStartClient()
        {
            base.OnStartClient();
            ApplyAuthority();
        }

        private static bool IsNetworkActive()
        {
            return NetworkClient.active || NetworkServer.active;
        }

        private void ConfigureNetworkTransform()
        {
            NetworkTransformBase networkTransform = GetComponent<NetworkTransformBase>();
            if (networkTransform == null)
            {
                return;
            }

            // WHY: a wrong target in the Inspector silently breaks replication, and the character always moves its own
            // root transform — so the target is corrected here rather than left to hand-wiring.
            if (networkTransform.target != transform)
            {
                networkTransform.target = transform;
            }

            networkTransform.syncDirection = SyncDirection.ClientToServer;
        }
#endif

        /// <summary>
        ///     Re-applies the local/remote split. Call after enabling components at runtime (respawn, possession).
        /// </summary>
        public void ApplyAuthority()
        {
            bool local = HasInputAuthority;

            if (_controller != null)
            {
                _controller.enabled = local;
            }

            if (_mover != null)
            {
                _mover.enabled = local;
            }

            if (_characterInput != null)
            {
                _characterInput.enabled = local;
            }

            if (_cameraRig != null)
            {
                _cameraRig.SetActive(local);
            }

            for (int i = 0; i < _localOnlyObjects.Length; i++)
            {
                if (_localOnlyObjects[i] != null)
                {
                    _localOnlyObjects[i].SetActive(local);
                }
            }

            if (_kinematicOnRemote && _rigidbody != null)
            {
                _rigidbody.isKinematic = !local;
            }
        }

        private void ResolveReferences()
        {
            if (_controller == null)
            {
                _controller = GetComponent<Controller>();
            }

            if (_mover == null)
            {
                _mover = GetComponent<Mover>();
            }

            if (_characterInput == null)
            {
                _characterInput = GetComponent<NeoCharacterInput>();
            }

            if (_rigidbody == null)
            {
                _rigidbody = GetComponent<Rigidbody>();
            }
        }
    }
}
