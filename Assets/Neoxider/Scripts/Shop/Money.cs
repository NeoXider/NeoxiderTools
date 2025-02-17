using Neo.Tools;
using TMPro;
using UnityEngine;
using UnityEngine.Events;

namespace Neo
{
    namespace Shop
    {
        [AddComponentMenu("Neoxider/" + "Shop/" + nameof(Money))]
        public class Money : Singleton<Money>, IMoneySpend, IMoneyAdd
        {
            public float levelMoney => _levelMoney;
            public float money => _money;
            public float allMoney => _allMoney;

            [SerializeField, Color(ColorEnum.SoftGreen)] private float _money;
            [SerializeField, Color(ColorEnum.SoftBlue)] private float _levelMoney;

            [SerializeField] private string _moneySave = "Money";

            [Space, Header("Text")]
            [SerializeField] private int _roundToDecimal = 2;
            [SerializeField] private TMP_Text[] t_money;
            [SerializeField] private TMP_Text[] t_levelMoney;

            [Space, Header("Events")]
            public UnityEvent<float> OnChangedLevelMoney;
            public UnityEvent<float> OnChangedMoney;

            private float _allMoney;

            protected override void Init()
            {
                base.Init();

            }

            void Start()
            {
                Load();
                SetLevelMoney(0);
                ChangeMoneyEvent();
            }

            private void Load()
            {
                _money = PlayerPrefs.GetFloat(_moneySave, _money);
                _allMoney = PlayerPrefs.GetFloat(_moneySave + nameof(_allMoney), 0);
            }

            private void Save()
            {
                PlayerPrefs.SetFloat(_moneySave, _money);
                PlayerPrefs.SetFloat(_moneySave + nameof(_allMoney), _allMoney);
            }

            public void AddLevelMoney(float count)
            {
                _levelMoney += count;
                ChangeLevelMoneyEvent();
            }

            public float SetLevelMoney(float count = 0)
            {
                float levelMoney = _levelMoney;
                _levelMoney = count;
                ChangeLevelMoneyEvent();
                return levelMoney;
            }

            public float SetMoneyForLevel(bool resetLevelMoney = true)
            {
                float count = _levelMoney;
                _money += _levelMoney;

                if (resetLevelMoney)
                {
                    SetLevelMoney(0);
                }

                ChangeMoneyEvent();
                Save();
                return count;
            }

            public bool CanSpend(float count)
            {
                return _money >= count;
            }

            public bool Spend(float count)
            {
                if (CanSpend(count))
                {
                    _money -= count;
                    ChangeMoneyEvent();
                    Save();
                    return true;
                }

                return false;
            }

            public void Add(float count)
            {
                _money += count;
                _allMoney += count;
                Save();
                ChangeMoneyEvent();
            }

            private void ChangeMoneyEvent()
            {
                SetText(t_money, _money);

                OnChangedMoney?.Invoke(_money);
            }

            private void ChangeLevelMoneyEvent()
            {
                SetText(t_levelMoney, _levelMoney);

                OnChangedLevelMoney?.Invoke(_levelMoney);
            }

            private void SetText(TMP_Text[] text, float count)
            {
                foreach (var item in text)
                {
                    item.text = count.RoundToDecimal(_roundToDecimal).ToString();
                }
            }
        }
    }
}