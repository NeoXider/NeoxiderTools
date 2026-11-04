using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using Neo.GridSystem;
using Neo.GridSystem.Match3;
using Neo.Tests.Performance;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using Object = UnityEngine.Object;

namespace Neo.Editor.Tests.GridSystem
{
    /// <summary>
    ///     Contracts of the 2026-11 Match3 closure pass: the allocation-free match queries of
    ///     <see cref="Match3MatchFinder" />, allocation-free warm resolve cascades and no-moves shuffles, the injectable
    ///     <see cref="Match3BoardService.RandomIndexProvider" />, and the busy state and per-step buffer use of a delayed
    ///     resolve.
    /// </summary>
    [TestFixture]
    public sealed class Match3ResolveHotPathTests
    {
        private const int BoardSize = 6;

        private static readonly Vector3Int[] FourDirections =
        {
            Vector3Int.right, Vector3Int.left, Vector3Int.up, Vector3Int.down
        };

        // WHY: a static patch keeps Seed allocation-free inside the measured windows.
        private static readonly int[,] CanonicalPatch = { { 1, 3, 1 }, { 2, 1, 2 } };

        private readonly List<GameObject> _roots = new();

        [TearDown]
        public void TearDown()
        {
            DestroyRoots();
        }

        [Test]
        public void MatchFinder_HasMatchAndCollectMatchedCells_AgreeWithFindMatchesOnRandomBoards()
        {
            System.Random random = new System.Random(20261103);
            FieldGenerator field = CreateService(out _).GetComponent<FieldGenerator>();
            HashSet<FieldCell> collected = new();
            HashSet<FieldCell> union = new();
            int positives = 0;
            for (int board = 0; board < 200; board++)
            {
                Vector3Int size = new Vector3Int(random.Next(1, 9), random.Next(1, 9), random.Next(0, 4) == 0 ? 2 : 1);
                field.GenerateField(new FieldGeneratorConfig(size, new MovementRule(FourDirections)));
                int colors = random.Next(2, 6);
                foreach (FieldCell cell in field.GetAllCells())
                {
                    int roll = random.Next(0, 30);
                    cell.IsEnabled = roll != 0;
                    cell.IsWalkable = roll != 1;
                    cell.IsOccupied = roll == 2;
                    cell.ContentId = roll == 3 ? 0 : random.Next(1, colors + 1);
                }

                int minimum = random.Next(0, 5);
                List<List<FieldCell>> groups = Match3MatchFinder.FindMatches(field, minimum);
                union.Clear();
                foreach (List<FieldCell> group in groups)
                {
                    union.UnionWith(group);
                }

                int count = Match3MatchFinder.CollectMatchedCells(field, minimum, collected);

                Assert.That(Match3MatchFinder.HasMatch(field, minimum), Is.EqualTo(groups.Count > 0), "Board " + board);
                Assert.That(count, Is.EqualTo(union.Count), "Board " + board);
                Assert.That(collected.SetEquals(union), Is.True, "Board " + board);
                positives += groups.Count > 0 ? 1 : 0;
            }

            Assert.That(positives, Is.GreaterThan(0).And.LessThan(200), "The random set must exercise both outcomes.");
        }

        [Test]
        public void MatchFinder_InvalidInputs_AreHandled()
        {
            Assert.That(Match3MatchFinder.HasMatch(null), Is.False);
            Assert.That(Match3MatchFinder.CollectMatchedCells(null, 3, new HashSet<FieldCell>()), Is.Zero);
            Assert.Throws<ArgumentNullException>(() =>
                Match3MatchFinder.CollectMatchedCells(CreateService(out _).GetComponent<FieldGenerator>(), 3, null));
        }

