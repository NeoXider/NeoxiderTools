using Neo.Extensions;
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
            [Space] [SerializeField] private string _moneySave = "Money";

            [Space] [Header("Text")] [SerializeField]
            private readonly int _roundToDecimal = 2;

            [SerializeField] private float _allMoney;
            [SerializeField] private float _lastChangeMoney;

            [SerializeField] [GUIColor(0,1,1)]
            private float _levelMoney;

            [SerializeField] [GUIColor(0,1,0)]
            private float _money;

            public UnityEvent<float> OnChangeAllMoney;

            [Space] [Header("Events")] public UnityEvent<float> OnChangedLevelMoney;
            public UnityEvent<float> OnChangedMoney;
            public UnityEvent<float> OnChangeLastMoney;
            [SerializeField] private SetText[] st_levelMoney;
            [SerializeField] private SetText[] st_money;
            [SerializeField] private TMP_Text[] t_levelMoney;

            [SerializeField] private TMP_Text[] t_money;
            public float levelMoney => _levelMoney;
            public float money => _money;
            public float allMoney => _allMoney;

            public float LastChangeMoney => _lastChangeMoney;

#if ODIN_INSPECTOR
            [Sirenix.OdinInspector.Button]
#else
            [Button]
#endif
            public void Add(float amount)
            {
                _money += amount;
                _allMoney += amount;
                _lastChangeMoney = amount;
                Save();
                ChangeMoneyEvent();
            }

#if ODIN_INSPECTOR
            [Sirenix.OdinInspector.Button]
#else
            [Button]
#endif
            public bool Spend(float amount)
            {
                if (CanSpend(amount))
                {
                    _money -= amount;
                    _lastChangeMoney = amount;
                    ChangeMoneyEvent();
                    Save();
                    return true;
                }

                return false;
            }


            protected override void Init()
            {
                base.Init();
            }

            private void Start()
            {
                Load();
                SetLevelMoney();
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
            
            public float SetMoney(float count = 0)
            {
                _lastChangeMoney = count - _money;
                _money = count;
                ChangeMoneyEvent();
                return _money;
            }

            public float SetMoneyForLevel(bool resetLevelMoney = true)
            {
                float count = _levelMoney;
                _money += _levelMoney;

                if (resetLevelMoney)
                {
                    SetLevelMoney();
                }

                ChangeMoneyEvent();
                Save();
                return count;
            }

            public bool CanSpend(float count)
            {
                return _money >= count;
            }

            private void ChangeMoneyEvent()
            {
                SetText(t_money, _money);

                foreach (SetText item in st_money)
                {
                    item.Set(_money);
                }

                OnChangedMoney?.Invoke(_money);
                OnChangeLastMoney?.Invoke(_lastChangeMoney);
                OnChangeAllMoney?.Invoke(_allMoney);
            }

            private void ChangeLevelMoneyEvent()
            {
                SetText(t_levelMoney, _levelMoney);

                foreach (SetText item in st_levelMoney)
                {
                    item.Set(_levelMoney);
                }

                OnChangedLevelMoney?.Invoke(_levelMoney);
            }

            private void SetText(TMP_Text[] text, float count)
            {
                foreach (TMP_Text item in text)
                {
                    item.text = count.RoundToDecimal(_roundToDecimal).ToString();
                }
            }
        }
    }
}