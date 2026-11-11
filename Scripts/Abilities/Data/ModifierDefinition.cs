using UnityEngine;

namespace Neo.Abilities
{
    /// <summary>
    ///     ScriptableObject wrapper of a <see cref="ModifierBlueprint" /> — the authoring asset for one
    ///     buff/debuff/DoT/aura/shield. Referenced from ability effect nodes by its id.
    /// </summary>
    [NeoDoc("Abilities/ModifierDefinition.md")]
    [CreateAssetMenu(menuName = "Neoxider/Abilities/Modifier", fileName = "Modifier")]
    public sealed class ModifierDefinition : ScriptableObject
    {
        [SerializeField] private ModifierBlueprint _blueprint = new ModifierBlueprint();

        public ModifierBlueprint Blueprint => _blueprint;

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
