using System;
using System.Collections.Generic;
using System.Text;
using TMPro;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Neo.Demo.Editor.Builders
{
    /// <summary>
    ///     Shared steps of the Shop and NPC demo builders on top of the kit: lookup of hand-authored objects with a
    ///     message that lists what the scene has instead, the compact rows of the control cards and small helpers.
    ///     Step reporting, listener ownership (ctx.OwnListeners), asset loading and log rows come from the kit.
    /// </summary>
    internal static class ShopNpcBuildSupport
    {

        /// <summary>The first hand-authored <typeparamref name="T" />; throws naming the scene part that is missing.</summary>
        public static T RequireHandAuthored<T>(DemoSceneBuildContext ctx, string role) where T : Component
        {
            List<T> all = ctx.FindAll<T>();
            if (all.Count == 0)
            {
                throw new InvalidOperationException("the scene has no hand-authored " + typeof(T).Name + " (" + role +
                                                    "). Restore the scene from version control instead of rebuilding it.");
            }

            if (all.Count > 1)
            {
                ctx.Note("Several " + typeof(T).Name + " in the scene; the demo uses '" +
                         DemoSceneBuildContext.PathOf(all[0].transform) + "'.");
            }

            return all[0];
        }

        /// <summary>
        ///     Hand-authored <typeparamref name="T" /> on an object named one of <paramref name="names" /> (priority order;
        ///     later names are legacy names). Throws listing the candidates the scene has.
        /// </summary>
        public static T RequireNamed<T>(DemoSceneBuildContext ctx, params string[] names) where T : Component
        {
            List<T> all = ctx.FindAll<T>();
            foreach (string name in names)
            {
                foreach (T candidate in all)
                {
                    if (candidate.name == name)
                    {
                        return candidate;
                    }
                }
            }

            StringBuilder present = new();
            foreach (T candidate in all)
            {
                present.Append(present.Length == 0 ? string.Empty : ", ")
                    .Append(DemoSceneBuildContext.PathOf(candidate.transform));
            }

            throw new InvalidOperationException("expected a hand-authored " + typeof(T).Name + " named '" +
                                                string.Join("' or '", names) + "', the scene has " +
                                                (all.Count == 0 ? "none" : present.ToString()) +
                                                ". Restore the scene from version control instead of rebuilding it.");
        }

        /// <summary>Destroys the collider a primitive came with (demo markers and props must not block or push agents).</summary>
        public static void RemoveCollider(GameObject target)
        {
            Collider collider = target.GetComponent<Collider>();
            if (collider != null)
            {
                Object.DestroyImmediate(collider);
            }
        }

        /// <summary><see cref="DemoPanel.ValueLabel" /> with smaller fonts for long live values; returns the value label.</summary>
        public static TMP_Text CompactValue(DemoPanel panel, string caption, string initial)
        {
            return panel.CompactValue(caption, initial, 0.42f, 17f, 18f);
        }

        /// <summary>Rich-text color tag around <paramref name="text" />.</summary>
        public static string Colored(string text, Color color)
        {
            return "<color=#" + ColorUtility.ToHtmlStringRGB(color) + ">" + text + "</color>";
        }
    }
}
