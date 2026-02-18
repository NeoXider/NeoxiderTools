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

        public void Awake()
        {
            scoreManager.OnStarChange.AddListener(OnStarChange);
            OnStarChange(scoreManager.CountStars);
        }

        [Button]
        private void OnStarChange(int count)
        {
            if (stars != null)
            {
                for (int i = 0; i < stars.Length; i++)
                {
                    stars[i].SetActive(i < count);
                }
            }

            if (starObjects != null)
            {
                for (int i = 0; i < starObjects.Length; i++)
                {
                    starObjects[i].SetActive(i < count);
                }
            }
        }
    }
}