/*           INFINITY CODE          */
/*     https://infinity-code.com    */

using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEditor;

namespace InfinityCode.UltimateEditorEnhancer.UnityTypes
{
    public static class PropertyEditorRef
    {
        private static Type _type;

        private static MethodInfo _addMethod;
        private static MethodInfo _openPropertyEditorMethod;

        private static MethodInfo openPropertyEditorMethod
        {
            get
            {
                if (_openPropertyEditorMethod == null) _openPropertyEditorMethod = type.GetMethod("OpenPropertyEditor", Reflection.StaticLookup, null, new[] { typeof(UnityEngine.Object), typeof(bool) }, null);
                return _openPropertyEditorMethod;
            }
        }
        
        public static Type type
        {
            get
            {
                if (_type == null) _type = Reflection.GetEditorType("PropertyEditor");
                return _type;
            }
        }
        
        public static EditorWindow OpenPropertyEditor(Object obj, bool showWindow = true)
        {
            return openPropertyEditorMethod.Invoke(null, new []{obj, showWindow}) as EditorWindow;
        }

#if UNITY_2021_2_OR_NEWER
        private static MethodInfo _openPropertyEditor2Method;

        private static MethodInfo openPropertyEditor2Method
        {
            get
            {
                if (_openPropertyEditor2Method == null) _openPropertyEditor2Method = type.GetMethod("OpenPropertyEditor", Reflection.StaticLookup, null, new[] { typeof(IList<UnityEngine.Object>) }, null);
                return _openPropertyEditor2Method;
            }
        }

        public static EditorWindow OpenPropertyEditor(IList<UnityEngine.Object> objects)
        {
            return openPropertyEditor2Method.Invoke(null, new []{ objects }) as EditorWindow;
        }
#endif
    }
}