using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;
using NUnit.Framework;

namespace Neo.Editor.Tests
{
    public class ModulePrinciplesTests
    {
        private static readonly string RuntimeRoot = "Assets/Neoxider/Scripts";
        private static readonly string PackageRoot = "Assets/Neoxider";

        private static readonly string[] TextFileExtensions =
        {
            ".asmdef",
            ".asmref",
            ".cs",
            ".json",
            ".md",
            ".txt",
            ".uss",
            ".uxml"
        };

        private static readonly string[] MojibakeMarkers =
        {
            "\u0432\u0402",
            "\u0432\u2020",
            "\u0432\u2030",
            "\u0432\u20AC",
            "\u0432\u201D",
            "\u0412\u00AB",
            "\u0412\u00BB",
            "\u0412\u0406",
            "\u0413\u2014",
            "\u043F\u0457\u0405",
            "\u0420\u00B0",
            "\u0420\u00B5",
            "\u0420\u0451",
            "\u0420\u0455",
            "\u0420\u0405",
            "\u0420\u00BB",
            "\u0420\u0491",
            "\u0421\u0403",
            "\u0421\u201A",
            "\u0421\u040A",
            "\u0421\u2039"
        };

        private static readonly HashSet<string> RawDebugLogGateways = new()
        {
            "Assets/Neoxider/Scripts/Extensions/NeoDiagnostics.cs",
            "Assets/Neoxider/Scripts/Network/Core/NeoNetworkState.cs",
            "Assets/Neoxider/Scripts/PropertyAttribute/RequireInterfaceDrawer.cs",
            "Assets/Neoxider/Scripts/Save/SaveProvider.cs",
            "Assets/Neoxider/Scripts/StateMachine/StateMachine.cs"
        };

        [Test]
        public void RuntimeModules_DoNotUseRawDebugLogsOutsideDiagnosticGateways()
        {
            List<string> offenders = new();

            foreach (string file in Directory.GetFiles(RuntimeRoot, "*.cs", SearchOption.AllDirectories))
            {
                string normalized = file.Replace('\\', '/');
                if (RawDebugLogGateways.Contains(normalized))
                {
                    continue;
                }

                string text = File.ReadAllText(file);
                if (text.Contains("Debug.Log"))
                {
                    offenders.Add(normalized);
                }
            }

            Assert.That(offenders, Is.Empty,
                "Runtime package logs must go through NeoDiagnostics or an explicit debug-only wrapper.");
        }

        [Test]
        public void PackageTextFiles_DoNotContainKnownMojibakeMarkers()
        {
            List<string> offenders = new();

            foreach (string file in Directory.GetFiles(PackageRoot, "*.*", SearchOption.AllDirectories))
            {
                if (!IsTrackedTextExtension(Path.GetExtension(file)))
                {
                    continue;
                }

                string text = File.ReadAllText(file);
                foreach (string marker in MojibakeMarkers)
                {
                    if (text.Contains(marker))
                    {
                        offenders.Add($"{file.Replace('\\', '/')} contains '{marker}'");
                        break;
                    }
                }
            }

            Assert.That(offenders, Is.Empty, "Package text files must stay valid UTF-8 without mojibake.");
        }

        [Test]
        public void RuntimeModules_DoNotImportUnityEditorOutsideEditorGuards()
        {
            List<string> offenders = new();

            foreach (string file in Directory.GetFiles(RuntimeRoot, "*.cs", SearchOption.AllDirectories))
            {
                string normalized = file.Replace('\\', '/');
                if (normalized.Contains("/Editor/"))
                {
                    continue;
                }

                string[] lines = File.ReadAllLines(file);
                int editorGuardDepth = 0;

                for (int i = 0; i < lines.Length; i++)
                {
                    string trimmed = lines[i].Trim();
                    if (trimmed.StartsWith("#if") && trimmed.Contains("UNITY_EDITOR"))
                    {
                        editorGuardDepth++;
                    }
                    else if (trimmed.StartsWith("#endif") && editorGuardDepth > 0)
                    {
                        editorGuardDepth--;
                    }

                    if (editorGuardDepth == 0 &&
                        (trimmed.Contains("using UnityEditor") || trimmed.Contains("UnityEditor.")))
                    {
                        offenders.Add($"{normalized}:{i + 1}: {trimmed}");
                    }
                }
            }

            Assert.That(offenders, Is.Empty,
                "Runtime modules must guard UnityEditor API behind #if UNITY_EDITOR or move code to an Editor asmdef.");
        }

