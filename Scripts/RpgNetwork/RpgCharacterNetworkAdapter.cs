using Neo.Network;
using Neo.Rpg.Components;
using Neo.Rpg.Runtime;
using UnityEngine;

#if MIRROR
using Mirror;
#endif

namespace Neo.Rpg.Network
{
    /// <summary>
    ///     Optional Mirror transport for <see cref="RpgCharacter"/>. Keeping this on a separate
    ///     component lets the RPG assembly compile and run without Mirror or Neo.Network.
    /// </summary>
    [NeoDoc("Rpg/Network.md")]
    [AddComponentMenu("Neoxider/RPG/Network/Rpg Character Network Adapter")]
    [RequireComponent(typeof(RpgCharacter))]
#if MIRROR
    [RequireComponent(typeof(NetworkIdentity))]
#endif
    public sealed class RpgCharacterNetworkAdapter : NeoNetworkComponent, IRpgCharacterNetworkAdapter
    {
        private readonly RpgCharacterProfileService _profileService = new();
        private RpgCharacter _character;
        private int _publishedSnapshots;

        /// <summary>Number of snapshots this adapter serialized into the replicated state (a test seam).</summary>
        internal int PublishedSnapshotCount => _publishedSnapshots;

#if MIRROR
        [SyncVar(hook = nameof(OnSnapshotSynced))]
        private string _syncSnapshot = string.Empty;
#endif

        /// <summary>True on a pure client of a networked character: the server simulates regeneration and effects, the client only applies snapshots.</summary>
        public bool SuppressLocalSimulation
        {
            get
            {
#if MIRROR
                return IsEnabled && NeoNetworkState.IsClientOnly;
#else
                return false;
#endif
            }
        }

        private bool IsEnabled => _character != null && _character.isNetworked;

        private void Awake()
        {
            _character = GetComponent<RpgCharacter>();
            isNetworked = IsEnabled;
        }

        /// <summary>Sends <paramref name="command"/> to the server from a pure client; <see langword="false"/> when the caller must execute it locally.</summary>
        public bool TryRoute(RpgCharacterNetworkCommand command)
        {
#if MIRROR
            if (IsEnabled && NeoNetworkState.IsClientOnly && !NeoNetworkState.IsServer)
            {
                CmdExecute((int)command.Type, command.Text, command.Number, command.Integer, command.Flag);
                return true;
            }
#endif
            return false;
        }

        /// <summary>
        ///     Server: publishes the character profile into the replicated snapshot. <see cref="RpgCharacter"/> calls it
        ///     once per completed step (see <see cref="RpgCharacter.DeferSnapshotPublication"/>); an executed command
        ///     publishes exactly once.
        /// </summary>
        public void NotifyStateChanged()
        {
#if MIRROR
            if (!IsEnabled || !NeoNetworkState.IsServer)
            {
                return;
            }

            _syncSnapshot = _profileService.Serialize(_character.CaptureProfile());
            _publishedSnapshots++;
#endif
        }

        /// <summary>Damages the character on the authority (routed through the server from a pure client).</summary>
        public void NetDamage(float amount)
        {
            RouteOrExecute(new RpgCharacterNetworkCommand(RpgCharacterNetworkCommandType.Damage, number: amount));
        }

        /// <summary>Typed damage on the authority (routed through the server from a pure client).</summary>
        public void NetDamageType(string damageType, float amount)
        {
            RouteOrExecute(new RpgCharacterNetworkCommand(RpgCharacterNetworkCommandType.Damage, damageType, amount));
        }

        /// <summary>Heals on the authority (routed through the server from a pure client).</summary>
        public void NetHeal(float amount)
        {
            RouteOrExecute(new RpgCharacterNetworkCommand(RpgCharacterNetworkCommandType.Heal, number: amount));
        }

        /// <summary>Spends a resource on the authority (routed through the server from a pure client).</summary>
        public void NetSpend(string resourceId, float amount)
        {
            RouteOrExecute(new RpgCharacterNetworkCommand(RpgCharacterNetworkCommandType.Spend, resourceId, amount));
        }

        /// <summary>Refills a resource on the authority (routed through the server from a pure client).</summary>
        public void NetRefill(string resourceId, float amount)
        {
            RouteOrExecute(new RpgCharacterNetworkCommand(RpgCharacterNetworkCommandType.Refill, resourceId, amount));
        }

        /// <summary>Applies a buff by id on the authority (routed through the server from a pure client).</summary>
        public void NetApplyBuffById(string id)
        {
            RouteOrExecute(new RpgCharacterNetworkCommand(RpgCharacterNetworkCommandType.ApplyBuff, id));
        }

        /// <summary>Applies an inline template buff on the authority (routed through the server from a pure client).</summary>
        public void NetApplyInlineBuff(int index)
        {
            RouteOrExecute(new RpgCharacterNetworkCommand(RpgCharacterNetworkCommandType.ApplyInlineBuff,
                integer: index));
        }

        /// <summary>Applies a status by id on the authority (routed through the server from a pure client).</summary>
        public void NetApplyStatusById(string id)
        {
            RouteOrExecute(new RpgCharacterNetworkCommand(RpgCharacterNetworkCommandType.ApplyStatus, id));
        }

        /// <summary>Adds levels on the authority (routed through the server from a pure client).</summary>
        public void NetAddLevel(int delta)
        {
            RouteOrExecute(new RpgCharacterNetworkCommand(RpgCharacterNetworkCommandType.AddLevel, integer: delta));
        }

        private void RouteOrExecute(RpgCharacterNetworkCommand command)
        {
            // WHY: public UnityEvent calls can reach an inactive object before Awake has run.
            if (_character == null)
            {
                _character = GetComponent<RpgCharacter>();
            }

            if (!TryRoute(command))
            {
                ExecuteAndPublish(command);
            }
        }

