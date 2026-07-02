using System.Collections.Generic;
using System.Reflection;
using Neo.Shop;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.UI;

namespace Neo.Editor.Tests
{
    /// <summary>
    ///     Covers the 9.7.0 multi-category equipment manager: equip/unequip/toggle,
    ///     slot visuals and equipped-id tracking (persistence off — session only).
    /// </summary>
    [TestFixture]
    public class EquipmentManagerTests
    {
        private GameObject _go;
        private EquipmentManager _manager;
        private Image _hairImage;
        private Sprite _sprite;
        private EquipItemDefinition _hairItem;
        private readonly List<Object> _cleanup = new();

        [SetUp]
        public void SetUp()
        {
            _go = new GameObject("EquipmentManagerTests");
            _manager = _go.AddComponent<EquipmentManager>();

            var imageGo = new GameObject("HairImage", typeof(Image));
            imageGo.transform.SetParent(_go.transform);
            _hairImage = imageGo.GetComponent<Image>();

            var texture = new Texture2D(4, 4);
            _sprite = Sprite.Create(texture, new Rect(0, 0, 4, 4), new Vector2(0.5f, 0.5f));
            _cleanup.Add(texture);
            _cleanup.Add(_sprite);

            _hairItem = CreateItem("hair_red", "Hair", _sprite);

            var slot = new EquipmentManager.CategorySlot
            {
                CategoryId = "Hair",
                ImageTarget = _hairImage,
                ApplyNativeSize = false
            };

            SetPrivate("_items", new[] { _hairItem });
            SetPrivate("_slots", new[] { slot });
            SetPrivate("_persist", false);
        }

        [TearDown]
        public void TearDown()
        {
            Object.DestroyImmediate(_go);
            foreach (Object obj in _cleanup)
            {
                if (obj != null)
                {
                    Object.DestroyImmediate(obj);
                }
            }

            _cleanup.Clear();
        }

        private EquipItemDefinition CreateItem(string id, string categoryId, Sprite sprite)
        {
            var item = ScriptableObject.CreateInstance<EquipItemDefinition>();
            _cleanup.Add(item);

            typeof(EquipItemDefinition).GetField("_id", BindingFlags.Instance | BindingFlags.NonPublic)
                .SetValue(item, id);
            typeof(EquipItemDefinition).GetField("_categoryId", BindingFlags.Instance | BindingFlags.NonPublic)
                .SetValue(item, categoryId);
            typeof(EquipItemDefinition).GetField("_sprite", BindingFlags.Instance | BindingFlags.NonPublic)
                .SetValue(item, sprite);

            return item;
        }

        private void SetPrivate(string field, object value)
        {
            FieldInfo info = typeof(EquipmentManager).GetField(field,
                BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.IsNotNull(info, $"EquipmentManager.{field} field expected");
            info.SetValue(_manager, value);
        }

        [Test]
        public void Equip_AppliesSpriteAndTracksEquippedId()
        {
            _manager.Equip(_hairItem);

            Assert.AreEqual(_sprite, _hairImage.sprite);
            Assert.IsTrue(_hairImage.enabled);
            Assert.AreEqual("hair_red", _manager.GetEquippedId("Hair"));
            Assert.IsTrue(_manager.IsEquipped("hair_red"));
        }

        [Test]
        public void Unequip_ClearsSlotVisualAndId()
        {
            _manager.Equip(_hairItem);
            _manager.Unequip("Hair");

            Assert.IsNull(_hairImage.sprite);
            Assert.IsFalse(_hairImage.enabled);
            Assert.AreEqual("", _manager.GetEquippedId("Hair"));
            Assert.IsFalse(_manager.IsEquipped("hair_red"));
        }

        [Test]
        public void ToggleById_EquipsThenUnequips()
        {
            _manager.ToggleById("hair_red");
            Assert.IsTrue(_manager.IsEquipped("hair_red"), "first toggle equips");

            _manager.ToggleById("hair_red");
            Assert.IsFalse(_manager.IsEquipped("hair_red"), "second toggle unequips");
            Assert.IsFalse(_hairImage.enabled);
        }

        [Test]
        public void EquipById_ResolvesCatalogItem()
        {
            _manager.EquipById("hair_red");

            Assert.AreEqual("hair_red", _manager.GetEquippedId("Hair"));
            Assert.AreEqual(_sprite, _hairImage.sprite);
        }

        [Test]
        public void Equip_ReplacesItemInSameCategory()
        {
            var texture = new Texture2D(4, 4);
            Sprite otherSprite = Sprite.Create(texture, new Rect(0, 0, 4, 4), new Vector2(0.5f, 0.5f));
            _cleanup.Add(texture);
            _cleanup.Add(otherSprite);

            EquipItemDefinition blueHair = CreateItem("hair_blue", "Hair", otherSprite);
            SetPrivate("_items", new[] { _hairItem, blueHair });

            _manager.Equip(_hairItem);
            _manager.Equip(blueHair);

            Assert.AreEqual("hair_blue", _manager.GetEquippedId("Hair"));
            Assert.AreEqual(otherSprite, _hairImage.sprite);
            Assert.IsFalse(_manager.IsEquipped("hair_red"));
        }
    }
}