        // ─── TECH DEBT: Public camelCase member allowlist ──────────────────────────────
        // The files below pre-date this naming-convention check and have public
        // camelCase fields / properties that violate the PascalCase rule.
        // Do NOT add new entries here — fix the violation instead.
        // [Obsolete]-marked members are excluded at scan time (back-compat forwarders).
        // Tracked issue: migrate these to PascalCase + [Obsolete] forwarders.
        private static readonly HashSet<string> CamelCasePascalViolationAllowedFiles = new()
        {
            // Audio
            "Assets/Neoxider/Scripts/Audio/AMSettings.cs",
            "Assets/Neoxider/Scripts/Audio/AudioSimple/AM.cs",
            "Assets/Neoxider/Scripts/Audio/SettingMixer.cs",
            // Animations
            "Assets/Neoxider/Scripts/Animations/ColorAnimator.cs",
            "Assets/Neoxider/Scripts/Animations/FloatAnimator.cs",
            "Assets/Neoxider/Scripts/Animations/Vector3Animator.cs",
            // Bonus / Slot
            "Assets/Neoxider/Scripts/Bonus/LineRoulett.cs",
            "Assets/Neoxider/Scripts/Bonus/Slot/CheckSpin.cs",
            "Assets/Neoxider/Scripts/Bonus/Slot/Data/BetsData.cs",
            "Assets/Neoxider/Scripts/Bonus/Slot/Data/LinesData.cs",
            "Assets/Neoxider/Scripts/Bonus/Slot/Data/SpriteMultiplayerData.cs",
            "Assets/Neoxider/Scripts/Bonus/Slot/Data/SpritesData.cs",
            "Assets/Neoxider/Scripts/Bonus/Slot/Row.cs",
            "Assets/Neoxider/Scripts/Bonus/Slot/SlotElement.cs",
            "Assets/Neoxider/Scripts/Bonus/Slot/SpeedControll.cs",
            "Assets/Neoxider/Scripts/Bonus/Slot/SpinController.cs",
            "Assets/Neoxider/Scripts/Bonus/Slot/VisualSlotLines.cs",
            "Assets/Neoxider/Scripts/Bonus/Slot/WinLineRendererPlayback.cs",
            "Assets/Neoxider/Scripts/Bonus/TimeReward/TimeReward.cs",
            // Extensions
            "Assets/Neoxider/Scripts/Extensions/Shapes.cs",
            // Level
            "Assets/Neoxider/Scripts/Level/LevelButton.cs",
            "Assets/Neoxider/Scripts/Level/Map.cs",
            // Network
            "Assets/Neoxider/Scripts/Network/Core/NetworkActionRelay.cs",
            "Assets/Neoxider/Scripts/Network/Core/NetworkContextActionRelay.cs",
            "Assets/Neoxider/Scripts/Network/Core/NetworkEventDispatcher.cs",
            "Assets/Neoxider/Scripts/Network/Core/NetworkOwnerFilter.cs",
            "Assets/Neoxider/Scripts/Network/Core/NetworkPropertySync.cs",
            "Assets/Neoxider/Scripts/Network/Core/NeoNetworkComponent.cs",
            // NPC
            "Assets/Neoxider/Scripts/NPC/NpcNavigation.cs",
            // PropertyAttribute
            "Assets/Neoxider/Scripts/PropertyAttribute/GUIColorAttribute.cs",
            // Rpg
            "Assets/Neoxider/Scripts/Rpg/Components/Weapons/MeleeWeapon.cs",
            // Save
            "Assets/Neoxider/Scripts/Save/Providers/FileSaveProvider.cs",
            // Shop
            "Assets/Neoxider/Scripts/Shop/ButtonPrice.cs",
            "Assets/Neoxider/Scripts/Shop/Data/ShopBundleData.cs",
            "Assets/Neoxider/Scripts/Shop/Money.cs",
            "Assets/Neoxider/Scripts/Shop/Shop.cs",
            "Assets/Neoxider/Scripts/Shop/ShopItem.cs",
            "Assets/Neoxider/Scripts/Shop/ShopItemData.cs",
            // StateMachine
            "Assets/Neoxider/Scripts/StateMachine/NoCode/StateMachineData.cs",
            // Tools
            "Assets/Neoxider/Scripts/Tools/Components/AttackSystem/AdvancedAttackCollider.cs",
            "Assets/Neoxider/Scripts/Tools/Components/AttackSystem/AttackExecution.cs",
            "Assets/Neoxider/Scripts/Tools/Dialogue/DialogueController.cs",
            "Assets/Neoxider/Scripts/Tools/Dialogue/DialogueData.cs",
            "Assets/Neoxider/Scripts/Tools/Dialogue/DialogueUI.cs",
            "Assets/Neoxider/Scripts/Tools/Draw/Drawer.cs",
            "Assets/Neoxider/Scripts/Tools/FakeLeaderboard/Leaderboard.cs",
            "Assets/Neoxider/Scripts/Tools/FakeLeaderboard/LeaderboardItem.cs",
            "Assets/Neoxider/Scripts/Tools/FakeLeaderboard/LeaderboardMove.cs",
            "Assets/Neoxider/Scripts/Tools/Input/MouseEffect.cs",
            "Assets/Neoxider/Scripts/Tools/Input/MouseInputManager.cs",
            "Assets/Neoxider/Scripts/Tools/Input/MultiKeyEventTrigger.cs",
            "Assets/Neoxider/Scripts/Tools/Input/SwipeController.cs",
            "Assets/Neoxider/Scripts/Tools/InteractableObject/InteractiveObject.cs",
            "Assets/Neoxider/Scripts/Tools/InteractableObject/PhysicsEvents2D.cs",
            "Assets/Neoxider/Scripts/Tools/InteractableObject/PhysicsEvents3D.cs",
            "Assets/Neoxider/Scripts/Tools/Managers/GM.cs",
            "Assets/Neoxider/Scripts/Tools/Move/AdvancedForceApplier.cs",
            "Assets/Neoxider/Scripts/Tools/Move/CameraRotationController.cs",
            "Assets/Neoxider/Scripts/Tools/Move/DistanceChecker.cs",
            "Assets/Neoxider/Scripts/Tools/Move/Follow.cs",
            "Assets/Neoxider/Scripts/Tools/Move/MovementToolkit/ConstantMover.cs",
            "Assets/Neoxider/Scripts/Tools/Move/MovementToolkit/ConstantRotator.cs",
            "Assets/Neoxider/Scripts/Tools/Other/AiNavigation.cs",
            "Assets/Neoxider/Scripts/Tools/Other/CameraShake.cs",
            "Assets/Neoxider/Scripts/Tools/Spawner/PoolManager.cs",
            "Assets/Neoxider/Scripts/Tools/Spawner/SimpleSpawner.cs",
            "Assets/Neoxider/Scripts/Tools/Spawner/Spawner.cs",
            "Assets/Neoxider/Scripts/Tools/Text/SetText.cs",
            "Assets/Neoxider/Scripts/Tools/Time/TimerObject.cs",
            "Assets/Neoxider/Scripts/Tools/View/LightAnimator.cs",
            "Assets/Neoxider/Scripts/Tools/View/MeshEmission.cs",
            "Assets/Neoxider/Scripts/Tools/View/Selector.cs",
            // UI
            "Assets/Neoxider/Scripts/UI/AnimationFly.cs",
            "Assets/Neoxider/Scripts/UI/Simple/UI.cs",
            "Assets/Neoxider/Scripts/UI/View/VariantView.cs",
            "Assets/Neoxider/Scripts/UI/View/VisualToggle.cs",
            // Cards
            "Assets/Neoxider/Scripts/Cards/Model/BoardModel.cs",
            // Core
            "Assets/Neoxider/Scripts/Core/Resources/Components/ResourceEntryInspector.cs",
            // Extensions
            "Assets/Neoxider/Scripts/Extensions/LegacyComponentAttribute.cs",
            // PropertyAttribute (attribute constructors use camelCase by convention)
            "Assets/Neoxider/Scripts/PropertyAttribute/ButtonAttribute.cs",
            "Assets/Neoxider/Scripts/PropertyAttribute/InjectAttribute/FindAllInSceneAttribute.cs",
            "Assets/Neoxider/Scripts/PropertyAttribute/InjectAttribute/GetComponentAttribute.cs",
            "Assets/Neoxider/Scripts/PropertyAttribute/InjectAttribute/GetComponentsAttribute.cs",
            "Assets/Neoxider/Scripts/PropertyAttribute/InjectAttribute/LoadAllFromResourcesAttribute.cs",
            "Assets/Neoxider/Scripts/PropertyAttribute/InjectAttribute/LoadFromResourcesAttribute.cs",
            // Rpg
            "Assets/Neoxider/Scripts/Rpg/Data/RpgCharacterTemplate.cs",
            "Assets/Neoxider/Scripts/Rpg/Data/RpgProgressionDefinition.cs",
            "Assets/Neoxider/Scripts/Rpg/Data/RpgRegenDefinition.cs",
            "Assets/Neoxider/Scripts/Rpg/Data/RpgResourceDefinition.cs",
            "Assets/Neoxider/Scripts/Rpg/Data/RpgResourceModifier.cs",
            "Assets/Neoxider/Scripts/Rpg/Data/RpgStatDefinition.cs",
            "Assets/Neoxider/Scripts/Rpg/Data/RpgStatId.cs",
            "Assets/Neoxider/Scripts/Rpg/Data/RpgStatUpgradeRule.cs",
            // Shop
            "Assets/Neoxider/Scripts/Shop/TextMoney.cs",
            // StateMachine
            "Assets/Neoxider/Scripts/StateMachine/StateMachine.cs",
            // Tools — additional files
            "Assets/Neoxider/Scripts/Tools/Components/AttackSystem/Evade.cs",
            "Assets/Neoxider/Scripts/Tools/Components/AttackSystem/Health.cs",
            "Assets/Neoxider/Scripts/Tools/Components/Loot.cs",
            "Assets/Neoxider/Scripts/Tools/Components/ScoreManager.cs",
            "Assets/Neoxider/Scripts/Tools/Components/TypewriterEffect.cs",
            "Assets/Neoxider/Scripts/Tools/Debug/ErrorLogger.cs",
            "Assets/Neoxider/Scripts/Tools/Move/UniversalRotator.cs",
            "Assets/Neoxider/Scripts/Tools/Text/TimeToText.cs",
        };

