using System.Collections.Generic;
using UnityEngine;

namespace Neo.Abilities
{
    /// <summary>
    ///     A bundle of ability and modifier definitions registered into the system in one call —
    ///     the project-level catalog asset. Multiple libraries may be registered (base game + DLC + mods).
    /// </summary>
    [NeoDoc("Abilities/AbilityLibrary.md")]
    [CreateAssetMenu(menuName = "Neoxider/Abilities/Ability Library", fileName = "AbilityLibrary")]
    public sealed class AbilityLibrary : ScriptableObject
    {
        [SerializeField] private List<AbilityDefinition> _abilities = new List<AbilityDefinition>();
        [SerializeField] private List<ModifierDefinition> _modifiers = new List<ModifierDefinition>();

        public IReadOnlyList<AbilityDefinition> Abilities => _abilities;
        public IReadOnlyList<ModifierDefinition> Modifiers => _modifiers;

        public void RegisterInto(AbilitySystem system)
        {
            if (system == null)
            {
                return;
            }

            for (int i = 0; i < _modifiers.Count; i++)
            {
                if (_modifiers[i] != null)
                {
                    system.RegisterModifier(_modifiers[i].Blueprint);
                }
            }

            for (int i = 0; i < _abilities.Count; i++)
            {
                if (_abilities[i] != null)
                {
                    system.RegisterAbility(_abilities[i].Blueprint);
                }
            }
        }
    }
}
