using UnityEngine;

namespace Flexalon
{
    /// <summary> Common math help functions. </summary>
    public static class Math
    {
        public static readonly float MaxValue = 999999f;
        public static readonly Vector3 MaxVector = new Vector3(MaxValue, MaxValue, MaxValue);

        /// <summary> Returns the opposite direction. </summary>
        /// <param name="direction"> The direction to get the opposite of. </param>
        /// <returns> The opposite direction. </returns>
        public static Direction GetOppositeDirection(Direction direction)
        {
            switch (direction)
            {
                case Direction.PositiveX: return Direction.NegativeX;
                case Direction.NegativeX: return Direction.PositiveX;
                case Direction.PositiveY: return Direction.NegativeY;
                case Direction.NegativeY: return Direction.PositiveY;
                case Direction.PositiveZ: return Direction.NegativeZ;
                case Direction.NegativeZ: return Direction.PositiveZ;
                default: return Direction.PositiveX;
            }
        }

        /// <summary> Returns the opposite direction. </summary>
        /// <param name="direction"> The direction to get the opposite of. </param>
        /// <returns> The opposite direction. </returns>
        public static Direction GetOppositeDirection(int direction)
        {
            return GetOppositeDirection((Direction)direction);
        }

        /// <summary> Returns the axis of a direction. </summary>
        /// <param name="direction"> The direction to get the axis of. </param>
        /// <returns> The axis of the direction. </returns>
        public static Axis GetAxisFromDirection(Direction direction)
        {
            switch (direction)
            {
                case Direction.PositiveX: return Axis.X;
                case Direction.NegativeX: return Axis.X;
                case Direction.PositiveY: return Axis.Y;
                case Direction.NegativeY: return Axis.Y;
                case Direction.PositiveZ: return Axis.Z;
                case Direction.NegativeZ: return Axis.Z;
                default: return Axis.X;
            }
        }

        /// <summary> Returns the axis of a direction. </summary>
        /// <param name="direction"> The direction to get the axis of. </param>
        /// <returns> The axis of the direction. </returns>
        public static Axis GetAxisFromDirection(int direction)
        {
            return GetAxisFromDirection((Direction)direction);
        }

        /// <summary> Returns the positive and negative directions of an axis. </summary>
        /// <param name="axis"> The axis to get the directions of. </param>
        /// <returns> The positive and negative directions of the axis. </returns>
        public static (Direction, Direction) GetDirectionsFromAxis(Axis axis)
        {
            switch (axis)
            {
                case Axis.X: return (Direction.PositiveX, Direction.NegativeX);
                case Axis.Y: return (Direction.PositiveY, Direction.NegativeY);
                case Axis.Z: return (Direction.PositiveZ, Direction.NegativeZ);
                default: return (Direction.PositiveX, Direction.NegativeX);
            }
        }

        /// <summary> Returns the positive and negative directions of an axis. </summary>
        /// <param name="axis"> The axis to get the directions of. </param>
        /// <returns> The positive and negative directions of the axis. </returns>
        public static (Direction, Direction) GetDirectionsFromAxis(int axis)
        {
            return GetDirectionsFromAxis((Axis)axis);
        }

        /// <summary> Returns the positive direction of an axis. </summary>
        /// <param name="axis"> The axis to get the direction of. </param>
        /// <returns> The positive direction of the axis. </returns>
        public static float GetPositiveFromDirection(Direction direction)
        {
            switch (direction)
            {
                case Direction.PositiveX:
                case Direction.PositiveY:
                case Direction.PositiveZ:
                    return 1;
                default:
                    return -1;
            }
        }

        /// <summary> Returns the positive direction of an axis. </summary>
        /// <param name="axis"> The axis to get the direction of. </param>
        /// <returns> The positive direction of the axis. </returns>
        public static float GetPositiveFromDirection(int direction)
        {
            return GetPositiveFromDirection((Direction)direction);
        }

