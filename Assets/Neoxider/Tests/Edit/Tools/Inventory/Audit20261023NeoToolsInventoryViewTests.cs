using System;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.ExceptionServices;
using Neo.Tools;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using UnityEngine.TestTools;
using Object = UnityEngine.Object;

namespace Neo.Tests.Edit
{
    public sealed class Audit20261023NeoToolsInventoryViewTests : Audit20261023NeoToolsInventoryFixture
    {
        [Test]
        public void ItemView_BindAfterClear_ReactivatesItsOwnGameObject()
        {
            InventoryItemView itemView = CreateObject("ItemView").AddComponent<InventoryItemView>();
            itemView.Bind(null, 7, 1);
            itemView.Clear();
            Assert.That(itemView.gameObject.activeSelf, Is.False);

            itemView.Bind(null, 7, 2);

            Assert.That(itemView.gameObject.activeSelf, Is.True);
            Assert.That(itemView.BoundItemId, Is.EqualTo(7));
        }

        [Test]
        public void SnapshotView_HidesRemovedItemsAndRowsFromPreviousInventory()
        {
            InventoryComponent first = CreateInventory();
            first.AddItemById(7);
            first.AddItemById(8);
            InventoryComponent second = CreateInventory();
            second.AddItemById(9);
            InventoryView view = CreateObject("InventoryView").AddComponent<InventoryView>();
            InventoryItemView prefab = CreateObject("ItemViewPrefab").AddComponent<InventoryItemView>();
            prefab.gameObject.SetActive(true);
            Configure(view, serialized =>
            {
                serialized.FindProperty("_itemViewPrefab").objectReferenceValue = prefab;
                serialized.FindProperty("_itemsRoot").objectReferenceValue = view.transform;
                serialized.FindProperty("_sourceMode").enumValueIndex = (int)InventoryViewSourceMode.SnapshotItems;
                serialized.FindProperty("_refreshNextFrameOnEnable").boolValue = false;
            });
            view.SetInventory(first);
            InventoryItemView removed = FindRow(view, 7);
            Assert.That(removed.gameObject.activeSelf, Is.True);

            first.RemoveItemById(7);
            view.Refresh();

            Assert.That(removed.gameObject.activeSelf, Is.False);
            Assert.That(FindRow(view, 8).gameObject.activeSelf, Is.True);
            view.SetInventory(second);
            Assert.That(FindRow(view, 8).gameObject.activeSelf, Is.False);
            Assert.That(FindRow(view, 9).gameObject.activeSelf, Is.True);
        }

        [TestCase(false)]
        [TestCase(true)]
        public void RebindingDisabledView_DoesNotSubscribeItToFutureInventoryChanges(bool grid)
        {
            InventoryComponent inventory = CreateInventory(InventoryStorageMode.SlotGrid, 1);
            InventoryItemView itemView = CreateObject("ItemView").AddComponent<InventoryItemView>();
            GameObject visual = CreateObject("ItemVisual");
            Configure(itemView, serialized =>
            {
                serialized.FindProperty("_itemId").intValue = 7;
                serialized.FindProperty("_root").objectReferenceValue = visual;
            });
            if (grid)
            {
                InventorySlotView slot = CreateObject("SlotView").AddComponent<InventorySlotView>();
                Configure(slot, serialized => serialized.FindProperty("_itemView").objectReferenceValue = itemView);
                InventorySlotGridView view = CreateObject("GridView").AddComponent<InventorySlotGridView>();
                Configure(view, serialized =>
                {
                    SerializedProperty slots = serialized.FindProperty("_manualSlots");
                    slots.arraySize = 1;
                    slots.GetArrayElementAtIndex(0).objectReferenceValue = slot;
                    serialized.FindProperty("_refreshNextFrameOnEnable").boolValue = false;
                });
                view.SetInventory(inventory);
            }
            else
            {
                InventoryView view = CreateObject("ListView").AddComponent<InventoryView>();
                Configure(view, serialized =>
                {
                    serialized.FindProperty("_viewMode").enumValueIndex = (int)InventoryViewMode.ManualList;
                    SerializedProperty views = serialized.FindProperty("_manualViews");
                    views.arraySize = 1;
                    views.GetArrayElementAtIndex(0).objectReferenceValue = itemView;
                    serialized.FindProperty("_refreshNextFrameOnEnable").boolValue = false;
                });
                view.SetInventory(inventory);
            }
            Assert.That(visual.activeSelf, Is.False);

            inventory.AddItemById(7);

            Assert.That(inventory.GetCount(7), Is.EqualTo(1));
            Assert.That(visual.activeSelf, Is.False, "Disabled views must wait until enable or explicit Refresh().");
        }

