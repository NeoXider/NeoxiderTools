using UnityEditor;
using UnityEngine;

namespace Neo.Editor
{
    [FilePath("ProjectSettings/NeoInspectorSettings.asset", FilePathAttribute.Location.ProjectFolder)]
    internal sealed class NeoInspectorSettings : ScriptableSingleton<NeoInspectorSettings>
    {
        [SerializeField] private bool migratedFromEditorPrefs;

        [Header("Rainbow Effects - Text")] [SerializeField]
        private bool enableRainbowSignature = true;

        [SerializeField] private bool enableRainbowSignatureAnimation = true;

        [Header("Rainbow Effects - Line")] [SerializeField]
        private bool enableRainbowOutline = true;

        [SerializeField] private bool enableRainbowComponentOutline = true;

        [SerializeField] private bool enableRainbowLineAnimation = true;

        [Header("Rainbow Settings")] [SerializeField]
        private float rainbowSpeed = 0.1f;

        [SerializeField] private float rainbowSaturation = 0.8f;

        [SerializeField] private float rainbowBrightness = 1f;

        [SerializeField] private float rainbowOutlineSize = 1.5f;

        [SerializeField] private float rainbowOutlineAlpha = 0.6f;

        [SerializeField] private float rainbowComponentOutlineWidth = 2f;

        [Header("Header")] [SerializeField] private Color scriptNameColor = new(0.35f, 1f, 0.35f, 1f);

        [SerializeField] private int minFieldsForHeaderCategory = 3;

        public bool EnableRainbowSignature => enableRainbowSignature;
        public bool EnableRainbowSignatureAnimation => enableRainbowSignatureAnimation;
        public bool EnableRainbowOutline => enableRainbowOutline;
        public bool EnableRainbowComponentOutline => enableRainbowComponentOutline;
        public bool EnableRainbowLineAnimation => enableRainbowLineAnimation;

        public float RainbowSpeed => rainbowSpeed;
        public float RainbowSaturation => rainbowSaturation;
        public float RainbowBrightness => rainbowBrightness;
        public float RainbowOutlineSize => rainbowOutlineSize;
        public float RainbowOutlineAlpha => rainbowOutlineAlpha;
        public float RainbowComponentOutlineWidth => rainbowComponentOutlineWidth;
        public Color ScriptNameColor => scriptNameColor;
        public int MinFieldsForHeaderCategory => minFieldsForHeaderCategory;

        public void SetEnableRainbowSignature(bool value)
        {
            enableRainbowSignature = value;
            Save(true);
        }

        public void SetEnableRainbowSignatureAnimation(bool value)
        {
            enableRainbowSignatureAnimation = value;
            Save(true);
        }

        public void SetEnableRainbowOutline(bool value)
        {
            enableRainbowOutline = value;
            Save(true);
        }

        public void SetEnableRainbowComponentOutline(bool value)
        {
            enableRainbowComponentOutline = value;
            Save(true);
        }

        public void SetEnableRainbowLineAnimation(bool value)
        {
            enableRainbowLineAnimation = value;
            Save(true);
        }

        public void SetRainbowSpeed(float value)
        {
            rainbowSpeed = value;
            Save(true);
        }

        public void SetScriptNameColor(Color value)
        {
            scriptNameColor = value;
            Save(true);
        }

        public void SetMinFieldsForHeaderCategory(int value)
        {
            minFieldsForHeaderCategory = Mathf.Max(0, value);
            Save(true);
        }

        public void EnsureMigratedFromEditorPrefs()
        {
            if (migratedFromEditorPrefs)
            {
                return;
            }

            // Переносим старые значения из EditorPrefs (если они были заданы) в проектные настройки.
            bool hasAnyKey =
                EditorPrefs.HasKey("Neo.EnableRainbowSignature") ||
                EditorPrefs.HasKey("Neo.EnableRainbowSignatureAnimation") ||
                EditorPrefs.HasKey("Neo.EnableRainbowOutline") ||
                EditorPrefs.HasKey("Neo.EnableRainbowComponentOutline") ||
                EditorPrefs.HasKey("Neo.EnableRainbowLineAnimation") ||
                EditorPrefs.HasKey("Neo.RainbowSpeed") ||
                EditorPrefs.HasKey("Neo.RainbowSaturation") ||
                EditorPrefs.HasKey("Neo.RainbowBrightness") ||
                EditorPrefs.HasKey("Neo.RainbowOutlineSize") ||
                EditorPrefs.HasKey("Neo.RainbowOutlineAlpha") ||
                EditorPrefs.HasKey("Neo.RainbowComponentOutlineWidth");

            if (hasAnyKey)
            {
                enableRainbowSignature = EditorPrefs.GetBool("Neo.EnableRainbowSignature", enableRainbowSignature);
                enableRainbowSignatureAnimation =
                    EditorPrefs.GetBool("Neo.EnableRainbowSignatureAnimation", enableRainbowSignatureAnimation);

                enableRainbowOutline = EditorPrefs.GetBool("Neo.EnableRainbowOutline", enableRainbowOutline);
                enableRainbowComponentOutline =
                    EditorPrefs.GetBool("Neo.EnableRainbowComponentOutline", enableRainbowComponentOutline);
                enableRainbowLineAnimation =
                    EditorPrefs.GetBool("Neo.EnableRainbowLineAnimation", enableRainbowLineAnimation);

                rainbowSpeed = EditorPrefs.GetFloat("Neo.RainbowSpeed", rainbowSpeed);
                rainbowSaturation = EditorPrefs.GetFloat("Neo.RainbowSaturation", rainbowSaturation);
                rainbowBrightness = EditorPrefs.GetFloat("Neo.RainbowBrightness", rainbowBrightness);
                rainbowOutlineSize = EditorPrefs.GetFloat("Neo.RainbowOutlineSize", rainbowOutlineSize);
                rainbowOutlineAlpha = EditorPrefs.GetFloat("Neo.RainbowOutlineAlpha", rainbowOutlineAlpha);
                rainbowComponentOutlineWidth =
                    EditorPrefs.GetFloat("Neo.RainbowComponentOutlineWidth", rainbowComponentOutlineWidth);
            }

            migratedFromEditorPrefs = true;
            Save(true);
        }
    }
}