        /// <summary> Returns a unit vector in the direction. </summary>
        /// <param name="direction"> The direction to get the vector of. </param>
        /// <returns> A unit vector in the direction. </returns>
        public static Vector3 GetVectorFromDirection(Direction direction)
        {
            switch (direction)
            {
                case Direction.PositiveX:
                    return Vector3.right;
                case Direction.PositiveY:
                    return Vector3.up;
                case Direction.PositiveZ:
                    return Vector3.forward;
                case Direction.NegativeX:
                    return Vector3.left;
                case Direction.NegativeY:
                    return Vector3.down;
                case Direction.NegativeZ:
                    return Vector3.back;
            }

            return Vector3.zero;
        }

        /// <summary> Returns a unit vector in the direction. </summary>
        /// <param name="direction"> The direction to get the vector of. </param>
        /// <returns> A unit vector in the direction. </returns>
        public static Vector3 GetVectorFromDirection(int direction)
        {
            return GetVectorFromDirection((Direction)direction);
        }

        /// <summary> Returns a unit vector in the positive direction of axis. </summary>
        /// <param name="axis"> The axis to get the vector of. </param>
        /// <returns> A unit vector in the axis. </returns>
        public static Vector3 GetVectorFromAxis(Axis axis)
        {
            switch (axis)
            {
                case Axis.X:
                    return Vector3.right;
                case Axis.Y:
                    return Vector3.up;
                case Axis.Z:
                    return Vector3.forward;
            }

            return Vector3.zero;
        }

        /// <summary> Returns a unit vector in the positive direction of axis. </summary>
        /// <param name="axis"> The axis to get the vector of. </param>
        /// <returns> A unit vector in the axis. </returns>
        public static Vector3 GetVectorFromAxis(int axis)
        {
            return GetVectorFromAxis((Axis)axis);
        }

        /// <summary> Returns the other two axes. </summary>
        /// <param name="axis"> The axis to get the other two axes of. </param>
        /// <returns> The other two axes. </returns>
        public static (Axis, Axis) GetOtherAxes(Axis axis)
        {
            switch (axis)
            {
                case Axis.X: return (Axis.Y, Axis.Z);
                case Axis.Y: return (Axis.X, Axis.Z);
                default: return (Axis.X, Axis.Y);
            }
        }

        /// <summary> Returns the other two axes. </summary>
        /// <param name="axis"> The axis to get the other two axes of. </param>
        /// <returns> The other two axes. </returns>
        public static (int, int) GetOtherAxes(int axis)
        {
            var other = GetOtherAxes((Axis)axis);
            return ((int)other.Item1, (int)other.Item2);
        }

        /// <summary> Given two axes, returns the third axis. </summary>
        /// <param name="axis1"> The first axis. </param>
        /// <param name="axis2"> The second axis. </param>
        /// <returns> The third axis. </returns>
        public static Axis GetThirdAxis(Axis axis1, Axis axis2)
        {
            var otherAxes = GetOtherAxes(axis1);
            return (otherAxes.Item1 == axis2) ? otherAxes.Item2 : otherAxes.Item1;
        }

        /// <summary> Given two axes, returns the third axis. </summary>
        /// <param name="axis1"> The first axis. </param>
        /// <param name="axis2"> The second axis. </param>
        /// <returns> The third axis. </returns>
        public static int GetThirdAxis(int axis1, int axis2)
        {
            return (int) GetThirdAxis((Axis)axis1, (Axis)axis2);
        }

        /// <summary> Returns the axes of a plane. </summary>
        /// <param name="plane"> The plane to get the axes of. </param>
        /// <returns> The axes of the plane. </returns>
        public static (Axis, Axis) GetPlaneAxes(Plane plane)
        {
            switch (plane)
            {
                case Plane.XY: return (Axis.X, Axis.Y);
                case Plane.XZ: return (Axis.X, Axis.Z);
                default: return (Axis.Z, Axis.Y);
            }
        }

        /// <summary> Returns the axes of a plane. </summary>
        /// <param name="plane"> The plane to get the axes of. </param>
        /// <returns> The axes of the plane. </returns>
        public static (int, int) GetPlaneAxesInt(Plane plane)
        {
            var axes = GetPlaneAxes(plane);
            return ((int)axes.Item1, (int)axes.Item2);
        }

