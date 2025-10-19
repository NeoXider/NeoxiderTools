using Neo.Runtime.Features.Money.Model;

namespace Neo.Runtime.Features.Wallet.Interfaces
{
    /// <summary>
    /// Defines a contract for creating currency models.
    /// </summary>
    public interface ICurrencyFactory
    {
        /// <summary>
        /// Creates a new money model with the specified currency identifier.
        /// </summary>
        /// <param name="currencyId">The unique identifier of the currency to create.</param>
        /// <returns>A new instance of MoneyModel for the specified currency.</returns>
        MoneyModel Create(string currencyId);
    }
}
