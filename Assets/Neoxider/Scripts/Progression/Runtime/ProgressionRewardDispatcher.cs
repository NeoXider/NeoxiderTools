using System.Collections.Generic;
using Neo.Bonus;
using Neo.Quest;
using Neo.Shop;
using UnityEngine;

namespace Neo.Progression
{
    /// <summary>
    /// Applies progression rewards to the target gameplay systems.
    /// </summary>
    public static class ProgressionRewardDispatcher
    {
        /// <summary>
        /// Dispatches all supplied rewards.
        /// </summary>
        public static void DispatchRewards(IEnumerable<ProgressionReward> rewards, ProgressionManager manager)
        {
            if (rewards == null || manager == null)
            {
                return;
            }

            foreach (ProgressionReward reward in rewards)
            {
                DispatchReward(reward, manager);
            }
        }

        /// <summary>
        /// Dispatches a single reward.
        /// </summary>
        public static void DispatchReward(ProgressionReward reward, ProgressionManager manager)
        {
            if (reward == null || manager == null)
            {
                return;
            }

            switch (reward.RewardType)
            {
                case ProgressionRewardType.None:
                    return;
                case ProgressionRewardType.Xp:
                    manager.AddXp(reward.Amount);
                    return;
                case ProgressionRewardType.PerkPoints:
                    manager.AddPerkPoints(reward.Amount);
                    return;
                case ProgressionRewardType.Money:
                {
                    Money money = reward.MoneyTarget != null ? reward.MoneyTarget : Money.I;
                    if (money != null)
                    {
                        money.Add(reward.MoneyAmount);
                    }
                    else
                    {
                        Debug.LogWarning("[ProgressionRewardDispatcher] Money reward skipped because no Money target was found.");
                    }

                    return;
                }
                case ProgressionRewardType.UnlockCollectionItem:
                {
                    Collection collection = reward.CollectionTarget != null ? reward.CollectionTarget : Collection.I;
                    if (collection != null && reward.CollectionItem != null)
                    {
                        collection.AddItem(reward.CollectionItem);
                    }
                    else
                    {
                        Debug.LogWarning(
                            "[ProgressionRewardDispatcher] Collection reward skipped because collection or item is missing.");
                    }

                    return;
                }
                case ProgressionRewardType.AcceptQuest:
                {
                    QuestManager questManager = reward.QuestManagerTarget != null
                        ? reward.QuestManagerTarget
                        : QuestManager.Instance;
                    if (questManager != null && reward.Quest != null)
                    {
                        questManager.AcceptQuest(reward.Quest);
                    }
                    else
                    {
                        Debug.LogWarning(
                            "[ProgressionRewardDispatcher] Quest reward skipped because QuestManager or QuestConfig is missing.");
                    }

                    return;
                }
                default:
                    Debug.LogWarning($"[ProgressionRewardDispatcher] Unsupported reward type: {reward.RewardType}");
                    return;
            }
        }
    }
}
