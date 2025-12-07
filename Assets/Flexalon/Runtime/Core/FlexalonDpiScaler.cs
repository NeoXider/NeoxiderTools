#if UNITY_UI
using System.Runtime.InteropServices;
using UnityEngine;
using UnityEngine.UI;

namespace Flexalon
{
    /// <summary>
    /// This class is used to set the scale a canvas based on the pixel density of the screen.
    /// </summary>
    [ExecuteAlways]
    public class FlexalonDpiScaler : MonoBehaviour
    {
        private CanvasScaler _canvasScaler;

        private void OnEnable()
        {
#if UNITY_EDITOR && UNITY_2021_3_OR_NEWER
            if (UnityEditor.SceneManagement.PrefabStageUtility.GetPrefabStage(gameObject))
            {
                return;
            }
#endif

            if (TryGetComponent<CanvasScaler>(out var canvasScaler))
            {
                _canvasScaler = canvasScaler;
                _canvasScaler.uiScaleMode = CanvasScaler.ScaleMode.ConstantPixelSize;
                InvokeRepeating(nameof(UpdateScale), 0f, 1f);
            }
        }

        private void OnDisable()
        {
            CancelInvoke(nameof(UpdateScale));
        }

       private void UpdateScale()
       {
            _canvasScaler.scaleFactor = GetPixelDensity();
       }

#if PLATFORM_STANDALONE_OSX && !UNITY_EDITOR
        [DllImport("__Internal", EntryPoint = "getPixelDensity")]
        public static extern float GetPixelDensity();
#else
        public static float GetPixelDensity()
        {
            var dpi = Screen.dpi;
            if (dpi < 1)
            {
                dpi = 96;
            }

            var scale = dpi / 96f;
#if UNITY_EDITOR_OSX // Retina display fix for the editor
            scale *= 0.75f;
#endif
            return scale;
        }
#endif
    }
}
#endif