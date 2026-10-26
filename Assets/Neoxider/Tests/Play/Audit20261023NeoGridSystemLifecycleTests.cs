using System.Collections;
using System.Collections.Generic;
using Neo.GridSystem;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using Object = UnityEngine.Object;

namespace Neo.Tests.Play
{
    public sealed class Audit20261023NeoGridSystemLifecycleTests
    {
        private readonly List<GameObject> _objects = new List<GameObject>();

        [UnityTearDown]
        public IEnumerator TearDown()
        {
            for (int i = _objects.Count - 1; i >= 0; i--)
            {
                if (_objects[i] != null)
                {
                    Object.Destroy(_objects[i]);
                }
            }

            _objects.Clear();
            yield return null;
        }

        [UnityTest]
        public IEnumerator RemoveDetachedObject_PreservesReplacementOccupancyTrackingAndEvents()
        {
            FieldObjectSpawner spawner = CreateSpawner(out FieldGenerator field);
            SpawnedObjectInfo previous = spawner.SpawnAt(Vector3Int.zero);
            field.GenerateField();
            SpawnedObjectInfo replacement = spawner.SpawnAt(Vector3Int.zero);
            List<FieldCell> changes = new List<FieldCell>();
            List<FieldCell> freed = new List<FieldCell>();
            List<SpawnedObjectInfo> removed = new List<SpawnedObjectInfo>();
            field.OnCellStateChanged.AddListener(cell => changes.Add(cell));
            spawner.OnCellFreed.AddListener(cell => freed.Add(cell));
            spawner.OnObjectRemoved.AddListener(info => removed.Add(info));

            spawner.RemoveObject(previous.GameObject);

            Assert.That(replacement.Cell, Is.Not.SameAs(previous.Cell));
            Assert.That(field.GetCell(Vector3Int.zero), Is.SameAs(replacement.Cell));
            Assert.That(replacement.Cell.IsOccupied, Is.True);
            Assert.That(spawner.GetObjectsInCell(Vector3Int.zero), Is.EqualTo(new[] { replacement }));
            Assert.That(spawner.GetAllObjects(), Is.EqualTo(new[] { replacement }));
            Assert.That(removed, Is.EqualTo(new[] { previous }));
            Assert.That(changes, Is.Empty);
            Assert.That(freed, Is.Empty);
            yield return null;
            Assert.That(previous.GameObject == null, Is.True);
            Assert.That(replacement.GameObject != null, Is.True);
        }

        [UnityTest]
        public IEnumerator SpawnCallbackRegeneration_KeepsTheObjectDetachedWithoutOccupyingTheNewBoard()
        {
            FieldObjectSpawner spawner = CreateSpawner(out FieldGenerator field);
            FieldCell originalCell = field.GetCell(Vector3Int.zero);
            List<FieldCell> changes = new List<FieldCell>();
            List<FieldCell> occupied = new List<FieldCell>();
            field.OnCellStateChanged.AddListener(cell => changes.Add(cell));
            spawner.OnCellOccupied.AddListener(cell => occupied.Add(cell));
            spawner.OnObjectSpawned.AddListener(info => field.GenerateField());

            SpawnedObjectInfo detached = spawner.SpawnAt(Vector3Int.zero);

            Assert.That(detached.Cell, Is.SameAs(originalCell));
            Assert.That(field.GetCell(Vector3Int.zero), Is.Not.SameAs(originalCell));
            Assert.That(field.GetCell(Vector3Int.zero).IsOccupied, Is.False);
            Assert.That(spawner.GetObjectsInCell(Vector3Int.zero), Is.Empty);
            Assert.That(spawner.GetAllObjects(), Is.EqualTo(new[] { detached }));
            Assert.That(changes, Is.Empty);
            Assert.That(occupied, Is.Empty);
            yield return null;
            Assert.That(detached.GameObject != null, Is.True);
        }