        /// <summary> Multiplies each component of two vectors. </summary>
        /// <param name="a"> The first vector. </param>
        /// <param name="b"> The second vector. </param>
        /// <returns> The multiplied vector. </returns>
        public static Vector3 Mul(Vector3 a, Vector3 b)
        {
            a.x *= b.x;
            a.y *= b.y;
            a.z *= b.z;
            return a;
        }

        /// <summary> Divides each component of two vectors. </summary>
        /// <param name="a"> The divided vector. </param>
        /// <param name="b"> The divisor vector. </param>
        /// <returns> The divided vector. </returns>
        public static Vector3 Div(Vector3 a, Vector3 b)
        {
            a.x /= b.x;
            a.y /= b.y;
            a.z /= b.z;
            return a;
        }

        /// <summary> Divides each component of two vectors. If b is zero, returns 1. </summary>
        /// <param name="a"> The divided vector. </param>
        /// <param name="b"> The divisor vector. </param>
        /// <returns> The divided vector. </returns>
        public static Vector3 SafeDivOne(Vector3 a, Vector3 b)
        {
            a.x = b.x == 0 ? 1 : a.x / b.x;
            a.y = b.y == 0 ? 1 : a.y / b.y;
            a.z = b.z == 0 ? 1 : a.z / b.z;
            return a;
        }

        /// <summary> Divides each component of two vectors. If b is zero, returns 1. </summary>
        /// <param name="a"> The divided vector. </param>
        /// <param name="b"> The divisor vector. </param>
        /// <returns> The divided vector. </returns>
        public static Vector3 SafeDivZero(Vector3 a, Vector3 b)
        {
            a.x = b.x == 0 ? 0 : a.x / b.x;
            a.y = b.y == 0 ? 0 : a.y / b.y;
            a.z = b.z == 0 ? 0 : a.z / b.z;
            return a;
        }

        /// <summary> Rotates a bounds around the origin and returns a new bounds
        /// that encapsulates all of the rotated corners. </summary>
        /// <param name="bounds"> The bounds to rotate. </param>
        /// <param name="rotation"> The rotation to rotate the bounds by. </param>
        /// <returns> The new bounds. </returns>
        public static Bounds RotateBounds(Bounds bounds, Quaternion rotation)
        {
            if (rotation == Quaternion.identity) return bounds;

            var rotatedCenter = rotation * bounds.center;
            var p1 = rotation * bounds.max;
            var p2 = rotation * new Vector3(bounds.max.x, bounds.max.y, bounds.min.z);
            var p3 = rotation * new Vector3(bounds.max.x, bounds.min.y, bounds.max.z);
            var p4 = rotation * new Vector3(bounds.max.x, bounds.min.y, bounds.min.z);
            var p5 = rotation * new Vector3(bounds.min.x, bounds.max.y, bounds.max.z);
            var p6 = rotation * new Vector3(bounds.min.x, bounds.max.y, bounds.min.z);
            var p7 = rotation * new Vector3(bounds.min.x, bounds.min.y, bounds.max.z);
            var p8 = rotation * bounds.min;

            var rotatedBounds = new Bounds(rotatedCenter, Vector3.zero);
            rotatedBounds.Encapsulate(p1);
            rotatedBounds.Encapsulate(p2);
            rotatedBounds.Encapsulate(p3);
            rotatedBounds.Encapsulate(p4);
            rotatedBounds.Encapsulate(p5);
            rotatedBounds.Encapsulate(p6);
            rotatedBounds.Encapsulate(p7);
            rotatedBounds.Encapsulate(p8);
            return rotatedBounds;
        }

        /// <summary> Creates rotated and scaled bounds at center. </summary>
        /// <param name="center"> The center of the bounds. </param>
        /// <param name="size"> The size of the bound before rotation. </param>
        /// <param name="rotation"> The rotation to apply to the size.  </param>
        public static Bounds CreateRotatedBounds(Vector3 center, Vector3 size, Quaternion rotation)
        {
            if (rotation == Quaternion.identity) return new Bounds(center, size);
            var bounds = RotateBounds(new Bounds(Vector3.zero, size), rotation);
            bounds.center = center;
            return bounds;
        }

