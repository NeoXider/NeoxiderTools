using UnityEngine;

namespace Neo.Runtime.Features.Wallet.Data
{
    [System.Serializable]
    public class CurrencyDefinition
    {
        [Tooltip("Уникальный ID валюты (ключ, по которому ищем модель и вью).")]
        public string CurrencyId = "coins";

        [Tooltip("Иконка валюты (опционально для UI).")]
        public Sprite Icon;

        [Header("Defaults")] [Tooltip("Стартовый баланс при отсутствии сохранений.")]
        public float StartAmount = 0f;

        [Tooltip("Лимит; 0 — без лимита (процент не отображается).")]
        public float MaxAmount = 0f;
    }
}