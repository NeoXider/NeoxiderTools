using UnityEngine;

namespace Neo
{
    /// <summary>
    ///     Attribute that automatically finds and assigns a component of the specified type from the scene.
    ///     Use this attribute to automatically inject scene dependencies into your components.
    /// </summary>
    /// <example>
    ///     <code>
    /// public class MyComponent : MonoBehaviour
    /// {
    ///     [FindInScene] // Will find the first AudioSource in the scene
    ///     private AudioSource audioSource;
    ///     
    ///     [FindInScene] // Will find the first instance of your custom component
    ///     private MyOtherComponent dependency;
    /// }
    /// </code>
    /// </example>
    public class FindInSceneAttribute : PropertyAttribute
    {
        // This attribute's behavior is implemented in CustomAttributeDrawer
    }
}