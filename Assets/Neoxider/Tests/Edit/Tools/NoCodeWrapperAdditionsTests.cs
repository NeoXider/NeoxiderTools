using System.Collections.Generic;
using System.Reflection;
using Neo.Animations;
using Neo.Tools;
using Neo.UI;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;
using Object = UnityEngine.Object;

namespace Neo.Editor.Tests
{
    /// <summary>
    ///     The one-argument wrappers added in 12.0 so UnityEvents can drive what used to take an enum, two arguments
    ///     or an Inspector edit, plus the event initializers of components created from code.
    /// </summary>
    public sealed class NoCodeWrapperAdditionsTests
    {
        private readonly List<Object> _objects = new();

        [TearDown]
        public void TearDown()
        {
            foreach (Object item in _objects)
            {
                if (item != null)
                {
                    Object.DestroyImmediate(item);
                }
            }

            _objects.Clear();
        }

        [Test]
        public void AnchorMove_SettersClampApplyAndZeroThePosition()
        {
            GameObject host = Track(new GameObject("Anchored", typeof(RectTransform)));
            RectTransform rect = (RectTransform)host.transform;
            AnchorMove move = host.AddComponent<AnchorMove>();
            rect.anchoredPosition = new Vector2(30f, -12f);

            move.SetX(0.25f);
            Assert.That(rect.anchorMin, Is.EqualTo(new Vector2(0.25f, 0.5f)));
            Assert.That(rect.anchorMax, Is.EqualTo(rect.anchorMin));
            Assert.That(rect.anchoredPosition, Is.EqualTo(Vector2.zero));

            move.SetY(1.5f);
            Assert.That(move.y, Is.EqualTo(1f), "Anchors are clamped to 0-1.");
            move.SetAnchor(new Vector2(-1f, 0.75f));
            Assert.That(rect.anchorMin, Is.EqualTo(new Vector2(0f, 0.75f)));

            move.x = 0.6f;
            move.Apply();
            Assert.That(rect.anchorMax, Is.EqualTo(new Vector2(0.6f, 0.75f)));
        }

        [Test]
        public void CategoryBar_EnableAndDisableEntry_WrapSetEntryDisabled()
        {
            GameObject host = Track(new GameObject("Bar", typeof(RectTransform)));
            CategoryBar bar = host.AddComponent<CategoryBar>();
            bar.SetCategories(new[]
            {
                new CategoryBar.Entry { Id = "a" }, new CategoryBar.Entry { Id = "b" }
            });

            bar.DisableEntry(1);
            Assert.That(bar.Categories[1].Disabled, Is.True);
            Assert.That(bar.Select(1), Is.False, "A disabled entry cannot be selected.");

            bar.EnableEntry(1);
            Assert.That(bar.Categories[1].Disabled, Is.False);
            Assert.That(bar.Select(1), Is.True);
            bar.DisableEntry(7);
        }

        [Test]
        public void VariantView_Awake_AppliesTheSavedState()
        {
            GameObject host = Track(new GameObject("Variant", typeof(RectTransform)));
            Image image = Track(new GameObject("Swatch", typeof(RectTransform))).AddComponent<Image>();
            image.color = Color.white;
            VariantView view = host.AddComponent<VariantView>();
            SerializedObject serialized = new(view);
            SerializedProperty colors = serialized.FindProperty("_imageColors");
            colors.arraySize = 1;
            SerializedProperty entry = colors.GetArrayElementAtIndex(0);
            entry.FindPropertyRelative("image").objectReferenceValue = image;
            SerializedProperty palette = entry.FindPropertyRelative("colors");
            palette.arraySize = 2;
            palette.GetArrayElementAtIndex(0).colorValue = Color.red;
            palette.GetArrayElementAtIndex(1).colorValue = Color.green;
            serialized.FindProperty("_currentStateIndex").intValue = 1;
            serialized.ApplyModifiedPropertiesWithoutUndo();
            image.color = Color.white;

            typeof(VariantView).GetMethod("Awake", BindingFlags.Instance | BindingFlags.NonPublic)
                .Invoke(view, null);

            Assert.That(image.color, Is.EqualTo(Color.green), "A spawned view shows its saved state at once.");
        }

        [Test]
        public void MagneticFieldAndImpulseZone_IndexSetters_AcceptDefinedValuesOnly()
        {
            GameObject host = Track(new GameObject("Physics"));
            MagneticField field = host.AddComponent<MagneticField>();
            // WHY: ImpulseZone requires an (abstract) Collider, which AddComponent cannot add on its own.
            host.AddComponent<BoxCollider>().isTrigger = true;
            ImpulseZone zone = host.AddComponent<ImpulseZone>();

            field.SetModeIndex((int)MagneticField.FieldMode.Repel);
            Assert.That(field.CurrentMode, Is.EqualTo(MagneticField.FieldMode.Repel));
            field.SetModeIndex(99);
            Assert.That(field.CurrentMode, Is.EqualTo(MagneticField.FieldMode.Repel), "An undefined index is ignored.");

            zone.SetDirectionIndex((int)ImpulseZone.ImpulseDirection.TransformForward);
            Assert.That(zone.CurrentDirection, Is.EqualTo(ImpulseZone.ImpulseDirection.TransformForward));
            zone.SetDirectionIndex(-1);
            Assert.That(zone.CurrentDirection, Is.EqualTo(ImpulseZone.ImpulseDirection.TransformForward));
        }

        [Test]
        public void Leaderboard_SubmitScore_KeepsTheBestResultAndRoundsFloats()
        {
            GameObject host = Track(new GameObject("Leaderboard"));
            Leaderboard leaderboard = host.AddComponent<Leaderboard>();
            leaderboard.playerSaveKey = string.Empty;
            leaderboard.player = new LeaderboardUser("Player");
            leaderboard.users = new List<LeaderboardUser>();
            leaderboard.sortUsers = null;
            leaderboard.leaderboardItems = new List<LeaderboardItem>();
            leaderboard.sortOrder = SortOrder.Descending;

            leaderboard.SubmitScore(40);
            leaderboard.SubmitScore(25);
            Assert.That(leaderboard.player.score, Is.EqualTo(40), "A worse result does not replace the best one.");

            leaderboard.SubmitScore(41.6f);
            Assert.That(leaderboard.player.score, Is.EqualTo(42));
        }

        [Test]
        public void ComponentsAddedFromCode_HaveTheirUnityEvents()
        {
            GameObject host = Track(new GameObject("Events", typeof(RectTransform)));
            ColorAnimator color = host.AddComponent<ColorAnimator>();
            Vector3Animator vector = host.AddComponent<Vector3Animator>();
            FloatAnimator number = host.AddComponent<FloatAnimator>();
            TimerObject timer = host.AddComponent<TimerObject>();
            FakeLoad load = host.AddComponent<FakeLoad>();

            Assert.That(color.OnColorChanged, Is.Not.Null);
            Assert.That(color.OnAnimationPaused, Is.Not.Null);
            Assert.That(vector.OnVectorChanged, Is.Not.Null);
            Assert.That(number.OnAnimationStopped, Is.Not.Null);
            Assert.That(timer.OnTimerCompleted, Is.Not.Null);
            Assert.That(timer.OnDayTimeChanged, Is.Not.Null);
            Assert.That(load.OnFinisLoad, Is.Not.Null);
        }

        private T Track<T>(T item) where T : Object
        {
            _objects.Add(item);
            return item;
        }
    }
}
