using UnityEngine;

/// <summary>
///     Marks an array or list that the Neo inspector fills with every asset of the element type under a Resources
///     path while the collection is empty. Editor-time assignment only.
/// </summary>
public class LoadAllFromResourcesAttribute : PropertyAttribute
{
    /// <summary>Folder relative to a Resources folder; empty searches only objects that are already loaded.</summary>
    public string ResourcePath = "";

    /// <param name="resourcePath">Folder relative to a Resources folder; empty searches only already loaded objects.</param>
    public LoadAllFromResourcesAttribute(string resourcePath = "")
    {
        ResourcePath = resourcePath;
    }
}
