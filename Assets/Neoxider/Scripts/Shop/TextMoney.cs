using Neo.Reactive;
using Neo.Tools;
using UnityEngine;

namespace Neo.Shop
{
    /// <summary>
    ///     <see cref="SetText"/> that mirrors one <see cref="Money"/> balance (current, level or all-time).
    ///     Resolves the wallet by save key, explicit source, then <see cref="Money.I"/>, and binds as soon as a
    ///     wallet registers when none exists yet, without polling the scene every frame.
    /// </summary>
    [NeoDoc("Shop/TextMoney.md")]
    public class TextMoney : SetText
    {
        [SerializeField] private MoneyDisplayMode _displayMode = MoneyDisplayMode.Money;

        [SerializeField]
        [Tooltip(
            "Value source. If not set, Money.I (global singleton) is used. Set for energy or another separate counter display.")]
        private Money _moneySource;

        [SerializeField] [Tooltip("Optional Money.SaveKey to display. Empty = use Money Source, then Money.I.")]
        private string _moneySaveKey = "";

        public float amount;
        private Money _money;
        private ReactivePropertyFloat _subscribedProperty;
        private bool _waitingForWallet;

        public TextMoney()
        {
            @decimal = 0;
        }

        /// <summary>Wallet currently displayed; null while waiting for one to register.</summary>
        public Money BoundMoney => _subscribedProperty != null ? _money : null;

        private void Start()
        {
            if (GetMoney() == null)
            {
                WaitForWallet();
                return;
            }

            Init();
        }

        protected override void OnEnable()
        {
            base.OnEnable();
            if (GetMoney() != null)
            {
                Init();
            }
            else
            {
                WaitForWallet();
            }
        }

        protected override void OnDisable()
        {
            try
            {
                StopWaitingForWallet();
                Unsubscribe();
            }
            finally
            {
                base.OnDisable();
            }
        }

        public void SetMoneySource(Money money)
        {
            if (_moneySource == money)
            {
                return;
            }

            Unsubscribe();
            _moneySource = money;
            Init();
        }

        public void SetMoneySaveKey(string saveKey)
        {
            string next = saveKey ?? "";
            if (_moneySaveKey == next)
            {
                return;
            }

            Unsubscribe();
            _moneySaveKey = next;
            Init();
        }

        private void Unsubscribe()
        {
            // WHY: remove from the property actually subscribed; the display mode or wallet may have changed
            // (or the wallet been destroyed) since Init.
            if (_subscribedProperty == null)
            {
                return;
            }

            _subscribedProperty.OnChanged.RemoveListener(SetAmount);
            _subscribedProperty = null;
        }

        private Money GetMoney()
        {
            if (!string.IsNullOrEmpty(_moneySaveKey))
            {
                Money keyedMoney = Money.FindBySaveKey(_moneySaveKey);
                if (keyedMoney != null)
                {
                    return keyedMoney;
                }
            }

            return _moneySource != null ? _moneySource : Money.I;
        }

        private void Init()
        {
            Unsubscribe();
            _money = GetMoney();
            if (_money == null || !isActiveAndEnabled)
            {
                if (isActiveAndEnabled)
                {
                    WaitForWallet();
                }

                return;
            }

            StopWaitingForWallet();
            ReactivePropertyFloat property = _displayMode switch
            {
                MoneyDisplayMode.LevelMoney => _money.LevelMoney,
                MoneyDisplayMode.AllMoney => _money.AllMoney,
                _ => _money.CurrentMoney
            };

            SetAmount(property.CurrentValue);
            property.OnChanged.AddListener(SetAmount);
            _subscribedProperty = property;
        }

        private void WaitForWallet()
        {
            if (_waitingForWallet || !isActiveAndEnabled)
            {
                return;
            }

            _waitingForWallet = true;
            Money.WalletRegistered += HandleWalletRegistered;
        }

        private void StopWaitingForWallet()
        {
            if (!_waitingForWallet)
            {
                return;
            }

            _waitingForWallet = false;
            Money.WalletRegistered -= HandleWalletRegistered;
        }

        private void HandleWalletRegistered(Money wallet)
        {
            if (this == null)
            {
                Money.WalletRegistered -= HandleWalletRegistered;
                return;
            }

            if (!isActiveAndEnabled || GetMoney() == null)
            {
                return;
            }

            Init();
        }

        private void SetAmount(float count)
        {
            amount = count;
            Set(amount);
        }

        private enum MoneyDisplayMode
        {
            Money = 0,
            LevelMoney = 1,
            AllMoney = 2
        }
    }
}
