using System;
using System.Collections.Generic;
using Neo.Abilities;
using Neo.NoCode;
using Neo.Samples;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;
using Object = UnityEngine.Object;

namespace Neo.Demo.Editor.Builders
{
    /// <summary>
    ///     Shared steps of the Abilities group scene builders (AbilitiesNoCodeDemo, SurvivorDemo, VampireSurvivorMCP) on
    ///     top of the kit: hand-authored lookups and a few HUD factories. Step reporting (DemoSceneKit.Step), asset
    ///     loading (DemoSceneAssets.LoadDemoAsset) and the input-module upgrade (DemoSceneKit.EnsureEventSystem) live
    ///     in the kit.
    /// </summary>
    internal static class AbilitiesDemoBuild
    {
        /// <summary>First hand-authored component of type <typeparamref name="T" /> on an object named <paramref name="objectName" />.</summary>
        public static T FindNamed<T>(DemoSceneBuildContext ctx, string objectName) where T : Component
        {
            foreach (T component in ctx.FindAll<T>())
            {
                if (component.gameObject.name == objectName)
                {
                    return component;
                }
            }

            return null;
        }

        /// <summary>Recolors the labels of buttons made by DemoPanel.ButtonRow to the light text color.</summary>
        public static void LightLabels(params Button[] buttons)
        {
            foreach (Button button in buttons)
            {
                TMP_Text text = button != null ? button.GetComponentInChildren<TMP_Text>(true) : null;
                if (text != null)
                {
                    text.color = DemoPalette.Text;
                }
            }
        }

        /// <summary>Gives a button its own fixed size inside a layout group.</summary>
        public static void Size(DemoSceneBuildContext ctx, Component target, float width, float height)
        {
            LayoutElement element = ctx.Ensure<LayoutElement>(target.gameObject);
            element.minWidth = width;
            element.preferredWidth = width;
            element.minHeight = height;
            element.preferredHeight = height;
            element.flexibleWidth = 0f;
            element.flexibleHeight = 0f;
        }

        /// <summary>
        ///     Read-only progress row driven by the NoCode <see cref="SetProgress" />: a non-interactable kit slider whose
        ///     value follows <paramref name="memberName" /> of <paramref name="componentTypeName" /> on <paramref name="source" />.
        /// </summary>
        public static DemoSliderRow ProgressRow(DemoSceneBuildContext ctx, DemoPanel panel, string label, Color color,
            GameObject source, string componentTypeName, string memberName, float initial, float pollSeconds)
        {
            DemoSliderRow row = panel.SliderRow(label, 0f, 1f, initial, false, "0%");
            Slider slider = row.Slider;
            slider.interactable = false;
            slider.transition = Selectable.Transition.None;
            if (slider.handleRect != null && slider.handleRect.TryGetComponent(out Image handle))
            {
                handle.enabled = false;
            }

            if (slider.fillRect != null && slider.fillRect.TryGetComponent(out Image fill))
            {
                fill.color = color;
            }

            SetProgress progress = ctx.Ensure<SetProgress>(slider.gameObject);
            ctx.Set(progress, "_binding._useSceneSearch", false);
            ctx.Set(progress, "_binding._sourceRoot", source);
            ctx.Set(progress, "_binding._componentTypeName", componentTypeName);
            ctx.Set(progress, "_binding._memberName", memberName);
            ctx.Set(progress, "_updateMode", (int)NoCodeFloatUpdateMode.Poll);
            ctx.Set(progress, "_pollIntervalSeconds", pollSeconds);
            ctx.Set(progress, "_slider", slider);
            ctx.Set(progress, "_image", null);
            ctx.Set(progress, "_minValue", 0f);
            ctx.Set(progress, "_maxValue", 1f);
            return row;
        }

        /// <summary>Replaces the hub's archetype bindings (id → template) through its serialized list.</summary>
        public static void SetArchetypes(DemoSceneBuildContext ctx, AbilitySystemBehaviour hub,
            IReadOnlyList<KeyValuePair<string, GameObject>> archetypes)
        {
            SerializedObject serialized = new(hub);
            SerializedProperty list = serialized.FindProperty("_archetypes");
            if (list == null || !list.isArray)
            {
                throw new InvalidOperationException("AbilitySystemBehaviour has no serialized '_archetypes' list.");
            }

            list.arraySize = archetypes.Count;
            for (int i = 0; i < archetypes.Count; i++)
            {
                SerializedProperty entry = list.GetArrayElementAtIndex(i);
                entry.FindPropertyRelative(nameof(SpawnArchetypeEntry.Id)).stringValue = archetypes[i].Key;
                entry.FindPropertyRelative(nameof(SpawnArchetypeEntry.Prefab)).objectReferenceValue = archetypes[i].Value;
            }

            serialized.ApplyModifiedPropertiesWithoutUndo();
            ctx.MarkModified(hub);
        }

        /// <summary>Reads a serialized object reference without exposing it on the component's API.</summary>
        public static T ReadReference<T>(Object owner, string field) where T : Object
        {
            SerializedObject serialized = new(owner);
            SerializedProperty property = serialized.FindProperty(field);
            return property != null && property.propertyType == SerializedPropertyType.ObjectReference
                ? property.objectReferenceValue as T
                : null;
        }

        /// <summary>Sets anchors, pivot, position and size of a rect in one call.</summary>
        public static RectTransform Place(RectTransform rect, Vector2 anchorMin, Vector2 anchorMax, Vector2 pivot,
            Vector2 anchoredPosition, Vector2 sizeDelta)
        {
            rect.anchorMin = anchorMin;
            rect.anchorMax = anchorMax;
            rect.pivot = pivot;
            rect.anchoredPosition = anchoredPosition;
            rect.sizeDelta = sizeDelta;
            return rect;
        }
    }
}
