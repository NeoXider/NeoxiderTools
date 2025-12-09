using System;
using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Neo
{
    namespace UI
    {
        public enum SceneLoadMode
        {
            /// <summary>
            ///     Загружает сцену синхронно.
            /// </summary>
            Sync,

            /// <summary>
            ///     Загружает сцену асинхронно и активирует сразу.
            /// </summary>
            Async,

            /// <summary>
            ///     Загружает сцену асинхронно, но ждёт ручной активации.
            /// </summary>
            AsyncManual
        }

        [AddComponentMenu("Neo/" + "UI/" + nameof(UIReady))]
        public class UIReady : MonoBehaviour
        {
            [Header("Scene Loading")] public SceneLoadMode loadMode = SceneLoadMode.Sync;

            [Header("Async Load Scene")] public AsyncLoadScene ALS;

            private void Update()
            {
                if (Input.GetKeyDown(KeyCode.Space)
                    || Input.GetKeyDown(KeyCode.KeypadEnter))
                {
                    ProceedScene();
                }
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
                int idScene = SceneManager.GetActiveScene().buildIndex;
                LoadScene(idScene);
            }

            public void Pause(bool activ)
            {
                if (activ)
                {
                    Time.timeScale = 0;
                }
                else
                {
                    Time.timeScale = 1.0f;
                }
            }

            /// <summary>
            ///     Загружает сцену согласно выбранному режиму.
            /// </summary>
            public void LoadScene(int idScene)
            {
                switch (loadMode)
                {
                    case SceneLoadMode.Sync:
                        SceneManager.LoadScene(idScene);
                        break;
                    case SceneLoadMode.Async:
                        StartCoroutine(LoadSceneCoroutine(idScene, true));
                        break;
                    case SceneLoadMode.AsyncManual:
                        StartCoroutine(LoadSceneCoroutine(idScene, false));
                        break;
                }
            }

            /// <summary>
            ///     Загружает сцену синхронно.
            /// </summary>
            public void LoadSceneSync(int idScene)
            {
                SceneManager.LoadScene(idScene);
            }

            /// <summary>
            ///     Загружает сцену асинхронно с автоактивацией.
            /// </summary>
            public void LoadSceneAsync(int idScene)
            {
                StartCoroutine(LoadSceneCoroutine(idScene, true));
            }

            /// <summary>
            ///     Загружает сцену асинхронно без автоактивации (требует вызова ProceedScene).
            /// </summary>
            public void LoadSceneAsyncManual(int idScene)
            {
                StartCoroutine(LoadSceneCoroutine(idScene, false));
            }

            /// <summary>
            ///     Активирует сцену после асинхронной загрузки без автоактивации.
            /// </summary>
            public void ProceedScene()
            {
                if (ALS.operationScene != null)
                {
                    ALS.operationScene.allowSceneActivation = true;
                }
            }

            private IEnumerator LoadSceneCoroutine(int idScene, bool autoActivate)
            {
                ALS.operationScene = SceneManager.LoadSceneAsync(idScene);
                ALS.operationScene.allowSceneActivation = autoActivate;

                if (ALS.gameObjectLoad != null)
                {
                    ALS.gameObjectLoad.SetActive(true);
                }

                if (ALS.animator != null)
                {
                    ALS.animator.enabled = true;
                }

                while (!ALS.operationScene.isDone)
                {
                    ALS.progress = ALS.operationScene.progress;

                    if (ALS.textProgress != null)
                    {
                        if (ALS.progress > 0.89)
                        {
                            ALS.textProgress.text = ALS.loadEndText[1];
                        }
                        else
                        {
                            ALS.textProgress.text = ALS.loadEndText[0] + (int)(ALS.progress * 100);
                        }
                    }

                    yield return null;
                }

                if (ALS.animator != null)
                {
                    ALS.animator.enabled = false;
                }

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