        [UnityTest]
        public IEnumerator RemoveCallbackRegeneration_DoesNotFreeOrNotifyTheNewOccupant()
        {
            FieldObjectSpawner spawner = CreateSpawner(out FieldGenerator field);
            SpawnedObjectInfo previous = spawner.SpawnAt(Vector3Int.zero);
            SpawnedObjectInfo replacement = null;
            List<FieldCell> changes = new List<FieldCell>();
            int freed = 0;
            int removed = 0;
            field.OnCellStateChanged.AddListener(cell => changes.Add(cell));
            spawner.OnCellFreed.AddListener(cell => freed++);
            spawner.OnObjectRemoved.AddListener(info =>
            {
                removed++;
                field.GenerateField();
                replacement = spawner.SpawnAt(Vector3Int.zero);
            });

            spawner.RemoveObject(previous.GameObject);

            Assert.That(replacement, Is.Not.Null);
            Assert.That(field.GetCell(Vector3Int.zero), Is.SameAs(replacement.Cell));
            Assert.That(replacement.Cell.IsOccupied, Is.True);
            Assert.That(spawner.GetObjectsInCell(Vector3Int.zero), Is.EqualTo(new[] { replacement }));
            Assert.That(spawner.GetAllObjects(), Is.EqualTo(new[] { replacement }));
            Assert.That(changes, Is.EqualTo(new[] { replacement.Cell }));
            Assert.That(freed, Is.Zero);
            Assert.That(removed, Is.EqualTo(1));
            yield return null;
            Assert.That(previous.GameObject == null, Is.True);
            Assert.That(replacement.GameObject != null, Is.True);
        }

        [UnityTest]
        public IEnumerator SpawnCallbackRemoval_DoesNotReoccupyAnUnregisteredObject()
        {
            FieldObjectSpawner spawner = CreateSpawner(out FieldGenerator field);
            int occupied = 0;
            int removed = 0;
            spawner.OnCellOccupied.AddListener(cell => occupied++);
            spawner.OnObjectRemoved.AddListener(info => removed++);
            spawner.OnObjectSpawned.AddListener(info => spawner.RemoveObject(info.GameObject));

            SpawnedObjectInfo detached = spawner.SpawnAt(Vector3Int.zero);

            Assert.That(spawner.GetAllObjects(), Is.Empty);
            Assert.That(spawner.GetObjectsInCell(Vector3Int.zero), Is.Empty);
            Assert.That(field.GetCell(Vector3Int.zero).IsOccupied, Is.False);
            Assert.That(occupied, Is.Zero);
            Assert.That(removed, Is.EqualTo(1));
            yield return null;
            Assert.That(detached.GameObject == null, Is.True);
        }

        [UnityTest]
        public IEnumerator OccupiedStateCallbackRegeneration_SuppressesTheOldCellOccupiedEvent()
        {
            FieldObjectSpawner spawner = CreateSpawner(out FieldGenerator field);
            int occupied = 0;
            int changes = 0;
            spawner.OnCellOccupied.AddListener(cell => occupied++);
            field.OnCellStateChanged.AddListener(cell =>
            {
                changes++;
                field.GenerateField();
            });

            SpawnedObjectInfo detached = spawner.SpawnAt(Vector3Int.zero);

            Assert.That(changes, Is.EqualTo(1));
            Assert.That(occupied, Is.Zero);
            Assert.That(detached.Cell.IsOccupied, Is.True);
            Assert.That(field.GetCell(Vector3Int.zero), Is.Not.SameAs(detached.Cell));
            Assert.That(field.GetCell(Vector3Int.zero).IsOccupied, Is.False);
            Assert.That(spawner.GetObjectsInCell(Vector3Int.zero), Is.Empty);
            Assert.That(spawner.GetAllObjects(), Is.EqualTo(new[] { detached }));
            yield return null;
        }

