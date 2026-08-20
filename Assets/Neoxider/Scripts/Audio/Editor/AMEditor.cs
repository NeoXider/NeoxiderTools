using System.Collections.Generic;
using Neo.Editor;
using UnityEditor;
using UnityEngine;

namespace Neo.Audio.Editor
{
    /// <summary>
    ///     Inspector for <see cref="AM" />.
    ///     <para>
    ///         It <b>extends</b> <see cref="CustomEditorBase" /> rather than replacing it: the banner, the
    ///         documentation foldout, the health panel, the section rails and the Actions block are all
    ///         still drawn by the base pass, and the fields themselves are still ordinary serialized
    ///         properties with <c>[Header]</c> and <c>[Tooltip]</c>. Only the things no attribute can
    ///         express live here - bulk authoring, id validation and a live readout while the game runs.
    ///     </para>
    /// </summary>
    [CustomEditor(typeof(AM))]
    public sealed class AMEditor : CustomEditorBase
    {
        protected override string NeoxiderModuleName => "Audio";

        protected override void ProcessAttributeAssignments()
        {
        }

        protected override void OnAfterDrawNeoProperties()
        {
            var manager = (AM)target;

            DrawBulkAuthoring();
            DrawIdWarnings(manager);
            DrawRuntimeStatus(manager);
        }

        /// <summary>
        ///     The fast path into an empty manager: drop a folder's worth of clips here and get one entry per
        ///     clip, already named after it. Filling a bank used to mean growing the array by hand and
        ///     assigning each element separately.
        /// </summary>
        private void DrawBulkAuthoring()
        {
            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Authoring", EditorStyles.boldLabel);

            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button(new GUIContent("+ Sound", "Adds one empty sound entry.")))
            {
                AppendEntry("_soundEntries");
            }

            if (GUILayout.Button(new GUIContent("+ Music Pool", "Adds one empty music pool.")))
            {
                AppendEntry("_musicEntries");
            }

            EditorGUILayout.EndHorizontal();

            DrawDropZone("Drop clips here -> one SOUND entry per clip", "_soundEntries");
            DrawDropZone("Drop clips here -> one MUSIC pool per clip", "_musicEntries");

            EditorGUILayout.LabelField(
                "Hold the clips together on one row instead to make them variations of a single entry: " +
                "drag them straight onto that entry's row.",
                EditorStyles.wordWrappedMiniLabel);
        }

        private void AppendEntry(string arrayPropertyName)
        {
            SerializedProperty array = serializedObject.FindProperty(arrayPropertyName);
            if (array == null)
            {
                return;
            }

            array.arraySize++;
            SerializedProperty added = array.GetArrayElementAtIndex(array.arraySize - 1);
            ResetEntry(added, arrayPropertyName == "_soundEntries");
            added.isExpanded = true;
            serializedObject.ApplyModifiedProperties();
        }

        /// <summary>
        ///     Unity copies the previous element when an array grows, so a new entry would inherit the last
        ///     one's clips and id. Clear it back to the documented defaults instead.
        /// </summary>
        private static void ResetEntry(SerializedProperty entry, bool isSound)
        {
            SerializedProperty id = entry.FindPropertyRelative("_id");
            if (id != null)
            {
                id.stringValue = string.Empty;
            }

            SerializedProperty clips = entry.FindPropertyRelative("_clips");
            if (clips != null)
            {
                clips.arraySize = 0;
            }

            SerializedProperty volume = entry.FindPropertyRelative("_volume");
            if (volume != null)
            {
                volume.floatValue = 1f;
            }

            SerializedProperty randomize = entry.FindPropertyRelative("_randomizePitch");
            if (randomize != null)
            {
                // WHY: effects repeat and want the detune; a detuned music bed reads as a fault.
                randomize.boolValue = isSound;
            }

            SerializedProperty pitchMin = entry.FindPropertyRelative("_pitchMin");
            if (pitchMin != null)
            {
                pitchMin.floatValue = 0.94f;
            }

            SerializedProperty pitchMax = entry.FindPropertyRelative("_pitchMax");
            if (pitchMax != null)
            {
                pitchMax.floatValue = 1.06f;
            }

            SerializedProperty mode = entry.FindPropertyRelative("_mode");
            if (mode != null)
            {
                mode.enumValueIndex = (int)MusicPoolMode.Loop;
            }

            SerializedProperty fade = entry.FindPropertyRelative("_fadeDuration");
            if (fade != null)
            {
                fade.floatValue = -1f;
            }
        }

