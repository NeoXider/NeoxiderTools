using Neo.Extensions;
using Neo.Tools;
using TMPro;
using UnityEngine;

namespace Neo.Shop
{
    public class TextMoney : SetText
    {
        [SerializeField] private bool _levelMoney = false;

        public float amount;
        private Money _money;

        public TextMoney()
        {
            _decimal = 0;
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
            Set(amount.RoundToDecimal(_decimal).ToString());
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