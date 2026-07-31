#if UNITY_EDITOR
using System;
using UnityEditor;
using UnityEngine;

namespace Neo
{
    /// <summary>
    ///     Custom property drawer for RequireInterface attribute.
    ///     Ensures that assigned objects implement the required interface.
    /// </summary>
    [CustomPropertyDrawer(typeof(RequireInterface))]
    public class RequireInterfaceDrawer : PropertyDrawer
    {
        /// <summary>
        ///     Draws the property field and validates the assigned value
        /// </summary>
        /// <param name="position">Rectangle on the screen to use for the property GUI</param>
        /// <param name="property">The SerializedProperty to make the custom GUI for</param>
        /// <param name="label">The label to show on the property</param>
        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            var requireInterface = attribute as RequireInterface;
            Type requireType = requireInterface.RequireType;

            if (IsValidProperty(property, requireType))
            {
                label.tooltip = $"Requires {requireType.Name} interface";

                CheckProperty(property, requireType);
            }

            Color originalColor = GUI.color;
            GUI.color = new Color(0.7f, 1f, 0.7f);
            EditorGUI.PropertyField(position, property, label);
            GUI.color = originalColor;
        }

        /// <summary>
        ///     Validates that the property can hold object references and the required type is an interface
        /// </summary>
        /// <param name="property">The property to validate</param>
        /// <param name="targetType">The required interface type</param>
        /// <returns>True if the property is valid for interface checking</returns>
        public static bool IsValidProperty(SerializedProperty property, Type targetType)
        {
            return targetType != null &&
                   targetType.IsInterface &&
                   property != null &&
                   property.propertyType == SerializedPropertyType.ObjectReference;
        }

        /// <summary>
        ///     Checks if the assigned object implements the required interface
        /// </summary>
        /// <param name="property">The property containing the object reference</param>
        /// <param name="targetType">The required interface type</param>
        private void CheckProperty(SerializedProperty property, Type targetType)
        {
            if (property.objectReferenceValue == null)
            {
                return;
            }

            if (property.objectReferenceValue is GameObject gameObject)
            {
                if (!IsReferenceValid(gameObject, targetType))
                {
                    property.objectReferenceValue = null;
                    Debug.LogError($"GameObject must have a component that implements {targetType.Name} interface");
                }
            }
            else if (property.objectReferenceValue is ScriptableObject scriptableObject)
            {
                if (!IsReferenceValid(scriptableObject, targetType))
                {
                    property.objectReferenceValue = null;
                    Debug.LogError($"ScriptableObject must implement {targetType.Name} interface");
                }
            }
        }

        public static bool IsReferenceValid(UnityEngine.Object reference, Type targetType)
        {
            if (reference == null)
            {
                return true;
            }

            if (targetType == null || !targetType.IsInterface)
            {
                return false;
            }

            if (reference is GameObject gameObject)
            {
                return gameObject.GetComponent(targetType) != null;
            }

            if (reference is ScriptableObject scriptableObject)
            {
                return targetType.IsAssignableFrom(scriptableObject.GetType());
            }

            return targetType.IsAssignableFrom(reference.GetType());
        }
    }
}
#endif