        [Test]
        public void MatchFinder_WarmQueries_DoNotAllocate()
        {
            FieldGenerator field = CreateService(out _).GetComponent<FieldGenerator>();
            Seed(field);
            HashSet<FieldCell> matched = new();
            field.Cells[0, 0, 0].ContentId = 7;
            field.Cells[1, 0, 0].ContentId = 7;
            field.Cells[2, 0, 0].ContentId = 7;
            Assert.That(Match3MatchFinder.CollectMatchedCells(field, 3, matched), Is.EqualTo(3));
            int found = 0;
            bool any = false;

            long calls = GcAlloc.TryCount(() =>
            {
                for (int i = 0; i < 50; i++)
                {
                    found += Match3MatchFinder.CollectMatchedCells(field, 3, matched);
                    any |= Match3MatchFinder.HasMatch(field, 4);
                }
            }, out string notAdmitted);

            Assert.That(found, Is.EqualTo(150));
            Assert.That(any, Is.False, "A full HasMatch scan without a match is part of the measured work.");
            GcAlloc.IgnoreIfNotAdmitted(notAdmitted);
            Assert.That(calls, Is.Zero, "HasMatch and CollectMatchedCells with a reused set must not allocate.");
        }

        [Test]
        public void TrySwapAndResolve_WarmAcceptedCascade_DoesNotAllocate()
        {
            Match3BoardService service = CreateService(out FieldGenerator field);
            int next = 0;
            service.RandomIndexProvider = count => next++ % count;
            Vector3Int a = new Vector3Int(1, 1, 0);
            Vector3Int b = new Vector3Int(1, 0, 0);
            Seed(field);
            next = 0;
            Assert.That(service.TrySwapAndResolve(a, b), Is.True);
            string expected = Describe(field);
            Seed(field);
            next = 0;
            Assert.That(service.TrySwapAndResolve(a, b), Is.True);
            Assert.That(Describe(field), Is.EqualTo(expected), "The injected source makes the cascade reproducible.");
            bool accepted = true;

            long calls = GcAlloc.TryCount(() =>
            {
                for (int i = 0; i < 20; i++)
                {
                    Seed(field);
                    next = 0;
                    accepted &= service.TrySwapAndResolve(a, b);
                }
            }, out string notAdmitted);

            Assert.That(accepted, Is.True);
            Assert.That(Describe(field), Is.EqualTo(expected));
            GcAlloc.IgnoreIfNotAdmitted(notAdmitted);
            Assert.That(calls, Is.Zero,
                "Swap validation, clear, collapse and refill reuse the service's buffers once warm.");
        }

        [Test]
        public void ShuffleIfNoMoves_WarmShuffle_DoesNotAllocate()
        {
            Match3BoardService service = CreateService(out FieldGenerator field);
            int next = 0;
            service.RandomIndexProvider = count => (next++ * 7) % count;
            SeedUnique(field);
            next = 0;
            bool firstResult = service.ShuffleIfNoMoves();
            string expected = Describe(field);
            int results = 0;

            long calls = GcAlloc.TryCount(() =>
            {
                for (int i = 0; i < 5; i++)
                {
                    SeedUnique(field);
                    next = 0;
                    results += service.ShuffleIfNoMoves() == firstResult ? 1 : 0;
                }
            }, out string notAdmitted);

            Assert.That(results, Is.EqualTo(5), "The injected source makes the shuffle reproducible.");
            Assert.That(Describe(field), Is.EqualTo(expected));
            GcAlloc.IgnoreIfNotAdmitted(notAdmitted);
            Assert.That(calls, Is.Zero, "The shuffle attempts reuse the service's buffers and board copy.");
        }

        [Test]
        public void RandomIndexProvider_DrivesFillAndResolveAndLeavesUnityRandomUntouched()
        {
            UnityEngine.Random.State saved = UnityEngine.Random.state;
            try
            {
                UnityEngine.Random.InitState(77);
                string first = InitializeAndPlay(new System.Random(5));
                string second = InitializeAndPlay(new System.Random(5));
                string other = InitializeAndPlay(new System.Random(6));
                float observed = UnityEngine.Random.value;
                UnityEngine.Random.InitState(77);

                Assert.That(second, Is.EqualTo(first));
                Assert.That(other, Is.Not.EqualTo(first));
                Assert.That(observed, Is.EqualTo(UnityEngine.Random.value),
                    "With a provider the service must not consume UnityEngine.Random.");
            }
            finally
            {
                UnityEngine.Random.state = saved;
            }
        }

