using Neo.Extensions;
using Neo.Reactive;
using Neo.Save;
using Neo.Tools;
using TMPro;
using UnityEngine;

namespace Neo.Shop
{
    [NeoDoc("Shop/Money.md")]
    [CreateFromMenu("Neoxider/Shop/Money")]
    [AddComponentMenu("Neoxider/" + "Shop/" + nameof(Money))]
    public class Money : Singleton<Money>, IMoneySpend, IMoneyAdd
    {
        [Space] [SerializeField] private string _moneySave = "Money";

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
                CurrentMoney.Value = CurrentMoney.CurrentValue - amount;
                LastChangeMoney.Value = -amount;
                ApplyMoneyToText();
                PersistBalanceToSave();
                return true;
            }

            return false;
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
    }
}
