using System.Collections;
using System.Reflection;
using Neo.Save;
using Neo.Tools;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace Neo.Tests.Play
{
    public class InventoryPlayModeTests
    {
        private GameObject _go;
        private InventoryComponent _inventory;

        [SetUp]
        public void SetUp()
        {
            SaveProvider.DeleteAll();

            _go = new GameObject("InventoryPlayModeInstance");
            _inventory = _go.AddComponent<InventoryComponent>();
        }

        [TearDown]
        public void TearDown()
        {
            if (_go != null)
            {
                Object.DestroyImmediate(_go);
            }

            // Reset singleton
            typeof(Singleton<InventoryComponent>)
                .GetField("_instance", BindingFlags.NonPublic | BindingFlags.Static)
                ?.SetValue(null, null);
            SaveProvider.DeleteAll();
        }

        [UnityTest]
        public IEnumerator OnInventoryChanged_FiresEventProperly()
        {
            bool eventFired = false;
            _inventory.OnInventoryChanged.AddListener(() => eventFired = true);

            // Wait one frame to ensure initialization is fully settled
            yield return null;

            _inventory.AddItemByIdAmount(101, 1);

            Assert.IsTrue(eventFired);
            Assert.AreEqual(1, _inventory.TotalItemCount);
        }

        [UnityTest]
        public IEnumerator AddAndRemove_ItemCount_IsCorrect()
        {
            yield return null;

            _inventory.AddItemByIdAmount(42, 3);
            Assert.AreEqual(3, _inventory.GetCount(42), "Should have 3 items after adding");

            _inventory.RemoveItemByIdAmount(42, 1);
            Assert.AreEqual(2, _inventory.GetCount(42), "Should have 2 items after removing 1");
        }

        [UnityTest]
        public IEnumerator ClearInventory_RemovesAllItems()
        {
            yield return null;

            _inventory.AddItemByIdAmount(10, 5);
            _inventory.AddItemByIdAmount(20, 3);
            Assert.AreEqual(8, _inventory.TotalItemCount);

            _inventory.ClearInventory();
            Assert.AreEqual(0, _inventory.TotalItemCount, "After clear, total should be 0");
        }
    }
}
