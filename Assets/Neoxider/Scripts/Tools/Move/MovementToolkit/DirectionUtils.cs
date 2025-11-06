using UnityEngine;

namespace Neo.Tools 
{
    public static class DirectionUtils
    {
        public static Vector3 ResolveDirection(Transform t, ConstantMover.DirectionSource source, Vector3 custom, bool local)
        {
            switch (source)
            {
                case ConstantMover.DirectionSource.Up2D:
                    return (local ? t.up : Vector3.up).normalized;
                case ConstantMover.DirectionSource.Right2D:
                    return (local ? t.right : Vector3.right).normalized;
                case ConstantMover.DirectionSource.Custom:
                    return (local ? t.TransformDirection(custom) : custom).normalized;
                case ConstantMover.DirectionSource.LocalForward3D:
                default:
                    return (local ? t.forward : Vector3.forward).normalized;
            }
        }

        public static Vector3 ResolveAxis(Transform t, ConstantRotator.AxisSource source, Vector3 custom, bool local)
        {
            switch (source)
            {
                case ConstantRotator.AxisSource.None:
                    return Vector3.zero;
                case ConstantRotator.AxisSource.Up2D:
                    return (local ? t.up : Vector3.up).normalized;
                case ConstantRotator.AxisSource.Right2D:
                    return (local ? t.right : Vector3.right).normalized;
                case ConstantRotator.AxisSource.Custom:
                    return (local ? t.TransformDirection(custom) : custom).normalized;
                case ConstantRotator.AxisSource.LocalForward3D:
                default:
                    return (local ? t.forward : Vector3.forward).normalized;
            }
        }
    }
}


