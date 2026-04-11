using System;
using Neo.Bonus;
using Neo.Quest;
using Neo.Shop;
using UnityEngine;

namespace Neo.Progression
{
    /// <summary>
    ///     Describes a single reward that can be granted by levels, unlock nodes, or perks.
    /// </summary>
    [Serializable]
    public sealed class ProgressionReward
    {
        [SerializeField] private ProgressionRewardType _rewardType;
        [SerializeField] [Min(0)] private int _amount = 1;
        [SerializeField] [Min(0f)] private float _moneyAmount = 1f;
        [SerializeField] private Money _moneyTarget;
        [SerializeField] private Collection _collectionTarget;
        [SerializeField] private ItemCollectionData _collectionItem;
        [SerializeField] private QuestManager _questManager;
        [SerializeField] private QuestConfig _quest;
        [SerializeField] [TextArea(2, 4)] private string _description;
        [SerializeField] private bool _isPremium;

        /// <summary>
        ///     Gets whether this reward is only granted to premium track/pass owners.
        /// </summary>
        public bool IsPremium => _isPremium;

        /// <summary>
        ///     Gets the configured reward type.
        /// </summary>
        public ProgressionRewardType RewardType => _rewardType;

        /// <summary>
        ///     Gets the integer amount used by XP and perk point rewards.
        /// </summary>
        public int Amount => _amount;

        /// <summary>
        ///     Gets the amount used by money rewards.
        /// </summary>
        public float MoneyAmount => _moneyAmount;

        /// <summary>
        ///     Gets the explicit money target. When null, the system falls back to <see cref="Money.I" />.
        /// </summary>
        public Money MoneyTarget => _moneyTarget;

        /// <summary>
        ///     Gets the explicit collection target. When null, the system falls back to <see cref="Collection.I" />.
        /// </summary>
        public Collection CollectionTarget => _collectionTarget;

        /// <summary>
        ///     Gets the collection item that should be unlocked.
        /// </summary>
        public ItemCollectionData CollectionItem => _collectionItem;

        /// <summary>
        ///     Gets the explicit quest manager target. When null, the system falls back to <see cref="QuestManager.Instance" />.
        /// </summary>
        public QuestManager QuestManagerTarget => _questManager;

        /// <summary>
        ///     Gets the quest asset that should be accepted.
        /// </summary>
        public QuestConfig Quest => _quest;

        /// <summary>
        ///     Gets the optional designer-facing reward note.
        /// </summary>
        public string Description => _description;
    }
}
