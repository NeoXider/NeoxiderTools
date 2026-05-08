using Neo.NoCode;
using Neo.Reactive;
using NUnit.Framework;
using TMPro;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Neo.Editor.Tests.Edit
{
    /// <summary>
    ///     Tests for the NoCodeBindText lazy-resolve fix (P0-4).
    ///     Verifies that components added AFTER OnEnable are still found.
    /// </summary>
    [TestFixture]
    public class NoCodeBindTextLazyResolveTests
    {
        private static readonly string SrcType = typeof(NoCodeBindEditModeFloatSource).FullName;

        private static void WireBinding(NoCodeFloatBindingBehaviour target, GameObject sourceRoot, string member)
        {
            SerializedObject so = new SerializedObject(target);
            SerializedProperty binding = so.FindProperty("_binding");
            binding.FindPropertyRelative("_sourceRoot").objectReferenceValue = sourceRoot;
            binding.FindPropertyRelative("_componentTypeName").stringValue = SrcType;
            binding.FindPropertyRelative("_memberName").stringValue = member;
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        private static void SetMode(NoCodeFloatBindingBehaviour target, NoCodeFloatUpdateMode mode)
        {
            SerializedObject so = new SerializedObject(target);
            so.FindProperty("_updateMode").enumValueIndex = (int)mode;
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        [Test]
        public void LazyResolve_TMPAddedBeforeBind_StillFindsComponent()
        {
            // Arrange: TMP added first, then Bind
            GameObject src = new GameObject("src");
            GameObject dst = new GameObject("dst");
            dst.SetActive(false);
            try
            {
                var fs = src.AddComponent<NoCodeBindEditModeFloatSource>();
                fs.Score = 7.5f;
                TMP_Text tmp = dst.AddComponent<TextMeshProUGUI>();
                NoCodeBindText bind = dst.AddComponent<NoCodeBindText>();
                WireBinding(bind, src, nameof(NoCodeBindEditModeFloatSource.Score));
                SetMode(bind, NoCodeFloatUpdateMode.Once);

                dst.SetActive(true);
                bind.EditorInvokeRefreshFromSource();

                Assert.AreEqual("7.5", tmp.text);
            }
            finally
            {
                Object.DestroyImmediate(dst);
                Object.DestroyImmediate(src);
            }
        }

        [Test]
        public void LazyResolve_BindAddedBeforeTMP_StillFindsComponent()
        {
            // Arrange: Bind added FIRST, TMP added AFTER → OnEnable won't find TMP
            // but lazy resolve in ApplyFloat should find it
            GameObject src = new GameObject("src");
            GameObject dst = new GameObject("dst");
            dst.SetActive(false);
            try
            {
                var fs = src.AddComponent<NoCodeBindEditModeFloatSource>();
                fs.Score = 3.14f;
                NoCodeBindText bind = dst.AddComponent<NoCodeBindText>();
                TMP_Text tmp = dst.AddComponent<TextMeshProUGUI>(); // added after bind
                WireBinding(bind, src, nameof(NoCodeBindEditModeFloatSource.Score));
                SetMode(bind, NoCodeFloatUpdateMode.Once);

                dst.SetActive(true);
                bind.EditorInvokeRefreshFromSource();

                Assert.AreEqual("3.14", tmp.text,
                    "Lazy resolve should find TMP added after NoCodeBindText");
            }
            finally
            {
                Object.DestroyImmediate(dst);
                Object.DestroyImmediate(src);
            }
        }

        [Test]
        public void LazyResolve_ReactiveMode_UpdatesPushedValue()
        {
            GameObject src = new GameObject("src");
            GameObject dst = new GameObject("dst");
            dst.SetActive(false);
            try
            {
                var fs = src.AddComponent<NoCodeBindEditModeFloatSource>();
                fs.ReactiveScore.Value = 1.0f;
                TMP_Text tmp = dst.AddComponent<TextMeshProUGUI>();
                NoCodeBindText bind = dst.AddComponent<NoCodeBindText>();
                WireBinding(bind, src, nameof(NoCodeBindEditModeFloatSource.ReactiveScore));
                SetMode(bind, NoCodeFloatUpdateMode.Reactive);

                dst.SetActive(true);
                bind.EditorInvokeRefreshFromSource();

                Assert.AreEqual("1", tmp.text);

                // Push reactive change
                fs.ReactiveScore.Value = 99.9f;
                Assert.AreEqual("99.9", tmp.text,
                    "Reactive push should update TMP text");
            }
            finally
            {
                Object.DestroyImmediate(dst);
                Object.DestroyImmediate(src);
            }
        }
    }
}
