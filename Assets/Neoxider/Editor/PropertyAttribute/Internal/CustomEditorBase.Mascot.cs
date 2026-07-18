using System.Collections.Generic;
using UnityEditor;
using UnityEditorInternal;
using UnityEngine;

namespace Neo.Editor
{
    public abstract partial class CustomEditorBase
    {
        private static Texture2D _cachedWorriedIcon;
        private static bool _worriedIconLoadAttempted;
        private static Texture2D _cachedAngryIcon;
        private static bool _angryIconLoadAttempted;
        private static Texture2D _cachedSurprisedIcon;
        private static bool _surprisedIconLoadAttempted;
        private static Texture2D _cachedWatchingIcon;
        private static bool _watchingIconLoadAttempted;

        // WHY: Expanded-state backup per GameObject so a second solo click restores the exact layout.
        private static readonly Dictionary<int, List<KeyValuePair<Component, bool>>> SoloBackups = new();

        private NeoComponentHealth.Mood _frameMood;
        private bool _framePlayMode;
        private bool _showHealthPanel;
        private int _lastSeenConsoleErrors = -1;
        private double _surpriseStart = -1000.0;

        private static Texture2D GetMascotIcon(ref Texture2D cache, ref bool attempted, string fileName)
        {
            if (attempted)
            {
                return cache;
            }

            attempted = true;
            try
            {
                EnsureNeoxiderPackageInfo();
                string root = _cachedNeoxiderRootPath;
                if (!string.IsNullOrEmpty(root))
                {
                    cache = AssetDatabase.LoadAssetAtPath<Texture2D>(
                        $"{root}/Editor/Icons/{fileName}".Replace('\\', '/'));
                }
            }
            catch
            {
            }

            return cache;
        }

        private static Texture2D GetWorriedIcon()
        {
            return GetMascotIcon(ref _cachedWorriedIcon, ref _worriedIconLoadAttempted, "NeoLogoWorried.png");
        }

        private static Texture2D GetAngryIcon()
        {
            return GetMascotIcon(ref _cachedAngryIcon, ref _angryIconLoadAttempted, "NeoLogoAngry.png");
        }

        private static Texture2D GetSurprisedIcon()
        {
            return GetMascotIcon(ref _cachedSurprisedIcon, ref _surprisedIconLoadAttempted, "NeoLogoSurprised.png");
        }

        private static Texture2D GetWatchingIcon()
        {
            return GetMascotIcon(ref _cachedWatchingIcon, ref _watchingIconLoadAttempted, "NeoLogoWatching.png");
        }

        /// <summary>
        ///     Picks the mascot face and frame mood from component health. Also fires the short
        ///     "surprised" reaction when a NEW console error appears while the inspector is open.
        /// </summary>
        private Texture2D SelectMascotFace(Texture2D neutral, Texture2D blink, bool blinking, double now,
            in NeoComponentHealth.Report health)
        {
            if (_lastSeenConsoleErrors >= 0 && health.ConsoleErrors > _lastSeenConsoleErrors)
            {
                _surpriseStart = now;
                // WHY: A fresh error while the inspector is open should surface itself, not hide behind the badge.
                _showHealthPanel = true;
            }

            _lastSeenConsoleErrors = health.ConsoleErrors;

            _frameMood = health.Mood;
            _framePlayMode = EditorApplication.isPlaying;

            bool surprised = now - _surpriseStart < 1.6;
            if (surprised)
            {
                Texture2D icon = GetSurprisedIcon();
                if (icon != null)
                {
                    return icon;
                }
            }

            switch (health.Mood)
            {
                case NeoComponentHealth.Mood.Alarmed:
                {
                    Texture2D icon = GetAngryIcon();
                    if (icon != null)
                    {
                        return icon;
                    }

                    break;
                }
                case NeoComponentHealth.Mood.Worried:
                {
                    Texture2D icon = GetWorriedIcon();
                    if (icon != null)
                    {
                        return icon;
                    }

                    break;
                }
            }

            // WHY: In Play Mode a healthy slime "watches the game" instead of the neutral stare.
            if (_framePlayMode)
            {
                Texture2D watching = GetWatchingIcon();
                if (watching != null)
                {
                    return blinking && blink != null ? blink : watching;
                }
            }

            return blinking && blink != null ? blink : neutral;
        }

