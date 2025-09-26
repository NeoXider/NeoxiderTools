using UnityEngine;

public class FindAllInSceneAttribute : PropertyAttribute
{
    public FindObjectsSortMode SortMode = FindObjectsSortMode.InstanceID;

    public FindAllInSceneAttribute(FindObjectsSortMode sottMode = FindObjectsSortMode.InstanceID)
    {
        SortMode = sottMode;
    }
}