        [TestCase(999, Match3TileState.Purple)]
        [TestCase(-5, Match3TileState.Red)]
        public void RandomIndexProvider_OutOfRangeResults_AreClamped(int providerResult, Match3TileState expected)
        {
            Match3BoardService service = CreateService(out FieldGenerator field);
            JsonUtility.FromJsonOverwrite("{\"_avoidStartMatches\":false,\"_resolveSpawnMatchesWhenAllowed\":false}",
                service);
            field.GenerateField(new FieldGeneratorConfig(new Vector3Int(3, 3, 1), new MovementRule(FourDirections)));
            service.RandomIndexProvider = count => providerResult;

            service.InitializeBoard();

            foreach (FieldCell cell in field.GetAllCells())
            {
                Assert.That(cell.ContentId, Is.EqualTo((int)expected));
            }
        }

        [UnityTest]
        public IEnumerator DelayedResolve_ReportsBusyUntilCompletion_AndAZeroCascadeLimitNeverStaysBusy()
        {
            yield return new EnterPlayMode();
            GameObject root = new GameObject("Live Match3 Busy", typeof(Grid), typeof(FieldGenerator));
            UnityEngine.Random.State randomBefore = UnityEngine.Random.state;
            try
            {
                FieldGenerator generator = root.GetComponent<FieldGenerator>();
                generator.GenerateField(new FieldGeneratorConfig(new Vector3Int(3, 1, 1),
                    new MovementRule(FourDirections)));
                Match3BoardService service = root.AddComponent<Match3BoardService>();
                JsonUtility.FromJsonOverwrite(
                    "{\"_autoGenerateOnStart\":false,\"_useResolveDelay\":true,\"_resolveDelaySeconds\":0.01," +
                    "\"_maxCascadeIterations\":0}", service);
                int next = 0;
                service.RandomIndexProvider = count => next++ % count;
                int completed = 0;
                service.OnMatchesResolved.AddListener(count => completed++);
                SetRow(generator, Match3TileState.Red);

                service.ResolveCurrentMatchesButton();

                Assert.That(completed, Is.EqualTo(1), "A zero cascade limit completes without yielding.");
                Assert.That(service.IsResolving, Is.False, "A finished routine must not leave a stale busy handle.");

                JsonUtility.FromJsonOverwrite("{\"_maxCascadeIterations\":4}", service);
                service.ResolveCurrentMatchesButton();
                Assert.That(service.IsResolving, Is.True, "Positive control: a real cascade is busy until it completes.");
                Assert.That(service.TrySwapAndResolve(new Vector3Int(0, 0, 0), new Vector3Int(1, 0, 0)), Is.False);
                float deadline = Time.realtimeSinceStartup + 3f;
                while (service.IsResolving && Time.realtimeSinceStartup < deadline)
                {
                    yield return null;
                }

                Assert.That(service.IsResolving, Is.False);
                Assert.That(completed, Is.EqualTo(2));
            }
            finally
            {
                Object.DestroyImmediate(root);
                UnityEngine.Random.state = randomBefore;
            }

            yield return new ExitPlayMode();
        }

