using System;
using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Neo
{
    namespace UI
    {
        [AddComponentMenu("Neoxider/" + "UI/" + nameof(UIReady))]
        public class UIReady : MonoBehaviour
        {
            public int s = 10;

            [Header("Async Load Scene")] public AsyncLoadScene ALS;

            private void Update()
            {
                if (Input.GetKeyDown(KeyCode.Space)
                    || Input.GetKeyDown(KeyCode.KeypadEnter))
                    ProceedScene();
            }

            private void OnValidate()
            {
                name = nameof(UIReady);
#if UNITY_2023_1_OR_NEWER
#else
#endif
            }

            public void Quit()
            {
                Application.Quit();
            }

            public void Restart()
            {
                var idScene = SceneManager.GetActiveScene().buildIndex;
                LoadScene(idScene);
            }

            public void Pause(bool activ)
            {
                if (activ)
                    Time.timeScale = 0;
                else
                    Time.timeScale = 1.0f;
            }

            public void LoadScene(int idScene)
            {
                SceneManager.LoadScene(idScene);
            }

            public void LoadSceneAsync(int idScene)
            {
                StartCoroutine(LoadSceneCoroutine(idScene));
            }

            public void ProceedScene()
            {
                if (ALS.operationScene != null)
                    ALS.operationScene.allowSceneActivation = true;
            }

            private IEnumerator LoadSceneCoroutine(int idScene)
            {
                ALS.operationScene = SceneManager.LoadSceneAsync(idScene);
                ALS.operationScene.allowSceneActivation = ALS.isProgressLoad;

                if (ALS.gameObjectLoad != null)
                    ALS.gameObjectLoad.SetActive(true);

                if (ALS.animator != null)
                    ALS.animator.enabled = true;

                while (!ALS.operationScene.isDone)
                {
                    ALS.progress = ALS.operationScene.progress;

                    if (ALS.textProgress != null)
                    {
                        if (ALS.progress > 0.89)
                            ALS.textProgress.text = ALS.loadEndText[1];
                        else
                            ALS.textProgress.text = ALS.loadEndText[0] + ((int)(ALS.progress * 100));
                    }

                    yield return null;
                }

                if (ALS.animator != null)
                    ALS.animator.enabled = false;

                Debug.Log("����� ��������� � ������������!");
            }

            [Serializable]
            public class AsyncLoadScene
            {
                public GameObject gameObjectLoad;
                public Animator animator;
                public TextMeshProUGUI textProgress;
                public string[] loadEndText = { "Loading... ", "Click a start" };
                public float progress;
                public bool isProgressLoad;
                public AsyncOperation operationScene;
            }
        }
    }
}