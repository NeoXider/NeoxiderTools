using UnityEngine;

/// <summary>
///     Marks a Component array or list that the Neo inspector fills from the owning GameObject (and optionally its
///     children) while the collection is empty. Editor-time assignment only: the attribute does nothing in Awake.
/// </summary>
public class GetComponentsAttribute : PropertyAttribute
{
    /// <summary>When true, the lookup also searches child objects.</summary>
    public bool SearchInChildren;

    /// <param name="searchInChildren">Also search child objects.</param>
    public GetComponentsAttribute(bool searchInChildren = false)
    {
        SearchInChildren = searchInChildren;
    }
}
