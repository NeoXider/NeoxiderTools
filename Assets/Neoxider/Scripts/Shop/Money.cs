using TMPro;
using UnityEngine;
using UnityEngine.Events;

namespace Neoxider
{
    namespace Shop
    {
        [AddComponentMenu("Neoxider/" + "Shop/" + nameof(Money))]
        public class Money : MonoBehaviour, IMoneySpend, IMoneyAdd
        {
            public static Money Instance;

            public int levelMoney => _levelMoney;
            public int money => _money;
            public int allMoney => _allMoney;

            [SerializeField, Color(ColorEnum.SoftGreen)] private int _money;
            [SerializeField, Color(ColorEnum.SoftBlue)] private int _levelMoney;

            [SerializeField] private string _moneySave = "Money";

            [Space, Header("Text")]
            [SerializeField] private TMP_Text[] t_money;
            [SerializeField] private TMP_Text[] t_levelMoney;

            [Space, Header("Events")]
            public UnityEvent<int> OnChangedLevelMoney;
            public UnityEvent<int> OnChangedMoney;

            private int _allMoney;

            private void Awake()
            {
                Instance = this;
            }

            void Start()
            {
                Load();
                SetLevelMoney(0);
                ChangeMoneyEvent();
            }

            private void Load()
            {
                _money = PlayerPrefs.GetInt(_moneySave, _money);
                _allMoney = PlayerPrefs.GetInt(_moneySave + nameof(_allMoney), 0);
            }

            private void Save()
            {
                PlayerPrefs.SetInt(_moneySave, _money);
                PlayerPrefs.SetInt(_moneySave + nameof(_allMoney), _allMoney);
            }

            public void AddLevelMoney(int count)
            {
                _levelMoney += count;
                ChangeLevelMoneyEvent();
            }

            public int SetLevelMoney(int count = 0)
            {
                int levelMoney = _levelMoney;
                _levelMoney = count;
                ChangeLevelMoneyEvent();
                return levelMoney;
            }

            public int SetMoneyForLevel(bool resetLevelMoney = true)
            {
                int count = _levelMoney;
                _money += _levelMoney;

                if (resetLevelMoney)
                {
                    SetLevelMoney(0);
                }

                ChangeMoneyEvent();
                Save();
                return count;
            }

            public bool CheckSpend(int count)
            {
                return _money >= count;
            }

            public bool Spend(int count)
            {
                if (CheckSpend(count))
                {
                    _money -= count;
                    ChangeMoneyEvent();
                    Save();
                    return true;
                }

                return false;
            }

            public void Add(int count)
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

            private void SetText(TMP_Text[] text, int count)
            {
                foreach (var item in text)
                {
                    item.text = count.ToString();
                }
            }
        }
    }
}