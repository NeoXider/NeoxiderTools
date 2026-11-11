using UnityEngine;

/// <summary>
///     Marks a Component field that the Neo inspector fills from the owning GameObject (and optionally its children)
///     while the reference is missing. Editor-time assignment only: the attribute does nothing in Awake.
/// </summary>
public class GetComponentAttribute : PropertyAttribute
{
    /// <summary>When true, the lookup also searches child objects.</summary>
    public bool SearchInChildren;

    /// <param name="searchInChildren">Also search child objects.</param>
    public GetComponentAttribute(bool searchInChildren = false)
    {
        SearchInChildren = searchInChildren;
    }
}
