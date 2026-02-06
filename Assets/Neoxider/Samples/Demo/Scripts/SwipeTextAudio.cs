using TMPro;
using UnityEngine;

namespace Neo
{
    namespace Demo
    {
        public class SwipeTextAudio : MonoBehaviour, ISwipeSubscriber
        {
            public TextMeshProUGUI text;

            private void OnValidate()
            {
                text = GetComponent<TextMeshProUGUI>();
            }

            public void SubscribeToSwipe(SwipeData swipeData)
            {
                text.text = swipeData.Direction.ToString();
            }
        }
    }
}