using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace Neo.Audio.Editor
{
    /// <summary>
    ///     Draws an <see cref="AudioIdAttribute" /> string as a dropdown of the ids configured on the
    ///     <see cref="AM" /> that this object can see.
    ///     <para>
    ///         Why a dropdown at all: a typed id is a silent failure - the sound simply never plays, and
    ///         nothing says why until someone reads the console. Picking from a list makes that impossible.
    ///     </para>
    ///     <para>
    ///         It degrades rather than blocks. With no AM in the scene (a prefab opened on its own, an
    ///         early setup) the field stays an ordinary text box, and an id that no longer exists is shown
    ///         as "missing" instead of being silently replaced.
    ///     </para>
    /// </summary>
    [CustomPropertyDrawer(typeof(AudioIdAttribute))]
    public sealed class AudioIdDrawer : PropertyDrawer
    {
        private const string None = "<none>";

        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            if (property.propertyType != SerializedPropertyType.String)
            {
                EditorGUI.PropertyField(position, property, label);
                return;
            }

            var audioIdAttribute = (AudioIdAttribute)attribute;
            AM manager = FindManager();
            List<string> ids = CollectIds(manager, audioIdAttribute.Kind);

            if (ids == null)
            {
                Rect fieldRect = position;
                fieldRect.width -= 18f;
                EditorGUI.PropertyField(fieldRect, property, label);

                Rect hintRect = new Rect(position.xMax - 16f, position.y, 16f, position.height);
                EditorGUI.LabelField(hintRect,
                    new GUIContent(EditorGUIUtility.IconContent("console.infoicon.sml").image,
                        "No AM with ids found. Typing the id by hand works, but a dropdown appears once " +
                        "an AM in the scene has ids configured."));
                return;
            }

            string current = property.stringValue;
            var options = new List<string> { None };
            options.AddRange(ids);

            int selected = string.IsNullOrEmpty(current) ? 0 : options.IndexOf(current);
            bool missing = selected < 0;
            if (missing)
            {
                options.Add(current + "  (missing)");
                selected = options.Count - 1;
            }

            EditorGUI.BeginProperty(position, label, property);
            EditorGUI.BeginChangeCheck();
            int picked = EditorGUI.Popup(position, label.text, selected, options.ToArray());
            if (EditorGUI.EndChangeCheck())
            {
                property.stringValue = picked == 0 ? string.Empty : options[picked];
            }

            EditorGUI.EndProperty();
        }

        private static AM _cachedManager;
        private static double _cacheStamp;

        /// <summary>
        ///     Finds the AM, at most a few times a second.
        ///     <para>
        ///         WHY the cache: <c>OnGUI</c> runs on every repaint, and an inspector with several id fields
        ///         would otherwise sweep the whole scene many times a frame. A one-second staleness is
        ///         invisible - it only delays a newly added manager appearing in the dropdown.
        ///     </para>
        /// </summary>
        private static AM FindManager()
        {
            double now = EditorApplication.timeSinceStartup;
            if (_cachedManager != null && now - _cacheStamp < 1.0)
            {
                return _cachedManager;
            }

            _cacheStamp = now;
            _cachedManager = Object.FindFirstObjectByType<AM>(FindObjectsInactive.Include);
            return _cachedManager;
        }

        /// <summary>Returns the ids of the requested list, or null when there is nothing to offer.</summary>
        private static List<string> CollectIds(AM manager, AudioIdKind kind)
        {
            if (manager == null)
            {
                return null;
            }

            var ids = new List<string>();

            if (kind == AudioIdKind.Music)
            {
                IReadOnlyList<MusicEntry> entries = manager.MusicEntries;
                for (int index = 0; index < entries.Count; index++)
                {
                    AddId(ids, entries[index]);
                }
            }
            else
            {
                IReadOnlyList<SoundEntry> entries = manager.SoundEntries;
                for (int index = 0; index < entries.Count; index++)
                {
                    AddId(ids, entries[index]);
                }
            }

            return ids.Count > 0 ? ids : null;
        }

        private static void AddId(List<string> ids, AudioEntry entry)
        {
            if (entry == null || string.IsNullOrEmpty(entry.Id) || ids.Contains(entry.Id))
            {
                return;
            }

            ids.Add(entry.Id);
        }
    }
}
