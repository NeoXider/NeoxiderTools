using UnityEngine;
using UnityEngine.Events;

namespace Neo
{
    namespace Tools
    {
        [NeoDoc("Tools/UpdateChilds.md")]
        [CreateFromMenu("Neoxider/Tools/UpdateChilds")]
        [AddComponentMenu("Neoxider/" + "Tools/" + nameof(UpdateChilds))]
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
