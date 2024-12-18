using UnityEngine;
using UnityEngine.Events;

namespace Neoxider
{
    namespace Tools
    {
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
