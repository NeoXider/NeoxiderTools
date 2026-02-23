using Neo.Extensions;
using Neo.Reactive;
using Neo.Save;
using Neo.Tools;
using TMPro;
using UnityEngine;
using UnityEngine.Events;

namespace Neo.Shop
{
    [NeoDoc("Shop/Money.md")]
    [CreateFromMenu("Neoxider/Shop/Money")]
    [AddComponentMenu("Neoxider/" + "Shop/" + nameof(Money))]
    public class Money : Singleton<Money>, IMoneySpend, IMoneyAdd
    {
            [Space] [SerializeField] private string _moneySave = "Money";

            public ReactivePropertyFloat CurrentMoney = new();
            public ReactivePropertyFloat LevelMoney = new();
            public ReactivePropertyFloat AllMoney = new();
            public ReactivePropertyFloat LastChangeMoney = new();

            [SerializeField] private SetText[] st_levelMoney;
            [SerializeField] private SetText[] st_money;
            [SerializeField] private TMP_Text[] t_levelMoney;

            [SerializeField] private TMP_Text[] t_money;

            [Space] [Header("Text")] private readonly int _roundToDecimal = 2;
            public float levelMoney => LevelMoney.CurrentValue;
            public float money => CurrentMoney.CurrentValue;
            public float allMoney => AllMoney.CurrentValue;
            /// <summary>Последнее изменение суммы (для NeoCondition и рефлексии).</summary>
            public float LastChangeMoneyValue => LastChangeMoney.CurrentValue;

            private void Start()
            {
                Load();
                SetLevelMoney();
                ApplyMoneyToText();
                ApplyLevelMoneyToText();
            }

            [Button]
            public void Add(float amount)
            {
                CurrentMoney.Value = CurrentMoney.CurrentValue + amount;
                AllMoney.Value = AllMoney.CurrentValue + amount;
                LastChangeMoney.Value = amount;
                Save();
                ApplyMoneyToText();
            }

            [Button]
            public bool Spend(float amount)
            {
                if (CanSpend(amount))
                {
                    CurrentMoney.Value = CurrentMoney.CurrentValue - amount;
                    LastChangeMoney.Value = amount;
                    ApplyMoneyToText();
                    Save();
                    return true;
                }

                return false;
            }

            protected override void Init()
            {
                base.Init();
            }

            private void Load()
            {
                CurrentMoney.SetValueWithoutNotify(SaveProvider.GetFloat(_moneySave, CurrentMoney.CurrentValue));
                AllMoney.SetValueWithoutNotify(SaveProvider.GetFloat(_moneySave + nameof(AllMoney), AllMoney.CurrentValue));
            }

            private void Save()
            {
                SaveProvider.SetFloat(_moneySave, CurrentMoney.CurrentValue);
                SaveProvider.SetFloat(_moneySave + nameof(AllMoney), AllMoney.CurrentValue);
            }

            public void AddLevelMoney(float count)
            {
                LevelMoney.Value = LevelMoney.CurrentValue + count;
                ApplyLevelMoneyToText();
            }

            public float SetLevelMoney(float count = 0)
            {
                float prev = LevelMoney.CurrentValue;
                LevelMoney.Value = count;
                ApplyLevelMoneyToText();
                return prev;
            }

            public float SetMoney(float count = 0)
            {
                LastChangeMoney.Value = count - CurrentMoney.CurrentValue;
                CurrentMoney.Value = count;
                ApplyMoneyToText();
                return CurrentMoney.CurrentValue;
            }

            public float SetMoneyForLevel(bool resetLevelMoney = true)
            {
                float count = LevelMoney.CurrentValue;
                CurrentMoney.Value = CurrentMoney.CurrentValue + LevelMoney.CurrentValue;

                if (resetLevelMoney)
                {
                    LevelMoney.Value = 0f;
                    ApplyLevelMoneyToText();
                }

                ApplyMoneyToText();
                Save();
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
                            item.Set(v);
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
                            item.Set(v);
                    }
                }
            }

            private void SetText(TMP_Text[] text, float count)
            {
                if (text == null)
                    return;
                string s = count.RoundToDecimal(_roundToDecimal).ToString();
                foreach (TMP_Text item in text)
                {
                    if (item != null)
                        item.text = s;
                }
            }
        }
}