        [UnityTest]
        public IEnumerator FreedStateCallbackRegeneration_SuppressesTheOldCellFreedEvent()
        {
            FieldObjectSpawner spawner = CreateSpawner(out FieldGenerator field);
            SpawnedObjectInfo previous = spawner.SpawnAt(Vector3Int.zero);
            SpawnedObjectInfo replacement = null;
            int freed = 0;
            bool regenerated = false;
            spawner.OnCellFreed.AddListener(cell => freed++);
            field.OnCellStateChanged.AddListener(cell =>
            {
                if (!regenerated && !cell.IsOccupied)
                {
                    regenerated = true;
                    field.GenerateField();
                    replacement = spawner.SpawnAt(Vector3Int.zero);
                }
            });

            spawner.RemoveObject(previous.GameObject);

            Assert.That(regenerated, Is.True);
            Assert.That(replacement, Is.Not.Null);
            Assert.That(field.GetCell(Vector3Int.zero), Is.SameAs(replacement.Cell));
            Assert.That(replacement.Cell.IsOccupied, Is.True);
            Assert.That(spawner.GetObjectsInCell(Vector3Int.zero), Is.EqualTo(new[] { replacement }));
            Assert.That(spawner.GetAllObjects(), Is.EqualTo(new[] { replacement }));
            Assert.That(freed, Is.Zero);
            yield return null;
            Assert.That(previous.GameObject == null, Is.True);
        }

        [UnityTest]
        public IEnumerator MultipleBlockers_PreserveOrdinaryEventOrderAndDecorativeObjectTracking()
        {
            FieldObjectSpawner spawner = CreateSpawner(out FieldGenerator field);
            List<string> events = new List<string>();
            spawner.OnObjectSpawned.AddListener(info => events.Add("spawn"));
            spawner.OnObjectRemoved.AddListener(info => events.Add("remove"));
            field.OnCellStateChanged.AddListener(cell => events.Add(cell.IsOccupied ? "occupied-state" : "freed-state"));
            spawner.OnCellOccupied.AddListener(cell => events.Add("occupied"));
            spawner.OnCellFreed.AddListener(cell => events.Add("freed"));

            SpawnedObjectInfo first = spawner.SpawnAt(Vector3Int.zero);
            SpawnedObjectInfo second = spawner.SpawnAt(Vector3Int.zero);
            SpawnedObjectInfo decoration = spawner.SpawnAt(Vector3Int.zero, occupiesSpace: false);
            spawner.RemoveObject(first.GameObject);
            Assert.That(field.GetCell(Vector3Int.zero).IsOccupied, Is.True);
            spawner.RemoveObject(second.GameObject);
            Assert.That(field.GetCell(Vector3Int.zero).IsOccupied, Is.False);
            Assert.That(spawner.GetObjectsInCell(Vector3Int.zero), Is.EqualTo(new[] { decoration }));
            spawner.RemoveObject(decoration.GameObject);

            Assert.That(events, Is.EqualTo(new[]
            {
                "spawn", "occupied-state", "occupied", "spawn", "spawn",
                "remove", "remove", "freed-state", "freed", "remove"
            }));
            Assert.That(spawner.GetAllObjects(), Is.Empty);
            yield return null;
            Assert.That(first.GameObject == null, Is.True);
            Assert.That(second.GameObject == null, Is.True);
            Assert.That(decoration.GameObject == null, Is.True);
        }

        [UnityTest]
        public IEnumerator NestedBlockers_PublishOnceAndFreeAfterOuterThenInnerRemoval()
        {
            return VerifySpawnPairLifecycle(true, true, true, true);
        }

        [UnityTest]
        public IEnumerator NestedBlockers_PublishOnceAndFreeAfterInnerThenOuterRemoval()
        {
            return VerifySpawnPairLifecycle(true, false, true, true);
        }

        [UnityTest]
        public IEnumerator SequentialBlockers_PublishOnceAndFreeAfterOuterThenInnerRemoval()
        {
            return VerifySpawnPairLifecycle(false, true, true, true);
        }

        [UnityTest]
        public IEnumerator SequentialBlockers_PublishOnceAndFreeAfterInnerThenOuterRemoval()
        {
            return VerifySpawnPairLifecycle(false, false, true, true);
        }

