using System;
using System.Collections.Generic;
using UnityEngine;

namespace Neo.Tools
{
    /// <summary>
    /// Manages a collection of chances and provides methods for random selection based on probability.
    /// </summary>
    /// <remarks>
    /// Usage examples:
    /// 
    /// 1. Using ChanceSystemBehaviour in a scene:
    /// <code>
    /// // Attach ChanceSystemBehaviour to a GameObject
    /// public class LootSystem : MonoBehaviour
    /// {
    ///     [SerializeField] private ChanceSystemBehaviour chanceSystem;
    ///     
    ///     private void Start()
    ///     {
    ///         // Setup drop chances
    ///         chanceSystem.AddChance(0.7f);  // Common item: 70%
    ///         chanceSystem.AddChance(0.25f); // Rare item: 25%
    ///         chanceSystem.AddChance(0.05f); // Epic item: 5%
    ///         
    ///         // Listen for random drops
    ///         chanceSystem.OnIdGenerated.AddListener(OnItemDropped);
    ///     }
    ///     
    ///     private void OnItemDropped(int itemRarityId)
    ///     {
    ///         switch(itemRarityId)
    ///         {
    ///             case 0: DropCommonItem(); break;
    ///             case 1: DropRareItem(); break;
    ///             case 2: DropEpicItem(); break;
    ///         }
    ///     }
    ///     
    ///     public void TryDropItem()
    ///     {
    ///         chanceSystem.GenerateId(); // This will trigger OnItemDropped
    ///     }
    /// }
    /// </code>
    /// 
    /// 2. Using ChanceData ScriptableObject:
    /// <code>
    /// // Create ChanceData asset: Right click > Create > Neoxider > Random > ChanceData
    /// // Then in your script:
    /// public class CardSystem : MonoBehaviour
    /// {
    ///     [SerializeField] private ChanceData cardChances;
    ///     
    ///     private void Start()
    ///     {
    ///         // Setup card draw chances in inspector or code
    ///         cardChances.AddChance(0.6f);  // Common card: 60%
    ///         cardChances.AddChance(0.3f);  // Rare card: 30%
    ///         cardChances.AddChance(0.1f);  // Legendary card: 10%
    ///         
    ///         // Draw a random card
    ///         int cardRarity = cardChances.GenerateId();
    ///         Debug.Log($"Drew a card with rarity level {cardRarity}");
    ///     }
    /// }
    /// </code>
    /// 
    /// 3. Using ChanceManager directly:
    /// <code>
    /// public class EnemySpawner
    /// {
    ///     private ChanceManager spawnChances = new ChanceManager();
    ///     
    ///     public void SetupSpawnChances()
    ///     {
    ///         // Chances will be automatically normalized to sum to 1
    ///         spawnChances.AddChance(50f); // Weak enemy: ~45%
    ///         spawnChances.AddChance(40f); // Normal enemy: ~36%
    ///         spawnChances.AddChance(20f); // Strong enemy: ~18%
    ///         
    ///         // Get spawn type
    ///         int enemyType = spawnChances.GetChanceId();
    ///     }
    /// }
    /// </code>
    /// </remarks>
    [Serializable]
    public class ChanceManager
    {
        /// <summary>
        /// List of chances with their probability values
        /// </summary>
        [SerializeField]
        [Tooltip("List of chances. Values will be automatically normalized to sum to 1")]
        public List<Chance> chances = new List<Chance>();

        /// <summary>
        /// Determines whether to preserve the first (true) or last (false) chance value during normalization
        /// </summary>
        [SerializeField]
        [Tooltip("If true, preserves the first chance value. If false, preserves the last chance value")]
        public bool preserveFirstChance = false;

        /// <summary>
        /// Adds a new chance with the specified probability value.
        /// The new value is preserved based on preserveFirstChance setting.
        /// </summary>
        /// <param name="chance">Probability value between 0 and 1</param>
        /// <returns>Index of the newly added chance</returns>
        public int AddChance(float chance)
        {
            chance = Mathf.Clamp01(chance);
            chances.Add(new Chance(chance));
            NormalizeChances();
            return chances.Count - 1;
        }

