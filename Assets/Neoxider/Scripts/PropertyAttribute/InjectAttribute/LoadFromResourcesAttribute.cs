using UnityEngine;

/// <summary>
///     Marks a Unity Object field that the Neo inspector fills from a Resources path while the reference is missing;
///     without a match it falls back to the first already loaded object of the field type.
///     Editor-time assignment only: call <c>Resources.Load</c> explicitly for runtime loading.
/// </summary>
public class LoadFromResourcesAttribute : PropertyAttribute
{
    /// <param name="resourcePath">Path relative to a Resources folder, without the file extension.</param>
    public LoadFromResourcesAttribute(string resourcePath = "")
    {
        ResourcePath = resourcePath;
    }

    /// <summary>Path relative to a Resources folder, without the file extension.</summary>
    public string ResourcePath { get; }
}