        /// <summary> Scales a bounds by multiplying the center and size by 'scale'. </summary>
        /// <param name="bounds"> The bounds to scale. </param>
        /// <param name="scale"> The scale to scale the bounds by. </param>
        /// <returns> The scaled bounds. </returns>
        public static Bounds ScaleBounds(Bounds bounds, Vector3 scale)
        {
            bounds.center = Math.Mul(bounds.center, scale);
            bounds.size = Math.Mul(bounds.size, scale);
            return bounds;
        }

        /// <summary> Determines the aligned position in a size. </summary>
        /// <param name="size"> The size to align to. </param>
        /// <param name="align"> The alignment. </param>
        /// <returns> The aligned position. </returns>
        public static float Align(float size, Align align)
        {
            if (align == global::Flexalon.Align.Start)
            {
                return -size * 0.5f;
            }
            else if (align == global::Flexalon.Align.End)
            {
                return size * 0.5f;
            }

            return 0;
        }

        /// <summary> Determines the aligned position in a size for an axis. </summary>
        /// <param name="size"> The size to align to. </param>
        /// <param name="axis"> The axis to align to. </param>
        /// <param name="align"> The alignment. </param>
        /// <returns> The aligned position. </returns>
        public static float Align(Vector3 size, int axis, Align align)
        {
            return Align(size[axis], align);
        }

        /// <summary> Determines the aligned position in a size. </summary>
        /// <param name="size"> The size to align to. </param>
        /// <param name="horizontal"> The horizontal alignment. </param>
        /// <param name="vertical"> The vertical alignment. </param>
        /// <param name="depth"> The depth alignment. </param>
        /// <returns> The aligned position. </returns>
        public static Vector3 Align(Vector3 size, Align horizontal, Align vertical, Align depth)
        {
            return new Vector3(
                Align(size, 0, horizontal),
                Align(size, 1, vertical),
                Align(size, 2, depth));
        }

        /// <summary> Aligns a child size to a parent size. </summary>
        /// <param name="childSize"> The size of the child. </param>
        /// <param name="parentSize"> The size of the parent. </param>
        /// <param name="parentAlign"> The alignment of the parent. </param>
        /// <param name="childAlign"> The pivot of the child. </param>
        /// <returns> The aligned position of the child. </returns>
        public static float Align(float childSize, float parentSize, Align parentAlign, Align childAlign)
        {
            return Align(parentSize, parentAlign) - Align(childSize, childAlign);
        }

        /// <summary> Aligns a child size to a parent size. </summary>
        /// <param name="childSize"> The size of the child. </param>
        /// <param name="parentSize"> The size of the parent. </param>
        /// <param name="align"> The alignment of the parent and child. </param>
        /// <returns> The aligned position of the child. </returns>
        public static float Align(float childSize, float parentSize, Align align)
        {
            return Align(childSize, parentSize, align, align);
        }

        /// <summary> Aligns a child size to a parent size on an axis. </summary>
        /// <param name="childSize"> The size of the child. </param>
        /// <param name="parentSize"> The size of the parent. </param>
        /// <param name="axis"> The axis to align on. </param>
        /// <param name="align"> The alignment of the parent and child. </param>
        /// <returns> The aligned position of the child. </returns>
        public static float Align(Vector3 childSize, Vector3 parentSize, int axis, Align align)
        {
            return Align(childSize[axis], parentSize[axis], align);
        }

        /// <summary> Aligns a child size to a parent size on an axis. </summary>
        /// <param name="childSize"> The size of the child. </param>
        /// <param name="parentSize"> The size of the parent. </param>
        /// <param name="axis"> The axis to align on. </param>
        /// <param name="align"> The alignment of the parent and child. </param>
        /// <returns> The aligned position of the child. </returns>
        public static float Align(Vector3 childSize, Vector3 parentSize, Axis axis, Align align)
        {
            return Align(childSize, parentSize, (int)axis, align);
        }