        /// <summary>
        /// Normalizes chances while preserving either first or last value based on preserveFirstChance setting
        /// </summary>
        private void NormalizeChances()
        {
            if (chances.Count <= 1) return;

            // Get the preserved value (first or last)
            int preservedIndex = preserveFirstChance ? 0 : chances.Count - 1;
            float preservedValue = chances[preservedIndex].value;
            
            // Calculate remaining probability for other elements
            float remainingProbability = 1f - preservedValue;
            
            // Early exit if no probability left for others
            if (remainingProbability <= 0)
            {
                // Set all other values to 0
                for (int i = 0; i < chances.Count; i++)
                {
                    if (i != preservedIndex)
                    {
                        chances[i].value = 0;
                    }
                }
                chances[preservedIndex].value = 1f;
                return;
            }

            // Calculate sum of other probabilities
            float otherSum = 0f;
            for (int i = 0; i < chances.Count; i++)
            {
                if (i != preservedIndex)
                {
                    otherSum += chances[i].value;
                }
            }

            // If other sum is 0, we can't preserve proportions
            if (Mathf.Approximately(otherSum, 0f))
            {
                // Distribute remaining probability based on position relative to preserved value
                int count = chances.Count - 1; // number of values to distribute
                if (preserveFirstChance)
                {
                    // Distribute with increasing weights from start
                    float total = 0f;
                    for (int i = 1; i < chances.Count; i++)
                    {
                        float weight = i / (float)chances.Count;
                        chances[i].value = remainingProbability * weight;
                        total += chances[i].value;
                    }
                    
                    // Normalize to exactly match remaining probability
                    if (total > 0f)
                    {
                        float normalizer = remainingProbability / total;
                        for (int i = 1; i < chances.Count; i++)
                        {
                            chances[i].value *= normalizer;
                        }
                    }
                }
                else
                {
                    // Distribute with decreasing weights from end
                    float total = 0f;
                    for (int i = 0; i < chances.Count - 1; i++)
                    {
                        float weight = (chances.Count - 1 - i) / (float)chances.Count;
                        chances[i].value = remainingProbability * weight;
                        total += chances[i].value;
                    }
                    
                    // Normalize to exactly match remaining probability
                    if (total > 0f)
                    {
                        float normalizer = remainingProbability / total;
                        for (int i = 0; i < chances.Count - 1; i++)
                        {
                            chances[i].value *= normalizer;
                        }
                    }
                }
            }
            else
            {
                // Scale other values to fit remaining probability while preserving proportions
                float scale = remainingProbability / otherSum;
                for (int i = 0; i < chances.Count; i++)
                {
                    if (i != preservedIndex)
                    {
                        chances[i].value *= scale;
                    }
                }
            }

            // Ensure preserved value maintains its exact value
            chances[preservedIndex].value = preservedValue;

            // Validate that sum is exactly 1
            float finalSum = 0f;
            for (int i = 0; i < chances.Count; i++)
            {
                finalSum += chances[i].value;
            }
            
            if (!Mathf.Approximately(finalSum, 1f))
            {
                float diff = 1f - finalSum;
                // Distribute any tiny floating-point error to the largest non-preserved value
                float maxVal = 0f;
                int maxIdx = -1;
                for (int i = 0; i < chances.Count; i++)
                {
                    if (i != preservedIndex && chances[i].value > maxVal)
                    {
                        maxVal = chances[i].value;
                        maxIdx = i;
                    }
                }
                if (maxIdx >= 0)
                {
                    chances[maxIdx].value += diff;
                }
            }
        }

        /// <summary>
        /// Removes a chance at the specified index
        /// </summary>
        /// <param name="index">Index of the chance to remove</param>
        public void RemoveChance(int index)
        {
            if (index >= 0 && index < chances.Count)
            {
                chances.RemoveAt(index);
                NormalizeChances();
            }
        }

        /// <summary>
        /// Gets the index based on a specific random value
        /// </summary>
        /// <param name="randomValue">Random value between 0 and 1</param>
        /// <returns>Selected index based on probabilities, or -1 if no valid selection</returns>
        public int GetId(float randomValue)
        {
            if (chances.Count == 0) return -1;
            
            float cumulative = 0f;
            for (int i = 0; i < chances.Count; i++)
            {
                cumulative += chances[i].value;
                if (randomValue <= cumulative)
                {
                    return i;
                }
            }
            return chances.Count - 1; // Fallback to last item
        }

        /// <summary>
        /// Gets a random index based on the configured probabilities
        /// </summary>
        /// <returns>Randomly selected index based on probabilities</returns>
        public int GetChanceId()
        {
            return GetId(UnityEngine.Random.Range(0f, 1f));
        }

        /// <summary>
        /// Gets the probability value at the specified index
        /// </summary>
        /// <param name="index">Index of the chance</param>
        /// <returns>Probability value, or 0 if index is invalid</returns>
        public float GetChanceValue(int index)
        {
            return index >= 0 && index < chances.Count ? chances[index].value : 0f;
        }

        /// <summary>
        /// Sets the probability value at the specified index
        /// </summary>
        /// <param name="index">Index of the chance</param>
        /// <param name="value">New probability value</param>
        public void SetChanceValue(int index, float value)
        {
            if (index >= 0 && index < chances.Count)
            {
                chances[index].value = Mathf.Clamp01(value);
                NormalizeChances();
            }
        }
    }

    /// <summary>
    /// Represents a single chance with a probability value
    /// </summary>
    [Serializable]
    public class Chance
    {
        /// <summary>
        /// Probability value between 0 and 1
        /// </summary>
        [Range(0, 1)]
        [Tooltip("Probability value between 0 and 1")]
        public float value;

        public Chance(float value)
        {
            this.value = Mathf.Clamp01(value);
        }
    }
}