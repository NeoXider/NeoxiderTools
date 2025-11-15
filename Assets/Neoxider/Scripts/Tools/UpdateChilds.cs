using UnityEngine;
using UnityEngine.Events;

namespace Neo
{
    namespace Tools
    {
        [AddComponentMenu("Neo/" + "Tools/" + nameof(UpdateChilds))]
        public class UpdateChilds : MonoBehaviour
        {
            public UnityEvent OnChangeChildsCount;

            private void OnTransformChildrenChanged()
            {
                OnChangeChildsCount?.Invoke();
            }
        }
    }
}