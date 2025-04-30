using Neo.Audio;
using TMPro;
using UnityEngine;

namespace Neo
{
    namespace Demo
    {
        public class SwipeTextAudio : MonoBehaviour, ISwipeSubscriber
        {
            public TextMeshProUGUI text;

            public void SubscribeToSwipe(SwipeData swipeData)
            {
                text.text = swipeData.Direction.ToString();
            }

            private void OnValidate()
            {
                text = GetComponent<TextMeshProUGUI>();
            }
        }
    }
}
