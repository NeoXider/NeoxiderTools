using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Neo.Tools
{
    public class StarView : MonoBehaviour
    {
        [FindInScene] public ScoreManager scoreManager;
        [Space] [Header("ToggleView")] public Neo.UI.ToggleView[] stars;

        [Header("GameObjects")] public GameObject[] starObjects;

        public void Awake()
        {
            scoreManager.OnStarChange.AddListener(OnStarChange);
            OnStarChange(scoreManager.CountStars);
        }

#if ODIN_INSPECTOR
        [Sirenix.OdinInspector.Button]
#else
        [Button]
#endif
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