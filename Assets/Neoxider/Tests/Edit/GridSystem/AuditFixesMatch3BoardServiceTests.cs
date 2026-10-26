using System.Collections;
using System.Reflection;
using Neo.GridSystem;
using Neo.GridSystem.Match3;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
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

        [UnityTest]
        public IEnumerator OnDisable_ClearsPendingResolveRoutineAndStopsCascadeWork()
        {
            Object.DestroyImmediate(_boardGo);
            yield return new EnterPlayMode();
            GameObject root = new GameObject("Live Match3 Stop", typeof(Grid), typeof(FieldGenerator));
            Random.State randomBefore = Random.state;
            try
            {
                FieldGenerator generator = root.GetComponent<FieldGenerator>();
                generator.GenerateField(new FieldGeneratorConfig { Size = new Vector3Int(3, 1, 1) });
                Match3BoardService service = root.AddComponent<Match3BoardService>();
                SetField(service, "_autoGenerateOnStart", false);
                SetField(service, "_useResolveDelay", true);
                SetField(service, "_resolveDelaySeconds", 0.03f);
                for (int x = 0; x < 3; x++) generator.SetContentId(new Vector3Int(x, 0, 0), (int)Match3TileState.Red);
                int phases = 0;
                int changed = 0;
                int completed = 0;
                service.OnResolvePhase += (phase, count) => phases++;
                service.OnBoardChanged.AddListener(() => changed++);
                service.OnMatchesResolved.AddListener(count => completed++);
                Assert.That(service.FindMatches(), Has.Count.EqualTo(1));
                service.ResolveCurrentMatchesButton();
                Assert.That(phases, Is.EqualTo(1), "Positive control: a real Clear phase ran before its delay.");
                Assert.That(changed, Is.EqualTo(1));
                Assert.That(GetResolveRoutineField().GetValue(service), Is.Not.Null);
                service.enabled = false; // Real Unity OnDisable delivery; component disable alone does not stop coroutines.
                Assert.That(GetResolveRoutineField().GetValue(service), Is.Null);
                yield return new WaitForSecondsRealtime(0.15f);
                Assert.That(phases, Is.EqualTo(1), "No Collapse/Refill/Completed work may follow disable.");
                Assert.That(changed, Is.EqualTo(1));
                Assert.That(completed, Is.Zero);
                for (int x = 0; x < 3; x++)
                    Assert.That(generator.GetCell(new Vector3Int(x, 0, 0)).ContentId, Is.EqualTo((int)Match3TileState.None));
            }
            finally
            {
                Object.DestroyImmediate(root);
                Random.state = randomBefore;
            }
            yield return new ExitPlayMode();
        }

        [UnityTearDown]
        public IEnumerator LeavePlayModeAfterFailure()
        {
            if (Application.isPlaying) yield return new ExitPlayMode();
        }

        private static void SetField(object target, string name, object value)
        {
            FieldInfo field = target.GetType().GetField(name, BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.That(field, Is.Not.Null, name);
            field.SetValue(target, value);
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
