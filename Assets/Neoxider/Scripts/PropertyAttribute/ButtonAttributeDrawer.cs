using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Neo
{
#if UNITY_EDITOR
    [CustomPropertyDrawer(typeof(ButtonAttribute))]
    public class ButtonAttributeDrawer : PropertyDrawer
    {
        private readonly Dictionary<string, Type> _parameterTypes = new();
        private readonly Dictionary<string, object> _parameterValues = new();

        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            // Get the method info
            MethodInfo methodInfo = GetMethodInfo(property);
            if (methodInfo == null)
            {
                return;
            }

            // Get the attribute
            var buttonAttribute = (ButtonAttribute)attribute;

            // Calculate button height
            float buttonHeight = EditorGUIUtility.singleLineHeight;
            float totalHeight = buttonHeight;

            // Draw parameter fields
            ParameterInfo[] parameters = methodInfo.GetParameters();
            foreach (ParameterInfo param in parameters)
            {
                totalHeight += EditorGUIUtility.singleLineHeight + EditorGUIUtility.standardVerticalSpacing;
                Rect paramRect = new(position.x, position.y + buttonHeight, position.width,
                    EditorGUIUtility.singleLineHeight);

                // Initialize default value if not set
                if (!_parameterValues.ContainsKey(param.Name))
                {
                    _parameterValues[param.Name] = GetDefaultValue(param.ParameterType);
                    _parameterTypes[param.Name] = param.ParameterType;
                }

                // Draw appropriate field based on parameter type
                _parameterValues[param.Name] = DrawParameterField(paramRect, param.Name, _parameterValues[param.Name],
                    param.ParameterType);
                buttonHeight += EditorGUIUtility.singleLineHeight + EditorGUIUtility.standardVerticalSpacing;
            }

            // Draw the button
            Rect buttonRect = new(position.x, position.y + totalHeight - buttonHeight, position.width,
                buttonHeight);
            string buttonText = GetButtonText(methodInfo, buttonAttribute);

            if (GUI.Button(buttonRect, buttonText))
            {
                // Call the method with parameters
                object[] paramValues = new object[parameters.Length];
                for (int i = 0; i < parameters.Length; i++)
                {
                    paramValues[i] = _parameterValues[parameters[i].Name];
                }

                methodInfo.Invoke(property.serializedObject.targetObject, paramValues);
            }
        }

        public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
        {
            MethodInfo methodInfo = GetMethodInfo(property);
            if (methodInfo == null)
            {
                return EditorGUIUtility.singleLineHeight;
            }

            float height = EditorGUIUtility.singleLineHeight;
            ParameterInfo[] parameters = methodInfo.GetParameters();
            height += parameters.Length *
                      (EditorGUIUtility.singleLineHeight + EditorGUIUtility.standardVerticalSpacing);

            return height;
        }

        private static MethodInfo GetMethodInfo(SerializedProperty property)
        {
            Object targetObject = property.serializedObject.targetObject;
            return targetObject != null ? FindButtonMethod(targetObject.GetType()) : null;
        }

        public static MethodInfo FindButtonMethod(Type targetObjectType)
        {
            if (targetObjectType == null)
            {
                return null;
            }

            MethodInfo[] methods = targetObjectType.GetMethods(BindingFlags.Instance | BindingFlags.Static |
                                                               BindingFlags.Public | BindingFlags.NonPublic);
            Array.Sort(methods, (left, right) => left.MetadataToken.CompareTo(right.MetadataToken));
            foreach (MethodInfo method in methods)
            {
                if (method.GetCustomAttribute<ButtonAttribute>(false) != null)
                {
                    return method;
                }
            }

            return null;
        }

        public static string GetButtonText(MethodInfo method, ButtonAttribute buttonAttribute)
        {
            if (buttonAttribute != null && !string.IsNullOrEmpty(buttonAttribute.ButtonName))
            {
                return buttonAttribute.ButtonName;
            }

            return method != null ? method.Name : string.Empty;
        }

        private object DrawParameterField(Rect position, string label, object value, Type type)
        {
            if (type == typeof(int))
            {
                return EditorGUI.IntField(position, label, (int)value);
            }

            if (type == typeof(float))
            {
                return EditorGUI.FloatField(position, label, (float)value);
            }

            if (type == typeof(string))
            {
                return EditorGUI.TextField(position, label, (string)value);
            }

            if (type == typeof(bool))
            {
                return EditorGUI.Toggle(position, label, (bool)value);
            }

            if (type == typeof(GameObject))
            {
                return EditorGUI.ObjectField(position, label, (GameObject)value, typeof(GameObject), true);
            }

            if (typeof(MonoBehaviour).IsAssignableFrom(type))
            {
                return EditorGUI.ObjectField(position, label, (MonoBehaviour)value, type, true);
            }

            if (type.IsEnum)
            {
                return EditorGUI.EnumPopup(position, label, (Enum)value);
            }

            return value;
        }

        public static object GetDefaultValue(Type type)
        {
            if (type.IsValueType)
            {
                return Activator.CreateInstance(type);
            }

            return null;
        }
    }
#else
    public class ButtonAttributeDrawer : MonoBehaviour { }
#endif
}
