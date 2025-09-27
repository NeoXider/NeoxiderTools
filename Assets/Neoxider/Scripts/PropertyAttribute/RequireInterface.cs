using System;
using UnityEngine;

namespace Neo
{
    /// <summary>
    ///     Attribute that requires a Unity Object reference to implement a specific interface.
    ///     Can be used with both GameObjects (checking their components) and ScriptableObjects.
    /// </summary>
    /// <example>
    ///     <code>
    /// public interface IMoneyAdd { }
    /// 
    /// public class MyComponent : MonoBehaviour
    /// {
    ///     [RequireInterface(typeof(IMoneyAdd))]
    ///     public GameObject objectWithInterface;
    ///     
    ///     [FindInScene, RequireInterface(typeof(IMoneyAdd))]
    ///     public GameObject moneyWithFind;
    /// }
    /// </code>
    /// </example>
    [AttributeUsage(AttributeTargets.Field, AllowMultiple = true)]
    public class RequireInterface : PropertyAttribute
    {
        /// <summary>
        ///     The interface type that the referenced object must implement
        /// </summary>
        public readonly Type RequireType;

        /// <summary>
        ///     Creates a new RequireInterface attribute
        /// </summary>
        /// <param name="requireType">The interface type that the referenced object must implement</param>
        /// <exception cref="ArgumentException">Thrown when the specified type is not an interface</exception>
        public RequireInterface(Type requireType)
        {
            if (!requireType.IsInterface)
                throw new ArgumentException($"Type {requireType.Name} is not an interface", nameof(requireType));

            RequireType = requireType;
        }
    }
}