        // Matches: "public <type> <camelCaseName>" where the member name starts with a lowercase letter.
        // Excludes: operator overloads, explicit interface implementations.
        // [Obsolete] forwarders are excluded by a separate line-context check.
        private static readonly Regex CamelCasePublicMemberPattern = new Regex(
            @"^\s*public\s+(?:static\s+|readonly\s+|override\s+|virtual\s+|abstract\s+|sealed\s+|new\s+)*" +
            @"(?!operator\b)(?!explicit\b)(?!implicit\b)" +
            @"\S+\s+([a-z][a-zA-Z0-9_]*)\s*[{;=(]",
            RegexOptions.Compiled);

        [Test]
        public void RuntimePublicMembers_UsePascalCase()
        {
            // Scans runtime .cs files for public members whose name starts with a lowercase letter.
            // Files listed in CamelCasePascalViolationAllowedFiles are pre-existing tech debt and
            // are skipped. The test will FAIL if a NEW file introduces such a violation.
            // Members decorated with [Obsolete] (on the immediately preceding line) are skipped
            // because those are intentional back-compat forwarders.

            List<string> offenders = new();

            foreach (string file in Directory.GetFiles(RuntimeRoot, "*.cs", SearchOption.AllDirectories))
            {
                string normalized = file.Replace('\\', '/');

                if (CamelCasePascalViolationAllowedFiles.Contains(normalized))
                    continue;

                // Skip Editor-only files
                if (normalized.Contains("/Editor/"))
                    continue;

                string[] lines = File.ReadAllLines(file);

                for (int i = 0; i < lines.Length; i++)
                {
                    Match m = CamelCasePublicMemberPattern.Match(lines[i]);
                    if (!m.Success)
                        continue;

                    // Skip if the immediately preceding non-blank line contains [Obsolete]
                    bool isObsolete = false;
                    for (int j = i - 1; j >= 0 && j >= i - 3; j--)
                    {
                        string prev = lines[j].Trim();
                        if (prev.Length == 0)
                            continue;
                        if (prev.Contains("[Obsolete") || prev.Contains("[System.Obsolete"))
                            isObsolete = true;
                        break;
                    }

                    if (isObsolete)
                        continue;

                    string memberName = m.Groups[1].Value;

                    // Operator overloads (e.g. "public static bool operator ==") are not named members:
                    // the return type sits between "public" and the "operator" keyword, so the misplaced
                    // lookahead lets "operator" be captured as the name. Skip them explicitly.
                    if (memberName == "operator")
                        continue;

                    offenders.Add($"{normalized}:{i + 1}: public member '{memberName}' should be PascalCase");
                }
            }

            Assert.That(offenders, Is.Empty,
                "New public fields/properties must use PascalCase. Add [Obsolete] forwarder for back-compat if renaming a public API.");
        }

        private static bool IsTrackedTextExtension(string extension)
        {
            foreach (string tracked in TextFileExtensions)
            {
                if (extension == tracked)
                {
                    return true;
                }
            }

            return false;
        }
    }
}
