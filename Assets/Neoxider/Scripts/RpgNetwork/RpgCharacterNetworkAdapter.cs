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
    [NeoDoc("Rpg/RpgCharacterNetworkAdapter.md")]
    [AddComponentMenu("Neoxider/RPG/Network/Rpg Character Network Adapter")]
    [RequireComponent(typeof(RpgCharacter))]
#if MIRROR
    [RequireComponent(typeof(NetworkIdentity))]
#endif
    public sealed class RpgCharacterNetworkAdapter : NeoNetworkComponent, IRpgCharacterNetworkAdapter
    {
        private readonly RpgCharacterProfileService _profileService = new();
        private RpgCharacter _character;

#if MIRROR
        [SyncVar(hook = nameof(OnSnapshotSynced))]
        private string _syncSnapshot = string.Empty;
#endif

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

        public void NotifyStateChanged()
        {
#if MIRROR
            if (!IsEnabled || !NeoNetworkState.IsServer)
            {
                return;
            }

            _syncSnapshot = _profileService.Serialize(_character.CaptureProfile());
#endif
        }

        public void NetDamage(float amount)
        {
            RouteOrExecute(new RpgCharacterNetworkCommand(RpgCharacterNetworkCommandType.Damage, number: amount));
        }

        public void NetDamageType(string damageType, float amount)
        {
            RouteOrExecute(new RpgCharacterNetworkCommand(RpgCharacterNetworkCommandType.Damage, damageType, amount));
        }

        public void NetHeal(float amount)
        {
            RouteOrExecute(new RpgCharacterNetworkCommand(RpgCharacterNetworkCommandType.Heal, number: amount));
        }

        public void NetSpend(string resourceId, float amount)
        {
            RouteOrExecute(new RpgCharacterNetworkCommand(RpgCharacterNetworkCommandType.Spend, resourceId, amount));
        }

        public void NetRefill(string resourceId, float amount)
        {
            RouteOrExecute(new RpgCharacterNetworkCommand(RpgCharacterNetworkCommandType.Refill, resourceId, amount));
        }

        public void NetApplyBuffById(string id)
        {
            RouteOrExecute(new RpgCharacterNetworkCommand(RpgCharacterNetworkCommandType.ApplyBuff, id));
        }

        public void NetApplyInlineBuff(int index)
        {
            RouteOrExecute(new RpgCharacterNetworkCommand(RpgCharacterNetworkCommandType.ApplyInlineBuff,
                integer: index));
        }

        public void NetApplyStatusById(string id)
        {
            RouteOrExecute(new RpgCharacterNetworkCommand(RpgCharacterNetworkCommandType.ApplyStatus, id));
        }

        public void NetAddLevel(int delta)
        {
            RouteOrExecute(new RpgCharacterNetworkCommand(RpgCharacterNetworkCommandType.AddLevel, integer: delta));
        }

        private void RouteOrExecute(RpgCharacterNetworkCommand command)
        {
            if (!TryRoute(command))
            {
                Execute(command);
                NotifyStateChanged();
            }
        }

#if MIRROR
        [Command(requiresAuthority = false)]
        private void CmdExecute(int type, string text, float number, int integer, bool flag,
            NetworkConnectionToClient sender = null)
        {
            if (RateLimitCheck(sender) || !IsAuthorized(sender))
            {
                return;
            }

            Execute(new RpgCharacterNetworkCommand((RpgCharacterNetworkCommandType)type, text, number, integer,
                flag));
            NotifyStateChanged();
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
            }
        }
    }
}
