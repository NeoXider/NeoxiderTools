using System;
using System.Collections.Generic;
using Neo.GridSystem;
using Neo.Tests.Performance;
using NUnit.Framework;
using UnityEngine;
using Object = UnityEngine.Object;
using Random = System.Random;

namespace Neo.Editor.Tests.GridSystem
{
    /// <summary>
    ///     Allocation and complexity guards for the GridSystem hot paths (pathfinding, neighbor queries, placement
    ///     validation and world-to-cell picking), plus parity checks against brute-force references.
    /// </summary>
    [TestFixture]
    public sealed class GridSystemPerformanceTests
    {
        private readonly List<Object> _created = new();

        [TearDown]
        public void TearDown()
        {
            foreach (Object created in _created)
            {
                if (created != null)
                {
                    Object.DestroyImmediate(created);
                }
            }

            _created.Clear();
        }

        [Test]
        public void TryFindPath_WarmCall_DoesNotAllocate()
        {
            FieldGenerator field = CreateField(40, 40);
            for (int y = 0; y < 39; y++)
            {
                field.SetWalkable(new Vector3Int(20, y, 0), false);
            }

            GridPathRequest request = new GridPathRequest
            {
                Start = new Vector3Int(0, 0, 0),
                End = new Vector3Int(39, 0, 0)
            };
            List<FieldCell> path = new List<FieldCell>(256);
            Assert.That(GridPathfinder.TryFindPath(field, request, path, out NoPathReason warmReason), Is.True);
            Assert.That(warmReason, Is.EqualTo(NoPathReason.None));

            using GcAllocCounter allocationCounter = GcAllocCounter.CreateOrIgnore();
            allocationCounter.Begin();
            for (int i = 0; i < 20; i++)
            {
                GridPathfinder.TryFindPath(field, request, path, out _);
            }

            long allocationCalls = allocationCounter.End();

            Assert.That(allocationCalls, Is.EqualTo(0L));
            Assert.That(path[0].Position, Is.EqualTo(request.Start));
            Assert.That(path[path.Count - 1].Position, Is.EqualTo(request.End));
        }

        [Test]
        public void FindPath_ReturnsAShortestValidPathOnRandomMazes()
        {
            Random random = new Random(1234);
            for (int round = 0; round < 25; round++)
            {
                FieldGenerator field = CreateField(12, 9);
                foreach (FieldCell cell in field.GetAllCells())
                {
                    if (random.NextDouble() < 0.28)
                    {
                        cell.IsWalkable = false;
                    }
                }

                Vector3Int start = new Vector3Int(random.Next(12), random.Next(9), 0);
                Vector3Int end = new Vector3Int(random.Next(12), random.Next(9), 0);
                field.GetCell(start).IsWalkable = true;
                field.GetCell(end).IsWalkable = true;

                GridPathResult result = field.FindPathDetailed(start, end);
                int expectedDistance = ReferenceDistance(field, start, end);
                if (expectedDistance < 0)
                {
                    Assert.That(result.HasPath, Is.False, $"round {round}");
                    Assert.That(result.Reason, Is.EqualTo(NoPathReason.NoPathFound), $"round {round}");
                    continue;
                }

                Assert.That(result.Reason, Is.EqualTo(NoPathReason.None), $"round {round}");
                Assert.That(result.Path.Count - 1, Is.EqualTo(expectedDistance), $"round {round}");
                Assert.That(result.Path[0].Position, Is.EqualTo(start));
                Assert.That(result.Path[result.Path.Count - 1].Position, Is.EqualTo(end));
                for (int i = 1; i < result.Path.Count; i++)
                {
                    Vector3Int delta = result.Path[i].Position - result.Path[i - 1].Position;
                    Assert.That(Mathf.Abs(delta.x) + Mathf.Abs(delta.y), Is.EqualTo(1), $"round {round} step {i}");
                    Assert.That(result.Path[i].IsWalkable, Is.True);
                }

                Object.DestroyImmediate(field.gameObject);
            }
        }

