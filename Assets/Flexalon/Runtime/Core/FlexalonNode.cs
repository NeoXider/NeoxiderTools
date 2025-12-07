using UnityEngine;
using System.Collections.Generic;

namespace Flexalon
{
    /// <summary> Represents a node in the Flexalon layout tree. </summary>
    public interface FlexalonNode
    {
        /// <summary> The GameObject associated with this node. </summary>
        GameObject GameObject{ get; }

        /// <summary> Marks this node and its parents as dirty, so they will be updated by the Flexalon component. </summary>
        void MarkDirty();

        /// <summary> True if this node is dirty. </summary>
        bool Dirty { get; }

        /// <summary> Forces this node, its parent nodes, and its children nodes to update immediately. </summary>
        void ForceUpdate();

        /// <summary> The parent layout node of this node. </summary>
        FlexalonNode Parent { get; }

        /// <summary> The children of this layout node. </summary>
        IReadOnlyList<FlexalonNode> Children { get; }

        /// <summary> The index of this node in its parent's Children list. </summary>
        int Index { get; }

        /// <summary> Adds a child to this layout node. </summary>
        /// <param name="child"> The child to add. </param>
        void AddChild(FlexalonNode child);

        /// <summary> Inserts a child into this layout node. </summary>
        /// <param name="child"> The child to insert. </param>
        /// <param name="index"> The index to insert the child at. </param>
        void InsertChild(FlexalonNode child, int index);

        /// <summary> Returns the child of this layout node. </summary>
        /// <param name="index"> The index of the child to return. </param>
        /// <returns> The child at the given index. </returns>
        FlexalonNode GetChild(int index);

        /// <summary> Removes this node from its parent layout node. </summary>
        void Detach();

        /// <summary> Removes all children from this layout node. </summary>
        void DetachAllChildren();

        /// <summary> Assigns a layout method to this node. </summary>
        void SetMethod(Layout method);

        /// <summary> Returns the layout method of this node. </summary>
        Layout Method { get; }

        /// <summary> Assigns a transform updater to this node. </summary>
        void SetTransformUpdater(TransformUpdater updater);

        /// <summary> Returns the FlexalonObject of this node. </summary>
        FlexalonObject FlexalonObject { get; }

        /// <summary> Returns true if FlexalonObject is set. </summary>
        bool HasFlexalonObject { get; }

        /// <summary> Assigns a FlexalonObject to this node. </summary>
        void SetFlexalonObject(FlexalonObject obj);

        /// <summary> Returns the assigned fixed size of this node. </summary>
        Vector3 Size { get; }

        /// <summary> Returns the assigned size factor of this node relative to the available space. </summary>
        Vector3 SizeOfParent { get; }

        /// <summary> Returns the assigned offset of this node relative to its layout position. </summary>
        Vector3 Offset { get; }

        /// <summary> Returns the assigned SizeType of this node. </summary>
        /// <param name="axis"> The axis to get the SizeType of. </param>
        /// <returns> The SizeType of the given axis. </returns>
        SizeType GetSizeType(Axis axis);

        /// <summary> Returns the assigned SizeType of this node. </summary>
        /// <param name="axis"> The axis to get the SizeType of. </param>
        /// <returns> The SizeType of the given axis. </returns>
        SizeType GetSizeType(int axis);

        /// <summary> Returns true if this node is not filling this axis and has a min size set. </summary>
        bool CanShrink(int axis);

        /// <summary> Returns the assigned relative scale of this node. </summary>
        Vector3 Scale { get; }

        /// <summary> Returns the assigned relative rotation of this node. </summary>
        Quaternion Rotation { get; }

        /// <summary> Returns the assigned margin of this node. </summary>
        Directions Margin { get; }

        /// <summary> Returns the assigned padding of this node. </summary>
        Directions Padding { get; }

        /// <summary> Returns the computed size of this node during the measure step. </summary>
        Vector3 GetMeasureSize(Vector3 layoutSize);

        /// <summary> Returns the computed size of this node during the measure step. </summary>
        float GetMeasureSize(int axis, float layoutSize);

        /// <summary> Returns the min size of this node, including margin. </summary>
        Vector3 GetMinSize(Vector3 parentSize);

        /// <summary> Returns the min size of this node, including margin. </summary>
        float GetMinSize(int axis, float parentSize);

        /// <summary> Returns the max size of this node, including margin. </summary>
        float GetMaxSize(int axis, float parentSize);

        /// <summary> Returns the computed size of this node during the arrange step. </summary>
        Vector3 GetArrangeSize();

        /// <summary> Returns the world position of the layout box. Used for gizmos. </summary>
        Vector3 GetWorldBoxPosition(Vector3 scale, bool includePadding);

        /// <summary> Returns the world scale of the layout box. Used for gizmos. </summary>
        Vector3 GetWorldBoxScale(bool includeLocalScale);

        /// <summary> Has layout ever run on this node? </summary>
        bool HasResult { get; }

        /// <summary> Returns the result of the last layout run. </summary>
        FlexalonResult Result { get; }

        /// <summary> Sets the space a child should shrink or fill. </summary>
        void SetShrinkFillSize(Vector3 childSize, Vector3 layoutSize, bool includesSizeOfParent = false);

        /// <summary> Sets the space a child should shrink or fill. </summary>
        void SetShrinkFillSize(int axis, float childSize, float layoutSize, bool includesSizeOfParent = false);

        /// <summary> Set the position result from a layout arrange step. </summary>
        void SetPositionResult(Vector3 position);

        /// <summary> Set the rotation result from a layout arrange step. </summary>
        void SetRotationResult(Quaternion quaternion);

        /// <summary> Constrains this node to the given target node. </summary>
        void SetConstraint(Constraint constraint, FlexalonNode target);

        /// <summary> Returns the constraint of this node. </summary>
        Constraint Constraint { get; }

        /// <summary> Returns the active adapter for this node. </summary>
        Adapter Adapter { get; }

        /// <summary> Overrides the default adapter for this node. </summary>
        void SetAdapter(Adapter adapter);

        /// <summary> Only applies rotation and scale changes to the node. Faster than marking it dirty. </summary>
        void ApplyScaleAndRotation();

        /// <summary> Returns the set of modifiers that apply to layout results. </summary>
        IReadOnlyList<FlexalonModifier> Modifiers { get; }

        /// <summary> Adds a modifier to this node. </summary>
        void AddModifier(FlexalonModifier modifier);

        /// <summary> Removes a modifier from this node. </summary>
        void RemoveModifier(FlexalonModifier modifier);

        /// <summary> Event invoked when layout results change. </summary>
        event System.Action<FlexalonNode> ResultChanged;

        /// <summary> True when this node is being dragged. </summary>
        bool IsDragging { get; set; }

        /// <summary> True when this node should not skipped when performing layout. </summary>
        bool SkipLayout { get; }
    }
}