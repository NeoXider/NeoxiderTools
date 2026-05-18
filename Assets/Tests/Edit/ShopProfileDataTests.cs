using Neo.Shop;
using NUnit.Framework;
using UnityEngine;

namespace Neo.Tests.Edit
{
    /// <summary>
    ///     Pure EditMode coverage for <see cref="ShopProfileData"/> — JSON round-trip, sanitize
    ///     (dedupe), and the helper APIs used by <see cref="Shop"/>.
    /// </summary>
    public sealed class ShopProfileDataTests
    {
        [Test]
        public void NewProfile_HasNoOwnedItemsAndEmptyEquipped()
        {
            ShopProfileData profile = new();

            Assert.That(profile.Version, Is.EqualTo(1));
            Assert.That(profile.OwnedItemIds, Is.Empty);
            Assert.That(profile.OwnedBundleIds, Is.Empty);
            Assert.That(profile.PriceOverrides, Is.Empty);
            Assert.That(profile.EquippedId, Is.EqualTo(""));
            Assert.That(profile.IsItemOwned("anything"), Is.False);
        }

        [Test]
        public void TryAddOwnedItem_AddsOnceThenIgnoresDuplicate()
        {
            ShopProfileData profile = new();

            Assert.That(profile.TryAddOwnedItem("hat"), Is.True);
            Assert.That(profile.TryAddOwnedItem("hat"), Is.False);
            Assert.That(profile.OwnedItemIds.Count, Is.EqualTo(1));
            Assert.That(profile.IsItemOwned("hat"), Is.True);
        }

        [Test]
        public void TryAddOwnedBundle_AddsOnceThenIgnoresDuplicate()
        {
            ShopProfileData profile = new();

            Assert.That(profile.TryAddOwnedBundle("starter"), Is.True);
            Assert.That(profile.TryAddOwnedBundle("starter"), Is.False);
            Assert.That(profile.OwnedBundleIds.Count, Is.EqualTo(1));
            Assert.That(profile.IsBundleOwned("starter"), Is.True);
        }

        [Test]
        public void SetPriceOverride_AddsThenUpdates()
        {
            ShopProfileData profile = new();

            profile.SetPriceOverride("hat", 10f);
            profile.SetPriceOverride("hat", 5f);

            Assert.That(profile.PriceOverrides.Count, Is.EqualTo(1));
            Assert.That(profile.GetPriceOrDefault("hat", 999f), Is.EqualTo(5f));
        }

        [Test]
        public void ClearPriceOverride_RemovesEntry()
        {
            ShopProfileData profile = new();
            profile.SetPriceOverride("hat", 10f);

            Assert.That(profile.ClearPriceOverride("hat"), Is.True);
            Assert.That(profile.PriceOverrides, Is.Empty);
            Assert.That(profile.GetPriceOrDefault("hat", 7f), Is.EqualTo(7f));
        }

        [Test]
        public void GetPriceOrDefault_ReturnsDefaultWhenAbsent()
        {
            ShopProfileData profile = new();

            Assert.That(profile.GetPriceOrDefault("unknown", 42f), Is.EqualTo(42f));
        }

        [Test]
        public void JsonRoundTrip_PreservesAllFields()
        {
            ShopProfileData source = new();
            source.TryAddOwnedItem("a");
            source.TryAddOwnedItem("b");
            source.TryAddOwnedBundle("starter");
            source.SetPriceOverride("a", 5f);
            source.EquippedId = "b";

            string json = JsonUtility.ToJson(source);
            ShopProfileData clone = JsonUtility.FromJson<ShopProfileData>(json);

            Assert.That(clone.OwnedItemIds, Is.EquivalentTo(new[] { "a", "b" }));
            Assert.That(clone.OwnedBundleIds, Is.EquivalentTo(new[] { "starter" }));
            Assert.That(clone.PriceOverrides.Count, Is.EqualTo(1));
            Assert.That(clone.PriceOverrides[0].Id, Is.EqualTo("a"));
            Assert.That(clone.PriceOverrides[0].Price, Is.EqualTo(5f));
            Assert.That(clone.EquippedId, Is.EqualTo("b"));
        }

        [Test]
        public void Clone_IsIndependentDeepCopy()
        {
            ShopProfileData source = new();
            source.TryAddOwnedItem("a");

            ShopProfileData clone = source.Clone();
            clone.TryAddOwnedItem("b");

            Assert.That(source.OwnedItemIds, Is.EquivalentTo(new[] { "a" }));
            Assert.That(clone.OwnedItemIds, Is.EquivalentTo(new[] { "a", "b" }));
        }

        [Test]
        public void Sanitize_DedupesOwnedAndPriceOverrides()
        {
            ShopProfileData profile = new();
            // Bypass TryAddOwnedItem to plant duplicates directly.
            profile.OwnedItemIds.Add("a");
            profile.OwnedItemIds.Add("a");
            profile.OwnedItemIds.Add("");
            profile.OwnedBundleIds.Add("bundle");
            profile.OwnedBundleIds.Add("bundle");
            profile.PriceOverrides.Add(new ShopRuntimePriceEntry("a", 1f));
            profile.PriceOverrides.Add(new ShopRuntimePriceEntry("a", 2f));
            profile.PriceOverrides.Add(new ShopRuntimePriceEntry("", 9f));

            profile.Sanitize();

            Assert.That(profile.OwnedItemIds, Has.Count.EqualTo(1));
            Assert.That(profile.OwnedItemIds[0], Is.EqualTo("a"));
            Assert.That(profile.OwnedBundleIds, Has.Count.EqualTo(1));
            Assert.That(profile.PriceOverrides, Has.Count.EqualTo(1));
            Assert.That(profile.PriceOverrides[0].Id, Is.EqualTo("a"));
        }

        [Test]
        public void Sanitize_ClampsVersionToOne()
        {
            ShopProfileData profile = new() { Version = 0 };
            profile.Sanitize();

            Assert.That(profile.Version, Is.EqualTo(1));
        }
    }
}
