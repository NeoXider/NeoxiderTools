using System.Collections.Generic;
using Neo.Bonus;
using Neo.Quest;
using Neo.Shop;
using UnityEngine;

namespace Neo.Progression
{
    /// <summary>
    ///     Applies progression rewards to the target gameplay systems.
    /// </summary>
    public static class ProgressionRewardDispatcher
    {
        /// <summary>
        ///     Dispatches all supplied rewards. If premiumOnly is true, only premium rewards are dispatched.
        /// </summary>
        public static void DispatchRewards(IEnumerable<ProgressionReward> rewards, ProgressionManager manager,
            bool premiumOnly = false)
        {
            if (rewards is IReadOnlyList<ProgressionReward> list)
            {
                DispatchRewards(list, manager, premiumOnly);
                return;
            }

            if (rewards == null || manager == null)
            {
                return;
            }

            manager.EnsureInitialized();
            foreach (ProgressionReward reward in rewards)
            {
                DispatchFiltered(reward, manager, premiumOnly);
            }
        }

        /// <summary>
        ///     Indexed overload of <see cref="DispatchRewards(IEnumerable{ProgressionReward},ProgressionManager,bool)" />
        ///     used by level, node and perk rewards; it avoids the boxed enumerator of the interface overload.
        /// </summary>
        public static void DispatchRewards(IReadOnlyList<ProgressionReward> rewards, ProgressionManager manager,
            bool premiumOnly = false)
        {
            if (rewards == null || manager == null)
            {
                return;
            }

            manager.EnsureInitialized();
            for (int i = 0; i < rewards.Count; i++)
            {
                DispatchFiltered(rewards[i], manager, premiumOnly);
            }
        }

        private static void DispatchFiltered(ProgressionReward reward, ProgressionManager manager, bool premiumOnly)
        {
            if (reward == null)
            {
                return;
            }

            if (premiumOnly && !reward.IsPremium)
            {
                return;
            }

            if (!premiumOnly && reward.IsPremium && !manager.HasPremium)
            {
                return;
            }

            DispatchReward(reward, manager);
        }

        /// <summary>
        ///     Dispatches a single reward.
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
                        NeoDiagnostics.LogWarning(
                            "[ProgressionRewardDispatcher] Money reward skipped because no Money target was found.");
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
                        NeoDiagnostics.LogWarning(
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
                        NeoDiagnostics.LogWarning(
                            "[ProgressionRewardDispatcher] Quest reward skipped because QuestManager or QuestConfig is missing.");
                    }

                    return;
                }
                default:
                    NeoDiagnostics.LogWarning(
                        $"[ProgressionRewardDispatcher] Unsupported reward type: {reward.RewardType}");
                    return;
            }
        }
    }
}
