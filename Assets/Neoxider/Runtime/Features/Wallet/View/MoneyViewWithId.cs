using UnityEngine;
using Neo.Runtime.Features.Money.View;

namespace Neo.Runtime.Features.Wallet.Presentation
{
    /// <summary>
    /// Обёртка над MoneyView с привязкой к конкретной валюте через CurrencyId.
    /// Положи этот компонент на UI-объект с MoneyView.
    /// </summary>
    public class MoneyViewWithId : MoneyView
    {
        [SerializeField] private string currencyId = "coins";
        public string CurrencyId => currencyId;
    }
}