using UnityEngine;
using Neo.Runtime.Features.Money.View;

namespace Neo.Runtime.Features.Wallet.Presentation
{
    /// <summary>
    /// Wrapper around MoneyView that binds to a specific currency through CurrencyId.
    /// Attach this component to UI objects that have MoneyView.
    /// </summary>
    public class MoneyViewWithId : MoneyView
    {
        /// <summary>
        /// The unique identifier of the currency this view is bound to.
        /// </summary>
        [SerializeField] private string currencyId = "coins";
        
        /// <summary>
        /// Gets the currency ID that this view is associated with.
        /// </summary>
        public string CurrencyId => currencyId;
    }
}
