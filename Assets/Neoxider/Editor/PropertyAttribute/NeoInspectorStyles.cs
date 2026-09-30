using UnityEditor;
using UnityEngine;

namespace Neo.Editor
{
    /// <summary>
    ///     GUIStyles for the always-drawn inspector chrome (banner, badge, update strip), built once per editor
    ///     skin instead of once per repaint. Only valid inside an OnGUI pass, like <see cref="EditorStyles" />.
    /// </summary>
    internal static class NeoInspectorStyles
    {
        private static bool s_builtForDark;
        private static GUIStyle s_glyph;
        private static GUIStyle s_bannerTitle;
        private static GUIStyle s_bannerTagline;
        private static GUIStyle s_versionPill;
        private static GUIStyle s_badge;
        private static GUIStyle s_status;
        private static GUIStyle s_miniChipText;
        private static GUIStyle s_buttonText;
        private static GUIStyle s_buttonShadow;
        private static GUIContent s_refreshIcon;
        private static string s_pillVersion;
        private static string s_pillLabel;
        private static float s_pillWidth;

        /// <summary>Fallback "N" mark drawn when the mascot art is missing.</summary>
        public static GUIStyle Glyph
        {
            get
            {
                EnsureBuilt();
                return s_glyph;
            }
        }

        public static GUIStyle BannerTitle
        {
            get
            {
                EnsureBuilt();
                return s_bannerTitle;
            }
        }

        public static GUIStyle BannerTagline
        {
            get
            {
                EnsureBuilt();
                return s_bannerTagline;
            }
        }

        /// <summary>Version pill text; always solid white so it stays legible over the bright gradient.</summary>
        public static GUIStyle VersionPill
        {
            get
            {
                EnsureBuilt();
                return s_versionPill;
            }
        }

        public static GUIStyle Badge
        {
            get
            {
                EnsureBuilt();
                return s_badge;
            }
        }

        /// <summary>Update-strip status label tinted with <paramref name="color" />.</summary>
        public static GUIStyle Status(Color color)
        {
            EnsureBuilt();
            s_status.normal.textColor = color;
            return s_status;
        }

        /// <summary>Centered mini label used inside themed chip buttons.</summary>
        public static GUIStyle MiniChipText(Color color)
        {
            EnsureBuilt();
            s_miniChipText.normal.textColor = color;
            return s_miniChipText;
        }

        /// <summary>Label of the gradient action button.</summary>
        public static GUIStyle ButtonText
        {
            get
            {
                EnsureBuilt();
                return s_buttonText;
            }
        }

        /// <summary>Soft drop shadow drawn one pixel under <see cref="ButtonText" />.</summary>
        public static GUIStyle ButtonShadow
        {
            get
            {
                EnsureBuilt();
                return s_buttonShadow;
            }
        }

        /// <summary>Refresh icon content; falls back to a text glyph when the built-in icon is unavailable.</summary>
        public static GUIContent RefreshIcon
        {
            get
            {
                if (s_refreshIcon == null || (s_refreshIcon.image == null && string.IsNullOrEmpty(s_refreshIcon.text)))
                {
                    GUIContent icon = EditorGUIUtility.IconContent("d_Refresh");
                    s_refreshIcon = icon != null && icon.image != null ? new GUIContent(icon) : new GUIContent("⟳");
                }

                return s_refreshIcon;
            }
        }

        /// <summary>The "v1.2.3" pill text for <paramref name="version" />, allocated once per version string.</summary>
        public static string VersionLabel(string version)
        {
            if (!string.Equals(s_pillVersion, version))
            {
                s_pillVersion = version;
                s_pillLabel = $"v{version}";
                s_pillWidth = -1f;
            }

            return s_pillLabel;
        }

        /// <summary>Pill width for <paramref name="label" />; text measuring runs once per label.</summary>
        public static float VersionPillWidth(string label)
        {
            if (s_pillWidth < 0f || !ReferenceEquals(label, s_pillLabel))
            {
                s_pillWidth = Mathf.Max(46f, VersionPill.CalcSize(new GUIContent(label)).x + 20f);
            }

            return s_pillWidth;
        }

        private static void EnsureBuilt()
        {
            bool dark = EditorGUIUtility.isProSkin;
            if (s_glyph != null && s_builtForDark == dark)
            {
                return;
            }

            s_builtForDark = dark;
            s_pillWidth = -1f;

            s_glyph = new GUIStyle(EditorStyles.boldLabel)
            {
                fontSize = 26,
                alignment = TextAnchor.MiddleCenter,
                normal = { textColor = Color.white }
            };

            s_bannerTitle = new GUIStyle(EditorStyles.boldLabel)
            {
                fontSize = 16,
                alignment = TextAnchor.LowerLeft,
                clipping = TextClipping.Clip,
                normal = { textColor = new Color(1f, 1f, 1f, 0.98f) }
            };

            s_bannerTagline = new GUIStyle(EditorStyles.miniLabel)
            {
                fontSize = 11,
                alignment = TextAnchor.UpperLeft,
                clipping = TextClipping.Clip,
                normal = { textColor = new Color(1f, 1f, 1f, 0.72f) }
            };

            s_versionPill = new GUIStyle(EditorStyles.boldLabel)
            {
                fontSize = 12,
                alignment = TextAnchor.MiddleCenter,
                normal = { textColor = Color.white }
            };

            s_badge = new GUIStyle(EditorStyles.miniBoldLabel)
            {
                fontSize = 9,
                alignment = TextAnchor.MiddleCenter,
                normal = { textColor = Color.white }
            };

            s_status = new GUIStyle(EditorStyles.miniBoldLabel)
            {
                alignment = TextAnchor.MiddleLeft,
                clipping = TextClipping.Clip
            };

            s_miniChipText = new GUIStyle(EditorStyles.miniBoldLabel)
            {
                alignment = TextAnchor.MiddleCenter
            };

            s_buttonText = new GUIStyle(EditorStyles.boldLabel)
            {
                fontSize = 12,
                alignment = TextAnchor.MiddleCenter,
                normal = { textColor = new Color(1f, 1f, 1f, 0.98f) }
            };

            s_buttonShadow = new GUIStyle(EditorStyles.boldLabel)
            {
                fontSize = 12,
                alignment = TextAnchor.MiddleCenter,
                normal = { textColor = new Color(0f, 0f, 0f, 0.32f) }
            };
        }
    }
}
