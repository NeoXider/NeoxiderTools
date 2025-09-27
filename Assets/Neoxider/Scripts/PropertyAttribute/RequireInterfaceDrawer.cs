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
            var requireType = requireInterface.RequireType;

            // Validate property type and interface
            if (IsValid(property, requireType))
            {
                // Add tooltip showing required interface
                label.tooltip = $"Requires {requireType.Name} interface";

                // Check if current value implements interface
                CheckProperty(property, requireType);
            }

            // Draw the property field with a green tint to indicate interface requirement
            var originalColor = GUI.color;
            GUI.color = new Color(0.7f, 1f, 0.7f); // Lighter green tint
            EditorGUI.PropertyField(position, property, label);
            GUI.color = originalColor;
        }

        /// <summary>
        ///     Validates that the property can hold object references and the required type is an interface
        /// </summary>
        /// <param name="property">The property to validate</param>
        /// <param name="targetType">The required interface type</param>
        /// <returns>True if the property is valid for interface checking</returns>
        private bool IsValid(SerializedProperty property, Type targetType)
        {
            return targetType.IsInterface && property.propertyType == SerializedPropertyType.ObjectReference;
        }

        /// <summary>
        ///     Checks if the assigned object implements the required interface
        /// </summary>
        /// <param name="property">The property containing the object reference</param>
        /// <param name="targetType">The required interface type</param>
        private void CheckProperty(SerializedProperty property, Type targetType)
        {
            if (property.objectReferenceValue == null)
                return;

            // Handle different types of Unity objects
            if (property.objectReferenceValue is GameObject gameObject)
                CheckGameObject(property, targetType, gameObject);
            else if (property.objectReferenceValue is ScriptableObject scriptableObject)
                CheckScriptableObject(property, targetType, scriptableObject);
        }

        /// <summary>
        ///     Validates that a GameObject has a component implementing the required interface
        /// </summary>
        /// <param name="property">The property containing the GameObject reference</param>
        /// <param name="targetType">The required interface type</param>
        /// <param name="gameObject">The GameObject to check</param>
        private void CheckGameObject(SerializedProperty property, Type targetType, GameObject gameObject)
        {
            if (gameObject.GetComponent(targetType) == null)
            {
                property.objectReferenceValue = null;
                Debug.LogError($"GameObject must have a component that implements {targetType.Name} interface");
            }
        }

        /// <summary>
        ///     Validates that a ScriptableObject implements the required interface
        /// </summary>
        /// <param name="property">The property containing the ScriptableObject reference</param>
        /// <param name="targetType">The required interface type</param>
        /// <param name="scriptableObject">The ScriptableObject to check</param>
        private void CheckScriptableObject(SerializedProperty property, Type targetType,
            ScriptableObject scriptableObject)
        {
            var objectType = scriptableObject.GetType();
            if (!targetType.IsAssignableFrom(objectType))
            {
                property.objectReferenceValue = null;
                Debug.LogError($"ScriptableObject must implement {targetType.Name} interface");
            }
        }
    }
}
#endif