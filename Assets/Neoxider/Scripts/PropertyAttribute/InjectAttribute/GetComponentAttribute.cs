using UnityEngine;

public class GetComponentAttribute : PropertyAttribute
{
    public bool SearchInChildren;

    public GetComponentAttribute(bool searchInChildren = false)
    {
        SearchInChildren = searchInChildren;
    }
}