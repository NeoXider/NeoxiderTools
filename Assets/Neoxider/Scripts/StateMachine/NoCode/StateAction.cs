using System;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.SceneManagement;

namespace Neo.StateMachine.NoCode
{
    /// <summary>
    ///     Базовый класс для действий в NoCode состояниях.
    ///     Действия выполняются при входе, обновлении или выходе из состояния.
    /// </summary>
    /// <remarks>
    ///     Все действия должны наследоваться от этого класса и реализовывать метод Execute().
    ///     Действия используются в StateData для визуального создания логики состояний без кода.
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
        ///     Выполнить действие.
        /// </summary>
        public abstract void Execute();
    }

    /// <summary>
    ///     Действие для логирования сообщения.
    /// </summary>
    [Serializable]
    public class LogStateAction : StateAction
    {
        [SerializeField]
        private string message = "State Action Executed";

        [SerializeField]
        private LogType logType = LogType.Log;

        /// <summary>
        ///     Сообщение для логирования.
        /// </summary>
        public string Message
        {
            get => message;
            set => message = value;
        }

        /// <summary>
        ///     Тип логирования.
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
    ///     Действие для включения/выключения GameObject.
    /// </summary>
    [Serializable]
    public class SetGameObjectActiveAction : StateAction
    {
        [SerializeField]
        private GameObject target;

        [SerializeField]
        private bool setActive = true;

        /// <summary>
        ///     Целевой GameObject.
        /// </summary>
        public GameObject Target
        {
            get => target;
            set => target = value;
        }

        /// <summary>
        ///     Установить активность (true = включить, false = выключить).
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
    ///     Действие для вызова UnityEvent.
    /// </summary>
    [Serializable]
    public class InvokeUnityEventAction : StateAction
    {
        [SerializeField]
        private UnityEvent unityEvent = new UnityEvent();

        /// <summary>
        ///     UnityEvent для вызова.
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
    ///     Действие для смены сцены.
    /// </summary>
    [Serializable]
    public class ChangeSceneAction : StateAction
    {
        [SerializeField]
        private string sceneName = "";

        [SerializeField]
        private int sceneBuildIndex = -1;

        [SerializeField]
        private LoadSceneMode loadMode = LoadSceneMode.Single;

        /// <summary>
        ///     Имя сцены для загрузки.
        /// </summary>
        public string SceneName
        {
            get => sceneName;
            set => sceneName = value;
        }

        /// <summary>
        ///     Индекс сцены в Build Settings.
        /// </summary>
        public int SceneBuildIndex
        {
            get => sceneBuildIndex;
            set => sceneBuildIndex = value;
        }

        /// <summary>
        ///     Режим загрузки сцены.
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


