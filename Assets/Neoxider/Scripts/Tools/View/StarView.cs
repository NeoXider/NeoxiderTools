using Neo.UI;
#if ODIN_INSPECTOR
using Sirenix.OdinInspector;
#endif

using UnityEngine;

namespace Neo.Tools
{
    public class StarView : MonoBehaviour
    {
        [Header("GameObjects")] public GameObject[] starObjects;
        [FindInScene] public ScoreManager scoreManager;
        [Space] [Header("ToggleView")] public ToggleView[] stars;

        public void Awake()
        {
            scoreManager.OnStarChange.AddListener(OnStarChange);
            OnStarChange(scoreManager.CountStars);
        }


        [Button]
        private void OnStarChange(int count)
        {
            if (stars != null)
                for (var i = 0; i < stars.Length; i++)
                    stars[i].Set(i < count);

            if (starObjects != null)
                for (var i = 0; i < starObjects.Length; i++)
                    starObjects[i].SetActive(i < count);
        }
    }
}