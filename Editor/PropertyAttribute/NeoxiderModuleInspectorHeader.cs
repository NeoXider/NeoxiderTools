using System.Collections.Generic;
using System.Reflection;
using UnityEditor;
using UnityEngine;

namespace Neo.Editor
{
    /// <summary>
    ///     Banner and update row for inspectors of separately packaged Neoxider modules: module title, package root,
    ///     version pill and the cached update status of that module. Drawn in the same visual language as the Neo
    ///     component header.
    /// </summary>
    public static class NeoxiderModuleInspectorHeader
    {
        private sealed class HeaderInfo
        {
            public string Title;
            public string Version;
            public string VersionText;
            public string Root;
            public string StateKeyPrefix;
            public float PillWidth = -1f;
        }

        // WHY: the header redraws on every IMGUI event; the module title, version text and root path only change
        // with the package, so they are resolved once per assembly instead of formatted on each event.
        private static readonly Dictionary<Assembly, HeaderInfo> s_infos = new();

        private static HeaderInfo GetInfo(Assembly assembly, string fallbackDisplayName)
        {
            if (assembly != null && s_infos.TryGetValue(assembly, out HeaderInfo cached))
            {
                return cached;
            }

            HeaderInfo info = new()
            {
                Title = fallbackDisplayName ?? "Neoxider Module",
                Version = "Unknown"
            };

            if (NeoxiderModulePackageInfoUtility.TryGetForAssembly(assembly, out NeoxiderModulePackageInfo package))
            {
                if (!string.IsNullOrEmpty(package.DisplayName) && package.DisplayName != "Unknown")
                {
                    info.Title = package.DisplayName;
                }

                if (!string.IsNullOrEmpty(package.Version))
                {
                    info.Version = package.Version;
                }

                info.Root = package.RootPath;
                info.StateKeyPrefix = string.IsNullOrEmpty(package.Name) ? null : package.Name;
            }

            info.VersionText = "v" + info.Version;
            if (assembly != null)
            {
                s_infos[assembly] = info;
            }

            return info;
        }

        public static void Draw(Assembly assembly, string fallbackDisplayName = null)
        {
            HeaderInfo info = GetInfo(assembly, fallbackDisplayName);
            DrawModuleBanner(info);
            DrawUpdateStatus(info);
            EditorGUILayout.Space(4);
        }

        private static void DrawModuleBanner(HeaderInfo info)
        {
            bool hasRoot = !string.IsNullOrEmpty(info.Root);
            float height = hasRoot ? 44f : 34f;

            Rect full = GUILayoutUtility.GetRect(0f, height, NeoInspectorStyles.ExpandWidth);
            Rect rect = NeoInspectorTheme.Snap(new Rect(full.x + 1f, full.y, full.width - 2f, full.height));

            NeoInspectorTheme.DrawRoundedTexture(rect, NeoInspectorTheme.BannerGradient,
                new Color(1f, 1f, 1f, 0.12f), NeoInspectorTheme.RadiusCard, Color.white, 1f);

            const float pad = 10f;
            if (info.PillWidth < 0f)
            {
                info.PillWidth = Mathf.Max(42f,
                    NeoInspectorStyles.TextWidth(NeoInspectorStyles.VersionPill, info.VersionText) + 18f);
            }

            const float pillH = 18f;
            Rect pillRect = NeoInspectorTheme.Snap(new Rect(rect.xMax - pad - info.PillWidth,
                rect.y + (height - pillH) * 0.5f, info.PillWidth, pillH));
            NeoInspectorTheme.DrawRoundedRect(pillRect, new Color(0.04f, 0.04f, 0.10f, 0.42f),
                new Color(1f, 1f, 1f, 0.22f), pillH * 0.5f, 1f);
            GUI.Label(pillRect, NeoInspectorStyles.CachedContent(info.VersionText), NeoInspectorStyles.VersionPill);

            float textW = Mathf.Max(0f, pillRect.x - (rect.x + pad) - 8f);
            if (hasRoot)
            {
                GUI.Label(new Rect(rect.x + pad, rect.y + 6f, textW, 18f), NeoInspectorStyles.CachedContent(info.Title),
                    NeoInspectorStyles.BannerTitle);
                GUI.Label(new Rect(rect.x + pad, rect.y + 24f, textW, 14f), NeoInspectorStyles.CachedContent(info.Root),
                    NeoInspectorStyles.BannerTagline);
            }
            else
            {
                GUI.Label(new Rect(rect.x + pad, rect.y + 7f, textW, 18f), NeoInspectorStyles.CachedContent(info.Title),
                    NeoInspectorStyles.BannerTitle);
            }
        }

