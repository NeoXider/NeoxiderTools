using Neo.Runtime.Features.Money.Model;

namespace Neo.Runtime.Features.Wallet.Interfaces
{
    public interface ICurrencyFactory
    {
        MoneyModel Create(string currencyId);
    }
}