        [Test]
        public void RebindingSelectedGrid_CancelsTransferFromItsPreviousBinding()
        {
            InventoryComponent first = CreateInventory(InventoryStorageMode.SlotGrid);
            InventoryComponent replacement = CreateInventory(InventoryStorageMode.SlotGrid);
            InventoryComponent target = CreateInventory(InventoryStorageMode.SlotGrid);
            first.AddItemById(7);
            replacement.AddItemById(8);
            InventorySlotGridView sourceGrid = CreateGrid(first);
            InventorySlotGridView targetGrid = CreateGrid(target);
            sourceGrid.HandleSlotClick(0);

            sourceGrid.SetInventory(replacement);
            targetGrid.HandleSlotClick(0);

            Assert.That(first.GetCount(7), Is.EqualTo(1));
            Assert.That(replacement.GetCount(8), Is.EqualTo(1));
            Assert.That(target.TotalItemCount, Is.Zero);
            targetGrid.HandleSlotClick(0);
        }

        private InventorySlotGridView CreateGrid(InventoryComponent inventory)
        {
            InventorySlotGridView view = CreateObject("GridView").AddComponent<InventorySlotGridView>();
            Configure(view, serialized =>
                serialized.FindProperty("_refreshNextFrameOnEnable").boolValue = false);
            view.SetInventory(inventory);
            return view;
        }

        private static InventoryItemView FindRow(InventoryView owner, int itemId)
        {
            InventoryItemView[] rows = owner.GetComponentsInChildren<InventoryItemView>(true);
            InventoryItemView row = Array.Find(rows, candidate => candidate.BoundItemId == itemId);
            Assert.That(row, Is.Not.Null, $"Missing item row {itemId}.");
            return row;
        }
    }

    // These ownership checks need native OnEnable/OnDisable/OnDestroy. Keep their Play Mode
    // lifetime separate from the pure view contracts and the base fixture's global save provider.
    public sealed class Audit20261023NeoToolsInventoryViewLifetimeTests
    {
        private const double NativeWaitSeconds = 5d;
        private static readonly bool[] ViewModes = { false, true };
        private readonly List<GameObject> _createdObjects = new();
        private bool _ownsPlayMode;

        [UnitySetUp]
        public IEnumerator EnterNativeLifetime()
        {
            Assert.That(Application.isPlaying, Is.False, "This fixture must acquire its own Play Mode session.");
            yield return new EnterPlayMode();
            _ownsPlayMode = Application.isPlaying;
            Assert.That(Application.isPlaying, Is.True, "Ownership checks require native Play Mode lifetime.");
            yield return WaitForNativeCompletion(Time.frameCount, () => Application.isPlaying,
                "the first native frame after Play Mode entry");
        }

        [UnityTearDown]
        public IEnumerator LeaveNativeLifetime()
        {
            GameObject[] ownedObjects = _createdObjects.ToArray();
            ExceptionDispatchInfo cleanupFault = null;
            for (int i = ownedObjects.Length - 1; i >= 0; i--)
            {
                try
                {
                    if (ownedObjects[i] != null)
                    {
                        Object.Destroy(ownedObjects[i]);
                    }
                }
                catch (Exception exception)
                {
                    cleanupFault ??= ExceptionDispatchInfo.Capture(exception);
                }
            }

            if (_ownsPlayMode)
            {
                double deadline = EditorApplication.timeSinceStartup + NativeWaitSeconds;
                bool completionObserved = false;
                bool drained = false;
                int completionFrame = Time.frameCount;
                while (Application.isPlaying && EditorApplication.timeSinceStartup < deadline)
                {
                    if (AllDestroyed(ownedObjects))
                    {
                        if (!completionObserved)
                        {
                            completionObserved = true;
                            completionFrame = Time.frameCount;
                        }
                        else if (Time.frameCount != completionFrame)
                        {
                            drained = true;
                            break;
                        }
                    }

                    // This is an Editor poll; completion requires an actual native frame above.
                    yield return null;
                }

                if (!drained)
                {
                    Exception drainFault = Application.isPlaying
                        ? new TimeoutException("Owned objects did not become null and drain through a native frame.")
                        : new InvalidOperationException("Owned Play Mode ended before native cleanup completed.");
                    cleanupFault ??= ExceptionDispatchInfo.Capture(drainFault);
                }

                // Keep ExitPlayMode top-level, and attempt it even after a destruction/drain fault.
                if (Application.isPlaying)
                {
                    yield return new ExitPlayMode();
                }

                _ownsPlayMode = false;
            }

            _createdObjects.Clear();
            cleanupFault?.Throw();
        }

