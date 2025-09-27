using UnityEngine;

public class LoadFromResourcesAttribute : PropertyAttribute
{
    public LoadFromResourcesAttribute(string resourcePath = "")
    {
        ResourcePath = resourcePath;
    }

    public string ResourcePath { get; }
}