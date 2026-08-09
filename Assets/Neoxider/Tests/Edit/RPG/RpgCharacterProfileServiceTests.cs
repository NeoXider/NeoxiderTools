using Neo.Rpg.Runtime;
using Neo.Rpg.Components;
using NUnit.Framework;
using UnityEngine;

namespace Neo.Editor.Tests.RPG
{
    public sealed class RpgCharacterProfileServiceTests
    {
        [Test]
        public void RpgCharacter_IsLocalMonoBehaviourFacade()
        {
            Assert.That(typeof(RpgCharacter).BaseType, Is.EqualTo(typeof(MonoBehaviour)));
        }

        [Test]
        public void SerializeDeserialize_RoundTripsUniversalState()
        {
            RpgCharacterProfileData source = new()
            {
                Level = 7,
                Xp = 12.5f,
                UpgradePoints = 3,
                IsDead = true,
                InvulnerabilityLocks = 2
            };
            source.Resources.Add(new RpgResourceSaveEntry { Id = "DarkMana", Current = 4f, Max = 9f });
            source.Stats.Add(new RpgStatSaveEntry { Id = "Wisdom", Base = 11f });
            source.Upgrades.Add(new RpgUpgradeSaveEntry { StatId = "Wisdom", Count = 2 });

            RpgCharacterProfileService service = new();
            RpgCharacterProfileData restored = service.Deserialize(service.Serialize(source));

            Assert.That(restored, Is.Not.Null);
            Assert.That(restored.Level, Is.EqualTo(7));
            Assert.That(restored.Xp, Is.EqualTo(12.5f));
            Assert.That(restored.UpgradePoints, Is.EqualTo(3));
            Assert.That(restored.IsDead, Is.True);
            Assert.That(restored.InvulnerabilityLocks, Is.EqualTo(2));
            Assert.That(restored.Resources[0].Id, Is.EqualTo("DarkMana"));
            Assert.That(restored.Stats[0].Base, Is.EqualTo(11f));
            Assert.That(restored.Upgrades[0].Count, Is.EqualTo(2));
        }

        [Test]
        public void Serialize_ClonesBeforeSanitize_AndDoesNotMutateSource()
        {
            RpgCharacterProfileData source = new() { Level = 0, InvulnerabilityLocks = -2 };
            source.Resources.Add(new RpgResourceSaveEntry { Id = string.Empty, Current = 1f, Max = 2f });
            RpgCharacterProfileService service = new();

            RpgCharacterProfileData restored = service.Deserialize(service.Serialize(source));

            Assert.That(restored.Level, Is.EqualTo(1));
            Assert.That(restored.InvulnerabilityLocks, Is.EqualTo(0));
            Assert.That(restored.Resources, Is.Empty);
            Assert.That(source.Resources, Has.Count.EqualTo(1));
        }

        [Test]
        public void Deserialize_EmptyPayload_ReturnsNull()
        {
            RpgCharacterProfileService service = new();
            Assert.That(service.Deserialize(string.Empty), Is.Null);
        }
    }
}
