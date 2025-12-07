namespace Flexalon
{
    /// <summary> Interface for components that modify layout results. </summary>
    public interface FlexalonModifier
    {
        /// <summary> Called after the node's children are arranged.</summary>
        void PostArrange(FlexalonNode node);
    }
}