        private void ExecuteAndPublish(RpgCharacterNetworkCommand command)
        {
            int published = _publishedSnapshots;
            // WHY: one command (a lethal hit, a level-up) used to serialize the profile after every inner change
            // and once more here; the deferral publishes the final state once.
            using (_character.DeferSnapshotPublication())
            {
                Execute(command);
            }

            if (published == _publishedSnapshots)
            {
                NotifyStateChanged();
            }
        }

#if MIRROR
        [Command(requiresAuthority = false)]
        private void CmdExecute(int type, string text, float number, int integer, bool flag,
            NetworkConnectionToClient sender = null)
        {
            if (!IsEnabled || RateLimitCheck(sender) || !IsAuthorized(sender))
            {
                return;
            }

            // WHY: the wire carries a raw int; an unnamed value would execute nothing yet still make the server
            // capture, serialize and replicate a full snapshot.
            if (!RpgCharacterNetworkCommand.IsKnownType((RpgCharacterNetworkCommandType)type))
            {
                return;
            }

            ExecuteAndPublish(new RpgCharacterNetworkCommand((RpgCharacterNetworkCommandType)type, text, number,
                integer, flag));
        }

        private bool IsAuthorized(NetworkConnectionToClient sender)
        {
            NetworkAuthorityMode mode = _character.AllowClientStateCommands
                ? _character.AuthorityMode
                : NetworkAuthorityMode.ServerOnly;
            return NeoNetworkState.IsAuthorized(gameObject, sender, mode);
        }

        public override void OnStartServer()
        {
            base.OnStartServer();
            NotifyStateChanged();
        }

        public override void OnStartClient()
        {
            // WHY: runtime prefab setup may change the character toggle after this adapter's Awake.
            isNetworked = IsEnabled;
            base.OnStartClient();
        }

        protected override void ApplyNetworkState()
        {
            base.ApplyNetworkState();
            ApplySnapshot(_syncSnapshot);
        }

        private void OnSnapshotSynced(string _, string snapshot)
        {
            if (!NeoNetworkState.IsServer)
            {
                ApplySnapshot(snapshot);
            }
        }

        private void ApplySnapshot(string snapshot)
        {
            if (!IsEnabled)
            {
                return;
            }

            RpgCharacterProfileData profile = _profileService.Deserialize(snapshot);
            if (profile != null)
            {
                _character.ApplyProfile(profile);
            }
        }
#endif

        private void Execute(RpgCharacterNetworkCommand command)
        {
            switch (command.Type)
            {
                case RpgCharacterNetworkCommandType.Damage:
                    _character.DamageType(command.Text, command.Number);
                    break;
                case RpgCharacterNetworkCommandType.Heal:
                    _character.Heal(command.Number);
                    break;
                case RpgCharacterNetworkCommandType.Spend:
                    _character.Spend(command.Text, command.Number);
                    break;
                case RpgCharacterNetworkCommandType.Refill:
                    _character.Refill(command.Text, command.Number);
                    break;
                case RpgCharacterNetworkCommandType.RestoreResource:
                    _character.RestoreResource(command.Text);
                    break;
                case RpgCharacterNetworkCommandType.RestoreAll:
                    _character.Restore();
                    break;
                case RpgCharacterNetworkCommandType.SetMaxResource:
                    _character.SetMaxResource(command.Text, command.Number);
                    break;
                case RpgCharacterNetworkCommandType.AddMaxResource:
                    _character.AddMaxResource(command.Text, command.Number);
                    break;
                case RpgCharacterNetworkCommandType.AddStatBase:
                    _character.AddStatBase(command.Text, command.Number);
                    break;
                case RpgCharacterNetworkCommandType.SetStatBase:
                    _character.SetStatBase(command.Text, command.Number);
                    break;
                case RpgCharacterNetworkCommandType.ApplyBuff:
                    _character.ApplyBuffById(command.Text);
                    break;
                case RpgCharacterNetworkCommandType.ApplyInlineBuff:
                    _character.ApplyInlineBuff(command.Integer);
                    break;
                case RpgCharacterNetworkCommandType.RemoveBuff:
                    _character.RemoveBuff(command.Text);
                    break;
                case RpgCharacterNetworkCommandType.ClearBuffs:
                    _character.ClearAllBuffs();
                    break;
                case RpgCharacterNetworkCommandType.ApplyStatus:
                    _character.ApplyStatusById(command.Text);
                    break;
                case RpgCharacterNetworkCommandType.RemoveStatus:
                    _character.RemoveStatus(command.Text);
                    break;
                case RpgCharacterNetworkCommandType.ClearStatuses:
                    _character.ClearAllStatuses();
                    break;
                case RpgCharacterNetworkCommandType.AddLevel:
                    _character.AddLevel(command.Integer);
                    break;
                case RpgCharacterNetworkCommandType.SetLevel:
                    _character.SetLevel(command.Integer);
                    break;
                case RpgCharacterNetworkCommandType.AddXp:
                    _character.AddXp(command.Number);
                    break;
                case RpgCharacterNetworkCommandType.AddUpgradePoints:
                    _character.AddUpgradePoints(command.Integer);
                    break;
                case RpgCharacterNetworkCommandType.UpgradeStat:
                    _character.UpgradeStat(command.Text);
                    break;
                case RpgCharacterNetworkCommandType.SetInvulnerable:
                    if (command.Flag)
                    {
                        _character.LockInvulnerable();
                    }
                    else
                    {
                        _character.UnlockInvulnerable();
                    }
                    break;
                case RpgCharacterNetworkCommandType.ApplyLevelGrowth:
                    _character.ApplyLevelGrowth();
                    break;
            }
        }
    }
}
