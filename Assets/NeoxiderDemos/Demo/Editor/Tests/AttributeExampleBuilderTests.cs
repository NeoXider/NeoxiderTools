using System;
using System.Collections.Generic;
using System.IO;
using Neo.Samples;
using NUnit.Framework;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using Object = UnityEngine.Object;

namespace Neo.Demo.Editor.Tests
{
    /// <summary>
    ///     Edit-mode contracts of the Neo.Editor attribute demo builder (Scenes/Tools/AttributeExample.unity): the
    ///     shipped scene is copied, built twice, and the hand-authored objects, the resolved-field labels, the
    ///     persistent wiring to the [Button] methods and the multi-select target are checked; the second pass must
    ///     change nothing.
    /// </summary>
    public sealed class AttributeExampleBuilderTests
    {
        private const string ScenePath = "Scenes/Tools/AttributeExample.unity";
        private const string AttributeObjectName = "Attribute";
        private const string SecondTargetName = "Attribute B";
        private const string CompareLabel = "Compare with C#";
        private const string KickLabel = "Kick bodies";
        private const string ResetLabel = "Reset bodies";
        private const string PrintHelloLabel = "PrintHello()";
        private const string AddScoreLabel = "AddScore(10)";
        private const string ResetScoreLabel = "ResetScore()";
        private const int AddScoreAmount = 10;

        private static readonly string[] BodyNames = { "Cube", "Ball", "Ball (1)" };

        private static readonly Vector3[] BodyPositions =
        {
            new(-3f, -2.8f, 0f),
            new(-0.7f, -2.8f, 0f),
            new(1.6f, -2.8f, 0f)
        };

        private string _folder;
        private Scene _scene;
        private Scene _previousActive;

        [SetUp]
        public void SetUp()
        {
            _previousActive = SceneManager.GetActiveScene();
        }

        [TearDown]
        public void TearDown()
        {
            if (_scene.IsValid() && _scene.isLoaded)
            {
                EditorSceneManager.CloseScene(_scene, true);
            }

            if (_previousActive.IsValid() && _previousActive.isLoaded)
            {
                SceneManager.SetActiveScene(_previousActive);
            }

            if (!string.IsNullOrEmpty(_folder))
            {
                AssetDatabase.DeleteAsset(_folder);
                _folder = null;
            }
        }