        [UnityTest]
        public IEnumerator Hand_ReleasesSpawnedVisualOnUnequipAndOwnerDestruction_WithExternalAnchor()
        {
            InventoryComponent inventory = CreateInventory();
            InventoryHand hand = CreateObject("Hand").AddComponent<InventoryHand>();
            GameObject anchor = CreateObject("ExternalHandAnchor");
            GameObject prefab = CreateObject("HandPrefab");
            Configure(hand, serialized =>
            {
                serialized.FindProperty("_inventory").objectReferenceValue = inventory;
                serialized.FindProperty("_autoFindInventory").boolValue = false;
                serialized.FindProperty("_handAnchor").objectReferenceValue = anchor.transform;
                serialized.FindProperty("_fallbackHandPrefab").objectReferenceValue = prefab;
                serialized.FindProperty("_allowUseInput").boolValue = false;
                serialized.FindProperty("_allowDropInput").boolValue = false;
            });
            int activationFrame = Time.frameCount;
            anchor.SetActive(true);
            prefab.SetActive(true);
            hand.gameObject.SetActive(true);
            yield return WaitForNativeCompletion(activationFrame, () => hand.isActiveAndEnabled,
                "hand activation");
            Assert.That(hand.isActiveAndEnabled, Is.True);
            Assert.That(anchor.transform.childCount, Is.Zero);

            // No explicit refresh: this grant must reach the subscriber installed by native OnEnable.
            int grantFrame = Time.frameCount;
            Assert.That(inventory.AddItemById(7), Is.EqualTo(1));
            yield return WaitForNativeCompletion(grantFrame, () => anchor.transform.childCount == 1,
                "the granted hand visual");
            Assert.That(anchor.transform.childCount, Is.EqualTo(1));
            GameObject firstVisual = anchor.transform.GetChild(0).gameObject;
            Assert.That(firstVisual.activeInHierarchy, Is.True);

            int unequipFrame = Time.frameCount;
            hand.SetSlotIndex(-1);
            yield return WaitForNativeCompletion(unequipFrame, () => firstVisual == null,
                "unequipped visual destruction");
            Assert.That(anchor.transform.childCount, Is.Zero);
            Assert.That(firstVisual == null, Is.True);
            int equipFrame = Time.frameCount;
            hand.SetSlotIndex(0);
            yield return WaitForNativeCompletion(equipFrame, () => anchor.transform.childCount == 1,
                "the re-equipped hand visual");
            Assert.That(anchor.transform.childCount, Is.EqualTo(1));
            GameObject secondVisual = anchor.transform.GetChild(0).gameObject;
            int destructionFrame = Time.frameCount;
            Object.Destroy(hand.gameObject);
            yield return WaitForNativeCompletion(destructionFrame, () => hand == null,
                "hand owner destruction");
            Assert.That(hand == null, Is.True, "Native owner destruction must occur first.");
            // OnDestroy can itself schedule deferred destruction of its external visual.
            int ownerObservedFrame = Time.frameCount;
            yield return WaitForNativeCompletion(ownerObservedFrame, () => secondVisual == null,
                "hand visual destruction after its owner");

            Assert.That(anchor != null, Is.True);
            Assert.That(anchor.transform.childCount, Is.Zero);
            Assert.That(secondVisual == null, Is.True);
            Assert.That(prefab != null, Is.True);
            Assert.That(inventory.GetCount(7), Is.EqualTo(1));
        }