        [UnityTest]
        public IEnumerator NestedDecoration_DoesNotPublishOrRetainTheOuterBlockersOccupancy()
        {
            return VerifySpawnPairLifecycle(true, true, true, false);
        }

        [UnityTest]
        public IEnumerator NestedBlocker_PublishesWhileItsOuterDecorationIsStillInFlight()
        {
            return VerifySpawnPairLifecycle(true, false, false, true);
        }

        [UnityTest]
        public IEnumerator NestedPublishedBlockerRemoval_DoesNotRepeatOuterOccupation()
        {
            return VerifyPublishedBlockerRemoval(true);
        }

        [UnityTest]
        public IEnumerator SequentialPublishedBlockerRemoval_DoesNotRepeatReplacementOccupation()
        {
            return VerifyPublishedBlockerRemoval(false);
        }

        [UnityTest]
        public IEnumerator InitiallyOccupiedCell_FirstRegistrationStillNotifiesAndAllowsASecondBlocker()
        {
            return VerifySpawnPairLifecycle(false, true, true, true, true);
        }

        private IEnumerator VerifySpawnPairLifecycle(
            bool nested, bool removeOuterFirst, bool outerOccupiesSpace, bool innerOccupiesSpace,
            bool initiallyOccupied = false)
        {
            FieldObjectSpawner spawner = CreateSpawner(out FieldGenerator field);
            FieldCell cell = field.GetCell(Vector3Int.zero);
            if (initiallyOccupied)
            {
                field.SetOccupied(Vector3Int.zero, true);
            }

            SpawnedObjectInfo outer = null;
            SpawnedObjectInfo inner = null;
            bool spawnNested = nested;
            List<string> events = new List<string>();
            List<SpawnedObjectInfo> spawned = new List<SpawnedObjectInfo>();
            List<SpawnedObjectInfo> removed = new List<SpawnedObjectInfo>();
            List<bool> spawnCellStates = new List<bool>();
            List<bool> spawnQueries = new List<bool>();
            List<int> spawnRegistryCounts = new List<int>();
            List<FieldCell> changedCells = new List<FieldCell>();
            List<bool> changedStates = new List<bool>();
            List<FieldCell> occupiedCells = new List<FieldCell>();
            List<FieldCell> freedCells = new List<FieldCell>();

            field.OnCellStateChanged.AddListener(changed =>
            {
                changedCells.Add(changed);
                changedStates.Add(changed.IsOccupied);
                events.Add(changed.IsOccupied ? "state-occupied" : "state-free");
            });
            spawner.OnCellOccupied.AddListener(changed =>
            {
                occupiedCells.Add(changed);
                events.Add("occupied");
            });
            spawner.OnCellFreed.AddListener(changed =>
            {
                freedCells.Add(changed);
                events.Add("freed");
            });
            spawner.OnObjectRemoved.AddListener(info =>
            {
                removed.Add(info);
                events.Add(ReferenceEquals(info, outer) ? "remove-outer" : "remove-inner");
            });
            spawner.OnObjectSpawned.AddListener(info =>
            {
                if (outer == null)
                {
                    outer = info;
                    events.Add("spawn-outer");
                }
                else
                {
                    inner = info;
                    events.Add("spawn-inner");
                }

                spawned.Add(info);
                spawnCellStates.Add(cell.IsOccupied);
                spawnQueries.Add(spawner.IsCellOccupied(Vector3Int.zero));
                spawnRegistryCounts.Add(spawner.GetObjectsInCell(Vector3Int.zero).Count);
                if (spawnNested)
                {
                    spawnNested = false;
                    inner = spawner.SpawnAt(Vector3Int.zero, occupiesSpace: innerOccupiesSpace);
                    events.Add("inner-return");
                }
            });

            outer = spawner.SpawnAt(Vector3Int.zero, occupiesSpace: outerOccupiesSpace);
            events.Add("outer-return");
            if (!nested)
            {
                inner = spawner.SpawnAt(Vector3Int.zero, occupiesSpace: innerOccupiesSpace);
                events.Add("inner-return");
            }

            Assert.That(outer, Is.Not.Null);
            Assert.That(inner, Is.Not.Null);
            Assert.That(outer.GameObject, Is.Not.SameAs(inner.GameObject));
            Assert.That(spawned, Is.EqualTo(new[] { outer, inner }));
            Assert.That(spawnRegistryCounts, Is.EqualTo(new[] { 1, 2 }));
            Assert.That(spawnCellStates, Is.EqualTo(nested
                ? new[] { initiallyOccupied, initiallyOccupied }
                : new[] { initiallyOccupied, true }));
            Assert.That(spawnQueries, Is.EqualTo(new[] { outerOccupiesSpace, true }));
            Assert.That(field.GetCell(Vector3Int.zero), Is.SameAs(cell));
            Assert.That(cell.IsOccupied, Is.True);
            Assert.That(spawner.IsCellOccupied(Vector3Int.zero), Is.True);
            Assert.That(spawner.GetObjectsInCell(Vector3Int.zero), Is.EqualTo(new[] { outer, inner }));
            Assert.That(spawner.GetAllObjects(), Is.EqualTo(new[] { outer, inner }));
            Assert.That(occupiedCells, Is.EqualTo(new[] { cell }));
            Assert.That(changedStates, Is.EqualTo(new[] { true }));
            Assert.That(freedCells, Is.Empty);
            AssertOccupiedPlacementRejected(field);

            List<string> expectedEvents;
            if (!nested)
            {
                expectedEvents = new List<string>
                {
                    "spawn-outer", "state-occupied", "occupied", "outer-return", "spawn-inner", "inner-return"
                };
            }
            else if (innerOccupiesSpace)
            {
                expectedEvents = new List<string>
                {
                    "spawn-outer", "spawn-inner", "state-occupied", "occupied", "inner-return", "outer-return"
                };
            }
            else
            {
                expectedEvents = new List<string>
                {
                    "spawn-outer", "spawn-inner", "inner-return", "state-occupied", "occupied", "outer-return"
                };
            }

            Assert.That(events, Is.EqualTo(expectedEvents));
            SpawnedObjectInfo[] removalOrder = removeOuterFirst ? new[] { outer, inner } : new[] { inner, outer };
            for (int i = 0; i < removalOrder.Length; i++)
            {
                SpawnedObjectInfo current = removalOrder[i];
                bool blockerRemains = i == 0 && removalOrder[1].OccupiesSpace;
                spawner.RemoveObject(current.GameObject);
                expectedEvents.Add(ReferenceEquals(current, outer) ? "remove-outer" : "remove-inner");
                if (current.OccupiesSpace && !blockerRemains)
                {
                    expectedEvents.Add("state-free");
                    expectedEvents.Add("freed");
                }

                SpawnedObjectInfo[] expectedTracked = i == 0
                    ? new[] { removalOrder[1] }
                    : new SpawnedObjectInfo[0];
                Assert.That(field.GetCell(Vector3Int.zero), Is.SameAs(cell));
                Assert.That(cell.IsOccupied, Is.EqualTo(blockerRemains));
                Assert.That(spawner.IsCellOccupied(Vector3Int.zero), Is.EqualTo(blockerRemains));
                Assert.That(spawner.GetObjectsInCell(Vector3Int.zero), Is.EqualTo(expectedTracked));
                Assert.That(spawner.GetAllObjects(), Is.EqualTo(expectedTracked));
                Assert.That(events, Is.EqualTo(expectedEvents));
                if (blockerRemains)
                {
                    AssertOccupiedPlacementRejected(field);
                }
                else
                {
                    GridPlacementService placement = new GridPlacementService(field);
                    Assert.That(placement.CanPlace(GridPlacementRequest.Single(Vector3Int.zero, 99)), Is.True);
                }
            }

            Assert.That(removed, Is.EqualTo(removalOrder));
            Assert.That(occupiedCells, Is.EqualTo(new[] { cell }));
            Assert.That(freedCells, Is.EqualTo(new[] { cell }));
            Assert.That(changedCells, Is.EqualTo(new[] { cell, cell }));
            Assert.That(changedStates, Is.EqualTo(new[] { true, false }));
            Assert.That(cell.ContentId, Is.EqualTo(-1));
            yield return null;
            Assert.That(outer.GameObject == null, Is.True);
            Assert.That(inner.GameObject == null, Is.True);
        }

