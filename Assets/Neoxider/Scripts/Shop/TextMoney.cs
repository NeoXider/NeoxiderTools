using Neo.Extensions;
using Neo.Tools;
using UnityEngine;

namespace Neo.Shop
{
    public class TextMoney : SetText
    {
        [SerializeField] private MoneyDisplayMode _displayMode = MoneyDisplayMode.Money;

        [SerializeField]
        [Tooltip(
            "Value source. If not set, Money.I (global singleton) is used. Set for energy or another separate counter display.")]
        private Money _moneySource;

        [SerializeField]
        [Tooltip("Optional Money.SaveKey to display. Empty = use Money Source, then Money.I.")]
        private string _moneySaveKey = "";

        public float amount;
        private Money _money;
        private bool _subscribed;

        public TextMoney()
        {
            @decimal = 0;
        }

        private void Start()
        {
            if (GetMoney() == null)
            {
                this.WaitWhile(() => GetMoney() == null, Init);
                return;
            }

            Init();
        }

        private void OnEnable()
        {
            if (GetMoney() != null)
            {
                Init();
            }
        }

        private void OnDisable()
        {
            Unsubscribe();
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
            if (_money == null || !_subscribed)
            {
                return;
            }

            switch (_displayMode)
            {
                case MoneyDisplayMode.LevelMoney:
                    _money.LevelMoney.OnChanged.RemoveListener(SetAmount);
                    break;
                case MoneyDisplayMode.AllMoney:
                    _money.AllMoney.OnChanged.RemoveListener(SetAmount);
                    break;
                default:
                    _money.CurrentMoney.OnChanged.RemoveListener(SetAmount);
                    break;
            }

            _subscribed = false;
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
            if (_money == null)
            {
                return;
            }

            switch (_displayMode)
            {
                case MoneyDisplayMode.LevelMoney:
                    SetAmount(_money.levelMoney);
                    _money.LevelMoney.OnChanged.AddListener(SetAmount);
                    _subscribed = true;
                    break;
                case MoneyDisplayMode.AllMoney:
                    SetAmount(_money.allMoney);
                    _money.AllMoney.OnChanged.AddListener(SetAmount);
                    _subscribed = true;
                    break;
                default:
                    SetAmount(_money.money);
                    _money.CurrentMoney.OnChanged.AddListener(SetAmount);
                    _subscribed = true;
                    break;
            }
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
