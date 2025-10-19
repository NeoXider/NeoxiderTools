using UnityEngine;

namespace Neo.Runtime.Features.Wallet.Data
{
    /// <summary>
    /// Represents a currency definition with its properties.
    /// </summary>
    [System.Serializable]
    public class CurrencyDefinition
    {
        /// <summary>
        /// Unique identifier for the currency (key used to find model and view).
        /// </summary>
        [Tooltip("Уникальный ID валюты (ключ, по которому ищем модель и вью).")]
        public string CurrencyId = "coins";

        /// <summary>
        /// Icon for the currency (optional for UI purposes).
        /// </summary>
        [Tooltip("Иконка валюты (опционально для UI).")]
        public Sprite Icon;

        /// <summary>
        /// Default start amount when no save exists.
        /// </summary>
        [Header("Defaults")] 
        [Tooltip("Стартовый баланс при отсутствии сохранений.")]
        public float StartAmount = 0f;

        /// <summary>
        /// Maximum limit; 0 means unlimited (percentage is not displayed).
        /// </summary>
        [Tooltip("Лимит; 0 — без лимита (процент не отображается).")]
        public float MaxAmount = 0f;
    }
}
