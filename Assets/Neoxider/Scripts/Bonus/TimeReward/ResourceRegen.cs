using Neo.Shop;
using Neo.Tools;
using UnityEngine;

namespace Neo.Bonus
{
    /// <summary>
    ///     One-component regenerating resource (energy / lives / stamina): wires a
    ///     <see cref="CooldownReward"/> (auto-claim ticker) into a <see cref="Money"/> wallet
    ///     (soft-capped counter) and, optionally, a <see cref="TimeToText"/> countdown label.
    ///     <para>
    ///         Setup: Money (set <c>Max Money</c> to the cap) + CooldownReward (set
    ///         <c>Cooldown Seconds</c> to the regen period) + this component on one object.
    ///         Auto-claim is forced on; every claim deposits <see cref="_amountPerClaim"/>.
    ///         Sources allowed to exceed the cap (bonuses, purchases) should call
    ///         <see cref="Money.AddOverflow"/> themselves.
    ///     </para>
    /// </summary>
    [NeoDoc("Bonus/TimeReward/ResourceRegen.md")]
    [CreateFromMenu("Neoxider/Bonus/ResourceRegen")]
    [AddComponentMenu("Neoxider/Bonus/" + nameof(ResourceRegen))]
    [RequireComponent(typeof(CooldownReward))]
    public sealed class ResourceRegen : MonoBehaviour
    {
        [Tooltip("Cooldown ticker. Defaults to the CooldownReward on this object.")]
        [SerializeField]
        private CooldownReward _reward;

        [Tooltip("Wallet receiving the regenerated amount. Defaults to a Money on this object.")]
        [SerializeField]
        private Money _wallet;

        [Tooltip("Amount deposited per claim. Clamped by the wallet's Max Money (soft cap).")]
        [SerializeField] [Min(0f)]
        private float _amountPerClaim = 1f;

        [Tooltip("Optional countdown label driven by the reward's RemainingTime.")]
        [SerializeField]
        private TimeToText _timeText;

        [Tooltip("Pause the countdown display at zero while the wallet is at its cap.")]
        [SerializeField]
        private bool _showZeroWhenFull = true;

        private void Awake()
        {
            if (_reward == null)
            {
                _reward = GetComponent<CooldownReward>();
            }

            if (_wallet == null)
            {
                _wallet = GetComponent<Money>();
            }

            if (_reward != null)
            {
                _reward.AutoClaim = true;
                _reward.OnRewardClaimed.AddListener(Deposit);
                _reward.RemainingTime.AddListener(UpdateLabel);
            }
        }

        private void OnDestroy()
        {
            if (_reward != null)
            {
                _reward.OnRewardClaimed.RemoveListener(Deposit);
                _reward.RemainingTime.RemoveListener(UpdateLabel);
            }
        }

        private bool IsFull =>
            _wallet != null
            && _wallet.MaxMoney > 0f
            && _wallet.CurrentMoney.CurrentValue >= _wallet.MaxMoney;

        private void Deposit()
        {
            if (_wallet != null && _amountPerClaim > 0f)
            {
                _wallet.Add(_amountPerClaim);
            }
        }

        private void UpdateLabel(float remaining)
        {
            if (_timeText == null)
            {
                return;
            }

            _timeText.Set(_showZeroWhenFull && IsFull ? 0f : Mathf.Max(0f, remaining));
        }
    }
}
