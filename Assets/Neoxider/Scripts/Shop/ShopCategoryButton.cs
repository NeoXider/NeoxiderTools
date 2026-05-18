using UnityEngine;
using UnityEngine.UI;

namespace Neo.Shop
{
    /// <summary>
    ///     NoCode helper for category tabs. Put it on a Button and assign a ShopListView.
    ///     The button can show all items or switch the target view to one serialized category.
    /// </summary>
    [NeoDoc("Shop/ShopCategoryButton.md")]
    [CreateFromMenu("Neoxider/Shop/ShopCategoryButton")]
    [AddComponentMenu("Neoxider/Shop/" + nameof(ShopCategoryButton))]
    public sealed class ShopCategoryButton : MonoBehaviour
    {
        [SerializeField] private ShopListView _targetView;
        [SerializeField] private string _category = "";
        [SerializeField] private bool _showAll;
        [SerializeField] private bool _autoBindButton = true;
        [SerializeField] private Button _button;

        public string Category
        {
            get => _category;
            set => _category = value ?? "";
        }

        public ShopListView TargetView
        {
            get => _targetView;
            set => _targetView = value;
        }

        private void OnEnable()
        {
            ResolveButton();
            if (_autoBindButton && _button != null)
            {
                _button.onClick.AddListener(Apply);
            }
        }

        private void OnDisable()
        {
            if (_button != null)
            {
                _button.onClick.RemoveListener(Apply);
            }
        }

        private void OnValidate()
        {
            ResolveButton();
        }

        [Button]
        public void Apply()
        {
            ResolveView();
            if (_targetView == null)
            {
                return;
            }

            if (_showAll)
            {
                _targetView.ShowAll();
                return;
            }

            _targetView.ShowCategory(_category);
        }

        public void SetShowAll(bool showAll)
        {
            _showAll = showAll;
        }

        private void ResolveButton()
        {
            if (_button == null)
            {
                _button = GetComponent<Button>();
            }
        }

        private void ResolveView()
        {
            if (_targetView != null)
            {
                return;
            }

            _targetView = GetComponentInParent<ShopListView>();
            if (_targetView == null)
            {
                _targetView = FindFirstObjectByType<ShopListView>();
            }
        }
    }
}
