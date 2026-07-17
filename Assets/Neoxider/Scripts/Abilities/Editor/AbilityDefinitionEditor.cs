using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

namespace Neo.Abilities.Editor
{
    /// <summary>
    ///     Branded UI Toolkit inspector for <see cref="AbilityDefinition" /> assets: gradient header
    ///     card, default fields, and a shortcut into the Ability Designer window.
    /// </summary>
    [CustomEditor(typeof(AbilityDefinition))]
    public sealed class AbilityDefinitionEditor : UnityEditor.Editor
    {
        public override VisualElement CreateInspectorGUI()
        {
            var root = new VisualElement();
            StyleSheet sheet = AbilityDesignerUI.LoadStyleSheet();
            if (sheet != null)
            {
                root.styleSheets.Add(sheet);
            }

            root.Add(AbilityDesignerUI.BuildInspectorHeader(serializedObject, false));

            var open = new Button(() => AbilityDesignerWindow.Open((AbilityDefinition)target))
            {
                text = "Open in Ability Designer"
            };
            open.AddToClassList("nad-open-btn");
            root.Add(open);

            root.Add(AbilityDesignerUI.BuildDefaultFields(serializedObject));
            return root;
        }
    }
}