        /// <summary> Aligns a child size to a parent size on an axis. </summary>
        /// <param name="childSize"> The size of the child. </param>
        /// <param name="parentSize"> The size of the parent. </param>
        /// <param name="axis"> The axis to align on. </param>
        /// <param name="parentAlign"> The alignment of the parent. </param>
        /// <param name="childAlign"> The pivot of the child. </param>
        /// <returns> The aligned position of the child. </returns>
        public static float Align(Vector3 childSize, Vector3 parentSize, int axis, Align parentAlign, Align childAlign)
        {
            return Align(childSize[axis], parentSize[axis], parentAlign, childAlign);
        }

        /// <summary> Aligns a child size to a parent size on an axis. </summary>
        /// <param name="childSize"> The size of the child. </param>
        /// <param name="parentSize"> The size of the parent. </param>
        /// <param name="axis"> The axis to align on. </param>
        /// <param name="parentAlign"> The alignment of the parent. </param>
        /// <param name="childAlign"> The pivot of the child. </param>
        /// <returns> The aligned position of the child. </returns>
        public static float Align(Vector3 childSize, Vector3 parentSize, Axis axis, Align parentAlign, Align childAlign)
        {
            return Align(childSize, parentSize, (int)axis, parentAlign, childAlign);
        }

        /// <summary> Aligns a child size to a parent size on all axes. </summary>
        /// <param name="childSize"> The size of the child. </param>
        /// <param name="parentSize"> The size of the parent. </param>
        /// <param name="parentHorizontal"> The horizontal alignment of the parent. </param>
        /// <param name="parentVertical"> The vertical alignment of the parent. </param>
        /// <param name="parentDepth"> The depth alignment of the parent. </param>
        /// <param name="childHorizontal"> The horizontal pivot of the child. </param>
        /// <param name="childVertical"> The vertical pivot of the child. </param>
        /// <param name="childDepth"> The depth pivot of the child. </param>
        /// <returns> The aligned position of the child. </returns>
        public static Vector3 Align(Vector3 childSize, Vector3 parentSize, Align parentHorizontal, Align parentVertical, Align parentDepth, Align childHorizontal, Align childVertical, Align childDepth)
        {
            return Align(parentSize, parentHorizontal, parentVertical, parentDepth) -
                Align(childSize, childHorizontal, childVertical, childDepth);
        }

        /// <summary> Aligns a child size to a parent size on all axes. </summary>
        /// <param name="childSize"> The size of the child. </param>
        /// <param name="parentSize"> The size of the parent. </param>
        /// <param name="horizontal"> The horizontal alignment of the parent and child. </param>
        /// <param name="vertical"> The vertical alignment of the parent and child. </param>
        /// <param name="depth"> The depth alignment of the parent and child. </param>
        /// <returns> The aligned position of the child. </returns>
        public static Vector3 Align(Vector3 childSize, Vector3 parentSize, Align horizontal, Align vertical, Align depth)
        {
            return Align(parentSize, horizontal, vertical, depth) -
                Align(childSize, horizontal, vertical, depth);
        }

