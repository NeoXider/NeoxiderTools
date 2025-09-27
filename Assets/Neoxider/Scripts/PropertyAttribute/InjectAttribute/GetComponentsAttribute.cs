using UnityEngine;

public class GetComponentsAttribute : PropertyAttribute
{
    public bool SearchInChildren;

    public GetComponentsAttribute(bool searchInChildren = false)
    {
        SearchInChildren = searchInChildren;
    }
}