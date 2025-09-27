using UnityEngine;
using Object = UnityEngine.Object;

namespace Neo.Extensions
{
    /// <summary>
    ///     Extension methods for UnityEngine.Object
    /// </summary>
    public static class ObjectExtensions
    {
        /// <summary>
        ///     Safely destroys an object, handling both play mode and edit mode
        /// </summary>
        /// <param name="obj">Object to destroy</param>
        /// <param name="immediate">Whether to destroy immediately (use with caution)</param>
        public static void SafeDestroy(this Object obj, bool immediate = false)
        {
            if (obj == null) return;

            if (Application.isPlaying)
            {
                if (immediate)
                    Object.DestroyImmediate(obj);
                else
                    Object.Destroy(obj);
            }
            else
            {
                Object.DestroyImmediate(obj);
            }
        }

        /// <summary>
        ///     Checks if the object is not null and not destroyed
        /// </summary>
        /// <param name="obj">Object to check</param>
        /// <returns>True if object is valid</returns>
        public static bool IsValid(this Object obj)
        {
            return obj != null && !obj.Equals(null);
        }

        /// <summary>
        ///     Gets the name of the object safely (handles null)
        /// </summary>
        /// <param name="obj">Object to get name from</param>
        /// <returns>Object name or empty string if null</returns>
        public static string GetName(this Object obj)
        {
            return obj != null ? obj.name : string.Empty;
        }

        /// <summary>
        ///     Sets the object's name if it exists
        /// </summary>
        /// <param name="obj">Object to rename</param>
        /// <param name="newName">New name to set</param>
        public static void SetName(this Object obj, string newName)
        {
            if (obj != null) obj.name = newName;
        }
    }
}