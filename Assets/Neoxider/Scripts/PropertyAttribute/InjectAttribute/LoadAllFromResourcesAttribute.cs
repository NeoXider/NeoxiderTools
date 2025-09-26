using UnityEngine;

public class LoadAllFromResourcesAttribute : PropertyAttribute
{
    public string ResourcePath = "";

    public LoadAllFromResourcesAttribute(string resourcePath = "")
    {
        ResourcePath = resourcePath;
    }
}