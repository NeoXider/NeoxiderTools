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
            RequireInterface requireInterface = (RequireInterface)attribute;
            Type requireType = requireInterface.RequireType;

            if (IsValidProperty(property, requireType))
            {
                label = new GUIContent(label) { tooltip = $"Requires {requireType.Name} interface" };
            }

            Color originalColor = GUI.color;
            try
            {
                GUI.color = new Color(0.7f, 1f, 0.7f);
                EditorGUI.PropertyField(position, property, label, true);
            }
            finally
            {
                GUI.color = originalColor;
            }

            if (ClearInvalidReference(property, requireType))
            {
                Debug.LogError($"Reference must implement {requireType.Name} interface " +
                               "(GameObjects must have a component that implements it).",
                    property.serializedObject.targetObject);
            }
        }

        public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
        {
            return EditorGUI.GetPropertyHeight(property, label, true);
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
        ///     Clears an assigned reference that does not satisfy the required interface.
        /// </summary>
        /// <param name="property">The property containing the object reference</param>
        /// <param name="targetType">The required interface type</param>
        /// <returns>True when an invalid reference was cleared; otherwise false.</returns>
        /// <remarks>
        ///     Mixed multi-object values are preserved. The caller applies the serialized changes and owns any logging.
        /// </remarks>
        public static bool ClearInvalidReference(SerializedProperty property, Type targetType)
        {
            if (!IsValidProperty(property, targetType) || property.hasMultipleDifferentValues ||
                IsReferenceValid(property.objectReferenceValue, targetType))
            {
                return false;
            }

            property.objectReferenceValue = null;
            return true;
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
