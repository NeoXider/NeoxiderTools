using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace Neo.Audio.Editor
{
    /// <summary>
    ///     Draws an <see cref="AudioIdAttribute" /> string using ids from a global editor lookup of
    ///     loaded <see cref="AM" /> objects, including inactive objects.
    ///     <para>
    ///         Offers configured ids to reduce typing mistakes. This authoring lookup does not bind a
    ///         playback manager or inspect a host component's explicit manager reference.
    ///     </para>
    ///     <para>
    ///         With no manager or no ids of the requested kind, the field stays an ordinary text box.
    ///         An id that no longer exists is shown as "missing" without changing its serialized value.
    ///         A prefab inspected while another scene is loaded can see that scene's manager.
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

            AudioIdAttribute audioIdAttribute = (AudioIdAttribute)attribute;
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
            string[] options = BuildOptions(ids, current, out int selected);

            EditorGUI.BeginProperty(position, label, property);
            EditorGUI.BeginChangeCheck();
            int picked = EditorGUI.Popup(position, label.text, selected, options);
            if (EditorGUI.EndChangeCheck() && picked >= 0 && picked <= ids.Count)
            {
                property.stringValue = picked == 0 ? string.Empty : ids[picked - 1];
            }

            EditorGUI.EndProperty();
        }

        private static AM _cachedManager;
        private static double _cacheStamp = double.NegativeInfinity;

        private static string[] BuildOptions(List<string> ids, string current, out int selected)
        {
            List<string> options = new List<string> { None };
            for (int index = 0; index < ids.Count; index++)
            {
                // Display labels are not ids: the empty choice and a real id named "<none>" must differ.
                options.Add(ids[index] == None ? ids[index] + "  (id)" : ids[index]);
            }

            int indexOfId = ids.IndexOf(current);
            selected = string.IsNullOrEmpty(current) ? 0 : indexOfId + 1;
            if (!string.IsNullOrEmpty(current) && indexOfId < 0)
            {
                selected = options.Count;
                options.Add(current + "  (missing)");
            }

            return options.ToArray();
        }

        /// <summary>
        ///     Looks up a loaded AM at most once a second, including cached misses.
        ///     <para>
        ///         WHY the cache: <c>OnGUI</c> runs on every repaint, and an inspector with several id fields
        ///         would otherwise sweep the whole scene many times a frame. A one-second staleness is
        ///         invisible - it only delays a newly added manager appearing in the dropdown.
        ///     </para>
        /// </summary>
        private static AM FindManager()
        {
            double now = EditorApplication.timeSinceStartup;
            if (now - _cacheStamp < 1.0)
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

            List<string> ids = new List<string>();

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
