using System;
using System.Collections.Generic;
using System.Linq;
using Neo;
using Neo.Runtime.Core;
using Neo.Runtime.Features.Money.Model;
using Neo.Runtime.Features.Wallet.Data;
using Neo.Runtime.Features.Wallet.Domain;
using R3;
using UnityEngine;
using UnityEngine.Events;

namespace Neo.Runtime.Controllers
{
    /// <summary>
    /// Универсальный контроллер для управления Wallet через инспектор Unity.
    /// Автоматически создаёт список валют из WalletConfig и позволяет управлять ими через кнопки и события.
    /// </summary>
    [AddComponentMenu("Neoxider/Runtime/Wallet Controller")]
    public class WalletController : MonoBehaviour
    {
        [Header("Configuration")]
        [Tooltip("Конфигурация валют для автозаполнения списка")]
        [SerializeField] private WalletConfig walletConfig;

        [Tooltip("Автоматически синхронизировать список из конфига в OnValidate")]
        [SerializeField] private bool autoSyncFromConfig = true;

        [Header("Currencies")]
        [Tooltip("Список валют для управления. Автозаполняется из WalletConfig")]
        [SerializeField] private List<CurrencyControllerItem> currencies = new();

        /// <summary>
        /// Список валют (для доступа из Editor)
        /// </summary>
        public List<CurrencyControllerItem> Currencies => currencies;

        [Header("Settings")]
        [Tooltip("Валюта по умолчанию для методов без параметра")]
        [SerializeField] private string defaultCurrencyId = "money";

        private WalletModel _walletModel;

        private void Awake()
        {
            _walletModel = GameManager.Wallet;
        }

        private void Start()
        {
            InitializeAllItems();
        }

        private void OnDestroy()
        {
            DisposeAllItems();
        }

        private void OnValidate()
        {
            if (walletConfig != null && autoSyncFromConfig)
            {
                SyncCurrenciesFromConfig();
            }
        }

        private void SyncCurrenciesFromConfig()
        {
            if (walletConfig == null || walletConfig.Currencies == null)
                return;

            var configCurrencies = walletConfig.Currencies.ToList();

            // Обновляем существующие и добавляем новые
            foreach (var def in configCurrencies)
            {
                if (def == null || string.IsNullOrEmpty(def.CurrencyId))
                    continue;

                var existingItem = currencies.FirstOrDefault(c => c != null && c.CurrencyId == def.CurrencyId);
                
                if (existingItem == null)
                {
                    // Создаём новый элемент
                    var newItem = new CurrencyControllerItem
                    {
                        CurrencyId = def.CurrencyId,
                        DefaultAmount = def.StartAmount > 0 ? def.StartAmount : 100f
                    };
                    currencies.Add(newItem);
                }
                else
                {
                    // Обновляем defaultAmount из конфига (опционально)
                    if (def.StartAmount > 0)
                    {
                        existingItem.DefaultAmount = def.StartAmount;
                    }
                }
            }

            // Удаляем элементы, которых нет в конфиге (только если autoSyncFromConfig == true)
            if (autoSyncFromConfig)
            {
                currencies.RemoveAll(c => 
                    c == null || 
                    string.IsNullOrEmpty(c.CurrencyId) || 
                    !configCurrencies.Any(def => def != null && def.CurrencyId == c.CurrencyId));
            }
        }

        private void InitializeAllItems()
        {
            if (_walletModel == null)
            {
                Debug.LogWarning("[WalletController] WalletModel не найден. Убедитесь, что CoreLifetimeScope находится в сцене.");
                return;
            }

            foreach (var item in currencies)
            {
                if (item != null && !string.IsNullOrEmpty(item.CurrencyId))
                {
                    item.Initialize(_walletModel);
                }
            }
        }

        private void DisposeAllItems()
        {
            foreach (var item in currencies)
            {
                item?.Dispose();
            }
        }

        /// <summary>
        /// Получить элемент валюты по ID
        /// </summary>
        public CurrencyControllerItem GetItem(string currencyId)
        {
            return currencies.FirstOrDefault(c => c != null && c.CurrencyId == currencyId);
        }