        private static void DrawUpdateStatus(HeaderInfo info)
        {
            if (string.IsNullOrEmpty(info.Version) || string.IsNullOrEmpty(info.Root))
            {
                return;
            }

            // WHY: no background logic on repaint. Only read cached state.
            NeoxiderUpdateChecker.State s = NeoxiderUpdateChecker.Peek(info.StateKeyPrefix);

            EditorGUILayout.Space(3f);

            const float h = 22f;
            Rect full = GUILayoutUtility.GetRect(0f, h, NeoInspectorStyles.ExpandWidth);
            Rect rect = NeoInspectorTheme.Snap(new Rect(full.x + 1f, full.y, full.width - 2f, full.height));

            NeoInspectorTheme.DrawRoundedRect(rect, NeoInspectorTheme.PanelBackground,
                NeoInspectorTheme.Separator, NeoInspectorTheme.RadiusRow, 1f);

            const float pad = 4f;
            Rect refreshRect = new(rect.x + pad, rect.y + (h - 16f) * 0.5f, 22f, 16f);
            if (GUI.Button(refreshRect, NeoInspectorStyles.RefreshIcon, EditorStyles.miniButton))
            {
                NeoxiderUpdateChecker.RequestImmediateCheck(info.StateKeyPrefix, info.Version, info.Root);
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
                    color = new Color(0.30f, 0.78f, 0.46f, 1f);
                    break;

                case NeoxiderUpdateChecker.UpdateStatus.Ahead:
                    label = !string.IsNullOrEmpty(s.LatestVersion)
                        ? s.AheadLabel
                        : "Ahead of published";
                    color = new Color(1f, 0.72f, 0.30f, 1f);
                    break;

                case NeoxiderUpdateChecker.UpdateStatus.UpdateAvailable:
                    label = !string.IsNullOrEmpty(s.LatestVersion)
                        ? s.UpdateLabel
                        : "Update available";
                    color = new Color(0.96f, 0.42f, 0.40f, 1f);
                    break;

                default:
                    label = !string.IsNullOrEmpty(s.Error) ? s.Error : "Update status unknown - press refresh";
                    color = !string.IsNullOrEmpty(s.Error)
                        ? new Color(1f, 0.6f, 0.24f, 1f)
                        : NeoInspectorTheme.MutedText;
                    break;
            }

            Rect dotRect = NeoInspectorTheme.Snap(new Rect(refreshRect.xMax + 8f, rect.y + h * 0.5f - 3f, 6f, 6f));
            NeoInspectorTheme.DrawRoundedRect(dotRect, color, 3f);

            float labelRight = rect.xMax - pad;
            if (s.Status == NeoxiderUpdateChecker.UpdateStatus.UpdateAvailable && !string.IsNullOrEmpty(s.UpdateUrl))
            {
                const float actionW = 60f;
                Rect actionRect = new(rect.xMax - pad - actionW, rect.y + (h - 16f) * 0.5f, actionW, 16f);
                if (GUI.Button(actionRect, "Open", EditorStyles.miniButton))
                {
                    Application.OpenURL(s.UpdateUrl);
                }

                labelRight = actionRect.x - 6f;
            }

            float labelX = dotRect.xMax + 7f;
            GUIStyle statusStyle = NeoInspectorStyles.Status(NeoInspectorTheme.IsDark
                ? color
                : Color.Lerp(color, Color.black, 0.45f));
            GUI.Label(new Rect(labelX, rect.y, Mathf.Max(10f, labelRight - labelX), h), NeoInspectorStyles.CachedContent(label),
                statusStyle);
        }
    }
}
