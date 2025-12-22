using System.Reflection;
using UnityEditor;
using UnityEngine;

namespace Neo.Editor
{
    public static class NeoxiderModuleInspectorHeader
    {
        public static void Draw(Assembly assembly, string fallbackDisplayName = null)
        {
            string title = fallbackDisplayName ?? "Neoxider Module";
            string version = "Unknown";
            string root = null;
            string stateKeyPrefix = null;

            if (NeoxiderModulePackageInfoUtility.TryGetForAssembly(assembly, out NeoxiderModulePackageInfo info))
            {
                if (!string.IsNullOrEmpty(info.DisplayName) && info.DisplayName != "Unknown")
                {
                    title = info.DisplayName;
                }

                if (!string.IsNullOrEmpty(info.Version))
                {
                    version = info.Version;
                }

                root = info.RootPath;
                stateKeyPrefix = string.IsNullOrEmpty(info.Name) ? null : info.Name;
            }

            using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox))
            {
                using (new EditorGUILayout.HorizontalScope())
                {
                    GUIStyle titleStyle = new(EditorStyles.boldLabel)
                    {
                        fontSize = 14,
                        alignment = TextAnchor.MiddleLeft
                    };

                    GUILayout.Label(title, titleStyle);
                    GUILayout.FlexibleSpace();

                    GUIStyle versionStyle = new(EditorStyles.centeredGreyMiniLabel)
                    {
                        fontSize = 12,
                        alignment = TextAnchor.MiddleRight
                    };

                    GUILayout.Label($"v{version}", versionStyle);
                }

                if (!string.IsNullOrEmpty(root))
                {
                    GUILayout.Label(root, EditorStyles.miniLabel);
                }
            }

            DrawUpdateStatus(stateKeyPrefix, version, root);
            EditorGUILayout.Space(4);
        }

        private static void DrawUpdateStatus(string stateKeyPrefix, string currentVersion, string packageRootPath)
        {
            if (string.IsNullOrEmpty(currentVersion) || string.IsNullOrEmpty(packageRootPath))
            {
                return;
            }

            // IMPORTANT: no background logic on repaint. Only read cached state.
            NeoxiderUpdateChecker.State s = NeoxiderUpdateChecker.Peek(stateKeyPrefix);

            using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox))
            {
                using (new EditorGUILayout.HorizontalScope())
                {
                    GUIContent refreshContent = EditorGUIUtility.IconContent("d_Refresh") ?? new GUIContent("⟳");
                    if (GUILayout.Button(refreshContent, GUILayout.Width(22), GUILayout.Height(18)))
                    {
                        NeoxiderUpdateChecker.RequestImmediateCheck(stateKeyPrefix, currentVersion, packageRootPath);
                        GUI.FocusControl(null);
                    }

                    string label;
                    Color color;

                    switch (s.Status)
                    {
                        case NeoxiderUpdateChecker.UpdateStatus.Checking:
                            label = "Проверка обновлений…";
                            color = new Color(0.25f, 0.75f, 1f, 1f);
                            break;

                        case NeoxiderUpdateChecker.UpdateStatus.UpToDate:
                            label = "Актуальная версия";
                            color = new Color(0.35f, 1f, 0.35f, 1f);
                            break;

                        case NeoxiderUpdateChecker.UpdateStatus.Ahead:
                            label = !string.IsNullOrEmpty(s.LatestVersion)
                                ? $"Выше опубликованной (последняя: {s.LatestVersion})"
                                : "Выше опубликованной";
                            color = new Color(1f, 0.75f, 0.25f, 1f);
                            break;

                        case NeoxiderUpdateChecker.UpdateStatus.UpdateAvailable:
                            label = !string.IsNullOrEmpty(s.LatestVersion)
                                ? $"Есть обновление: {s.LatestVersion}"
                                : "Есть обновление";
                            color = new Color(1f, 0.25f, 0.25f, 1f);
                            break;

                        default:
                            label = "Статус обновлений: Unknown (нажми ⟳)";
                            color = new Color(1f, 1f, 1f, 0.7f);
                            break;
                    }

                    GUIStyle statusStyle = new(EditorStyles.miniBoldLabel)
                    {
                        normal = { textColor = color }
                    };

                    GUILayout.Label(label, statusStyle);
                    GUILayout.FlexibleSpace();

                    if (s.Status == NeoxiderUpdateChecker.UpdateStatus.UpdateAvailable &&
                        !string.IsNullOrEmpty(s.UpdateUrl) &&
                        GUILayout.Button("Открыть", GUILayout.Width(70), GUILayout.Height(20)))
                    {
                        Application.OpenURL(s.UpdateUrl);
                    }
                }

                if (!string.IsNullOrEmpty(s.Error))
                {
                    EditorGUILayout.LabelField(s.Error, EditorStyles.miniLabel);
                }
            }
        }
    }
}