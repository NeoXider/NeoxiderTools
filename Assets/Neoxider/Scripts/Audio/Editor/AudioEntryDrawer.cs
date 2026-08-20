using UnityEditor;
using UnityEngine;

namespace Neo.Audio.Editor
{
    /// <summary>
    ///     Inspector row for one <see cref="AudioEntry" />.
    ///     <para>
    ///         The default drawer for a nested class buries everything behind a foldout and then shows six
    ///         fields of equal weight, so "add a sound and set its volume" costs several clicks and a lot of
    ///         reading. Here the collapsed row already carries the three things that identify an entry - id,
    ///         how many clips it holds, and its volume slider - so the common tweak needs no expanding at
    ///         all. Everything else lives one click deeper.
    ///     </para>
    ///     <para>
    ///         <b>Drag several clips onto the row</b> and they are appended in one go. Filling an entry with
    ///         eight footstep variants is a single drag, not eight array-size edits.
    ///     </para>
    /// </summary>
    [CustomPropertyDrawer(typeof(AudioEntry), true)]
    public sealed class AudioEntryDrawer : PropertyDrawer
    {
        private const float Pad = 2f;

        public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
        {
            float line = EditorGUIUtility.singleLineHeight + Pad;
            if (!property.isExpanded)
            {
                return line;
            }

            float height = line;
            height += EditorGUI.GetPropertyHeight(property.FindPropertyRelative("_clips"), true) + Pad;
            height += line; // volume
            height += line; // randomize pitch

            SerializedProperty randomize = property.FindPropertyRelative("_randomizePitch");
            if (randomize != null && randomize.boolValue)
            {
                height += line * 2f;
            }

            if (property.FindPropertyRelative("_mode") != null)
            {
                height += line * 2f; // mode + fade override
            }

            height += line; // drop hint
            return height;
        }

        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            EditorGUI.BeginProperty(position, label, property);

            SerializedProperty id = property.FindPropertyRelative("_id");
            SerializedProperty clips = property.FindPropertyRelative("_clips");
            SerializedProperty volume = property.FindPropertyRelative("_volume");
            SerializedProperty randomize = property.FindPropertyRelative("_randomizePitch");
            SerializedProperty pitchMin = property.FindPropertyRelative("_pitchMin");
            SerializedProperty pitchMax = property.FindPropertyRelative("_pitchMax");
            SerializedProperty mode = property.FindPropertyRelative("_mode");
            SerializedProperty fade = property.FindPropertyRelative("_fadeDuration");

            float lineHeight = EditorGUIUtility.singleLineHeight;
            Rect row = new Rect(position.x, position.y, position.width, lineHeight);

            // WHY only the header row and not the whole entry: when the entry is expanded, its rect also
            // covers the Clips array, and swallowing drops there would break Unity's own per-element
            // assignment - dragging a clip onto element 2 to replace it would silently append instead.
            HandleClipDrop(row, clips);

            // WHY: the header is the whole point of this drawer - identity and the one value that gets
            // tweaked most, readable without expanding anything.
            Rect foldoutRect = new Rect(row.x, row.y, 14f, row.height);
            property.isExpanded = EditorGUI.Foldout(foldoutRect, property.isExpanded, GUIContent.none, true);

            float remaining = row.width - 16f;
            float countWidth = 42f;
            float volumeWidth = Mathf.Clamp(remaining * 0.42f, 90f, 220f);
            float idWidth = Mathf.Max(60f, remaining - countWidth - volumeWidth - 8f);

            Rect idRect = new Rect(row.x + 16f, row.y, idWidth, row.height);
            Rect countRect = new Rect(idRect.xMax + 4f, row.y, countWidth, row.height);
            Rect volumeRect = new Rect(countRect.xMax + 4f, row.y, row.width - (countRect.xMax + 4f - row.x),
                row.height);

            if (id != null)
            {
                string previous = id.stringValue;
                string typed = EditorGUI.TextField(idRect, previous);
                if (typed != previous)
                {
                    id.stringValue = typed;
                }

                if (string.IsNullOrEmpty(id.stringValue) && Event.current.type == EventType.Repaint)
                {
                    EditorGUI.LabelField(new Rect(idRect.x + 3f, idRect.y, idRect.width, idRect.height),
                        "id (optional)", EditorStyles.centeredGreyMiniLabel);
                }
            }

