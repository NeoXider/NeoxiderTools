namespace Neo.Runtime.Features.Money.View
{
    /// <summary>
    /// Interface for money view
    /// </summary>
    public interface IMoneyView
    {
        /// <summary>
        /// Update money display
        /// </summary>
        /// <param name="balance">Current balance</param>
        /// <param name="max">Maximum limit</param>
        void UpdateMoney(float balance, float max);

        /// <summary>
        /// Update maximum money display
        /// </summary>
        /// <param name="max">Maximum limit</param>
        void UpdateMaxMoney(float max);

        /// <summary>
        /// Update money percentage display (0..1)
        /// Called only when hasLimit == true
        /// </summary>
        /// <param name="percentage">Percentage value 0..1</param>
        void UpdateMoneyPercentage(float percentage);   // 0..1

        /// <summary>
        /// Update money percent display (0..100)
        /// Called only when hasLimit == true
        /// </summary>
        /// <param name="percent100">Percent value 0..100</param>
        void UpdateMoneyPercent100(float percent100);   // 0..100

        /// <summary>
        /// Show wallet full indicator
        /// Responds only when limit is active
        /// </summary>
        /// <param name="full">True if wallet is full</param>
        void ShowWalletFull(bool full);

        /// <summary>
        /// Update money delta (if used)
        /// </summary>
        /// <param name="delta">Delta value</param>
        void UpdateMoneyDelta(float delta);

        /// <summary>
        /// Set limit mode
        /// true - has limit, false - unlimited (hide percentages)
        /// </summary>
        /// <param name="hasLimit">True if has limit</param>
        void SetLimitMode(bool hasLimit);
    }
}