        private void DrawDropZone(string caption, string arrayPropertyName)
        {
            Rect zone = GUILayoutUtility.GetRect(0f, 30f, GUILayout.ExpandWidth(true));
            GUI.Box(zone, caption, EditorStyles.helpBox);

            Event current = Event.current;
            if (current.type != EventType.DragUpdated && current.type != EventType.DragPerform)
            {
                return;
            }

            if (!zone.Contains(current.mousePosition))
            {
                return;
            }

            List<AudioClip> dropped = CollectClips();
            if (dropped.Count == 0)
            {
                return;
            }

            DragAndDrop.visualMode = DragAndDropVisualMode.Copy;
            if (current.type != EventType.DragPerform)
            {
                return;
            }

            DragAndDrop.AcceptDrag();

            SerializedProperty array = serializedObject.FindProperty(arrayPropertyName);
            bool isSound = arrayPropertyName == "_soundEntries";
            for (int index = 0; index < dropped.Count; index++)
            {
                array.arraySize++;
                SerializedProperty entry = array.GetArrayElementAtIndex(array.arraySize - 1);
                ResetEntry(entry, isSound);
                entry.FindPropertyRelative("_id").stringValue = dropped[index].name;
                SerializedProperty clips = entry.FindPropertyRelative("_clips");
                clips.arraySize = 1;
                clips.GetArrayElementAtIndex(0).objectReferenceValue = dropped[index];
            }

            serializedObject.ApplyModifiedProperties();
            current.Use();
        }

        private static List<AudioClip> CollectClips()
        {
            var clips = new List<AudioClip>();
            for (int index = 0; index < DragAndDrop.objectReferences.Length; index++)
            {
                if (DragAndDrop.objectReferences[index] is AudioClip clip)
                {
                    clips.Add(clip);
                }
            }

            return clips;
        }

        /// <summary>
        ///     Duplicate ids and empty entries are both silent failures at runtime - the wrong entry plays,
        ///     or nothing does, with only a console warning to explain it. Say so while it can still be fixed.
        /// </summary>
        private static void DrawIdWarnings(AM manager)
        {
            string soundProblem = FindIdProblem(manager.SoundEntries, "sound");
            if (soundProblem != null)
            {
                EditorGUILayout.HelpBox(soundProblem, MessageType.Warning);
            }

            string musicProblem = FindIdProblem(manager.MusicEntries, "music");
            if (musicProblem != null)
            {
                EditorGUILayout.HelpBox(musicProblem, MessageType.Warning);
            }
        }

        private static string FindIdProblem<T>(IReadOnlyList<T> entries, string kind) where T : AudioEntry
        {
            var seen = new HashSet<string>();
            for (int index = 0; index < entries.Count; index++)
            {
                T entry = entries[index];
                if (entry == null)
                {
                    continue;
                }

                if (!string.IsNullOrEmpty(entry.Id) && !seen.Add(entry.Id))
                {
                    return $"Two {kind} entries share the id '{entry.Id}'. Only the first one will ever play.";
                }

                if (entry.IsEmpty)
                {
                    string name = string.IsNullOrEmpty(entry.Id) ? "#" + index : "'" + entry.Id + "'";
                    return $"The {kind} entry {name} has no clip assigned and will do nothing.";
                }
            }

            return null;
        }

        /// <summary>A readout of what is actually playing, so a wrong pool is visible instead of deduced.</summary>
        private void DrawRuntimeStatus(AM manager)
        {
            if (!Application.isPlaying)
            {
                return;
            }

            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Now Playing", EditorStyles.boldLabel);

            MusicEntry current = manager.CurrentMusicEntry;
            AudioClip clip = manager.GetCurrentMusicClip();
            EditorGUILayout.LabelField("Pool",
                current == null ? "-" : (string.IsNullOrEmpty(current.Id) ? "(clip)" : current.Id) +
                                        "  [" + current.Mode + "]");
            EditorGUILayout.LabelField("Track", clip == null ? "-" : clip.name);
            EditorGUILayout.LabelField("Music volume",
                manager.Music == null ? "-" : manager.Music.volume.ToString("0.00"));

            using (new EditorGUI.DisabledScope(current == null || current.ClipCount <= 1))
            {
                if (GUILayout.Button("Next Track"))
                {
                    manager.NextMusicTrack();
                }
            }

            Repaint();
        }
    }
}
