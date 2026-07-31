using System.Collections.Generic;
using Neo.Core.Resources;
using UnityEngine;

namespace Neo.Abilities
{
    /// <summary>
    ///     Authoring asset for a unit archetype: team, resource pools, base properties and granted
    ///     abilities. <c>AbilityUnitBehaviour</c> applies it when registering into the system.
    /// </summary>
    [NeoDoc("Abilities/UnitTemplate.md")]
    [CreateAssetMenu(menuName = "Neoxider/Abilities/Unit Template", fileName = "UnitTemplate")]
    public sealed class UnitTemplate : ScriptableObject
    {
        [SerializeField] private string _displayName;

        [Tooltip("Team id. 0 = neutral.")]
        [SerializeField] private int _team;

        [Tooltip("Starting unit level for per-unit-level leveled ability values. Minimum 1.")]
        [SerializeField] [Min(1)] private int _startLevel = 1;

        [Tooltip("Resource pools. Include a 'health' pool for damageable units.")]
        [SerializeField] private List<UnitResourceConfig> _resources = new List<UnitResourceConfig>
        {
            new UnitResourceConfig { ResourceId = AbilityResourceIds.Health, Max = 100f },
            new UnitResourceConfig { ResourceId = AbilityResourceIds.Mana, Max = 100f, RegenPerSecond = 5f }
        };

        [Tooltip("Base property values before modifiers.")]
        [SerializeField] private List<UnitPropertyDefault> _baseProperties = new List<UnitPropertyDefault>
        {
            new UnitPropertyDefault { PropertyId = AbilityProperties.MoveSpeed, Value = 5f },
            new UnitPropertyDefault { PropertyId = AbilityProperties.AttackDamage, Value = 10f }
        };

        [Tooltip("Abilities granted on registration.")]
        [SerializeField] private List<AbilityDefinition> _abilities = new List<AbilityDefinition>();

        public string DisplayName => string.IsNullOrEmpty(_displayName) ? name : _displayName;
        public TeamId Team => new TeamId(_team);
        public int StartLevel => _startLevel < 1 ? 1 : _startLevel;
        public IReadOnlyList<UnitResourceConfig> Resources => _resources;
        public IReadOnlyList<UnitPropertyDefault> BaseProperties => _baseProperties;
        public IReadOnlyList<AbilityDefinition> Abilities => _abilities;

        /// <summary>Applies pools, base properties and abilities to a freshly created unit.</summary>
        public void ApplyTo(AbilityUnit unit)
        {
            if (unit == null)
            {
                return;
            }

            unit.DisplayName = DisplayName;
            unit.Team = Team;
            unit.SetLevel(StartLevel);

            for (int i = 0; i < _resources.Count; i++)
            {
                UnitResourceConfig config = _resources[i];
                if (string.IsNullOrEmpty(config.ResourceId) || config.Max <= 0f)
                {
                    continue;
                }

                unit.Resources.AddPool(config.ResourceId, new ResourcePoolEntry
                {
                    Current = config.Max,
                    Max = config.Max,
                    RegenPerSecond = config.RegenPerSecond,
                    // WHY: ResourcePoolModel only regenerates when the interval is positive.
                    RegenInterval = config.RegenPerSecond > 0f ? 0.1f : 0f,
                    MaxDecreaseAmount = -1f,
                    MaxIncreaseAmount = -1f
                });
            }

            for (int i = 0; i < _baseProperties.Count; i++)
            {
                UnitPropertyDefault property = _baseProperties[i];
                if (!string.IsNullOrEmpty(property.PropertyId))
                {
                    unit.SetBaseProperty(property.PropertyId, property.Value);
                }
            }

            for (int i = 0; i < _abilities.Count; i++)
            {
                AbilityDefinition ability = _abilities[i];
                if (ability != null && !string.IsNullOrEmpty(ability.Id))
                {
                    unit.System.RegisterAbility(ability.Blueprint);
                    unit.System.GrantAbility(unit.Id, ability.Id);
                }
            }
        }
    }
}
