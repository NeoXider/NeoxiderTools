namespace Neo.Abilities
{
    /// <summary>What the <see cref="ModifierEngine" /> does when a modifier is applied to a unit.</summary>
    public enum ModifierStackAction
    {
        /// <summary>Create an independent instance.</summary>
        CreateNew = 0,

        /// <summary>Refresh the existing instance's duration (and re-capture the ability level).</summary>
        Refresh = 1,

        /// <summary>Add a stack (up to MaxStacks) and refresh the existing instance.</summary>
        AddStack = 2,

        /// <summary>Add the full duration to the existing instance's remaining time.</summary>
        Prolong = 3,

        /// <summary>Keep the existing instance untouched; the application is rejected.</summary>
        Ignore = 4,

        /// <summary>Remove the oldest instance of this modifier, then create a new one.</summary>
        ReplaceOldest = 5
    }

    /// <summary>
    ///     Decides what applying a modifier does. <paramref name="existing" /> is the oldest active instance with the
    ///     same id on the unit (null when there is none) and <paramref name="activeInstances" /> counts all of them.
    /// </summary>
    public interface IModifierStackingRule
    {
        ModifierStackAction Decide(ModifierBlueprint blueprint, ModifierInstance existing, int activeInstances);
    }

    /// <summary>
    ///     Default stacking by <see cref="ModifierBlueprint.StackPolicy" />: Independent ⇒ always new; Refresh ⇒
    ///     refresh; Stack ⇒ add a stack; Unique ⇒ ignore re-application; Prolong ⇒ extend; IndependentCapped ⇒ new
    ///     instances up to MaxStacks, then the oldest is replaced.
    /// </summary>
    public class StandardModifierStackingRule : IModifierStackingRule
    {
        public static readonly StandardModifierStackingRule Instance = new StandardModifierStackingRule();

        public virtual ModifierStackAction Decide(ModifierBlueprint blueprint, ModifierInstance existing,
            int activeInstances)
        {
            switch (blueprint.StackPolicy)
            {
                case ModifierStackPolicy.Independent:
                    return ModifierStackAction.CreateNew;
                case ModifierStackPolicy.IndependentCapped:
                    int cap = blueprint.MaxStacks > 0 ? blueprint.MaxStacks : 1;
                    return activeInstances >= cap ? ModifierStackAction.ReplaceOldest : ModifierStackAction.CreateNew;
            }

            if (existing == null)
            {
                return ModifierStackAction.CreateNew;
            }

            switch (blueprint.StackPolicy)
            {
                case ModifierStackPolicy.Stack:
                    return ModifierStackAction.AddStack;
                case ModifierStackPolicy.Unique:
                    return ModifierStackAction.Ignore;
                case ModifierStackPolicy.Prolong:
                    return ModifierStackAction.Prolong;
                default:
                    return ModifierStackAction.Refresh;
            }
        }
    }
}
