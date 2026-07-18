using System.Reflection;
using Neo.NoCode;
using NUnit.Framework;
using TMPro;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Neo.Editor.Tests.Edit
{
    /// <summary>
    ///     Regression: in Reactive mode a binding whose Find By Name target does not exist yet must keep polling
    ///     (poll fallback) and pick the value up once the object spawns, instead of freezing on the initial state.
    /// </summary>
    [TestFixture]
    public class NoCodeBindLateSpawnTests
    {
        private static readonly string SrcType = typeof(NoCodeBindEditModeFloatSource).FullName;

        private static void WireFindByName(NoCodeFloatBindingBehaviour target, string searchName, string member)
        {
            var so = new SerializedObject(target);
            SerializedProperty binding = so.FindProperty("_binding");
            binding.FindPropertyRelative("_useSceneSearch").boolValue = true;
            binding.FindPropertyRelative("_searchObjectName").stringValue = searchName;
            binding.FindPropertyRelative("_waitForObject").boolValue = true;
            binding.FindPropertyRelative("_sourceRoot").objectReferenceValue = null;
            binding.FindPropertyRelative("_componentTypeName").stringValue = SrcType;
            binding.FindPropertyRelative("_memberName").stringValue = member;
            // WHY: 0 = retry every poll so the edit-mode test is not blocked by the 1s Find throttle.
            binding.FindPropertyRelative("_findRetryIntervalSeconds").floatValue = 0f;
            so.FindProperty("_updateMode").enumValueIndex = (int)NoCodeFloatUpdateMode.Reactive;
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        private static void ForcePollTick(NoCodeFloatBindingBehaviour target)
        {
            const BindingFlags flags = BindingFlags.Instance | BindingFlags.NonPublic;
            typeof(NoCodeFloatBindingBehaviour).GetField("_nextPollTime", flags)?.SetValue(target, -1f);
            typeof(NoCodeFloatBindingBehaviour).GetMethod("LateUpdate", flags)?.Invoke(target, null);
        }

        [Test]
        public void Reactive_FindByName_LateSpawn_ResolvesOnNextPoll()
        {
            const string targetName = "LateSpawnedSource";
            var dst = new GameObject("dst");
            dst.SetActive(false);
            GameObject src = null;
            try
            {
                TMP_Text tmp = dst.AddComponent<TextMeshProUGUI>();
                NoCodeBindText bind = dst.AddComponent<NoCodeBindText>();
                WireFindByName(bind, targetName, nameof(NoCodeBindEditModeFloatSource.Score));

                // Object does not exist yet: first refresh finds no source and must arm the poll fallback.
                dst.SetActive(true);
                bind.EditorInvokeRefreshFromSource();
                // WHY: a code-added TMP_Text starts null; the point is the binding wrote nothing yet.
                Assert.IsTrue(string.IsNullOrEmpty(tmp.text), "No source yet: text stays untouched.");

                // Source appears later.
                src = new GameObject(targetName);
                src.AddComponent<NoCodeBindEditModeFloatSource>().Score = 42f;

                ForcePollTick(bind);

                Assert.AreEqual("42", tmp.text,
                    "Poll fallback must keep retrying and resolve the late-spawned source.");
            }
            finally
            {
                Object.DestroyImmediate(dst);
                if (src != null)
                {
                    Object.DestroyImmediate(src);
                }
            }
        }
    }
}