        [UnityTest]
        public IEnumerator DestroyingView_ReleasesOnlyItsSpawnedObjectsUnderExternalRoot(
            [ValueSource(nameof(ViewModes))] bool grid)
        {
            InventoryComponent inventory = CreateInventory(InventoryStorageMode.SlotGrid, 1);
            Assert.That(inventory.AddItemById(7), Is.EqualTo(1));
            GameObject root = CreateObject("ExternalViewRoot");
            GameObject owner = CreateObject("ViewOwner");
            GameObject prefabObject;
            Behaviour activeView;
            if (grid)
            {
                InventorySlotView prefab = CreateObject("SlotPrefab").AddComponent<InventorySlotView>();
                InventoryItemView item = CreateObject("SlotItemPrefab").AddComponent<InventoryItemView>();
                item.transform.SetParent(prefab.transform, false);
                Configure(prefab, serialized => serialized.FindProperty("_itemView").objectReferenceValue = item);
                InventorySlotGridView view = owner.AddComponent<InventorySlotGridView>();
                Configure(view, serialized =>
                {
                    serialized.FindProperty("_slotPrefab").objectReferenceValue = prefab;
                    serialized.FindProperty("_slotsRoot").objectReferenceValue = root.transform;
                    serialized.FindProperty("_refreshNextFrameOnEnable").boolValue = false;
                });
                view.SetInventory(inventory);
                activeView = view;
                prefabObject = prefab.gameObject;
            }
            else
            {
                InventoryItemView prefab = CreateObject("ItemPrefab").AddComponent<InventoryItemView>();
                InventoryView view = owner.AddComponent<InventoryView>();
                Configure(view, serialized =>
                {
                    serialized.FindProperty("_itemViewPrefab").objectReferenceValue = prefab;
                    serialized.FindProperty("_itemsRoot").objectReferenceValue = root.transform;
                    serialized.FindProperty("_refreshNextFrameOnEnable").boolValue = false;
                });
                view.SetInventory(inventory);
                activeView = view;
                prefabObject = prefab.gameObject;
            }

            GameObject unrelatedChild = CreateObject("UnrelatedChild");
            unrelatedChild.transform.SetParent(root.transform, false);
            unrelatedChild.SetActive(true);
            root.SetActive(true);
            int activationFrame = Time.frameCount;
            owner.SetActive(true);
            yield return WaitForNativeCompletion(activationFrame, () => activeView.isActiveAndEnabled,
                "view activation");
            Assert.That(activeView.isActiveAndEnabled, Is.True);
            Assert.That(root.transform.childCount, Is.EqualTo(2));
            GameObject spawned = root.transform.GetChild(0).gameObject;
            InventoryItemView spawnedItem = spawned.GetComponentInChildren<InventoryItemView>(true);
            Assert.That(spawnedItem, Is.Not.Null);
            Assert.That(spawnedItem.BoundItemId, Is.EqualTo(7));
            Assert.That(spawnedItem.gameObject.activeInHierarchy, Is.True);

            // These changes prove actual subscriptions; do not call Refresh or private lifecycle methods.
            Assert.That(inventory.RemoveItemById(7), Is.EqualTo(1));
            Assert.That(spawnedItem.gameObject.activeSelf, Is.False);
            Assert.That(inventory.AddItemById(7), Is.EqualTo(1));
            Assert.That(spawnedItem.gameObject.activeSelf, Is.True);
            Assert.That(root.transform.childCount, Is.EqualTo(2));
            int destructionFrame = Time.frameCount;
            Object.Destroy(owner);
            yield return WaitForNativeCompletion(destructionFrame, () => owner == null,
                "view owner destruction");
            Assert.That(owner == null, Is.True, "Native owner destruction must occur first.");
            int ownerObservedFrame = Time.frameCount;
            yield return WaitForNativeCompletion(ownerObservedFrame, () => spawned == null,
                "spawned view destruction after its owner");

            Assert.That(root != null, Is.True);
            Assert.That(root.transform.childCount, Is.EqualTo(1));
            Assert.That(spawned == null, Is.True);
            Assert.That(unrelatedChild != null, Is.True);
            Assert.That(unrelatedChild.transform.parent, Is.EqualTo(root.transform));
            Assert.That(prefabObject != null, Is.True);
            Assert.That(inventory.GetCount(7), Is.EqualTo(1));
        }

