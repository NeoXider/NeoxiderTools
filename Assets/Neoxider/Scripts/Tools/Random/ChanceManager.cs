using System.Collections.Generic;
using UnityEngine;

namespace Neo.Tools
{
    [System.Serializable]
    public class ChanceManager
    {
        public List<Chance> chances = new List<Chance>();

        public void AddChance(float chance)
        {
            chances.Add(new Chance(chance));
            NormalizeChances();
        }

        public void NormalizeChances()
        {
            float total = 0f;
            foreach (var chance in chances)
            {
                total += chance.value;
            }

            if (total > 1f)
            {
                for (int i = 0; i < chances.Count; i++)
                {
                    chances[i].value /= total;
                }
            }
            else if (total < 1f && chances.Count > 0)
            {
                chances[chances.Count - 1].value = 1f - (total - chances[chances.Count - 1].value);
            }
        }

        public int GetId(float randomValue)
        {
            float cumulative = 0f;
            for (int i = 0; i < chances.Count; i++)
            {
                cumulative += chances[i].value;
                if (randomValue <= cumulative)
                {
                    return i;
                }
            }
            return -1;
        }

        public int GetChanceId()
        {
            float randomValue = Random.Range(0f, 1f);
            return GetId(randomValue);
        }
    }

    [System.Serializable]
    public class Chance
    {
        [Range(0, 1)]
        public float value;

        public Chance(float value)
        {
            this.value = value;
        }
    }
}