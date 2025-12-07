using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Text.RegularExpressions;
using UnityEngine;
using UnityEditor;

namespace Flexalon.Editor
{
    internal static class WindowUtil
    {
        private static readonly string _projectMeta = "5325f2ad02f14e242b86eb4bb0fcb5ee";

        private static string _version;

        private static Texture2D _flexalonIcon;
        private static Texture2D _proximaIcon;
        private static Texture2D _copilotIcon;

        public static void CenterOnEditor(EditorWindow window)
        {
#if UNITY_2020_1_OR_NEWER
            var main = EditorGUIUtility.GetMainWindowPosition();
            var pos = window.position;
            float w = (main.width - pos.width) * 0.5f;
            float h = (main.height - pos.height) * 0.5f;
            pos.x = main.x + w;
            pos.y = main.y + h;
            window.position = pos;
#endif
        }

        public static string GetVersion()
        {
            if (_version == null)
            {
                var version = AssetDatabase.GUIDToAssetPath(_projectMeta);
                var lines = File.ReadAllText(version);
                var rx = new Regex("\"version\": \"(.*?)\"");
                _version = rx.Match(lines).Groups[1].Value;
            }

            return _version;
        }

        public static bool DrawProximaButton(float width, GUIStyle style)
        {
            if (!_proximaIcon)
            {
                var proximaIconPath = AssetDatabase.GUIDToAssetPath("34efc6ae99ff42f438800428a52c50b5");
                _proximaIcon = AssetDatabase.LoadAssetAtPath<Texture2D>(proximaIconPath);
            }

            return GUILayout.Button(_proximaIcon, style, GUILayout.Width(width), GUILayout.Height(width * 0.337f));
        }

        public static bool DrawCopilotButton(float width, GUIStyle style)
        {
            if (!_copilotIcon)
            {
                var iconPath = AssetDatabase.GUIDToAssetPath("96aaefe6c810ba6469d7e7ce04421e94");
                _copilotIcon = AssetDatabase.LoadAssetAtPath<Texture2D>(iconPath);
            }

            return GUILayout.Button(_copilotIcon, style, GUILayout.Width(width), GUILayout.Height(width * 0.4023f));
        }

        public static List<string> GetInstalledLayouts()
        {
            var layouts = new List<string>();
            if (Assembly.GetAssembly(typeof(Flexalon)).GetType("Flexalon.FlexalonAlignLayout") != null)
            {
                layouts.Add("align");
            }

            if (Assembly.GetAssembly(typeof(Flexalon)).GetType("Flexalon.FlexalonCircleLayout") != null)
            {
                layouts.Add("circle");
            }

            if (Assembly.GetAssembly(typeof(Flexalon)).GetType("Flexalon.FlexalonConstraint") != null)
            {
                layouts.Add("constraint");
            }

            if (Assembly.GetAssembly(typeof(Flexalon)).GetType("Flexalon.FlexalonCurveLayout") != null)
            {
                layouts.Add("curve");
            }

            if (Assembly.GetAssembly(typeof(Flexalon)).GetType("Flexalon.FlexalonFlexibleLayout") != null)
            {
                layouts.Add("flexible");
            }

            if (Assembly.GetAssembly(typeof(Flexalon)).GetType("Flexalon.FlexalonGridLayout") != null)
            {
                layouts.Add("grid");
            }

            if (Assembly.GetAssembly(typeof(Flexalon)).GetType("Flexalon.FlexalonRandomLayout") != null)
            {
                layouts.Add("random");
            }

            if (Assembly.GetAssembly(typeof(Flexalon)).GetType("Flexalon.FlexalonShapeLayout") != null)
            {
                layouts.Add("shape");
            }

            return layouts;
        }

        public static bool AllLayoutsInstalled()
        {
            return GetInstalledLayouts().Count == 8;
        }
    }
}