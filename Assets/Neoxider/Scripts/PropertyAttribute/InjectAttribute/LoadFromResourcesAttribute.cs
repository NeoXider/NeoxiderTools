using UnityEngine;

public class LoadFromResourcesAttribute : PropertyAttribute
{
    public string ResourcePath { get; }

    public LoadFromResourcesAttribute(string resourcePath = "")
    {
        ResourcePath = resourcePath;
    }
}