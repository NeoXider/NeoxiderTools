using UnityEngine;

namespace Neo.Abilities
{
    /// <summary>
    ///     ScriptableObject wrapper of an <see cref="AbilityBlueprint" /> — the authoring asset for one
    ///     ability. Everything a new ability needs is configured here; no code required.
    ///     Register it through an <see cref="AbilityLibrary" /> or <c>AbilitySystemBehaviour</c>.
    /// </summary>
    [NeoDoc("Abilities/AbilityDefinition.md")]
    [CreateAssetMenu(menuName = "Neoxider/Abilities/Ability", fileName = "Ability")]
    public sealed class AbilityDefinition : ScriptableObject
    {
        [SerializeField] private AbilityBlueprint _blueprint = new AbilityBlueprint();

        public AbilityBlueprint Blueprint => _blueprint;

        public string Id => _blueprint?.Id;

        private void OnValidate()
        {
            if (_blueprint != null && string.IsNullOrEmpty(_blueprint.Id))
            {
                _blueprint.Id = name.ToLowerInvariant().Replace(' ', '_');
            }
        }
    }
}
