using System;
using UnityEditor;
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

        private static Type _gameViewType;
        private static bool _gameViewTypeResolved;
        private static double _gameViewCenterCheckedAt = -1000.0;
        private static Vector2 _gameViewScreenCenter;
        private static bool _gameViewCenterValid;

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
        ///     Screen-space center of the first open Game view, rescanned at most once per second
        ///     (the window enumeration is not free). False when no Game view exists.
        /// </summary>
        private static bool TryGetGameViewScreenCenter(double now, out Vector2 center)
        {
            if (now - _gameViewCenterCheckedAt >= 1.0)
            {
                _gameViewCenterCheckedAt = now;
                _gameViewCenterValid = false;

                if (!_gameViewTypeResolved)
                {
                    _gameViewTypeResolved = true;
                    _gameViewType = Type.GetType("UnityEditor.GameView,UnityEditor");
                }

                if (_gameViewType != null)
                {
                    try
                    {
                        foreach (UnityEngine.Object candidate in Resources.FindObjectsOfTypeAll(_gameViewType))
                        {
                            // WHY: position is in desktop coordinates (can be negative on multi-display);
                            // comparisons against GUIToScreenPoint results stay valid either way.
                            if (candidate is EditorWindow window && window.position.width > 0f)
                            {
                                _gameViewScreenCenter = window.position.center;
                                _gameViewCenterValid = true;
                                break;
                            }
                        }
                    }
                    catch
                    {
                    }
                }
            }

            center = _gameViewScreenCenter;
            return _gameViewCenterValid;
        }

        /// <summary>
        ///     Draws the mascot face. In Play Mode the "watching" face turns toward the Game view:
        ///     the art looks right by default, so it is mirrored when the Game view sits to the left.
        /// </summary>
        private void DrawMascotFace(Rect rect, Texture2D face)
        {
            if (_framePlayMode && face != null && face == GetWatchingIcon() &&
                TryGetGameViewScreenCenter(EditorApplication.timeSinceStartup, out Vector2 gameCenter) &&
                gameCenter.x < GUIUtility.GUIToScreenPoint(rect.center).x)
            {
                // WHY: Reversed U texcoords mirror horizontally without touching the GUI matrix.
                GUI.DrawTextureWithTexCoords(rect, face, new Rect(1f, 0f, -1f, 1f));
                return;
            }

            GUI.DrawTexture(rect, face, ScaleMode.ScaleToFit, true);
        }

        /// <summary>
        ///     Picks the mascot face and frame mood from component health. Also fires the short
        ///     "surprised" reaction when a NEW console error appears while the inspector is open.
        /// </summary>
        private Texture2D SelectMascotFace(Texture2D neutral, Texture2D blink, bool blinking, double now,
            bool animating, in NeoComponentHealth.Report health)
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

            // WHY: The startled face is a timed reaction; without animation nothing would repaint to end it.
            bool surprised = animating && now - _surpriseStart < 1.6;
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

        /// <summary>Issue-count badge on the mascot chip. Returns true when the badge consumed the click.</summary>
        private bool DrawHealthBadge(Rect chipRect, in NeoComponentHealth.Report health)
        {
            int issues = health.TotalIssues;
            if (issues == 0)
            {
                _showHealthPanel = false;
                return false;
            }

            string label = issues > 99 ? "99+" : NeoInspectorStyles.CountText(issues);
            float h = chipRect.height >= 28f ? 14f : 12f;
            float w = label.Length > 1 ? h + 6f : h;
            Rect badge = NeoInspectorTheme.Snap(new Rect(chipRect.xMax - w + 4f, chipRect.yMax - h + 3f, w, h));

            if (Event.current.type == EventType.Repaint)
            {
                Color bg = health.Mood == NeoComponentHealth.Mood.Alarmed
                    ? new Color(0.86f, 0.24f, 0.26f, 1f)
                    : new Color(0.93f, 0.58f, 0.12f, 1f);
                NeoInspectorTheme.DrawRoundedRect(badge, bg, new Color(0f, 0f, 0f, 0.30f), h * 0.5f, 1f);
                GUI.Label(badge, label, NeoInspectorStyles.Badge);
            }

            // WHY: no GUI.Button here - the badge exists only while issues do, and the validation scan can change
            // that between a Layout and its Repaint; a control id that comes and goes would shift every id after it.
            if (ClickZone(badge, GetHealthBadgeTooltip(health)))
            {
                _showHealthPanel = !_showHealthPanel;
                Repaint();
                return true;
            }

            return false;
        }

        private int _healthTextErrors = -1;
        private int _healthTextMissing = -1;
        private int _healthTextInvalid = -1;
        private string _healthTextMessage;
        private string _healthConsoleLine;
        private string _healthMissingLine;
        private string _healthInvalidLine;
        private string _healthBadgeTooltip;

        // WHY: the panel and the badge tooltip used to interpolate their sentences on every IMGUI event; the numbers
        // only change when the health report does.
        private void RefreshHealthTexts(in NeoComponentHealth.Report health)
        {
            if (health.ConsoleErrors == _healthTextErrors && health.MissingReferences == _healthTextMissing &&
                health.InvalidNumbers == _healthTextInvalid &&
                string.Equals(health.LastConsoleMessage, _healthTextMessage, StringComparison.Ordinal))
            {
                return;
            }

            _healthTextErrors = health.ConsoleErrors;
            _healthTextMissing = health.MissingReferences;
            _healthTextInvalid = health.InvalidNumbers;
            _healthTextMessage = health.LastConsoleMessage;
            _healthConsoleLine = health.ConsoleErrors > 0
                ? $"{health.ConsoleErrors} console error(s) this session: {health.LastConsoleMessage}"
                : null;
            _healthMissingLine = health.MissingReferences > 0
                ? $"{health.MissingReferences} missing reference(s): deleted objects are still assigned."
                : null;
            _healthInvalidLine = health.InvalidNumbers > 0
                ? $"{health.InvalidNumbers} field(s) hold NaN or Infinity."
                : null;
            _healthBadgeTooltip = $"{health.TotalIssues} issue(s). Click to show or hide the list.";
        }

        private string GetHealthBadgeTooltip(in NeoComponentHealth.Report health)
        {
            RefreshHealthTexts(health);
            return _healthBadgeTooltip;
        }

        /// <summary>Compact issue list under the header (opened by the badge).</summary>
        private void DrawHealthPanel(in NeoComponentHealth.Report health)
        {
            if (!_showHealthPanel || health.TotalIssues == 0)
            {
                return;
            }

            RefreshHealthTexts(health);
            Color accent = health.Mood == NeoComponentHealth.Mood.Alarmed
                ? new Color(0.9f, 0.3f, 0.32f)
                : new Color(0.95f, 0.66f, 0.2f);

            NeoInspectorStyles.Space(3f);
            Rect panel = EditorGUILayout.BeginVertical(NeoInspectorStyles.ActionsPanel);
            if (Event.current.type == EventType.Repaint)
            {
                NeoInspectorTheme.DrawRoundedRect(panel,
                    Color.Lerp(NeoInspectorTheme.PanelBackground, accent, 0.07f),
                    new Color(accent.r, accent.g, accent.b, 0.35f), NeoInspectorTheme.RadiusSection, 1f);
                NeoInspectorTheme.DrawAccentRail(panel, accent, 3f, 5f);
            }

            GUIStyle line = NeoInspectorStyles.HealthLine;
            if (_healthConsoleLine != null)
            {
                EditorGUILayout.BeginHorizontal();
                EditorGUILayout.LabelField(_healthConsoleLine, line);
                if (GUILayout.Button("Clear", EditorStyles.miniButton, NeoInspectorStyles.Width(48f)))
                {
                    NeoComponentHealth.ClearConsoleErrors(target.GetType());
                    _lastSeenConsoleErrors = 0;
                }

                EditorGUILayout.EndHorizontal();
            }

            if (_healthMissingLine != null)
            {
                EditorGUILayout.LabelField(_healthMissingLine, line);
            }

            if (_healthInvalidLine != null)
            {
                EditorGUILayout.LabelField(_healthInvalidLine, line);
            }

            EditorGUILayout.EndVertical();
        }
    }
}
