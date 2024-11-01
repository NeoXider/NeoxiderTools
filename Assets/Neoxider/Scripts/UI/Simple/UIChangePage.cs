using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace Neoxider
{
    namespace UI
    {
        public class UIChangePage : MonoBehaviour, IPointerClickHandler
        {
            [SerializeField] private Image _imageTarget;
            [SerializeField] private int _idPage;
            [SerializeField] private bool _onePage = false;

            public void OnPointerClick(PointerEventData eventData)
            {
                if (_onePage) SimpleUI.Instance?.SetOnePage(_idPage);
                else SimpleUI.Instance?.SetPage(_idPage);
            }

            private void OnValidate()
            {
                _imageTarget ??= GetComponent<Image>();
            }
        }
    }
}