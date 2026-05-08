using Neo.Extensions;
using Neo.Reactive;
using Neo.Save;
using Neo.Tools;
using TMPro;
using UnityEngine;

#if MIRROR
using Mirror;
using Neo.Network;
#endif

namespace Neo.Shop
{
    [NeoDoc("Shop/Money.md")]
    [CreateFromMenu("Neoxider/Shop/Money")]
    [AddComponentMenu("Neoxider/" + "Shop/" + nameof(Money))]
#if MIRROR
    [RequireComponent(typeof(NetworkIdentity))]
    public class Money : NetworkSingleton<Money>, IMoneySpend, IMoneyAdd
#else
    public class Money : Singleton<Money>, IMoneySpend, IMoneyAdd
#endif
    {
        [Header("Networking")]
        [Tooltip("If true, money is shared globally across the network. If false, each player has their own local wallet.")]
        public bool isNetworked = false;

#if MIRROR
        [SyncVar] private float _syncCurrentMoney;
        private float _lastCmdTime;
        private const float CmdRateLimit = 0.05f;
#endif

        [Space] [SerializeField] private string _moneySave = "Money";
        public string SaveKey => _moneySave;

        [Tooltip("When off, balance is not loaded from or written to SaveProvider (session-only; demos / arenas / NoCode).")]
        [SerializeField]
        private bool _persistMoney = true;

        public ReactivePropertyFloat CurrentMoney = new();
        public ReactivePropertyFloat LevelMoney = new();
        public ReactivePropertyFloat AllMoney = new();
        public ReactivePropertyFloat LastChangeMoney = new();

        [SerializeField] private SetText[] st_levelMoney;
        [SerializeField] private SetText[] st_money;
        [SerializeField] private TMP_Text[] t_levelMoney;

        [SerializeField] private TMP_Text[] t_money;

        private readonly int _roundToDecimal = 2;
        public float levelMoney => LevelMoney.CurrentValue;
        public float money => CurrentMoney.CurrentValue;
        public float allMoney => AllMoney.CurrentValue;

        /// <summary>Last amount change (for NeoCondition and reflection).</summary>
        public float LastChangeMoneyValue => LastChangeMoney.CurrentValue;

        private void Start()
        {
            if (_persistMoney)
            {
                LoadFromSave();
            }

            SetLevelMoney();
            ApplyMoneyToText();
            ApplyLevelMoneyToText();
        }

        [Button]
        public void Add(float amount)
        {
#if MIRROR
            if (isNetworked && NeoNetworkState.IsClient && !NeoNetworkState.IsServer)
            {
                CmdMoneyOp(MoneyOp.Add, amount);
                return;
            }
#endif
            AddLocal(amount);
#if MIRROR
            if (isNetworked && NeoNetworkState.IsServer) { SyncBalance(); RpcMoneyOp(MoneyOp.Add, amount); }
#endif
        }

        private void AddLocal(float amount)
        {
            CurrentMoney.Value = CurrentMoney.CurrentValue + amount;
            AllMoney.Value = AllMoney.CurrentValue + amount;
            LastChangeMoney.Value = amount;
            PersistBalanceToSave();
            ApplyMoneyToText();
        }

        [Button]
        public bool Spend(float amount)
        {
            if (CanSpend(amount))
            {
#if MIRROR
                if (isNetworked && NeoNetworkState.IsClient && !NeoNetworkState.IsServer)
                {
                    CmdMoneyOp(MoneyOp.Spend, amount);
                    return true;
                }
#endif
                SpendLocal(amount);
#if MIRROR
                if (isNetworked && NeoNetworkState.IsServer) { SyncBalance(); RpcMoneyOp(MoneyOp.Spend, amount); }
#endif
                return true;
            }

            return false;
        }

        private void SpendLocal(float amount)
        {
            CurrentMoney.Value = CurrentMoney.CurrentValue - amount;
            LastChangeMoney.Value = -amount;
            ApplyMoneyToText();
            PersistBalanceToSave();
        }

        protected override void Init()
        {
            base.Init();
        }

        private void LoadFromSave()
        {
            CurrentMoney.SetValueWithoutNotify(SaveProvider.GetFloat(_moneySave, CurrentMoney.CurrentValue));
            AllMoney.SetValueWithoutNotify(SaveProvider.GetFloat(_moneySave + nameof(AllMoney), AllMoney.CurrentValue));
        }

        private void PersistBalanceToSave()
        {
            if (!_persistMoney)
            {
                return;
            }

            SaveProvider.SetFloat(_moneySave, CurrentMoney.CurrentValue);
            SaveProvider.SetFloat(_moneySave + nameof(AllMoney), AllMoney.CurrentValue);
        }

        /// <summary>
        ///     Reloads current and all-time balance from <see cref="SaveProvider"/> when persistence is enabled.
        ///     Use after external changes to save keys, or from UnityEvent / NoCode flows.
        /// </summary>
        public void ReloadBalanceFromSave()
        {
            if (!_persistMoney)
            {
                return;
            }

            LoadFromSave();
            ApplyMoneyToText();
        }

        /// <summary>
        ///     Removes persisted money keys, resets runtime balance to zero, then re-persists zeros when persistence is on.
        ///     Wire from a reset button or UnityEvent for NoCode.
        /// </summary>
        public void ClearSavedMoneyAndReset()
        {
            SaveProvider.DeleteKey(_moneySave);
            SaveProvider.DeleteKey(_moneySave + nameof(AllMoney));
            LastChangeMoney.Value = 0f;
            CurrentMoney.Value = 0f;
            AllMoney.Value = 0f;
            ApplyMoneyToText();
            PersistBalanceToSave();
        }

        public void AddLevelMoney(float count)
        {
#if MIRROR
            if (isNetworked && NeoNetworkState.IsClient && !NeoNetworkState.IsServer)
            {
                CmdMoneyOp(MoneyOp.AddLevelMoney, count);
                return;
            }
#endif
            AddLevelMoneyLocal(count);
#if MIRROR
            if (isNetworked && NeoNetworkState.IsServer) { SyncBalance(); RpcMoneyOp(MoneyOp.AddLevelMoney, count); }
#endif
        }

        private void AddLevelMoneyLocal(float count)
        {
            LevelMoney.Value = LevelMoney.CurrentValue + count;
            ApplyLevelMoneyToText();
        }

        public float SetLevelMoney(float count = 0)
        {
#if MIRROR
            if (isNetworked && NeoNetworkState.IsClient && !NeoNetworkState.IsServer)
            {
                CmdMoneyOp(MoneyOp.SetLevelMoney, count);
                return count;
            }
#endif
            float res = SetLevelMoneyLocal(count);
#if MIRROR
            if (isNetworked && NeoNetworkState.IsServer) { SyncBalance(); RpcMoneyOp(MoneyOp.SetLevelMoney, count); }
#endif
            return res;
        }

        private float SetLevelMoneyLocal(float count)
        {
            float prev = LevelMoney.CurrentValue;
            LevelMoney.Value = count;
            ApplyLevelMoneyToText();
            return prev;
        }

        public float SetMoney(float count = 0)
        {
#if MIRROR
            if (isNetworked && NeoNetworkState.IsClient && !NeoNetworkState.IsServer)
            {
                CmdMoneyOp(MoneyOp.SetMoney, count);
                return count;
            }
#endif
            float res = SetMoneyLocal(count);
#if MIRROR
            if (isNetworked && NeoNetworkState.IsServer) { SyncBalance(); RpcMoneyOp(MoneyOp.SetMoney, count); }
#endif
            return res;
        }

        private float SetMoneyLocal(float count)
        {
            LastChangeMoney.Value = count - CurrentMoney.CurrentValue;
            CurrentMoney.Value = count;
            ApplyMoneyToText();
            PersistBalanceToSave();
            return CurrentMoney.CurrentValue;
        }

        /// <summary>
        ///     Same as <see cref="SetMoney"/> but explicit name for UnityEvent / Inspector wiring (NoCode).
        /// </summary>
        public void SetCurrentMoney(float amount)
        {
            SetMoney(amount);
        }

        public float SetMoneyForLevel(bool resetLevelMoney = true)
        {
#if MIRROR
            if (isNetworked && NeoNetworkState.IsClient && !NeoNetworkState.IsServer)
            {
                CmdMoneyOp(MoneyOp.SetMoneyForLevel, resetLevelMoney ? 1f : 0f);
                return LevelMoney.CurrentValue;
            }
#endif
            float res = SetMoneyForLevelLocal(resetLevelMoney);
#if MIRROR
            if (isNetworked && NeoNetworkState.IsServer) { SyncBalance(); RpcMoneyOp(MoneyOp.SetMoneyForLevel, resetLevelMoney ? 1f : 0f); }
#endif
            return res;
        }

        private float SetMoneyForLevelLocal(bool resetLevelMoney)
        {
            float count = LevelMoney.CurrentValue;
            CurrentMoney.Value = CurrentMoney.CurrentValue + LevelMoney.CurrentValue;

            if (resetLevelMoney)
            {
                LevelMoney.Value = 0f;
                ApplyLevelMoneyToText();
            }

            ApplyMoneyToText();
            PersistBalanceToSave();
            return count;
        }

        public bool CanSpend(float count)
        {
            return CurrentMoney.CurrentValue >= count;
        }

        private void ApplyMoneyToText()
        {
            float v = CurrentMoney.CurrentValue;
            SetText(t_money, v);
            if (st_money != null)
            {
                foreach (SetText item in st_money)
                {
                    if (item != null)
                    {
                        item.Set(v);
                    }
                }
            }
        }

        private void ApplyLevelMoneyToText()
        {
            float v = LevelMoney.CurrentValue;
            SetText(t_levelMoney, v);
            if (st_levelMoney != null)
            {
                foreach (SetText item in st_levelMoney)
                {
                    if (item != null)
                    {
                        item.Set(v);
                    }
                }
            }
        }

        private void SetText(TMP_Text[] text, float count)
        {
            if (text == null)
            {
                return;
            }

            string s = count.RoundToDecimal(_roundToDecimal).ToString();
            foreach (TMP_Text item in text)
            {
                if (item != null)
                {
                    item.text = s;
                }
            }
        }

#if MIRROR
        /// <summary>Operation type for the unified network dispatch.</summary>
        private enum MoneyOp : byte
        {
            Add = 0,
            Spend = 1,
            AddLevelMoney = 2,
            SetLevelMoney = 3,
            SetMoney = 4,
            SetMoneyForLevel = 5
        }

        private bool RateLimit()
        {
            if (Time.time - _lastCmdTime < CmdRateLimit) return true;
            _lastCmdTime = Time.time;
            return false;
        }

        private void SyncBalance() { _syncCurrentMoney = CurrentMoney.CurrentValue; }

        /// <summary>Applies the operation locally without network dispatch.</summary>
        private void ExecuteOp(MoneyOp op, float amount)
        {
            switch (op)
            {
                case MoneyOp.Add:            AddLocal(amount); break;
                case MoneyOp.Spend:          SpendLocal(amount); break;
                case MoneyOp.AddLevelMoney:  AddLevelMoneyLocal(amount); break;
                case MoneyOp.SetLevelMoney:  SetLevelMoneyLocal(amount); break;
                case MoneyOp.SetMoney:       SetMoneyLocal(amount); break;
                case MoneyOp.SetMoneyForLevel: SetMoneyForLevelLocal(amount != 0f); break;
            }
        }

        /// <summary>Single unified Command — replaces 6 individual CmdXxx methods.</summary>
        [Command(requiresAuthority = false)]
        private void CmdMoneyOp(MoneyOp op, float amount, NetworkConnectionToClient sender = null)
        {
            if (RateLimit()) return;
            // Server-side validation
            if (op == MoneyOp.Spend && !CanSpend(amount)) return;

            ExecuteOp(op, amount);
            SyncBalance();
            RpcMoneyOp(op, amount);
        }

        /// <summary>Single unified ClientRpc — replaces 6 individual RpcXxx methods.</summary>
        [ClientRpc(includeOwner = true)]
        private void RpcMoneyOp(MoneyOp op, float amount)
        {
            if (isServer) return;
            ExecuteOp(op, amount);
        }

        /// <summary>Late-join: apply server-authoritative balance to newly connected client.</summary>
        public override void OnStartClient()
        {
            base.OnStartClient();
            if (isNetworked && !isServer)
            {
                CurrentMoney.Value = _syncCurrentMoney;
                ApplyMoneyToText();
            }
        }
#endif
    }
}
