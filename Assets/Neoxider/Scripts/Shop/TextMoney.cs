using Neo.Extensions;
using Neo.Tools;
using UnityEngine;

namespace Neo.Shop
{
    public class TextMoney : SetText
    {
        [SerializeField] private MoneyDisplayMode _displayMode = MoneyDisplayMode.Money;
        public float amount;
        private Money _money;

        public TextMoney()
        {
            @decimal = 0;
        }

        private void OnEnable()
        {
            if (Money.I == null)
            {
                this.WaitWhile(() => Money.I == null, Init);
                return;
            }

            Init();
        }

        private void Init()
        {
            _money = Money.I;
            if (_money == null)
                return;

            switch (_displayMode)
            {
                case MoneyDisplayMode.LevelMoney:
                    _money.LevelMoney.OnChanged.RemoveListener(SetAmount);
                    SetAmount(_money.levelMoney);
                    _money.LevelMoney.OnChanged.AddListener(SetAmount);
                    break;
                case MoneyDisplayMode.AllMoney:
                    _money.AllMoney.OnChanged.RemoveListener(SetAmount);
                    SetAmount(_money.allMoney);
                    _money.AllMoney.OnChanged.AddListener(SetAmount);
                    break;
                default:
                    _money.CurrentMoney.OnChanged.RemoveListener(SetAmount);
                    SetAmount(_money.money);
                    _money.CurrentMoney.OnChanged.AddListener(SetAmount);
                    break;
            }
        }

        private void OnDisable()
        {
            if (_money == null)
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
        }

        private void SetAmount(float count)
        {
            amount = count;
            Set(amount.RoundToDecimal(@decimal).ToString());
        }

        private enum MoneyDisplayMode
        {
            Money = 0,
            LevelMoney = 1,
            AllMoney = 2
        }
    }
}