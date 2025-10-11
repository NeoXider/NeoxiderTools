using UnityEngine;
using Neo.Extensions;

namespace Neo
{
    namespace Tools
    {
        public class Loot : MonoBehaviour
        {
             public GameObject[] lootItems;
            [Space] [Range(0, 1)] public float dropChance = 1;

            [Space] [Header("Settings")] public bool spawnOnDestroy = true;
            [Min(0)] public int minCount = 1;
            [Min(0)] public int maxCount = 3;
            public float dropRadius;

            private void OnDestroy()
            {
                if (spawnOnDestroy)
                {
                    DropLoot();
                }
            }

            public void DropLoot()
            {
                if (dropChance.Chance())
                {
                    int lootCount = minCount.RandomToValue(maxCount + 1);

                    for (int i = 0; i < lootCount; i++)
                    {
                        GameObject selectedItem = GetRandomPrefab();

                        if (selectedItem != null)
                        {
                            Vector3 dropPosition = transform.position + Random.insideUnitSphere * dropRadius;
                            dropPosition.y = transform.position.y;
                            Instantiate(selectedItem, dropPosition, Quaternion.identity);
                        }
                    }
                }
            }

            private GameObject GetRandomPrefab()
            {
                if (lootItems.Length > 0)
                {
                    if (lootItems.Length == 1)
                    {
                        return lootItems[0];
                    }
                    
                    return lootItems.GetRandomElement();
                }

                return null;
            }
        }
    }
}