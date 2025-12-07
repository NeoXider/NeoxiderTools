using UnityEditor;
using UnityEngine;

namespace Flexalon.Editor
{
    [CustomEditor(typeof(FlexalonResult))]
    public class FlexalonResultEditor : UnityEditor.Editor
    {
        void OnEnable()
        {
            foreach (var target in targets)
            {
                target.hideFlags = HideFlags.HideInInspector;
            }
        }
    }
}