        [Test]
        public void PassabilityPredicate_CanRunANestedPathQueryWithoutCorruptingTheOuterSearch()
        {
            FieldGenerator field = CreateField(6, 6);
            int nestedQueries = 0;
            GridPathRequest outer = new GridPathRequest
            {
                Start = Vector3Int.zero,
                End = new Vector3Int(5, 5, 0),
                CustomPassabilityPredicate = cell =>
                {
                    if (cell.Position.x == 2)
                    {
                        nestedQueries++;
                        GridPathResult nested = GridPathfinder.FindPath(field, new GridPathRequest
                        {
                            Start = new Vector3Int(5, 0, 0),
                            End = new Vector3Int(0, 5, 0)
                        });
                        Assert.That(nested.Path.Count, Is.EqualTo(11));
                    }

                    return true;
                }
            };

            GridPathResult result = GridPathfinder.FindPath(field, outer);

            Assert.That(nestedQueries, Is.GreaterThan(0));
            Assert.That(result.Path.Count, Is.EqualTo(11));
            Assert.That(result.Path[10].Position, Is.EqualTo(new Vector3Int(5, 5, 0)));
        }

        [Test]
        public void PassabilityPredicate_IsEvaluatedAtMostOncePerCell()
        {
            FieldGenerator field = CreateField(8, 8);
            Dictionary<Vector3Int, int> calls = new Dictionary<Vector3Int, int>();
            GridPathRequest request = new GridPathRequest
            {
                Start = Vector3Int.zero,
                End = new Vector3Int(7, 7, 0),
                CustomPassabilityPredicate = cell =>
                {
                    calls.TryGetValue(cell.Position, out int count);
                    calls[cell.Position] = count + 1;
                    return cell.Position.x != 3 || cell.Position.y == 7;
                }
            };

            Assert.That(field.FindPathDetailed(request).HasPath, Is.True);
            foreach (KeyValuePair<Vector3Int, int> pair in calls)
            {
                bool endpoint = pair.Key == request.Start || pair.Key == request.End;
                Assert.That(pair.Value, Is.LessThanOrEqualTo(endpoint ? 2 : 1), pair.Key.ToString());
            }
        }

        [Test]
        public void CollectReachableCells_ReturnsTheMovementDiamondInStepOrder()
        {
            FieldGenerator field = CreateField(7, 7);
            Vector3Int origin = new Vector3Int(3, 3, 0);
            field.SetOccupied(origin, true);
            field.SetWalkable(new Vector3Int(3, 4, 0), false);
            List<FieldCell> reachable = new List<FieldCell>();
            List<int> steps = new List<int>();

            int count = GridPathfinder.CollectReachableCells(field, new GridPathRequest { Start = origin }, 2,
                reachable, steps);

            Assert.That(count, Is.EqualTo(reachable.Count));
            Assert.That(reachable[0].Position, Is.EqualTo(origin));
            Assert.That(steps[0], Is.EqualTo(0));
            for (int i = 1; i < steps.Count; i++)
            {
                Assert.That(steps[i], Is.GreaterThanOrEqualTo(steps[i - 1]));
            }

            HashSet<Vector3Int> positions = new HashSet<Vector3Int>();
            foreach (FieldCell cell in reachable)
            {
                positions.Add(cell.Position);
            }

            Assert.That(positions.Contains(new Vector3Int(3, 4, 0)), Is.False, "blocked cell");
            Assert.That(positions.Contains(new Vector3Int(3, 5, 0)), Is.False, "behind the blocker needs 4 steps");
            Assert.That(positions.Contains(new Vector3Int(4, 4, 0)), Is.True);
            Assert.That(positions.Contains(new Vector3Int(5, 3, 0)), Is.True);
            Assert.That(positions.Contains(new Vector3Int(3, 1, 0)), Is.True);
            Assert.That(positions.Contains(new Vector3Int(5, 4, 0)), Is.False, "three steps away");
            Assert.That(count, Is.EqualTo(11));

            Assert.That(GridPathfinder.CollectReachableCells(field, new GridPathRequest { Start = origin }, 0,
                reachable), Is.EqualTo(1));
            Assert.That(GridPathfinder.CollectReachableCells(field, new GridPathRequest { Start = origin }, -1,
                reachable), Is.EqualTo(48));
        }

