using Neo.Abilities;
using UnityEngine;

namespace Neo.Samples.Survivor
{
    /// <summary>
    ///     One data-authored upgrade offered on level-up. The whole survivor kit is data-driven:
    ///     add an upgrade asset to the config's pool and it shows up in the level-up choices —
    ///     no code changes. Each upgrade wraps an ability, a modifier, or a max-health boost.
    /// </summary>
    [CreateAssetMenu(menuName = "Neoxider/Survivor Demo/Upgrade", fileName = "Upgrade")]
    public sealed class SurvivorUpgrade : ScriptableObject
    {
        [Tooltip("Short title shown on the upgrade card.")]
        public string Title = "Upgrade";

        [TextArea]
        [Tooltip("One-line description of what it does.")]
        public string Description;

        [Tooltip("Accent color of the card (also used as an icon dot).")]
        public Color Accent = new Color(0.49f, 0.36f, 0.94f);

        [Tooltip("What picking this upgrade does.")]
        public SurvivorUpgradeKind Kind = SurvivorUpgradeKind.PermanentModifier;

        [Tooltip("PermanentModifier: modifier applied to the player each time this is chosen (stacks).")]
        public ModifierDefinition Modifier;

        [Tooltip("GrantAbility: the ability granted to the player.")]
        public AbilityDefinition Ability;

        [Tooltip("MaxHealth: how much maximum health to add.")]
        public float HealthBonus = 25f;

        [Tooltip("How many times this upgrade may be offered/taken. 0 = unlimited.")]
        public int MaxTimes;
    }
}
