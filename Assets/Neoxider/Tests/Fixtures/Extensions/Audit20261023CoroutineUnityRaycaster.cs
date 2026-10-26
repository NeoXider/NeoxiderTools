using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;

namespace Neo.Editor.Tests
{
    public sealed class Audit20261023CoroutineUnityRaycaster : BaseRaycaster
    {
        public GameObject Target { get; set; }
        public Vector2 LastPosition { get; private set; }
        public override Camera eventCamera => null;

        public override void Raycast(PointerEventData eventData, List<RaycastResult> resultAppendList)
        {
            LastPosition = eventData.position;
            if (new Rect(70f, 50f, 20f, 20f).Contains(eventData.position))
            {
                resultAppendList.Add(new RaycastResult
                {
                    gameObject = Target,
                    module = this,
                    screenPosition = eventData.position,
                    distance = 0f
                });
            }
        }
    }
}
