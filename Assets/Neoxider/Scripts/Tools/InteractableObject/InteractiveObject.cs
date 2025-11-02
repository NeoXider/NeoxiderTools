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
            [Header("Event System")]
            [SerializeField] private bool _autoCheckEventSystem = true;

            public enum MouseButton
            {
                Left = 0,
                Right = 1,
                Middle = 2
            }
            
            private static bool physicsRaycasterEnsured3D;
            private static bool physicsRaycasterEnsured2D;
            private static bool eventSystemChecked;

            public bool interactable = true;

            [Header("Down/Up — Mouse binding")]
            [SerializeField] private MouseButton downUpMouseButton = MouseButton.Left;
            [SerializeField] private bool requireHoverForMouse = true;
            [Header("Down/Up — Keyboard binding")]
            [SerializeField] private KeyCode keyboardKey = KeyCode.E;
            [SerializeField] private bool requireHoverForKeyboard = true;

            [Header("Hover — enabled by default")] 
            [Space] public UnityEvent onHoverEnter;
            public UnityEvent onHoverExit;
            
            [Header("Clicks — always enabled")]
            [SerializeField] private float doubleClickThreshold = 0.3f;
            public UnityEvent onClick;
            public UnityEvent onDoubleClick;
            public UnityEvent onRightClick;
            public UnityEvent onMiddleClick;
            
            [Header("Down/Up events key")] 
            public UnityEvent onInteractDown;
            public UnityEvent onInteractUp;

            private float clickTime;
            private bool isHovered;
            private bool mouseHeldPrev;
            private bool keyHeldPrev;

            private void Awake()
            {
                CheckEventSystemOnce();
                TryEnsureNeededRaycasterOnce();
            }

            private void CheckEventSystemOnce()
            {
                if (!_autoCheckEventSystem || eventSystemChecked)
                    return;

                if (EventSystem.current == null && FindObjectOfType<EventSystem>() == null)
                {
                    Debug.LogWarning("InteractiveObject: EventSystem not found in scene");
                }

                eventSystemChecked = true;
            }

            private void TryEnsureNeededRaycasterOnce()
            {
                var cam = Camera.main;
                if (cam == null)
                {
                    Debug.LogWarning("InteractiveObject: Camera.main not found");
                    return;
                }

                // UI элементы обрабатываются GraphicRaycaster'ом — пропускаем
                bool isUI = GetComponentInParent<Canvas>() != null && TryGetComponent<RectTransform>(out _);
                if (isUI) return;

                if (TryGetComponent<Collider2D>(out _) && !physicsRaycasterEnsured2D)
                {
                    if (cam.GetComponent<Physics2DRaycaster>() == null)
                        cam.gameObject.AddComponent<Physics2DRaycaster>();
                    physicsRaycasterEnsured2D = true;
                }

                if (TryGetComponent<Collider>(out _) && !physicsRaycasterEnsured3D)
                {
                    if (cam.GetComponent<PhysicsRaycaster>() == null)
                        cam.gameObject.AddComponent<PhysicsRaycaster>();
                    physicsRaycasterEnsured3D = true;
                }
            }

            public void OnPointerClick(PointerEventData eventData)
            {
                if (!interactable) return;

                if (eventData.button == PointerEventData.InputButton.Left)
                {
                    if (doubleClickThreshold > 0f && (Time.time - clickTime) < doubleClickThreshold)
                        onDoubleClick.Invoke();
                    else
                        onClick.Invoke();

                    clickTime = Time.time;
                }
                else if (eventData.button == PointerEventData.InputButton.Right)
                {
                    onRightClick.Invoke();
                }
                else if (eventData.button == PointerEventData.InputButton.Middle)
                {
                    onMiddleClick.Invoke();
                }
            }

            public void OnPointerEnter(PointerEventData eventData)
            {
                if (interactable)
                {
                    isHovered = true;
                    onHoverEnter.Invoke();
                }
            }

            public void OnPointerExit(PointerEventData eventData)
            {
                if (interactable)
                {
                    isHovered = false;
                    onHoverExit.Invoke();
                }
            }

            private void Update()
            {
                if (!interactable)
                    return;

                // Mouse down/up detection (parallel to keyboard)
                int mouseIndex = (int)downUpMouseButton;
                bool mouseHeld = Input.GetMouseButton(mouseIndex);

                if (mouseHeld && !mouseHeldPrev)
                {
                    if (!requireHoverForMouse || isHovered)
                        onInteractDown?.Invoke();
                }
                else if (!mouseHeld && mouseHeldPrev)
                {
                    // Release fires regardless of hover to avoid stuck state
                    onInteractUp?.Invoke();
                }
                mouseHeldPrev = mouseHeld;

                // Keyboard down/up detection
                bool keyDown = Input.GetKeyDown(keyboardKey);
                bool keyUp = Input.GetKeyUp(keyboardKey);

                if (keyDown)
                {
                    if (!requireHoverForKeyboard || isHovered)
                        onInteractDown?.Invoke();
                }
                if (keyUp)
                {
                    onInteractUp?.Invoke();
                }
            }
        }
    }
}