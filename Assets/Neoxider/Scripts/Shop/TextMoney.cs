using Neo.Extensions;
using Neo.Tools;
using UnityEngine;

namespace Neo.Shop
{
    public class TextMoney : SetText
    {
        [SerializeField] private readonly bool _levelMoney = false;
        private Money _money;

        public float amount;

        public TextMoney()
        {
            @decimal = 0;
        }

        private void OnEnable()
        {
            GetMoney();

            if (_levelMoney)
            {
                SetAmount(_money.levelMoney);
                _money.OnChangedLevelMoney.AddListener(SetAmount);
            }
            else
            {
                SetAmount(_money.money);
                _money.OnChangedMoney.AddListener(SetAmount);
            }
        }

        private void GetMoney()
        {
            if (_money == null)
                _money = Money.I;
        }

        private void SetAmount(float count)
        {
            amount = count;
            Set(amount.RoundToDecimal(@decimal).ToString());
        }

        private void OnDisable()
        {
            if (_levelMoney)
                _money.OnChangedLevelMoney.RemoveListener(SetAmount);
            else
                _money.OnChangedMoney.RemoveListener(SetAmount);
        }
    }
}