        /// <summary>
        /// Добавить новую валюту в список
        /// </summary>
        public void AddCurrency(string currencyId)
        {
            if (string.IsNullOrEmpty(currencyId))
            {
                Debug.LogWarning("[WalletController] currencyId не может быть пустым");
                return;
            }

            if (GetItem(currencyId) != null)
            {
                Debug.LogWarning($"[WalletController] Валюта с ID '{currencyId}' уже существует в списке");
                return;
            }

            var newItem = new CurrencyControllerItem
            {
                CurrencyId = currencyId,
                DefaultAmount = 100f
            };

            currencies.Add(newItem);

            // Если уже запущено - инициализируем
            if (_walletModel != null)
            {
                newItem.Initialize(_walletModel);
            }
        }

        /// <summary>
        /// Удалить валюту из списка
        /// </summary>
        public void RemoveCurrency(string currencyId)
        {
            var item = GetItem(currencyId);
            if (item != null)
            {
                item.Dispose();
                currencies.Remove(item);
            }
        }

        /// <summary>
        /// Обновить все элементы из WalletConfig и переинициализировать
        /// </summary>
        [Button]
        public void RefreshAll()
        {
            SyncCurrenciesFromConfig();
            
            if (_walletModel != null)
            {
                InitializeAllItems();
            }
        }

        [Button]
        public void Add(float amount)
        {
            Add(defaultCurrencyId, amount);
        }

        [Button]
        public void Spend(float amount)
        {
            Spend(defaultCurrencyId, amount);
        }

        [Button]
        public void Add(string currencyId, float amount)
        {
            var item = GetItem(currencyId);
            if (item != null)
            {
                item.Add(amount);
            }
            else
            {
                Debug.LogWarning($"[WalletController] Валюта с ID '{currencyId}' не найдена в списке");
            }
        }

        [Button]
        public void Spend(string currencyId, float amount)
        {
            var item = GetItem(currencyId);
            if (item != null)
            {
                item.Spend(amount);
            }
            else
            {
                Debug.LogWarning($"[WalletController] Валюта с ID '{currencyId}' не найдена в списке");
            }
        }

        [Button]
        public void SetBalance(string currencyId, float value)
        {
            var item = GetItem(currencyId);
            if (item != null)
            {
                item.SetBalance(value);
            }
            else
            {
                Debug.LogWarning($"[WalletController] Валюта с ID '{currencyId}' не найдена в списке");
            }
        }

        [Button]
        public void Reset(string currencyId)
        {
            var item = GetItem(currencyId);
            if (item != null)
            {
                item.Reset();
            }
            else
            {
                Debug.LogWarning($"[WalletController] Валюта с ID '{currencyId}' не найдена в списке");
            }
        }

        /// <summary>
        /// Элемент управления валютой в списке. Каждый элемент представляет одну валюту.
        /// </summary>
        [System.Serializable]
        public class CurrencyControllerItem : IDisposable
        {
            [Header("Currency Settings")]
            [Tooltip("Уникальный ID валюты")]
            [SerializeField] private string currencyId = "coins";

            [Tooltip("Дефолтная сумма для кнопок Add/Spend в инспекторе")]
            [SerializeField] private float defaultAmount = 100f;

            [Header("Current State")]
            [Tooltip("Текущий баланс (обновляется реактивно от модели). Не редактировать вручную!")]
            [SerializeField] private float currentBalance;

            [Header("Events")]
            [Tooltip("Событие при изменении баланса")]
            public UnityEvent<float> OnBalanceChanged;

            [Tooltip("Событие при добавлении валюты")]
            public UnityEvent<float> OnAdd;

            [Tooltip("Событие при трате валюты (amount, success)")]
            public UnityEvent<float, bool> OnSpend;

            [Tooltip("Событие при неудачной трате (недостаточно средств)")]
            public UnityEvent<float> OnSpendFailed;

            [Tooltip("Событие при достижении лимита")]
            public UnityEvent OnReachedMax;

            // Несериализуемые поля
            private MoneyModel _moneyModel;
            private CompositeDisposable _disposable = new();

            /// <summary>
            /// ID валюты
            /// </summary>
            public string CurrencyId
            {
                get => currencyId;
                set => currencyId = value;
            }

            /// <summary>
            /// Дефолтная сумма для кнопок
            /// </summary>
            public float DefaultAmount
            {
                get => defaultAmount;
                set => defaultAmount = value;
            }

