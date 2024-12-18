using UnityEngine;

public class GetComponentAttribute : PropertyAttribute
{
    public bool SearchInChildren = false;

    public GetComponentAttribute(bool searchInChildren = false)
    {
        SearchInChildren = searchInChildren;
    }
}