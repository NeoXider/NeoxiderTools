using System.Collections.Generic;
using System.IO;
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