        [UnityTest]
        public IEnumerator DelayedResolve_IsBusyInsideItsFirstStep_AndIdleInsideItsCompletionEvents()
        {
            yield return new EnterPlayMode();
            try
            {
                Match3BoardService service = CreateDelayedService(
                    "{\"_resolveDelaySeconds\":0.01,\"_maxCascadeIterations\":4}", out FieldGenerator field);
                Vector3Int a = new Vector3Int(1, 1, 0);
                Vector3Int b = new Vector3Int(1, 0, 0);
                Vector3Int otherA = new Vector3Int(4, 4, 0);
                Vector3Int otherB = new Vector3Int(4, 3, 0);
                int swaps = 0;
                int busyInSwap = 0;
                int reentrantAttempts = 0;
                int reentrantAccepted = 0;
                int completed = 0;
                bool busyInCompletedPhase = true;
                bool busyInMatchesResolved = true;
                bool chainedAccepted = false;
                service.OnResolvePhase += (phase, count) =>
                {
                    if (phase == Match3BoardService.Match3ResolvePhase.Swap)
                    {
                        swaps++;
                        busyInSwap += service.IsResolving ? 1 : 0;
                    }
                    else if (phase == Match3BoardService.Match3ResolvePhase.Completed && completed == 0)
                    {
                        busyInCompletedPhase = service.IsResolving;
                    }
                };
                service.OnBoardChanged.AddListener(() =>
                {
                    if (reentrantAttempts > 0)
                    {
                        return;
                    }

                    reentrantAttempts++;
                    reentrantAccepted += service.TrySwapAndResolve(otherA, otherB) ? 1 : 0;
                });
                service.OnMatchesResolved.AddListener(count =>
                {
                    completed++;
                    if (completed != 1)
                    {
                        return;
                    }

                    busyInMatchesResolved = service.IsResolving;
                    SeedTwoMoves(field);
                    chainedAccepted = service.TrySwapAndResolve(a, b);
                });
                SeedTwoMoves(field);

                Assert.That(service.TrySwapAndResolve(a, b), Is.True);

                Assert.That(busyInSwap, Is.EqualTo(1), "The Swap phase of the first step already reports busy.");
                Assert.That(reentrantAttempts, Is.EqualTo(1));
                Assert.That(reentrantAccepted, Is.Zero,
                    "A valid swap from the first OnBoardChanged must not start a second routine on the same board.");
                Assert.That(service.IsResolving, Is.True);
                float deadline = Time.realtimeSinceStartup + 5f;
                while ((completed < 2 || service.IsResolving) && Time.realtimeSinceStartup < deadline)
                {
                    yield return null;
                }

                Assert.That(busyInCompletedPhase, Is.False, "The Completed phase must already see an idle service.");
                Assert.That(busyInMatchesResolved, Is.False, "OnMatchesResolved must already see an idle service.");
                Assert.That(chainedAccepted, Is.True, "A move chained from OnMatchesResolved must be accepted.");
                Assert.That(completed, Is.EqualTo(2));
                Assert.That(swaps, Is.EqualTo(2), "Only the first and the chained move swap.");
                Assert.That(busyInSwap, Is.EqualTo(2));
                Assert.That(service.IsResolving, Is.False);
                Assert.That(service.TrySwapAndResolve(otherA, otherB), Is.True,
                    "Positive control: the rejected re-entrant move is valid once the service is idle.");
            }
            finally
            {
                DestroyRoots();
            }

            yield return new ExitPlayMode();
        }

        [UnityTest]
        public IEnumerator DelayedResolve_RestartFromCompletion_KeepsTheNewResolveBusyUntilItCompletes()
        {
            yield return new EnterPlayMode();
            try
            {
                Match3BoardService service = CreateDelayedService(
                    "{\"_resolveDelaySeconds\":0.05,\"_maxCascadeIterations\":2,\"_avoidStartMatches\":false}",
                    out FieldGenerator field);
                service.RandomIndexProvider = count => 0;
                int completed = 0;
                bool busyAfterRestart = false;
                service.OnMatchesResolved.AddListener(count =>
                {
                    completed++;
                    if (completed == 1)
                    {
                        service.InitializeBoard();
                        busyAfterRestart = service.IsResolving;
                    }
                });
                Seed(field);

                Assert.That(service.TrySwapAndResolve(new Vector3Int(1, 1, 0), new Vector3Int(1, 0, 0)), Is.True);
                float deadline = Time.realtimeSinceStartup + 5f;
                while (completed < 1 && Time.realtimeSinceStartup < deadline)
                {
                    yield return null;
                }

                Assert.That(completed, Is.EqualTo(1));
                Assert.That(busyAfterRestart, Is.True,
                    "Positive control: an all-red restart resolves its start matches with a delay.");
                Assert.That(service.IsResolving, Is.True,
                    "The finished routine's tail must not drop the busy state of the resolve its listener started.");
                while (service.IsResolving && Time.realtimeSinceStartup < deadline)
                {
                    yield return null;
                }

                Assert.That(service.IsResolving, Is.False);
                Assert.That(completed, Is.EqualTo(2));
            }
            finally
            {
                DestroyRoots();
            }

            yield return new ExitPlayMode();
        }

