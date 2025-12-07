using System.Collections.Generic;
using UnityEngine;

namespace Flexalon
{
    internal class FlexalonRaycaster
    {
        public Vector3 hitPosition;

        private int _raycastFrame = 0;
        private FlexalonInteractable _hitInteractable;
        private readonly Dictionary<GameObject, FlexalonInteractable> _handles = new Dictionary<GameObject, FlexalonInteractable>();

#if UNITY_UI
        private List<UnityEngine.EventSystems.RaycastResult> _graphicRaycastResult = new List<UnityEngine.EventSystems.RaycastResult>();
#endif

#if UNITY_PHYSICS
        private RaycastHit[] _raycastHits = new RaycastHit[10];
#endif

        public void Register(FlexalonInteractable interactable)
        {
            _handles.Add(interactable.Handle, interactable);
        }

        public void Unregister(FlexalonInteractable interactable)
        {
            _handles.Remove(interactable.Handle);
        }

        public bool IsHit(Vector3 uiPointer, Ray ray, FlexalonInteractable interactable)
        {
            // Check if we've already casted this frame.
            if (_raycastFrame != Time.frameCount)
            {
                _hitInteractable = null;
                _raycastFrame = Time.frameCount;
                float minDistance = float.MaxValue;
                RaycastUI(uiPointer, ref minDistance);
                RaycastPhysics(ray, ref minDistance);
            }

            return _hitInteractable == interactable;
        }

        private void RaycastUI(Vector3 uiPointer, ref float minDistance)
        {
#if UNITY_UI
            var eventSystem = UnityEngine.EventSystems.EventSystem.current;
            if (eventSystem)
            {
                eventSystem.RaycastAll(new UnityEngine.EventSystems.PointerEventData(eventSystem)
                {
                    position = uiPointer
                }, _graphicRaycastResult);

                for (int i = 0; i < _graphicRaycastResult.Count; i++)
                {
                    var hit = _graphicRaycastResult[i];
                    if (hit.distance < minDistance)
                    {
                        if (_handles.TryGetValue(hit.gameObject, out var hitInteractable))
                        {
                            _hitInteractable = hitInteractable;
                            minDistance = hit.distance;

                            hitInteractable.UpdateCanvas();

                            if (hitInteractable.Canvas && hitInteractable.Canvas.renderMode == RenderMode.ScreenSpaceOverlay)
                            {
                                hitPosition = hit.screenPosition;
                            }
                            else
                            {
                                hitPosition = hit.worldPosition;
                            }
                        }
                    }
                }
            }
#endif
        }

        private void RaycastPhysics(Ray ray, ref float minDistance)
        {
#if UNITY_PHYSICS
            int hits = Physics.RaycastNonAlloc(ray, _raycastHits, 1000);

            // Find the nearest hit interactable.
            for (int i = 0; i < hits; i++)
            {
                var hit = _raycastHits[i];
                if (hit.distance < minDistance && _handles.TryGetValue(hit.collider.gameObject, out var hitInteractable))
                {
                    _hitInteractable = hitInteractable;
                    minDistance = hit.distance;
                    hitPosition = hit.point;
                }
            }
#endif
        }
    }
}