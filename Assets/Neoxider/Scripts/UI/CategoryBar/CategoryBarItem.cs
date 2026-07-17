using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Neo.UI
{
    /// <summary>
    ///     One entry view inside a <see cref="CategoryBar"/>: a Button plus optional label, icon and a
    ///     selected visual (frame/underline/marker object). The bar only toggles the selected visual and
    ///     interactable state — it never resizes or repositions authored child graphics.
    /// </summary>
    [NeoDoc("UI/CategoryBar.md")]
    [AddComponentMenu("Neoxider/" + "UI/" + nameof(CategoryBarItem))]
    public sealed class CategoryBarItem : MonoBehaviour
    {
        [Tooltip("Click target; auto-resolved from this object when empty.")] [SerializeField]
        private Button _button;

        [Tooltip("Optional label bound from the entry display name.")] [SerializeField]
        private TMP_Text _label;

        [Tooltip("Optional icon bound from the entry sprite (hidden when the entry has none).")] [SerializeField]
        private Image _icon;

        [Tooltip("Optional per-item selected visual (frame, underline). Toggled on selection change.")]
        [SerializeField]
        private GameObject _selectedVisual;

        /// <summary>Click target of this item (may be null when the item is purely decorative).</summary>
        public Button Button
        {
            get
            {
                ResolveButton();
                return _button;
            }
        }

        private void Awake()
        {
            ResolveButton();
        }

        private void OnValidate()
        {
            ResolveButton();
        }

        /// <summary>Applies entry content (label text, icon sprite) without touching layout.</summary>
        public void Bind(string displayName, Sprite icon)
        {
            if (_label != null)
            {
                _label.text = displayName ?? "";
            }

            if (_icon != null)
            {
                _icon.sprite = icon;
                _icon.enabled = icon != null;
            }
        }

        /// <summary>Shows/hides the optional selected visual.</summary>
        public void SetSelected(bool selected)
        {
            if (_selectedVisual != null)
            {
                _selectedVisual.SetActive(selected);
            }
        }

        /// <summary>Enables/disables the click target (disabled entries stay visible but inert).</summary>
        public void SetInteractable(bool interactable)
        {
            ResolveButton();
            if (_button != null)
            {
                _button.interactable = interactable;
            }
        }

        private void ResolveButton()
        {
            if (_button == null)
            {
                _button = GetComponent<Button>();
            }
        }
    }
}
