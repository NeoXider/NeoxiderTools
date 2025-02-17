using UnityEngine;

namespace Neo
{
    namespace Tools
    {
        public class Loot : MonoBehaviour
        {
            [SerializeField] private GameObject[] _lootItems;
            [Space, Range(0, 100)] public float _dropChance = 100;

            [Space, Header("Settings")]
            [SerializeField, Min(0)] private int _minCount = 1;
            [SerializeField, Min(0)] private int _maxCount = 3;
            [SerializeField] private float _dropRadius = 0f;

            public void DropLoot()
            {
                if (Random.Range(0, 100f) > _dropChance)
                {
                    int lootCount = Random.Range(_minCount, _maxCount + 1);

                    for (int i = 0; i < lootCount; i++)
                    {
                        GameObject selectedItem = GetRandomPrefab();

                        if (selectedItem != null)
                        {
                            Vector3 dropPosition = transform.position + Random.insideUnitSphere * _dropRadius;
                            dropPosition.y = transform.position.y;
                            Instantiate(selectedItem, dropPosition, Quaternion.identity);
                        }
                    }
                }
            }

            private GameObject GetRandomPrefab()
            {
                if (_lootItems.Length > 0)
                {
                    if (_lootItems.Length == 1)
                    {
                        return _lootItems[0];
                    }
                    else
                    {
                        int randId = Random.Range(0, _lootItems.Length);
                        return _lootItems[randId];
                    }
                }

                return null;
            }
        }
    }
}