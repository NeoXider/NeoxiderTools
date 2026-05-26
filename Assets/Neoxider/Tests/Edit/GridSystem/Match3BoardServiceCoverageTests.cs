using System;
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
            Type type = typeof(Match3BoardService);
            Assert.IsNotNull(type.GetMethod(nameof(Match3BoardService.FindMatches)));
            Assert.IsNotNull(type.GetMethod(nameof(Match3BoardService.TryFindValidSwap)));
            Assert.IsNotNull(type.GetMethod(nameof(Match3BoardService.TrySwapAndResolve)));
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
