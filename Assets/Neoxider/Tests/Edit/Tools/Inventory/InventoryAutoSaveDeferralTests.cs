using System;
using Neo.Tools;
using NUnit.Framework;
using UnityEngine.Events;

namespace Neo.Tests.Edit
{
    /// <summary>
    ///     <see cref="InventoryComponent.DeferAutoSave" /> writes a burst of mutations once, keeps the per-mutation
    ///     events, nests, and tolerates a repeated or stale dispose; <see cref="InventoryComponent.AutoSave" /> mirrors
    ///     the Inspector toggle.
    /// </summary>
    public sealed class InventoryAutoSaveDeferralTests : Audit20261023NeoToolsInventoryFixture
    {
        [Test]
        public void WithoutDeferral_EveryChangingMutationSaves()
        {
            InventoryComponent inventory = CreateAutoSavingInventory(out SaveCounter saves);

            inventory.AddItemByIdAmount(1, 2);
            inventory.AddItemByIdAmount(2, 1);
            inventory.RemoveItemByIdAmount(1, 1);

            Assert.That(saves.Count, Is.EqualTo(3), "positive control: Auto Save writes after each mutation");
        }

        [Test]
        public void DeferredBurst_SavesOnceOnDispose_WithTheFinalContents()
        {
            string key = Guid.NewGuid().ToString("N");
            InventoryComponent inventory = CreateAutoSavingInventory(out SaveCounter saves, key);
            int changes = 0;
            inventory.OnInventoryChanged.AddListener(() => changes++);

            using (inventory.DeferAutoSave())
            {
                Assert.That(inventory.IsAutoSaveDeferred, Is.True);
                inventory.AddItemByIdAmount(1, 2);
                inventory.AddItemByIdAmount(2, 1);
                inventory.RemoveItemByIdAmount(1, 1);
                Assert.That(saves.Count, Is.Zero, "No write while the scope is open.");
                Assert.That(Provider.HasKey(key), Is.False);
            }

            Assert.That(inventory.IsAutoSaveDeferred, Is.False);
            Assert.That(saves.Count, Is.EqualTo(1));
            Assert.That(changes, Is.EqualTo(3), "Events still fire per mutation.");
            InventoryComponent restored = CreateInventory(saveKey: key);
            restored.Load();
            Assert.That(restored.GetCount(1), Is.EqualTo(1));
            Assert.That(restored.GetCount(2), Is.EqualTo(1));
        }

        [Test]
        public void NestedScopes_SaveOnlyWhenTheOutermostCloses()
        {
            InventoryComponent inventory = CreateAutoSavingInventory(out SaveCounter saves);

            using (inventory.DeferAutoSave())
            {
                using (inventory.DeferAutoSave())
                {
                    inventory.AddItemByIdAmount(1, 1);
                }

                Assert.That(saves.Count, Is.Zero, "An inner scope does not flush.");
                inventory.AddItemByIdAmount(1, 1);
            }

            Assert.That(saves.Count, Is.EqualTo(1));
        }

        [Test]
        public void ScopeWithoutChanges_DoesNotSave()
        {
            InventoryComponent inventory = CreateAutoSavingInventory(out SaveCounter saves);

            using (inventory.DeferAutoSave())
            {
                inventory.RemoveItemByIdAmount(5, 1);
            }

            Assert.That(saves.Count, Is.Zero);
        }

        [Test]
        public void ExplicitSaveInsideTheScope_ClearsThePendingWrite()
        {
            InventoryComponent inventory = CreateAutoSavingInventory(out SaveCounter saves);

            using (inventory.DeferAutoSave())
            {
                inventory.AddItemByIdAmount(1, 1);
                inventory.Save();
            }

            Assert.That(saves.Count, Is.EqualTo(1));
        }

        [Test]
        public void RepeatedInnerDispose_DoesNotCloseTheOuterScope()
        {
            InventoryComponent inventory = CreateAutoSavingInventory(out SaveCounter saves);
            InventoryComponent.AutoSaveScope first = inventory.DeferAutoSave();
            inventory.AddItemByIdAmount(1, 1);
            first.Dispose();
            Assert.That(saves.Count, Is.EqualTo(1));

            InventoryComponent.AutoSaveScope outer = inventory.DeferAutoSave();
            InventoryComponent.AutoSaveScope inner = inventory.DeferAutoSave();
            inner.Dispose();
            inner.Dispose();
            Assert.That(inventory.IsAutoSaveDeferred, Is.True, "A repeated inner dispose must not end the outer scope.");
            inventory.AddItemByIdAmount(1, 1);
            Assert.That(saves.Count, Is.EqualTo(1));
            outer.Dispose();

            Assert.That(inventory.IsAutoSaveDeferred, Is.False);
            Assert.That(saves.Count, Is.EqualTo(2));
        }

