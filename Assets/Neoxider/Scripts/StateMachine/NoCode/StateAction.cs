using System;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.SceneManagement;

namespace Neo.StateMachine.NoCode
{
    /// <summary>
    ///     Base type for actions listed on StateData.
    ///     Actions run on enter, per-frame update, or exit depending on the list they belong to.
    /// </summary>
    /// <remarks>
    ///     Derive from this class and override Execute().
    ///     Configure instances on StateData assets in the Inspector.
    /// </remarks>
    /// <example>
    ///     <code>
    /// public class CustomAction : StateAction
    /// {
    ///     public override void Execute()
    ///     {
    ///         Debug.Log("Action executed!");
    ///     }
    /// }
    /// </code>
    /// </example>
    [Serializable]
    public abstract class StateAction
    {
        /// <summary>
        ///     Runs the action.
        /// </summary>
        public abstract void Execute();
    }

    /// <summary>
    ///     Logs a message to the Unity console.
    /// </summary>
    [Serializable]
    public class LogStateAction : StateAction
    {
        [SerializeField] private string message = "State Action Executed";

        [SerializeField] private LogType logType = LogType.Log;

        /// <summary>
        ///     Message text.
        /// </summary>
        public string Message
        {
            get => message;
            set => message = value;
        }

        /// <summary>
        ///     Log level (Log / Warning / Error).
        /// </summary>
        public LogType LogType
        {
            get => logType;
            set => logType = value;
        }

        public override void Execute()
        {
            switch (logType)
            {
                case LogType.Log:
                    Debug.Log($"[StateAction] {message}");
                    break;
                case LogType.Warning:
                    Debug.LogWarning($"[StateAction] {message}");
                    break;
                case LogType.Error:
                    Debug.LogError($"[StateAction] {message}");
                    break;
            }
        }
    }

    /// <summary>
    ///     Enables or disables a GameObject.
    /// </summary>
    [Serializable]
    public class SetGameObjectActiveAction : StateAction
    {
        [SerializeField] private GameObject target;

        [SerializeField] private bool setActive = true;

        /// <summary>
        ///     Target GameObject.
        /// </summary>
        public GameObject Target
        {
            get => target;
            set => target = value;
        }

        /// <summary>
        ///     Desired active state (true = active, false = inactive).
        /// </summary>
        public bool SetActive
        {
            get => setActive;
            set => setActive = value;
        }

        public override void Execute()
        {
            if (target != null)
            {
                target.SetActive(setActive);
            }
        }
    }

    /// <summary>
    ///     Invokes a UnityEvent.
    /// </summary>
    [Serializable]
    public class InvokeUnityEventAction : StateAction
    {
        [SerializeField] private UnityEvent unityEvent = new();

        /// <summary>
        ///     Event to invoke.
        /// </summary>
        public UnityEvent UnityEvent
        {
            get => unityEvent;
            set => unityEvent = value;
        }

        public override void Execute()
        {
            unityEvent?.Invoke();
        }
    }

    /// <summary>
    ///     Loads a scene by name or build index.
    /// </summary>
    [Serializable]
    public class ChangeSceneAction : StateAction
    {
        [SerializeField] private string sceneName = "";

        [SerializeField] private int sceneBuildIndex = -1;

        [SerializeField] private LoadSceneMode loadMode = LoadSceneMode.Single;

        /// <summary>
        ///     Scene name in Build Settings.
        /// </summary>
        public string SceneName
        {
            get => sceneName;
            set => sceneName = value;
        }

        /// <summary>
        ///     Scene build index (-1 to ignore).
        /// </summary>
        public int SceneBuildIndex
        {
            get => sceneBuildIndex;
            set => sceneBuildIndex = value;
        }

        /// <summary>
        ///     Single vs additive load mode.
        /// </summary>
        public LoadSceneMode LoadMode
        {
            get => loadMode;
            set => loadMode = value;
        }

        public override void Execute()
        {
            if (!string.IsNullOrEmpty(sceneName))
            {
                SceneManager.LoadScene(sceneName, loadMode);
            }
            else if (sceneBuildIndex >= 0)
            {
                SceneManager.LoadScene(sceneBuildIndex, loadMode);
            }
            else
            {
                Debug.LogWarning("[StateAction] ChangeSceneAction: No scene name or build index specified.");
            }
        }
    }
}