        [Test]
        public void CollectReachableCells_WarmCall_DoesNotAllocate()
        {
            FieldGenerator field = CreateField(40, 40);
            for (int y = 0; y < 39; y++)
            {
                field.SetWalkable(new Vector3Int(20, y, 0), false);
            }

            GridPathRequest request = new GridPathRequest { Start = new Vector3Int(5, 5, 0) };
            List<FieldCell> reachable = new List<FieldCell>(2048);
            List<int> steps = new List<int>(2048);
            int expected = GridPathfinder.CollectReachableCells(field, request, 12, reachable, steps);
            int checksum = 0;

            using GcAllocCounter allocationCounter = GcAllocCounter.CreateOrIgnore();
            allocationCounter.Begin();
            for (int i = 0; i < 20; i++)
            {
                checksum += GridPathfinder.CollectReachableCells(field, request, 12, reachable, steps);
            }

            long allocationCalls = allocationCounter.End();

            Assert.That(allocationCalls, Is.EqualTo(0L), "A warm movement-range query reuses the search buffers.");
            Assert.That(checksum, Is.EqualTo(expected * 20));
            Assert.That(expected, Is.GreaterThan(100));
            Assert.That(reachable.Count, Is.EqualTo(steps.Count));
        }

        [Test]
        public void CollectNeighbors_WarmCall_DoesNotAllocateAndMatchesGetNeighbors()
        {
            FieldGenerator field = CreateField(5, 5);
            FieldCell center = field.GetCell(2, 2);
            List<FieldCell> buffer = new List<FieldCell>(8);
            field.CollectNeighbors(center, buffer);

            using GcAllocCounter allocationCounter = GcAllocCounter.CreateOrIgnore();
            allocationCounter.Begin();
            for (int i = 0; i < 100; i++)
            {
                field.CollectNeighbors(center, buffer);
            }

            long allocationCalls = allocationCounter.End();

            Assert.That(allocationCalls, Is.EqualTo(0L));
            Assert.That(buffer, Is.EqualTo(field.GetNeighbors(center)));
            Assert.That(field.CollectNeighbors(field.GetCell(0, 0), buffer), Is.EqualTo(2));
            Assert.Throws<ArgumentNullException>(() => field.CollectNeighbors(center, null));
        }

        [Test]
        public void CanPlaceContentFootprint_WarmCall_DoesNotAllocate()
        {
            FieldGenerator field = CreateField(6, 6);
            GridPlacementEntry[] entries =
            {
                new GridPlacementEntry(Vector3Int.zero, 1),
                new GridPlacementEntry(Vector3Int.right, 2),
                new GridPlacementEntry(Vector3Int.up, 3)
            };
            field.CanPlaceContentFootprint(Vector3Int.zero, entries);

            using GcAllocCounter allocationCounter = GcAllocCounter.CreateOrIgnore();
            allocationCounter.Begin();
            int fits = 0;
            for (int x = 0; x < 6; x++)
            for (int y = 0; y < 6; y++)
            {
                if (field.CanPlaceContentFootprint(new Vector3Int(x, y, 0), entries))
                {
                    fits++;
                }
            }

            long allocationCalls = allocationCounter.End();

            Assert.That(allocationCalls, Is.EqualTo(0L));
            Assert.That(fits, Is.EqualTo(25));
        }

        [Test]
        public void PlacementServiceCanPlace_WarmRejectedAndAcceptedChecks_DoNotAllocate()
        {
            FieldGenerator field = CreateField(4, 4);
            field.SetOccupied(new Vector3Int(1, 1, 0), true);
            GridPlacementService service = new GridPlacementService(field);
            GridPlacementRequest request = new GridPlacementRequest
            {
                Entries = new[]
                {
                    new GridPlacementEntry(Vector3Int.zero, 1),
                    new GridPlacementEntry(Vector3Int.right, 1)
                }
            };
            service.CanPlace(request);

            using GcAllocCounter allocationCounter = GcAllocCounter.CreateOrIgnore();
            allocationCounter.Begin();
            int fits = 0;
            for (int x = -1; x < 5; x++)
            for (int y = -1; y < 5; y++)
            {
                request.Anchor = new Vector3Int(x, y, 0);
                if (service.CanPlace(request))
                {
                    fits++;
                }
            }

            long allocationCalls = allocationCounter.End();

            Assert.That(allocationCalls, Is.EqualTo(0L));
            Assert.That(fits, Is.EqualTo(10));
            request.Anchor = new Vector3Int(1, 1, 0);
            Assert.That(service.CanPlace(request, out string reason), Is.False);
            Assert.That(reason, Does.Contain("occupied"));
        }

