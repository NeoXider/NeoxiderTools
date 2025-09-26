using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;

namespace Neo
{
    namespace Tools
    {
        [AddComponentMenu("Neoxider/" + "Tools/" + nameof(InteractiveObject))]
        public class InteractiveObject : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler, IPointerClickHandler
        {
            public bool interactable = true;

            [SerializeField] private float doubleClickThreshold = 0.3f;

            [Space] public UnityEvent onHoverEnter;
            public UnityEvent onHoverExit;
            public UnityEvent onClick;
            public UnityEvent onDoubleClick;
            public UnityEvent onRightClick;

            private float clickTime = 0f;

            public void OnPointerEnter(PointerEventData eventData)
            {
                if (interactable)
                    onHoverEnter.Invoke();
            }

            public void OnPointerExit(PointerEventData eventData)
            {
                if (interactable)
                    onHoverExit.Invoke();
            }

            public void OnPointerClick(PointerEventData eventData)
            {
                if (!interactable) return;

                if (eventData.button == PointerEventData.InputButton.Left)
                {
                    if (Time.time - clickTime < doubleClickThreshold)
                        onDoubleClick.Invoke();
                    else
                        onClick.Invoke();

                    clickTime = Time.time;
                }
                else if (eventData.button == PointerEventData.InputButton.Right)
                {
                    onRightClick.Invoke();
                }
            }
        }
    }
}