        [UnityTest]
        public IEnumerator DelayedResolve_RestartInsideTheFirstStep_StopsTheOldRoutine()
        {
            yield return new EnterPlayMode();
            try
            {
                Match3BoardService service = CreateDelayedService(
                    "{\"_resolveDelaySeconds\":0.02,\"_maxCascadeIterations\":4}", out FieldGenerator field);
                bool restarted = false;
                string afterRestart = null;
                int clears = 0;
                int completed = 0;
                service.OnResolvePhase += (phase, count) =>
                {
                    if (phase == Match3BoardService.Match3ResolvePhase.Clear)
                    {
                        clears++;
                    }

                    if (phase != Match3BoardService.Match3ResolvePhase.Swap || restarted)
                    {
                        return;
                    }

                    restarted = true;
                    service.InitializeBoard();
                    afterRestart = Describe(field);
                };
                service.OnMatchesResolved.AddListener(count => completed++);
                Seed(field);

                Assert.That(service.TrySwapAndResolve(new Vector3Int(1, 1, 0), new Vector3Int(1, 0, 0)), Is.True);

                Assert.That(restarted, Is.True);
                Assert.That(service.IsResolving, Is.False,
                    "A restart from the first step cancels the routine before its handle exists.");
                yield return new WaitForSecondsRealtime(0.2f);
                Assert.That(clears, Is.Zero, "The cancelled routine must not resolve the new board.");
                Assert.That(completed, Is.Zero, "The cancelled routine must not publish a completion.");
                Assert.That(Describe(field), Is.EqualTo(afterRestart));
                Assert.That(service.TryFindValidSwap(out Vector3Int a, out Vector3Int b), Is.True,
                    "Positive control: the restarted board is playable.");
                Assert.That(service.TrySwapAndResolve(a, b), Is.True, "The service must not stay busy.");
                float deadline = Time.realtimeSinceStartup + 5f;
                while (service.IsResolving && Time.realtimeSinceStartup < deadline)
                {
                    yield return null;
                }

                Assert.That(completed, Is.EqualTo(1));
            }
            finally
            {
                DestroyRoots();
            }

            yield return new ExitPlayMode();
        }

        [UnityTest]
        public IEnumerator DelayedResolve_HoldsNoScratchAcrossItsDelays_SoAShuffleBetweenStepsDoesNotAllocate()
        {
            yield return new EnterPlayMode();
            try
            {
                Match3BoardService service = CreateDelayedService(
                    "{\"_resolveDelaySeconds\":0.01,\"_maxCascadeIterations\":4}", out FieldGenerator field);
                for (int i = 0; i < 2; i++)
                {
                    SeedUnique(field);
                    service.ShuffleIfNoMoves();
                }

                int[] saved = new int[BoardSize * BoardSize];
                int collapses = 0;
                int completed = 0;
                long calls = -1;
                string notAdmitted = null;
                Action shuffle = () => service.ShuffleIfNoMoves();
                service.OnResolvePhase += (phase, count) =>
                {
                    if (phase != Match3BoardService.Match3ResolvePhase.Collapse || collapses++ > 0)
                    {
                        return;
                    }

                    // WHY: the Collapse phase runs after a delay; a scratch held across it would make this shuffle
                    // rent a fresh one. The board is restored so the cascade continues on its own content.
                    for (int y = 0; y < BoardSize; y++)
                    for (int x = 0; x < BoardSize; x++)
                    {
                        saved[y * BoardSize + x] = field.Cells[x, y, 0].ContentId;
                    }

                    SeedUnique(field);
                    calls = GcAlloc.TryCount(shuffle, out notAdmitted);
                    for (int y = 0; y < BoardSize; y++)
                    for (int x = 0; x < BoardSize; x++)
                    {
                        field.Cells[x, y, 0].ContentId = saved[y * BoardSize + x];
                    }
                };
                service.OnMatchesResolved.AddListener(count => completed++);
                Seed(field);

                Assert.That(service.TrySwapAndResolve(new Vector3Int(1, 1, 0), new Vector3Int(1, 0, 0)), Is.True);
                float deadline = Time.realtimeSinceStartup + 5f;
                while (service.IsResolving && Time.realtimeSinceStartup < deadline)
                {
                    yield return null;
                }

                Assert.That(service.IsResolving, Is.False);
                Assert.That(collapses, Is.GreaterThanOrEqualTo(1), "The measured shuffle runs between two delayed steps.");
                Assert.That(completed, Is.EqualTo(1));
                GcAlloc.IgnoreIfNotAdmitted(notAdmitted);
                Assert.That(calls, Is.Zero,
                    "The delayed routine rents its match buffer per step, so the warm shuffle buffers stay available.");
            }
            finally
            {
                DestroyRoots();
            }

            yield return new ExitPlayMode();
        }