        private IEnumerator VerifyPublishedBlockerRemoval(bool nested)
        {
            FieldObjectSpawner spawner = CreateSpawner(out FieldGenerator field);
            FieldCell cell = field.GetCell(Vector3Int.zero);
            SpawnedObjectInfo first = null;
            SpawnedObjectInfo second = null;
            bool spawnNested = nested;
            List<string> events = new List<string>();
            List<SpawnedObjectInfo> spawned = new List<SpawnedObjectInfo>();
            List<SpawnedObjectInfo> removed = new List<SpawnedObjectInfo>();
            List<FieldCell> changedCells = new List<FieldCell>();
            List<bool> changedStates = new List<bool>();
            List<FieldCell> occupiedCells = new List<FieldCell>();
            List<FieldCell> freedCells = new List<FieldCell>();
            field.OnCellStateChanged.AddListener(changed =>
            {
                changedCells.Add(changed);
                changedStates.Add(changed.IsOccupied);
                events.Add(changed.IsOccupied ? "state-occupied" : "state-free");
            });
            spawner.OnCellOccupied.AddListener(changed =>
            {
                occupiedCells.Add(changed);
                events.Add("occupied");
            });
            spawner.OnCellFreed.AddListener(changed =>
            {
                freedCells.Add(changed);
                events.Add("freed");
            });
            spawner.OnObjectRemoved.AddListener(info =>
            {
                removed.Add(info);
                events.Add(ReferenceEquals(info, first) ? "remove-A" : "remove-B");
            });
            spawner.OnObjectSpawned.AddListener(info =>
            {
                bool isFirst = first == null;
                if (isFirst)
                {
                    first = info;
                    events.Add("spawn-A");
                }
                else
                {
                    second = info;
                    events.Add("spawn-B");
                }

                spawned.Add(info);
                if (spawnNested)
                {
                    spawnNested = false;
                    second = spawner.SpawnAt(Vector3Int.zero);
                    events.Add("B-return");
                    spawner.RemoveObject(second.GameObject);
                    events.Add("B-remove-return");
                }
                else if (!nested && !isFirst)
                {
                    spawner.RemoveObject(first.GameObject);
                    events.Add("A-remove-return");
                }
            });

            first = spawner.SpawnAt(Vector3Int.zero);
            events.Add("A-return");
            if (!nested)
            {
                second = spawner.SpawnAt(Vector3Int.zero);
                events.Add("B-return");
            }

            Assert.That(first, Is.Not.Null);
            Assert.That(second, Is.Not.Null);
            SpawnedObjectInfo retired = nested ? second : first;
            SpawnedObjectInfo remaining = nested ? first : second;
            List<string> expectedEvents = nested
                ? new List<string>
                {
                    "spawn-A", "spawn-B", "state-occupied", "occupied", "B-return",
                    "remove-B", "B-remove-return", "A-return"
                }
                : new List<string>
                {
                    "spawn-A", "state-occupied", "occupied", "A-return", "spawn-B",
                    "remove-A", "A-remove-return", "B-return"
                };
            Assert.That(spawned, Is.EqualTo(new[] { first, second }));
            Assert.That(removed, Is.EqualTo(new[] { retired }));
            Assert.That(field.GetCell(Vector3Int.zero), Is.SameAs(cell));
            Assert.That(remaining.Cell, Is.SameAs(cell));
            Assert.That(cell.IsOccupied, Is.True);
            Assert.That(spawner.IsCellOccupied(Vector3Int.zero), Is.True);
            Assert.That(spawner.GetObjectsInCell(Vector3Int.zero), Is.EqualTo(new[] { remaining }));
            Assert.That(spawner.GetAllObjects(), Is.EqualTo(new[] { remaining }));
            Assert.That(changedCells, Is.EqualTo(new[] { cell }));
            Assert.That(changedStates, Is.EqualTo(new[] { true }));
            Assert.That(occupiedCells, Is.EqualTo(new[] { cell }));
            Assert.That(freedCells, Is.Empty);
            Assert.That(retired.GameObject != null, Is.True);
            Assert.That(remaining.GameObject != null, Is.True);
            AssertOccupiedPlacementRejected(field);
            Assert.That(events, Is.EqualTo(expectedEvents));

            yield return null;
            Assert.That(retired.GameObject == null, Is.True);
            Assert.That(remaining.GameObject != null, Is.True);
            Assert.That(cell.IsOccupied, Is.True);
            Assert.That(spawner.GetObjectsInCell(Vector3Int.zero), Is.EqualTo(new[] { remaining }));
            AssertOccupiedPlacementRejected(field);
            Assert.That(events, Is.EqualTo(expectedEvents));

            spawner.RemoveObject(remaining.GameObject);
            expectedEvents.Add(nested ? "remove-A" : "remove-B");
            expectedEvents.Add("state-free");
            expectedEvents.Add("freed");
            Assert.That(removed, Is.EqualTo(new[] { retired, remaining }));
            Assert.That(cell.IsOccupied, Is.False);
            Assert.That(spawner.IsCellOccupied(Vector3Int.zero), Is.False);
            Assert.That(spawner.GetObjectsInCell(Vector3Int.zero), Is.Empty);
            Assert.That(spawner.GetAllObjects(), Is.Empty);
            Assert.That(changedCells, Is.EqualTo(new[] { cell, cell }));
            Assert.That(changedStates, Is.EqualTo(new[] { true, false }));
            Assert.That(occupiedCells, Is.EqualTo(new[] { cell }));
            Assert.That(freedCells, Is.EqualTo(new[] { cell }));
            Assert.That(events, Is.EqualTo(expectedEvents));
            GridPlacementService placement = new GridPlacementService(field);
            Assert.That(placement.CanPlace(GridPlacementRequest.Single(Vector3Int.zero, 99)), Is.True);
            Assert.That(cell.ContentId, Is.EqualTo(-1));
            yield return null;
            Assert.That(remaining.GameObject == null, Is.True);
        }

