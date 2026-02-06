/*           INFINITY CODE          */
/*     https://infinity-code.com    */

using System;
using System.Reflection;
using UnityEngine;

namespace InfinityCode.UltimateEditorEnhancer.UnityTypes
{
    public static class HostViewRef
    {
        private static PropertyInfo _positionProp;
        private static MethodInfo _setPositionMethod;
        private static Type _type;

        private static PropertyInfo positionProp
        {
            get
            {
                if (_positionProp == null) _positionProp = type.GetProperty("position", Reflection.InstanceLookup);
                return _positionProp;
            }
        }

        private static MethodInfo setPositionMethod
        {
            get
            {
                if (_setPositionMethod == null)
                {
                    _setPositionMethod = Reflection.GetMethod(type, "SetPosition", new[] { typeof(Rect) }, Reflection.InstanceLookup);
                }

                return _setPositionMethod;
            }
        }

        public static Type type
        {
            get
            {
                if (_type == null) _type = Reflection.GetEditorType("HostView");
                return _type;
            }
        }

        public static Rect GetPosition(object view)
        {
            return (Rect)positionProp.GetValue(view);
        }

        public static void SetPosition(object view, Rect position)
        {
            setPositionMethod.Invoke(view, new object[] {position});
        }
    }
}