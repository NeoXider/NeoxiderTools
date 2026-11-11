using System;
using System.Collections.Generic;
using Neo.Samples;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;
using Object = UnityEngine.Object;

namespace Neo.Demo.Editor.Builders
{
    /// <summary>
    ///     Shared steps of the guides for the owner's hand-authored NoCode example games (adventure, ClickerExample,
    ///     Shooter2D). The games stay exactly as authored: a builder only adds the module info card on its own overlay
    ///     canvas, replaces the legacy StandaloneInputModule of the hand-authored EventSystem and points persisting
    ///     components at demo-only save keys (<see cref="DemoSaveKeys" />). Hints are added only for objects that
    ///     still exist, so the card never names an object the author removed.
    /// </summary>
    internal static class ExampleGameGuide
    {
        /// <summary>Generated root key of the info card canvas ("[Demo] Info Card").</summary>
        internal const string InfoRootKey = "Info Card";

        /// <summary>Width of the info card in the 1920x1080 reference canvas.</summary>
        internal const float CardWidth = 440f;

        /// <summary>
        ///     Ensures the scene's <see cref="ModuleDemoSceneInfo" /> and authors its card in the top-left corner on
        ///     "[Demo] Info Card"; the hand-authored EventSystem gets an Input System UI module.
        /// </summary>
        internal static ModuleDemoSceneInfo AuthorInfoCard(DemoSceneBuildContext ctx, string sceneName,
            DemoInfoContent content, Color accent)
        {
            ModuleDemoSceneInfo info = DemoSceneKit.Step(sceneName, "ensure the ModuleDemoSceneInfo service",
                () => ctx.FindOrCreate<ModuleDemoSceneInfo>("Services", "Demo Info"));
            DemoSceneKit.Step(sceneName, "give sprite-less filled bars a sprite", () => FixFilledImages(ctx));
            // WHY: the hand-authored EventSystem keeps StandaloneInputModule, which throws in Input-System-only projects.
            return DemoSceneKit.Step(sceneName, "author the info card and upgrade the input module",
                () => DemoSceneKit.InfoCardCanvas(ctx, info, content, InfoRootKey, accent, true));
        }

        /// <summary>
        ///     A Filled Image without a sprite draws a full quad whatever its fill amount, so a hand-authored health bar
        ///     never shrinks; such images get the kit's white square.
        /// </summary>
        internal static void FixFilledImages(DemoSceneBuildContext ctx)
        {
            Sprite white = DemoSceneAssets.WhiteSquare;
            foreach (Image image in ctx.FindAll<Image>())
            {
                if (image.type != Image.Type.Filled || image.sprite != null)
                {
                    continue;
                }

                image.sprite = white;
                if (PrefabUtility.IsPartOfPrefabInstance(image))
                {
                    PrefabUtility.RecordPrefabInstancePropertyModifications(image);
                }

                ctx.MarkModified(image);
                ctx.Note("Filled Image '" + DemoSceneBuildContext.PathOf(image.transform) +
                         "' had no sprite (its fill never showed); assigned the kit white square.");
            }
        }

        /// <summary>
        ///     First hand-authored <typeparamref name="T" /> (hierarchy order); throws when the game lacks it, because
        ///     these builders decorate the hand-authored game and never rebuild it.
        /// </summary>
        internal static T Require<T>(DemoSceneBuildContext ctx, string sceneName, string role) where T : Component
        {
            List<T> found = ctx.FindAll<T>();
            if (found.Count == 0)
            {
                throw new InvalidOperationException(sceneName + ": no hand-authored " + typeof(T).Name + " (" + role +
                                                    "). This builder only decorates the hand-authored game; restore " +
                                                    "the scene from version control instead of rebuilding it.");
            }

            return found[0];
        }

        /// <summary>
        ///     First hand-authored <typeparamref name="T" /> (hierarchy order) or null. Unlike ctx.Find it adds no
        ///     "several found" note, since the games hold several pickups, spawners and so on by design.
        /// </summary>
        internal static T First<T>(DemoSceneBuildContext ctx) where T : Component
        {
            List<T> found = ctx.FindAll<T>();
            return found.Count > 0 ? found[0] : null;
        }

        /// <summary>First hand-authored <typeparamref name="T" /> on an object named <paramref name="name" /> (trimmed), or null.</summary>
        internal static T Named<T>(DemoSceneBuildContext ctx, string name) where T : Component
        {
            foreach (T component in ctx.FindAll<T>())
            {
                if (component.gameObject.name.Trim() == name)
                {
                    return component;
                }
            }

            return null;
        }

        /// <summary>Trimmed object name (some hand-authored names carry a trailing space).</summary>
        internal static string NameOf(Component component)
        {
            return component.gameObject.name.Trim();
        }

        /// <summary>
        ///     Adds <paramref name="text" /> when <paramref name="subject" /> exists; otherwise warns, so a card that
        ///     lost a hint after the author changed the game is noticed in the build summary.
        /// </summary>
        internal static void Hint(DemoSceneBuildContext ctx, List<string> hints, Object subject, string what,
            Func<string> text)
        {
            if (subject != null)
            {
                hints.Add(text());
                return;
            }

            ctx.Warn("Info card hint skipped: " + what + " is missing from the hand-authored game.");
        }
    }
}
