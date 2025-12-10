using Neo.UI;
using UnityEngine;
#if ODIN_INSPECTOR
#endif

namespace Neo.Tools
{
    [AddComponentMenu("Neo/" + "Tools/" + nameof(StarView))]
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
#if ODIN_INSPECTOR
        [Button]
#else
        [ButtonAttribute]
#endif
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