        [UnityTearDown]
        public IEnumerator LeavePlayModeAfterFailure()
        {
            if (Application.isPlaying)
            {
                yield return new ExitPlayMode();
            }
        }

        private string InitializeAndPlay(System.Random random)
        {
            Match3BoardService service = CreateService(out FieldGenerator field);
            field.GenerateField(new FieldGeneratorConfig(new Vector3Int(BoardSize, BoardSize, 1),
                new MovementRule(FourDirections)));
            service.RandomIndexProvider = count => random.Next(count);
            service.InitializeBoard();
            for (int move = 0; move < 3 && service.TryFindValidSwap(out Vector3Int a, out Vector3Int b); move++)
            {
                Assert.That(service.TrySwapAndResolve(a, b), Is.True);
            }

            return Describe(field);
        }

        private Match3BoardService CreateService(out FieldGenerator field)
        {
            GameObject root = new GameObject("Match3ResolveHotPath");
            _roots.Add(root);
            root.SetActive(false);
            field = root.AddComponent<FieldGenerator>();
            field.DebugEnabled = false;
            Match3BoardService service = root.AddComponent<Match3BoardService>();
            JsonUtility.FromJsonOverwrite("{\"_autoGenerateOnStart\":false,\"_minMatchLength\":3}", service);
            root.SetActive(true);
            field.GenerateField(new FieldGeneratorConfig(new Vector3Int(BoardSize, BoardSize, 1),
                new MovementRule(FourDirections)));
            return service;
        }

        /// <summary>
        ///     <see cref="CreateService" /> with Use Resolve Delay on, the given serialized overrides and a sequential
        ///     random source.
        /// </summary>
        private Match3BoardService CreateDelayedService(string overridesJson, out FieldGenerator field)
        {
            Match3BoardService service = CreateService(out field);
            JsonUtility.FromJsonOverwrite("{\"_useResolveDelay\":true}", service);
            JsonUtility.FromJsonOverwrite(overridesJson, service);
            int next = 0;
            service.RandomIndexProvider = count => next++ % count;
            return service;
        }

        private void DestroyRoots()
        {
            foreach (GameObject root in _roots)
            {
                if (root != null)
                {
                    Object.DestroyImmediate(root);
                }
            }

            _roots.Clear();
        }

        /// <summary>
        ///     <see cref="Seed" /> plus a second copy of the canonical patch at (3,3), where swapping (4,4) with (4,3)
        ///     completes another row of three. The first move's cascade stays in columns 0-2.
        /// </summary>
        private static void SeedTwoMoves(FieldGenerator field)
        {
            Seed(field);
            for (int y = 0; y < 2; y++)
            for (int x = 0; x < 3; x++)
            {
                field.Cells[x + 3, y + 3, 0].ContentId = CanonicalPatch[y, x];
            }
        }

        /// <summary>
        ///     Unique tiles everywhere except the canonical 3x2 patch at the origin, where swapping (1,1) with (1,0)
        ///     completes a row of three.
        /// </summary>
        private static void Seed(FieldGenerator field)
        {
            SeedUnique(field);
            for (int y = 0; y < 2; y++)
            for (int x = 0; x < 3; x++)
            {
                field.Cells[x, y, 0].ContentId = CanonicalPatch[y, x];
            }
        }

        private static void SeedUnique(FieldGenerator field)
        {
            for (int y = 0; y < BoardSize; y++)
            for (int x = 0; x < BoardSize; x++)
            {
                field.Cells[x, y, 0].ContentId = 100 + y * BoardSize + x;
            }
        }

        private static void SetRow(FieldGenerator field, Match3TileState state)
        {
            for (int x = 0; x < field.Cells.GetLength(0); x++)
            {
                field.Cells[x, 0, 0].ContentId = (int)state;
            }
        }

        private static string Describe(FieldGenerator field)
        {
            StringBuilder builder = new StringBuilder();
            foreach (FieldCell cell in field.GetAllCells())
            {
                builder.Append(cell.Position).Append(':').Append(cell.ContentId).Append(';');
            }

            return builder.ToString();
        }
    }
}
