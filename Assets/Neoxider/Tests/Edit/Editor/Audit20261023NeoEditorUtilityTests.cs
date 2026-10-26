using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using Neo.Editor.Rpg;
using Neo.Rpg;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;
using Object = UnityEngine.Object;

namespace Neo.Editor.Tests
{
    public sealed class Audit20261023NeoEditorUtilityTests
    {
        [Serializable]
        private sealed class ReferenceSnapshot
        {
            public string[] references;
            public bool allowUnsafeCode;
        }

        [TestCase(false)]
        [TestCase(true)]
        public void AssemblyRepair_PreservesNamesAndCustomFieldsAndSkipsEditorAndTestDependencies(bool empty)
        {
            string json = empty
                ? "{\"references\":[],\"allowUnsafeCode\":true}"
                : "{\"references\" : [\"Neo.Core\"],\"allowUnsafeCode\":true}";
            Dictionary<string, string> candidates = new()
            {
                { "aaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaa", "Neo.Core" },
                { "bbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbb", "Neo.Editor.Tests" },
                { "cccccccccccccccccccccccccccccccc", "Neo.UI.Editor" },
                { "dddddddddddddddddddddddddddddddd", "Neo.NewRuntime" }
            };

            string updated = NeoEditorAsmdefFixer.AddRuntimeReferences(json, candidates, out int added);
            ReferenceSnapshot result = JsonUtility.FromJson<ReferenceSnapshot>(updated);

            Assert.That(added, Is.EqualTo(empty ? 2 : 1));
            Assert.That(result.allowUnsafeCode, Is.True);
            Assert.That(result.references, Does.Contain("GUID:dddddddddddddddddddddddddddddddd"));
            Assert.That(result.references, Does.Not.Contain("GUID:bbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbb"));
            Assert.That(result.references, Does.Not.Contain("GUID:cccccccccccccccccccccccccccccccc"));
            Assert.That(result.references, Does.Contain(empty ? "GUID:aaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaa" : "Neo.Core"));
            Assert.That(NeoEditorAsmdefFixer.AddRuntimeReferences(updated, candidates, out int secondAdded),
                Is.EqualTo(updated));
            Assert.That(secondAdded, Is.Zero);
        }

        [TestCase("123 Hero", "_123Hero")]
        [TestCase("!playerManager", "PlayerManager")]
        [TestCase("class", "Class")]
        public void SingletonTemplate_UsesValidClassNameInDeclarationAndBaseType(string input, string expected)
        {
            string template = SingletonCreator.BuildTemplate(input);

            Assert.That(template, Does.Contain("public class " + expected + " : Singleton<" + expected + ">"));
            Assert.That(template, Does.Contain("protected override void Init()"));
        }

        [TestCase(null)]
        [TestCase("")]
        [TestCase("!? ")]
        public void SingletonTemplate_EmptyOrCancelledNameProducesNoScript(string input)
        {
            Assert.That(SingletonCreator.BuildTemplate(input), Is.Null);
        }

        [Test]
        public void PowerGrowthDrawer_ReservesSpaceForBothBaseAndExponent()
        {
            RpgStatGrowthDefinition definition = ScriptableObject.CreateInstance<RpgStatGrowthDefinition>();
            try
            {
                using (SerializedObject serialized = new(definition))
                {
                    SerializedProperty rule = serialized.FindProperty("MaxHp");
                    rule.FindPropertyRelative("Mode").enumValueIndex = (int)RpgStatGrowthMode.Formula;
                    rule.FindPropertyRelative("FormulaType").enumValueIndex = (int)RpgStatFormulaType.Exponential;
                    RpgStatGrowthRuleDrawer drawer = new();
                    float exponentialHeight = drawer.GetPropertyHeight(rule, GUIContent.none);
                    rule.FindPropertyRelative("FormulaType").enumValueIndex = (int)RpgStatFormulaType.Power;

                    Assert.That(drawer.GetPropertyHeight(rule, GUIContent.none),
                        Is.EqualTo(exponentialHeight + EditorGUIUtility.singleLineHeight + 2f),
                        "Power exposes one additional control; otherwise it overlaps the next property.");
                }
            }
            finally
            {
                Object.DestroyImmediate(definition);
            }
        }