            int clipCount = clips != null ? clips.arraySize : 0;
            EditorGUI.LabelField(countRect,
                new GUIContent(clipCount == 1 ? "1 clip" : clipCount + " clips",
                    "Drag audio clips onto this row to add them all at once."),
                EditorStyles.miniLabel);

            if (volume != null)
            {
                EditorGUI.BeginChangeCheck();
                float value = EditorGUI.Slider(volumeRect, GUIContent.none, volume.floatValue, 0f, 1f);
                if (EditorGUI.EndChangeCheck())
                {
                    volume.floatValue = value;
                }
            }

            if (!property.isExpanded)
            {
                EditorGUI.EndProperty();
                return;
            }

            EditorGUI.indentLevel++;
            float y = row.yMax + Pad;
            float width = position.width;

            if (clips != null)
            {
                float clipsHeight = EditorGUI.GetPropertyHeight(clips, true);
                EditorGUI.PropertyField(new Rect(position.x, y, width, clipsHeight), clips,
                    new GUIContent("Clips", "Two or more -> a random one plays each time, never the same twice in a row."),
                    true);
                y += clipsHeight + Pad;
            }

            if (volume != null)
            {
                EditorGUI.PropertyField(new Rect(position.x, y, width, lineHeight), volume,
                    new GUIContent("Volume", "Multiplier of the channel volume. Final = channel x this."));
                y += lineHeight + Pad;
            }

            if (randomize != null)
            {
                EditorGUI.PropertyField(new Rect(position.x, y, width, lineHeight), randomize,
                    new GUIContent("Randomize Pitch", "Detune each play so repeats stop sounding identical."));
                y += lineHeight + Pad;

                if (randomize.boolValue)
                {
                    EditorGUI.PropertyField(new Rect(position.x, y, width, lineHeight), pitchMin,
                        new GUIContent("Pitch Min"));
                    y += lineHeight + Pad;
                    EditorGUI.PropertyField(new Rect(position.x, y, width, lineHeight), pitchMax,
                        new GUIContent("Pitch Max"));
                    y += lineHeight + Pad;
                }
            }

            if (mode != null)
            {
                EditorGUI.PropertyField(new Rect(position.x, y, width, lineHeight), mode,
                    new GUIContent("Mode",
                        "Loop: hold this track until the game switches. Shuffle: roll on to another track of " +
                        "the pool when this one ends."));
                y += lineHeight + Pad;

                EditorGUI.PropertyField(new Rect(position.x, y, width, lineHeight), fade,
                    new GUIContent("Fade Override", "Negative = use AM's Music Fade Duration."));
                y += lineHeight + Pad;
            }

            EditorGUI.LabelField(new Rect(position.x, y, width, lineHeight),
                "Tip: drag several clips onto the header row to add them all at once.",
                EditorStyles.centeredGreyMiniLabel);

            EditorGUI.indentLevel--;
            EditorGUI.EndProperty();
        }

        /// <summary>
        ///     Appends every dropped <see cref="AudioClip" /> to the entry. Accepts a drop anywhere on the
        ///     entry rect, so an eight-variant cue is one drag rather than eight array-size edits.
        /// </summary>
        private static void HandleClipDrop(Rect position, SerializedProperty clips)
        {
            if (clips == null || !clips.isArray)
            {
                return;
            }

            Event current = Event.current;
            if (current.type != EventType.DragUpdated && current.type != EventType.DragPerform)
            {
                return;
            }

            if (!position.Contains(current.mousePosition))
            {
                return;
            }

            bool hasClip = false;
            for (int index = 0; index < DragAndDrop.objectReferences.Length; index++)
            {
                if (DragAndDrop.objectReferences[index] is AudioClip)
                {
                    hasClip = true;
                    break;
                }
            }

            if (!hasClip)
            {
                return;
            }

            DragAndDrop.visualMode = DragAndDropVisualMode.Copy;

            if (current.type != EventType.DragPerform)
            {
                return;
            }

            DragAndDrop.AcceptDrag();

            for (int index = 0; index < DragAndDrop.objectReferences.Length; index++)
            {
                if (DragAndDrop.objectReferences[index] is not AudioClip clip)
                {
                    continue;
                }

                clips.arraySize++;
                clips.GetArrayElementAtIndex(clips.arraySize - 1).objectReferenceValue = clip;
            }

            current.Use();
        }
    }
}