        [Test]
        public void AttributeExampleBuilder_DecoratesTheShippedSceneIdempotently()
        {
            DemoSceneBuilderInfo builder = Builder("AttributeExampleBuilder.Build");
            OpenSceneCopy(ScenePath);
            GameObject attributeObject = Root(AttributeObjectName);
            Assert.That(attributeObject, Is.Not.Null, "The shipped scene has the hand-authored 'Attribute' object.");
            AttributeTest attributes = attributeObject.GetComponent<AttributeTest>();
            Assert.That(attributes, Is.Not.Null);
            int storedBodies = attributes.rbsFindAllInScene.Length;

            DemoSceneResult first = DemoSceneRunner.BuildOpenScene(_scene, new[] { builder });
            Assert.That(first.Success, Is.True, first.Summary());
            Assert.That(Root(AttributeObjectName), Is.SameAs(attributeObject));
            Assert.That(attributes.rbsFindAllInScene.Length, Is.EqualTo(storedBodies), "Filled fields are kept.");

            AttributeDemoController controller = Single<AttributeDemoController>();
            Assert.That(controller.IsSceneBuilt, Is.True);
            Assert.That(controller.Attributes, Is.SameAs(attributes));
            Assert.That(Single<ModuleDemoSceneInfo>().HasAuthoredOverlay, Is.True);

            string[] expected = AttributeDemoController.DescribeFields(attributes);
            Assert.That(FieldValue("rbsFindAllInScene"), Is.EqualTo(expected[AttributeDemoController.RigidbodiesField]),
                "Resolved values are visible in Edit Mode.");
            Assert.That(FieldValue("camFindInScene"), Is.EqualTo(expected[AttributeDemoController.CameraField]));
            Assert.That(FieldValue("toggleGetComponent"), Is.EqualTo(expected[AttributeDemoController.ToggleField]));

            Assert.That(Describe(CompareLabel), Is.EqualTo("AttributeDemoController.CompareWithCSharp"));
            Assert.That(Describe(KickLabel), Is.EqualTo("AttributeDemoController.KickBodies"));
            Assert.That(Describe(ResetLabel), Is.EqualTo("AttributeDemoController.ResetBodies"));
            Assert.That(Describe(PrintHelloLabel), Is.EqualTo("AttributeTest.PrintHello; DemoLogFeed.Log"));
            Assert.That(Describe(AddScoreLabel),
                Is.EqualTo("AttributeTest.AddScore; AttributeDemoController.RefreshScore"));
            Assert.That(Describe(ResetScoreLabel),
                Is.EqualTo("AttributeTest.ResetScore; AttributeDemoController.RefreshScore"));
            Assert.That(FindButton(AddScoreLabel).onClick.GetPersistentTarget(0), Is.SameAs(attributes),
                "The UI calls the hand-authored AttributeTest.");

            for (int i = 0; i < BodyNames.Length; i++)
            {
                GameObject body = Root(BodyNames[i]);
                Assert.That(body, Is.Not.Null, BodyNames[i] + " is kept.");
                Assert.That(Vector3.Distance(body.transform.position, BodyPositions[i]), Is.LessThan(0.001f),
                    BodyNames[i] + " stands on the stage.");
            }

            List<AttributeTest> all = All<AttributeTest>();
            Assert.That(all.Count, Is.EqualTo(2), "Attribute plus the multi-select target.");
            AttributeTest second = all.Find(candidate => candidate != attributes);
            Assert.That(second.name, Is.EqualTo(SecondTargetName));
            Assert.That(DemoSceneBuildContext.IsGenerated(second.gameObject), Is.True);
            Assert.That(second.rbsFindAllInScene, Is.EqualTo(attributes.rbsFindAllInScene));
            // WHY: ToggleObject lives in Neo.Tools.InteractableObject, which this test assembly does not reference.
            Object secondToggle = new SerializedObject(second).FindProperty("toggleGetComponent").objectReferenceValue;
            Assert.That(secondToggle, Is.Not.Null);
            Assert.That(secondToggle, Is.SameAs(second.GetComponent("ToggleObject")));

            DemoSceneResult secondPass = DemoSceneRunner.BuildOpenScene(_scene, new[] { builder });
            Assert.That(secondPass.Success, Is.True, secondPass.Summary());
            Assert.That(secondPass.Created, Is.Zero, secondPass.Summary());
            Assert.That(secondPass.Removed, Is.Zero, secondPass.Summary());
            Assert.That(secondPass.Listeners, Is.EqualTo(first.Listeners));
        }

        [Test]
        public void DescribeList_ListsThreeNamesThenTheRest()
        {
            GameObject a = new("A");
            GameObject b = new("B");
            GameObject c = new("C");
            GameObject d = new("D");
            try
            {
                Assert.That(AttributeDemoController.DescribeList(new Object[] { a, b, c, d }), Is.EqualTo("4 · A, B, C +1"));
                Assert.That(AttributeDemoController.DescribeList(new Object[] { a, null }), Is.EqualTo("2 · A, (missing)"));
                Assert.That(AttributeDemoController.DescribeList(null), Is.EqualTo(AttributeDemoController.EmptyText));
                Assert.That(AttributeDemoController.DescribeList(Array.Empty<Object>()),
                    Is.EqualTo(AttributeDemoController.EmptyText));
            }
            finally
            {
                Object.DestroyImmediate(a);
                Object.DestroyImmediate(b);
                Object.DestroyImmediate(c);
                Object.DestroyImmediate(d);
            }
        }

