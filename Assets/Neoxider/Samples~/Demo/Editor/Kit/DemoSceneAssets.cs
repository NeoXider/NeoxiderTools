using System;
using System.Collections.Generic;
using System.IO;
using TMPro;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Neo.Demo.Editor
{
    /// <summary>
    ///     Serializable art for authored demo scenes. Never uses runtime-generated textures: the kit sprites are
    ///     PNG assets in &lt;Demo root&gt;/Sprites/DemoKit (same shapes as the old procedural SurvivorArt), the
    ///     form controls use Unity's built-in UI skin sprites, and text uses the TMP default font asset
    ///     (LiberationSans SDF, what the existing demo scenes already reference).
    /// </summary>
    public static class DemoSceneAssets
    {
        public const string SpriteFolder = "Sprites/DemoKit";
        public const string RoundedRectFile = "DemoRoundedRect.png";
        public const string GradientFile = "DemoGradient.png";
        public const string DiscFile = "DemoDisc.png";
        public const string RingFile = "DemoRing.png";
        public const string GlowFile = "DemoGlow.png";
        public const string WhiteFile = "DemoWhite.png";

        private static readonly HashSet<string> Reported = new();

        /// <summary>9-sliced rounded rectangle (48 px, 14 px corners) for cards, buttons, tracks.</summary>
        public static Sprite RoundedRect => LoadKitSprite(RoundedRectFile) ?? BuiltinStandard;

        /// <summary>White vertical ramp (alpha = t squared, bottom transparent) for the backdrop.</summary>
        public static Sprite Gradient => LoadKitSprite(GradientFile);

        /// <summary>Soft-edged filled circle (128 px, 1 world unit).</summary>
        public static Sprite Disc => LoadKitSprite(DiscFile) ?? BuiltinKnob;

        /// <summary>Hollow ring (128 px, 1 world unit) for arenas and telegraphs.</summary>
        public static Sprite Ring => LoadKitSprite(RingFile) ?? BuiltinKnob;

        /// <summary>Radial glow (bright center fading out).</summary>
        public static Sprite Glow => LoadKitSprite(GlowFile) ?? BuiltinKnob;

        /// <summary>
        ///     Plain white square (8 px, 1 world unit, no border) for Filled bars and flat fills where the 9-sliced
        ///     rounded rect would stretch its corners, and for straight world bars.
        /// </summary>
        public static Sprite WhiteSquare => LoadKitSprite(WhiteFile) ?? BuiltinStandard;

        public static Sprite BuiltinStandard => Builtin("UI/Skin/UISprite.psd");
        public static Sprite BuiltinBackground => Builtin("UI/Skin/Background.psd");
        public static Sprite BuiltinInputField => Builtin("UI/Skin/InputFieldBackground.psd");
        public static Sprite BuiltinKnob => Builtin("UI/Skin/Knob.psd");
        public static Sprite BuiltinCheckmark => Builtin("UI/Skin/Checkmark.psd");
        public static Sprite BuiltinDropdownArrow => Builtin("UI/Skin/DropdownArrow.psd");
        public static Sprite BuiltinMask => Builtin("UI/Skin/UIMask.psd");

        /// <summary>TMP default font asset (TMP Settings), the font the existing demo scenes use.</summary>
        public static TMP_FontAsset Font
        {
            get
            {
                TMP_FontAsset font = TMP_Settings.defaultFontAsset;
                if (font == null)
                {
                    ReportOnce("TMP default font asset is missing (import TMP Essential Resources); texts keep their current font.");
                }

                return font;
            }
        }

        /// <summary>Built-in sprite set for TMP_DefaultControls (dropdown, input field).</summary>
        public static TMP_DefaultControls.Resources TmpResources()
        {
            return new TMP_DefaultControls.Resources
            {
                standard = BuiltinStandard,
                background = BuiltinBackground,
                inputField = BuiltinInputField,
                knob = BuiltinKnob,
                checkmark = BuiltinCheckmark,
                dropdown = BuiltinDropdownArrow,
                mask = BuiltinMask
            };
        }

        /// <summary>
        ///     Data asset of a demo (curves, definitions, configs) at &lt;Demo root&gt;/<paramref name="relativePath" />:
        ///     loaded when it exists, created otherwise, then passed to <paramref name="configure" /> (which must set
        ///     values, not append, so rebuilds stay idempotent). Use it instead of ScriptableObject.CreateInstance at
        ///     runtime: scene references to runtime instances are lost on save.
        /// </summary>
        /// <param name="relativePath">Path under the Demo root, e.g. "Data/Level/DemoLevelCurve.asset".</param>
        public static T EnsureAsset<T>(string relativePath, Action<T> configure = null) where T : ScriptableObject
        {
            string normalized = DemoSampleRoots.Normalize(relativePath);
            if (!normalized.EndsWith(".asset", StringComparison.OrdinalIgnoreCase))
            {
                throw new ArgumentException("Demo data assets need an .asset path, got '" + relativePath + "'.");
            }

            string path = DemoSampleRoots.Demo + "/" + normalized;
            T asset = AssetDatabase.LoadAssetAtPath<T>(path);
            if (asset == null)
            {
                if (AssetDatabase.LoadMainAssetAtPath(path) != null)
                {
                    throw new InvalidOperationException(path + " exists but is not a " + typeof(T).Name + ".");
                }

                EnsureFolder(Path.GetDirectoryName(path)?.Replace('\\', '/'));
                asset = ScriptableObject.CreateInstance<T>();
                AssetDatabase.CreateAsset(asset, path);
            }

            configure?.Invoke(asset);
            EditorUtility.SetDirty(asset);
            return asset;
        }

        /// <summary>
        ///     Non-ScriptableObject asset of a demo (RenderTexture, Material, AnimationClip...) at
        ///     &lt;Demo root&gt;/<paramref name="relativePath" />: loaded when it exists, otherwise made by
        ///     <paramref name="create" /> and saved there; then <paramref name="configure" /> sets its values (set, never
        ///     append, so rebuilds stay idempotent).
        /// </summary>
        public static T EnsureAsset<T>(string relativePath, Func<T> create, Action<T> configure = null) where T : Object
        {
            if (create == null)
            {
                throw new ArgumentNullException(nameof(create));
            }

            if (typeof(GameObject).IsAssignableFrom(typeof(T)))
            {
                throw new ArgumentException("Use DemoSceneAssets.EnsurePrefab for prefabs ('" + relativePath + "').");
            }

            string path = DemoAssetPath(relativePath);
            T asset = AssetDatabase.LoadAssetAtPath<T>(path);
            if (asset == null)
            {
                if (AssetDatabase.LoadMainAssetAtPath(path) != null)
                {
                    throw new InvalidOperationException(path + " exists but is not a " + typeof(T).Name + ".");
                }

                EnsureFolder(Path.GetDirectoryName(path)?.Replace('\\', '/'));
                asset = create();
                if (asset == null)
                {
                    throw new InvalidOperationException("EnsureAsset: the factory for " + path + " returned null.");
                }

                AssetDatabase.CreateAsset(asset, path);
            }

            configure?.Invoke(asset);
            EditorUtility.SetDirty(asset);
            return asset;
        }

        /// <summary>
        ///     Render texture asset of a demo (a camera target shown by a RawImage): ARGB32, no MSAA or mips, bilinear,
        ///     clamped; created once ("*.renderTexture") and resized when <paramref name="width" /> x
        ///     <paramref name="height" /> changed.
        /// </summary>
        public static RenderTexture EnsureRenderTexture(string relativePath, int width, int height, int depth = 16)
        {
            return EnsureAsset(relativePath, () => new RenderTexture(width, height, depth, RenderTextureFormat.ARGB32)
            {
                name = Path.GetFileNameWithoutExtension(relativePath),
                antiAliasing = 1,
                useMipMap = false,
                filterMode = FilterMode.Bilinear,
                wrapMode = TextureWrapMode.Clamp
            }, texture =>
            {
                if (texture.width == width && texture.height == height)
                {
                    return;
                }

                texture.Release();
                texture.width = width;
                texture.height = height;
            });
        }

        /// <summary>
        ///     Prefab asset of a demo at &lt;Demo root&gt;/<paramref name="relativePath" /> (".prefab"): created once from a
        ///     temporary object that <paramref name="build" /> fills; an existing prefab is returned as is, so hand
        ///     edits survive rebuilds. Use it when a component needs a real prefab (pooled spawns), not a scene template.
        /// </summary>
        /// <param name="ui">Give the temporary root a RectTransform and the UI layer.</param>
        public static GameObject EnsurePrefab(string relativePath, Action<GameObject> build, bool ui = false)
        {
            if (build == null)
            {
                throw new ArgumentNullException(nameof(build));
            }

            string path = DemoAssetPath(relativePath);
            if (!path.EndsWith(".prefab", StringComparison.OrdinalIgnoreCase))
            {
                throw new ArgumentException("Demo prefabs need a .prefab path, got '" + relativePath + "'.");
            }

            GameObject existing = AssetDatabase.LoadAssetAtPath<GameObject>(path);
            if (existing != null)
            {
                return existing;
            }

            if (AssetDatabase.LoadMainAssetAtPath(path) != null)
            {
                throw new InvalidOperationException(path + " exists but is not a prefab.");
            }

            EnsureFolder(Path.GetDirectoryName(path)?.Replace('\\', '/'));
            string name = Path.GetFileNameWithoutExtension(path);
            GameObject temp = ui ? new GameObject(name, typeof(RectTransform)) : new GameObject(name);
            try
            {
                if (ui)
                {
                    temp.layer = 5;
                }

                build(temp);
                GameObject saved = PrefabUtility.SaveAsPrefabAsset(temp, path, out bool success);
                if (!success || saved == null)
                {
                    throw new InvalidOperationException("Could not save the demo prefab " + path + ".");
                }

                return saved;
            }
            finally
            {
                Object.DestroyImmediate(temp);
            }
        }

        /// <summary>
        ///     Ready asset of the active Demo sample (AudioClip, Sprite, Material, prefab, data) at
        ///     &lt;Demo root&gt;/<paramref name="relativePath" />; throws naming the path and type when it is missing.
        /// </summary>
        public static T LoadDemoAsset<T>(string relativePath) where T : Object
        {
            string path = DemoAssetPath(relativePath);
            T asset = AssetDatabase.LoadAssetAtPath<T>(path);
            if (asset == null)
            {
                throw new InvalidOperationException("Demo asset missing or not a " + typeof(T).Name + ": " + path + ".");
            }

            return asset;
        }

        /// <summary>Creates every missing folder of an asset path ("Assets/A/B").</summary>
        public static void EnsureFolder(string folder)
        {
            if (string.IsNullOrEmpty(folder) || AssetDatabase.IsValidFolder(folder))
            {
                return;
            }

            string parent = Path.GetDirectoryName(folder)?.Replace('\\', '/');
            EnsureFolder(parent);
            AssetDatabase.CreateFolder(parent, Path.GetFileName(folder));
        }

        /// <summary>Loads a sprite from the kit folder of the active Demo sample, or null (reported once).</summary>
        public static Sprite LoadKitSprite(string fileName)
        {
            if (!DemoSampleRoots.TryResolveDemoRoot(out string root, out string error))
            {
                ReportOnce(error);
                return null;
            }

            string path = root + "/" + SpriteFolder + "/" + fileName;
            Sprite sprite = AssetDatabase.LoadAssetAtPath<Sprite>(path);
            if (sprite == null)
            {
                ReportOnce("Demo kit sprite missing or not imported as a Sprite: " + path + ". Falling back to built-in UI art.");
            }

            return sprite;
        }

        private static string DemoAssetPath(string relativePath)
        {
            string normalized = DemoSampleRoots.Normalize(relativePath).TrimStart('/');
            if (normalized.Length == 0)
            {
                throw new ArgumentException("Demo asset paths must not be empty.", nameof(relativePath));
            }

            return DemoSampleRoots.Demo + "/" + normalized;
        }

        private static Sprite Builtin(string path)
        {
            Sprite sprite = AssetDatabase.GetBuiltinExtraResource<Sprite>(path);
            if (sprite == null)
            {
                ReportOnce("Built-in UI sprite not found: " + path + ".");
            }

            return sprite;
        }

        private static void ReportOnce(string message)
        {
            if (Reported.Add(message))
            {
                Debug.LogWarning(DemoSceneRunner.LogPrefix + message);
            }
        }
    }
}