        [Test]
        public void PlacementServicePredicate_CanReenterTheSameService()
        {
            FieldGenerator field = CreateField(3, 1);
            GridPlacementService service = new GridPlacementService(field);
            GridPlacementRequest inner = GridPlacementRequest.Single(new Vector3Int(2, 0, 0), 4);
            GridPlacementRequest outer = new GridPlacementRequest
            {
                Entries = new[]
                {
                    new GridPlacementEntry(Vector3Int.zero, 1),
                    new GridPlacementEntry(Vector3Int.right, 2)
                },
                CellPredicate = cell => service.CanPlace(inner)
            };

            Assert.That(service.CanPlace(outer), Is.True);
            Assert.That(service.Place(outer).Placed, Is.True);
            Assert.That(field.GetCell(1, 0).ContentId, Is.EqualTo(2));
            Assert.That(service.CanPlace(outer), Is.False, "the footprint cells are now occupied");
        }

        [Test]
        public void PlaceContentFootprint_ListenerCanValidateAndPlaceAgainDuringNotification()
        {
            FieldGenerator field = CreateField(4, 1);
            GridPlacementEntry[] pair =
            {
                new GridPlacementEntry(Vector3Int.zero, 5),
                new GridPlacementEntry(Vector3Int.right, 6)
            };
            bool nestedPlaced = false;
            field.OnCellStateChanged.AddListener(cell =>
            {
                if (nestedPlaced)
                {
                    return;
                }

                nestedPlaced = true;
                Assert.That(field.CanPlaceContentFootprint(Vector3Int.zero, pair), Is.False);
                Assert.That(field.PlaceContentFootprint(new Vector3Int(2, 0, 0), pair).Placed, Is.True);
            });

            GridPlacementResult outer = field.PlaceContentFootprint(Vector3Int.zero, pair);

            Assert.That(outer.Placed, Is.True);
            Assert.That(outer.Positions, Is.EqualTo(new[] { Vector3Int.zero, Vector3Int.right }));
            Assert.That(field.GetCell(0, 0).ContentId, Is.EqualTo(5));
            Assert.That(field.GetCell(1, 0).ContentId, Is.EqualTo(6));
            Assert.That(field.GetCell(2, 0).ContentId, Is.EqualTo(5));
            Assert.That(field.GetCell(3, 0).ContentId, Is.EqualTo(6));
        }

        [TestCase(false)]
        [TestCase(true)]
        public void GetNearestCellFromWorld_MatchesBruteForceOnTransformedBoards(bool includeDisabled)
        {
            FieldGenerator field = CreateField(7, 5);
            field.transform.SetPositionAndRotation(new Vector3(3f, -2f, 1f), Quaternion.Euler(0f, 0f, 30f));
            field.transform.localScale = new Vector3(1.5f, 1.5f, 1.5f);
            field.SetEnabled(new Vector3Int(3, 2, 0), false);
            field.SetEnabled(new Vector3Int(0, 0, 0), false);
            Random random = new Random(42);

            for (int i = 0; i < 300; i++)
            {
                Vector3 local = new Vector3((float)(random.NextDouble() * 14 - 7), (float)(random.NextDouble() * 10 - 5),
                    0f);
                Vector3 world = field.transform.TransformPoint(local);

                FieldCell nearest = field.GetNearestCellFromWorld(world, includeDisabled);

                float expected = BruteForceNearestDistance(field, world, includeDisabled);
                Assert.That(nearest, Is.Not.Null);
                Assert.That(includeDisabled || nearest.IsEnabled, Is.True);
                float actual = (field.GetCellWorldCenter(nearest.Position) - world).sqrMagnitude;
                Assert.That(actual, Is.EqualTo(expected).Within(1e-3f), $"point {i}: {local}");
            }
        }

