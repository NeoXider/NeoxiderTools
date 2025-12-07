using UnityEngine;

namespace Flexalon
{
    /// <summary> A layout determines how the children of a node are positioned. </summary>
    public interface Layout
    {
        /// <summary> Perform minimal work to determine what the size of node and available size for node's children. </summary>
        Bounds Measure(FlexalonNode node, Vector3 size, Vector3 min, Vector3 max);

        /// <summary> Position the children of node within the available bounds. </summary>
        void Arrange(FlexalonNode node, Vector3 layoutSize);
    }

    /// <summary> A constraint runs whenever a target layout is updated. </summary>
    public interface Constraint
    {
        GameObject Target { get; }
        void Constrain(FlexalonNode node);
    }
}