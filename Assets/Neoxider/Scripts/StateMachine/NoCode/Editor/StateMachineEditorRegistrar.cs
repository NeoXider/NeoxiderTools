using UnityEditor;
using UnityEngine;

namespace Neo.StateMachine.NoCode.Editor
{
    /// <summary>
    ///     Единая точка регистрации для всех редакторов State Machine.
    ///     Обеспечивает централизованную инициализацию и управление редакторами.
    /// </summary>
    [InitializeOnLoad]
    public static class StateMachineEditorRegistrar
    {
        static StateMachineEditorRegistrar()
        {
            EditorApplication.delayCall += OnEditorReady;
        }

        /// <summary>
        ///     Получить статус инициализации.
        /// </summary>
        public static bool IsInitialized { get; private set; }

        private static void OnEditorReady()
        {
            if (IsInitialized)
            {
                return;
            }

            Initialize();
            IsInitialized = true;
        }

        /// <summary>
        ///     Инициализировать все редакторы State Machine.
        /// </summary>
        private static void Initialize()
        {
            // Регистрация происходит автоматически через атрибуты [CustomEditor]
            // Здесь можно добавить дополнительную логику инициализации

            // Проверяем, что все необходимые компоненты зарегистрированы
            ValidateRegistration();
        }

        /// <summary>
        ///     Проверить корректность регистрации редакторов.
        /// </summary>
        private static void ValidateRegistration()
        {
            // Проверка регистрации CustomEditor для StateMachineData
            StateMachineData testData = ScriptableObject.CreateInstance<StateMachineData>();
            UnityEditor.Editor stateMachineDataEditor = UnityEditor.Editor.CreateEditor(testData);
            if (stateMachineDataEditor == null || !(stateMachineDataEditor is StateMachineDataEditor))
            {
                Debug.LogWarning(
                    "[StateMachineEditorRegistrar] StateMachineDataEditor не зарегистрирован корректно.");
            }
            else
            {
                Object.DestroyImmediate(stateMachineDataEditor);
            }

            Object.DestroyImmediate(testData);
        }
    }
}