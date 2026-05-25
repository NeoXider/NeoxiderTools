using Neo.NoCode;
using Neo.Reactive;
using NUnit.Framework;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.TestTools;
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
        public ReactivePropertyInt ReactiveLevel = new ReactivePropertyInt(1);
        public float ScoreProperty => Score;
        public float GetScore() => Score;
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

        private static void InvokeBindingOnValidate(NoCodeFloatBindingBehaviour target)
        {
            typeof(NoCodeFloatBindingBehaviour)
                .GetMethod("OnValidate", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic)
                ?.Invoke(target, null);
        }

        private static void InvokeFormattedTextOnValidate(NoCodeFormattedText target)
        {
            typeof(NoCodeFormattedText)
                .GetMethod("OnValidate", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic)
                ?.Invoke(target, null);
        }

        private static void ForcePollTick(NoCodeFloatBindingBehaviour target)
        {
            const System.Reflection.BindingFlags flags =
                System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic;
            typeof(NoCodeFloatBindingBehaviour).GetField("_nextPollTime", flags)?.SetValue(target, -1f);
            typeof(NoCodeFloatBindingBehaviour).GetMethod("LateUpdate", flags)?.Invoke(target, null);
        }

        private static void ForceFormattedPollTick(NoCodeFormattedText target)
        {
            const System.Reflection.BindingFlags flags =
                System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic;
            typeof(NoCodeFormattedText).GetField("_nextPollTime", flags)?.SetValue(target, -1f);
            typeof(NoCodeFormattedText).GetMethod("LateUpdate", flags)?.Invoke(target, null);
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

        private static void WireFormattedText(NoCodeFormattedText target, GameObject sourceRoot, params string[] members)
        {
            SerializedObject so = new SerializedObject(target);
            SerializedProperty values = so.FindProperty("_values");
            Assert.IsNotNull(values, "_values");
            values.arraySize = members.Length;

            for (int i = 0; i < members.Length; i++)
            {
                SerializedProperty binding = values.GetArrayElementAtIndex(i);
                binding.FindPropertyRelative("_sourceRoot").objectReferenceValue = sourceRoot;
                binding.FindPropertyRelative("_componentTypeName").stringValue = SourceTypeFullName;
                binding.FindPropertyRelative("_memberName").stringValue = members[i];
            }

            so.ApplyModifiedPropertiesWithoutUndo();
        }

        private static void ConfigureFormattedText(NoCodeFormattedText target, string format,
            NoCodeFloatUpdateMode mode)
        {
            SerializedObject so = new SerializedObject(target);
            so.FindProperty("_format").stringValue = format;
            so.FindProperty("_updateMode").enumValueIndex = (int)mode;
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
        public void NoCodeBindText_Reactive_UpdatesTMPWhenIntReactiveChanges()
        {
            GameObject src = new GameObject("src");
            GameObject dst = new GameObject("dst");
            dst.SetActive(false);
            try
            {
                NoCodeBindEditModeFloatSource fs = src.AddComponent<NoCodeBindEditModeFloatSource>();
                fs.ReactiveLevel.Value = 2;
                TMP_Text tmp = dst.AddComponent<TextMeshProUGUI>();
                NoCodeBindText bind = dst.AddComponent<NoCodeBindText>();
                WireBinding(bind, src, nameof(NoCodeBindEditModeFloatSource.ReactiveLevel));
                SetUpdateMode(bind, NoCodeFloatUpdateMode.Reactive);

                dst.SetActive(true);
                bind.EditorInvokeRefreshFromSource();

                Assert.AreEqual("2", tmp.text);

                fs.ReactiveLevel.Value = 7;

                Assert.AreEqual("7", tmp.text);
            }
            finally
            {
                Object.DestroyImmediate(dst);
                Object.DestroyImmediate(src);
            }
        }

        [Test]
        public void NoCodeBindText_Reactive_PollsWhenMemberIsNotReactive()
        {
            GameObject src = new GameObject("src");
            GameObject dst = new GameObject("dst");
            dst.SetActive(false);
            try
            {
                NoCodeBindEditModeFloatSource fs = src.AddComponent<NoCodeBindEditModeFloatSource>();
                fs.Score = 40f;
                TMP_Text tmp = dst.AddComponent<TextMeshProUGUI>();
                NoCodeBindText bind = dst.AddComponent<NoCodeBindText>();
                WireBinding(bind, src, nameof(NoCodeBindEditModeFloatSource.Score));
                SetUpdateMode(bind, NoCodeFloatUpdateMode.Reactive);

                dst.SetActive(true);
                bind.EditorInvokeRefreshFromSource();

                Assert.AreEqual("40", tmp.text);

                fs.Score = 72f;
                ForcePollTick(bind);

                Assert.AreEqual("72", tmp.text);
            }
            finally
            {
                Object.DestroyImmediate(dst);
                Object.DestroyImmediate(src);
            }
        }

        [Test]
        public void NoCodeBindings_PollInterval_DefaultsTo016AndClampsTo0016()
        {
            GameObject dst = new GameObject("dst");
            try
            {
                SetProgress progress = dst.AddComponent<SetProgress>();
                SerializedObject so = new SerializedObject(progress);
                SerializedProperty interval = so.FindProperty("_pollIntervalSeconds");
                Assert.IsNotNull(interval, "_pollIntervalSeconds");
                Assert.That(interval.floatValue, Is.EqualTo(0.16f).Within(1e-6f));

                interval.floatValue = 0.001f;
                so.ApplyModifiedPropertiesWithoutUndo();

                InvokeBindingOnValidate(progress);
                so.Update();

                Assert.That(interval.floatValue, Is.EqualTo(0.016f).Within(1e-6f));

                NoCodeFormattedText formatted = dst.AddComponent<NoCodeFormattedText>();
                SerializedObject formattedSo = new SerializedObject(formatted);
                SerializedProperty formattedInterval = formattedSo.FindProperty("_pollIntervalSeconds");
                Assert.IsNotNull(formattedInterval, "NoCodeFormattedText._pollIntervalSeconds");
                Assert.That(formattedInterval.floatValue, Is.EqualTo(0.16f).Within(1e-6f));

                formattedInterval.floatValue = 0.001f;
                formattedSo.ApplyModifiedPropertiesWithoutUndo();

                InvokeFormattedTextOnValidate(formatted);
                formattedSo.Update();

                Assert.That(formattedInterval.floatValue, Is.EqualTo(0.016f).Within(1e-6f));
            }
            finally
            {
                Object.DestroyImmediate(dst);
            }
        }

        [Test]
        public void NoCodeFormattedText_FormatsMultipleValuesAndSubscribesToIntReactive()
        {
            GameObject src = new GameObject("src");
            GameObject dst = new GameObject("dst");
            dst.SetActive(false);
            try
            {
                NoCodeBindEditModeFloatSource fs = src.AddComponent<NoCodeBindEditModeFloatSource>();
                fs.Score = 12.5f;
                fs.ReactiveLevel.Value = 2;

                TMP_Text tmp = dst.AddComponent<TextMeshProUGUI>();
                NoCodeFormattedText formatted = dst.AddComponent<NoCodeFormattedText>();
                WireFormattedText(formatted, src,
                    nameof(NoCodeBindEditModeFloatSource.ReactiveLevel),
                    nameof(NoCodeBindEditModeFloatSource.Score));
                ConfigureFormattedText(formatted, "Level {0:0} | Score {1:0.0}", NoCodeFloatUpdateMode.Reactive);

                dst.SetActive(true);
                formatted.EditorInvokeRefreshFromSource();

                Assert.AreEqual("Level 2 | Score 12.5", tmp.text);

                fs.ReactiveLevel.Value = 3;

                Assert.AreEqual("Level 3 | Score 12.5", tmp.text);

                fs.Score = 99.5f;
                ForceFormattedPollTick(formatted);

                Assert.AreEqual("Level 3 | Score 99.5", tmp.text);
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

        [Test]
        public void ComponentFloatBindingContract_AllowsFieldsAndReadablePropertiesOnly()
        {
            System.Type sourceType = typeof(NoCodeBindEditModeFloatSource);

            Assert.IsTrue(ComponentFloatBinding.TryResolveSupportedSourceMember(
                sourceType,
                nameof(NoCodeBindEditModeFloatSource.Score),
                out System.Reflection.MemberInfo field));
            Assert.IsTrue(ComponentFloatBinding.IsSupportedSourceMember(field));

            Assert.IsTrue(ComponentFloatBinding.TryResolveSupportedSourceMember(
                sourceType,
                nameof(NoCodeBindEditModeFloatSource.ScoreProperty),
                out System.Reflection.MemberInfo property));
            Assert.IsTrue(ComponentFloatBinding.IsSupportedSourceMember(property));

            Assert.IsFalse(ComponentFloatBinding.TryResolveSupportedSourceMember(
                sourceType,
                nameof(NoCodeBindEditModeFloatSource.GetScore),
                out _));
        }

        [Test]
        public void ComponentFloatBinding_MethodName_DoesNotInvokeHiddenBehaviour()
        {
            GameObject src = new GameObject("src");
            GameObject dst = new GameObject("dst");
            try
            {
                NoCodeBindEditModeFloatSource fs = src.AddComponent<NoCodeBindEditModeFloatSource>();
                fs.Score = 55f;
                SetProgress progress = dst.AddComponent<SetProgress>();
                WireBinding(progress, src, nameof(NoCodeBindEditModeFloatSource.GetScore));
                LogAssert.Expect(LogType.Warning,
                    $"[Neo.NoCode] Property/field '{nameof(NoCodeBindEditModeFloatSource.GetScore)}' not found on '{SourceTypeFullName}' on '{src.name}'.");

                bool resolved = progress.Binding.TryReadFloat(progress, out float value);

                Assert.IsFalse(resolved);
                Assert.That(value, Is.EqualTo(0f));
            }
            finally
            {
                Object.DestroyImmediate(dst);
                Object.DestroyImmediate(src);
            }
        }
    }
}
