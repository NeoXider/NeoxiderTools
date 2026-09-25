using System.Reflection;
using Neo.Tools;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace Neo.Tests.Edit
{
    /// <summary>
    ///     Unity calls <c>OnValidate</c> on prefab assets as they load. A billboard that rotated there wrote a
    ///     <c>Camera.main</c>-dependent rotation into every prefab holding it the next time the project saved.
    /// </summary>
    public sealed class BillboardUniversalEditModeTests
    {
        private const BindingFlags InstanceMembers = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;

        private GameObject _cameraObject;
        private GameObject _host;

        [SetUp]
        public void SetUp()
        {
            _cameraObject = new GameObject("BillboardTests.MainCamera") { tag = "MainCamera" };
            _cameraObject.AddComponent<Camera>();
            _cameraObject.transform.position = new Vector3(7f, 2f, -5f);
            _host = new GameObject("BillboardTests.Host");
        }

        [TearDown]
        public void TearDown()
        {
            Object.DestroyImmediate(_host);
            Object.DestroyImmediate(_cameraObject);
        }

        [Test]
        public void Validation_InEditMode_DoesNotRotateOrCaptureCamera()
        {
            Quaternion authored = Quaternion.Euler(20f, 33f, 0f);
            _host.transform.SetPositionAndRotation(Vector3.zero, authored);
            BillboardUniversal billboard = _host.AddComponent<BillboardUniversal>();

            using (SerializedObject serialized = new(billboard))
            {
                SerializedProperty ignoreY = serialized.FindProperty("ignoreY");
                Assert.That(ignoreY, Is.Not.Null, "Field ignoreY is gone - update this test.");
                ignoreY.boolValue = !ignoreY.boolValue;
                serialized.ApplyModifiedPropertiesWithoutUndo();
            }

            // The asset-load path: Unity calls OnValidate without any edit. Absent method = nothing to call.
            typeof(BillboardUniversal).GetMethod("OnValidate", InstanceMembers)?.Invoke(billboard, null);

            Assert.That(Quaternion.Angle(_host.transform.rotation, authored), Is.LessThan(0.01f),
                "Validation rotated the Transform; on a prefab asset that rotation is saved to disk.");
            using (SerializedObject serialized = new(billboard))
            {
                Assert.That(serialized.FindProperty("targetCamera").objectReferenceValue, Is.Null,
                    "Validation wrote Camera.main into the serialized targetCamera field.");
            }
        }

        [Test]
        public void FaceCameraNow_ContextMenu_RotatesAwayFromTargetCamera()
        {
            _host.transform.SetPositionAndRotation(Vector3.zero, Quaternion.identity);
            BillboardUniversal billboard = _host.AddComponent<BillboardUniversal>();
            billboard.SetBillboardMode(BillboardUniversal.BillboardMode.AwayFromCamera);
            billboard.SetIgnoreY(true);

            using (SerializedObject serialized = new(billboard))
            {
                serialized.FindProperty("targetCamera").objectReferenceValue = _cameraObject.GetComponent<Camera>();
                serialized.ApplyModifiedPropertiesWithoutUndo();
            }

            MethodInfo faceCameraNow = typeof(BillboardUniversal).GetMethod("FaceCameraNowInEditor", InstanceMembers);
            Assert.That(faceCameraNow, Is.Not.Null, "The Face Camera Now context-menu method is gone.");
            faceCameraNow.Invoke(billboard, null);

            Vector3 away = _host.transform.position - _cameraObject.transform.position;
            away.y = 0f;
            Assert.That(Quaternion.Angle(_host.transform.rotation, Quaternion.LookRotation(away)), Is.LessThan(0.01f));
        }
    }
}
