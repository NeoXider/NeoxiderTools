using System;
using System.Linq;
using UnityEngine;
using UnityEngine.Events;

namespace Neo.Bonus
{
    public class Collection : MonoBehaviour
    {
        public static Collection Instance;
        [SerializeField] private bool randomPrize = true;
        public ItemCollectionData[] itemCollectionDatas;
        public bool[] enabledItems;

        [Space]
        public UnityEvent<int> OnGetItem;
        public UnityEvent OnLoadItems;

        private void Awake()
        {
            Instance = this;

            Load();
        }

        private void Load()
        {
            enabledItems = new bool[itemCollectionDatas.Length];

            for (int i = 0; i < itemCollectionDatas.Length; i++)
            {
                enabledItems[i] = PlayerPrefs.GetInt($"skin_{i}", 0) == 1;
            }

            OnLoadItems?.Invoke();
        }

        public ItemCollectionData GetPrize()
        {
            var uniqs = itemCollectionDatas.Where(x => !enabledItems[Array.IndexOf(itemCollectionDatas, x)]).ToArray();

            if (uniqs.Length == 0) return null;

            int prizeId = Array.IndexOf(itemCollectionDatas, randomPrize ? uniqs.GetRandomElement() : uniqs.First());

            SaveCollection(prizeId);

            OnGetItem?.Invoke(prizeId);

            return itemCollectionDatas[prizeId];
        }

        private void SaveCollection(int id)
        {
            enabledItems[id] = true;
            PlayerPrefs.SetInt($"skin_{id}", 1);
        }
    }
}