        [Test]
        public void SameSet_IgnoresOrderAndNullEntries()
        {
            GameObject a = new("A");
            GameObject b = new("B");
            try
            {
                Assert.That(AttributeDemoController.SameSet(new Object[] { a, b }, new Object[] { b, a }), Is.True);
                Assert.That(AttributeDemoController.SameSet(new Object[] { a, null, b }, new Object[] { b, a }), Is.True);
                Assert.That(AttributeDemoController.SameSet(new Object[] { a }, new Object[] { a, b }), Is.False);
                Assert.That(AttributeDemoController.SameSet(new Object[] { a, a }, new Object[] { a, b }), Is.False);
                Assert.That(AttributeDemoController.SameSet(null, Array.Empty<Object>()), Is.True);
            }
            finally
            {
                Object.DestroyImmediate(a);
                Object.DestroyImmediate(b);
            }
        }

        [Test]
        public void AttributeTest_ScoreButtonsSetTheField()
        {
            GameObject host = new("AttributeTest Score");
            try
            {
                AttributeTest attributes = host.AddComponent<AttributeTest>();
                attributes.AddScore(AddScoreAmount);
                attributes.AddScore(5);
                Assert.That(attributes.score, Is.EqualTo(AddScoreAmount + 5));
                attributes.ResetScore();
                Assert.That(attributes.score, Is.Zero);
            }
            finally
            {
                Object.DestroyImmediate(host);
            }
        }

        private static DemoSceneBuilderInfo Builder(string idSuffix)
        {
            DemoSceneBuilderInfo builder = DemoSceneRunner.DiscoverBuilders()
                .Find(info => info.Id.EndsWith(idSuffix, StringComparison.Ordinal));
            Assert.That(builder, Is.Not.Null, idSuffix + " must be registered.");
            Assert.That(builder.Error, Is.Null);
            return builder;
        }

        private void OpenSceneCopy(string relativeScenePath)
        {
            string source = DemoSampleRoots.Demo + "/" + relativeScenePath;
            Assert.That(AssetDatabase.LoadAssetAtPath<SceneAsset>(source), Is.Not.Null, "Shipped scene missing: " + source);
            string folderName = "__NeoAttributeDemoTests_" + Guid.NewGuid().ToString("N");
            AssetDatabase.CreateFolder("Assets", folderName);
            _folder = "Assets/" + folderName;
            string path = _folder + "/" + Path.GetFileName(relativeScenePath);
            Assert.That(AssetDatabase.CopyAsset(source, path), Is.True, "Could not copy " + source);
            _scene = EditorSceneManager.OpenScene(path, OpenSceneMode.Additive);
            Assert.That(_scene.IsValid() && _scene.isLoaded, Is.True, "Temporary scene did not open: " + path);
            SceneManager.SetActiveScene(_scene);
        }

        private string FieldValue(string field)
        {
            foreach (RectTransform row in All<RectTransform>())
            {
                if (row.name != "Field · " + field)
                {
                    continue;
                }

                Transform value = row.Find("Value");
                Assert.That(value, Is.Not.Null, "Field row '" + field + "' has a Value label.");
                return value.GetComponent<TMP_Text>().text;
            }

            Assert.Fail("Field row '" + field + "' not found.");
            return null;
        }

        private string Describe(string label)
        {
            return DemoWiring.Describe(FindButton(label).onClick);
        }

        private GameObject Root(string name)
        {
            foreach (GameObject root in _scene.GetRootGameObjects())
            {
                if (root.name == name)
                {
                    return root;
                }
            }

            return null;
        }

        private Button FindButton(string label)
        {
            foreach (Button button in All<Button>())
            {
                if (button.name == "Btn_" + label)
                {
                    return button;
                }
            }

            Assert.Fail("Button 'Btn_" + label + "' not found.");
            return null;
        }

        private List<T> All<T>() where T : Component
        {
            List<T> found = new();
            foreach (GameObject root in _scene.GetRootGameObjects())
            {
                found.AddRange(root.GetComponentsInChildren<T>(true));
            }

            return found;
        }

        private T Single<T>() where T : Component
        {
            List<T> found = All<T>();
            Assert.That(found.Count, Is.EqualTo(1), typeof(T).Name);
            return found[0];
        }
    }
}