        /// <summary> Given the bounds of a component, creates a bounds for the node respecting the
        /// size types. Aspect ratio is preserved when possible. </summary>
        /// <param name="componentBounds"> The bounds of the component. </param>
        /// <param name="node"> The node to measure the bounds for. </param>
        /// <param name="size"> The size of the node. </param>
        /// <param name="min"> The minimum size of the node. </param>
        /// <param name="max"> The maximum size of the node. </param>
        /// <returns> The bounds of the node. </returns>
        public static Bounds MeasureComponentBounds(Bounds componentBounds, FlexalonNode node, Vector3 size, Vector3 min, Vector3 max)
        {
            componentBounds.size = Vector3.Max(componentBounds.size, Vector3.one * 0.0001f);
            var bounds = componentBounds;

            bool componentX = node.GetSizeType(Axis.X) == SizeType.Component;
            bool componentY = node.GetSizeType(Axis.Y) == SizeType.Component;
            bool componentZ = node.GetSizeType(Axis.Z) == SizeType.Component;

            var scale = (componentX && componentY && componentZ) ? 1 :
                Mathf.Min(
                    componentX ? float.MaxValue : size.x / bounds.size.x,
                    componentY ? float.MaxValue : size.y / bounds.size.y,
                    componentZ ? float.MaxValue : size.z / bounds.size.z);

            var maxScale = Mathf.Min(max.x / bounds.size.x, max.y / bounds.size.y, max.z / bounds.size.z);
            var minScale = Mathf.Max(min.x / bounds.size.x, min.y / bounds.size.y, min.z / bounds.size.z);
            var clampedScale = Mathf.Clamp(scale, minScale, maxScale);
            var clampedSize = Math.Clamp(size, min, max);

            bounds.size = new Vector3(
                componentX ? bounds.size.x * clampedScale : clampedSize.x,
                componentY ? bounds.size.y * clampedScale : clampedSize.y,
                componentZ ? bounds.size.z * clampedScale : clampedSize.z);

            bounds.center = Math.Mul(bounds.center, Math.Div(bounds.size, componentBounds.size));
            return bounds;
        }

        /// <summary> Given the bounds of a component, creates a bounds for the node respecting the
        /// size types. Aspect ratio is preserved for X and Y when possible. </summary>
        /// <param name="componentBounds"> The bounds of the component. </param>
        /// <param name="node"> The node to measure the bounds for. </param>
        /// <param name="size"> The size of the node. </param>
        /// <param name="min"> The minimum size of the node. </param>
        /// <param name="max"> The maximum size of the node. </param>
        /// <returns> The bounds of the node. </returns>
        public static Bounds MeasureComponentBounds2D(Bounds componentBounds, FlexalonNode node, Vector3 size, Vector3 min, Vector3 max)
        {
            componentBounds.size = Vector3.Max(componentBounds.size, Vector3.one * 0.0001f);
            var bounds = componentBounds;

            bool componentX = node.GetSizeType(Axis.X) == SizeType.Component;
            bool componentY = node.GetSizeType(Axis.Y) == SizeType.Component;

            var scale = (componentX && componentY) ? 1 :
                Mathf.Min(
                    componentX ? float.MaxValue : size.x / bounds.size.x,
                    componentY ? float.MaxValue : size.y / bounds.size.y);

            var maxScale = Mathf.Min(max.x / bounds.size.x, max.y / bounds.size.y);
            var minScale = Mathf.Max(min.x / bounds.size.x, min.y / bounds.size.y);
            var clampedScale = Mathf.Clamp(scale, minScale, maxScale);
            var clampedSize = Math.Clamp(size, min, max);

            bounds.size = new Vector3(
                componentX ? bounds.size.x * clampedScale : clampedSize.x,
                componentY ? bounds.size.y * clampedScale : clampedSize.y,
                clampedSize.z);

            bounds.center = Math.Mul(bounds.center, Math.Div(bounds.size, componentBounds.size));
            return bounds;
        }

        /// <summary> Applies absolute value of to each vector component. </summary>
        /// <param name="v"> The vector to apply absolute value to. </param>
        /// <returns> The vector with absolute value applied. </returns>
        public static Vector3 Abs(Vector3 v)
        {
            v.x = Mathf.Abs(v.x);
            v.y = Mathf.Abs(v.y);
            v.z = Mathf.Abs(v.z);
            return v;
        }

        /// <summary> Clamps value of to each vector component between min and max. </summary>
        /// <param name="v"> The vector to clamp. </param>
        /// <param name="min"> The minimum value. </param>
        /// <param name="max"> The maximum value. </param>
        /// <returns> The clamped vector. </returns>
        public static Vector3 Clamp(Vector3 v, Vector3 min, Vector3 max)
        {
            v.x = Mathf.Clamp(v.x, min.x, max.x);
            v.y = Mathf.Clamp(v.y, min.y, max.y);
            v.z = Mathf.Clamp(v.z, min.z, max.z);
            return v;
        }
    }
}