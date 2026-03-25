using UnityEditor;
using UnityEngine;

namespace Neo.StateMachine.NoCode.Editor
{
    /// <summary>
    ///     Central registration hook for State Machine custom editors.
    /// </summary>
    [InitializeOnLoad]
    public static class StateMachineEditorRegistrar
    {
        static StateMachineEditorRegistrar()
        {
            EditorApplication.delayCall += OnEditorReady;
        }

        /// <summary>
        ///     True after the first successful initialization pass.
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
        ///     Runs one-time editor-side setup.
        /// </summary>
        private static void Initialize()
        {
            // [CustomEditor] types register themselves; extend here if you add manual wiring.

            ValidateRegistration();
        }

        /// <summary>
        ///     Sanity-checks that expected custom editors resolve.
        /// </summary>
        private static void ValidateRegistration()
        {
            // Ensure StateMachineData gets StateMachineDataEditor
            StateMachineData testData = ScriptableObject.CreateInstance<StateMachineData>();
            var stateMachineDataEditor = UnityEditor.Editor.CreateEditor(testData);
            if (stateMachineDataEditor == null || !(stateMachineDataEditor is StateMachineDataEditor))
            {
                Debug.LogWarning(
                    "[StateMachineEditorRegistrar] StateMachineDataEditor is not registered correctly.");
            }
            else
            {
                Object.DestroyImmediate(stateMachineDataEditor);
            }

            Object.DestroyImmediate(testData);
        }
    }
}
