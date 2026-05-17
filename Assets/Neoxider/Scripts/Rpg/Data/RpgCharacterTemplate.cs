using System;
using UnityEngine;

namespace Neo.Rpg
{
    /// <summary>
    ///     Top-level ScriptableObject describing a character archetype: starting resources,
    ///     stats, known buffs / status effects, and progression flow.
    ///     <para>Apply via <see cref="Components.RpgCharacter._template"/> or runtime
    ///     <c>RpgCharacter.ApplyTemplate(template)</c>.</para>
    ///     <para>Examples: <b>Warrior</b> (HP 150, Stamina 100, Strength 15), <b>Mage</b>
    ///     (HP 80, Mana 120, DarkMana 50, Intelligence 18), <b>Dota Hero</b>
    ///     (all stats grow every level), <b>Souls Hero</b> (manual upgrade points).</para>
    /// </summary>
    [NeoDoc("Rpg/RpgCharacterTemplate.md")]
    [CreateAssetMenu(menuName = "Neoxider/RPG/Character Template", fileName = "CharacterTemplate")]
    public sealed class RpgCharacterTemplate : ScriptableObject
    {
        [Header("Resources (HP / Mana / Stamina / custom …)")]
        [Tooltip("Resource pools this character has at start.")]
        public RpgResourceDefinition[] resources = Array.Empty<RpgResourceDefinition>();

        [Header("Stats (Strength / Defense / FireResist / custom …)")]
        [Tooltip("Single-value stats this character has at start.")]
        public RpgStatDefinition[] stats = Array.Empty<RpgStatDefinition>();

        [Header("Effect Library")]
        [Tooltip("Buff definitions this character KNOWS — referenced by id from runtime (ApplyBuffById).")]
        public BuffDefinition[] knownBuffs = Array.Empty<BuffDefinition>();

        [Tooltip("Status effect definitions this character KNOWS — referenced by id from runtime " +
                 "(ApplyStatusById).")]
        public StatusEffectDefinition[] knownStatuses = Array.Empty<StatusEffectDefinition>();

        [Header("Progression (level / upgrades)")]
        [Tooltip("Level-up flow. Leave empty to disable progression entirely.")]
        public RpgProgressionDefinition progression;

        [Header("Display (optional)")]
        public string displayName;

        [TextArea(2, 4)] public string description;

        public Sprite icon;
    }
}
