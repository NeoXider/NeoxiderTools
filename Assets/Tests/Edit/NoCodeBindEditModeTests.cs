using Neo.NoCode;
using Neo.Reactive;
using NUnit.Framework;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;
using Object = UnityEngine.Object;

namespace Neo.Editor.Tests.Edit
{
    /// <summary>
    ///     Non-nested test-only component so <see cref="ComponentFloatBinding"/> type resolution matches
    ///     <c>GetType().Name</c> / <c>FullName</c> (nested types can fail lookup under Unity).
    /// </summary>
    public sealed class NoCodeBindEditModeFloatSource : MonoBehaviour
    {
        public float Score = 40f;
        public ReactivePropertyFloat ReactiveScore = new ReactivePropertyFloat(0.25f);
    }

    /// <summary>
    ///     Edit Mode coverage for <see cref="NoCodeBindText"/> and <see cref="SetProgress"/> (no Play Mode).
    /// </summary>
    public sealed class NoCodeBindEditModeTests
    {
        private static readonly string SourceTypeFullName = typeof(NoCodeBindEditModeFloatSource).FullName;

        private static void WireBinding(NoCodeFloatBindingBehaviour target, GameObject sourceRoot, string memberName)
        {
            SerializedObject so = new SerializedObject(target);
            SerializedProperty binding = so.FindProperty("_binding");
            Assert.IsNotNull(binding, "_binding");
            binding.FindPropertyRelative("_sourceRoot").objectReferenceValue = sourceRoot;
            binding.FindPropertyRelative("_componentTypeName").stringValue = SourceTypeFullName;
            binding.FindPropertyRelative("_memberName").stringValue = memberName;
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        private static void SetUpdateMode(NoCodeFloatBindingBehaviour target, NoCodeFloatUpdateMode mode)
        {
            SerializedObject so = new SerializedObject(target);
            SerializedProperty p = so.FindProperty("_updateMode");
            Assert.IsNotNull(p, "_updateMode");
            p.enumValueIndex = (int)mode;
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        private static void AssignSlider(SetProgress progress, Slider slider)
        {
            SerializedObject so = new SerializedObject(progress);
            so.FindProperty("_slider").objectReferenceValue = slider;
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        private static void AssignRange(SetProgress progress, float min, float max)
        {
            SerializedObject so = new SerializedObject(progress);
            so.FindProperty("_minValue").floatValue = min;
            so.FindProperty("_maxValue").floatValue = max;
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        [Test]
        public void NoCodeBindText_TMPFallback_ShowsInvariantFloat()
        {
            GameObject src = new GameObject("src");
            GameObject dst = new GameObject("dst");
            dst.SetActive(false);
            try
            {
                NoCodeBindEditModeFloatSource fs = src.AddComponent<NoCodeBindEditModeFloatSource>();
                fs.Score = 12.5f;
                TMP_Text tmp = dst.AddComponent<TextMeshProUGUI>();
                NoCodeBindText bind = dst.AddComponent<NoCodeBindText>();
                WireBinding(bind, src, nameof(NoCodeBindEditModeFloatSource.Score));
                SetUpdateMode(bind, NoCodeFloatUpdateMode.Once);

                dst.SetActive(true);
                bind.EditorInvokeRefreshFromSource();

                Assert.AreEqual("12.5", tmp.text);
            }
            finally
            {
                Object.DestroyImmediate(dst);
                Object.DestroyImmediate(src);
            }
        }

        [Test]
        public void SetProgress_InverseLerp_SetsSliderNormalized()
        {
            GameObject src = new GameObject("src");
            GameObject dst = new GameObject("dst");
            dst.SetActive(false);
            try
            {
                NoCodeBindEditModeFloatSource fs = src.AddComponent<NoCodeBindEditModeFloatSource>();
                fs.Score = 25f;
                Slider slider = dst.AddComponent<Slider>();
                SetProgress prog = dst.AddComponent<SetProgress>();
                WireBinding(prog, src, nameof(NoCodeBindEditModeFloatSource.Score));
                AssignSlider(prog, slider);
                AssignRange(prog, 0f, 100f);
                SetUpdateMode(prog, NoCodeFloatUpdateMode.Once);

                dst.SetActive(true);
                prog.EditorInvokeRefreshFromSource();

                Assert.That(slider.normalizedValue, Is.EqualTo(0.25f).Within(1e-5f));
            }
            finally
            {
                Object.DestroyImmediate(dst);
                Object.DestroyImmediate(src);
            }
        }

        [Test]
        public void SetProgress_ImageFill_InverseLerp()
        {
            GameObject src = new GameObject("src");
            GameObject dst = new GameObject("dst");
            dst.SetActive(false);
            try
            {
                NoCodeBindEditModeFloatSource fs = src.AddComponent<NoCodeBindEditModeFloatSource>();
                fs.Score = 50f;
                Image image = dst.AddComponent<Image>();
                SetProgress prog = dst.AddComponent<SetProgress>();
                WireBinding(prog, src, nameof(NoCodeBindEditModeFloatSource.Score));

                SerializedObject so = new SerializedObject(prog);
                so.FindProperty("_image").objectReferenceValue = image;
                so.FindProperty("_minValue").floatValue = 0f;
                so.FindProperty("_maxValue").floatValue = 200f;
                so.FindProperty("_updateMode").enumValueIndex = (int)NoCodeFloatUpdateMode.Once;
                so.ApplyModifiedPropertiesWithoutUndo();

                dst.SetActive(true);
                prog.EditorInvokeRefreshFromSource();

                Assert.That(image.fillAmount, Is.EqualTo(0.25f).Within(1e-5f));
            }
            finally
            {
                Object.DestroyImmediate(dst);
                Object.DestroyImmediate(src);
            }
        }

        [Test]
        public void SetProgress_Reactive_UpdatesSliderWhenReactiveChanges()
        {
            GameObject src = new GameObject("src");
            GameObject dst = new GameObject("dst");
            dst.SetActive(false);
            try
            {
                NoCodeBindEditModeFloatSource fs = src.AddComponent<NoCodeBindEditModeFloatSource>();
                fs.ReactiveScore.Value = 0.1f;
                Slider slider = dst.AddComponent<Slider>();
                SetProgress prog = dst.AddComponent<SetProgress>();
                WireBinding(prog, src, nameof(NoCodeBindEditModeFloatSource.ReactiveScore));
                AssignSlider(prog, slider);
                AssignRange(prog, 0f, 1f);
                SetUpdateMode(prog, NoCodeFloatUpdateMode.Reactive);

                dst.SetActive(true);
                prog.EditorInvokeRefreshFromSource();

                Assert.That(slider.normalizedValue, Is.EqualTo(0.1f).Within(1e-5f));

                fs.ReactiveScore.Value = 0.9f;

                Assert.That(slider.normalizedValue, Is.EqualTo(0.9f).Within(1e-5f));
            }
            finally
            {
                Object.DestroyImmediate(dst);
                Object.DestroyImmediate(src);
            }
        }

        [Test]
        public void SetProgress_SceneSearchByName_InverseLerp()
        {
            GameObject src = new GameObject("BindByName");
            GameObject dst = new GameObject("dst");
            dst.SetActive(false);
            try
            {
                NoCodeBindEditModeFloatSource fs = src.AddComponent<NoCodeBindEditModeFloatSource>();
                fs.Score = 40f;
                Slider slider = dst.AddComponent<Slider>();
                SetProgress prog = dst.AddComponent<SetProgress>();
                SerializedObject so = new SerializedObject(prog);
                SerializedProperty binding = so.FindProperty("_binding");
                Assert.IsNotNull(binding);
                binding.FindPropertyRelative("_useSceneSearch").boolValue = true;
                binding.FindPropertyRelative("_searchObjectName").stringValue = "BindByName";
                binding.FindPropertyRelative("_waitForObject").boolValue = false;
                binding.FindPropertyRelative("_sourceRoot").objectReferenceValue = null;
                binding.FindPropertyRelative("_componentTypeName").stringValue = SourceTypeFullName;
                binding.FindPropertyRelative("_memberName").stringValue = nameof(NoCodeBindEditModeFloatSource.Score);
                so.ApplyModifiedPropertiesWithoutUndo();

                AssignSlider(prog, slider);
                AssignRange(prog, 0f, 100f);
                SetUpdateMode(prog, NoCodeFloatUpdateMode.Once);

                dst.SetActive(true);
                prog.EditorInvokeRefreshFromSource();

                Assert.That(slider.normalizedValue, Is.EqualTo(0.4f).Within(1e-5f));
            }
            finally
            {
                Object.DestroyImmediate(dst);
                Object.DestroyImmediate(src);
            }
        }
    }
}
