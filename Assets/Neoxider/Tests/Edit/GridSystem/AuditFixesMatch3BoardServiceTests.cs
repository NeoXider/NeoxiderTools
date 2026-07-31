using System.Collections;
using System.Reflection;
using Neo.GridSystem.Match3;
using NUnit.Framework;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Neo.Editor.Tests.GridSystem
{
    /// <summary>
    ///     Covers the audit fix for Match3BoardService: hiding the board mid-cascade kills the resolve
    ///     coroutine before its tail clears the handle, which used to lock every swap until a full
    ///     InitializeBoard. The lifecycle hook must clear the handle instead.
    /// </summary>
    [TestFixture]
    public class AuditFixesMatch3BoardServiceTests
    {
        private GameObject _boardGo;
        private Match3BoardService _service;

        [SetUp]
        public void SetUp()
        {
            _boardGo = new GameObject("AuditFixesMatch3Board");
            _service = _boardGo.AddComponent<Match3BoardService>();
        }

        [TearDown]
        public void TearDown()
        {
            if (_boardGo != null)
            {
                Object.DestroyImmediate(_boardGo);
            }
        }

        [Test]
        public void OnDisable_IsDeclared_AndLeavesTheBoardUsable()
        {
            Assert.DoesNotThrow(() => InvokeLifecycle(_service, "OnDisable"));

            Assert.IsNull(GetResolveRoutineField().GetValue(_service),
                "the resolve handle must stay clear after a disable");
            Assert.DoesNotThrow(() => _service.ResolveCurrentMatchesButton());
        }

        [Test]
        public void OnDisable_ClearsPendingResolveRoutine()
        {
            Coroutine handle = _service.StartCoroutine(PendingRoutine());

            // WHY: EditMode never pumps coroutines; the test only needs a real handle to stand in for
            // the cascade that Unity kills on disable.
            Assume.That(handle, Is.Not.Null, "EditMode did not return a coroutine handle");

            FieldInfo field = GetResolveRoutineField();
            field.SetValue(_service, handle);

            InvokeLifecycle(_service, "OnDisable");

            Assert.IsNull(field.GetValue(_service),
                "OnDisable must clear the handle the killed coroutine can no longer clear");
        }

        private static IEnumerator PendingRoutine()
        {
            yield return null;
        }

        private static FieldInfo GetResolveRoutineField()
        {
            FieldInfo field = typeof(Match3BoardService).GetField("_resolveRoutine",
                BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.That(field, Is.Not.Null, "_resolveRoutine");
            return field;
        }

        private static void InvokeLifecycle(object target, string methodName)
        {
            MethodInfo method = target.GetType().GetMethod(methodName,
                BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.That(method, Is.Not.Null, $"{target.GetType().Name} must declare {methodName}");
            method.Invoke(target, null);
        }
    }
}