        [Test]
        public void StaleDispose_DoesNotCloseALaterScopeAtTheSameDepth()
        {
            InventoryComponent inventory = CreateAutoSavingInventory(out SaveCounter saves);
            InventoryComponent.AutoSaveScope stale = inventory.DeferAutoSave();
            stale.Dispose();

            InventoryComponent.AutoSaveScope current = inventory.DeferAutoSave();
            inventory.AddItemByIdAmount(1, 1);
            stale.Dispose();

            Assert.That(inventory.IsAutoSaveDeferred, Is.True, "A stale dispose must not end a newer outermost scope.");
            Assert.That(saves.Count, Is.Zero);
            current.Dispose();
            Assert.That(inventory.IsAutoSaveDeferred, Is.False);
            Assert.That(saves.Count, Is.EqualTo(1));
        }

        [Test]
        public void DeepNesting_GrowsBeyondTheInitialCapacity()
        {
            InventoryComponent inventory = CreateAutoSavingInventory(out SaveCounter saves);
            InventoryComponent.AutoSaveScope[] scopes = new InventoryComponent.AutoSaveScope[9];
            for (int i = 0; i < scopes.Length; i++)
            {
                scopes[i] = inventory.DeferAutoSave();
            }

            inventory.AddItemByIdAmount(1, 1);
            for (int i = scopes.Length - 1; i > 0; i--)
            {
                scopes[i].Dispose();
                Assert.That(inventory.IsAutoSaveDeferred, Is.True);
            }

            Assert.That(saves.Count, Is.Zero);
            scopes[0].Dispose();
            Assert.That(inventory.IsAutoSaveDeferred, Is.False);
            Assert.That(saves.Count, Is.EqualTo(1));
        }

        [Test]
        public void DefaultScope_DisposeIsANoOp()
        {
            InventoryComponent inventory = CreateAutoSavingInventory(out SaveCounter saves);
            InventoryComponent.AutoSaveScope open = inventory.DeferAutoSave();
            inventory.AddItemByIdAmount(1, 1);

            default(InventoryComponent.AutoSaveScope).Dispose();

            Assert.That(inventory.IsAutoSaveDeferred, Is.True);
            open.Dispose();
            Assert.That(saves.Count, Is.EqualTo(1));
        }

        [Test]
        public void OuterScopeDisposedFirst_EndsTheDeferral()
        {
            InventoryComponent inventory = CreateAutoSavingInventory(out SaveCounter saves);
            InventoryComponent.AutoSaveScope outer = inventory.DeferAutoSave();
            InventoryComponent.AutoSaveScope inner = inventory.DeferAutoSave();
            inventory.AddItemByIdAmount(1, 1);

            outer.Dispose();
            inner.Dispose();

            Assert.That(inventory.IsAutoSaveDeferred, Is.False, "Closing the outer level closes the inner one too.");
            Assert.That(saves.Count, Is.EqualTo(1));
        }

        [Test]
        public void AutoSaveProperty_MirrorsTheInspectorToggle()
        {
            InventoryComponent inventory = CreateAutoSavingInventory(out SaveCounter saves);

            inventory.AutoSave = false;
            inventory.AddItemByIdAmount(1, 1);
            Assert.That(saves.Count, Is.Zero);

            inventory.AutoSave = true;
            inventory.AddItemByIdAmount(1, 1);
            Assert.That(inventory.AutoSave, Is.True);
            Assert.That(saves.Count, Is.EqualTo(1));
        }

        private InventoryComponent CreateAutoSavingInventory(out SaveCounter saves, string saveKey = null)
        {
            InventoryComponent inventory = CreateInventory(saveKey: saveKey);
            inventory.AutoSave = true;
            inventory.OnSaved = new UnityEvent();
            SaveCounter counter = new();
            inventory.OnSaved.AddListener(counter.Increment);
            saves = counter;
            return inventory;
        }

        private sealed class SaveCounter
        {
            public int Count { get; private set; }

            public void Increment()
            {
                Count++;
            }
        }
    }
}
