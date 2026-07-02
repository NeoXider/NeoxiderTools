using UnityEngine;
using UnityEngine.Events;
#if MIRROR
using Mirror;
#endif

namespace Neo.Network
{
    /// <summary>
    ///     Player nickname replicated to everyone — the missing piece for casual multiplayer HUDs.
    ///     Put on the player object next to <see cref="NeoNetworkPlayer"/>, bind
    ///     <see cref="OnNameChanged"/> to a TMP label, and call <see cref="SetLocalName"/> from an
    ///     input field (or your save system) on the local player.
    ///     <para>Names are trimmed and length-capped on the server; the change command is rate-limited.</para>
    ///     <para>Without Mirror the component works locally: <see cref="SetLocalName"/> just fires the event.</para>
    /// </summary>
    [NeoDoc("Network/NetworkPlayerName.md")]
    [AddComponentMenu("Neoxider/Network/" + nameof(NetworkPlayerName))]
    public class NetworkPlayerName : NeoNetworkComponent
    {
        [Tooltip("Fallback name shown until a real one arrives.")]
        [SerializeField]
        private string _defaultName = "Player";

        [Tooltip("Maximum accepted name length; longer names are trimmed server-side.")]
        [SerializeField] [Min(1)]
        private int _maxLength = 16;

        [Header("Events")]
        [Tooltip("Fires on every client whenever this player's name changes (bind a TMP label here).")]
        public UnityEvent<string> OnNameChanged = new();

#if MIRROR
        [SyncVar(hook = nameof(OnNameSynced))] private string _playerName = "";
#else
        private string _playerName = "";
#endif

        /// <summary>Current replicated name (falls back to the default while unset).</summary>
        public string PlayerName => string.IsNullOrEmpty(_playerName) ? _defaultName : _playerName;

        private void Start()
        {
            OnNameChanged?.Invoke(PlayerName);
        }

        /// <summary>
        ///     Sets this player's name. On the local player in multiplayer the value is sent to the
        ///     server (validated, rate-limited) and replicated; offline it applies immediately.
        /// </summary>
        public void SetLocalName(string value)
        {
            string sanitized = Sanitize(value);
#if MIRROR
            if (isNetworked && NeoNetworkState.IsNetworkActive)
            {
                if (isOwned || isLocalPlayer)
                {
                    CmdSetName(sanitized);
                }

                return;
            }
#endif
            _playerName = sanitized;
            OnNameChanged?.Invoke(PlayerName);
        }

        private string Sanitize(string value)
        {
            string result = (value ?? "").Trim();
            if (result.Length > _maxLength)
            {
                result = result.Substring(0, _maxLength);
            }

            return result;
        }

#if MIRROR
        [Command(requiresAuthority = true)]
        private void CmdSetName(string value)
        {
            if (RateLimitCheck())
            {
                return;
            }

            _playerName = Sanitize(value);
        }

        private void OnNameSynced(string _, string __)
        {
            OnNameChanged?.Invoke(PlayerName);
        }

        public override void OnStartClient()
        {
            base.OnStartClient();
            OnNameChanged?.Invoke(PlayerName);
        }
#endif
    }
}
