using UnityEngine;

public class GetComponentsAttribute : PropertyAttribute
{
    public bool SearchInChildren = false;

    public GetComponentsAttribute(bool searchInChildren = false)
    {
        SearchInChildren = searchInChildren;
    }
}