        [Test]
        public void ProjectZip_InsideAssets_ExcludesArchiveAndStagingFileAndIncludesProjectContent()
        {
            string folder = CreateTemporaryProject();
            try
            {
                string destination = Path.Combine(folder, "Assets", "Backup.zip");
                File.WriteAllText(destination, "previous backup");

                global::SaveProjectZip.CreateArchive(folder, destination);

                using (ZipArchive archive = ZipFile.OpenRead(destination))
                {
                    List<string> names = new();
                    foreach (ZipArchiveEntry entry in archive.Entries)
                    {
                        names.Add(entry.FullName);
                    }

                    Assert.That(names, Is.EquivalentTo(new[] { "Assets/Content.txt", "Packages/manifest.json" }));
                    using (StreamReader reader = new(archive.GetEntry("Assets/Content.txt").Open()))
                    {
                        Assert.That(reader.ReadToEnd(), Is.EqualTo("retained project content"));
                    }
                }

                Assert.That(Directory.GetFiles(Path.GetDirectoryName(destination), "*.tmp"), Is.Empty);
            }
            finally
            {
                Directory.Delete(folder, true);
            }
        }

        [Test]
        public void ProjectZip_ReadFailureKeepsPreviousBackupAndRemovesIncompleteArchive()
        {
            string folder = CreateTemporaryProject();
            try
            {
                string destination = Path.Combine(folder, "Backup.zip");
                File.WriteAllText(destination, "previous backup");
                using (FileStream locked = new(Path.Combine(folder, "Assets", "Content.txt"), FileMode.Open,
                           FileAccess.ReadWrite, FileShare.None))
                {
                    Assert.Throws<IOException>(() => global::SaveProjectZip.CreateArchive(folder, destination));
                }

                Assert.That(File.ReadAllText(destination), Is.EqualTo("previous backup"));
                Assert.That(Directory.GetFiles(folder, "*.tmp"), Is.Empty);
            }
            finally
            {
                Directory.Delete(folder, true);
            }
        }

        [Test]
        public void SpriteExport_DirectoryFailureReleasesOwnedCopyAndPreservesCallerTextureAndRenderTarget()
        {
            Assume.That(SystemInfo.graphicsDeviceType, Is.Not.EqualTo(GraphicsDeviceType.Null),
                "Readback requires a graphics device.");
            string folder = Path.Combine(Path.GetTempPath(), "Audit20261023NeoEditorSprite_" + Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(folder);
            string blocker = Path.Combine(folder, "FileInsteadOfDirectory");
            File.WriteAllText(blocker, "block directory creation");
            Texture2D source = new(7, 5, TextureFormat.RGBA32, false);
            source.Apply(false, true);
            HashSet<Texture2D> before = new(Resources.FindObjectsOfTypeAll<Texture2D>());
            RenderTexture previous = RenderTexture.active;
            RenderTexture marker = RenderTexture.GetTemporary(4, 4);
            try
            {
                RenderTexture.active = marker;

                Assert.Throws<IOException>(() => PrefabToSpriteUtility.CreateSpriteFromPreview(source,
                    Path.Combine(blocker, "Icon.png")));

                Assert.That(RenderTexture.active, Is.SameAs(marker));
                Assert.That(source != null, Is.True);
                Assert.That(source.isReadable, Is.False);
                foreach (Texture2D texture in Resources.FindObjectsOfTypeAll<Texture2D>())
                {
                    if (texture.width == 7 && texture.height == 5)
                    {
                        Assert.That(before.Contains(texture), Is.True, "A temporary readable texture leaked.");
                    }
                }
            }
            finally
            {
                RenderTexture.active = previous;
                RenderTexture.ReleaseTemporary(marker);
                foreach (Texture2D texture in Resources.FindObjectsOfTypeAll<Texture2D>())
                {
                    if (!before.Contains(texture) && texture.width == 7 && texture.height == 5)
                    {
                        Object.DestroyImmediate(texture);
                    }
                }

                Object.DestroyImmediate(source);
                Directory.Delete(folder, true);
            }
        }

        private static string CreateTemporaryProject()
        {
            string folder = Path.Combine(Path.GetTempPath(), "Audit20261023NeoEditorZip_" + Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(Path.Combine(folder, "Assets"));
            Directory.CreateDirectory(Path.Combine(folder, "Packages"));
            File.WriteAllText(Path.Combine(folder, "Assets", "Content.txt"), "retained project content");
            File.WriteAllText(Path.Combine(folder, "Packages", "manifest.json"), "{}");
            return folder;
        }
    }
}
