namespace Neo.Shop
{
    public enum MoneySpendStatus
    {
        Confirmed = 0,
        RejectedInvalidAmount = 1,
        RejectedInsufficientFunds = 2,
        RequestedServerAuthority = 3
    }

    public readonly struct MoneySpendResult
    {
        public MoneySpendResult(MoneySpendStatus status, float amount, float balanceBefore, float balanceAfter)
        {
            Status = status;
            Amount = amount;
            BalanceBefore = balanceBefore;
            BalanceAfter = balanceAfter;
        }

        public MoneySpendStatus Status { get; }
        public float Amount { get; }
        public float BalanceBefore { get; }
        public float BalanceAfter { get; }
        public bool IsConfirmed => Status == MoneySpendStatus.Confirmed;
        public bool IsRejected => Status is MoneySpendStatus.RejectedInvalidAmount or MoneySpendStatus.RejectedInsufficientFunds;
        public bool IsPendingServerAuthority => Status == MoneySpendStatus.RequestedServerAuthority;
    }
}

public interface IMoneySpend
{
    public bool Spend(float count);
}

public interface IMoneySpendWithResult : IMoneySpend
{
    public Neo.Shop.MoneySpendResult TrySpend(float count);
}

public interface IMoneySpendAuthority : IMoneySpendWithResult
{
    public bool CanConfirmSpendNow(float count);
}

public interface IMoneyAdd
{
    public void Add(float count);
}
