using UnityEngine;
using UnityEngine.Events;

namespace Neo.Tools
{
    public class DistanceChecker : MonoBehaviour
    {
        [GetComponent] public Transform currentObject;
        public Transform targetObject;
        public float distanceThreshold = 5.0f;

        public UnityEvent onApproach;
        public UnityEvent onDepart;

        private bool isWithinDistance;

        private void Update()
        {
            if (currentObject == null || targetObject == null)
                return;

            var distance = Vector3.Distance(currentObject.position, targetObject.position);

            if (distance < distanceThreshold && !isWithinDistance)
            {
                isWithinDistance = true;
                onApproach?.Invoke();
            }
            else if (distance >= distanceThreshold && isWithinDistance)
            {
                isWithinDistance = false;
                onDepart?.Invoke();
            }
        }
    }
}