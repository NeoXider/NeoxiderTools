using System;
using System.Linq;
using Neo.GridSystem.Match3;
using NUnit.Framework;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Neo.Editor.Tests.GridSystem
{
    [TestFixture]
    public class Match3BoardServiceCoverageTests
    {
        [Test]
        public void Match3BoardService_ExposesExpectedPublicApi()
        {
            // WHY: match by name only — TrySwapAndResolve has Vector2Int/Vector3Int overloads, so
            // GetMethod(name) would throw AmbiguousMatchException.
            string[] methods = typeof(Match3BoardService).GetMethods().Select(m => m.Name).ToArray();
            Assert.Contains(nameof(Match3BoardService.FindMatches), methods);
            Assert.Contains(nameof(Match3BoardService.TryFindValidSwap), methods);
            Assert.Contains(nameof(Match3BoardService.TrySwapAndResolve), methods);
        }

        [Test]
        public void Match3BoardService_MethodCalls_DoNotCrash_WhenNoBoardConfigured()
        {
            var owner = new GameObject("Match3BoardServiceCoverage");
            Match3BoardService service = owner.AddComponent<Match3BoardService>();

            Assert.DoesNotThrow(() =>
            {
                service.InitializeBoard();
            });
            Assert.DoesNotThrow(() =>
            {
                service.FindMatches();
            });
            Assert.DoesNotThrow(() =>
            {
                service.TryFindValidSwap(out _, out _);
            });
            Assert.DoesNotThrow(() =>
            {
                service.TrySwapAndResolve(Vector3Int.zero, Vector3Int.right);
            });

            Object.DestroyImmediate(owner);
        }
    }
}