        /// <summary>
        ///     Solo: collapse every other component on the GameObject (backing up their expanded state);
        ///     second click restores the backup (or expands everything when there is none).
        /// </summary>
        private void ToggleSoloForTarget()
        {
            if (target is not Component component || component == null)
            {
                return;
            }

            GameObject go = component.gameObject;
            Component[] all = go.GetComponents<Component>();
            int key = go.GetInstanceID();

            bool anyOtherExpanded = false;
            foreach (Component c in all)
            {
                if (c != null && c != component && InternalEditorUtility.GetIsInspectorExpanded(c))
                {
                    anyOtherExpanded = true;
                    break;
                }
            }

            if (anyOtherExpanded)
            {
                var backup = new List<KeyValuePair<Component, bool>>(all.Length);
                foreach (Component c in all)
                {
                    if (c == null)
                    {
                        continue;
                    }

                    backup.Add(new KeyValuePair<Component, bool>(c,
                        InternalEditorUtility.GetIsInspectorExpanded(c)));
                    if (c != component)
                    {
                        InternalEditorUtility.SetIsInspectorExpanded(c, false);
                    }
                }

                InternalEditorUtility.SetIsInspectorExpanded(component, true);
                SoloBackups[key] = backup;
            }
            else if (SoloBackups.TryGetValue(key, out List<KeyValuePair<Component, bool>> saved))
            {
                foreach (KeyValuePair<Component, bool> kv in saved)
                {
                    if (kv.Key != null)
                    {
                        InternalEditorUtility.SetIsInspectorExpanded(kv.Key, kv.Value);
                    }
                }

                SoloBackups.Remove(key);
            }
            else
            {
                foreach (Component c in all)
                {
                    if (c != null)
                    {
                        InternalEditorUtility.SetIsInspectorExpanded(c, true);
                    }
                }
            }

            ActiveEditorTracker.sharedTracker.ForceRebuild();
        }

        /// <summary>Issue-count badge on the mascot chip. Returns true when the badge consumed the click.</summary>
        private bool DrawHealthBadge(Rect chipRect, in NeoComponentHealth.Report health)
        {
            int issues = health.TotalIssues;
            if (issues == 0)
            {
                _showHealthPanel = false;
                return false;
            }

            string label = issues > 99 ? "99+" : issues.ToString();
            float w = label.Length > 1 ? 20f : 15f;
            Rect badge = new(chipRect.xMax - w + 4f, chipRect.yMax - 11f, w, 14f);

            if (Event.current.type == EventType.Repaint)
            {
                Color bg = health.Mood == NeoComponentHealth.Mood.Alarmed
                    ? new Color(0.86f, 0.24f, 0.26f, 1f)
                    : new Color(0.95f, 0.62f, 0.14f, 1f);
                NeoInspectorTheme.DrawRoundedRect(badge, bg, new Color(0f, 0f, 0f, 0.35f), 7f, 1f);
                GUIStyle st = new(EditorStyles.miniBoldLabel)
                {
                    fontSize = 9,
                    alignment = TextAnchor.MiddleCenter,
                    normal = { textColor = Color.white }
                };
                GUI.Label(badge, label, st);
            }

            if (Event.current.type == EventType.MouseDown && Event.current.button == 0 &&
                badge.Contains(Event.current.mousePosition))
            {
                _showHealthPanel = !_showHealthPanel;
                Event.current.Use();
                Repaint();
                return true;
            }

            return false;
        }

        /// <summary>Compact issue list under the banner (opened by the badge).</summary>
        private void DrawHealthPanel(in NeoComponentHealth.Report health)
        {
            if (!_showHealthPanel || health.TotalIssues == 0)
            {
                return;
            }

            Color accent = health.Mood == NeoComponentHealth.Mood.Alarmed
                ? new Color(0.9f, 0.3f, 0.32f)
                : new Color(0.95f, 0.66f, 0.2f);

            Rect panel = EditorGUILayout.BeginVertical();
            if (Event.current.type == EventType.Repaint)
            {
                NeoInspectorTheme.DrawRoundedRect(panel,
                    Color.Lerp(NeoInspectorTheme.PanelBackground, accent, 0.08f),
                    new Color(accent.r, accent.g, accent.b, 0.35f), NeoInspectorTheme.RadiusSection, 1f);
            }

            GUILayout.Space(6f);

            if (health.ConsoleErrors > 0)
            {
                EditorGUILayout.BeginHorizontal();
                GUILayout.Space(8f);
                EditorGUILayout.LabelField(
                    $"Console: {health.ConsoleErrors} error(s) this session — {health.LastConsoleMessage}",
                    EditorStyles.wordWrappedMiniLabel);
                if (GUILayout.Button("Clear", EditorStyles.miniButton, GUILayout.Width(46f)))
                {
                    NeoComponentHealth.ClearConsoleErrors(target.GetType());
                    _lastSeenConsoleErrors = 0;
                }

                GUILayout.Space(6f);
                EditorGUILayout.EndHorizontal();
            }

            if (health.MissingReferences > 0)
            {
                EditorGUILayout.BeginHorizontal();
                GUILayout.Space(8f);
                EditorGUILayout.LabelField(
                    $"Missing references: {health.MissingReferences} (deleted objects still assigned)",
                    EditorStyles.wordWrappedMiniLabel);
                GUILayout.Space(6f);
                EditorGUILayout.EndHorizontal();
            }

            if (health.InvalidNumbers > 0)
            {
                EditorGUILayout.BeginHorizontal();
                GUILayout.Space(8f);
                EditorGUILayout.LabelField(
                    $"Invalid numbers: {health.InvalidNumbers} field(s) are NaN / Infinity",
                    EditorStyles.wordWrappedMiniLabel);
                GUILayout.Space(6f);
                EditorGUILayout.EndHorizontal();
            }

            GUILayout.Space(6f);
            EditorGUILayout.EndVertical();
            EditorGUILayout.Space(2f);
        }
    }
}
