using System;
using UnityEngine;

namespace Neo
{
    /// <summary>
    ///     Attribute to create a button in the inspector that can call a method
    /// </summary>
    /// <remarks>
    ///     Can be used with methods that have parameters. Parameters will be displayed as input fields in the inspector.
    /// </remarks>
    [AttributeUsage(AttributeTargets.Method)]
    public class ButtonAttribute : PropertyAttribute
    {
        /// <summary>
        ///     Creates a button attribute with optional custom name
        /// </summary>
        /// <param name="buttonName">Custom name for the button. If null, method name will be used</param>
        public ButtonAttribute(string buttonName = null, float width = 120)
        {
            ButtonName = buttonName;
            Width = width;
        }

        /// <summary>
        ///     Name to display on the button. If null, method name will be used
        /// </summary>
        public string ButtonName { get; private set; }

        /// <summary>
        ///     Width button.
        /// </summary>
        public float Width { get; private set; }
    }
}