        private static void AssertOccupiedPlacementRejected(FieldGenerator field)
        {
            GridPlacementService placement = new GridPlacementService(field);
            GridPlacementRequest request = GridPlacementRequest.Single(Vector3Int.zero, 99);
            request.RequireUnoccupied = true;
            Assert.That(placement.CanPlace(request, out string failureReason), Is.False);
            Assert.That(failureReason, Does.Contain("occupied"));
            GridPlacementResult result = placement.Place(request);
            Assert.That(result.Placed, Is.False);
            Assert.That(result.Cells, Is.Empty);
            Assert.That(result.Positions, Is.Empty);
            Assert.That(field.GetCell(Vector3Int.zero).ContentId, Is.EqualTo(-1));
            Assert.That(field.GetCell(Vector3Int.zero).IsOccupied, Is.True);
        }

        private FieldObjectSpawner CreateSpawner(out FieldGenerator field)
        {
            GameObject owner = CreateObject("Audit20261023GridLifecycle");
            field = owner.AddComponent<FieldGenerator>();
            field.DebugEnabled = false;
            field.GenerateField(new FieldGeneratorConfig(Vector3Int.one));
            FieldObjectSpawner spawner = owner.AddComponent<FieldObjectSpawner>();
            spawner.Prefabs = new[] { CreateObject("Audit20261023GridTemplate") };
            return spawner;
        }

        private GameObject CreateObject(string name)
        {
            GameObject owner = new GameObject(name);
            _objects.Add(owner);
            return owner;
        }
    }
}