        [Test]
        public void GetNearestCellFromWorld_InsideAnEnabledCell_DoesNotAllocate()
        {
            FieldGenerator field = CreateField(50, 50);
            Vector3 world = field.GetCellWorldCenter(new Vector3Int(10, 20, 0)) + new Vector3(0.2f, -0.1f, 0f);
            field.GetNearestCellFromWorld(world);

            using GcAllocCounter allocationCounter = GcAllocCounter.CreateOrIgnore();
            allocationCounter.Begin();
            FieldCell cell = null;
            for (int i = 0; i < 100; i++)
            {
                cell = field.GetNearestCellFromWorld(world);
            }

            long allocationCalls = allocationCounter.End();

            Assert.That(allocationCalls, Is.EqualTo(0L));
            Assert.That(cell.Position, Is.EqualTo(new Vector3Int(10, 20, 0)));
        }

        [Test]
        public void TryGetContainingCell_ResolvesEligibleCellsDirectlyOnGaplessRectangularBoards()
        {
            FieldGenerator field = CreateField(50, 50);
            Vector3Int disabledPosition = new Vector3Int(30, 30, 0);
            field.SetEnabled(disabledPosition, false);
            Vector3Int[] probes = { new(0, 0, 0), new(10, 20, 0), new(49, 49, 0) };

            foreach (Vector3Int probe in probes)
            {
                Vector3 world = field.GetCellWorldCenter(probe) + new Vector3(0.3f, -0.2f, 0f);
                Assert.That(field.TryGetContainingCell(world, false, out FieldCell cell), Is.True, probe.ToString());
                Assert.That(cell.Position, Is.EqualTo(probe));
                Assert.That(field.GetNearestCellFromWorld(world), Is.SameAs(cell));
            }

            Vector3 overDisabled = field.GetCellWorldCenter(disabledPosition);
            Assert.That(field.TryGetContainingCell(overDisabled, false, out FieldCell excluded), Is.False);
            Assert.That(excluded, Is.Null);
            Assert.That(field.TryGetContainingCell(overDisabled, true, out FieldCell included), Is.True);
            Assert.That(included.Position, Is.EqualTo(disabledPosition));

            Vector3 outside = field.GetCellWorldCenter(new Vector3Int(49, 0, 0)) + new Vector3(5f, 0f, 0f);
            Assert.That(field.TryGetContainingCell(outside, false, out _), Is.False);
            Assert.That(field.GetNearestCellFromWorld(outside).Position, Is.EqualTo(new Vector3Int(49, 0, 0)));
        }

        [TestCase(0)]
        [TestCase(1)]
        [TestCase(2)]
        public void TryGetContainingCell_DeclinesLayoutsThatNeedTheScan(int layout)
        {
            GameObject owner = new GameObject("ScanOnlyLayout");
            _created.Add(owner);
            Grid grid = owner.AddComponent<Grid>();
            Vector3Int size = new Vector3Int(5, 5, 1);
            switch (layout)
            {
                case 0:
                    grid.cellLayout = GridLayout.CellLayout.Hexagon;
                    break;
                case 1:
                    grid.cellGap = new Vector3(0.25f, 0.25f, 0f);
                    break;
                default:
                    // WHY: several layers with zero cell depth collapse onto one plane.
                    grid.cellSize = new Vector3(1f, 1f, 0f);
                    size = new Vector3Int(5, 5, 3);
                    break;
            }

            FieldGenerator field = owner.AddComponent<FieldGenerator>();
            field.GenerateField(new FieldGeneratorConfig(size));
            Vector3 world = field.GetCellWorldCenter(new Vector3Int(2, 2, 0));

            Assert.That(field.TryGetContainingCell(world, true, out FieldCell cell), Is.False);
            Assert.That(cell, Is.Null);
            Assert.That(field.GetNearestCellFromWorld(world).Position, Is.EqualTo(new Vector3Int(2, 2, 0)));
        }

