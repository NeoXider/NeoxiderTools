using System;
using System.Reflection;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;
using Object = UnityEngine.Object;

namespace Neo.Demo.Editor.Builders
{
    /// <summary>
    ///     Helpers shared by the Tools demo builders (Random, Dialogue) on top of the kit: shape checks before
    ///     <see cref="DemoSceneBuildContext.Retire" />, bar markers and rich-text colors. Retiring, listener ownership,
    ///     step reporting and compact rows live in the kit (ctx.Retire, ctx.OwnListeners, DemoSceneKit.Step,
    ///     DemoPanel.ValueRow).
    /// </summary>
    internal static class ToolsDemoBuildUtility
    {
        private const BindingFlags InstanceFields = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic |
                                                    BindingFlags.DeclaredOnly;

        /// <summary>True when a Button under <paramref name="root" /> has a persistent call targeting a <typeparamref name="T" />.</summary>
        public static bool HasButtonCalling<T>(GameObject root) where T : Object
        {
            foreach (Button button in root.GetComponentsInChildren<Button>(true))
            {
                for (int i = 0; i < button.onClick.GetPersistentEventCount(); i++)
                {
                    if (button.onClick.GetPersistentTarget(i) is T)
                    {
                        return true;
                    }
                }
            }

            return false;
        }

        /// <summary>
        ///     Number of persistent listeners on the components of <paramref name="host" /> that still have a live
        ///     target. Zero means the object only served objects that no longer exist.
        /// </summary>
        public static int CountLiveListeners(GameObject host)
        {
            int live = 0;
            foreach (MonoBehaviour behaviour in host.GetComponents<MonoBehaviour>())
            {
                if (behaviour == null)
                {
                    continue;
                }

                for (Type type = behaviour.GetType(); type != null && type != typeof(MonoBehaviour); type = type.BaseType)
                {
                    foreach (FieldInfo field in type.GetFields(InstanceFields))
                    {
                        if (!typeof(UnityEventBase).IsAssignableFrom(field.FieldType) ||
                            !(field.GetValue(behaviour) is UnityEventBase unityEvent))
                        {
                            continue;
                        }

                        for (int i = 0; i < unityEvent.GetPersistentEventCount(); i++)
                        {
                            if (unityEvent.GetPersistentTarget(i) != null)
                            {
                                live++;
                            }
                        }
                    }
                }
            }

            return live;
        }

        /// <summary>Thin marker on a <see cref="DemoPanel.Bar" /> track at <paramref name="position" /> (0..1).</summary>
        public static RectTransform BarMarker(DemoSceneBuildContext ctx, Image fill, string name, float position, Color color)
        {
            Image marker = DemoSceneKit.Image(ctx, fill.transform.parent, name, color, false);
            RectTransform rect = marker.rectTransform;
            float at = Mathf.Clamp01(position);
            rect.anchorMin = new Vector2(at, 0f);
            rect.anchorMax = new Vector2(at, 1f);
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = Vector2.zero;
            rect.sizeDelta = new Vector2(3f, 8f);
            return rect;
        }

        /// <summary>Hex "#RRGGBB" of a color for rich text.</summary>
        public static string Hex(Color color)
        {
            return "#" + ColorUtility.ToHtmlStringRGB(color);
        }
    }
}
