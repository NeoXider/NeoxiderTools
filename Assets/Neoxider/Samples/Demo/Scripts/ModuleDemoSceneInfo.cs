using UnityEngine;

namespace Neo.Samples
{
    [AddComponentMenu("Neoxider/Demo/Module Demo Scene Info")]
    public sealed class ModuleDemoSceneInfo : MonoBehaviour
    {
        [SerializeField] private string _moduleName = "Module";
        [SerializeField] private string _purpose = "Smoke demo scene.";
        [SerializeField] private string _runtimeApi = "Use the module runtime API from C#.";
        [SerializeField] private string _sceneWorkflow = "Use scene components as optional authoring wrappers.";
        [SerializeField] private string _verification = "Open scene and enter Play Mode.";
        [SerializeField] private bool _showRuntimeOverlay = true;

        public string ModuleName => _moduleName;
        public string Purpose => _purpose;
        public string RuntimeApi => _runtimeApi;
        public string SceneWorkflow => _sceneWorkflow;
        public string Verification => _verification;

        public void Configure(
            string moduleName,
            string purpose,
            string runtimeApi,
            string sceneWorkflow,
            string verification,
            bool showRuntimeOverlay = true)
        {
            _moduleName = moduleName;
            _purpose = purpose;
            _runtimeApi = runtimeApi;
            _sceneWorkflow = sceneWorkflow;
            _verification = verification;
            _showRuntimeOverlay = showRuntimeOverlay;
        }

        private void OnGUI()
        {
            if (!_showRuntimeOverlay)
            {
                return;
            }

            const float width = 620f;
            GUILayout.BeginArea(new Rect(20f, 20f, width, 210f), GUI.skin.window);
            GUILayout.Label(_moduleName, GUI.skin.box);
            GUILayout.Label(_purpose);
            GUILayout.Space(6f);
            GUILayout.Label("Runtime API: " + _runtimeApi);
            GUILayout.Label("Scene workflow: " + _sceneWorkflow);
            GUILayout.Label("Verification: " + _verification);
            GUILayout.EndArea();
        }
    }
}
