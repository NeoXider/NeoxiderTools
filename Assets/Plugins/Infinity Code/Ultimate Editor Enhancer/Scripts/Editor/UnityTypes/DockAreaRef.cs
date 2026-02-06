/*           INFINITY CODE          */
/*     https://infinity-code.com    */

using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEditor;

namespace InfinityCode.UltimateEditorEnhancer.UnityTypes
{
    public static class DockAreaRef
    {
        private static FieldInfo _panesField;
        private static Type _type;
        
        private static FieldInfo panesField
        {
            get
            {
                if (_panesField == null) _panesField = type.GetField("m_Panes", Reflection.InstanceLookup);
                return _panesField;
            }
        }

        private static Type type
        {
            get
            {
                if (_type == null) _type = Reflection.GetEditorType("DockArea");
                return _type;
            }
        }
        
        public static List<EditorWindow> GetPanes(object dockArea)
        {
            return panesField.GetValue(dockArea) as List<EditorWindow>;
        }
    }
}