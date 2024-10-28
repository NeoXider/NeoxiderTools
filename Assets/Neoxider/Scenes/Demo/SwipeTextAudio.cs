using Neoxider.Audio;
using TMPro;
using UnityEngine;

namespace Neoxider
{
public class SwipeTextAudio : MonoBehaviour, ISwipeSubscriber
{
        public TextMeshProUGUI text;

        public void SubscribeToSwipe(SwipeData swipeData)
        {
            AudioManager.PlaySound(ClipType.jump);
            text.text = swipeData.Direction.ToString();
        }

        private void OnValidate()
        {
            text = GetComponent<TextMeshProUGUI>();
        }
    }
}
