using Neo.Core.Resources;
using NUnit.Framework;

namespace Neo.Editor.Tests
{
    public sealed class Audit20261023NeoCoreProfileTests
    {
        [Test]
        public void Sanitize_PreservesUncappedResourceForRoundTripIntoModel()
        {
            ResourceProfileData profile = new ResourceProfileData();
            profile.Entries.Add(new ResourceEntryData { Id = "Energy", Current = 25f, Max = 0f });

            profile.Sanitize();
            ResourcePoolModel model = new ResourcePoolModel();
            model.AddPool("Energy", new ResourcePoolEntry { MaxIncreaseAmount = -1f, MaxDecreaseAmount = -1f });
            model.SetMax("Energy", profile.Entries[0].Max);
            model.SetCurrent("Energy", profile.Entries[0].Current);

            Assert.That(profile.Entries[0].Current, Is.EqualTo(25f));
            Assert.That(model.GetCurrent("Energy"), Is.EqualTo(25f));
            Assert.That(model.GetMax("Energy"), Is.Zero);
        }

        [Test]
        public void Sanitize_RemovesInvalidEntriesAndRepairsNonFiniteValues()
        {
            ResourceProfileData profile = new ResourceProfileData();
            profile.Entries.Add(null);
            profile.Entries.Add(new ResourceEntryData { Id = " ", Current = 10f });
            profile.Entries.Add(new ResourceEntryData { Id = "HP", Current = float.NaN, Max = 100f });
            profile.Entries.Add(new ResourceEntryData { Id = "Mana", Current = 25f, Max = float.PositiveInfinity });

            profile.Sanitize();

            Assert.That(profile.Entries.Count, Is.EqualTo(2));
            Assert.That(profile.Entries[0].Current, Is.Zero);
            Assert.That(profile.Entries[0].Max, Is.EqualTo(100f));
            Assert.That(profile.Entries[1].Current, Is.EqualTo(25f));
            Assert.That(profile.Entries[1].Max, Is.Zero);
        }

        [Test]
        public void Clone_WithNullEntry_CopiesValidEntriesWithoutSharingState()
        {
            ResourceProfileData original = new ResourceProfileData();
            original.Entries.Add(null);
            original.Entries.Add(new ResourceEntryData { Id = "HP", Current = 25f, Max = 100f });

            ResourceProfileData copy = original.Clone();
            copy.Entries[0].Current = 50f;

            Assert.That(original.Entries.Count, Is.EqualTo(2));
            Assert.That(original.Entries[1].Current, Is.EqualTo(25f));
            Assert.That(copy.Entries.Count, Is.EqualTo(1));
            Assert.That(copy.Entries[0].Current, Is.EqualTo(50f));
            Assert.That(copy.Entries[0].Id, Is.EqualTo("HP"));
        }
    }
}
