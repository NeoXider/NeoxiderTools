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

            DrawModuleBanner(title, version, root);
            DrawUpdateStatus(stateKeyPrefix, version, root);
            EditorGUILayout.Space(4);
        }

        private static void DrawModuleBanner(string title, string version, string root)
        {
            bool hasRoot = !string.IsNullOrEmpty(root);
            float height = hasRoot ? 50f : 42f;

            Rect full = GUILayoutUtility.GetRect(0f, height, GUILayout.ExpandWidth(true));
            Rect rect = new(full.x + 1f, full.y, full.width - 2f, full.height);

            NeoInspectorTheme.DrawRoundedTexture(rect, NeoInspectorTheme.BannerGradient,
                new Color(1f, 1f, 1f, 0.16f), NeoInspectorTheme.RadiusCard, Color.white, 1f);

            const float pad = 12f;

            string versionText = $"v{version}";
            GUIStyle pillTextStyle = new(EditorStyles.boldLabel)
            {
                fontSize = 11,
                alignment = TextAnchor.MiddleCenter
            };
            float pillW = Mathf.Max(42f, pillTextStyle.CalcSize(new GUIContent(versionText)).x + 18f);
            const float pillH = 20f;
            Rect pillRect = new(rect.xMax - pad - pillW, rect.y + (height - pillH) * 0.5f, pillW, pillH);
            NeoInspectorTheme.DrawRoundedRect(pillRect, new Color(1f, 1f, 1f, 0.17f),
                new Color(1f, 1f, 1f, 0.28f), NeoInspectorTheme.RadiusPill, 1f);
            pillTextStyle.normal.textColor = NeoInspectorTheme.OnAccentText;
            GUI.Label(pillRect, versionText, pillTextStyle);

            float textW = Mathf.Max(20f, pillRect.x - (rect.x + pad) - 8f);
            GUIStyle titleStyle = new(EditorStyles.boldLabel)
            {
                fontSize = 15,
                alignment = hasRoot ? TextAnchor.LowerLeft : TextAnchor.MiddleLeft,
                clipping = TextClipping.Clip,
                normal = { textColor = new Color(1f, 1f, 1f, 0.98f) }
            };

            if (hasRoot)
            {
                GUI.Label(new Rect(rect.x + pad, rect.y + 8f, textW, 20f), title, titleStyle);
                GUIStyle rootStyle = new(EditorStyles.miniLabel)
                {
                    alignment = TextAnchor.UpperLeft,
                    clipping = TextClipping.Clip,
                    normal = { textColor = new Color(1f, 1f, 1f, 0.72f) }
                };
                GUI.Label(new Rect(rect.x + pad, rect.y + 27f, textW, 16f), root, rootStyle);
            }
            else
            {
                GUI.Label(new Rect(rect.x + pad, rect.y, textW, height), title, titleStyle);
            }
        }

        private static void DrawUpdateStatus(string stateKeyPrefix, string currentVersion, string packageRootPath)
        {
            if (string.IsNullOrEmpty(currentVersion) || string.IsNullOrEmpty(packageRootPath))
            {
                return;
            }

            // WHY: no background logic on repaint. Only read cached state.
            NeoxiderUpdateChecker.State s = NeoxiderUpdateChecker.Peek(stateKeyPrefix);

            EditorGUILayout.Space(4f);

            const float h = 26f;
            Rect full = GUILayoutUtility.GetRect(0f, h, GUILayout.ExpandWidth(true));
            Rect rect = new(full.x + 1f, full.y, full.width - 2f, full.height);

            NeoInspectorTheme.DrawRoundedRect(rect, NeoInspectorTheme.PanelBackground,
                NeoInspectorTheme.Separator, NeoInspectorTheme.RadiusRow, 1f);

            const float pad = 6f;
            Rect refreshRect = new(rect.x + pad, rect.y + (h - 18f) * 0.5f, 24f, 18f);
            GUIContent refreshContent = EditorGUIUtility.IconContent("d_Refresh") ?? new GUIContent("⟳");
            if (GUI.Button(refreshRect, refreshContent, EditorStyles.miniButton))
            {
                NeoxiderUpdateChecker.RequestImmediateCheck(stateKeyPrefix, currentVersion, packageRootPath);
                GUI.FocusControl(null);
            }

            string label;
            Color color;

            switch (s.Status)
            {
                case NeoxiderUpdateChecker.UpdateStatus.Checking:
                    label = "Checking for updates…";
                    color = new Color(0.40f, 0.72f, 1f, 1f);
                    break;

                case NeoxiderUpdateChecker.UpdateStatus.UpToDate:
                    label = "Up to date";
                    color = new Color(0.35f, 0.85f, 0.48f, 1f);
                    break;

                case NeoxiderUpdateChecker.UpdateStatus.Ahead:
                    label = !string.IsNullOrEmpty(s.LatestVersion)
                        ? $"Ahead of published (latest: {s.LatestVersion})"
                        : "Ahead of published";
                    color = new Color(1f, 0.75f, 0.32f, 1f);
                    break;

                case NeoxiderUpdateChecker.UpdateStatus.UpdateAvailable:
                    label = !string.IsNullOrEmpty(s.LatestVersion)
                        ? $"Update available: {s.LatestVersion}"
                        : "Update available";
                    color = new Color(1f, 0.42f, 0.42f, 1f);
                    break;

                default:
                    label = !string.IsNullOrEmpty(s.Error) ? s.Error : "Update status: Unknown (click ⟳)";
                    color = !string.IsNullOrEmpty(s.Error)
                        ? new Color(1f, 0.6f, 0.24f, 1f)
                        : NeoInspectorTheme.MutedText;
                    break;
            }

            Rect dotRect = new(refreshRect.xMax + 8f, rect.y + h * 0.5f - 3f, 6f, 6f);
            NeoInspectorTheme.DrawRoundedRect(dotRect, color, 3f);

            float labelRight = rect.xMax - pad;
            if (s.Status == NeoxiderUpdateChecker.UpdateStatus.UpdateAvailable && !string.IsNullOrEmpty(s.UpdateUrl))
            {
                const float actionW = 66f;
                Rect actionRect = new(rect.xMax - pad - actionW, rect.y + (h - 18f) * 0.5f, actionW, 18f);
                if (GUI.Button(actionRect, "Open", EditorStyles.miniButton))
                {
                    Application.OpenURL(s.UpdateUrl);
                }

                labelRight = actionRect.x - 6f;
            }

            float labelX = dotRect.xMax + 7f;
            GUIStyle statusStyle = new(EditorStyles.miniBoldLabel)
            {
                alignment = TextAnchor.MiddleLeft,
                clipping = TextClipping.Clip,
                normal = { textColor = color }
            };
            GUI.Label(new Rect(labelX, rect.y, Mathf.Max(10f, labelRight - labelX), h), label, statusStyle);
        }
    }
}