        [UnityTest]
        public IEnumerator DestroyingGrid_DoesNotDestroyManuallyAssignedSlotViews()
        {
            InventoryComponent inventory = CreateInventory(InventoryStorageMode.SlotGrid, 1);
            InventorySlotView manual = CreateObject("ManualSlot").AddComponent<InventorySlotView>();
            InventorySlotGridView view = CreateObject("ManualGrid").AddComponent<InventorySlotGridView>();
            Configure(view, serialized =>
            {
                SerializedProperty slots = serialized.FindProperty("_manualSlots");
                slots.arraySize = 1;
                slots.GetArrayElementAtIndex(0).objectReferenceValue = manual;
                serialized.FindProperty("_refreshNextFrameOnEnable").boolValue = false;
            });
            view.SetInventory(inventory);
            int activationFrame = Time.frameCount;
            view.gameObject.SetActive(true);
            yield return WaitForNativeCompletion(activationFrame, () => view.isActiveAndEnabled,
                "manual-slot grid activation");
            Assert.That(view.isActiveAndEnabled, Is.True);
            Assert.That(manual != null, Is.True);

            int destructionFrame = Time.frameCount;
            Object.Destroy(view.gameObject);
            yield return WaitForNativeCompletion(destructionFrame, () => view == null,
                "manual-slot grid owner destruction");
            Assert.That(view == null, Is.True);
            int ownerObservedFrame = Time.frameCount;
            yield return WaitForNativeCompletion(ownerObservedFrame, () => view == null,
                "a native cleanup frame after the manual-slot grid owner");

            Assert.That(Time.frameCount, Is.Not.EqualTo(ownerObservedFrame));
            Assert.That(manual != null, Is.True);
        }

        private static IEnumerator WaitForNativeCompletion(int afterFrame, Func<bool> completed, string description)
        {
            double deadline = EditorApplication.timeSinceStartup + NativeWaitSeconds;
            while (true)
            {
                Assert.That(Application.isPlaying, Is.True, $"Lost Play Mode while waiting for {description}.");
                if (Time.frameCount != afterFrame && completed())
                {
                    yield break;
                }

                Assert.That(EditorApplication.timeSinceStartup, Is.LessThan(deadline),
                    $"Timed out waiting for {description}; native frame {Time.frameCount}, starting frame {afterFrame}.");
                // Edit-assembly UnityTests still poll from the Editor after EnterPlayMode.
                yield return null;
            }
        }

        private static bool AllDestroyed(GameObject[] ownedObjects)
        {
            foreach (GameObject owned in ownedObjects)
            {
                if (owned != null)
                {
                    return false;
                }
            }

            return true;
        }

        private GameObject CreateObject(string name)
        {
            GameObject created = new(name);
            created.SetActive(false);
            _createdObjects.Add(created);
            return created;
        }

        private InventoryComponent CreateInventory(InventoryStorageMode mode = InventoryStorageMode.Aggregated,
            int slots = 4)
        {
            InventoryComponent inventory = CreateObject("NativeViewInventory").AddComponent<InventoryComponent>();
            Configure(inventory, serialized =>
            {
                serialized.FindProperty("_setInstanceOnAwake").boolValue = false;
                serialized.FindProperty("_storageMode").enumValueIndex = (int)mode;
                serialized.FindProperty("_slotCount").intValue = slots;
                serialized.FindProperty("_autoLoad").boolValue = false;
                serialized.FindProperty("_autoSave").boolValue = false;
                serialized.FindProperty("_saveKey").stringValue = string.Empty;
            });
            inventory.GetCount(0);
            return inventory;
        }

        private static void Configure(Object target, Action<SerializedObject> configure)
        {
            using SerializedObject serialized = new(target);
            configure(serialized);
            serialized.ApplyModifiedPropertiesWithoutUndo();
        }
    }
}
