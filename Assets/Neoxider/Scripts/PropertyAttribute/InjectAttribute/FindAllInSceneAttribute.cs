using UnityEngine;

/// <summary>
///     Marks a Component or GameObject array/list that the Neo inspector fills with every matching scene object while
///     the collection is empty. Editor-time assignment only: use an explicit scene query for runtime discovery.
/// </summary>
public class FindAllInSceneAttribute : PropertyAttribute
{
    /// <summary>Order passed to the scene query.</summary>
    public FindObjectsSortMode SortMode = FindObjectsSortMode.InstanceID;

    /// <param name="sottMode">Order passed to the scene query (the parameter keeps its historical spelling).</param>
    public FindAllInSceneAttribute(FindObjectsSortMode sottMode = FindObjectsSortMode.InstanceID)
    {
        SortMode = sottMode;
    }
}
