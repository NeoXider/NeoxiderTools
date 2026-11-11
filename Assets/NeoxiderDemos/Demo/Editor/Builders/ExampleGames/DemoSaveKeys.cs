using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace Neo.Demo.Editor.Builders
{
    /// <summary>
    ///     Demo-only SaveProvider keys for persisting components of sample scenes. With a library default key
    ///     ("Inventory_Default", "Money", "Shop", "BestScore"...) an imported sample reads and overwrites the save of
    ///     the user's own game. Keys use the "NeoDemo.&lt;Demo&gt;.&lt;Data&gt;" form; SampleSceneSaveKeyTests
    ///     (Neo.Editor.Tests) fails when a sample scene persists under a library default key. Any builder of this
    ///     assembly can call it.
    /// </summary>
    internal static class DemoSaveKeys
    {
        /// <summary>
        ///     Points the string field <paramref name="field" /> of <paramref name="component" /> at
        ///     <paramref name="demoKey" />. Works on prefab instances too (recorded as an override).
        /// </summary>
        /// <returns>True when the key changed.</returns>
        internal static bool Use(DemoSceneBuildContext ctx, Component component, string field, string demoKey)
        {
            using SerializedObject serialized = new(component);
            SerializedProperty property = serialized.FindProperty(field);
            if (property == null || property.propertyType != SerializedPropertyType.String)
            {
                throw new InvalidOperationException(component.GetType().Name + " on '" +
                                                    DemoSceneBuildContext.PathOf(component.transform) +
                                                    "' has no string save key field '" + field + "'.");
            }

            string previous = property.stringValue;
            if (previous == demoKey)
            {
                return false;
            }

            ctx.Set(component, field, demoKey);
            ctx.Note("Save key of " + component.GetType().Name + " on '" +
                     DemoSceneBuildContext.PathOf(component.transform) + "': '" + previous + "' -> '" + demoKey +
                     "' (demo-only key).");
            return true;
        }

        /// <summary>
        ///     <see cref="Use" /> for every hand-authored <typeparamref name="T" /> of the scene: one component gets
        ///     <paramref name="demoKey" />, several get "<paramref name="demoKey" />.1", ".2"... in hierarchy order.
        /// </summary>
        /// <returns>Number of components found.</returns>
        internal static int UseForAll<T>(DemoSceneBuildContext ctx, string field, string demoKey) where T : Component
        {
            List<T> components = ctx.FindAll<T>();
            for (int i = 0; i < components.Count; i++)
            {
                string key = components.Count == 1 ? demoKey : demoKey + "." + (i + 1);
                Use(ctx, components[i], field, key);
            }

            return components.Count;
        }
    }
}
