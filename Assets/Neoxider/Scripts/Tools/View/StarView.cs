using Neo.Reactive;
using Neo.UI;
using UnityEngine;

namespace Neo.Tools
{
    [NeoDoc("Tools/View/StarView.md")]
    [CreateFromMenu("Neoxider/Tools/View/StarView")]
    [AddComponentMenu("Neoxider/" + "Tools/" + nameof(StarView))]
    public class StarView : MonoBehaviour
    {
        [Header("GameObjects")] public GameObject[] starObjects;
        [FindInScene] public ScoreManager scoreManager;
        [Space] [Header("VisualToggle")] public VisualToggle[] stars;

        private ReactivePropertyInt _subscribedStars;

        public void Awake()
        {
            _subscribedStars?.RemoveListener(OnStarChange);
            _subscribedStars = null;
            if (scoreManager == null)
            {
                return;
            }

            _subscribedStars = scoreManager.CountStarsReactive;
            _subscribedStars?.AddListener(OnStarChange);
            OnStarChange(scoreManager.CountStars);
        }

        private void OnDestroy()
        {
            _subscribedStars?.RemoveListener(OnStarChange);
            _subscribedStars = null;
        }

        [Button]
        private void OnStarChange(int count)
        {
            if (stars != null)
            {
                for (int i = 0; i < stars.Length; i++)
                {
                    if (stars[i] != null)
                    {
                        stars[i].SetActive(i < count);
                    }
                }
            }

            if (starObjects != null)
            {
                for (int i = 0; i < starObjects.Length; i++)
                {
                    if (starObjects[i] != null)
                    {
                        starObjects[i].SetActive(i < count);
                    }
                }
            }
        }
    }
}