            /// <summary>
            /// Текущий баланс (readonly, обновляется реактивно)
            /// </summary>
            public float CurrentBalance => currentBalance;

            /// <summary>
            /// Инициализировать элемент, установить ссылку на MoneyModel и подписаться на события
            /// </summary>
            public void Initialize(WalletModel walletModel)
            {
                if (walletModel == null)
                {
                    Debug.LogWarning($"[CurrencyControllerItem] WalletModel null для валюты '{currencyId}'");
                    return;
                }

                if (string.IsNullOrEmpty(currencyId))
                {
                    Debug.LogWarning("[CurrencyControllerItem] CurrencyId пустой, инициализация невозможна");
                    return;
                }

                // Получаем или создаём MoneyModel
                _moneyModel = walletModel.Get(currencyId);

                // Устанавливаем начальное значение
                currentBalance = _moneyModel.Balance.Value;

                // Подписываемся на изменения баланса
                _moneyModel.Balance.AsObservable()
                    .Subscribe(balance =>
                    {
                        currentBalance = balance;
                        OnBalanceChanged?.Invoke(balance);
                    })
                    .AddTo(_disposable);

                // Подписываемся на достижение лимита
                _moneyModel.OnReachedMax
                    .Subscribe(_ =>
                    {
                        OnReachedMax?.Invoke();
                    })
                    .AddTo(_disposable);
            }

            /// <summary>
            /// Освободить все подписки
            /// </summary>
            public void Dispose()
            {
                _disposable?.Dispose();
                _disposable = null;
                _moneyModel = null;
            }

            /// <summary>
            /// Добавить валюту через WalletModel (использует defaultAmount)
            /// </summary>
            [Button]
            public void Add()
            {
                Add(defaultAmount);
            }

            /// <summary>
            /// Добавить валюту через WalletModel
            /// </summary>
            [Button]
            public void Add(float amount)
            {
                if (_moneyModel == null)
                {
                    Debug.LogWarning($"[CurrencyControllerItem] MoneyModel не инициализирован для валюты '{currencyId}'");
                    return;
                }

                if (amount < 0f)
                {
                    Debug.LogWarning($"[CurrencyControllerItem] Нельзя добавить отрицательное количество. Amount: {amount}");
                    return;
                }

                _moneyModel.Add(amount);
                OnAdd?.Invoke(amount);
            }

            /// <summary>
            /// Потратить валюту через WalletModel (использует defaultAmount)
            /// </summary>
            [Button]
            public void Spend()
            {
                Spend(defaultAmount);
            }

            /// <summary>
            /// Потратить валюту через WalletModel
            /// </summary>
            /// <returns>True если успешно, false если недостаточно средств</returns>
            [Button]
            public bool Spend(float amount)
            {
                if (_moneyModel == null)
                {
                    Debug.LogWarning($"[CurrencyControllerItem] MoneyModel не инициализирован для валюты '{currencyId}'");
                    return false;
                }

                if (amount < 0f)
                {
                    Debug.LogWarning($"[CurrencyControllerItem] Нельзя потратить отрицательное количество. Amount: {amount}");
                    return false;
                }

                bool success = _moneyModel.Spend(amount);
                OnSpend?.Invoke(amount, success);

                if (!success)
                {
                    OnSpendFailed?.Invoke(amount);
                }

                return success;
            }

            /// <summary>
            /// Установить баланс напрямую через WalletModel (использует defaultAmount)
            /// </summary>
            [Button]
            public void SetBalance()
            {
                SetBalance(defaultAmount);
            }

            /// <summary>
            /// Установить баланс напрямую через WalletModel
            /// </summary>
            [Button]
            public void SetBalance(float value)
            {
                if (_moneyModel == null)
                {
                    Debug.LogWarning($"[CurrencyControllerItem] MoneyModel не инициализирован для валюты '{currencyId}'");
                    return;
                }

                _moneyModel.SetBalance(value);
            }

            /// <summary>
            /// Сбросить баланс через WalletModel
            /// </summary>
            [Button]
            public void Reset()
            {
                if (_moneyModel == null)
                {
                    Debug.LogWarning($"[CurrencyControllerItem] MoneyModel не инициализирован для валюты '{currencyId}'");
                    return;
                }

                _moneyModel.Reset();
            }
        }
    }
}

