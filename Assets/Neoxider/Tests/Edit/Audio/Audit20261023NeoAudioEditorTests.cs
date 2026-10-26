using System;
using System.Collections.Generic;
using System.Reflection;
using Neo.Audio;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Neo.Editor.Tests
{
    public sealed class Audit20261023NeoAudioEditorEntryHost : ScriptableObject
    {
        public SoundEntry Sound;
        public MusicEntry Music;
    }

    public sealed class Audit20261023NeoAudioEditorTests
    {
        private delegate string[] BuildOptionsCallback(List<string> ids, string current, out int selected);

        // The shared test asmdef has no Neo.Audio.Editor reference. Bind only the editor boundaries;
        // assertions below inspect the real serialized entries and the choices offered to the user.
        private static readonly Type EntryDrawerType =
            Type.GetType("Neo.Audio.Editor.AudioEntryDrawer, Neo.Audio.Editor", true);
        private static readonly Type IdDrawerType =
            Type.GetType("Neo.Audio.Editor.AudioIdDrawer, Neo.Audio.Editor", true);
        private static readonly Action<SerializedProperty, SerializedProperty, AudioClip> AppendClip =
            (Action<SerializedProperty, SerializedProperty, AudioClip>)EntryDrawerType.GetMethod(
                "AppendClip", BindingFlags.Static | BindingFlags.NonPublic).CreateDelegate(
                typeof(Action<SerializedProperty, SerializedProperty, AudioClip>));
        private static readonly BuildOptionsCallback BuildOptions =
            (BuildOptionsCallback)IdDrawerType.GetMethod("BuildOptions", BindingFlags.Static | BindingFlags.NonPublic)
                .CreateDelegate(typeof(BuildOptionsCallback));

        private readonly List<AudioClip> _clips = new List<AudioClip>();
        private Audit20261023NeoAudioEditorEntryHost _host;

        [SetUp]
        public void SetUp()
        {
            _host = ScriptableObject.CreateInstance<Audit20261023NeoAudioEditorEntryHost>();
        }

        [TearDown]
        public void TearDown()
        {
            Object.DestroyImmediate(_host);
            foreach (AudioClip clip in _clips)
            {
                Object.DestroyImmediate(clip);
            }

            _clips.Clear();
        }

        [TestCase(false, false)]
        [TestCase(false, true)]
        [TestCase(true, false)]
        [TestCase(true, true)]
        public void AppendAfterPublicClipListShrink_PreservesExistingLevelAndDoesNotMuteNewClip(
            bool music, bool emptySlotFirst)
        {
            AudioClip kept = CreateClip("kept");
            AudioClip removed = CreateClip("removed");
            AudioClip added = CreateClip("added");
            AudioEntry entry = CreateEntry(music, kept, removed);
            entry.SetClipVolume(0, 0.35f);
            entry.SetClipVolume(1, 0f);
            entry.Clips = new[] { kept };

            AppendThroughSerializedEntry(music, emptySlotFirst ? null : added, added);

            AudioEntry actual = music ? _host.Music : _host.Sound;
            Assert.That(actual.ClipCount, Is.EqualTo(2));
            Assert.That(actual.ClipAt(0), Is.SameAs(kept));
            Assert.That(actual.LastClipVolume, Is.EqualTo(0.35f).Within(0.0001f));
            Assert.That(actual.ClipAt(1), Is.SameAs(added));
            Assert.That(actual.LastClipVolume, Is.EqualTo(1f),
                "A new clip must not inherit the zero trim of the clip removed through the public API.");
        }

        [TestCase(false)]
        [TestCase(true)]
        public void FirstClipOfEmptiedEntry_DoesNotInheritAnOrphanedTrim(bool music)
        {
            AudioEntry entry = CreateEntry(music, CreateClip("removed"));
            entry.SetClipVolume(0, 0f);
            entry.Clips = Array.Empty<AudioClip>();
            AudioClip added = CreateClip("first");

            AppendThroughSerializedEntry(music, added, added);

            AudioEntry actual = music ? _host.Music : _host.Sound;
            Assert.That(actual.ClipCount, Is.EqualTo(1));
            Assert.That(actual.NextClip(), Is.SameAs(added));
            Assert.That(actual.LastClipVolume, Is.EqualTo(1f));
        }

        [TestCase(false)]
        [TestCase(true)]
        public void AppendWithSparseTrims_PreservesAuthoredTrimAndNeutralDefaults(bool music)
        {
            AudioEntry entry = CreateEntry(music, CreateClip("trimmed"), CreateClip("untrimmed"));
            entry.SetClipVolume(0, 0.4f);
            AudioClip added = CreateClip("new");

            AppendThroughSerializedEntry(music, added, added);

            AudioEntry actual = music ? _host.Music : _host.Sound;
            Assert.That(actual.ClipCount, Is.EqualTo(3));
            Assert.That(actual.ClipVolume(0), Is.EqualTo(0.4f).Within(0.0001f));
            Assert.That(actual.ClipVolume(1), Is.EqualTo(1f));
            Assert.That(actual.ClipAt(2), Is.SameAs(added));
            Assert.That(actual.LastClipVolume, Is.EqualTo(1f));
        }

        [Test]
        public void IdNamedNone_HasItsOwnSelectedChoiceAndDistinctLabel()
        {
            List<string> ids = new List<string> { "coin", "<none>" };

            string[] options = BuildOptions(ids, "<none>", out int selected);

            Assert.That(selected, Is.EqualTo(2), "The actual id must not select the empty choice.");
            Assert.That(options[selected], Does.Contain("<none>"));
            Assert.That(options[selected], Is.Not.EqualTo(options[0]));
            Assert.That(ids[selected - 1], Is.EqualTo("<none>"),
                "The display decoration must not become part of the serialized id.");
        }

        [Test]
        public void EmptyId_StillSelectsEmptyChoiceWhenAnIdIsNamedNone()
        {
            string[] options = BuildOptions(new List<string> { "<none>" }, string.Empty, out int selected);

            Assert.That(selected, Is.Zero);
            Assert.That(options.Length, Is.EqualTo(2));
            Assert.That(options[0], Is.Not.EqualTo(options[1]));
        }

        [Test]
        public void MissingId_KeepsASeparateChoiceEvenIfItsDecoratedLabelIsAnExistingId()
        {
            List<string> ids = new List<string> { "retired  (missing)", "coin" };

            string[] options = BuildOptions(ids, "retired", out int selected);

            Assert.That(selected, Is.EqualTo(3));
            Assert.That(options[selected], Does.Contain("retired"));
            BuildOptions(ids, "retired  (missing)", out int existingSelected);
            Assert.That(existingSelected, Is.EqualTo(1), "A legal decorated id is still a configured choice.");
        }

        [Test]
        public void MissingManagerResult_IsReusedUntilTheLookupCacheExpires()
        {
            FieldInfo managerField = IdDrawerType.GetField("_cachedManager", BindingFlags.Static | BindingFlags.NonPublic);
            FieldInfo stampField = IdDrawerType.GetField("_cacheStamp", BindingFlags.Static | BindingFlags.NonPublic);
            Func<AM> findManager = (Func<AM>)IdDrawerType.GetMethod("FindManager",
                BindingFlags.Static | BindingFlags.NonPublic).CreateDelegate(typeof(Func<AM>));
            object previousManager = managerField.GetValue(null);
            object previousStamp = stampField.GetValue(null);
            GameObject managerObject = new GameObject("Audit20261023NeoAudioEditorCache");
            try
            {
                managerObject.AddComponent<AM>();
                // Establish a fresh negative-cache result without depending on which scene the central
                // test run has open. An uncached lookup must find at least the manager created above.
                managerField.SetValue(null, null);
                stampField.SetValue(null, EditorApplication.timeSinceStartup);

                Assert.That(findManager(), Is.Null,
                    "Repeated empty dropdowns must reuse the miss instead of searching the scene again.");
                stampField.SetValue(null, double.NegativeInfinity);
                Assert.That(findManager(), Is.Not.Null, "A newly available manager appears after expiry.");
            }
            finally
            {
                managerField.SetValue(null, previousManager);
                stampField.SetValue(null, previousStamp);
                Object.DestroyImmediate(managerObject);
            }
        }

        private AudioEntry CreateEntry(bool music, params AudioClip[] clips)
        {
            if (music)
            {
                _host.Music = new MusicEntry("cue", clips);
                return _host.Music;
            }

            _host.Sound = new SoundEntry("cue", clips);
            return _host.Sound;
        }

        private void AppendThroughSerializedEntry(bool music, AudioClip appended, AudioClip assigned)
        {
            using (SerializedObject serialized = new SerializedObject(_host))
            {
                SerializedProperty entry = serialized.FindProperty(music ? "Music" : "Sound");
                SerializedProperty clips = entry.FindPropertyRelative("_clips");
                AppendClip(clips, entry.FindPropertyRelative("_clipVolumes"), appended);
                if (appended == null)
                {
                    clips.GetArrayElementAtIndex(clips.arraySize - 1).objectReferenceValue = assigned;
                }

                serialized.ApplyModifiedPropertiesWithoutUndo();
            }
        }

        private AudioClip CreateClip(string name)
        {
            AudioClip clip = AudioClip.Create(name, 64, 1, 8000, false);
            _clips.Add(clip);
            return clip;
        }
    }
}
