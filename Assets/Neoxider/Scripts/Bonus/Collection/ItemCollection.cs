using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

namespace Neo.Bonus
{
    public class ItemCollection : MonoBehaviour
    {
        [GetComponent] public Button button;
        [SerializeField] private Image _itemImage;
        public UnityEvent<bool> OnChangeEnabled;
        public UnityEvent OnActive;
        public UnityEvent OnDeactivated;

        public void SetEnabled(bool active)
        {
            OnChangeEnabled.Invoke(active);

            if (active)
                OnActive?.Invoke();
            else
                OnDeactivated?.Invoke();
        }

        public void SetSprite(Sprite sprite)
        {
            _itemImage.sprite = sprite;
        }

        public void SetData(ItemCollectionData itemCollectionData)
        {
            SetSprite(itemCollectionData.sprite);
        }
    }
}