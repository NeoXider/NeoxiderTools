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
            ///     Loads the scene synchronously.
            /// </summary>
            Sync,

            /// <summary>
            ///     Loads the scene asynchronously and activates immediately.
            /// </summary>
            Async,

            /// <summary>
            ///     Loads the scene asynchronously but waits for manual activation.
            /// </summary>
            AsyncManual
        }

        [LegacyComponent("Neo.Level.SceneFlowController")]
        [Obsolete("Use SceneFlowController for new code. UIReady remains functional but is deprecated.")]
        [NeoDoc("UI/UIReady.md")]
        [CreateFromMenu("Neoxider/UI/UIReady")]
        [AddComponentMenu("Neoxider/UI/UIReady (Legacy)")]
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
            ///     Loads the scene using the selected mode.
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
            ///     Loads the scene synchronously.
            /// </summary>
            public void LoadSceneSync(int idScene)
            {
                SceneManager.LoadScene(idScene);
            }

            /// <summary>
            ///     Loads the scene asynchronously with auto-activation.
            /// </summary>
            public void LoadSceneAsync(int idScene)
            {
                StartCoroutine(LoadSceneCoroutine(idScene, true));
            }

            /// <summary>
            ///     Loads the scene asynchronously without auto-activation (call ProceedScene to activate).
            /// </summary>
            public void LoadSceneAsyncManual(int idScene)
            {
                StartCoroutine(LoadSceneCoroutine(idScene, false));
            }

            /// <summary>
            ///     Activates the scene after async load when auto-activation was disabled.
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

                Debug.Log("[UIReady] Scene loaded and activated.");
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
