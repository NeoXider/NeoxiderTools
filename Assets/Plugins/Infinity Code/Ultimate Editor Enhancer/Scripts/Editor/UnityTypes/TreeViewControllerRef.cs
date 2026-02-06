/*           INFINITY CODE          */
/*     https://infinity-code.com    */

using System;
using System.Reflection;
using UnityEditor.IMGUI.Controls;

namespace InfinityCode.UltimateEditorEnhancer.UnityTypes
{
    public static class TreeViewControllerRef
    {
        private static MethodInfo _changeExpandedStateMethod;
        private static PropertyInfo _dataProp;
        private static PropertyInfo _guiProp;
        private static Type _type;
        private static MethodInfo _userInputChangedExpandedStateMethod;
        private static FieldInfo _useExpansionAnimation;
        
        public static FieldInfo useExpansionAnimationField
        {
            get
            {
                if (_useExpansionAnimation == null) _useExpansionAnimation = type.GetField("m_UseExpansionAnimation", Reflection.InstanceLookup);
                return _useExpansionAnimation;
            }
        }

        private static MethodInfo changeExpandedStateMethod
        {
            get
            {
                if (_changeExpandedStateMethod == null) _changeExpandedStateMethod = type.GetMethod("ChangeExpandedState", Reflection.InstanceLookup);
                return _changeExpandedStateMethod;
            }
        }

        private static PropertyInfo dataProp
        {
            get
            {
                if (_dataProp == null) _dataProp = type.GetProperty("data", Reflection.InstanceLookup);
                return _dataProp;
            }
        }

        private static PropertyInfo guiProp
        {
            get
            {
                if (_guiProp == null) _guiProp = type.GetProperty("gui", Reflection.InstanceLookup);
                return _guiProp;
            }
        }

        public static Type type
        {
            get
            {
                if (_type == null) _type = Reflection.GetEditorType("IMGUI.Controls.TreeViewController");
                return _type;
            }
        }

        public static MethodInfo userInputChangedExpandedStateMethod
        {
            get
            {
                if (_userInputChangedExpandedStateMethod == null) _userInputChangedExpandedStateMethod = type.GetMethod("UserInputChangedExpandedState", Reflection.InstanceLookup);
                return _userInputChangedExpandedStateMethod;
            }
        }
        
        public static void ChangeExpandedState(object instance, TreeViewItem item, bool expanded, bool includeChildren)
        {
            changeExpandedStateMethod.Invoke(instance, new object[] { item, expanded, includeChildren });
        }

        public static object GetData(object instance)
        {
            return dataProp.GetValue(instance);
        }

        public static object GetGUI(object instance)
        {
            return guiProp.GetValue(instance);
        }
        
        public static bool GetUseExpansionAnimation(object instance)
        {
            return (bool)useExpansionAnimationField.GetValue(instance);
        }
        
        public static void SetUseExpansionAnimation(object instance, bool value)
        {
            useExpansionAnimationField.SetValue(instance, value);
        }
    }
}