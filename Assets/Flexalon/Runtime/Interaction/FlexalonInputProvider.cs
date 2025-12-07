using UnityEngine;

namespace Flexalon
{
    public enum InputMode
    {
        /// <summary> A ray is provided to determine which object is hovered and how it should be moved. </summary>
        Raycast,

        /// <summary> Objects are moved by an external system. Only state changes are provided. </summary>
        External
    }

    /// <summary> Implement this interface and assign it to the Flexalon.InputProvider
    /// to override how FlexalonInteractables receive input. </summary>
    public interface InputProvider
    {
        InputMode InputMode { get; }

        /// <summary> True if the input is active, e.g. button is being held down. </summary>
        bool Active { get; }

        /// <summary> In Raycast Mode, the screen-space position used to pick UI objects. </summary>
        Vector3 UIPointer { get; }

        /// <summary> In Raycast Mode, the ray to cast to determine what should be moved / hit. </summary>
        Ray Ray { get; }

        /// <summary> In External Mode, the object that is currently being hovered or selected. </summary>
        GameObject ExternalFocusedObject { get; }
    }
}