        [Test]
        public void GetNearestCellFromWorld_PointOnASharedEdge_ResolvesLikeTheUnityGridContainingCell()
        {
            FieldGenerator field = CreateField(50, 50);
            Vector3 edge = field.GetCellCornerWorld(new Vector3Int(11, 20, 0)) + new Vector3(0f, 0.5f, 0f);

            FieldCell nearest = field.GetNearestCellFromWorld(edge);

            Assert.That(nearest, Is.Not.Null);
            Assert.That(nearest, Is.SameAs(field.GetCellFromWorld(edge)));
            Assert.That(nearest.Position.y, Is.EqualTo(20));
        }

        [Test]
        public void GetNearestCellFromWorld_LayersCollapsedByZeroCellDepth_KeepTheFirstLayerTieBreak()
        {
            GameObject owner = new GameObject("CollapsedLayers");
            _created.Add(owner);
            Grid grid = owner.AddComponent<Grid>();
            grid.cellSize = new Vector3(1f, 1f, 0f);
            FieldGenerator field = owner.AddComponent<FieldGenerator>();
            field.GenerateField(new FieldGeneratorConfig(new Vector3Int(3, 3, 3)));
            Vector3 world = field.GetCellWorldCenter(new Vector3Int(1, 2, 1)) + new Vector3(0.1f, 0.1f, 0f);

            FieldCell nearest = field.GetNearestCellFromWorld(world);

            Assert.That(nearest.Position, Is.EqualTo(new Vector3Int(1, 2, 0)));
        }

        [Test]
        public void GeneratedSize_FollowsRegenerationWithoutReadingTheArray()
        {
            FieldGenerator field = CreateField(3, 4);
            Assert.That(field.GeneratedSize, Is.EqualTo(new Vector3Int(3, 4, 1)));

            field.Config.Size = new Vector3Int(6, 2, 2);
            Assert.That(field.GeneratedSize, Is.EqualTo(new Vector3Int(3, 4, 1)), "pending resize");
            Assert.That(field.InBounds(new Vector3Int(2, 3, 0)), Is.True);
            Assert.That(field.InBounds(new Vector3Int(5, 1, 1)), Is.False);

            field.GenerateField();
            Assert.That(field.GeneratedSize, Is.EqualTo(new Vector3Int(6, 2, 2)));
            Assert.That(field.InBounds(new Vector3Int(5, 1, 1)), Is.True);
            Assert.That(field.GetCell(new Vector3Int(5, 1, 1)), Is.Not.Null);
        }

        private FieldGenerator CreateField(int width, int height)
        {
            GameObject owner = new GameObject("GridSystemPerformanceTests");
            _created.Add(owner);
            owner.AddComponent<Grid>();
            FieldGenerator field = owner.AddComponent<FieldGenerator>();
            field.GenerateField(new FieldGeneratorConfig(new Vector3Int(width, height, 1)));
            return field;
        }

        private static int ReferenceDistance(FieldGenerator field, Vector3Int start, Vector3Int end)
        {
            Dictionary<Vector3Int, int> distance = new Dictionary<Vector3Int, int> { [start] = 0 };
            Queue<Vector3Int> queue = new Queue<Vector3Int>();
            queue.Enqueue(start);
            Vector3Int[] directions = { Vector3Int.right, Vector3Int.left, Vector3Int.up, Vector3Int.down };
            while (queue.Count > 0)
            {
                Vector3Int current = queue.Dequeue();
                if (current == end)
                {
                    return distance[current];
                }

                foreach (Vector3Int direction in directions)
                {
                    Vector3Int next = current + direction;
                    FieldCell cell = field.GetCell(next);
                    if (cell == null || !cell.IsWalkable || distance.ContainsKey(next))
                    {
                        continue;
                    }

                    distance[next] = distance[current] + 1;
                    queue.Enqueue(next);
                }
            }

            return -1;
        }

        private static float BruteForceNearestDistance(FieldGenerator field, Vector3 world, bool includeDisabled)
        {
            float best = float.PositiveInfinity;
            foreach (FieldCell cell in field.GetAllCells(includeDisabled))
            {
                float distance = (field.GetCellWorldCenter(cell.Position) - world).sqrMagnitude;
                if (distance < best)
                {
                    best = distance;
                }
            }

            return best;
        }
    }
}
